// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 2 * 1;

using var world = new World();
//using var world2 = new World();


//world2.Entity().Set(new Position() { X = 123.23f, Y = -23f, Z = 2f });
//var eeee = world.Entity().Set<Likes>();
//eeee.Set<Position>();


//foreach (var it in world.Query().With<Likes>().With<Position>())
//{
//    ref var l = ref it.Field<Likes>();
//    ref var b = ref it.Field<Position>();

//    for (int i = 0; i < it.Count; ++i)
//    {
//        ref var ll = ref it.Get(ref l, i);
//        ref var bb = ref it.Get(ref b, i);
//    }
//}

//foreach (var it in world2.Query().With<Position>())
//{
//    ref var l2 = ref it.Field<Position>();

//    for (int i = 0; i < it.Count; ++i)
//    {
//        ref var ll2 = ref it.Get(ref l2, i);
//    }
//}


var rnd = new Random();
var sw = Stopwatch.StartNew();

var root = world.Entity();

for (int i = 0; i < 100; ++i)
{
    var ee = world.Entity()
        .Set<Position>()
        .Set<Velocity>()
        .Set<Likes, Dogs>()
        .Set<Likes, Cats>()
        .Set(TileType.Static)
        .Set<Likes>(root)
        .AttachTo(root)
        ;

}


Console.WriteLine("entities created in {0} ms", sw.ElapsedMilliseconds);

var query = world.Query()
    .With<Position>()
    .With<Velocity>()
    .With<Likes, Dogs>()
    .With<Likes, Cats>()
    .With<Likes, EntityView>()
    //.With<EcsParent>()
    .With<TileType>()
    ;



var queryCmp = world.Query()
    .With<EcsComponent>();

foreach (var it in queryCmp)
{
    ref var e = ref it.Field<EntityView>();
    ref var p = ref it.Field<EcsComponent>();

    for (var row = 0; row < it.Count; ++row)
    {
        ref var ent = ref it.Get(ref e, row);
        ref var metadata = ref it.Get(ref p, row);

        //it.World.Unset<EcsEnabled>(ent.ID);
        
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
        world.Step();
    Console.WriteLine(sw.ElapsedMilliseconds);
}

Console.ReadLine();


static void ASystem(in Iterator it)
{
    ref var e = ref it.Field<EntityView>();
    ref var p = ref it.Field<Position>();
    ref var v = ref it.Field<Velocity>();
    ref var t = ref it.Field<TileType>();
    //ref var b = ref it.Field<EcsChildOf, EntityView>();

    for (var row = 0; row < it.Count; ++row)
    {
        //ref var parentID = ref it.Get(ref b, row);
        ref var ent = ref it.Get(ref e, row);
        ref var pos = ref it.Get(ref p, row);
        ref var vel = ref it.Get(ref v, row);
        ref var tileType = ref it.Get(ref t, row);

        pos.X *= vel.X;
        pos.Y *= vel.Y;
    }
}


static void ASystem2(in Iterator it)
{
    //Console.WriteLine("ASystem2 - Count: {0}", it.Count);

    ref var p = ref it.Field<Position>();
    ref var v = ref it.Field<Velocity>();

    for (var row = 0; row < it.Count; ++row)
    {
        //ref readonly var entity = ref it.Entity(row);
        ref var pos = ref it.Get(ref p, row);
        ref var vel = ref it.Get(ref v, row);
    }
}

static void PreUpdate(in Iterator it)
{
    Console.WriteLine("pre update");
}

static void PostUpdate(in Iterator it)
{
    Console.WriteLine("post update");

    ref var p = ref it.Field<Position>();

    for (var row = 0; row < it.Count; ++row)
    {
        //var e = it.Entity(row);
        //ref readonly var entity = ref it.Entity(row);
        ref var pos = ref it.Get(ref p, row);

        Console.WriteLine("position: {0}, {1}", pos.X, pos.Y);
    }
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
record struct PlayerTag();
struct ReflectedPosition { public float X, Y; }


record struct ATestComp(bool X, float Y, float Z);
record struct ASecondTestComp(IntPtr x);

static class DEBUG
{
    public static int VelocityCount = 0, PositionCount = 0, Both = 0;
}