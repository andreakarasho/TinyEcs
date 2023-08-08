namespace TinyEcs.Tests
{
    public class Relation
    {
		[Fact]
		public void Attach_ChildOf()
		{
			using var world = new World();

			var root = world.Spawn();
			var child = world.Spawn();

			child.ChildOf(root);

			Assert.True(world.Has<EcsChild>(child, root));
		}

		[Fact]
		public void Detach_ChildOf()
		{
			using var world = new World();

			var root = world.Spawn();
			var child = world.Spawn();

			child.ChildOf(root);
			child.Unset<EcsChild>(root);

			Assert.False(world.Has<EcsChild>(child, root));
		}
    }
}
