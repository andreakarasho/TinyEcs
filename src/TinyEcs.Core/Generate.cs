using System.Text;

namespace TinyEcs
{
	internal static class Generate
	{
		public static string CreateQueries(bool queryForEntity)
		{
			var sb = new StringBuilder(4096);
			var sb0 = new StringBuilder(4096);
			var sb1 = new StringBuilder(4096);
			var sbIterators = new StringBuilder(4096);
			var sbWiths = new StringBuilder(4096);
			var sbCmpIndices = new StringBuilder(4096);
			var sbFieldDecl = new StringBuilder(4096);
			var sbFnCalls = new StringBuilder(4096);
			var sbUnsafeAdd = new StringBuilder(4096);
			var sbWhere = new StringBuilder(4096);

			var delTemplate = $"public delegate void QueryTemplate{(queryForEntity ? "WithEntity" : "")}<{{0}}>({{1}}) {{2}};\n";

			var iteratorTemplate = """

		public void Each<{0}>(QueryTemplate{4}<{0}> fn) {5}
		{{
			{1}
			foreach (var archetype in this)
			{{
				{9}
				foreach (ref var chunk in archetype.Chunks)
				{{
					{2}
					{7}
					ref var last = ref Unsafe.Add(ref t0A, chunk.Count);
					while (Unsafe.IsAddressLessThan(ref t0A, ref last))
					{{
						fn({3});
						{8}
						{6}
					}}
				}}
			}}
		}}

""";

			for (int i = 0, max = Query.TERMS_COUNT; i < max; ++i)
			{
				sb0.Clear();
				sb1.Clear();
				sbFnCalls.Clear();
				sbWhere.Clear();
				sbUnsafeAdd.Clear();

				sbWiths.AppendFormat("With<T{0}>();\n", i);
				sbCmpIndices.AppendFormat("var column{0} = archetype.GetComponentIndex<T{0}>();\n", i);
				sbFieldDecl.AppendFormat("ref var t{0}A = ref chunk.GetReference<T{0}>(column{0});\n", i);

				if (queryForEntity)
				{
					sbFnCalls.Append("in firstEnt, ");
					sb1.Append("ref readonly EntityView entity, ");
				}

				for (int j = 0, count = i; j <= count; ++j)
				{
					sb0.AppendFormat("T{0}", j);
					sb1.AppendFormat("ref T{0} t{0}", j);
					sbFnCalls.AppendFormat("ref t{0}A", j);
					sbUnsafeAdd.AppendFormat("t{0}A = ref Unsafe.Add(ref t{0}A, 1);\n", j);
					sbWhere.AppendFormat("where T{0} : struct ", j);

					if (j + 1 <= count)
					{
						sb0.Append(", ");
						sb1.Append(", ");
						sbFnCalls.Append(", ");
					}
				}

				sb.AppendFormat(delTemplate, sb0, sb1, sbWhere);

				sbIterators.AppendFormat(
					iteratorTemplate,
					sb0,
					sbWiths,
					sbFieldDecl,
					sbFnCalls,
					queryForEntity ? "WithEntity" : "",
					sbWhere,
					sbUnsafeAdd,
					queryForEntity ? "ref var firstEnt = ref chunk.Entities[0];\n" : "",
					queryForEntity ? "firstEnt = ref Unsafe.Add(ref firstEnt, 1);\n" : "",
					sbCmpIndices
				);
			}


			var text = $"namespace TinyEcs;\npartial class Query\n{{\n{sb}\n\n{sbIterators}\n}}";

			return text;
		}
	}
}
