// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 2 * 1;

using var ecs = new World();

unsafe
{
	//var posID = ecs.Component<Position>().ID;
	//var velID = ecs.Component<Velocity>().ID;
	//var serialID = ecs.Entity<Serial>();
	//var playerTagID = ecs.Entity<PlayerTag>();

	//ecs.SetSingleton(new Serial(){Value = 1});
	//ecs.SetSingleton(new PlayerTag() { ID = 0xDEADBEEF });

	var sb = new StringBuilder(4096);
	var sb0 = new StringBuilder();
	var sb1 = new StringBuilder();
	var sbMethods = new StringBuilder(4096);
	var sbWiths = new StringBuilder();
	var sbFieldDecl = new StringBuilder();
	var sbFnCalls = new StringBuilder();

	var delTemplate = "public delegate void QueryTemplate<{0}>({1});\n";
	var fnTemplate = """
		public void System<TPhase, {0}>(QueryTemplate<{0}> fn)
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

	for (int i = 0, max = Query.TERMS_COUNT; i < max; ++i)
	{
		sb0.Clear();
		sb1.Clear();
		sbFnCalls.Clear();

		sbWiths.AppendFormat("With<T{0}>();\n", i);
		sbFieldDecl.AppendFormat("var t{0}A = it.Field<T{0}>({0});\n", i);

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

		sbMethods.AppendFormat(fnTemplate, sb0.ToString(), sbWiths.ToString(), sbFieldDecl.ToString(), sbFnCalls.ToString());
	}


	var text =$"namespace TinyEcs;\npartial struct Query\n{{\n{sb.ToString() + "\n\n" + sbMethods.ToString()}\n}}";
	

	for (int i = 0; i < ENTITIES_COUNT; i++)
		ecs.Entity()
			.Set<Position>(new Position())
			.Set<Velocity>(new Velocity())
			//.Set<ManagedData>(new ManagedData() { Integer = i, Text = i.ToString() })
			;

	ecs.Entity()
			.Set<PlayerTag>()
			.Set<Position>(new Position() { X = 1 })
			.Set<Velocity>(new Velocity() { Y = 1 })
			.Set<ManagedData>(new ManagedData() { Integer = 1, Text = "PALLE" });

	ecs.Query()
		.With<EcsComponent>()
		.Iterate(static (ref Iterator it) => {
			var cmpA = it.Field<EcsComponent>(0);

			for (int i = 0; i < it.Count; ++i)
			{
				ref var cmp = ref cmpA[i];
				var entity = it.Entity(i);

				Console.WriteLine("{0} --> ID: {1} - SIZE: {2}", cmp.Size <= 0 ? "tag      " : "component", entity.ID, cmp.Size);
			}
		});

	ecs.Query()
		.With<Position>()
		.With<Velocity>()
		//.With<ManagedData>()
		.Without<PlayerTag>()
		.System(static (ref Iterator it) =>
		{
			var posA = it.Field<Position>(0);
			var velA = it.Field<Velocity>(1);
			//var manA = it.Field<ManagedData>(2);

			for (int i = 0, count = it.Count; i < count; ++i)
			{
				ref var pos = ref posA[i];
				ref var vel = ref velA[i];
				//ref var man = ref manA[i];

				pos.X *= vel.X;
				pos.Y *= vel.Y;
			}
		});

	//ecs.Query()
	//	.With<Position>()
	//	.With<Velocity>()
	//	.With<ManagedData>()
	//	.With<PlayerTag>()
	//	.System(static (ref Iterator it) =>
	//	{
	//		var posA = it.Field<Position>(0);
	//		var velA = it.Field<Velocity>(1);
	//		var manA = it.Field<ManagedData>(2);

	//		for (int i = 0, count = it.Count; i < count; ++i)
	//		{
	//			ref var pos = ref posA[i];
	//			ref var vel = ref velA[i];
	//			ref var man = ref manA[i];

	//			pos.X *= vel.X;
	//			pos.Y *= vel.Y;
	//		}
	//	});

	ecs.Query()
		//.Without<PlayerTag>()
		.System<EcsSystemPhaseOnUpdate, Position, Velocity>((ref Position pos, ref Velocity vel) =>
		{
			pos.X *= vel.X;
			pos.Y *= vel.Y;
		});
}

var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;

while (true)
{
	var cur = (start - last) / 1000f;

	for (int i = 0; i < 3600; ++i)
	{
		ecs.Step(cur);
	}

	last = start;
	start = sw.ElapsedMilliseconds;

	Console.WriteLine("query done in {0} ms", start - last);
}



enum TileType
{
	Land,
	Static
}


struct Serial { public uint Value; }
struct Position { public float X, Y, Z; }
struct Velocity { public float X, Y; }
struct PlayerTag { }

struct CustomEvent { }

struct Likes;
struct Dogs { }
struct Apples { }

struct TestStr { public byte v; }

struct ManagedData { public string Text; public int Integer; }

struct Context1 {}
struct Context2 {}
