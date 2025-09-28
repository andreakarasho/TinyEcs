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

        // Find compatible methods for cached delegate generation (any accessibility)
        var allCompatibleMethods = context.SyntaxProvider.CreateSyntaxProvider(
            static (s, _) => s is MethodDeclarationSyntax methodDecl,
            static (ctx, _) =>
            {
                var methodDecl = (MethodDeclarationSyntax)ctx.Node;
                var methodSymbol = ctx.SemanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;

                if (methodSymbol == null) return null;

                // Check if all parameters implement ISystemParam
                foreach (var param in methodSymbol.Parameters)
                {
                    var paramType = param.Type;
                    
                    // Check if parameter implements ISystemParam
                    var implementsISystemParam = paramType.AllInterfaces.Any(i =>
                        i.ToDisplayString().StartsWith("TinyEcs.ISystemParam"));

                    // If any parameter doesn't implement ISystemParam, exclude this method
                    if (!implementsISystemParam)
                    {
                        return null;
                    }
                }

                return methodSymbol;
            }
        ).Where(static method => method != null);

        // Combine TinySystem methods for adapter class generation
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

        // Generate cached delegates and systems for ALL compatible methods
        context.RegisterSourceOutput(
            allCompatibleMethods.Collect(),
            (spc, compatibleMethods) =>
            {
                if (compatibleMethods.IsDefaultOrEmpty) return;

                // Create delegate signature cache
                var delegateCache = new Dictionary<string, (int Index, string ReturnType)>();
                var currentIndex = 0;

                // Create extension class cache for each unique signature
                var extensionCache = new Dictionary<string, string>();

                // Create stub system cache for each unique signature
                var stubSystemCache = new Dictionary<string, (string Name, IList<IParameterSymbol> Parameters, bool ReturnsBool)>();

                // Collect all unique delegate signatures from ALL compatible methods
                foreach (var method in compatibleMethods)
                {
                    // Double-check that all parameters implement ISystemParam before processing
                    var hasIncompatibleParams = method.Parameters.Any(param =>
                    {
                        var paramType = param.Type;
                        return !paramType.AllInterfaces.Any(i =>
                            i.ToDisplayString().StartsWith("TinyEcs.ISystemParam"));
                    });

                    // Skip methods with incompatible parameters
                    if (hasIncompatibleParams)
                    {
                        continue;
                    }

                    var returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean;
                    var delegateReturnType = returnsBool ? "bool" : "void";

                    var parameters = method.Parameters.ToList();
                    var delegateSignature = string.Join(", ", parameters.Select((p, i) => $"{p.Type.ToDisplayString()} arg{i}"));
                    var fullSignature = $"{delegateReturnType}({delegateSignature})";

                    // Cache delegate signature
                    if (!delegateCache.ContainsKey(fullSignature))
                    {
                        delegateCache[fullSignature] = (currentIndex++, delegateReturnType);
                    }

                    // Cache extension class name for this signature
                    if (!extensionCache.ContainsKey(fullSignature))
                    {
                        var delegateIndex = delegateCache[fullSignature].Index;
                        extensionCache[fullSignature] = $"OnTinyDelegate{delegateIndex}Ext";
                    }

                    // Cache stub system name for this signature
                    if (!stubSystemCache.ContainsKey(fullSignature))
                    {
                        var delegateIndex = delegateCache[fullSignature].Index;
                        var stubSystemName = $"OnTinyDelegate{delegateIndex}StubSystem";
                        stubSystemCache[fullSignature] = (stubSystemName, method.Parameters.ToList(), returnsBool);
                    }
                }

                // Generate delegates file
                GenerateDelegatesFile(spc, delegateCache);

                // Generate cached extensions file
                GenerateExtensionsFile(spc, delegateCache, extensionCache);

                // Generate cached stub systems file
                GenerateStubSystemsFile(spc, delegateCache, stubSystemCache);
            }
        );

        // Generate adapter classes for TinySystem methods only
        context.RegisterSourceOutput(
            allSystemMethods,
            (spc, systemMethods) =>
            {
                if (systemMethods.IsDefaultOrEmpty) return;

                // Generate adapter classes only for methods with [TinySystem] attribute
                foreach (var systemMethod in systemMethods)
                {
                    foreach (var method in systemMethod.TinySystemMethods)
                    {
                        // Generate only adapter classes (stub systems are now cached)
                        GenerateAdapterClass(spc, systemMethod.ContainingType, method, systemMethod.IsFromPlugin);
                    }
                }
            }
        );
    }

    private static void GenerateDelegatesFile(SourceProductionContext context, Dictionary<string, (int Index, string ReturnType)> delegateCache)
    {
        var delegatesClass = new IndentedStringBuilder();

        delegatesClass.AppendLine("// Auto-generated delegate types for TinyEcs stub systems");
        delegatesClass.AppendLine("// Only includes signatures where all parameters implement ISystemParam");
        delegatesClass.AppendLine("namespace TinyEcs");
        delegatesClass.AppendLine("{");
        delegatesClass.IncrementIndent();

        foreach (var kvp in delegateCache.OrderBy(x => x.Value.Index))
        {
            var signature = kvp.Key;
            var (index, returnType) = kvp.Value;

            // Extract parameter part from full signature like "void(World world, Query<...> query)"
            var paramStart = signature.IndexOf('(');
            var paramEnd = signature.LastIndexOf(')');
            var parameters = signature.Substring(paramStart + 1, paramEnd - paramStart - 1);

            delegatesClass.AppendLine($"internal delegate {returnType} OnTinyDelegate{index}({parameters});");
        }

        delegatesClass.DecrementIndent();
        delegatesClass.AppendLine("}");

        context.AddSource("TinyDelegates.g.cs", delegatesClass.ToString());
    }

    private static void GenerateExtensionsFile(SourceProductionContext context, Dictionary<string, (int Index, string ReturnType)> delegateCache, Dictionary<string, string> extensionCache)
    {
        var extensionsClass = new IndentedStringBuilder();

        extensionsClass.AppendLine("// Auto-generated extension methods for TinyEcs stub systems");
        extensionsClass.AppendLine("namespace TinyEcs");
        extensionsClass.AppendLine("{");
        extensionsClass.IncrementIndent();

        // Generate extension classes for each unique delegate signature
        foreach (var kvp in extensionCache.OrderBy(x => delegateCache[x.Key].Index))
        {
            var signature = kvp.Key;
            var extensionClassName = kvp.Value;
            var (index, returnType) = delegateCache[signature];

            extensionsClass.AppendLine($"internal static class {extensionClassName}");
            extensionsClass.AppendLine("{");
            extensionsClass.IncrementIndent();

            if (returnType == "bool")
            {
                // Generate RunIf extension method for bool-returning delegates
                extensionsClass.AppendLine($"public static TinyEcs.ITinySystem RunIf(this TinyEcs.ITinySystem system, OnTinyDelegate{index} condition)");
                extensionsClass.AppendLine("{");
                extensionsClass.IncrementIndent();
                extensionsClass.AppendLine($"var conditionSystem = new OnTinyDelegate{index}StubSystem(condition);");
                extensionsClass.AppendLine("return system.RunIf(conditionSystem);");
                extensionsClass.DecrementIndent();
                extensionsClass.AppendLine("}");
            }
            else
            {
                // Generate AddSystems extension method for void-returning delegates
                extensionsClass.AppendLine($"public static TinyEcs.ITinySystem AddSystem(this TinyEcs.Scheduler scheduler, TinyEcs.Stage stage, OnTinyDelegate{index} del)");
                extensionsClass.AppendLine("{");
                extensionsClass.IncrementIndent();
                extensionsClass.AppendLine($"var sys = new OnTinyDelegate{index}StubSystem(del);");
                extensionsClass.AppendLine("scheduler.AddSystem(stage, sys);");
                extensionsClass.AppendLine("return sys;");
                extensionsClass.DecrementIndent();
                extensionsClass.AppendLine("}");
                extensionsClass.AppendLine();

                // Also generate string overload
                extensionsClass.AppendLine($"public static TinyEcs.ITinySystem AddSystem(this TinyEcs.Scheduler scheduler, string stageName, OnTinyDelegate{index} del)");
                extensionsClass.AppendLine("{");
                extensionsClass.IncrementIndent();
                extensionsClass.AppendLine($"var sys = new OnTinyDelegate{index}StubSystem(del);");
                extensionsClass.AppendLine("scheduler.AddSystem(stageName, sys);");
                extensionsClass.AppendLine("return sys;");
                extensionsClass.DecrementIndent();
                extensionsClass.AppendLine("}");
            }

            extensionsClass.DecrementIndent();
            extensionsClass.AppendLine("}");
            extensionsClass.AppendLine();
        }

        extensionsClass.DecrementIndent();
        extensionsClass.AppendLine("}");

        context.AddSource("TinyExtensions.g.cs", extensionsClass.ToString());
    }

    private static void GenerateStubSystemsFile(SourceProductionContext context, Dictionary<string, (int Index, string ReturnType)> delegateCache, Dictionary<string, (string Name, IList<IParameterSymbol> Parameters, bool ReturnsBool)> stubSystemCache)
    {
        var stubSystemsClass = new IndentedStringBuilder();

        stubSystemsClass.AppendLine("// Auto-generated cached stub systems for TinyEcs");
        stubSystemsClass.AppendLine("// Only includes systems where all parameters implement ISystemParam");
        stubSystemsClass.AppendLine("namespace TinyEcs");
        stubSystemsClass.AppendLine("{");
        stubSystemsClass.IncrementIndent();

        // Generate cached stub system classes for each unique delegate signature
        foreach (var kvp in stubSystemCache.OrderBy(x => delegateCache[x.Key].Index))
        {
            var signature = kvp.Key;
            var (stubSystemName, parameters, returnsBool) = kvp.Value;
            var (index, returnType) = delegateCache[signature];

            // Validate that all parameters implement ISystemParam before生成 stub system
            var hasValidParameters = parameters.All(param =>
            {
                var paramType = param.Type;
                return paramType.AllInterfaces.Any(i =>
                    i.ToDisplayString().StartsWith("TinyEcs.ISystemParam"));
            });

            // Skip generating stub system if parameters are not valid
            if (!hasValidParameters)
            {
                continue;
            }

            var baseClass = returnsBool ? "TinyEcs.TinyConditionalSystem" : "TinyEcs.TinySystem";
            var delegateName = $"OnTinyDelegate{index}";

            stubSystemsClass.AppendLine($"internal class {stubSystemName} : {baseClass}");
            stubSystemsClass.AppendLine("{");
            stubSystemsClass.IncrementIndent();

            // Private field for delegate
            stubSystemsClass.AppendLine($"private readonly {delegateName} _fn;");
            stubSystemsClass.AppendLine();

            // Constructor
            stubSystemsClass.AppendLine($"public {stubSystemName}({delegateName} fn)");
            stubSystemsClass.AppendLine("{");
            stubSystemsClass.IncrementIndent();
            stubSystemsClass.AppendLine("_fn = fn;");
            stubSystemsClass.DecrementIndent();
            stubSystemsClass.AppendLine("}");
            stubSystemsClass.AppendLine();

            // Setup method using actual parameter symbols
            stubSystemsClass.AppendLine("protected override void Setup(TinyEcs.SystemParamBuilder builder)");
            stubSystemsClass.AppendLine("{");
            stubSystemsClass.IncrementIndent();

            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                var paramType = param.Type.ToDisplayString();
                stubSystemsClass.AppendLine($"        builder.Add<{paramType}>();");
            }

            stubSystemsClass.DecrementIndent();
            stubSystemsClass.AppendLine("}");
            stubSystemsClass.AppendLine();

            // Execute method
            stubSystemsClass.AppendLine("protected override bool Execute(TinyEcs.World world)");
            stubSystemsClass.AppendLine("{");
            stubSystemsClass.IncrementIndent();
            stubSystemsClass.AppendLine("Lock();");
            stubSystemsClass.AppendLine("world.BeginDeferred();");

            // Build parameter list for delegate call using actual parameter types
            if (parameters.Count > 0)
            {
                var executeParameters = string.Join(", ", parameters.Select((p, i) =>
                {
                    var paramType = p.Type.ToDisplayString();
                    return $"({paramType})SystemParams[{i}]";
                }));

                if (returnsBool)
                {
                    stubSystemsClass.AppendLine($"bool result = _fn({executeParameters});");
                    stubSystemsClass.AppendLine("world.EndDeferred();");
                    stubSystemsClass.AppendLine("Unlock();");
                    stubSystemsClass.AppendLine("return result;");
                }
                else
                {
                    stubSystemsClass.AppendLine($"_fn({executeParameters});");
                    stubSystemsClass.AppendLine("world.EndDeferred();");
                    stubSystemsClass.AppendLine("Unlock();");
                    stubSystemsClass.AppendLine("return true;");
                }
            }
            else
            {
                // No parameters
                if (returnsBool)
                {
                    stubSystemsClass.AppendLine("bool result = _fn();");
                    stubSystemsClass.AppendLine("world.EndDeferred();");
                    stubSystemsClass.AppendLine("Unlock();");
                    stubSystemsClass.AppendLine("return result;");
                }
                else
                {
                    stubSystemsClass.AppendLine("_fn();");
                    stubSystemsClass.AppendLine("world.EndDeferred();");
                    stubSystemsClass.AppendLine("Unlock();");
                    stubSystemsClass.AppendLine("return true;");
                }
            }

            stubSystemsClass.DecrementIndent();
            stubSystemsClass.AppendLine("}");
            stubSystemsClass.DecrementIndent();
            stubSystemsClass.AppendLine("}");
            stubSystemsClass.AppendLine();
        }

        stubSystemsClass.DecrementIndent();
        stubSystemsClass.AppendLine("}");

        context.AddSource("TinyStubSystems.g.cs", stubSystemsClass.ToString());
    }

    private static void GenerateStubClass(SourceProductionContext context, INamedTypeSymbol containingType, IMethodSymbol method, bool isFromPlugin, int delegateIndex)
    {
        var stubName = $"{method.Name}StubSystem";
        var ns = containingType.ContainingNamespace.IsGlobalNamespace ? "" : containingType.ContainingNamespace.ToDisplayString();
        var delegateName = $"TinyEcs.OnTinyDelegate{delegateIndex}";

        // Validate method requirements for TinySystemAttribute
        ValidateMethodRequirements(context, method);

        // Check if method returns bool (for conditional systems)
        var returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean;
        var baseClass = returnsBool ? "TinyEcs.TinyConditionalSystem" : "TinyEcs.TinySystem";

        // Determine class visibility based on the containing type's visibility
        var classVisibility = GetClassVisibility(containingType);

        // Get method parameters for dependency injection
        var parameters = method.Parameters.ToList();
        var setupAssignments = new StringBuilder();
        var executeParameters = new StringBuilder();

        // Generate parameter lists
        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var paramType = param.Type.ToDisplayString();

            setupAssignments.AppendLine($"        builder.Add<{paramType}>();");

            if (i > 0) executeParameters.Append(", ");
            executeParameters.Append($"({paramType})SystemParams[{i}]");
        }

        // Use a single IndentedStringBuilder for all code blocks
        var stubClass = new IndentedStringBuilder();

        if (!string.IsNullOrEmpty(ns))
        {
            stubClass.AppendLine($"namespace {ns}");
            stubClass.AppendLine("{");
            stubClass.IncrementIndent();
        }

        // Generate the stub system class
        stubClass.AppendLine($"{classVisibility} class {stubName} : {baseClass}");
        stubClass.AppendLine("{");
        stubClass.IncrementIndent();

        // Generate private field for delegate (using cached delegate type)
        stubClass.AppendLine($"private readonly {delegateName} _fn;");
        stubClass.AppendLine();

        // Generate constructor
        stubClass.AppendLine($"public {stubName}({delegateName} fn)");
        stubClass.AppendLine("{");
        stubClass.IncrementIndent();
        stubClass.AppendLine("_fn = fn;");
        stubClass.DecrementIndent();
        stubClass.AppendLine("}");
        stubClass.AppendLine();

        // Setup method
        stubClass.AppendLine("protected override void Setup(TinyEcs.SystemParamBuilder builder)");
        stubClass.AppendLine("{");
        stubClass.IncrementIndent();
        foreach (var line in setupAssignments.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            stubClass.AppendLine(line);
        stubClass.DecrementIndent();
        stubClass.AppendLine("}");
        stubClass.AppendLine();

        // Execute method
        stubClass.AppendLine("protected override bool Execute(TinyEcs.World world)");
        stubClass.AppendLine("{");
        stubClass.IncrementIndent();
        stubClass.AppendLine("Lock();");
        stubClass.AppendLine("world.BeginDeferred();");
        if (returnsBool)
        {
            stubClass.AppendLine($"bool result = _fn({executeParameters});");
            stubClass.AppendLine("world.EndDeferred();");
            stubClass.AppendLine("Unlock();");
            stubClass.AppendLine("return result;");
        }
        else
        {
            stubClass.AppendLine($"_fn({executeParameters});");
            stubClass.AppendLine("world.EndDeferred();");
            stubClass.AppendLine("Unlock();");
            stubClass.AppendLine("return true;");
        }
        stubClass.DecrementIndent();
        stubClass.AppendLine("}");

        stubClass.DecrementIndent();
        stubClass.AppendLine("}");

        if (!string.IsNullOrEmpty(ns))
        {
            stubClass.DecrementIndent();
            stubClass.AppendLine("}");
        }

        var sourceText = stubClass.ToString();
        context.AddSource($"{stubName}.g.cs", sourceText);
    }    private static void GenerateAdapterClass(SourceProductionContext context, INamedTypeSymbol containingType, IMethodSymbol method, bool isFromPlugin)
    {
        var adapterName = $"{method.Name}Adapter";
        var ns = containingType.ContainingNamespace.IsGlobalNamespace ? "" : containingType.ContainingNamespace.ToDisplayString();

        // Use fully-qualified type name (global::Namespace.Type) for instance/static references
        var instanceTypeName = containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Validate method requirements for TinySystemAttribute
        ValidateMethodRequirements(context, method);

        // Check if method returns bool (for conditional systems)
        var returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean;
        var baseClass = returnsBool ? "TinyEcs.TinyConditionalSystem" : "TinyEcs.TinySystem";

        // Determine class visibility based on the containing type's visibility
        var classVisibility = GetClassVisibility(containingType);

        // Get method parameters for dependency injection
        var parameters = method.Parameters.ToList();
        var setupAssignments = new StringBuilder();
        var methodCallParameters = new StringBuilder();

        // Generate setup assignments and method call parameters
        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var paramType = param.Type.ToDisplayString();

            setupAssignments.AppendLine($"        builder.Add<{paramType}>();");

            if (i > 0) methodCallParameters.Append(", ");
            methodCallParameters.Append($"({paramType})SystemParams[{i}]");
        }

        // For non-static methods, add a field for the instance
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

        // Make the adapter class sealed and partial with visibility matching the containing type
        adapterClass.AppendLine($"{classVisibility} sealed partial class {adapterName} : {baseClass}");
        adapterClass.AppendLine("{");
        adapterClass.IncrementIndent();

        if (method.IsStatic)
        {
            // Use namespace-qualified type for static call
            methodCall = $"{instanceTypeName}.{method.Name}({methodCallParameters})";
        }
        else
        {
            // Adapter will accept the instance via constructor using namespace-qualified type
            instanceField = $"private readonly {instanceTypeName} _instance;";
            ctor = $"public {adapterName}({instanceTypeName} instance) {{ _instance = instance; }}";
            methodCall = $"_instance.{method.Name}({methodCallParameters})";
            adapterClass.AppendLine(instanceField);
            adapterClass.AppendLine();
            adapterClass.AppendLine(ctor);
        }

        adapterClass.AppendLine();

        // Setup method
        adapterClass.AppendLine("protected override void Setup(TinyEcs.SystemParamBuilder builder)");
        adapterClass.AppendLine("{");
        adapterClass.IncrementIndent();
        foreach (var line in setupAssignments.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            adapterClass.AppendLine(line);
        adapterClass.DecrementIndent();
        adapterClass.AppendLine("}");

        adapterClass.AppendLine();

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
    }

    internal class SystemMethodInfo
    {
        public INamedTypeSymbol ContainingType { get; set; } = null!;
        public List<IMethodSymbol> TinySystemMethods { get; set; } = null!;
        public bool IsFromPlugin { get; set; }
    }
}
