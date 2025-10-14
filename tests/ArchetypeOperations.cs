namespace TinyEcs.Tests
{
	public class ArchetypeOperationsTest
	{
		[Fact]
		public void Archetype_SameComponentsShareArchetype()
		{
			using var ctx = new Context();

			// Create many entities with same components - should reuse archetype
			for (int i = 0; i < 100; i++)
			{
				var e = ctx.World.Entity();
				e.Set(new FloatComponent());
				e.Set(new IntComponent());
				e.Set(new BoolComponent());

				Assert.True(e.Has<FloatComponent>());
				Assert.True(e.Has<IntComponent>());
				Assert.True(e.Has<BoolComponent>());
			}

			// Query should find all
			var count = ctx.World.QueryBuilder()
				.With<FloatComponent>()
				.With<IntComponent>()
				.With<BoolComponent>()
				.Build()
				.Count();

			Assert.Equal(100, count);
		}

		[Fact]
		public void Archetype_TransitionCycle()
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();
			e.Set(new FloatComponent());

			Assert.True(e.Has<FloatComponent>());
			Assert.False(e.Has<IntComponent>());

			e.Set(new IntComponent());
			Assert.True(e.Has<FloatComponent>());
			Assert.True(e.Has<IntComponent>());

			e.Unset<IntComponent>();
			Assert.True(e.Has<FloatComponent>());
			Assert.False(e.Has<IntComponent>());

			// Should be able to re-add
			e.Set(new IntComponent());
			Assert.True(e.Has<FloatComponent>());
			Assert.True(e.Has<IntComponent>());
		}

		[Theory]
		[InlineData(10)]
		[InlineData(50)]
		public void Archetype_MultipleTransitions(int transitions)
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();

			for (int i = 0; i < transitions; i++)
			{
				e.Set(new FloatComponent { Value = i });
				e.Set(new IntComponent { Value = i * 2 });
				Assert.True(e.Has<FloatComponent>());
				Assert.True(e.Has<IntComponent>());

				e.Unset<FloatComponent>();
				Assert.False(e.Has<FloatComponent>());
				Assert.True(e.Has<IntComponent>());

				e.Unset<IntComponent>();
				Assert.False(e.Has<FloatComponent>());
				Assert.False(e.Has<IntComponent>());
			}
		}

		[Fact]
		public void Archetype_ComplexGraph()
		{
			using var ctx = new Context();

			// Create entities with overlapping components
			var e1 = ctx.World.Entity();
			e1.Set(new FloatComponent());
			e1.Set(new IntComponent());

			var e2 = ctx.World.Entity();
			e2.Set(new FloatComponent());
			e2.Set(new BoolComponent());

			var e3 = ctx.World.Entity();
			e3.Set(new IntComponent());
			e3.Set(new BoolComponent());

			// Verify each has correct components
			Assert.True(e1.Has<FloatComponent>() && e1.Has<IntComponent>());
			Assert.True(e2.Has<FloatComponent>() && e2.Has<BoolComponent>());
			Assert.True(e3.Has<IntComponent>() && e3.Has<BoolComponent>());

			// Transition e3 to match e1's archetype
			e3.Set(new FloatComponent());
			e3.Unset<BoolComponent>();

			Assert.True(e3.Has<FloatComponent>() && e3.Has<IntComponent>());
		}
	}
}
