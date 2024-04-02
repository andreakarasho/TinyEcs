namespace TinyEcs.Tests
{
    public class DeferredTest
    {
		[Fact]
		public void Deferred_CheckWorldState()
		{
			using var ctx = new Context();
			ctx.World.BeginDeferred();
			Assert.True(ctx.World.IsDeferred);
			ctx.World.EndDeferred();
			Assert.False(ctx.World.IsDeferred);
		}

        [Theory]
        [InlineData(1_000)]
        [InlineData(1_000_00)]
        [InlineData(1_000_000)]
        public void Deferred_NewEntity(int amount)
        {
            using var ctx = new Context();

			var count = ctx.World.EntityCount;
			var expected = count + amount * 2;

			var t0 = new Thread(() => { for (var i = 0; i < amount; ++i) ctx.World.Entity(); })
			{ IsBackground = true };
			var t1 = new Thread(() => { for (var i = 0; i < amount; ++i) ctx.World.Entity(); })
			{ IsBackground = true };

			ctx.World.BeginDeferred();
			t0.Start();
			t1.Start();

			t0.Join();
			t1.Join();
			ctx.World.EndDeferred();

            Assert.Equal(expected, ctx.World.EntityCount);
        }

		[Fact]
		public void Deferred_DeleteEntity()
		{
			using var ctx = new Context();
			var count = ctx.World.EntityCount;

			var list = new List<EntityView>();
			for (var i = 0; i < 100; ++i)
				list.Add(ctx.World.Entity());

			ctx.World.BeginDeferred();
			list.ForEach(s => s.Delete());
			ctx.World.EndDeferred();

			Assert.Equal(count, ctx.World.EntityCount);
		}

		[Fact]
		public void Deferred_SetComponent()
		{
			using var ctx = new Context();

			var entity = ctx.World.Entity();

			ctx.World.BeginDeferred();
			entity.Set(new FloatComponent() { Value = 9f });
			entity.Set(new IntComponent() { Value = 123 });
			entity.Set<NormalTag>();
			ctx.World.EndDeferred();

			Assert.True(entity.Has<FloatComponent>());
			Assert.True(entity.Has<IntComponent>());
			Assert.True(entity.Has<NormalTag>());

			Assert.True(entity.Get<FloatComponent>().Value.Equals(9f));
			Assert.True(entity.Get<IntComponent>().Value.Equals(123));
		}

		[Fact]
		public void Deferred_UnsetComponent()
		{
			using var ctx = new Context();

			var entity = ctx.World.Entity();
			entity.Set(new FloatComponent() { Value = 9f });

			ctx.World.BeginDeferred();
			entity.Set(new IntComponent() { Value = 123 });
			entity.Set<NormalTag>();

			entity.Unset<FloatComponent>();
			entity.Unset<IntComponent>();
			ctx.World.EndDeferred();

			entity.Unset<NormalTag>();

			Assert.False(entity.Has<FloatComponent>());
			Assert.False(entity.Has<IntComponent>());
			Assert.False(entity.Has<NormalTag>());
		}

		[Fact]
		public void Deferred_GetComponent()
		{
			using var ctx = new Context();

			var entity = ctx.World.Entity();

			ctx.World.BeginDeferred();
			entity.Set(new FloatComponent() { Value = 9f });
			entity.Set(new IntComponent() { Value = 123 });
			entity.Set<NormalTag>();

			entity.Get<FloatComponent>().Value += 1f;
			entity.Get<IntComponent>().Value += 1;
			ctx.World.EndDeferred();

			Assert.True(entity.Has<FloatComponent>());
			Assert.True(entity.Has<IntComponent>());
			Assert.True(entity.Has<NormalTag>());

			Assert.True(entity.Get<FloatComponent>().Value.Equals(9f + 1f));
			Assert.True(entity.Get<IntComponent>().Value.Equals(123 + 1));
		}

		[Fact]
		public void Deferred_Nested_DeferredOp()
		{
			using var ctx = new Context();

			var entity = ctx.World.Entity();

			ctx.World.BeginDeferred();
			{
				Assert.True(ctx.World.IsDeferred);
				entity.Set(new FloatComponent() { Value = 9f });

				ctx.World.BeginDeferred();
				{
					Assert.True(ctx.World.IsDeferred);
					entity.Set(new IntComponent() { Value = 123 });
					entity.Set<NormalTag>();

					ctx.World.BeginDeferred();
					{
						Assert.True(ctx.World.IsDeferred);
						entity.Get<FloatComponent>().Value += 1f;
						entity.Get<IntComponent>().Value += 1;
					}
					ctx.World.EndDeferred();
					Assert.True(ctx.World.IsDeferred);
				}
				ctx.World.EndDeferred();
				Assert.True(ctx.World.IsDeferred);

				entity.Get<FloatComponent>().Value += 1f;
				entity.Get<IntComponent>().Value += 1;
			}
			ctx.World.EndDeferred();
			Assert.False(ctx.World.IsDeferred);


			Assert.True(entity.Has<FloatComponent>());
			Assert.True(entity.Has<IntComponent>());
			Assert.True(entity.Has<NormalTag>());

			Assert.True(entity.Get<FloatComponent>().Value.Equals(9f + 1f + 1f));
			Assert.True(entity.Get<IntComponent>().Value.Equals(123 + 1 + 1));
		}
    }
}
