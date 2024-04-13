// https://github.com/jasonliang-dev/entity-component-system
using System.Diagnostics;
using System;
using TinyEcs;
using System.Runtime.CompilerServices;

const int ENTITIES_COUNT = (524_288 * 2 * 1);


using var ecs = new World();

ecs.Entity<PlayerTag>().Set<Networked>();
ecs.Entity<Likes>();
ecs.Entity<Dogs>();
ecs.Entity<Position>();
ecs.Entity<Velocity>().Set<Networked>();

ecs.Query<With<Networked>>()
	.Each((EntityView asd) => {
	Console.WriteLine("networked entity {0}", asd.ID);
});

ecs.OnEntityDeleted += en => {
	var qry = ecs.QueryBuilder()
		.With<Defaults.Wildcard>(en)
		.Build();

	qry.Each((EntityView child) => child.Delete());
};


var alice = ecs.Entity("Alice");
var carl = ecs.Entity("Carl");
var thatPerson = ecs.Entity("That person");


var likes = ecs.Entity("Likes");
var palle = ecs.Entity("Palle");

carl.Set(likes, palle);
carl.Set(likes, alice);

carl.AddChild(palle);
carl.AddChild(alice);
alice.AddChild(likes);

// this remove (ChildOf, Carl) and get replaced with (ChildOf, ThatPerson)
// because ChildOf is Unique
thatPerson.AddChild(palle);


var aa = ecs.Entity("Alice");

// Carl likes 23 apples
//ecs.BeginDeferred();
carl.Set<Likes, Apples>(new Apples() {Amount = 23});
ref var apples = ref carl.Get<Likes, Apples>();
apples.Amount += 11;
//ecs.EndDeferred();
carl.Set(new Apples() { Amount = 9 });

// Carl likes dogs
carl.Set<Likes, Dogs>();
var h = carl.Has<Likes, PlayerTag>();
var h2 = carl.Has(likes, alice);
var h3 = carl.Has<Defaults.Wildcard, Apples>();
var h4 = carl.Has<Defaults.Wildcard, Defaults.Wildcard>();
var h5 = carl.Has<Defaults.Wildcard>();

carl.Set(alice);
var k = carl.Has(alice);


// Carl likes Alice
carl.Set<Likes>(alice);

// Get the 23 apples that Carl likes
ref var apples2 = ref carl.Get<Apples>();

apples.Amount += 1;
apples2.Amount += 1;

var id = carl.Target<Likes>();


// That person likes Alice
thatPerson.Set<Likes>(alice);

ecs.Query<With<(Defaults.Wildcard, Defaults.Wildcard)>>()
	.Each((EntityView entity) => {
		//Console.WriteLine("{0} ({1},{2})", entity.Name(), ecs.Entity(entity.Action<Wildcard>()).Name(), ecs.Entity(entity.Target<Wildcard>()).Name());

		var index = 0;
		EcsID actionId = 0;
		EcsID targetId = 0;

		while ((actionId = entity.Action<Defaults.Wildcard>(index)) != 0 && (targetId = entity.Target<Defaults.Wildcard>(index)) != 0)
		{
			Console.WriteLine("{0} ({1}, {2})", entity.Name(), ecs.Entity(actionId).Name(), ecs.Entity(targetId).Name());

			index += 1;
		}
});

// Gimme all entities that are liked by something
ecs.Query<With<(Likes, Defaults.Wildcard)>>()
	.Each((EntityView entity) => {
		var index = 0;
		EcsID targetId = 0;

		while ((targetId = entity.Target<Likes>(index++)) != 0)
		{
			Console.WriteLine("{0} Likes {1}", entity.Name(), ecs.Entity(targetId).Name());
		}
});

// Gimme all entities that likes apples
ecs.Query<(With<(Likes, Apples)>, With<Apples>)>()
	.Each((EntityView entity, ref (Likes, Apples) applesByRelation, ref Apples apples) => {
		applesByRelation.Item2.Amount += 1000;
		Console.WriteLine("{0} Likes {1} Apples", entity.Name(), applesByRelation.Item2.Amount);
});

// Gemme all entities that have a relation with Apples
ecs.Query<With<(Defaults.Wildcard, Apples)>>()
	.Each((EntityView entity, ref Apples apples) => {
		Console.WriteLine("{0} Likes {1} Apples", entity.Name(), apples.Amount);
});



ecs.Deferred(w => {
	carl.Set<PlayerTag>()
		.Set(new Position() {X = 999});

	carl.Get<Position>().X += 1;

	w.Deferred(w => {
		carl.Get<Position>().X += 1;
	});
});

ref var ppp = ref carl.Get<Position>();

var scheduler = new Scheduler(ecs);

scheduler.AddSystem((Local<int> i32, Res<string> str, Local<string> strLocal) => {
	Console.WriteLine(i32.Value++);
})
.RunIf(() => true)
.RunIf((Res<GameStates> state, Res<GameStates> state1, Res<GameStates> state2, Query<Velocity> velQuery) => state.Value == GameStates.InGame);

scheduler.AddSystem((Local<int> i32, Res<string> str, SchedulerState schedState) => {
	Console.WriteLine(i32.Value++);

	schedState.AddResource(23ul);
}, Stages.Startup);
scheduler.AddSystem((Res<ulong> ul) => {
	Console.WriteLine(ul.Value++);
});
scheduler.AddState<GameStates>();
scheduler.AddSystem(() => Console.WriteLine("playing the game"))
	.RunIf((Res<GameStates> state) => state == GameStates.InGame);

