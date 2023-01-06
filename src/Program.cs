// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

const int ENTITIES_COUNT = 1_000_000 * 1;

var world = new World();


world.RegisterComponent<Position>();
world.RegisterComponent<Velocity>();
world.RegisterComponent<PlayerTag>();
world.RegisterComponent<ATestComp>();
world.RegisterComponent<ASecondTestComp>();

var bothCount = 0;
var velocicyCount = 0;
var positionCount = 0;

var rnd = new Random();

for (int i = 0; i < ENTITIES_COUNT; ++i)
{
    var entity = world.CreateEntity();
    //world.Attach(entity, positionID);
    //world.Attach(entity, velocityID);

    world.Attach<Position>(entity);
    world.Attach<Velocity>(entity);
    //world.Attach<PlayerTag>(entity);

    world.Set(entity, new Position() { X = 200f });
    ref var p = ref world.Get<Position>(entity);

    if (rnd.Next() % 3 == 0)
    {
        world.Destroy(entity);
    }

    bothCount++;
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

var sw = Stopwatch.StartNew();
while (true)
{
    DEBUG.VelocityCount = 0;
    DEBUG.PositionCount = 0;
    DEBUG.Both = 0;

    sw.Restart();

    world.Step();

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

record struct Position(float X, float Y);
record struct Velocity(float X, float Y);
record struct PlayerTag();

record struct ATestComp(bool X, float Y, float Z);
record struct ASecondTestComp(IntPtr x);

static class DEBUG
{
    public static int VelocityCount = 0, PositionCount = 0, Both = 0;
}