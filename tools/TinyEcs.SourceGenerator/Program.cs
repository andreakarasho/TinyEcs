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


	private class Comparer : IEqualityComparer<MethodDeclarationSyntax>
	{
		public static readonly Comparer Instance = new();

		public bool Equals(MethodDeclarationSyntax x, MethodDeclarationSyntax y)
		{
			return x.Equals(y);
		}

		public int GetHashCode(MethodDeclarationSyntax obj)
		{
			return obj.GetHashCode();
		}
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValuesProvider<MethodDeclarationSyntax> methodDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
			static (s, _) => s is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
			static (ctx, _) => GetMethodSymbolIfAttributeof(ctx, "TinyEcs.TinySystemAttribute")
		).Where(static m => m is not null)!;

		IncrementalValueProvider<(Compilation, ImmutableArray<MethodDeclarationSyntax>)> compilationAndMethods = context.CompilationProvider.Combine(methodDeclarations.WithComparer(Comparer.Instance).Collect());
		context.RegisterSourceOutput(compilationAndMethods, (spc, source) => Generate(source.Item1, source.Item2, spc));
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

			var isValid = false;

			var mem = methodSymbol.ContainingSymbol;
			while (mem is INamedTypeSymbol symb)
			{
				var members = symb.GetMembers("SetupSystems");
				if (members.OfType<IMethodSymbol>().Any(member => member.IsOverride))
				{
					isValid = false;
					break;
				}

				mem = symb.BaseType;

				if (mem?.Name == "TinyPlugin")
				{
					isValid = true;
					break;
				}
			}

			if (isValid)
				AddMethodToClass(methodSymbol);
		}

		foreach (var classToMethod in _classToMethods)
		{
			var sb = new StringBuilder();
			foreach (var method in classToMethod.Value)
				sb.AppendLine(GenerateOne(method));

			var @namespace = classToMethod.Key.ContainingNamespace.ToString();
			var className = classToMethod.Key.Name.Substring(classToMethod.Key.Name.LastIndexOf('.') + 1);

			var template = $@"
			{(classToMethod.Key.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {@namespace} {{")}
				{(classToMethod.Key.IsStatic ? "static" : "")} partial class {className} {{
					public override void SetupSystems(TinyEcs.Scheduler scheduler)
					{{
						{sb}
					}}
				}}
			{(classToMethod.Key.ContainingNamespace.IsGlobalNamespace ? "" : "}")}";

			if (string.IsNullOrEmpty(template)) continue;

			var fileName = (classToMethod.Key as INamedTypeSymbol).ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat).Replace('<', '{').Replace('>', '}');
			context.AddSource($"{fileName}.g.cs",
				CSharpSyntaxTree.ParseText(template).GetRoot().NormalizeWhitespace().ToFullString());
		}
	}

	private string GenerateOne(IMethodSymbol methodSymbol)
	{
		var systemDescData = GetAttributeData(methodSymbol, "TinySystem");
		var runifData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("RunIf")).ToArray();

		var arguments = methodSymbol.Parameters.ToList();

		if (systemDescData.ConstructorArguments.Length < 2)
		{
			Debug.WriteLine($"Method {methodSymbol.Name} does not have the correct number of arguments for TinySystem attribute. Expected 2, got {systemDescData.ConstructorArguments.Length}.");
			return string.Empty;
		}

		var stage = (systemDescData.ConstructorArguments[0].Type.ToDisplayString(), systemDescData.ConstructorArguments[0].Value);
		var threading = (systemDescData.ConstructorArguments[1].Type.ToDisplayString(), systemDescData.ConstructorArguments[1].Value);

		var classSymbol = methodSymbol.ContainingType;
		var allMethods = classSymbol.GetMembers().OfType<IMethodSymbol>().ToDictionary(k => k.Name, v => v);

		var sbRunIfFn = new StringBuilder();
		var sb = new StringBuilder();
		foreach (var runif in runifData)
		{
			var runifMethodName = runif.ConstructorArguments[0].Value.ToString();
			if (string.IsNullOrEmpty(runifMethodName)) continue;

			if (!allMethods.TryGetValue(runifMethodName, out var runIfMethod))
			{
				Debug.WriteLine($"RunIf method {runifMethodName} not found in class {classSymbol.Name}.");
				continue;
			}

			sbRunIfFn.AppendLine($"var {runIfMethod.Name}fn = {runIfMethod.Name};");
			sb.AppendLine($".RunIf({runIfMethod.Name}fn)");
		}

		var method = $@"
			{sbRunIfFn}
			var {methodSymbol.Name}fn = {methodSymbol.Name};
			scheduler.AddSystem(
				{methodSymbol.Name}fn,
				stage: ({stage.Item1}){stage.Value},
				threadingType: ({threading.Item1}){threading.Value}
			){sb}; ";

		return method;
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
}
