using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable IDE0055
#pragma warning disable IDE0008
#pragma warning disable IDE0058

namespace TinyEcs.Generator;

[Generator]
public sealed class MyGenerator : IIncrementalGenerator
{
	private const int MAX_GENERICS = 16;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput((IncrementalGeneratorPostInitializationContext postContext) =>
		{
			postContext.AddSource("TinyEcs.Systems.g.cs", CodeFormatter.Format(GenerateSystems()));
			postContext.AddSource("TinyEcs.Archetypes.g.cs", CodeFormatter.Format(GenerateArchetypes()));
		});

		static string GenerateArchetypes()
		{
			return $@"
                #pragma warning disable 1591
                #nullable enable

                namespace TinyEcs
                {{
					{GenerateArchetypeSigns()}
                }}

                #pragma warning restore 1591
            ";
		}

		static string GenerateSystems()
		{
			return $@"
                #pragma warning disable 1591
                #nullable enable

                namespace TinyEcs
                {{
					#if NET
					{GenerateSchedulerSystems()}
					{GenerateSystemsInterfaces()}
					{CreateDataAndFilterStructs()}
					#endif
                }}

                #pragma warning restore 1591
            ";
		}

		static string GenerateArchetypeSigns()
		{
			var sb = new StringBuilder();

			sb.AppendLine("public partial class World {");

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var generics = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var whereGenerics = GenerateSequence(i + 1, " ", j => $"where T{j} : struct");
				var objsArgs = GenerateSequence(i + 1, ", ", j => $"Component<T{j}>()");

				sb.AppendLine($@"
					/// <inheritdoc cref=""World.Archetype(Span{{EcsID}})""/>
					public Archetype Archetype<{generics}>()
						{whereGenerics}
						=> Archetype({objsArgs});
				");
			}

			sb.AppendLine("}");

			return sb.ToString();
		}

		static string CreateDataAndFilterStructs()
		{
			var sb = new StringBuilder();

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var genericsArgs = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var genericsArgsWhere = GenerateSequence(i + 1, "\n", j => $"where T{j} : struct, IComponent");
				var queryBuilderCalls = GenerateSequence(i + 1, "\n", j => $"if (!FilterBuilder<T{j}>.Build(builder)) builder.Data<T{j}>();");
				var fieldSign = GenerateSequence(i + 1, ", ", j => $"out Span<T{j}> field{j}");
				var fieldAssignments = GenerateSequence(i + 1, "\n", j => $"field{j} = chunk.GetSpan<T{j}>(arch.GetComponentIndex<T{j}>());");

				sb.AppendLine($@"
					public struct Data<{genericsArgs}> : IData<Data<{genericsArgs}>>, IQueryIterator<Data<{genericsArgs}>>
						{genericsArgsWhere}
					{{
						private ComponentsSpanIterator _iterator;

						internal Data(ComponentsSpanIterator iterator) => _iterator = iterator;

						public static void Build(QueryBuilder builder)
						{{
							{queryBuilderCalls}
						}}

						public static IQueryIterator<Data<{genericsArgs}>> CreateIterator(ComponentsSpanIterator iterator)
							=> new Data<{genericsArgs}>(iterator);

						[MethodImpl(MethodImplOptions.AggressiveInlining)]
						public void Deconstruct({fieldSign})
						{{
							var arch = _iterator.Archetype;
							ref readonly var chunk = ref _iterator.Current;
							{fieldAssignments}
						}}

						[MethodImpl(MethodImplOptions.AggressiveInlining)]
						public unsafe void Deconstruct(out ReadOnlySpan<EntityView> entities, {fieldSign})
						{{
							var arch = _iterator.Archetype;
							ref readonly var chunk = ref _iterator.Current;
							entities = chunk.Entities.AsSpan(0, chunk.Count);
							{fieldAssignments}
						}}

						[MethodImpl(MethodImplOptions.AggressiveInlining)]
						public bool MoveNext() => _iterator.MoveNext();

						readonly Data<{genericsArgs}> IQueryIterator<Data<{genericsArgs}>>.Current => this;

						readonly IQueryIterator<Data<{genericsArgs}>> IQueryIterator<Data<{genericsArgs}>>.GetEnumerator() => this;
					}}
				");
			}

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var genericsArgs = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var genericsArgsWhere = GenerateSequence(i + 1, "\n", j => $"where T{j} : struct, IFilter");
				var appendTermsCalls = GenerateSequence(i + 1, "\n", j => $"if (!FilterBuilder<T{j}>.Build(builder)) T{j}.Build(builder);");

				sb.AppendLine($@"
					public readonly struct Filter<{genericsArgs}> : IFilter
						{genericsArgsWhere}
					{{
						public static void Build(QueryBuilder builder)
						{{
							{appendTermsCalls}
						}}
					}}
				");
			}

			return sb.ToString();
		}

