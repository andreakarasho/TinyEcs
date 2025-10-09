namespace TinyEcs.Tests
{
	public class ComponentOperationsTest
	{
		[Fact]
		public void Component_SetMultipleTimes()
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();

			e.Set(new FloatComponent { Value = 1.0f });
			Assert.Equal(1.0f, e.Get<FloatComponent>().Value);

			e.Set(new FloatComponent { Value = 2.0f });
			Assert.Equal(2.0f, e.Get<FloatComponent>().Value);

			e.Set(new FloatComponent { Value = 3.0f });
			Assert.Equal(3.0f, e.Get<FloatComponent>().Value);
		}

		[Fact]
		public void Component_AddRemoveCycle()
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();

			for (int i = 0; i < 10; i++)
			{
				e.Set(new FloatComponent());
				e.Set(new IntComponent());

				Assert.True(e.Has<FloatComponent>());
				Assert.True(e.Has<IntComponent>());

				e.Unset<FloatComponent>();
				e.Unset<IntComponent>();

				Assert.False(e.Has<FloatComponent>());
				Assert.False(e.Has<IntComponent>());
			}
		}

		[Fact]
		public void Component_UnsetNonExistent()
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();

			// Should not throw
			e.Unset<FloatComponent>();
			e.Unset<IntComponent>();
			e.Unset<BoolComponent>();

			Assert.False(e.Has<FloatComponent>());
		}

		[Fact]
		public void Component_HasDuringDeferred()
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();
			e.Set(new FloatComponent());

			ctx.World.BeginDeferred();

			// Should see existing component
			Assert.True(e.Has<FloatComponent>());

			// Should NOT see deferred component until after merge
			e.Set(new IntComponent());
			Assert.False(e.Has<IntComponent>());

			ctx.World.EndDeferred();

			// Now should see it
			Assert.True(e.Has<IntComponent>());
		}

		[Fact]
		public void Component_ModifyExistingInDeferred()
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();
			e.Set(new FloatComponent { Value = 1.0f });

			ctx.World.BeginDeferred();

			// Modify existing component
			e.Get<FloatComponent>().Value = 2.0f;
			Assert.Equal(2.0f, e.Get<FloatComponent>().Value);

			ctx.World.EndDeferred();

			// Should persist
			Assert.Equal(2.0f, e.Get<FloatComponent>().Value);
		}

		[Fact]
		public void Component_RapidTransitions()
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();

			e.Set(new FloatComponent());
			e.Set(new IntComponent());
			e.Unset<FloatComponent>();
			e.Set(new BoolComponent());
			e.Unset<IntComponent>();
			e.Set(new FloatComponent());
			e.Unset<BoolComponent>();

			Assert.True(e.Has<FloatComponent>());
			Assert.False(e.Has<IntComponent>());
			Assert.False(e.Has<BoolComponent>());
		}

		[Theory]
		[InlineData(5)]
		[InlineData(10)]
		[InlineData(20)]
		public void Component_AttachManySequentially(int count)
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();

			for (int i = 0; i < count; i++)
			{
				if (i % 4 == 0) e.Set(new FloatComponent { Value = i });
				if (i % 4 == 1) e.Set(new IntComponent { Value = i });
				if (i % 4 == 2) e.Set(new BoolComponent { Value = i % 2 == 0 });
				if (i % 4 == 3) e.Set<NormalTag>();
			}           // Verify final state based on count
			var hasFloat = count > 0;
			var hasInt = count > 1;
			var hasBool = count > 2;
			var hasTag = count > 3;

			Assert.Equal(hasFloat, e.Has<FloatComponent>());
			Assert.Equal(hasInt, e.Has<IntComponent>());
			Assert.Equal(hasBool, e.Has<BoolComponent>());
			Assert.Equal(hasTag, e.Has<NormalTag>());
		}

		[Fact]
		public void Component_DeferredUnset()
		{
			using var ctx = new Context();

			var e = ctx.World.Entity();
			e.Set(new FloatComponent());
			e.Set(new IntComponent());

			ctx.World.BeginDeferred();

			e.Unset<FloatComponent>();

			// Should still have it during deferred
			Assert.True(e.Has<FloatComponent>());

			ctx.World.EndDeferred();

			// Now it should be gone
			Assert.False(e.Has<FloatComponent>());
			Assert.True(e.Has<IntComponent>());
		}
	}
}
