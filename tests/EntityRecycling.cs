namespace TinyEcs.Tests
{
	public class EntityRecyclingTest
	{
		[Fact]
		public void Entity_RecycleAfterDelete()
		{
			using var ctx = new Context();

			var e1 = ctx.World.Entity();
			var id1 = e1.ID;

			e1.Delete();
			Assert.False(e1.Exists());

			var e2 = ctx.World.Entity();
			var id2 = e2.ID;

			// Should recycle the same index but with incremented generation
			Assert.NotEqual(id1, id2);
			Assert.True(IDOp.GetGeneration(id2) > IDOp.GetGeneration(id1));
		}

		[Theory]
		[InlineData(10)]
		[InlineData(100)]
		public void Entity_MultipleRecycleCycles(int cycles)
		{
			using var ctx = new Context();

			ulong lastId = 0;

			for (int i = 0; i < cycles; i++)
			{
				var e = ctx.World.Entity();
				var id = e.ID;

				if (i > 0)
				{
					// Generation should increase
					Assert.True(IDOp.GetGeneration(id) >= IDOp.GetGeneration(lastId));
				}

				lastId = id;
				e.Delete();
			}
		}

		[Fact]
		public void Entity_OldIdNotValid()
		{
			using var ctx = new Context();

			var e1 = ctx.World.Entity();
			var oldId = e1.ID;

			e1.Delete();

			var e2 = ctx.World.Entity();

			// Old ID should not be valid
			Assert.False(ctx.World.Exists(oldId));
			Assert.True(ctx.World.Exists(e2.ID));
		}

		[Theory]
		[InlineData(100)]
		[InlineData(1000)]
		public void Entity_ManyCreateDeleteCycles(int count)
		{
			using var ctx = new Context();

			for (int i = 0; i < count; i++)
			{
				var entities = new List<EntityView>();

				// Create batch
				for (int j = 0; j < 10; j++)
				{
					entities.Add(ctx.World.Entity());
				}

				// Delete batch
				foreach (var e in entities)
				{
					e.Delete();
				}
			}

			// Should have recycled many times
			var finalEntity = ctx.World.Entity();
			Assert.True(IDOp.GetGeneration(finalEntity.ID) > 0);
		}

		[Fact]
		public void Entity_WithComponentsRecycle()
		{
			using var ctx = new Context();

			var e1 = ctx.World.Entity();
			e1.Set(new FloatComponent { Value = 123.0f });
			e1.Set(new IntComponent { Value = 456 });

			e1.Delete();

			var e2 = ctx.World.Entity();

			// New entity should not have old components
			Assert.False(e2.Has<FloatComponent>());
			Assert.False(e2.Has<IntComponent>());

			// Should be able to add components normally
			e2.Set(new BoolComponent { Value = true });
			Assert.True(e2.Has<BoolComponent>());
		}

		[Fact]
		public void Entity_LargeIdRecycling()
		{
			using var ctx = new Context();

			// Create entity with large ID
			var e1 = ctx.World.Entity(1000000);
			Assert.Equal(1000000ul, e1.ID);

			e1.Delete();

			// Recycle should work with large IDs
			var e2 = ctx.World.Entity();

			// Should recycle the large ID slot
			Assert.True(IDOp.GetGeneration(e2.ID) > 0);
		}
	}
}
