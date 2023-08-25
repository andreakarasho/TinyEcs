namespace TinyEcs.Tests
{
    public class Relation
    {
		[Fact]
		public void Attach_ChildOf<TContext>()
		{
			using var world = new World<TContext>();

			var root = world.New();
			var child = world.New();

			child.ChildOf(root);

			Assert.True(world.Has<EcsChildOf>(child, root));
		}

		[Fact]
		public void Detach_ChildOf<TContext>()
		{
			using var world = new World<TContext>();

			var root = world.New();
			var child = world.New();

			child.ChildOf(root);
			child.Unset<EcsChildOf>(root);

			Assert.False(world.Has<EcsChildOf>(child, root));
		}

		[Fact]
		public void Count_Children<TContext>()
		{
			using var world = new World<TContext>();

			var root = world.New();

			var count = 100;
			for (int i = 0; i < count; ++i)
				world.New().ChildOf(root);

			var done = 0;
			root.Children(s => done += 1);

			Assert.Equal(count, done);
		}

		[Fact]
		public void Clear_Children<TContext>()
		{
			using var world = new World<TContext>();

			var root = world.New();

			var count = 100;
			for (int i = 0; i < count; ++i)
				world.New().ChildOf(root);

			root.ClearChildren();
			var done = 0;
			root.Children(s => done += 1);

			Assert.Equal(0, done);
		}

		[Fact]
		public void Exclusive_Relation<TContext>()
		{
			using var world = new World<TContext>();

			var root = world.New();
			var platoonCmp = world.New().Set<EcsExclusive>();
			var platoon1 = world.New();
			var platoon2 = world.New();
			var unit = world.New();

			unit.Set(platoonCmp, platoon1);
			Assert.True(world.Has(unit.ID, platoonCmp.ID, platoon1.ID));

			unit.Set(platoonCmp, platoon2);
			Assert.False(world.Has(unit.ID, platoonCmp.ID, platoon1.ID));
			Assert.True(world.Has(unit.ID, platoonCmp.ID, platoon2.ID));
		}
    }
}
