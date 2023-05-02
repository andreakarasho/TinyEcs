// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 2;

using var world = new World();


var e1 = world.Entity();
var e2 = world.Entity();
var e3 = world.Entity();
var e4 = world.Entity();

Console.WriteLine("created {0:X16}", e1);
Console.WriteLine("created {0:X16}", e2);
Console.WriteLine("created {0:X16}", e3);
Console.WriteLine("created {0:X16}", e4);

e2.Destroy();
e2 = world.Entity();
Console.WriteLine("created {0:X16}", e2);

e3.Destroy();
e3 = world.Entity();
Console.WriteLine("created {0:X16}", e3);

e4.Destroy();
e4 = world.Entity();
Console.WriteLine("created {0:X16}", e4);



var rnd = new Random();

var sw = Stopwatch.StartNew();


var parent = world.Entity()
        .Set<Position>(new Position() { X = 1, Y = 1})
        .Set<Velocity>(new Velocity() { X = 3, Y = 3});

var arr = new [] {
    world.Entity(), world.Entity(), world.Entity(), world.Entity()
};

for (int i = 0; i < 10; ++i)
{
   //var entity = world.CreateEntity();
   //entity.Set<Position>();
   //entity.Set<Velocity>();

   _ = world.Entity()
       .Set<Position>()
       .Set<Velocity>()
       .Set<Likes, Dogs>()
       .Set<Likes, Cats>()
       .Set(TileType.Static)
       .Set<ChildOf>(arr[i % arr.Length])
       ;

   //world.Set<Position>(entity);
   //world.Set<Velocity>(entity);
}

//var list = new List<ulong>();

//for (int i = 0; i < 100; ++i)
//{
//    var e = world.CreateEntity();
//    world.Set<Position>(e);
//    world.Set<Velocity>(e);
//    world.Set<PlayerTag>(e);

//    list.Add(e);
//}

//foreach (var e in list)
//{
//    world.DestroyEntity(e);
//}

//for (int i = 0; i < list.Count; ++i)
//{
//    var e = world.CreateEntity();
//    Console.WriteLine(e);
//    //world.Set<Position>(e);
//    //world.Set<Velocity>(e);
//    //world.Set<PlayerTag>(e);
//}


//var e2 = world.CreateEntity();
//world.Set<Position>(e2);
//world.Set<Velocity>(e2);
//world.Set<int>(e2); 
//world.Set<float>(e2);

//var e3 = world.CreateEntity();
//world.Set<Position>(e3);
//world.Set<Velocity>(e3);
//world.Set<int>(e3);
//world.Set<float>(e3);
//world.Set<PlayerTag>(e3);

//var plat = world.CreateEntity();
//world.Tag(e3, plat);
////world.Untag(e3, plat);

Console.WriteLine("entities created in {0} ms", sw.ElapsedMilliseconds);

var query = world.Query()
    .With<EcsEntity>()
    .With<Position>()
    .With<Velocity>()
    .With<Likes, Dogs>()
    .With<Likes, Cats>()
    .With<ChildOf, EcsEntity>()
    .With<TileType>()
    ;


var queryCmp = world.Query()
    .With<EcsComponent>();

foreach (var it in queryCmp)
{
    ref var p = ref it.Field<EcsComponent>();

    for (var row = 0; row < it.Count; ++row)
    {
        //ref readonly var entity = ref it.Entity(row);
        //ref var pos = ref it.Get(ref p, row);

        //Console.WriteLine("Component {{ ID = {0}, GlobalID: {1}, Name = {2}, Size = {3} }}", entity, pos.GlobalIndex, pos.Name.ToString(), pos.Size);
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
    
    //for (int i = 0; i < 3600; ++i)
    {
        world.Step();
        //foreach (var it in query)
        //{
        //    ref var p = ref it.Field<Position>();
        //    ref var v = ref it.Field<Velocity>();

        //    for (var row = 0; row < it.Count; ++row)
        //    {
        //        ref readonly var entity = ref it.Entity(row);
        //        ref var pos = ref it.Get(ref p, row);
        //        ref var vel = ref it.Get(ref v, row);
        //    }
        //}
    }
    Console.WriteLine(sw.ElapsedMilliseconds);
}

Console.ReadLine();


static void ASystem(in Iterator it)
{
    //Console.WriteLine("ASystem - Count: {0}", it.Count);
    ref var e = ref it.Field<EcsEntity>();
    ref var p = ref it.Field<Position>();
    ref var v = ref it.Field<Velocity>();
    ref var b = ref it.Field<ChildOf, EcsEntity>();
    ref var t = ref it.Field<TileType>();

    for (var row = 0; row < it.Count; ++row)
    {
        //var e = it.Entity(row);
        //ref readonly var entity = ref it.Entity(row);
        ref var ent = ref it.Get(ref e, row);
        ref var pos = ref it.Get(ref p, row);
        ref var vel = ref it.Get(ref v, row);
        ref var bob = ref it.Get(ref b, row);
        ref var tileType = ref it.Get(ref t, row);

        Console.WriteLine("ent: {0}, world id: {1} parent id: {2} {3}", ent.ID, ent.WorldID, bob.Target.ID, tileType);

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

struct ChildOf {}

enum TileType
{
    Land = 1,
    Static
}

struct Position { public float X, Y; }
struct Velocity { public float X, Y; }
record struct PlayerTag();
struct ReflectedPosition { public float X, Y; }


record struct ATestComp(bool X, float Y, float Z);
record struct ASecondTestComp(IntPtr x);

static class DEBUG
{
    public static int VelocityCount = 0, PositionCount = 0, Both = 0;
}