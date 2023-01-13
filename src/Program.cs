// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 1;

using var world = new World();

var rnd = new Random();


for (int i = 0; i < ENTITIES_COUNT; ++i)
{
    var entity = world.CreateEntity();
    world.Attach<Position>(entity);
    world.Attach<Velocity>(entity);
}


var query = world.Query()
    .With<Position>()
    .With<Velocity>()
    //.WithTag(plat)
    //.WithTag(posC)
    //.Without<PlayerTag>()
    ;



var sw = Stopwatch.StartNew();

while (true)
{
    sw.Restart();
    //for (int i = 0; i < 3600; ++i)
    //{
    //    foreach (var view in query)
    //    {
    //        var p = view.Field<Position>();
    //        var v = view.Field<Velocity>();

    //        for (int row = 0, count = view.Count; row < count; ++row)
    //        {
    //            ref readonly var entity = ref view.Entity(row);
    //            ref var pos = ref p[row];
    //            ref var vel = ref v[row];
    //        }
    //    }
    //}

    Console.WriteLine(sw.ElapsedMilliseconds);


    sw.Restart();
    for (int i = 0; i < 3600; ++i)
    {
        foreach (var it in query)
        {
            ref var p = ref it.Field2<Position>();
            ref var v = ref it.Field2<Velocity>();

            for (var row = 0; row < it.Count; ++row)
            {
                ref readonly var entity = ref it.Entity(row);
                ref var pos = ref it.Get(ref p, row);
                ref var vel = ref it.Get(ref v, row);
            }

            //foreach (var view in it)
            //{
            //    ref readonly var entity = ref it.Entity;
            //    ref var pos = ref view.Get(ref p);
            //    ref var vel = ref view.Get(ref v);
            //}

            //for (int row = 0, count = view.Count; row < count; ++row)
            //{
            //    ref readonly var entity = ref view.Entity(row);
            //    ref var pos = ref view.Get(ref p, row);
            //    ref var vel = ref view.Get(ref v, row);
            //}

        }
    }

    Console.WriteLine(sw.ElapsedMilliseconds);
}

Console.ReadLine();



struct Likes { }
struct Dogs { }
struct Cats { }

struct Position { public float X, Y; }
struct Velocity { public float X, Y; }
record struct PlayerTag();
struct ReflectedPosition { public float X, Y; }

struct Relation<TAction, TTarget> 
    where TAction : struct 
    where TTarget : struct
{ }


record struct ATestComp(bool X, float Y, float Z);
record struct ASecondTestComp(IntPtr x);

static class DEBUG
{
    public static int VelocityCount = 0, PositionCount = 0, Both = 0;
}