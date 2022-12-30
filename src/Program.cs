// https://github.com/jasonliang-dev/entity-component-system
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

var world = new World();

for (int i = 0; i < 1_000_000; ++i)
{
    var entity = world.CreateEntity();
    world.Attach<Position>(entity);
    world.Attach<Velocity>(entity);
}

var entity2 = world.CreateEntity();
world.Attach<Velocity>(entity2);
world.Attach<Position>(entity2);

var entity3 = world.CreateEntity();
world.Attach<Velocity>(entity3);

unsafe
{
    world.RegisterSystem<Velocity>(&VelocitySys);
}

while (true)
{
    world.Step();
}

Console.WriteLine("done");
Console.ReadLine();


static unsafe void VelocitySys(in EcsView view, int row)
{
    ref var c0 = ref Unsafe.AsRef<Velocity>((byte*)view.ComponentArrays[view.SignatureToIndex[0]] + (view.ComponentSizes[0] * row));
}

record struct Position(float X, float Y);
record struct Velocity(float X, float Y);
record struct PlayerTag();