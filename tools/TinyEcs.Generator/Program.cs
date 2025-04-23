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
			postContext.AddSource("TinyEcs.QueryIteratorEach.g.cs", CodeFormatter.Format(GenerateQueryIteratorEach()));
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
					{GenerateSchedulerSystemsState()}
					{GenerateSchedulerStageSpecificSystems()}
					{GenerateSystemsInterfaces()}
					{CreateDataAndFilterStructs()}
					#endif
                }}

                #pragma warning restore 1591
            ";
		}

		static string GenerateQueryIteratorEach()
		{
			return $@"
			#pragma warning disable 1591
                #nullable enable

                namespace TinyEcs
                {{
					#if NET
					{GenerateIterators()}
					#endif
                }}

                #pragma warning restore 1591
			";
		}

		static string GenerateIterators()
		{
			var sb = new StringBuilder();

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var generics = GenerateSequence(i + 1, ", ", j => $"T{j}");
				var whereGenerics = GenerateSequence(i + 1, " ", j => $"where T{j} : struct");
				var ptrList = GenerateSequence(i + 1, "\n", j => $"private DataRow<T{j}> _current{j};");
				var ptrSet = GenerateSequence(i + 1, "\n", j => $"_current{j} = _iterator.GetColumn<T{j}>({j});");
				var ptrAdvance = GenerateSequence(i + 1, "\n", j => $"_current{j}.Next();");
				var fieldSign = GenerateSequence(i + 1, ", ", j => $"out Ptr<T{j}> ptr{j}");
				var fieldAssignments = GenerateSequence(i + 1, "\n", j => $"ptr{j} = _current{j}.Value;");
				var queryBuilderCalls = GenerateSequence(i + 1, "\n", j => $"if (!FilterBuilder<T{j}>.Build(builder)) builder.With<T{j}>();");

				sb.AppendLine($@"
					[SkipLocalsInit]
					public unsafe ref struct Data<{generics}> : IData<Data<{generics}>>
						{whereGenerics}
					{{
						private QueryIterator _iterator;
						private int _index, _count;
						private ReadOnlySpan<EntityView> _entities;
						{ptrList}

						[MethodImpl(MethodImplOptions.AggressiveInlining)]
						internal Data(QueryIterator queryIterator)
						{{
							_iterator = queryIterator;
							_index = -1;
							_count = -1;
						}}

						public static void Build(QueryBuilder builder)
						{{
							{queryBuilderCalls}
						}}

						[MethodImpl(MethodImplOptions.AggressiveInlining)]
						public static Data<{generics}> CreateIterator(QueryIterator iterator)
							=> new Data<{generics}>(iterator);

						[System.Diagnostics.CodeAnalysis.UnscopedRef]
						public ref Data<{generics}> Current
						{{
							[MethodImpl(MethodImplOptions.AggressiveInlining)]
							get => ref this;
						}}

						[MethodImpl(MethodImplOptions.AggressiveInlining)]
						public readonly void Deconstruct({fieldSign})
						{{
							{fieldAssignments}
						}}

						[MethodImpl(MethodImplOptions.AggressiveInlining)]
						public readonly void Deconstruct(out PtrRO<EntityView> entity, {fieldSign})
						{{
							entity = new (in _entities[_index]);
							{fieldAssignments}
						}}

						[MethodImpl(MethodImplOptions.AggressiveInlining)]
						public bool MoveNext()
						{{
							if (++_index >= _count)
							{{
								if (!_iterator.Next())
									return false;

								{ptrSet}
								_entities = _iterator.Entities();

								_index = 0;
								_count = _iterator.Count;
							}}
							else
							{{
								{ptrAdvance}
							}}

							return true;
						}}

						[MethodImpl(MethodImplOptions.AggressiveInlining)]
						public readonly Data<{generics}> GetEnumerator() => this;
					}}
				");
			}

			return sb.ToString();
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

			// for (var i = 0; i < MAX_GENERICS; ++i)
			// {
			// 	var genericsArgs = GenerateSequence(i + 1, ", ", j => $"T{j}");
			// 	var genericsArgsWhere = GenerateSequence(i + 1, "\n", j => $"where T{j} : struct");
			// 	var queryBuilderCalls = GenerateSequence(i + 1, "\n", j => $"if (!FilterBuilder<T{j}>.Build(builder)) builder.Data<T{j}>();");
			// 	var fieldSign = GenerateSequence(i + 1, ", ", j => $"out Span<T{j}> field{j}");
			// 	var fieldAssignments = GenerateSequence(i + 1, "\n", j => $"field{j} = _iterator.Data<T{j}>({j});");

			// 	sb.AppendLine($@"
			// 		public struct Data<{genericsArgs}> : IData<Data<{genericsArgs}>>, IQueryIterator<Data<{genericsArgs}>>
			// 			{genericsArgsWhere}
			// 		{{
			// 			private QueryIteratorEach<{genericsArgs}> _iterator;

			// 			internal Data(QueryIterator iterator) => _iterator = new (iterator);

			// 			public static void Build(QueryBuilder builder)
			// 			{{
			// 				{queryBuilderCalls}
			// 			}}

			// 			public static IQueryIterator<Data<{genericsArgs}>> CreateIterator(QueryIterator iterator)
			// 				=> new Data<{genericsArgs}>(iterator);

			//
			// 			public readonly void Deconstruct({fieldSign})
			// 			{{
			// 				{fieldAssignments}
			// 			}}

			//
			// 			public readonly void Deconstruct(out ReadOnlySpan<EntityView> entities, {fieldSign})
			// 			{{
			// 				entities = _iterator.Entities();
			// 				{fieldAssignments}
			// 			}}

			//
			// 			public bool MoveNext() => _iterator.Next();

			// 			readonly Data<{genericsArgs}> IQueryIterator<Data<{genericsArgs}>>.Current => this;

			// 			readonly IQueryIterator<Data<{genericsArgs}>> IQueryIterator<Data<{genericsArgs}>>.GetEnumerator() => this;
			// 		}}
			// 	");
			// }

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
							return false;

						{objsGen}
						{objsLock}
						args.BeginDeferred();
						system({systemCall});
						args.EndDeferred();
						{objsUnlock}
						return true;
					}};
					var sys = new FuncSystem<World>(_world, fn, checkInuse, stage, threadingType);
					Add(sys, stage);
					return sys;
				}}
				");
			}

			sb.AppendLine("}");

			return sb.ToString();
		}

		static string GenerateSchedulerSystemsState()
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

				// OnEnter method
				sb.AppendLine($@"
				public FuncSystem<World> OnEnter<TState, {genericsArgs}>(TState st, Action<{genericsArgs}> system, ThreadingMode threadingType = ThreadingMode.Auto)
					where TState : notnull, Enum
					{genericsArgsWhere}
				{{
					{objs}
					var checkInuse = () => {objsCheckInuse};
					var fn = (World args, Func<World, bool> runIf) =>
					{{
						if (runIf != null && !runIf.Invoke(args))
							return false;

						{objsGen}
						{objsLock}
						args.BeginDeferred();
						system({systemCall});
						args.EndDeferred();
						{objsUnlock}
						return true;
					}};
					var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.OnEnter, threadingType)
						.RunIf((State<TState> state) => state.EnterState(st));
					Add(sys, Stages.OnEnter);
					return sys;
				}}");

				// OnExit method
				sb.AppendLine($@"
				public FuncSystem<World> OnExit<TState, {genericsArgs}>(TState st, Action<{genericsArgs}> system, ThreadingMode threadingType = ThreadingMode.Auto)
					where TState : notnull, Enum
					{genericsArgsWhere}
				{{
					{objs}
					var checkInuse = () => {objsCheckInuse};
					var fn = (World args, Func<World, bool> runIf) =>
					{{
						if (runIf != null && !runIf.Invoke(args))
							return false;

						{objsGen}
						{objsLock}
						args.BeginDeferred();
						system({systemCall});
						args.EndDeferred();
						{objsUnlock}
						return true;
					}};
					var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.OnExit, threadingType)
						.RunIf((State<TState> state) => state.ExitState(st));
					Add(sys, Stages.OnExit);
					return sys;
				}}");
			}

			sb.AppendLine("}");

			return sb.ToString();
		}

		static string GenerateSchedulerStageSpecificSystems()
		{
			var sb = new StringBuilder();

			sb.AppendLine("public partial class Scheduler {");

			// Define the stage names to generate (excluding OnEnter and OnExit)
			var stageNames = new[]
			{
				("Startup", "OnStartup"),
				("FrameStart", "OnFrameStart"),
				("BeforeUpdate", "OnBeforeUpdate"),
				("Update", "OnUpdate"),
				("AfterUpdate", "OnAfterUpdate"),
				("FrameEnd", "OnFrameEnd")
			};

			foreach (var (stageName, methodName) in stageNames)
			{
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
					public FuncSystem<World> {methodName}<{genericsArgs}>(Action<{genericsArgs}> system, ThreadingMode threadingType = ThreadingMode.Auto)
						{genericsArgsWhere}
					{{
						{objs}
						var checkInuse = () => {objsCheckInuse};
						var fn = (World args, Func<World, bool> runIf) =>
						{{
							if (runIf != null && !runIf.Invoke(args))
								return false;

							{objsGen}
							{objsLock}
							args.BeginDeferred();
							system({systemCall});
							args.EndDeferred();
							{objsUnlock}
							return true;
						}};
						var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.{stageName}, threadingType);
						Add(sys, Stages.{stageName});
						return sys;
					}}");
				}
			}

			// Also add versions that take just Action with no parameters
			foreach (var (stageName, methodName) in stageNames)
			{
				sb.AppendLine($@"
				public FuncSystem<World> {methodName}(Action system, ThreadingMode threadingType = ThreadingMode.Auto)
				{{
					var sys = new FuncSystem<World>(_world, (args, runIf) =>
					{{
						if (runIf?.Invoke(args) ?? true)
						{{
							system();
							return true;
						}}
						return false;
					}}, () => false, Stages.{stageName}, threadingType);
					Add(sys, Stages.{stageName});
					return sys;
				}}");
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
