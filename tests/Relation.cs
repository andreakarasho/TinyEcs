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

		[Fact]
		public void Count_Children()
		{
			using var world = new World();

			var root = world.Spawn();

			var count = 100;
			for (int i = 0; i < count; ++i)
				world.Spawn().ChildOf(root);

			var done = 0;
			root.Children(s => done += 1);

			Assert.Equal(count, done);
		}

		[Fact]
		public void Clear_Children()
		{
			using var world = new World();

			var root = world.Spawn();

			var count = 100;
			for (int i = 0; i < count; ++i)
				world.Spawn().ChildOf(root);

			root.ClearChildren();
			var done = 0;
			root.Children(s => done += 1);

			Assert.Equal(0, done);
		}
    }
}
