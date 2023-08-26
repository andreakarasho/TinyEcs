namespace TinyEcs.Tests
{
    public class Relation
    {
		[Fact]
		public void Attach_ChildOf()
		{
			using var world = new World();

			var root = world.Entity();
			var child = world.Entity();

			child.ChildOf(root);

			Assert.True(world.Has<EcsChildOf>(child, root));
		}

		[Fact]
		public void Detach_ChildOf()
		{
			using var world = new World();

			var root = world.Entity();
			var child = world.Entity();

			child.ChildOf(root);
			child.Unset<EcsChildOf>(root);

			Assert.False(world.Has<EcsChildOf>(child, root));
		}

		[Fact]
		public void Count_Children()
		{
			using var world = new World();

			var root = world.Entity();

			var count = 100;
			for (int i = 0; i < count; ++i)
				world.Entity().ChildOf(root);

			var done = 0;
			root.Children(s => done += 1);

			Assert.Equal(count, done);
		}

		[Fact]
		public void Clear_Children()
		{
			using var world = new World();

			var root = world.Entity();

			var count = 100;
			for (int i = 0; i < count; ++i)
				world.Entity().ChildOf(root);

			root.ClearChildren();
			var done = 0;
			root.Children(s => done += 1);

			Assert.Equal(0, done);
		}

		[Fact]
		public void Exclusive_Relation()
		{
			using var world = new World();

			var root = world.Entity();
			var platoonCmp = world.Entity().Set<EcsExclusive>();
			var platoon1 = world.Entity();
			var platoon2 = world.Entity();
			var unit = world.Entity();

			unit.Set(platoonCmp, platoon1);
			Assert.True(world.Has(unit.ID, platoonCmp.ID, platoon1.ID));

			unit.Set(platoonCmp, platoon2);
			Assert.False(world.Has(unit.ID, platoonCmp.ID, platoon1.ID));
			Assert.True(world.Has(unit.ID, platoonCmp.ID, platoon2.ID));
		}
    }
}
