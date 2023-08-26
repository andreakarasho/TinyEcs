using System.Numerics;
using System.Runtime.InteropServices;

namespace TinyEcs.Tests
{
    public class EntityTest
    {
        [Fact]
        public void Entity_Creation()
        {
            using var world = new World();
            var entity = world.Entity();

            Assert.True(entity.Exists());
        }

        [Fact]
        public void Entity_Deletion()
        {
            using var world = new World();

            var e1 = world.Entity();
            var e2 = world.Entity();
            var e3 = world.Entity();
            var e4 = world.Entity();

            e2.Delete();
            e2 = world.Entity();

            e3.Delete();
            e3 = world.Entity();

            e2.Delete();
            e2 = world.Entity();

            Assert.True(e1.Exists());
            Assert.True(e2.Exists());
            Assert.True(e3.Exists());
            Assert.True(e4.Exists());
        }

        [Fact]
        public void Entity_Enable()
        {
            using var world = new World();

            var entity = world.Entity();
            entity.Enable();

            Assert.True(entity.IsEnabled());
        }

        [Fact]
        public void Entity_Disabled()
        {
            using var world = new World();

            var entity = world.Entity();
            entity.Disable();

            Assert.False(entity.IsEnabled());
        }

        [Fact]
        public void Entity_Attach_TwoSameComponent()
        {
            using var world = new World();
            var entity = world.Entity();

            world.Set<FloatComponent>(entity);
            world.Set<FloatComponent>(entity);
            world.Unset<FloatComponent>(entity);

            Assert.False(world.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Attach_OneComponent()
        {
            using var world = new World();
            var entity = world.Entity();

            world.Set<FloatComponent>(entity);

            Assert.True(world.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Attach_TwoComponent()
        {
            using var world = new World();
            var entity = world.Entity();

            world.Set<FloatComponent>(entity);
            world.Set<NormalTag>(entity);

            Assert.True(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<NormalTag>(entity));
        }

        [Fact]
        public void Entity_Attach_ThreeComponent()
        {
            using var world = new World();
            var entity = world.Entity();

            world.Set<FloatComponent>(entity);
            world.Set<NormalTag>(entity);
            world.Set<LargeComponent>(entity);

            Assert.True(world.Has<FloatComponent>(entity));
            Assert.True(world.Has<NormalTag>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_OneComponent()
        {
            using var world = new World();
            var entity = world.Entity();

            world.Set<FloatComponent>(entity);
            world.Unset<FloatComponent>(entity);

            Assert.True(!world.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_TwoComponent()
        {
            using var world = new World();
            var entity = world.Entity();

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
        public void Entity_Detach_ThreeComponent()
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var world = new World();
            var entity = world.Entity();

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
        public void Entity_Detach_ThreeComponent_Sparse()
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var world = new World();
            var entity = world.Entity();

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
        public void Entity_Attach_Detach_ThreeComponent_NTimes(int times)
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var world = new World();
            var entity = world.Entity();

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
		public void Detach_Sequential_Components()
		{
			using var world = new World();

			var e0 = world.Entity();
			var e1 = world.Entity();

			e0.Set<IntComponent>();
			e1.Set<IntComponent>();
			e0.Unset<IntComponent>();

			Assert.True(world.Has<IntComponent>(e1));
			Assert.False(world.Has<IntComponent>(e0));
		}
    }
}
