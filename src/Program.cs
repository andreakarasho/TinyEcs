// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

const int ENTITIES_COUNT = 1_000_000 * 1;

using var world = new World();

var t0 = typeof(Relation<Likes, Dogs>);
var t1 = typeof(Relation<Likes, Cats>);


var bothCount = 0;
var velocicyCount = 0;
var positionCount = 0;

var rnd = new Random();
var sw = Stopwatch.StartNew();

for (int i = 0; i < ENTITIES_COUNT; ++i)
{
    var entity = world.CreateEntity();
    //world.Attach(entity, positionID);
    //world.Attach(entity, velocityID);

    world.Attach<Position>(entity);
    world.Attach<Velocity>(entity);

    //world.Detach<Velocity>(entity);

    //world.Attach<Name>(entity);
    //world.Attach<PlayerTag>(entity);
    //world.Attach<Relation<Likes, Dogs>>(entity);
    //world.Attach<Relation<Likes, Cats>>(entity);


    world.Set(entity, new Position() { X = 200f });
    world.Set(entity, new Velocity() { X = 100f });
    ref var p = ref world.Get<Position>(entity);

    if (rnd.Next() % 3 == 0)
    {
        //world.Destroy(entity);
    }

    bothCount++;
}

Console.WriteLine("spawned {0} entities in {1} ms", ENTITIES_COUNT, sw.ElapsedMilliseconds);

for (int i = 0; i < 1000; ++i)
{
    var entity = world.CreateEntity();
    world.Attach<Position>(entity);
    world.Attach<Velocity>(entity);
    world.Attach<PlayerTag>(entity);
}

//var pos = new Position() { X = 1, Y = 2 };
//var vel = new Velocity() { X = 3, Y = 4 };
//var entity2 = world.CreateEntity();
//world.Attach(entity2, positionID);
//world.Attach(entity2, velocityID);
//unsafe
//{
//    world.Set(entity2, positionID, new ReadOnlySpan<byte>(Unsafe.AsPointer(ref pos), Unsafe.SizeOf<Position>()));
//    world.Set(entity2, velocityID, new ReadOnlySpan<byte>(Unsafe.AsPointer(ref vel), Unsafe.SizeOf<Velocity>()));
//}

//pos = new Position() { X = 5, Y = 6 };
//vel = new Velocity() { X = 7, Y = 8 };
//var entity3 = world.CreateEntity();
//world.Attach(entity3, positionID);
//world.Attach(entity3, velocityID);
//unsafe
//{
//    world.Set(entity3, positionID, new ReadOnlySpan<byte>(Unsafe.AsPointer(ref pos), Unsafe.SizeOf<Position>()));
//    world.Set(entity3, velocityID, new ReadOnlySpan<byte>(Unsafe.AsPointer(ref vel), Unsafe.SizeOf<Velocity>()));
//}

//for (var i = 0; i < 100; ++i)
//{
//    world.Attach(world.CreateEntity(), velocityID);
//    velocicyCount++;
//}



unsafe
{
    world.RegisterSystem<Position, Velocity>(&TwoComponentsSystem);
    world.RegisterSystem<Position>(&PositionOnlySystem);
    world.RegisterSystem<Velocity>(&VelocityOnlySystem);
    world.RegisterSystem<Position, Velocity, PlayerTag>(&ThreeComponentsSystem);
    world.RegisterSystem<ATestComp, ASecondTestComp>(&PosAndTagComponentsSystem);
}

var query = world.Query()
    .With<Position>()
    .With<Velocity>()
    .Without<PlayerTag>()
    ;


foreach (var view in world.Query()
    .With<Relation<Likes, Dogs>>()
    .With<Relation<Likes, Cats>>())
{

}


while (true)
{
    DEBUG.VelocityCount = 0;
    DEBUG.PositionCount = 0;
    DEBUG.Both = 0;

    sw.Restart();

    //world.Step();
    var done = 0;
    foreach (var view in query)
    {
        ref readonly var entity = ref view.Entity;
        ref var pos = ref view.Get<Position>();
        ref var vel = ref view.Get<Velocity>();

        //world.Attach<float>(entity);

        //var e = world.CreateEntity();
        //world.Attach<Position>(e);
        //world.Attach<Velocity>(e);
        //world.DestroyEntity(e);

        //if (view.Has<Name>())
        //{
        //    ref var name = ref view.Get<Name>();
        //}
        //else
        //{

        //}

        pos.X++;
        vel.Y++;

        //world.Destroy(entity);

        ++done;
    }

    //Debug.Assert(done == ENTITIES_COUNT + 1000);

    Console.WriteLine(sw.ElapsedMilliseconds);

    //Debug.Assert(DEBUG.Both == bothCount);
    //Debug.Assert(DEBUG.VelocityCount == velocicyCount);
    //Debug.Assert(DEBUG.PositionCount == positionCount);

    //var e = world.CreateEntity();
    //world.Attach<Position>(e);
    ////world.Attach<Velocity>(e);
    //world.Attach<PlayerTag>(e);

    //var ee = world.CreateEntity();
    //world.Attach<ATestComp>(ee);
    //world.Attach<ASecondTestComp>(ee);

    positionCount++;
}

Console.WriteLine("done");
Console.ReadLine();


static void VelocityOnlySystem(in EcsView view, int row)
{
    ref var c0 = ref view.Get<Velocity>(0, row);
    c0.X++;

    DEBUG.VelocityCount++;
}

static void PositionOnlySystem(in EcsView view, int row)
{
    ref var c0 = ref view.Get<Position>(0, row);
    c0.X++;

    DEBUG.PositionCount++;
}

static void TwoComponentsSystem(in EcsView view, int row)
{
    ref var c0 = ref view.Get<Position>(0, row);
    c0.X++;

    ref var c1 = ref view.Get<Velocity>(1, row);
    c1.X++;

    DEBUG.Both++;
}

static void ThreeComponentsSystem(in EcsView view, int row)
{
    ref var c0 = ref view.Get<Position>(0, row);
    c0.X++;

    ref var c1 = ref view.Get<Velocity>(1, row);
    c1.X++;

    ref var c2 = ref view.Get<PlayerTag>(2, row);


    DEBUG.Both++;
}

static void PosAndTagComponentsSystem(in EcsView view, int row)
{

}


struct Likes { }
struct Dogs { }
struct Cats { }

struct Position { public float X, Y; }
struct Velocity { public float X, Y; }
record struct PlayerTag();

struct Relation<TAction, TTarget> 
    where TAction : struct 
    where TTarget : struct
{ }


unsafe struct Name { public fixed char Value[64]; public Velocity Vel; }

record struct ATestComp(bool X, float Y, float Z);
record struct ASecondTestComp(IntPtr x);

static class DEBUG
{
    public static int VelocityCount = 0, PositionCount = 0, Both = 0;
}