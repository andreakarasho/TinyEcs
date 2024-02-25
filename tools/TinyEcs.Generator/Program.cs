using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace TinyEcs.Generator;

[Generator]
public sealed class MyGenerator : IIncrementalGenerator
{
	private const int MAX_GENERICS = 25;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput((IncrementalGeneratorPostInitializationContext postContext) =>
		{
			postContext.AddSource("TinyEcs.g.cs", CodeFormatter.Format(Generate()));
		});

		static string Generate()
		{
			return $@"
                #pragma warning disable 1591
                #nullable enable

                namespace TinyEcs
                {{
					{GenerateQueryTemplateDelegates(false)}
					{GenerateQueryTemplateDelegates(true)}
					{GenerateFilterQuery(false, false)}
					{GenerateFilterQuery(true, false)}
					{GenerateFilterQuery(false, true)}
					{GenerateFilterQuery(true, true)}
                }}

                #pragma warning restore 1591
            ";
		}

		static string GenerateQueryTemplateDelegates(bool withEntityView)
		{
			var sb = new StringBuilder();
			var delegateName = withEntityView ? "QueryFilterDelegateWithEntity" : "QueryFilterDelegate";

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var typeParams = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var whereParams = GenerateSequence(i + 1, " ", j => $"where T{j} : struct");
				var signParams = (withEntityView ? "EntityView entity, " : "") +
				                 GenerateSequence(i + 1, ", ", j => $"ref T{j} t{j}");

				sb.AppendLine($"public delegate void {delegateName}<{typeParams}>({signParams}) {whereParams};");
			}

			return sb.ToString();
		}

		// static string GenerateQueryInterfaceMethods(string name)
		// {
		// 	var sb = new StringBuilder();
		// 	sb.AppendLine(@$"
		// 		public partial interface {name}
		// 		{{
		// 	");
		//
		// 	for (var i = 0; i < MAX_GENERICS; ++i)
		// 	{
		// 		var typeParams = GenerateSequence(i + 1, ", ", j => $"T{j}");
		// 		var whereParams = GenerateSequence(i + 1, " ", j => $"where T{j} : struct");
		//
		// 		sb.AppendLine($"void Each<{typeParams}>(QueryFilterDelegate<{typeParams}> fn) {whereParams};");
		// 	}
		//
		// 	sb.AppendLine("}");
		//
		// 	return sb.ToString();
		// }

		static string GenerateFilterQuery(bool withFilter, bool withEntityView)
		{
			var className = withFilter ? "struct FilterQuery" : "class World";
			var filter = withFilter ? "<TFilter>" : "";
			var filterMethod = withFilter ? ", TFilter" : "";
			var delegateName = withEntityView ? "QueryFilterDelegateWithEntity" : "QueryFilterDelegate";

			var sb = new StringBuilder();
			sb.AppendLine($@"
				public partial {className}{filter}
				{{
			");

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var typeParams = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var whereParams = GenerateSequence(i + 1, " ",j => $"where T{j} : struct");
				var columnIndices = GenerateSequence(i + 1, "\n" , j => $"var column{j} = arch.GetComponentIndex<T{j}>();");
				var fieldList = (withEntityView ? "ref var entityA = ref chunk.Entities[0];\n" : "") +
				                GenerateSequence(i + 1, "\n" , j => $"ref var t{j}A = ref chunk.GetReference<T{j}>(column{j});");
				var signCallback = (withEntityView ? "entityA, " : "") +
				                   GenerateSequence(i + 1, ", " , j => $"ref t{j}A");
				var advanceField = (withEntityView ? "entityA = ref Unsafe.Add(ref entityA, 1);\n" : "") +
				                   GenerateSequence(i + 1, "\n" , j => $"t{j}A = ref Unsafe.Add(ref t{j}A, 1);");

				sb.AppendLine($@"
						public void Query<{typeParams}>({delegateName}<{typeParams}> fn) {whereParams}
						{{
							var terms = Lookup.Query<{(i > 0 ? "(" : "")}{typeParams}{(i > 0 ? ")" : "")}{filterMethod}>.Terms;
							var query = new QueryInternal({(withFilter ? "_archetypes" : "Archetypes")}, terms);
							foreach (var arch in query)
							{{
								// columns
								{columnIndices}

								foreach (ref readonly var chunk in arch)
								{{
									// field list
									{fieldList}

									ref var last = ref Unsafe.Add(ref t0A, chunk.Count);
									while (Unsafe.IsAddressLessThan(ref t0A, ref last))
									{{
										// sign list
										fn({signCallback});

										// unsafe add list
										{advanceField}
									}}
								}}
							}}
						}}
				");
			}

			sb.AppendLine("}");

			return sb.ToString();
		}

		static string GenerateSequence(int count, string separator, Func<int, string> generator)
		{
			var sb = new StringBuilder();
			for (var i = 0; i < count; ++i)
			{
				sb.Append(generator(i));

				if (i < count - 1)
				{
					sb.Append(separator);
				}
			}

			return sb.ToString();
		}
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
		return base.VisitNamespaceDeclaration(FormatMembers(node, node.Members));
	}

	public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
	{
		return base.VisitClassDeclaration(FormatMembers(node, node.Members));
	}

	public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
	{
		return base.VisitStructDeclaration(FormatMembers(node, node.Members));
	}
}
