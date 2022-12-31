// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

var world = new World();

var positionID = world.RegisterComponent<Position>();
var velocityID = world.RegisterComponent<Velocity>();

for (int i = 0; i < 1_000_000; ++i)
{
    var entity = world.CreateEntity();
    world.Attach<Position>(entity);
    world.Attach<Velocity>(entity);

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

//var e = world.CreateEntity();
//world.Attach(e, positionID);


unsafe
{
    //world.RegisterSystem(&VelocitySys, positionID, velocityID);

    //world.RegisterSystem<Position, Velocity>(&TwoComponentsSystem);
    //world.RegisterSystem<Position>(&PositionOnlySystem);
    world.RegisterSystem<Velocity>(&VelocityOnlySystem);
}

var sw = Stopwatch.StartNew();
while (true)
{
    sw.Restart();

    world.Step();

    //world.Attach(world.CreateEntity(), positionID);
    Console.WriteLine(sw.ElapsedMilliseconds);
}

Console.WriteLine("done");
Console.ReadLine();


static void VelocityOnlySystem(in EcsView view, int row)
{
    ref var c0 = ref view.Get<Velocity>(0, row);
    c0.X++;
}

static void PositionOnlySystem(in EcsView view, int row)
{
    ref var c0 = ref view.Get<Position>(0, row);
    c0.X++;
}

static void TwoComponentsSystem(in EcsView view, int row)
{

}

record struct Position(float X, float Y);
record struct Velocity(float X, float Y);
record struct PlayerTag();