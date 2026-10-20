using System.Collections.Generic;
using TinyEcs;
using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests;

// Bevy fires hooks / observers for zero-size components; so must we. A tag has
// no column, so its triggers carry default(T).
public class TagObserverTests
{
	private struct Marker;

	[Fact]
	public void OnAdd_OnInsert_OnRemove_fire_for_a_tag()
	{
		using var world = new World();
		var app = new App(world);
		var log = new List<string>();
		app.AddObserver<OnAdd<Marker>>(t => log.Add($"add {t.EntityId}"));
		app.AddObserver<OnInsert<Marker>>(t => log.Add($"insert {t.EntityId}"));
		app.AddObserver<OnRemove<Marker>>(t => log.Add($"remove {t.EntityId}"));

		var e = world.Entity();
		e.Set(new Marker());
		world.FlushObservers();
		Assert.Equal(new[] { $"add {e.ID}", $"insert {e.ID}" }, log);

		log.Clear();
		e.Set(new Marker()); // already there: an insert, not an add
		world.FlushObservers();
		Assert.Equal(new[] { $"insert {e.ID}" }, log);

		log.Clear();
		e.Unset<Marker>();
		Assert.Equal(new[] { $"remove {e.ID}" }, log);
	}

	[Fact]
	public void Tag_inserted_through_commands_fires_OnAdd_in_the_flush()
	{
		using var world = new World();
		var app = new App(world, ThreadingMode.Single);
		var added = new List<ulong>();
		app.AddObserver<OnAdd<Marker>>(t => added.Add(t.EntityId));
		ulong id = 0;
		app.AddSystem((Commands c) => { id = c.Spawn().Insert(new Marker()).Id; })
			.InStage(Stage.Startup).Build();
		app.RunStartup();
		Assert.Equal(new[] { id }, added);
	}

	[Fact]
	public void Despawning_a_tagged_entity_fires_OnRemove()
	{
		using var world = new World();
		var app = new App(world);
		var removed = 0;
		app.AddObserver<OnRemove<Marker>>(_ => removed++);
		var e = world.Entity();
		e.Set(new Marker());
		world.FlushObservers();
		e.Delete();
		Assert.Equal(1, removed);
	}
}
