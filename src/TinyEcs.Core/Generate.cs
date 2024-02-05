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
			var sbMethods = new StringBuilder(4096);
			var sbIterators = new StringBuilder(4096);
			var sbWiths = new StringBuilder(4096);
			var sbFieldDecl = new StringBuilder(4096);
			var sbFnCalls = new StringBuilder(4096);

			var delTemplate = $"public delegate void QueryTemplate{(queryForEntity ? "WithEntity" : "")}<{{0}}>({{1}});\n";
			var fnTemplate = """

		public void System<TPhase, {0}>(QueryTemplate{4}<{0}> fn)
		{{
			{1}
			var terms = Terms;
			EcsID query = terms.Length > 0 ? _world.Entity() : 0;

			_world.Entity()
				.Set(new EcsSystem((ref Iterator it) =>
				{{
					{2}
					for (int i = 0; i < it.Count; ++i)
					{{
						fn({3});
					}}
				}}, query, terms, float.NaN))
				.Set<TPhase>();
		}}

""";

			var iteratorTemplate = """

		public void Iterator<{0}>(QueryTemplate{4}<{0}> fn)
		{{
			{1}

			_world.Query(Terms, (ref Iterator it) =>
			{{
				{2}
				for (int i = 0; i < it.Count; ++i)
				{{
					fn({3});
				}}
			}});
		}}

""";

			for (int i = 0, max = Query.TERMS_COUNT; i < max; ++i)
			{
				sb0.Clear();
				sb1.Clear();
				sbFnCalls.Clear();

				sbWiths.AppendFormat("With<T{0}>();\n", i);
				sbFieldDecl.AppendFormat("var t{0}A = it.Field<T{0}>({0});\n", i);

				if (queryForEntity)
				{
					sbFnCalls.Append("in it.Entity(i), ");
					sb1.Append("ref readonly EntityView entity, ");
				}

				for (int j = 0, count = i; j <= count; ++j)
				{
					sb0.AppendFormat("T{0}", j);
					sb1.AppendFormat("ref T{0} t{0}", j);
					sbFnCalls.AppendFormat("ref t{0}A[i]", j);

					if (j + 1 <= count)
					{
						sb0.Append(", ");
						sb1.Append(", ");
						sbFnCalls.Append(", ");
					}
				}

				sb.AppendFormat(delTemplate, sb0.ToString(), sb1.ToString());

				sbMethods.AppendFormat(fnTemplate, sb0.ToString(), sbWiths.ToString(), sbFieldDecl.ToString(), sbFnCalls.ToString(), queryForEntity ? "WithEntity" : "");

				sbIterators.AppendFormat(iteratorTemplate, sb0.ToString(), sbWiths.ToString(), sbFieldDecl.ToString(), sbFnCalls.ToString(), queryForEntity ? "WithEntity" : "");
			}


			var text = $"namespace TinyEcs;\npartial struct Query\n{{\n{sb.ToString() + "\n\n" + sbMethods.ToString()}" + "\n\n" + sbIterators.ToString() + "\n}";

			return text;
		}
	}
}
