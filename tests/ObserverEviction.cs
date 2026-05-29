using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests;

public class ObserverEvictionTests
{
	private struct EvFoo
	{
		public int Value;
	}

	[Fact]
	public void OnDisposed_FiresOnWorldDispose()
	{
		var world = new World();
		var fired = 0;
		World? observed = null;
		world.OnDisposed += w => { fired++; observed = w; };

		world.Dispose();

		Assert.Equal(1, fired);
		Assert.Same(world, observed);
	}

	[Fact]
	public void ObserverState_EvictedOnWorldDispose()
	{
		var world = new World();
		world.EnableObservers<EvFoo>();

		// Touch the observer state so it is registered in the static map.
		Assert.True(world.HasObserverState());

		world.Dispose();

		Assert.False(world.HasObserverState());
	}

	[Fact]
	public void EntityObservers_EvictedOnEntityDelete()
	{
		var app = new App();
		var world = app.GetWorld();
		ulong id = 0;

		app.AddSystem((Commands commands) =>
		{
			var e = commands.Spawn()
				.Observe<OnInsert<EvFoo>>(_ => { })
				.Insert(new EvFoo { Value = 1 });
			id = e.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();

		var state = world.GetObserverState();
		Assert.True(state.EntityObservers.ContainsKey(id));

		world.Delete(id);

		Assert.False(state.EntityObservers.ContainsKey(id));
	}

	[Fact]
	public void EntityObservers_EvictedViaDespawnCommand()
	{
		var app = new App();
		var world = app.GetWorld();
		ulong id = 0;

		app.AddSystem((Commands commands) =>
		{
			var e = commands.Spawn()
				.Observe<OnInsert<EvFoo>>(_ => { })
				.Insert(new EvFoo { Value = 7 });
			id = e.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();

		var state = world.GetObserverState();
		Assert.True(state.EntityObservers.ContainsKey(id));

		app.AddSystem((Commands commands) => commands.Entity(id).Despawn())
			.InStage(Stage.Update)
			.Build();

		app.Update();

		Assert.False(state.EntityObservers.ContainsKey(id));
	}
}
