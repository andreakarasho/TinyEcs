using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable IDE0055
#pragma warning disable IDE0008
#pragma warning disable IDE0058

namespace TinyEcs.SourceGenerator;

[Generator]
public sealed class TinySystemMethodGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find IPlugin classes that contain methods with [TinySystem] attributes
        var pluginClassesWithTinySystemMethods = context.SyntaxProvider.CreateSyntaxProvider(
            static (s, _) => s is ClassDeclarationSyntax classDecl &&
                            classDecl.Members.OfType<MethodDeclarationSyntax>()
                                     .Any(m => m.AttributeLists.Count > 0),
            static (ctx, _) =>
            {
                var classDecl = (ClassDeclarationSyntax)ctx.Node;
                var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

                if (classSymbol == null) return null;

                // Check if the class implements IPlugin
                var implementsIPlugin = classSymbol.AllInterfaces.Any(i =>
                    i.ToDisplayString() == "TinyEcs.IPlugin");

                if (!implementsIPlugin) return null;

                // Find methods with [TinySystem] attribute
                var tinySystemMethods = classSymbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.GetAttributes().Any(attr =>
                        attr.AttributeClass?.ToDisplayString() == "TinyEcs.TinySystemAttribute"))
                    .ToList();

                if (!tinySystemMethods.Any()) return null;

                return new PluginClassInfo
                {
                    ClassSymbol = classSymbol,
                    TinySystemMethods = tinySystemMethods
                };
            }
        ).Where(static info => info != null);

        // Generate adapter classes for each method
        context.RegisterSourceOutput(
            pluginClassesWithTinySystemMethods.Collect(),
            (spc, pluginClasses) =>
            {
                if (pluginClasses.IsDefaultOrEmpty) return;

                foreach (var pluginClass in pluginClasses)
                {
                    foreach (var method in pluginClass.TinySystemMethods)
                    {
                        GenerateAdapterClass(spc, pluginClass.ClassSymbol, method);
                    }
                }
            }
        );
    }

    private static void GenerateAdapterClass(SourceProductionContext context, INamedTypeSymbol pluginClass, IMethodSymbol method)
    {
        var adapterName = $"{method.Name}Adapter";
        var pluginClassName = pluginClass.ToDisplayString();
        var ns = pluginClass.ContainingNamespace.IsGlobalNamespace ? "" : pluginClass.ContainingNamespace.ToDisplayString();

        // Validate method requirements for TinySystemAttribute
        ValidateMethodRequirements(context, method);

        // Check if method returns bool (for conditional systems)
        var returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean;
        var conditionalInterface = returnsBool ? ", TinyEcs.ITinyConditionalSystem" : "";

        // Get method parameters for dependency injection
        var parameters = method.Parameters.ToList();
        var fieldDeclarations = new StringBuilder();
        var setupAssignments = new StringBuilder();
        var methodCallParameters = new StringBuilder();

        // Generate field declarations and setup assignments
        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var fieldName = $"_{param.Name}";
            var paramType = param.Type.ToDisplayString();

            fieldDeclarations.AppendLine($"    private {paramType} {fieldName};");
            setupAssignments.AppendLine($"        {fieldName} = builder.Add<{paramType}>();");

            if (i > 0) methodCallParameters.Append(", ");
            methodCallParameters.Append(fieldName);
        }

        // For non-static methods, add a field for the plugin instance
        string instanceField = "";
        string ctor = "";
        string methodCall;

        // Use a single IndentedStringBuilder for all code blocks
        var adapterClass = new IndentedStringBuilder();

        if (!string.IsNullOrEmpty(ns))
        {
            adapterClass.AppendLine($"namespace {ns}");
            adapterClass.AppendLine("{");
            adapterClass.IncrementIndent();
        }

        // Make the adapter class sealed and partial
        adapterClass.AppendLine($"public sealed partial class {adapterName} : TinyEcs.TinySystem{conditionalInterface}");
        adapterClass.AppendLine("{");
        adapterClass.IncrementIndent();

        // Fields
        foreach (var line in fieldDeclarations.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            adapterClass.AppendLine(line);

        if (method.IsStatic)
        {
            methodCall = $"{pluginClassName}.{method.Name}({methodCallParameters})";
        }
        else
        {
            instanceField = $"private readonly {pluginClassName} _instance;";
            ctor = $"public {adapterName}({pluginClassName} instance) {{ _instance = instance; }}";
            methodCall = $"_instance.{method.Name}({methodCallParameters})";
            adapterClass.AppendLine(instanceField);
            adapterClass.AppendLine(ctor);
        }

        // Setup method
        adapterClass.AppendLine("protected override void Setup(TinyEcs.SystemParamBuilder builder)");
        adapterClass.AppendLine("{");
        adapterClass.IncrementIndent();
        foreach (var line in setupAssignments.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            adapterClass.AppendLine(line);
        adapterClass.DecrementIndent();
        adapterClass.AppendLine("}");

        // Execute method
        adapterClass.AppendLine("protected override bool Execute(TinyEcs.World world)");
        adapterClass.AppendLine("{");
        adapterClass.IncrementIndent();
        adapterClass.AppendLine("Lock();");
        adapterClass.AppendLine("world.BeginDeferred();");
        if (returnsBool)
        {
            adapterClass.AppendLine($"bool result = {methodCall};");
            adapterClass.AppendLine("world.EndDeferred();");
            adapterClass.AppendLine("Unlock();");
            adapterClass.AppendLine("return result;");
        }
        else
        {
            adapterClass.AppendLine($"{methodCall};");
            adapterClass.AppendLine("world.EndDeferred();");
            adapterClass.AppendLine("Unlock();");
            adapterClass.AppendLine("return true;");
        }
        adapterClass.DecrementIndent();
        adapterClass.AppendLine("}");

        adapterClass.DecrementIndent();
        adapterClass.AppendLine("}");

        if (!string.IsNullOrEmpty(ns))
        {
            adapterClass.DecrementIndent();
            adapterClass.AppendLine("}");
        }

        var sourceText = adapterClass.ToString();
        context.AddSource($"{adapterName}.g.cs", sourceText);
    }

    private static void ValidateMethodRequirements(SourceProductionContext context, IMethodSymbol method)
    {
        var location = method.Locations.FirstOrDefault() ?? Location.None;

        // Check if method is public
        if (method.DeclaredAccessibility != Accessibility.Public)
        {
            var descriptor = new DiagnosticDescriptor(
                "TINYECS001",
                "TinySystem method must be public",
                "Method '{0}' with [TinySystem] attribute must be public",
                "TinyEcs",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, method.Name));
        }

        // Check if method is static
        // if (!method.IsStatic)
        // {
        //     var descriptor = new DiagnosticDescriptor(
        //         "TINYECS002",
        //         "TinySystem method must be static",
        //         "Method '{0}' with [TinySystem] attribute must be static",
        //         "TinyEcs",
        //         DiagnosticSeverity.Error,
        //         isEnabledByDefault: true);
        //
        //     context.ReportDiagnostic(Diagnostic.Create(descriptor, location, method.Name));
        // }
    }

    private class PluginClassInfo
    {
        public INamedTypeSymbol ClassSymbol { get; set; }
        public List<IMethodSymbol> TinySystemMethods { get; set; }
    }
}
