using Xunit;
using TinyEcs.Bevy;

namespace TinyEcs.Tests;

public class BubblingTests
{
	public struct BubbleEvent { public int Payload; }

	[Fact]
	public void Diagnostic_ParentComponentSetAfterAddChild()
	{
		using var world = new World();
		var app = new App(world);
		ulong rootId = 0, leafId = 0;

		app.AddSystem((Commands cmd) =>
		{
			var root = cmd.Spawn();
			var leaf = cmd.Spawn();
			root.AddChild(leaf);
			rootId = root.Id;
			leafId = leaf.Id;
		}).InStage(Stage.Startup).Build();

		app.Run();

		Assert.True(world.Has<Parent>(leafId), "leaf must have Parent component after AddChild");
		Assert.Equal(rootId, world.Get<Parent>(leafId).Id);
	}

	[Fact]
	public void Emit_FiresOnTargetFirst_ThenWalksParentsLeafToRoot()
	{
		var app = new App();
		var order = new List<ulong>();
		ulong rootId = 0, midId = 0, leafId = 0;

		app.AddSystem((Commands cmd) =>
		{
			var root = cmd.Spawn();
			var mid = cmd.Spawn();
			var leaf = cmd.Spawn();
			root.AddChild(mid);
			mid.AddChild(leaf);

			rootId = root.Id;
			midId = mid.Id;
			leafId = leaf.Id;

			root.Observe<On<BubbleEvent>>(t => order.Add(t.CurrentEntityId));
			mid.Observe<On<BubbleEvent>>(t => order.Add(t.CurrentEntityId));
			leaf.Observe<On<BubbleEvent>>(t => order.Add(t.CurrentEntityId));
		}).InStage(Stage.Startup).Label("setup").Build();

		app.AddSystem((Commands cmd) =>
		{
			cmd.Entity(leafId).EmitTrigger(new BubbleEvent { Payload = 42 });
		}).InStage(Stage.Startup).After("setup").Build();

		app.Run();

		Assert.Equal(new[] { leafId, midId, rootId }, order);
	}

	[Fact]
	public void Emit_StopsBubbling_WhenObserverCallsPropagateFalse()
	{
		var app = new App();
		bool leafFired = false, midFired = false, rootFired = false;
		ulong leafId = 0;

		app.AddSystem((Commands cmd) =>
		{
			var root = cmd.Spawn();
			var mid = cmd.Spawn();
			var leaf = cmd.Spawn();
			root.AddChild(mid);
			mid.AddChild(leaf);

			leafId = leaf.Id;

			root.Observe<On<BubbleEvent>>(t => rootFired = true);
			mid.Observe<On<BubbleEvent>>(t =>
			{
				midFired = true;
				t.Propagate(false);
			});
			leaf.Observe<On<BubbleEvent>>(t => leafFired = true);
		}).InStage(Stage.Startup).Label("setup").Build();

		app.AddSystem((Commands cmd) =>
		{
			cmd.Entity(leafId).EmitTrigger(new BubbleEvent());
		}).InStage(Stage.Startup).After("setup").Build();

		app.Run();

		Assert.True(leafFired, "leaf observer should fire");
		Assert.True(midFired, "mid observer should fire and stop bubble");
		Assert.False(rootFired, "root must not fire after mid stopped propagation");
	}

	[Fact]
	public void Emit_TargetEntityIdStaysSame_CurrentEntityIdChangesAlongPath()
	{
		var app = new App();
		var targets = new List<ulong>();
		var currents = new List<ulong>();
		ulong rootId = 0, midId = 0, leafId = 0;

		app.AddSystem((Commands cmd) =>
		{
			var root = cmd.Spawn();
			var mid = cmd.Spawn();
			var leaf = cmd.Spawn();
			root.AddChild(mid);
			mid.AddChild(leaf);

			rootId = root.Id;
			midId = mid.Id;
			leafId = leaf.Id;

			Action<On<BubbleEvent>> capture = t =>
			{
				targets.Add(t.EntityId);
				currents.Add(t.CurrentEntityId);
			};

			root.Observe<On<BubbleEvent>>(capture);
			mid.Observe<On<BubbleEvent>>(capture);
			leaf.Observe<On<BubbleEvent>>(capture);
		}).InStage(Stage.Startup).Label("setup").Build();

		app.AddSystem((Commands cmd) =>
		{
			cmd.Entity(leafId).EmitTrigger(new BubbleEvent());
		}).InStage(Stage.Startup).After("setup").Build();

		app.Run();

		Assert.Equal(new[] { leafId, leafId, leafId }, targets);
		Assert.Equal(new[] { leafId, midId, rootId }, currents);
	}

