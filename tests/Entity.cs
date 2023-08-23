using System.Numerics;
using System.Runtime.InteropServices;

namespace TinyEcs.Tests
{
    public class EntityTest
    {
        [Fact]
        public void Entity_Creation<TContext>()
        {
            using var world = new World<TContext>();
            var entity = world.Spawn();

            Assert.True(entity.Exists());
        }

        [Fact]
        public void Entity_Deletion<TContext>()
        {
            using var world = new World<TContext>();

            var e1 = world.Spawn();
            var e2 = world.Spawn();
            var e3 = world.Spawn();
            var e4 = world.Spawn();

            e2.Despawn();
            e2 = world.Spawn();

            e3.Despawn();
            e3 = world.Spawn();

            e2.Despawn();
            e2 = world.Spawn();

            Assert.True(e1.Exists());
            Assert.True(e2.Exists());
            Assert.True(e3.Exists());
            Assert.True(e4.Exists());
        }

        [Fact]
        public void Entity_Enable<TContext>()
        {
            using var world = new World<TContext>();

            var entity = world.Spawn();
            entity.Enable();

            Assert.True(entity.IsEnabled());
        }

        [Fact]
        public void Entity_Disabled<TContext>()
        {
            using var world = new World<TContext>();

            var entity = world.Spawn();
            entity.Disable();

            Assert.False(entity.IsEnabled());
        }

