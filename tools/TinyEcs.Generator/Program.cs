using System.Text;
using Microsoft.CodeAnalysis;

// var text0 = CreateQueries(false);
// var text1 = CreateQueries(true);
// var text2 = CreateWorldQueries();
//
// var dir = new DirectoryInfo("../../../../../src");
//
// File.WriteAllText(Path.Combine(dir.FullName, "Query.Generics.cs"), text0);
// File.WriteAllText(Path.Combine(dir.FullName, "Query.Generics.WithEntity.cs"), text1);
// File.WriteAllText(Path.Combine(dir.FullName, "World.Queries.cs"), text2);
//
//
// string CreateQueries(bool queryForEntity)
// {
// 	var sb = new StringBuilder(4096);
// 	var sb0 = new StringBuilder(4096);
// 	var sb1 = new StringBuilder(4096);
// 	var sbIterators = new StringBuilder(4096);
// 	var sbWiths = new StringBuilder(4096);
// 	var sbCmpIndices = new StringBuilder(4096);
// 	var sbFieldDecl = new StringBuilder(4096);
// 	var sbFnCalls = new StringBuilder(4096);
// 	var sbUnsafeAdd = new StringBuilder(4096);
// 	var sbWhere = new StringBuilder(4096);
//
// 	var delTemplate = $"public delegate void QueryTemplate{(queryForEntity ? "WithEntity" : "")}<{{0}}>({{1}}) {{2}};\n";
//
// 	var iteratorTemplate = """
//
// 		public void Each{4}<{0}>(QueryTemplate{4}<{0}> fn) {5}
// 		{{
// 			{1}
// 			foreach (var archetype in this)
// 			{{
// 				{9}
// 				foreach (ref readonly var chunk in archetype)
// 				{{
// 					{2}
// 					{7}
// 					ref var last = ref Unsafe.Add(ref t0A, chunk.Count);
// 					while (Unsafe.IsAddressLessThan(ref t0A, ref last))
// 					{{
// 						fn({3});
// 						{8}
// 						{6}
// 					}}
// 				}}
// 			}}
// 		}}
//
// """;
//
// 	for (int i = 0, max = 25; i < max; ++i)
// 	{
// 		sb0.Clear();
// 		sb1.Clear();
// 		sbFnCalls.Clear();
// 		sbWhere.Clear();
// 		sbUnsafeAdd.Clear();
//
// 		sbWiths.AppendFormat("With<T{0}>();\n", i);
// 		sbCmpIndices.AppendFormat("var column{0} = archetype.GetComponentIndex<T{0}>();\n", i);
// 		sbFieldDecl.AppendFormat("ref var t{0}A = ref chunk.GetReference<T{0}>(column{0});\n", i);
//
// 		if (queryForEntity)
// 		{
// 			sbFnCalls.Append("in firstEnt, ");
// 			sb1.Append("ref readonly EntityView entity, ");
// 		}
//
// 		for (int j = 0, count = i; j <= count; ++j)
// 		{
// 			sb0.AppendFormat("T{0}", j);
// 			sb1.AppendFormat("ref T{0} t{0}", j);
// 			sbFnCalls.AppendFormat("ref t{0}A", j);
// 			sbUnsafeAdd.AppendFormat("t{0}A = ref Unsafe.Add(ref t{0}A, 1);\n", j);
// 			sbWhere.AppendFormat("where T{0} : struct ", j);
//
// 			if (j + 1 <= count)
// 			{
// 				sb0.Append(", ");
// 				sb1.Append(", ");
// 				sbFnCalls.Append(", ");
// 			}
// 		}
//
// 		sb.AppendFormat(delTemplate, sb0, sb1, sbWhere);
//
// 		sbIterators.AppendFormat(
// 			iteratorTemplate,
// 			sb0,
// 			sbWiths,
// 			sbFieldDecl,
// 			sbFnCalls,
// 			queryForEntity ? "WithEntity" : "",
// 			sbWhere,
// 			sbUnsafeAdd,
// 			queryForEntity ? "ref var firstEnt = ref chunk.Entities[0];\n" : "",
// 			queryForEntity ? "firstEnt = ref Unsafe.Add(ref firstEnt, 1);\n" : "",
// 			sbCmpIndices
// 		);
// 	}
//
//
// 	var text = $"namespace TinyEcs;\npartial class Query\n{{\n{sb}\n\n{sbIterators}\n}}";
//
// 	return text;
// }
//
// string CreateWorldQueries()
// {
// 	var template =
// 		"""
//
//
// 		 public delegate void QueryDelegate<{0}>({6}){1};
// 		 public void Query<TQuery, {0}>(QueryDelegate<{0}> fn)
// 			    where TQuery : ITuple
// 			    {1}
// 		    {{
// 			    var query = new QueryInternal<TQuery>(Archetypes);
//
// 			    foreach (var arch in query)
// 			    {{
// 					// columns
// 					{2}
//
// 					foreach (ref readonly var chunk in arch)
// 					{{
// 						// field list
// 						{3}
//
// 						ref var last = ref Unsafe.Add(ref t0A, chunk.Count);
// 						while (Unsafe.IsAddressLessThan(ref t0A, ref last))
// 						{{
// 							// sign list
// 							fn({4});
//
// 							// unsafe add list
// 							{5}
// 						}}
// 					}}
// 			    }}
// 		    }}
//
//
// 		""";
//
// 	var file = new StringBuilder();
//
// 	var genericList = new StringBuilder();
// 	var genericListSign = new StringBuilder();
// 	var whereList = new StringBuilder();
// 	var columnList = new StringBuilder();
// 	var fieldList = new StringBuilder();
// 	var signList = new StringBuilder();
// 	var unsafeAddList = new StringBuilder();
//
// 	for (int i = 0, max = 25; i < max; ++i)
// 	{
// 		genericList.Clear();
// 		whereList.Clear();
// 		unsafeAddList.Clear();
// 		signList.Clear();
// 		genericListSign.Clear();
//
// 		columnList.AppendFormat("var column{0} = arch.GetComponentIndex<T{0}>();\n", i);
// 		fieldList.AppendFormat("ref var t{0}A = ref chunk.GetReference<T{0}>(column{0});\n", i);
//
// 		for (int j = 0, count = i; j <= count; ++j)
// 		{
// 			genericList.AppendFormat("T{0}", j);
// 			genericListSign.AppendFormat("ref T{0} t{0}", j);
// 			signList.AppendFormat("ref t{0}A", j);
// 			unsafeAddList.AppendFormat("t{0}A = ref Unsafe.Add(ref t{0}A, 1);\n", j);
// 			whereList.AppendFormat("where T{0} : struct ", j);
//
// 			if (j + 1 <= count)
// 			{
// 				genericListSign.Append(", ");
// 				genericList.Append(", ");
// 				signList.Append(", ");
// 			}
// 		}
//
// 		file.AppendFormat(template,
// 			genericList,
// 			whereList,
// 			columnList,
// 			fieldList,
// 			signList,
// 			unsafeAddList,
// 			genericListSign
// 		);
// 	}
//
// 	var text = $@"
// 			   namespace TinyEcs;
// 	           sealed partial class World
// 	           {{
// 	            {file}
// 				}}
// 	           ";
//
// 	return text;
// }


