// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 2 * 1;

using var world = new World();
using var cmd = new Commands(world);


var we = world.Spawn().Set<Position>();
var weview = world.Query()
    .With<Position>()
    .Get(we);

var ff = weview.Has<Position>();


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

//var worldEntity = world.Entity();

//cmd.Entity();
//cmd.Entity();

//var sr = cmd.Entity();

//sr.Set<Position>()
//  .Set<Velocity>();

//sr.Unset<Position>();

//cmd.Set<Position>(worldEntity, new Position() { X = 23, Y = 566, Z = -123.02f });
//cmd.Unset<Position>(worldEntity);
//sr.Destroy();
//cmd.Merge();

//cmd.Destroy(world.Entity());
//cmd.Merge();


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
    world.RegisterSystem(query, &ASystem);
    //world.RegisterSystem(world.Query(), &PreUpdate, SystemPhase.OnPreUpdate);
    //world.RegisterSystem(world.Query().With<Position>(), &PostUpdate, SystemPhase.OnPostUpdate);
}


while (true)
{
    sw.Restart();

    for (int i = 0; i < 3600; ++i)
    {
        world.Step(0f);
    }

    Console.WriteLine(sw.ElapsedMilliseconds);
}

Console.ReadLine();


static void ASystem(in Iterator it, float deltaTime)
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


static void ASystem2(in Iterator it)
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

static void PreUpdate(in Iterator it)
{
    Console.WriteLine("pre update");
}

static void PostUpdate(in Iterator it)
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

static class DEBUG
{
    public static int VelocityCount = 0, PositionCount = 0, Both = 0;
}