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
	private readonly Dictionary<ISymbol, List<IMethodSymbol>> _classToMethods = new ();

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var pluginDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
			static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
			static (ctx, _) => GetClassDeclarationSymbolIfAttributeof(ctx, "TinyEcs.TinyPluginAttribute")
		).Where(static m => m is not null)!;

		var compilationAndClasses = context.CompilationProvider.Combine(pluginDeclarations.WithComparer(Comparer<ClassDeclarationSyntax>.Instance).Collect());
		context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Generate(source.Item1, source.Item2, spc));


		// var methodDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
		// 	static (s, _) => s is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
		// 	static (ctx, _) => GetMethodSymbolIfAttributeof(ctx, "TinyEcs.TinySystemAttribute")
		// ).Where(static m => m is not null)!;
		//
		// var compilationAndMethods = context.CompilationProvider.Combine(methodDeclarations.WithComparer(Comparer<MethodDeclarationSyntax>.Instance).Collect());
		// context.RegisterSourceOutput(compilationAndMethods, (spc, source) => Generate(source.Item1, source.Item2, spc));
	}

	private static ClassDeclarationSyntax? GetClassDeclarationSymbolIfAttributeof(GeneratorSyntaxContext context, string name)
	{
		var enumDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

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

	private void Generate(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
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
				.Any(s => s.AttributeClass.ToDisplayString() == "TinyEcs.TinySystemAttribute"))
				.ToList();

			var sb = new StringBuilder();
			var sbEnd = new StringBuilder();
			foreach (var method in allSystems)
			{
				(var systems, var systemsOrder) = GenerateOne(method);
				sb.AppendLine(systems);
				sbEnd.AppendLine(systemsOrder);
			}

			var @namespace = nameSymbol.ContainingNamespace.ToString();
			var className = nameSymbol.Name.Substring(nameSymbol.Name.LastIndexOf('.') + 1);

			var template = $@"
			using TinyEcs;
			{(nameSymbol.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {@namespace} {{")}
				{(nameSymbol.IsStatic ? "static" : "")} partial class {className} : IPlugin {{
					void IPlugin.Build(Scheduler scheduler)
					{{
						this.Build(scheduler);

						{sb}
						{sbEnd}
					}}
				}}
			{(nameSymbol.ContainingNamespace.IsGlobalNamespace ? "" : "}")}";

			var fileName = nameSymbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat).Replace('<', '{').Replace('>', '}');
			context.AddSource($"{fileName}.g.cs",
				CSharpSyntaxTree.ParseText(template).GetRoot().NormalizeWhitespace().ToFullString());
		}
	}

	private static MethodDeclarationSyntax? GetMethodSymbolIfAttributeof(GeneratorSyntaxContext context, string name)
	{
		var enumDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

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

	private void AddMethodToClass(IMethodSymbol methodSymbol)
	{
		if (!_classToMethods.TryGetValue(methodSymbol.ContainingSymbol, out var list))
		{
			list = new List<IMethodSymbol>();
			_classToMethods[methodSymbol.ContainingSymbol] = list;
		}
		list.Add(methodSymbol);
	}

	private void Generate(Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methods, SourceProductionContext context)
	{
		if (methods.IsDefaultOrEmpty) return;

		foreach (var methodSyntax in methods)
		{
			IMethodSymbol? methodSymbol = null;
			try
			{
				var semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);
				methodSymbol = ModelExtensions.GetDeclaredSymbol(semanticModel, methodSyntax) as IMethodSymbol;
			}
			catch
			{
				continue;
			}

			if (methodSymbol == null)
				continue;

			var isValid = false;
			if (methodSymbol.ContainingSymbol is INamedTypeSymbol symb)
			{
				var members = symb.GetMembers("Build");
				if (members.OfType<IMethodSymbol>().Any(s => !s.IsStatic))
				{
					isValid = true;
				}

				if (symb.AllInterfaces.Any(s => s.Name == "IPlugin"))
				{
					isValid = true;
				}
			}

			if (isValid)
				AddMethodToClass(methodSymbol);
		}

		foreach (var classToMethod in _classToMethods)
		{
			var sb = new StringBuilder();
			var sbEnd = new StringBuilder();
			foreach (var method in classToMethod.Value)
			{
				(var systems, var systemsOrder) = GenerateOne(method);
				sb.AppendLine(systems);
				sbEnd.AppendLine(systemsOrder);
			}

			var @namespace = classToMethod.Key.ContainingNamespace.ToString();
			var className = classToMethod.Key.Name.Substring(classToMethod.Key.Name.LastIndexOf('.') + 1);

			var template = $@"
			using TinyEcs;
			{(classToMethod.Key.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {@namespace} {{")}
				{(classToMethod.Key.IsStatic ? "static" : "")} partial class {className} : ISourceGenPlugin {{
					void ISourceGenPlugin.InternalBuild(Scheduler scheduler)
					{{
						{sb}
						{sbEnd}
					}}
				}}
			{(classToMethod.Key.ContainingNamespace.IsGlobalNamespace ? "" : "}")}";

			if (string.IsNullOrEmpty(template)) continue;

			var fileName = (classToMethod.Key as INamedTypeSymbol).ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat).Replace('<', '{').Replace('>', '}');
			context.AddSource($"{fileName}.g.cs",
				CSharpSyntaxTree.ParseText(template).GetRoot().NormalizeWhitespace().ToFullString());
		}
	}

	private (string, string) GenerateOne(IMethodSymbol methodSymbol)
	{
		var systemDescData = GetAttributeData(methodSymbol, "TinySystem");
		var runifData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("RunIf")).ToArray();
		var beforeOfData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("BeforeOf")).ToArray();
		var afterOfData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("AfterOf")).ToArray();

		var arguments = methodSymbol.Parameters.ToList();

		if (systemDescData.ConstructorArguments.Length < 2)
		{
			Debug.WriteLine($"Method {methodSymbol.Name} does not have the correct number of arguments for TinySystem attribute. Expected 2, got {systemDescData.ConstructorArguments.Length}.");
			return (string.Empty, string.Empty);
		}

		var stage = (systemDescData.ConstructorArguments[0].Type.ToDisplayString(), systemDescData.ConstructorArguments[0].Value);
		var threading = (systemDescData.ConstructorArguments[1].Type.ToDisplayString(), systemDescData.ConstructorArguments[1].Value);

		var classSymbol = methodSymbol.ContainingType;
		var allMethods = classSymbol.GetMembers().OfType<IMethodSymbol>().ToList();


		var runIfMethods = GetAssociatedMethods(runifData, allMethods, "RunIf", classSymbol);
		var beforeMethodsTargets = GetAssociatedMethods(beforeOfData, allMethods, "BeforeOf", classSymbol);
		var afterMethodsTargets = GetAssociatedMethods(afterOfData, allMethods, "AfterOf", classSymbol);

		var sbRunIfFns = new StringBuilder();
		var sbRunIfActions = new StringBuilder();
		foreach (var runIf in runIfMethods)
		{
			sbRunIfFns.AppendLine($"var {runIf.Name}fn = {runIf.Name};");
			sbRunIfActions.AppendLine($".RunIf({runIf.Name}fn)");
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
				threadingType: ({threading.Item1}){threading.Value}
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
