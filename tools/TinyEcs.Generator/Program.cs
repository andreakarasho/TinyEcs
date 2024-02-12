using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace TinyEcs.Generator;

[Generator]
public sealed class Generator : IIncrementalGenerator
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
					{GenerateQueryTemplateDelegates()}
                    {GenerateQueryInterfaceMethods("IQuery")}
					{GenerateQueryInterfaceMethods("IFilterOrQuery")}
                    {GenerateFilterQuery()}
                }}

                #pragma warning restore 1591
            ";
		}

		static string GenerateQueryTemplateDelegates()
		{
			var sb = new StringBuilder();

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var typeParams = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var whereParams = GenerateSequence(i + 1, " ", j => $"where T{j} : struct");
				var signParams = GenerateSequence(i + 1, ", ", j => $"ref T{j} t{j}");

				sb.AppendLine($"public delegate void QueryFilterDelegate<{typeParams}>({signParams}) {whereParams};");
			}

			return sb.ToString();
		}

		static string GenerateQueryInterfaceMethods(string name)
		{
			var sb = new StringBuilder();
			sb.AppendLine(@$"
				public partial interface {name}
				{{
			");

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var typeParams = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var whereParams = GenerateSequence(i + 1, " ", j => $"where T{j} : struct");

				sb.AppendLine($"void Each<{typeParams}>(QueryFilterDelegate<{typeParams}> fn) {whereParams};");
			}

			sb.AppendLine("}");

			return sb.ToString();
		}

		static string GenerateFilterQuery()
		{
			var sb = new StringBuilder();
			sb.AppendLine(@"
				public partial struct FilterQuery
				{
			");

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var typeParams = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var whereParams = GenerateSequence(i + 1, " ",j => $"where T{j} : struct");
				var fillMissingTerms = GenerateSequence(i + 1, "", j =>
					$@"_fullTerms[^{j + 1}].ID = Lookup.Entity<T{j}>.HashCode;
					   _fullTerms[^{j + 1}].Op = TermOp.With;
					");
				var columnIndices = GenerateSequence(i + 1, "\n" , j => $"var column{j} = arch.GetComponentIndex<T{j}>();");
				var fieldList = GenerateSequence(i + 1, "\n" , j => $"ref var t{j}A = ref chunk.GetReference<T{j}>(column{j});");
				var signCallback = GenerateSequence(i + 1, ", " , j => $"ref t{j}A");
				var advanceField = GenerateSequence(i + 1, "\n" , j => $"t{j}A = ref Unsafe.Add(ref t{j}A, 1);");

				sb.AppendLine($@"
						public void Each<{typeParams}>(QueryFilterDelegate<{typeParams}> fn) {whereParams}
						{{
							if (_fullTerms.Length == 0)
							{{
								_fullTerms = new Term[_terms.Length + {i + 1}];
								{fillMissingTerms}
								_terms.CopyTo(_fullTerms.AsSpan());
								Array.Sort(_fullTerms);
							}}
							var query = new QueryInternal(_archetypes.Span, _fullTerms);
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
