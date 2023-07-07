// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 2 * 1;


var ecs = new Ecs();

for (int i = 0; i < 1; i++)
    ecs.Spawn()
        .Set<Position>(new Position() { X = 123, Y = 0.23f, Z = -1f})
        .Set<Velocity>(new Velocity() {  X = -0.891237f, Y = 1f});


var builder = ecs.Query()
	.With<Position>()
	.With<Velocity>()
	.Without<float>()
	;



static void Setup(Commands cmds, in EntityIterator it, float delta)
{
    var sw = Stopwatch.StartNew();

    for (int i = 0; i < 3; i++)
        cmds.Spawn()
            .Set<Position>()
            .Set<Velocity>()
            .Set<int>(123);

    var character = cmds.Spawn()
		.Set(new SerialComponent() { Value = 0xDEAD_BEEF });

    Console.WriteLine("Setup done in {0} ms", sw.ElapsedMilliseconds);
}

static void ParseQuery(Commands cmds, in EntityIterator it, float delta)
{
    Console.WriteLine("parse query");

	var sw = Stopwatch.StartNew();
	var posF = it.Field<Position>();
	var velF = it.Field<Velocity>();

    foreach (ref readonly var e in it)
    {
        ref var pos = ref posF.Get();
        ref var vel = ref velF.Get();

        //cmds.Set<float>(e);
        //e.Set<float>();

        //cmds.Despawn(e);

        //Console.WriteLine("entity {0}", e.ID);
    }

	Console.WriteLine("query done in {0} ms - parsed {1}", sw.ElapsedMilliseconds, it.Count);
}

unsafe
{
	//ecs.AddStartupSystem(ecs.Query(), &Setup);
	ecs.AddSystem(in builder, &ParseQuery);
}

while (true)
{
	ecs.Step(0f);
}


var list = new List<object>();
list.Add(123);
list.Add("asdasd");
list.Add(new SerialComponent() { Value = 0xDEAD_BEEF });
ref var ser = ref Unsafe.Unbox<SerialComponent>(list[2]);
ser.Value = 12345;


using var world = new World();
using var cmd = new Commands(world);


var we = world.Spawn().Set<Position>();


var parent = cmd.Spawn();
var child = cmd.Spawn();

child.AttachTo(parent);

for (int i = 0; i < 10; ++i)
    cmd.Spawn().AttachTo(child);

cmd.Merge();

var qq = world.Query()
        .With<EcsChild>();

foreach (var it in qq)
{
    var c = it.Field<EcsChild>();
    var e = it.Field<EntityView>();

    foreach (var row in it)
    {
        ref var ee = ref e.Get();
        ref var child1 = ref c.Get();
        Console.WriteLine("child {0} -> parent {1}", ee.ID, child1.Parent);
    }
}


Console.WriteLine("");


var rnd = new Random();
var sw = Stopwatch.StartNew();

var root = world.Spawn();

for (int i = 0; i < ENTITIES_COUNT; ++i)
{
    var ee = cmd.Spawn()
        .Set<Position>()
        .Set<Velocity>()
        //.Set(TileType.Static)
        //.Set<Likes>(root)
        //.AttachTo(root)
        ;

}

cmd.Merge();

Console.WriteLine("entities created in {0} ms", sw.ElapsedMilliseconds);

var query = world.Query()
    .With<Position>()
    .With<Velocity>()
    //.With<TileType>()
    ;

var queryCmp = world.Query()
    .With<EcsComponent>();

foreach (var it in queryCmp)
{
    var e = it.Field<EntityView>();
    var p = it.Field<EcsComponent>();

    foreach (var row in it)
    {
        ref var ent = ref e.Get();
        ref var metadata = ref p.Get();
        Console.WriteLine("Component {{ ID = {0}, GlobalID: {1}, Name = {2}, Size = {3} }}", metadata.ID, metadata.GlobalIndex, "", metadata.Size);
    }
}

unsafe
{
    //world.RegisterSystem(query, &ASystem);
    //world.RegisterSystem(world.Query(), &PreUpdate, SystemPhase.OnPreUpdate);
    //world.RegisterSystem(world.Query().With<Position>(), &PostUpdate, SystemPhase.OnPostUpdate);
}


while (true)
{
    sw.Restart();

    //for (int i = 0; i < 3600; ++i)
    //{
    //    world.Step(ecs0f);
    //}

    Console.WriteLine(sw.ElapsedMilliseconds);
}

Console.ReadLine();


static void ASystem(in EntityIterator it, float deltaTime)
{
    var e = it.Field<EntityView>();
    var p = it.Field<Position>();
    var v = it.Field<Velocity>();
    var t = it.Field<TileType>();

    foreach (var _ in it)
    {
        ref var ent = ref e.Get();
        ref var pos = ref p.Get();
        ref var vel = ref v.Get();
        ref var tile = ref t.Get();

        pos.X *= vel.X;
        pos.Y *= vel.Y;
    }
}


static void ASystem2(in EntityIterator it)
{
    //Console.WriteLine("ASystem2 - Count: {0}", it.Count);

    //ref var p = ref it.Field<Position>();
    //ref var v = ref it.Field<Velocity>();

    //for (var row = 0; row < it.Count; ++row)
    //{
    //    ref readonly var entity = ref it.Entity(row);
    //    ref var pos = ref it.Get(ref p, row);
    //    ref var vel = ref it.Get(ref v, row);
    //}
}

static void PreUpdate(in EntityIterator it)
{
    Console.WriteLine("pre update");
}

static void PostUpdate(in EntityIterator it)
{
    Console.WriteLine("post update");

    //ref var p = ref it.Field<Position>();

    //for (var row = 0; row < it.Count; ++row)
    //{
    //    //var e = it.Entity(row);
    //    //ref readonly var entity = ref it.Entity(row);
    //    ref var pos = ref it.Get(ref p, row);

    //    Console.WriteLine("position: {0}, {1}", pos.X, pos.Y);
    //}
}

struct Likes { }
struct Dogs { }
struct Cats { }


enum TileType
{
    Land,
    Static
}

struct Position { public float X, Y, Z; }
struct Velocity { public float X, Y; }
struct PlayerTag { }
struct ReflectedPosition { public float X, Y; }

struct BigComponent
{
    private unsafe fixed uint _buf[128];
}

ref struct Aspect<T0, T1, T2>
    where T0 : unmanaged
    where T1 : unmanaged
    where T2 : unmanaged
{
    public Field<T0> Field0;
    public Field<T1> Field1;
    public Field<T2> Field2;
}

struct SerialComponent
{
    public uint Value;
}

static class DEBUG
{
    public static int VelocityCount = 0, PositionCount = 0, Both = 0;
}