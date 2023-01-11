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
            var entity = world.CreateEntity();

            Assert.True(entity > 0);
        }

        [Fact]
        public void Entity_Deletion()
        {
            using var world = new World();
            var entity = world.CreateEntity();
            world.DestroyEntity(entity);
            
            Assert.True(world.EntityCount == 0);
        }

        [Fact]
        public void Entity_Attach_TwoSameComponent()
        {
            using var world = new World();
            var entity = world.CreateEntity();

            world.Attach<float>(entity);
            world.Attach<float>(entity);
            world.Detach<float>(entity);

            Assert.True(!world.Has<float>(entity));
        }

        [Fact]
        public void Entity_Attach_OneComponent()
        {
            using var world = new World();
            var entity = world.CreateEntity();

            world.Attach<float>(entity);

            Assert.True(world.Has<float>(entity));
        }

        [Fact]
        public void Entity_Attach_TwoComponent()
        {
            using var world = new World();
            var entity = world.CreateEntity();

            world.Attach<float>(entity);
            world.Attach<int>(entity);

            Assert.True(world.Has<float>(entity));
            Assert.True(world.Has<int>(entity));
        }

        [Fact]
        public void Entity_Attach_ThreeComponent()
        {
            using var world = new World();
            var entity = world.CreateEntity();

            world.Attach<float>(entity);
            world.Attach<int>(entity);
            world.Attach<LargeComponent>(entity);

            Assert.True(world.Has<float>(entity));
            Assert.True(world.Has<int>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_OneComponent()
        {
            using var world = new World();
            var entity = world.CreateEntity();

            world.Attach<float>(entity);
            world.Detach<float>(entity);

            Assert.True(!world.Has<float>(entity));
        }

        [Fact]
        public void Entity_Detach_TwoComponent()
        {
            using var world = new World();
            var entity = world.CreateEntity();

            world.Attach<float>(entity);
            world.Attach<int>(entity);

            Assert.True(world.Has<float>(entity));
            Assert.True(world.Has<int>(entity));

            world.Detach<float>(entity);
            Assert.True(!world.Has<float>(entity));
            Assert.True(world.Has<int>(entity));

            world.Detach<int>(entity);
            Assert.True(!world.Has<float>(entity));
            Assert.True(!world.Has<int>(entity));
        }

        [Fact]
        public void Entity_Detach_ThreeComponent()
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var world = new World();
            var entity = world.CreateEntity();

            world.Attach<float>(entity);
            world.Attach<int>(entity);
            world.Attach<LargeComponent>(entity);

            world.Get<float>(entity) = FLOAT_VALUE;
            world.Get<int>(entity) = INT_VALUE;
            world.Get<LargeComponent>(entity).Span[346] = FLOAT_VALUE_ARR;

            Assert.True(world.Has<float>(entity));
            Assert.True(world.Has<int>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
            Assert.Equal(INT_VALUE, world.Get<int>(entity));
            Assert.Equal(FLOAT_VALUE, world.Get<float>(entity));
            Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);

            
            world.Detach<float>(entity);
            Assert.True(!world.Has<float>(entity));
            Assert.True(world.Has<int>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
            Assert.Equal(INT_VALUE, world.Get<int>(entity));
            Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);

            world.Detach<int>(entity);
            Assert.True(!world.Has<float>(entity));
            Assert.True(!world.Has<int>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
            Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);

            world.Detach<LargeComponent>(entity);
            Assert.True(!world.Has<float>(entity));
            Assert.True(!world.Has<int>(entity));
            Assert.True(!world.Has<LargeComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_ThreeComponent_Sparse()
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var world = new World();
            var entity = world.CreateEntity();

            world.Attach<float>(entity);
            world.Attach<int>(entity);
            world.Attach<LargeComponent>(entity);

            world.Get<float>(entity) = FLOAT_VALUE;
            world.Get<int>(entity) = INT_VALUE;
            world.Get<LargeComponent>(entity).Span[346] = FLOAT_VALUE_ARR;

            Assert.True(world.Has<float>(entity));
            Assert.True(world.Has<int>(entity));
            Assert.True(world.Has<LargeComponent>(entity));
            Assert.Equal(INT_VALUE, world.Get<int>(entity));
            Assert.Equal(FLOAT_VALUE, world.Get<float>(entity));
            Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);


            world.Detach<LargeComponent>(entity);
            Assert.True(world.Has<float>(entity));
            Assert.True(world.Has<int>(entity));
            Assert.True(!world.Has<LargeComponent>(entity));
            Assert.Equal(FLOAT_VALUE, world.Get<float>(entity));
            Assert.Equal(INT_VALUE, world.Get<int>(entity));

            world.Detach<int>(entity);
            Assert.True(world.Has<float>(entity));
            Assert.True(!world.Has<int>(entity));
            Assert.True(!world.Has<LargeComponent>(entity));
            Assert.Equal(FLOAT_VALUE, world.Get<float>(entity));

            world.Detach<float>(entity);
            Assert.True(!world.Has<float>(entity));
            Assert.True(!world.Has<int>(entity));
            Assert.True(!world.Has<LargeComponent>(entity));
        }


        [Theory]
        [InlineData(true, 9082331231821223701, -0.099477f)]
        [InlineData(9082331231821223701, false, -0.099477f)]
        [InlineData(-0.099477f, true, 9082331231821223701)]
        public void Entity_Detach_ThreeComponent_Generics<T0, T1, T2>(T0 t0, T1 t1, T2 t2)
            where T0 : struct
            where T1 : struct
            where T2 : struct
        {
            using var world = new World();
            var entity = world.CreateEntity();

            world.Attach<T0>(entity);
            world.Attach<T1>(entity);
            world.Attach<T2>(entity);

            world.Get<T0>(entity) = t0;
            world.Get<T1>(entity) = t1;
            world.Get<T2>(entity) = t2;


            Assert.True(world.Has<T0>(entity));
            Assert.True(world.Has<T1>(entity));
            Assert.True(world.Has<T2>(entity));
            Assert.Equal(t0, world.Get<T0>(entity));
            Assert.Equal(t1, world.Get<T1>(entity));
            Assert.Equal(t2, world.Get<T2>(entity));


            world.Detach<T0>(entity);
            Assert.True(!world.Has<T0>(entity));
            Assert.True(world.Has<T1>(entity));
            Assert.True(world.Has<T2>(entity));
            Assert.Equal(t1, world.Get<T1>(entity));
            Assert.Equal(t2, world.Get<T2>(entity));

            world.Detach<T1>(entity);
            Assert.True(!world.Has<T0>(entity));
            Assert.True(!world.Has<T1>(entity));
            Assert.True(world.Has<T2>(entity));
            Assert.Equal(t2, world.Get<T2>(entity));

            world.Detach<T2>(entity);
            Assert.True(!world.Has<T0>(entity));
            Assert.True(!world.Has<T1>(entity));
            Assert.True(!world.Has<T2>(entity));
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
            var entity = world.CreateEntity();

            for (int i = 0; i < times; i++)
            {
                world.Attach<float>(entity);
                world.Attach<int>(entity);
                world.Attach<LargeComponent>(entity);

                world.Get<float>(entity) = FLOAT_VALUE;
                world.Get<int>(entity) = INT_VALUE;
                world.Get<LargeComponent>(entity).Span[346] = FLOAT_VALUE_ARR;

                Assert.True(world.Has<float>(entity));
                Assert.True(world.Has<int>(entity));
                Assert.True(world.Has<LargeComponent>(entity));
                Assert.Equal(INT_VALUE, world.Get<int>(entity));
                Assert.Equal(FLOAT_VALUE, world.Get<float>(entity));
                Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);


                world.Detach<float>(entity);
                Assert.True(!world.Has<float>(entity));
                Assert.True(world.Has<int>(entity));
                Assert.True(world.Has<LargeComponent>(entity));
                Assert.Equal(INT_VALUE, world.Get<int>(entity));
                Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);

                world.Detach<int>(entity);
                Assert.True(!world.Has<float>(entity));
                Assert.True(!world.Has<int>(entity));
                Assert.True(world.Has<LargeComponent>(entity));
                Assert.Equal(FLOAT_VALUE_ARR, world.Get<LargeComponent>(entity).Span[346]);

                world.Detach<LargeComponent>(entity);
                Assert.True(!world.Has<float>(entity));
                Assert.True(!world.Has<int>(entity));
                Assert.True(!world.Has<LargeComponent>(entity));
            }         
        }
    }
}