using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable IDE0055
#pragma warning disable IDE0008
#pragma warning disable IDE0058

namespace TinyEcs.SourceGenerator;

[Generator]
public sealed class SourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
        RegisterPluginExtensionsGenerator(context);
        RegisterTinySystemGenerator(context);
    }

    #region Plugin Extensions Generator
    private void RegisterPluginExtensionsGenerator(IncrementalGeneratorInitializationContext context)
    {
        var pluginDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
            static (s, _) => s is TypeDeclarationSyntax { AttributeLists.Count: > 0 },
            static (ctx, _) => GetClassDeclarationSymbolIfAttributeOf(ctx, "TinyEcs.TinyPluginAttribute")
        ).Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(pluginDeclarations.WithComparer(Comparer<TypeDeclarationSyntax>.Instance).Collect());
        context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Generate(source.Left, source.Right, spc));

        var pluginDeclarations2 = context.SyntaxProvider.CreateSyntaxProvider(
            static (s, _) => s is TypeDeclarationSyntax,
            static (ctx, _) => (TypeDeclarationSyntax)ctx.Node
        ).Where(static m => m is not null);

        var compilationAndClasses2 = context.CompilationProvider.Combine(pluginDeclarations2.WithComparer(Comparer<TypeDeclarationSyntax>.Instance).Collect());
        context.RegisterSourceOutput(compilationAndClasses2, (spc, source) =>
        {
            var classes = source.Right;
            var compilation = source.Left;

            if (classes.IsDefaultOrEmpty) return;

            var iPluginSymbol = compilation.GetTypeByMetadataName("TinyEcs.IPlugin");
            if (iPluginSymbol == null) return;

            var hashDelegates = new HashSet<(string MethodName, string Parameters)>();
            var sb = new StringBuilder();
            var allowedMethodNames = new Dictionary<string, string>
            {
                { "OnStartup2", "OnStartup" },
                { "OnFrameStart2", "OnFrameStart" },
                { "OnBeforeUpdate2", "OnBeforeUpdate" },
                { "OnUpdate2", "OnUpdate" },
                { "OnAfterUpdate2", "OnAfterUpdate" },
                { "OnFrameEnd2", "OnFrameEnd" },
            };

            foreach (var cl in classes)
            {
                var semanticModel = compilation.GetSemanticModel(cl.SyntaxTree);

                if (ModelExtensions.GetDeclaredSymbol(semanticModel, cl) is not INamedTypeSymbol symbol)
                    continue;

                var pluginInterfaces = symbol.AllInterfaces
                    .Where(s => SymbolEqualityComparer.Default.Equals(s, iPluginSymbol))
                    .ToArray();

                if (pluginInterfaces.Length == 0)
                    continue;


                var build = symbol.GetMembers("Build")
                    .OfType<IMethodSymbol>()
                    .SingleOrDefault(m => m.Parameters.Length == 1 &&
                                          m.Parameters[0].Type.ToDisplayString() == "TinyEcs.Scheduler");

                if (build == null)
                    continue;

                // Step 1: Get the syntax node for the method
                if (build.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax buildSyntax)
                    continue;

                var semanticModel2 = compilation.GetSemanticModel(buildSyntax.SyntaxTree);

                // Step 2: Traverse the method body to find calls

                foreach (var methodName in allowedMethodNames.Keys)
                {
                    var calls = GetMethodCalls(buildSyntax, semanticModel2, methodName);

                    foreach (var invocation in calls)
                    {
                        ProcessCallInvocations(invocation, semanticModel2, methodName, hashDelegates);
                    }
                }
            }

            foreach ((var methodName, var paramList) in hashDelegates)
            {
                sb.AppendLine($@"
                    public static FuncSystem<World> {methodName}(this Scheduler scheduler, {paramList} fn, ThreadingMode threading = ThreadingMode.Auto) {{
                        return scheduler.{allowedMethodNames[methodName]}(fn, threading);
                    }}");
            }

            var template = $@"
                    // This code is auto-generated.

                    using TinyEcs;

                    public static partial class SchedulerExt {{
                        {sb}
                    }}
                ";

            spc.AddSource($"SchedulerExt.g.cs",
                CSharpSyntaxTree.ParseText(template).GetRoot().NormalizeWhitespace().ToFullString());
        });
    }
    #endregion

    #region TinySystem Generator
    private void RegisterTinySystemGenerator(IncrementalGeneratorInitializationContext context)
    {
        var tinySystemClasses = context.SyntaxProvider.CreateSyntaxProvider(
            static (s, _) => s is TypeDeclarationSyntax t && t.AttributeLists.Count > 0,
            static (ctx, _) =>
            {
                var typeSyntax = (TypeDeclarationSyntax)ctx.Node;
                var typeSymbol = ctx.SemanticModel.GetDeclaredSymbol(typeSyntax);
                if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) return null;
                if (!namedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "TinyEcs.TinySystemAttribute")) return null;
                if (namedTypeSymbol.TypeKind != TypeKind.Class) return null;
                return namedTypeSymbol;
            }
        ).Where(static t => t is not null);

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(tinySystemClasses.Collect()),
            (spc, source) =>
            {
                var classes = source.Right;
                if (classes.IsDefaultOrEmpty) return;

                foreach (var classSymbolObj in classes)
                {
                    if (classSymbolObj is { } classSymbol)
                    {
                        var className = classSymbol.Name.EndsWith("System") ? classSymbol.Name : classSymbol.Name + "System";
                        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : classSymbol.ContainingNamespace.ToDisplayString();
                        // Find the Execute method in the class
                        var executeMethodSymbol = classSymbol.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(m => m.Name == "OnExecute" && !m.IsStatic);
                        if (executeMethodSymbol == null) continue;

                        // Accept all parameters for injection
                        var validParams = executeMethodSymbol.Parameters.ToList();

                        // Declare private fields for each parameter
                        var fieldDecls = string.Join("\n", validParams.Select(p => $"    private {p.Type.ToDisplayString()} _{p.Name};"));

                        // Setup method: assign all fields
                        var setupAssignments = string.Join("\n", validParams.Select(p => $"        _{p.Name} = builder.Add<{p.Type.ToDisplayString()}>();"));
                        var setupCode = $@"    protected override void Setup(SystemParamBuilder builder)
    {{
{setupAssignments}
    }}";

                        var onExecuteParams = string.Join(", ", validParams.Select(p => $"_{p.Name}"));
                        var executeOverride = $@"    public override void Execute(World world)
    {{
		Lock();
        world.BeginDeferred();
        OnExecute({onExecuteParams});
        world.EndDeferred();
		Unlock();
    }}";

                        // Only generate Setup, Execute, and OnExecute
                        var systemClass = classSymbol.ContainingNamespace.IsGlobalNamespace
                            ? $@"using TinyEcs;
public partial class {className} : TinySystem2
{{
{fieldDecls}
{setupCode}
{executeOverride}
}}"
                            : $@"using TinyEcs;
namespace {ns}
{{
    public partial class {className} : TinySystem2
    {{
{fieldDecls}
{setupCode}
{executeOverride}
    }}
}}";

                        var fileName = $"{className}.g.cs";
                        spc.AddSource(fileName, systemClass);
                    }
                }
            }
        );
    }
    #endregion

	private static InvocationExpressionSyntax[] GetMethodCalls(MethodDeclarationSyntax buildSyntax, SemanticModel semanticModel, string name)
	{
		var calls = buildSyntax.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation =>
			{
				// Get the expression being called
				if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					var methodName = memberAccess.Name.Identifier.Text;
					if (methodName != name)
						return false;

					// Ensure the receiver is 'scheduler'
					var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
					if (symbolInfo is IParameterSymbol parameter &&
					    parameter.Name == "scheduler")
					{
						return true;
					}

					// Optional: match by string name instead (less reliable)
					// return memberAccess.Expression.ToString() == "scheduler";
				}

				return false;
			})
			.ToArray();

		return calls;
	}

	private static void ProcessCallInvocations(
		InvocationExpressionSyntax invocation,
		SemanticModel semanticModel,
		string methodName,
		HashSet<(string ,string)> cacheDelegates
	)
	{
		var argument = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
		if (argument is null)
			return;

		var symbolInfo = semanticModel.GetSymbolInfo(argument);
		var found = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

		if (found is not IMethodSymbol method)
			return;

		// if (!cache.Add(method.Name))
		// 	return;

		var j = string.Join(", ", method.Parameters.Select(s => s.Type.ToDisplayString()));
		var act = $"{(method.Parameters.Any() ? $"Action<{j}>" : "Action")}";
		// var p = string.Join(", ", method.Parameters.Select(s => s.ToDisplayString()));
		if (!cacheDelegates.Add((methodName, act)))
			return;

		// var fileName = $"SchedulerExt_{found.Name}";


// 		var template = $@"
// 			using TinyEcs;
//
// 			public static class {fileName} {{
// 				public static void OnUpdate2(this Scheduler scheduler, {act} fn)
// 				{{
// 					var system = scheduler.OnUpdate(fn);
// 				}}
// 			}}
// 		";
//
// 		context.AddSource($"{fileName}.g.cs",
// 			CSharpSyntaxTree.ParseText(template).GetRoot().NormalizeWhitespace().ToFullString());
	}

	private static void ProcessLocalFunction(LocalFunctionStatementSyntax localFunc, INamedTypeSymbol pluginClass, GeneratorExecutionContext context)
	{
		var methodName = $"OnUpdate_{Guid.NewGuid():N}";

		var parameters = localFunc.ParameterList.Parameters;
		var body = localFunc.Body ?? SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(localFunc.ExpressionBody!.Expression));

		var methodSource = new StringBuilder();
		methodSource.AppendLine($"private static void {methodName}({string.Join(", ", parameters.Select(p => p.ToFullString()))})");
		methodSource.AppendLine(body.ToFullString());