		static string GenerateSchedulerSystems()
		{
			var sb = new StringBuilder();

			sb.AppendLine("public partial class Scheduler {");

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var genericsArgs = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var genericsArgsWhere = GenerateSequence(i + 1, "\n", j => $"where T{j} : class, ISystemParam<World>, IIntoSystemParam<World>");
				var objs = GenerateSequence(i + 1, "\n", j => $"T{j}? obj{j} = null;");
				var objsGen = GenerateSequence(i + 1, "\n", j => $"obj{j} ??= (T{j})T{j}.Generate(args);");
				var objsLock = GenerateSequence(i + 1, "\n", j => $"obj{j}.Lock();");
				var objsUnlock = GenerateSequence(i + 1, "\n", j => $"obj{j}.Unlock();");
				var systemCall = GenerateSequence(i + 1, ", ", j => $"obj{j}");
				var objsCheckInuse = GenerateSequence(i + 1, " ", j => $"obj{j}?.UseIndex != 0" + (j < i ? "||" : ""));

				sb.AppendLine($@"
				public FuncSystem<World> AddSystem<{genericsArgs}>(Action<{genericsArgs}> system, Stages stage = Stages.Update, ThreadingMode threadingType = ThreadingMode.Auto)
					{genericsArgsWhere}
				{{
					{objs}
					var checkInuse = () => {objsCheckInuse};
					var fn = (World args, Func<World, bool> runIf) =>
					{{
						if (runIf != null && !runIf.Invoke(args))
							return;

						{objsGen}
						{objsLock}
						system({systemCall});
						{objsUnlock}
					}};
					var sys = new FuncSystem<World>(_world, fn, checkInuse, threadingType);
					Add(sys, stage);
					return sys;
				}}
				");
			}

			sb.AppendLine("}");

			return sb.ToString();
		}

		static string GenerateSystemsInterfaces()
		{
			var sb = new StringBuilder();
			sb.AppendLine("public sealed partial class FuncSystem<TArg> {");

			for (var i = 0; i < 16; ++i)
			{
				var genericsArgs = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var genericsArgsWhere = GenerateSequence(i + 1, "\n", j => $"where T{j} : class, ISystemParam<TArg>, IIntoSystemParam<TArg>");
				var objs = GenerateSequence(i + 1, "\n", j => $"T{j}? obj{j} = null;");
				var objsGen = GenerateSequence(i + 1, "\n", j => $"obj{j} ??= (T{j})T{j}.Generate(args);");
				var objsLock = GenerateSequence(i + 1, "\n", j => $"obj{j}.Lock();");
				var objsUnlock = GenerateSequence(i + 1, "\n", j => $"obj{j}.Unlock();");
				var systemCall = GenerateSequence(i + 1, ", ", j => $"obj{j}");
				var objsCheckInuse = GenerateSequence(i + 1, " ", j => $"obj{j}?.UseIndex != 0" + (j < i ? "||" : ""));

				sb.AppendLine($@"
					public FuncSystem<TArg> RunIf<{genericsArgs}>(Func<{genericsArgs}, bool> condition)
						{genericsArgsWhere}
					{{
						{objs}
						var fn = (TArg args) => {{
							{objsGen}
							return condition({systemCall});
						}};
						_conditions.Add(fn);
						return this;
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