        [Fact]
        public void Entity_Attach_TwoSameComponent<TContext>()
        {
            using var world = new World<TContext>();
            var entity = world.Spawn();

            world.Set<FloatComponent>(entity);
            world.Set<FloatComponent>(entity);
            world.Unset<FloatComponent>(entity);

            Assert.False(world.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Attach_OneComponent<TContext>()
        {
            using var world = new World<TContext>();
            var entity = world.Spawn();

            world.Set<FloatComponent>(entity);

            Assert.True(world.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Attach_TwoComponent<TContext>()
        {
            using var world = new World<TContext>();
            var entity = world.Spawn();

            world.Set<FloatComponent>(entity);
            world.Set<NormalTag>(entity);

            Assert.True(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<NormalTag>(entity));
        }

        [Fact]
        public void Entity_Attach_ThreeComponent<TContext>()
        {
            using var world = new World<TContext>();
            var entity = world.Spawn();

            world.Set<FloatComponent>(entity);
            world.Set<NormalTag>(entity);
            world.Set<LargeComponent>(entity);

            Assert.True(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<NormalTag>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_OneComponent<TContext>()
        {
            using var world = new World<TContext>();
            var entity = world.Spawn();

            world.Set<FloatComponent>(entity);
            world.Unset<FloatComponent>(entity);

            Assert.True(!world.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_TwoComponent<TContext>()
        {
            using var world = new World<TContext>();
            var entity = world.Spawn();

            world.Set<FloatComponent>(entity);
            world.Set<NormalTag>(entity);

            Assert.True(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<NormalTag>(entity));

            world.Unset<FloatComponent>(entity);
            Assert.False(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<NormalTag>(entity));

            world.Unset<NormalTag>(entity);
            Assert.False(world.Has<FloatComponent>(entity));
            Assert.False(world.Has<NormalTag>(entity));
        }

        [Fact]
        public void Entity_Detach_ThreeComponent<TContext>()
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var world = new World<TContext>();
            var entity = world.Spawn();

            world.Set<FloatComponent>(entity);
            world.Set<IntComponent>(entity);
            world.Set<LargeComponent>(entity);

            world.Get<FloatComponent>(entity).Value = FLOAT_VALUE;
            world.Get<IntComponent>(entity).Value = INT_VALUE;
            world.Get<LargeComponent>(entity).Span[346] = FLOAT_VALUE_ARR;

            Assert.True(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<IntComponent>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
            Assert.Equal(INT_VALUE, world.Get<IntComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE, world.Get<FloatComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);


            world.Unset<FloatComponent>(entity);
            Assert.False(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<IntComponent>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
            Assert.Equal(INT_VALUE, world.Get<IntComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);

            world.Unset<IntComponent>(entity);
            Assert.False(world.Has<FloatComponent>(entity));
            Assert.False(world.Has<IntComponent>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
            Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);

            world.Unset<LargeComponent>(entity);
            Assert.False(world.Has<FloatComponent>(entity));
            Assert.False(world.Has<IntComponent>(entity));
            Assert.False(world.Has<LargeComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_ThreeComponent_Sparse<TContext>()
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var world = new World<TContext>();
            var entity = world.Spawn();

            world.Set<FloatComponent>(entity);
            world.Set<IntComponent>(entity);
            world.Set<LargeComponent>(entity);

            world.Get<FloatComponent>(entity).Value = FLOAT_VALUE;
            world.Get<IntComponent>(entity).Value = INT_VALUE;
            world.Get<LargeComponent>(entity).Span[346] = FLOAT_VALUE_ARR;

            Assert.True(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<IntComponent>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
            Assert.Equal(INT_VALUE, world.Get<IntComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE, world.Get<FloatComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);


            world.Unset<LargeComponent>(entity);
            Assert.True(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<IntComponent>(entity));
            Assert.False(world.Has<LargeComponent>(entity));
            Assert.Equal(FLOAT_VALUE, world.Get<FloatComponent>(entity).Value);
            Assert.Equal(INT_VALUE, world.Get<IntComponent>(entity).Value);

            world.Unset<IntComponent>(entity);
            Assert.True(world.Has<FloatComponent>(entity));
            Assert.False(world.Has<IntComponent>(entity));
            Assert.False(world.Has<LargeComponent>(entity));
            Assert.Equal(FLOAT_VALUE, world.Get<FloatComponent>(entity).Value);

            world.Unset<FloatComponent>(entity);
            Assert.True(!world.Has<FloatComponent>(entity));
            Assert.True(!world.Has<IntComponent>(entity));
            Assert.True(!world.Has<LargeComponent>(entity));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(10000)]
        public void Entity_Attach_Detach_ThreeComponent_NTimes<TContext>(int times)
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var world = new World<TContext>();
            var entity = world.Spawn();

            for (int i = 0; i < times; i++)
            {
                world.Set<FloatComponent>(entity);
                world.Set<IntComponent>(entity);
                world.Set<LargeComponent>(entity);

                world.Get<FloatComponent>(entity).Value = FLOAT_VALUE;
                world.Get<IntComponent>(entity).Value = INT_VALUE;
                world.Get<LargeComponent>(entity).Span[346] = FLOAT_VALUE_ARR;

                Assert.True(world.Has<FloatComponent>(entity));
                Assert.True(world.Has<IntComponent>(entity));
                Assert.True(world.Has<LargeComponent>(entity));
                Assert.Equal(INT_VALUE, world.Get<IntComponent>(entity).Value);
                Assert.Equal(FLOAT_VALUE, world.Get<FloatComponent>(entity).Value);
                Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);


                world.Unset<FloatComponent>(entity);
                Assert.False(world.Has<FloatComponent>(entity));
                Assert.True(world.Has<IntComponent>(entity));
                Assert.True(world.Has<LargeComponent>(entity));
                Assert.Equal(INT_VALUE, world.Get<IntComponent>(entity).Value);
                Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);

                world.Unset<IntComponent>(entity);
                Assert.False(world.Has<FloatComponent>(entity));
                Assert.False(world.Has<IntComponent>(entity));
                Assert.True(world.Has<LargeComponent>(entity));
                Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);

                world.Unset<LargeComponent>(entity);
                Assert.False(world.Has<FloatComponent>(entity));
                Assert.False(world.Has<IntComponent>(entity));
                Assert.False(world.Has<LargeComponent>(entity));
            }
        }

		[Fact]
		public void Detach_Sequential_Components<TContext>()
		{
			using var world = new World<TContext>();

			var e0 = world.Spawn();
			var e1 = world.Spawn();

			e0.Set<IntComponent>();
			e1.Set<IntComponent>();
			e0.Unset<IntComponent>();

			Assert.True(world.Has<IntComponent>(e1));
			Assert.False(world.Has<IntComponent>(e0));
		}
    }
}
