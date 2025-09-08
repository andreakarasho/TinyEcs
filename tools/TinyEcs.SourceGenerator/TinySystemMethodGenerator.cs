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

        // Determine how to call the method (static vs instance)
        string methodCall;
        if (method.IsStatic)
        {
            methodCall = $"{pluginClassName}.{method.Name}({methodCallParameters})";
        }
        else
        {
            // For instance methods, we need to create an instance of the plugin class
            methodCall = $"new {pluginClassName}().{method.Name}({methodCallParameters})";
        }

        // Generate the adapter class
        var adapterClass = new StringBuilder();

        if (!string.IsNullOrEmpty(ns))
        {
            adapterClass.AppendLine($"namespace {ns}");
            adapterClass.AppendLine("{");
        }

        adapterClass.AppendLine($@"    public class {adapterName} : TinyEcs.TinySystem
    {{
{fieldDeclarations}
        protected override void Setup(TinyEcs.SystemParamBuilder builder)
        {{
{setupAssignments}        }}

        protected override bool Execute(TinyEcs.World world)
        {{
            Lock();
            world.BeginDeferred();
            {methodCall};
            world.EndDeferred();
            Unlock();
            return true;
        }}
    }}");

        if (!string.IsNullOrEmpty(ns))
        {
            adapterClass.AppendLine("}");
        }

        var sourceText = adapterClass.ToString();
        context.AddSource($"{adapterName}.g.cs", sourceText);
    }

    private class PluginClassInfo
    {
        public INamedTypeSymbol ClassSymbol { get; set; }
        public List<IMethodSymbol> TinySystemMethods { get; set; }
    }
}
