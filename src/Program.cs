// https://github.com/jasonliang-dev/entity-component-system
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

var entity3 = world.CreateEntity();
world.Attach<Velocity>(entity3);

Console.WriteLine("done");
Console.ReadLine();



record struct Position(float X, float Y);
record struct Velocity(float X, float Y);
record struct PlayerTag();