[Generator]
public sealed class Generator : IIncrementalGenerator
{
	private const int MAX_GENERICS = 25;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput((IncrementalGeneratorPostInitializationContext postContext) =>
		{
			postContext.AddSource("TinyEcs.g.cs", Generate());
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

		static string GeenrateTypeParams(int count, string separator, Func<int, string> generator)
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

		static string GenerateQueryTemplateDelegates()
		{
			var sb = new StringBuilder();

			for (var i = 0; i < MAX_GENERICS; ++i)
			{
				var typeParams = GeenrateTypeParams(i + 1, ", ", j => $"T{j}");
				var whereParams = GeenrateTypeParams(i + 1, " ", j => $"where T{j} : struct");
				var signParams = GeenrateTypeParams(i + 1, ", ", j => $"ref T{j} t{j}");

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
				var typeParams = GeenrateTypeParams(i + 1, ", ", j => $"T{j}");
				var whereParams = GeenrateTypeParams(i + 1, " ", j => $"where T{j} : struct");

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
				var typeParams = GeenrateTypeParams(i + 1, ", ", j => $"T{j}");
				var whereParams = GeenrateTypeParams(i + 1, " ",j => $"where T{j} : struct");
				var fillMissingTerms = GeenrateTypeParams(i + 1, "", j =>
					$@"_fullTerms[^{j + 1}].ID = Lookup.Entity<T{j}>.HashCode;
					   _fullTerms[^{j + 1}].Op = TermOp.With;
					");
				var columnIndices = GeenrateTypeParams(i + 1, "\n" , j => $"var column{j} = arch.GetComponentIndex<T{j}>();");
				var fieldList = GeenrateTypeParams(i + 1, "\n" , j => $"ref var t{j}A = ref chunk.GetReference<T{j}>(column{j});");
				var signCallback = GeenrateTypeParams(i + 1, ", " , j => $"ref t{j}A");
				var advanceField = GeenrateTypeParams(i + 1, "\n" , j => $"t{j}A = ref Unsafe.Add(ref t{j}A, 1);");

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
	}
}
