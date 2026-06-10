namespace TinyEcs.Tests
{
	// Reparent / unlink must be detach-only: the moved entity's own subtree
	// stays linked and alive. CleanupPolicy.DeleteDescendants applies only to
	// real entity deletion. Regression for the bug where re-AddChild'ing an
	// already-parented entity (or world.RemoveChild on it) cascaded
	// World.Delete into its children.
	public class EntityMapperReparentTest
	{
		[Fact]
		public void ReAddChildToSameParentKeepsGrandchildren()
		{
			using var ctx = new Context();
			var world = ctx.World;

			var root = world.Entity();
			var mid = world.Entity();
			var leaf = world.Entity();

			world.AddChild(root.ID, mid.ID);
			world.AddChild(mid.ID, leaf.ID);

			world.AddChild(root.ID, mid.ID);

			Assert.True(world.Exists(mid.ID));
			Assert.True(world.Exists(leaf.ID));
			Assert.Equal(root.ID, world.GetParent(mid.ID));
			Assert.Equal(mid.ID, world.GetParent(leaf.ID));
		}

		[Fact]
		public void ReparentToNewParentKeepsSubtree()
		{
			using var ctx = new Context();
			var world = ctx.World;

			var oldParent = world.Entity();
			var newParent = world.Entity();
			var mid = world.Entity();
			var leaf = world.Entity();

			world.AddChild(oldParent.ID, mid.ID);
			world.AddChild(mid.ID, leaf.ID);

			world.AddChild(newParent.ID, mid.ID);

			Assert.True(world.Exists(leaf.ID));
			Assert.Equal(newParent.ID, world.GetParent(mid.ID));
			Assert.Equal(mid.ID, world.GetParent(leaf.ID));
			Assert.False(world.Has<Children>(oldParent.ID));
		}

		[Fact]
		public void RemoveChildUnlinkKeepsChildren()
		{
			using var ctx = new Context();
			var world = ctx.World;

			var root = world.Entity();
			var mid = world.Entity();
			var leaf = world.Entity();

			world.AddChild(root.ID, mid.ID);
			world.AddChild(mid.ID, leaf.ID);

			world.RemoveChild(mid.ID);

			Assert.True(world.Exists(mid.ID));
			Assert.True(world.Exists(leaf.ID));
			Assert.Equal(0ul, world.GetParent(mid.ID));
			Assert.False(world.Has<Parent>(mid.ID));
			Assert.Equal(mid.ID, world.GetParent(leaf.ID));
		}

		[Fact]
		public void DeleteStillCascadesToDescendants()
		{
			using var ctx = new Context();
			var world = ctx.World;

			var root = world.Entity();
			var mid = world.Entity();
			var leaf = world.Entity();

			world.AddChild(root.ID, mid.ID);
			world.AddChild(mid.ID, leaf.ID);

			root.Delete();

			Assert.False(world.Exists(root.ID));
			Assert.False(world.Exists(mid.ID));
			Assert.False(world.Exists(leaf.ID));
		}
	}
}
