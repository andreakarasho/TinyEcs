﻿// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 2;

using var world = new World();

var rnd = new Random();

var sw = Stopwatch.StartNew();



for (int i = 0; i < ENTITIES_COUNT; ++i)
{
    var entity = world.CreateEntity();
    world.Set<Position>(entity);
    world.Set<Velocity>(entity);
}

var list = new List<ulong>();

for (int i = 0; i < 100; ++i)
{
    var e = world.CreateEntity();
    world.Set<Position>(e);
    world.Set<Velocity>(e);
    world.Set<PlayerTag>(e);

    list.Add(e);
}

foreach (var e in list)
{
    world.DestroyEntity(e);
}

for (int i = 0; i < list.Count; ++i)
{
    var e = world.CreateEntity();
    Console.WriteLine(e);
    //world.Set<Position>(e);
    //world.Set<Velocity>(e);
    //world.Set<PlayerTag>(e);
}


var e2 = world.CreateEntity();
world.Set<Position>(e2);
world.Set<Velocity>(e2);
world.Set<int>(e2); 
world.Set<float>(e2);

var e3 = world.CreateEntity();
world.Set<Position>(e3);
world.Set<Velocity>(e3);
world.Set<int>(e3);
world.Set<float>(e3);
world.Set<PlayerTag>(e3);

//var plat = world.CreateEntity();
//world.Tag(e3, plat);

Console.WriteLine("entities created in {0} ms", sw.ElapsedMilliseconds);

var query = world.Query()
    .With<Position>()
    .With<Velocity>()
    //.WithTag(plat)
    //.WithTag(posC)
    //.Without<PlayerTag>()
    ;


var queryCmp = world.Query()
    .With<EcsComponent>();

foreach (var it in queryCmp)
{
    ref var p = ref it.Field<EcsComponent>();

    for (var row = 0; row < it.Count; ++row)
    {
        ref readonly var entity = ref it.Entity(row);
        ref var pos = ref it.Get(ref p, row);

        Console.WriteLine("Component {{ ID = {0}, GlobalID: {1}, Name = {2}, Size = {3} }}", entity, pos.GlobalIndex, pos.Name.ToString(), pos.Size);
    }
}

unsafe
{
    world.RegisterSystem(query, &ASystem);
    world.RegisterSystem(query, &PreUpdate, SystemPhase.OnPreUpdate);
    world.RegisterSystem(query, &PostUpdate, SystemPhase.OnPostUpdate);
}


while (true)
{
    sw.Restart();
    
    for (int i = 0; i < 3600; ++i)
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

    ref var p = ref it.Field<Position>();
    ref var v = ref it.Field<Velocity>();

    for (var row = 0; row < it.Count; ++row)
    {
        ref readonly var entity = ref it.Entity(row);
        ref var pos = ref it.Get(ref p, row);
        ref var vel = ref it.Get(ref v, row);
    }
}

static void ASystem2(in Iterator it)
{
    //Console.WriteLine("ASystem2 - Count: {0}", it.Count);

    ref var p = ref it.Field<Position>();
    ref var v = ref it.Field<Velocity>();

    for (var row = 0; row < it.Count; ++row)
    {
        ref readonly var entity = ref it.Entity(row);
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
}

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