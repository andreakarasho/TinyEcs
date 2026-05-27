using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests;

public class FlushObserversDeadEntityTests
{
	private struct FlushFoo
	{
		public int Value;
	}

	[Fact]
	public void FlushObservers_DoesNotPanic_WhenEntityDeletedAfterSetQueued()
	{
		var app = new App();
		var world = app.GetWorld();
		world.EnableObservers<FlushFoo>();

		var entity = world.Entity();
		entity.Set(new FlushFoo { Value = 42 });

		world.Delete(entity.ID);

		var ex = Record.Exception(() => world.FlushObservers());
		Assert.Null(ex);
	}

	[Fact]
	public void FlushObservers_DoesNotPanic_WhenEntityDeletedBeforeOnAddFires()
	{
		var app = new App();
		var world = app.GetWorld();
		world.EnableObservers<FlushFoo>();

		var addFired = false;
		app.AddObserver<OnAdd<FlushFoo>>(trigger => addFired = true);

		var entity = world.Entity();
		entity.Set(new FlushFoo { Value = 1 });

		world.Delete(entity.ID);

		var ex = Record.Exception(() => world.FlushObservers());
		Assert.Null(ex);
		Assert.False(addFired);
	}

	[Fact]
	public void FlushObservers_SkipsDeadEntity_FiresLiveEntity()
	{
		var app = new App();
		var world = app.GetWorld();
		world.EnableObservers<FlushFoo>();

		var fireCount = 0;
		var observedValue = 0;
		app.AddObserver<OnInsert<FlushFoo>>(trigger =>
		{
			fireCount++;
			observedValue = trigger.Component.Value;
		});

		var dead = world.Entity();
		dead.Set(new FlushFoo { Value = 1 });

		var alive = world.Entity();
		alive.Set(new FlushFoo { Value = 99 });

		world.Delete(dead.ID);

		world.FlushObservers();

		Assert.Equal(1, fireCount);
		Assert.Equal(99, observedValue);
	}

	[Fact]
	public void FlushObservers_HandlesDeletedEntity_ViaDeferredCommands()
	{
		var app = new App();
		ulong spawnedId = 0;

		app.AddSystem((Commands commands) =>
		{
			var e = commands.Spawn().Insert(new FlushFoo { Value = 7 });
			spawnedId = e.Id;
			commands.Entity(e.Id).Despawn();
		})
		.InStage(Stage.Startup)
		.Build();

		var ex = Record.Exception(() => app.Run());
		Assert.Null(ex);
		Assert.NotEqual(0ul, spawnedId);
	}
}