scheduler.AddSystem(() => Console.WriteLine("game paused"))
	.RunIf((Res<GameStates> state) => state.Value == GameStates.Paused)
	.RunIf((Query<Position, (With<PlayerTag>, Without<ComponentInfo>)> query)
		=> query.Count() > 0 && query.Single<Position>().X > 0);

scheduler.AddSystem((Res<GameStates> state) =>
	state.Value = state.Value switch
	{
		GameStates.InGame => GameStates.Paused,
		GameStates.Paused => GameStates.InGame,
		_ => state.Value,
	});

scheduler.AddEvent<MyEvent>();
scheduler.AddSystem((EventWriter<MyEvent> writer) => {
	writer.Enqueue(new MyEvent() { Value = 1 });
	writer.Enqueue(new MyEvent() { Value = 2 });
	writer.Enqueue(new MyEvent() { Value = 3 });
});
scheduler.AddSystem((EventReader<MyEvent> reader) => {
	foreach (var val in reader.Read())
	{
		Console.WriteLine(val.Value);
	}
});

scheduler.AddPlugin<MyPlugin>();

scheduler.AddSystem((
		Query<(Position, Velocity), (Not<PlayerTag>, Not<Likes>)> query0,
		Query<Position> query1,
		Res<string> myText
	) =>
	{
		query0.Each((ref Position pos, ref Velocity vel) => {
			pos.X *= vel.X;
			pos.Y *= vel.Y;
		});

		query1.Each((ref Position pos) => { });

		Console.WriteLine("What: {0}", myText.Value);
	}
);
scheduler.AddSystem((World world) => {

	world.BeginDeferred();
	var ff = world.Entity()
		.Set<Position>(default)
		.Set<Velocity>(default);
	world.EndDeferred();

	world.Deferred(w => {
		var ff = w.Entity()
			.Set<Position>(default)
			.Set<Velocity>(default)
			.Set<Likes>();
	});
});
scheduler.AddSystem((World world) => {
	Console.WriteLine("entities in world {0}", world.EntityCount);
});
scheduler.AddSystem((ComplexQuery complex) => {
	complex.Q0!.Each((ref Position pos, ref Velocity vel) => {

	});

	complex.Q1!.Each((ref Position pos, ref Velocity vel) => {

	});
});

scheduler.AddResource("oh shit i made it");

scheduler.Run();
scheduler.Run();


// var e = ecs.Entity("Main")
// 	.Set<Position>(new Position() {X = 2})
// 	.Set<Velocity>(new Velocity());



for (int i = 0; i < ENTITIES_COUNT / 1; i++)
	ecs.Entity()
		.Set<Position>(new Position() {X = i})
		.Set<Velocity>(new Velocity(){X = i})
		//  .Set<PlayerTag>()
		//  .Set<Dogs>()
		//  .Set<Likes>()
		 ;

// ecs.Entity<MyEvent>();

ecs.Query<Position>()
	.EachJob((EntityView ent, ref Position pos) => {

		ent.Set(new MyEvent() { Value = 2 });

		ref var v = ref ent.Get<MyEvent>();
		v.Value += 1;

		ref var v2 = ref ent.Get<MyEvent>();
		v.Value += 1;

		ent.Unset<MyEvent>();


		// var ee = ent.World.Entity()
		// 	.Set<MyEvent>(new MyEvent() { Value = 222 });

		// ref var v22 = ref ee.Get<MyEvent>();
		// v22.Value += 2;

	});

// for (var i = 7000; i < 8000 * 2; ++i)
// 	ecs.Entity((ulong)i).Delete();
var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;

while (true)
{
	for (int i = 0; i < 3600; ++i)
	{
		// ecs.Query<(Position, Velocity)>().Each((ref Position pos, ref Velocity vel) => {
		// 	pos.X *= vel.X;
		// 	pos.Y *= vel.Y;
		// });

		ecs//.Query<(Position, Velocity)>()
			.Each((ref Position pos, ref Velocity vel) => {
				pos.X *= vel.X;
				pos.Y *= vel.Y;
			});

		//scheduler.Run();
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
struct Apples { public int Amount; }

struct TestStr { public byte v; }

struct ManagedData { public string Text; public int Integer; }

struct Context1 {}
struct Context2 {}

struct Chunk;
struct ChunkTile;


class ComplexQuery : ISystemParam
{
	public Query<(Position, Velocity)>? Q0;
	public Query<(Position, Velocity), (With<PlayerTag>, Not<Likes>)>? Q1;

	void ISystemParam.New(object arguments)
	{
		var world = (World) arguments;
		Q0 = world.Query<(Position, Velocity)>();
		Q1 = world.Query<(Position, Velocity), (With<PlayerTag>, Not<Likes>)>();
	}
}


readonly struct MyPlugin : IPlugin
{
	public readonly void Build(Scheduler scheduler)
	{
		scheduler.AddSystem((Res<int> myNum) => Console.WriteLine("My num is {0}", myNum.Value), Stages.Startup);
		scheduler.AddResource(123);
	}
}

struct MyEvent
{
	public int Value;
}


enum GameStates
{
	InGame,
	Paused
}

struct Networked { }