	[Fact]
	public void Emit_GlobalObserverFiresOnce_RegardlessOfBubbleStop()
	{
		var app = new App();
		int globalCount = 0;
		ulong leafId = 0;

		app.AddObserver<On<BubbleEvent>>((w, t) => globalCount++);

		app.AddSystem((Commands cmd) =>
		{
			var root = cmd.Spawn();
			var leaf = cmd.Spawn();
			root.AddChild(leaf);
			leafId = leaf.Id;

			leaf.Observe<On<BubbleEvent>>(t => t.Propagate(false));
		}).InStage(Stage.Startup).Label("setup").Build();

		app.AddSystem((Commands cmd) =>
		{
			cmd.Entity(leafId).EmitTrigger(new BubbleEvent());
		}).InStage(Stage.Startup).After("setup").Build();

		app.Run();

		Assert.Equal(1, globalCount);
	}

	[Fact]
	public void Emit_OrphanEntity_FiresOnlyOnTarget()
	{
		var app = new App();
		int fired = 0;
		ulong eId = 0;

		app.AddSystem((Commands cmd) =>
		{
			var e = cmd.Spawn();
			eId = e.Id;
			e.Observe<On<BubbleEvent>>(t => fired++);
		}).InStage(Stage.Startup).Label("setup").Build();

		app.AddSystem((Commands cmd) =>
		{
			cmd.Entity(eId).EmitTrigger(new BubbleEvent());
		}).InStage(Stage.Startup).After("setup").Build();

		app.Run();

		Assert.Equal(1, fired);
	}

	[Fact]
	public void Emit_DeepHierarchy_BubblesAllLevels()
	{
		var app = new App();
		var order = new List<ulong>();
		var ids = new ulong[5];

		app.AddSystem((Commands cmd) =>
		{
			ulong prevId = 0;
			for (int i = 0; i < 5; i++)
			{
				var e = cmd.Spawn();
				ids[i] = e.Id;
				if (i > 0) cmd.AddChild(prevId, e.Id);
				prevId = e.Id;

				int idx = i;
				e.Observe<On<BubbleEvent>>(t => order.Add(t.CurrentEntityId));
			}
		}).InStage(Stage.Startup).Label("setup").Build();

		app.AddSystem((Commands cmd) =>
		{
			cmd.Entity(ids[4]).EmitTrigger(new BubbleEvent());
		}).InStage(Stage.Startup).After("setup").Build();

		app.Run();

		Assert.Equal(new[] { ids[4], ids[3], ids[2], ids[1], ids[0] }, order);
	}

	[Fact]
	public void Emit_IntermediateAncestorWithoutObserver_StillBubblesPastIt()
	{
		var app = new App();
		bool rootFired = false, leafFired = false;
		ulong leafId = 0;

		app.AddSystem((Commands cmd) =>
		{
			var root = cmd.Spawn();
			var mid = cmd.Spawn();   // no observer attached
			var leaf = cmd.Spawn();
			root.AddChild(mid);
			mid.AddChild(leaf);

			leafId = leaf.Id;

			root.Observe<On<BubbleEvent>>(t => rootFired = true);
			leaf.Observe<On<BubbleEvent>>(t => leafFired = true);
		}).InStage(Stage.Startup).Label("setup").Build();

		app.AddSystem((Commands cmd) =>
		{
			cmd.Entity(leafId).EmitTrigger(new BubbleEvent());
		}).InStage(Stage.Startup).After("setup").Build();

		app.Run();

		Assert.True(leafFired);
		Assert.True(rootFired);
	}

	[Fact]
	public void Emit_AtRootTarget_NoParentWalk()
	{
		var app = new App();
		int rootCount = 0;
		ulong rootId = 0;

		app.AddSystem((Commands cmd) =>
		{
			var root = cmd.Spawn();
			rootId = root.Id;
			root.Observe<On<BubbleEvent>>(t => rootCount++);
		}).InStage(Stage.Startup).Label("setup").Build();

		app.AddSystem((Commands cmd) =>
		{
			cmd.Entity(rootId).EmitTrigger(new BubbleEvent());
		}).InStage(Stage.Startup).After("setup").Build();

		app.Run();

		Assert.Equal(1, rootCount);
	}

	[Fact]
	public void Emit_ChildStopsPropagation_ParentObserverNeverInvoked()
	{
		var app = new App();
		ulong observedCurrent = 0;
		ulong leafId = 0;

		app.AddSystem((Commands cmd) =>
		{
			var parent = cmd.Spawn();
			var child = cmd.Spawn();
			parent.AddChild(child);
			leafId = child.Id;

			parent.Observe<On<BubbleEvent>>(t => observedCurrent = t.CurrentEntityId);
			child.Observe<On<BubbleEvent>>(t => t.Propagate(false));
		}).InStage(Stage.Startup).Label("setup").Build();

		app.AddSystem((Commands cmd) =>
		{
			cmd.Entity(leafId).EmitTrigger(new BubbleEvent());
		}).InStage(Stage.Startup).After("setup").Build();

		app.Run();

		Assert.Equal(0ul, observedCurrent);
	}
}