// 		var ns = pluginClass.ContainingNamespace.ToDisplayString();
// 		var className = pluginClass.Name;
//
// 		var fullSource = $@"
// namespace {ns}
// {{
//     public partial class {className}
//     {{
//         {methodSource}
//     }}
// }}
// ";
//
// 		context.AddSource($"{className}_{methodName}.g.cs", SourceText.From(fullSource, Encoding.UTF8));
	}


	private static void ProcessLambda(
		LambdaExpressionSyntax lambda,
		InvocationExpressionSyntax originalInvocation,
		INamedTypeSymbol pluginClassSymbol,
		GeneratorExecutionContext context)
	{
		var parameters = lambda switch
		{
			SimpleLambdaExpressionSyntax simple => new[] { simple.Parameter },
			ParenthesizedLambdaExpressionSyntax complex => complex.ParameterList.Parameters.ToArray(),
			_ => Array.Empty<ParameterSyntax>()
		};

		var lambdaBody = lambda.Body;

		var methodName = $"OnUpdate_{Guid.NewGuid().ToString("N")}";

		var methodText = new StringBuilder();
		methodText.AppendLine($"private static void {methodName}({string.Join(", ", parameters.Select(p => p.ToFullString()))})");

		if (lambdaBody is BlockSyntax block)
			methodText.AppendLine(block.ToFullString());
		else if (lambdaBody is ExpressionSyntax expr)
			methodText.AppendLine($"{{ return {expr.ToFullString()}; }}");

// 		// Create the partial class
// 		var ns = pluginClassSymbol.ContainingNamespace.ToDisplayString();
// 		var className = pluginClassSymbol.Name;
//
// 		var fullSource = $@"
// namespace {ns}
// {{
//     public partial class {className}
//     {{
//         {methodText}
//     }}
// }}
// ";
//
// 		context.AddSource($"{className}_{methodName}.g.cs", SourceText.From(fullSource, Encoding.UTF8));

		// Optionally: replace the lambda with the method name in a syntax rewriter
	}



	private static TypeDeclarationSyntax? GetClassDeclarationSymbolIfAttributeOf(GeneratorSyntaxContext context, string name)
	{
		var enumDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

		foreach (var attributeListSyntax in enumDeclarationSyntax.AttributeLists)
		{
			foreach (var attributeSyntax in attributeListSyntax.Attributes)
			{
				if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol) continue;

				var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
				var fullName = attributeContainingTypeSymbol.ToDisplayString();

				if (fullName != name) continue;
				return enumDeclarationSyntax;
			}
		}

		return null;
	}

	private void Generate(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> classes, SourceProductionContext context)
	{
		if (classes.IsDefaultOrEmpty) return;

		foreach (var cl in classes)
		{
			INamedTypeSymbol? nameSymbol = null;
			try
			{
				var semanticModel = compilation.GetSemanticModel(cl.SyntaxTree);
				nameSymbol = ModelExtensions.GetDeclaredSymbol(semanticModel, cl) as INamedTypeSymbol;
			}
			catch
			{
				continue;
			}

			if (nameSymbol == null)
				continue;

			var allMethods = nameSymbol.GetMembers().OfType<IMethodSymbol>().ToList();
			var allSystems = allMethods.Where(s => s.GetAttributes()
				.Any(k => k.AttributeClass.ToDisplayString() is "TinyEcs.TinySystemAttribute"))
				.ToList();

			var sbDeclarations = new StringBuilder();
			var sbSystemsOrder = new StringBuilder();
			var cache = new HashSet<string>();
			foreach (var method in allSystems)
			{
				(var systems, var systemsOrder) = GenerateOne(method, allMethods, cache);
				sbDeclarations.AppendLine(systems);
				sbSystemsOrder.AppendLine(systemsOrder);
			}

			var @namespace = nameSymbol.ContainingNamespace.ToString();
			var className = nameSymbol.Name.Substring(nameSymbol.Name.LastIndexOf('.') + 1);
			var typeName = nameSymbol.TypeKind switch
			{
				TypeKind.Class => "class",
				TypeKind.Struct => "struct",
				_ => throw new InvalidOperationException($"Unsupported type kind: {nameSymbol.TypeKind}")
			};

			var template = $@"
			using TinyEcs;
			{(nameSymbol.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {@namespace} {{")}
				{(nameSymbol.IsStatic ? "static" : "")} partial {typeName} {className} : IPlugin {{
					void IPlugin.Build(Scheduler scheduler)
					{{
						this.Build(scheduler);

						{sbDeclarations}
						{sbSystemsOrder}
					}}
				}}
			{(nameSymbol.ContainingNamespace.IsGlobalNamespace ? "" : "}")}";

			var fileName = nameSymbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat).Replace('<', '{').Replace('>', '}');
			context.AddSource($"{fileName}.g.cs",
				CSharpSyntaxTree.ParseText(template).GetRoot().NormalizeWhitespace().ToFullString());
		}
	}

	private static (string, string) GenerateOne(IMethodSymbol methodSymbol, List<IMethodSymbol> allMethods, HashSet<string> cache)
	{
		var systemDescData = GetAttributeData(methodSymbol, "TinySystem");
		var runifData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("RunIf")).ToArray();
		var beforeOfData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("BeforeOf")).ToArray();
		var afterOfData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("AfterOf")).ToArray();
		var onEnterData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("OnEnter")).ToArray();
		var onExitData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("OnExit")).ToArray();

		var stagee = systemDescData.ConstructorArguments.FirstOrDefault(s => s.Type.ToDisplayString() == "TinyEcs.Stages");
		var threadingg = systemDescData.ConstructorArguments.FirstOrDefault(s => s.Type.ToDisplayString() == "TinyEcs.ThreadingMode");

		var stage = stagee is { Kind: TypedConstantKind.Error } ? ("TinyEcs.Stages", "TinyEcs.Stages.Update")
			: (stagee.Type.ToDisplayString(), stagee.Value);
		var threading = threadingg is { Kind: TypedConstantKind.Error } ? ("TinyEcs.ThreadingMode?", "null")
			: (threadingg.Type.ToDisplayString(), threadingg.Value);

		var classSymbol = methodSymbol.ContainingType;
		var runIfMethods = GetAssociatedMethods(runifData, allMethods, "RunIf", classSymbol);
		var beforeMethodsTargets = GetAssociatedMethods(beforeOfData, allMethods, "BeforeOf", classSymbol);
		var afterMethodsTargets = GetAssociatedMethods(afterOfData, allMethods, "AfterOf", classSymbol);
		var onEnterMethodsTargets = GetAssociatedMethods(afterOfData, allMethods, "OnEnter", classSymbol);
		var onExitMethodsTargets = GetAssociatedMethods(afterOfData, allMethods, "OnExit", classSymbol);

		var sbRunIfFns = new StringBuilder();
		var sbRunIfActions = new StringBuilder();
		foreach (var runIf in runIfMethods)
		{
			sbRunIfActions.AppendLine($".RunIf({runIf.Name}fn)");

			if (cache.Add(runIf.Name))
			{
				sbRunIfFns.AppendLine($"var {runIf.Name}fn = {runIf.Name};");
			}
		}

		var sb = new StringBuilder();

		foreach (var after in afterMethodsTargets)
		{
			sb.AppendLine($"{methodSymbol.Name}System.RunAfter({after.Name}System);");
		}

		foreach (var before in beforeMethodsTargets)
		{
			sb.AppendLine($"{methodSymbol.Name}System.RunBefore({before.Name}System);");
		}

		var method = $@"
			{sbRunIfFns}
			var {methodSymbol.Name}fn = {methodSymbol.Name};
			var {methodSymbol.Name}System = scheduler.AddSystem(
				{methodSymbol.Name}fn,
				stage: ({stage.Item1}){stage.Value},
				threadingType: ({threading.Item1}){threading.Value ?? "null"}
			)
			{sbRunIfActions};";

		return (method, sb.ToString());
	}

	private static List<IMethodSymbol> GetAssociatedMethods(AttributeData[] attributeData, List<IMethodSymbol> allMethods, string attributeType, INamedTypeSymbol classSymbol)
	{
		var associatedMethods = new List<IMethodSymbol>();
		foreach (var attr in attributeData.Where(s => !s.ConstructorArguments.IsEmpty))
		{
			var methodName = attr.ConstructorArguments[0].Value?.ToString();
			if (string.IsNullOrEmpty(methodName)) continue;

			var method = allMethods.FirstOrDefault(m => m.Name == methodName);
			if (method != null)
				associatedMethods.Add(method);
			else
				Debug.WriteLine($"{attributeType} method {methodName} not found in class {classSymbol.Name}.");
		}
		return associatedMethods;
	}

	private static AttributeData GetAttributeData(IMethodSymbol ms, string name)
	{
		foreach (var attribute in ms.GetAttributes())
		{
			var classSymbol = attribute.AttributeClass;
			if(!classSymbol.Name.Contains(name)) continue;

			return attribute;
		}

		return default;
	}


	private class Comparer<T> : IEqualityComparer<T>
	{
		public static readonly Comparer<T> Instance = new();

		public bool Equals(T x, T y)
		{
			return x.Equals(y);
		}

		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}
	}
}
