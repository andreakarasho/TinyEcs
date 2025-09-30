using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0055
#pragma warning disable IDE0008
#pragma warning disable IDE0058

namespace TinyEcs.SourceGenerator;

[Generator]
public sealed class Program : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find IPlugin classes that contain methods with [TinySystem] attributes
        var pluginClassesWithTinySystemMethods = context.SyntaxProvider.CreateSyntaxProvider(
            static (s, _) => (s is ClassDeclarationSyntax || s is StructDeclarationSyntax) &&
                            s is TypeDeclarationSyntax typeDecl &&
                            typeDecl.Members.OfType<MethodDeclarationSyntax>()
                                     .Any(m => m.AttributeLists.Count > 0),
            static (ctx, _) =>
            {
                var typeDecl = (TypeDeclarationSyntax)ctx.Node;
                var typeSymbol = ctx.SemanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;

                if (typeSymbol == null) return null;

                // Check if the type implements IPlugin
                var implementsIPlugin = typeSymbol.AllInterfaces.Any(i =>
                    i.ToDisplayString() == "TinyEcs.IPlugin");

                if (!implementsIPlugin) return null;

                // Find methods with [TinySystem] attribute
                var tinySystemMethods = typeSymbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.GetAttributes().Any(attr =>
                        attr.AttributeClass?.ToDisplayString() == "TinyEcs.TinySystemAttribute"))
                    .ToList();

                if (!tinySystemMethods.Any()) return null;

                return new SystemMethodInfo
                {
                    ContainingType = typeSymbol,
                    TinySystemMethods = tinySystemMethods,
                    IsFromPlugin = true
                };
            }
        ).Where(static info => info != null);

        // Find all methods with [TinySystem] attributes (including those outside IPlugin classes/structs)
        var allTinySystemMethods = context.SyntaxProvider.CreateSyntaxProvider(
            static (s, _) => s is MethodDeclarationSyntax methodDecl &&
                            methodDecl.AttributeLists.Count > 0,
            static (ctx, _) =>
            {
                var methodDecl = (MethodDeclarationSyntax)ctx.Node;
                var methodSymbol = ctx.SemanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;

                if (methodSymbol == null) return null;

                // Check if method has [TinySystem] attribute
                var hasTinySystemAttribute = methodSymbol.GetAttributes().Any(attr =>
                    attr.AttributeClass?.ToDisplayString() == "TinyEcs.TinySystemAttribute");

                if (!hasTinySystemAttribute) return null;

                var containingType = methodSymbol.ContainingType;

                // Check if the containing type implements IPlugin
                var implementsIPlugin = containingType.AllInterfaces.Any(i =>
                    i.ToDisplayString() == "TinyEcs.IPlugin");

                // Only include if it's NOT from an IPlugin type (those are handled separately)
                if (implementsIPlugin) return null;

                return new SystemMethodInfo
                {
                    ContainingType = containingType,
                    TinySystemMethods = new List<IMethodSymbol> { methodSymbol },
                    IsFromPlugin = false
                };
            }
        ).Where(static info => info != null);

        // Combine both providers
        var allSystemMethods = pluginClassesWithTinySystemMethods.Collect()
            .Combine(allTinySystemMethods.Collect())
            .Select(static (combined, _) =>
            {
                var (pluginMethods, standaloneMethods) = combined;
                var result = new List<SystemMethodInfo>();

                if (!pluginMethods.IsDefaultOrEmpty)
                    result.AddRange(pluginMethods);

                if (!standaloneMethods.IsDefaultOrEmpty)
                    result.AddRange(standaloneMethods);

                return result.ToImmutableArray();
            });

        // Generate adapter classes for each method
        context.RegisterSourceOutput(
            allSystemMethods,
            (spc, systemMethods) =>
            {
                if (systemMethods.IsDefaultOrEmpty) return;

                foreach (var systemMethod in systemMethods)
                {
                    foreach (var method in systemMethod.TinySystemMethods)
                    {
                        GenerateAdapterClass(spc, systemMethod.ContainingType, method, systemMethod.IsFromPlugin);
                    }
                }
            }
        );
    }

    private static void GenerateAdapterClass(SourceProductionContext context, INamedTypeSymbol containingType, IMethodSymbol method, bool isFromPlugin)
    {
        var adapterName = $"{method.Name}Adapter";
        var ns = containingType.ContainingNamespace.IsGlobalNamespace ? "" : containingType.ContainingNamespace.ToDisplayString();
        var instanceTypeName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        ValidateMethodRequirements(context, method);
        var returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean;
        var baseClass = returnsBool ? "TinyEcs.TinyConditionalSystem" : "TinyEcs.TinySystem";
        var classVisibility = GetClassVisibility(containingType);
        var parameters = method.Parameters.ToList();
        var setupAssignments = new StringBuilder();
        var methodCallParameters = new StringBuilder();
        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var paramType = param.Type.ToDisplayString();
            setupAssignments.AppendLine($"        builder.Add<{paramType}>();");
            if (i > 0) methodCallParameters.Append(", ");
            methodCallParameters.Append($"({paramType})SystemParams[{i}]");
        }
        string instanceField = "";
        string ctor = "";
        string methodCall;
        var adapterClass = new IndentedStringBuilder();
        if (!string.IsNullOrEmpty(ns))
        {
            adapterClass.AppendLine($"namespace {ns}");
            adapterClass.AppendLine("{");
            adapterClass.IncrementIndent();
        }
        adapterClass.AppendLine($"{classVisibility} sealed partial class {adapterName} : {baseClass}");
        adapterClass.AppendLine("{");
        adapterClass.IncrementIndent();
        bool isPrivate = method.DeclaredAccessibility == Accessibility.Private;
        bool isStruct = containingType.TypeKind == TypeKind.Struct;
        if (isPrivate)
        {
            // Generate UnsafeAccessor for private method
            string accessorName = $"Invoke_{method.Name}_Accessor";
            string paramList = string.Join(", ", parameters.Select((p, i) => $"{p.Type.ToDisplayString()} arg{i}"));
            string targetType = instanceTypeName;
            string instanceParam = method.IsStatic ? "" : (isStruct ? $"ref {targetType} instance, " : $"{targetType} instance, ");
            string unsafeAccessorKind = method.IsStatic
                ? "global::System.Runtime.CompilerServices.UnsafeAccessorKind.StaticMethod"
                : "global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method";
            adapterClass.AppendLine($"    [global::System.Runtime.CompilerServices.UnsafeAccessor({unsafeAccessorKind}, Name = \"{method.Name}\")]" );
            adapterClass.AppendLine($"    private static extern {method.ReturnType.ToDisplayString()} {accessorName}({instanceParam}{paramList});");
            adapterClass.AppendLine();
            if (!method.IsStatic)
            {
                // Always declare the field without 'ref'
                instanceField = $"private {instanceTypeName} _instance;";
                // Constructor for both structs and classes accepts by value
                ctor = $"public {adapterName}({instanceTypeName} instance) {{ _instance = instance; }}";
                adapterClass.AppendLine(instanceField);
                adapterClass.AppendLine();
                adapterClass.AppendLine(ctor);
            }
            adapterClass.AppendLine();
            adapterClass.AppendLine("protected override void Setup(TinyEcs.SystemParamBuilder builder)");
            adapterClass.AppendLine("{");
            adapterClass.IncrementIndent();
            foreach (var line in setupAssignments.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                adapterClass.AppendLine(line);
            adapterClass.DecrementIndent();
            adapterClass.AppendLine("}");
            adapterClass.AppendLine();
            adapterClass.AppendLine("protected override bool Execute(TinyEcs.World world)");
            adapterClass.AppendLine("{");
            adapterClass.IncrementIndent();
            adapterClass.AppendLine("Lock();");
            adapterClass.AppendLine("world.BeginDeferred();");
            string call = method.IsStatic
                ? $"{accessorName}({methodCallParameters})"
                : (isStruct
                    ? $"{accessorName}(ref _instance, {methodCallParameters})"
                    : $"{accessorName}(_instance, {methodCallParameters})");
            if (returnsBool)
            {
                adapterClass.AppendLine($"bool result = {call};");
                adapterClass.AppendLine("world.EndDeferred();");
                adapterClass.AppendLine("Unlock();");
                adapterClass.AppendLine("return result;");
            }
            else
            {
                adapterClass.AppendLine($"{call};");
                adapterClass.AppendLine("world.EndDeferred();");
                adapterClass.AppendLine("Unlock();");
                adapterClass.AppendLine("return true;");
            }
            adapterClass.DecrementIndent();
            adapterClass.AppendLine("}");
        }
        else
        {
            if (!method.IsStatic)
            {
                instanceField = $"private readonly {instanceTypeName} _instance;";
                ctor = $"public {adapterName}({instanceTypeName} instance) {{ _instance = instance; }}";
                adapterClass.AppendLine(instanceField);
                adapterClass.AppendLine();
                adapterClass.AppendLine(ctor);
            }
            adapterClass.AppendLine();
            adapterClass.AppendLine("protected override void Setup(TinyEcs.SystemParamBuilder builder)");
            adapterClass.AppendLine("{");
            adapterClass.IncrementIndent();
            foreach (var line in setupAssignments.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                adapterClass.AppendLine(line);
            adapterClass.DecrementIndent();
            adapterClass.AppendLine("}");
            adapterClass.AppendLine();
            adapterClass.AppendLine("protected override bool Execute(TinyEcs.World world)");
            adapterClass.AppendLine("{");
            adapterClass.IncrementIndent();
            adapterClass.AppendLine("Lock();");
            adapterClass.AppendLine("world.BeginDeferred();");
            methodCall = method.IsStatic
                ? $"{instanceTypeName}.{method.Name}({methodCallParameters})"
                : $"_instance.{method.Name}({methodCallParameters})";
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
        }
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

    private static string GetClassVisibility(INamedTypeSymbol containingType)
    {
        return containingType.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => "internal" // Default to internal if accessibility is not recognized
        };
    }

    private static void ValidateMethodRequirements(SourceProductionContext context, IMethodSymbol method)
    {
        // No diagnostics needed; private methods are now supported via UnsafeAccessor
    }

    private class SystemMethodInfo
    {
        public INamedTypeSymbol ContainingType { get; set; }
        public List<IMethodSymbol> TinySystemMethods { get; set; }
        public bool IsFromPlugin { get; set; }
    }
}
