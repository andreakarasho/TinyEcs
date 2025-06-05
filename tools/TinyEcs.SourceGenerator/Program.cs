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
	private static Dictionary<ISymbol, List<IMethodSymbol>> _classToMethods = new();


	class Comparer : IEqualityComparer<MethodDeclarationSyntax>
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
		context.RegisterSourceOutput(compilationAndMethods, static (spc, source) => Generate(source.Item1, source.Item2, spc));
	}

	static MethodDeclarationSyntax? GetMethodSymbolIfAttributeof(GeneratorSyntaxContext context, string name)
	{
		var enumDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

		foreach (var attributeListSyntax in enumDeclarationSyntax.AttributeLists)
		{
			foreach (var attributeSyntax in attributeListSyntax.Attributes)
			{
				if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol) continue;

				var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
				var fullName = attributeContainingTypeSymbol.ToDisplayString();

				// Is the attribute the [EnumExtensions] attribute?
				if (fullName != name) continue;
				return enumDeclarationSyntax;
			}
		}

		return null;
	}

	private static void AddMethodToClass(IMethodSymbol methodSymbol)
	{
		if (!_classToMethods.TryGetValue(methodSymbol.ContainingSymbol, out var list))
		{
			list = new List<IMethodSymbol>();
			_classToMethods[methodSymbol.ContainingSymbol] = list;
		}
		list.Add(methodSymbol);
	}

	static void Generate(Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methods, SourceProductionContext context)
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

			AddMethodToClass(methodSymbol);
		}

		foreach (var classToMethod in _classToMethods)
		{
			var sb = new StringBuilder();
			foreach (var method in classToMethod.Value)
				sb.AppendLine(GenerateOne(method));

			var template = $@"
			partial class {classToMethod.Key} {{
				public override void SetupSystems(TinyEcs.Scheduler scheduler)
				{{
					{sb}
				}}

			}} ";

			if (string.IsNullOrEmpty(template)) continue;

			var fileName = (classToMethod.Key as INamedTypeSymbol).ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat).Replace('<', '{').Replace('>', '}');
			context.AddSource($"{fileName}.g.cs",
				CSharpSyntaxTree.ParseText(template).GetRoot().NormalizeWhitespace().ToFullString());
		}
	}

	private static string GenerateOne(IMethodSymbol methodSymbol)
	{
		var systemDescData = GetAttributeData(methodSymbol, "TinySystem");
		var runifData = methodSymbol.GetAttributes().Where(s => s.AttributeClass.Name.Contains("RunIf")).ToArray();

		var arguments = methodSymbol.Parameters.ToList();

		if (systemDescData.ConstructorArguments.Length == 2)
		{
			Debug.WriteLine($"Method {methodSymbol.Name} does not have the correct number of arguments for TinySystem attribute. Expected 2, got {systemDescData.ConstructorArguments.Length}.");
			return string.Empty;
		}

		var stage = (systemDescData.ConstructorArguments[0].Type.ToDisplayString(), systemDescData.ConstructorArguments[0].Value);
		var threading = (systemDescData.ConstructorArguments[1].Type.ToDisplayString(), systemDescData.ConstructorArguments[1].Value);

		var @namespace = methodSymbol.ContainingNamespace.ToString();
		var className = methodSymbol.ContainingSymbol.ToString();
		className = className.Replace("TinyEcs.", "");

		var method = $@"
				var {methodSymbol.Name}fn = {methodSymbol.Name};
				scheduler.AddSystem(
					{methodSymbol.Name}fn,
					stage: ({stage.Item1}){stage.Value},
					threadingType: ({threading.Item1}){threading.Value}
				); ";

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


internal sealed class CodeFormatter : CSharpSyntaxRewriter
{
	public static string Format(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
		SyntaxNode normalized = syntaxTree.GetRoot().NormalizeWhitespace();

		normalized = new CodeFormatter().Visit(normalized);

		return normalized.ToFullString();
	}

	private static T FormatMembers<T>(T node, IEnumerable<SyntaxNode> members) where T : SyntaxNode
	{
		SyntaxNode[] membersArray = members as SyntaxNode[] ?? members.ToArray();

		int memberCount = membersArray.Length;
		int current = 0;

		return node.ReplaceNodes(membersArray, RewriteTrivia);

		SyntaxNode RewriteTrivia<TNode>(TNode oldMember, TNode _) where TNode : SyntaxNode
		{
			string trailingTrivia = oldMember.GetTrailingTrivia().ToFullString().TrimEnd() + "\n\n";
			return current++ != memberCount - 1
				? oldMember.WithTrailingTrivia(SyntaxFactory.Whitespace(trailingTrivia))
				: oldMember;
		}
	}

	public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
	{
		return base.VisitNamespaceDeclaration(FormatMembers(node, node.Members))!;
	}

	public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
	{
		return base.VisitClassDeclaration(FormatMembers(node, node.Members))!;
	}

	public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
	{
		return base.VisitStructDeclaration(FormatMembers(node, node.Members))!;
	}
}
