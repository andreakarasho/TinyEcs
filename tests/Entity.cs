using System.Numerics;
using System.Runtime.InteropServices;

namespace TinyEcs.Tests
{
    public class EntityTest
    {
        [Fact]
        public void Entity_Creation()
        {
            using var ctx = new Context();
            var entity = ctx.World.Entity();

            Assert.True(entity.Exists());
        }

        [Fact]
        public void Entity_Deletion()
        {
            using var ctx = new Context();

            var e1 = ctx.World.Entity();
            var e2 = ctx.World.Entity();
            var e3 = ctx.World.Entity();
            var e4 = ctx.World.Entity();

            e2.Delete();
            e2 = ctx.World.Entity();

            e3.Delete();
            e3 = ctx.World.Entity();

            e2.Delete();
            e2 = ctx.World.Entity();

            Assert.True(e1.Exists());
            Assert.True(e2.Exists());
            Assert.True(e3.Exists());
            Assert.True(e4.Exists());
        }

        [Fact]
        public void Entity_Enable()
        {
            using var ctx = new Context();

            var entity = ctx.World.Entity();
            entity.Enable();

            Assert.True(entity.IsEnabled());
        }

        [Fact]
        public void Entity_Disabled()
        {
            using var ctx = new Context();

            var entity = ctx.World.Entity();
            entity.Disable();

            Assert.False(entity.IsEnabled());
        }

        [Fact]
        public void Entity_Attach_TwoSameComponent()
        {
            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Unset<FloatComponent>(entity);

            Assert.False(ctx.World.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Attach_OneComponent()
        {
            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());

            Assert.True(ctx.World.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Attach_TwoComponent()
        {
            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Set<NormalTag>(entity);

            Assert.True(ctx.World.Has<FloatComponent>(entity));
            Assert.True(ctx.World.Has<NormalTag>(entity));
        }

        [Fact]
        public void Entity_Attach_ThreeComponent()
        {
            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Set<NormalTag>(entity);
            ctx.World.Set(entity, new LargeComponent());

            Assert.True(ctx.World.Has<FloatComponent>(entity));
            Assert.True(ctx.World.Has<NormalTag>(entity));
            Assert.True(ctx.World.Has<LargeComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_OneComponent()
        {
            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Unset<FloatComponent>(entity);

            Assert.True(!ctx.World.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_TwoComponent()
        {
            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Set<NormalTag>(entity);

            Assert.True(ctx.World.Has<FloatComponent>(entity));
            Assert.True(ctx.World.Has<NormalTag>(entity));

            ctx.World.Unset<FloatComponent>(entity);
            Assert.False(ctx.World.Has<FloatComponent>(entity));
            Assert.True(ctx.World.Has<NormalTag>(entity));

            ctx.World.Unset<NormalTag>(entity);
            Assert.False(ctx.World.Has<FloatComponent>(entity));
            Assert.False(ctx.World.Has<NormalTag>(entity));
        }

        [Fact]
        public void Entity_Detach_ThreeComponent()
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Set(entity, new IntComponent());
            ctx.World.Set(entity, new LargeComponent());

            ctx.World.Get<FloatComponent>(entity).Value = FLOAT_VALUE;
            ctx.World.Get<IntComponent>(entity).Value = INT_VALUE;
            ctx.World.Get<LargeComponent>(entity).Span[346] = FLOAT_VALUE_ARR;

            Assert.True(ctx.World.Has<FloatComponent>(entity));
            Assert.True(ctx.World.Has<IntComponent>(entity));
            Assert.True(ctx.World.Has<LargeComponent>(entity));
            Assert.Equal(INT_VALUE, ctx.World.Get<IntComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE, ctx.World.Get<FloatComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE_ARR, ctx.World.Get<LargeComponent>(entity).Span[346]);

            ctx.World.Unset<FloatComponent>(entity);
            Assert.False(ctx.World.Has<FloatComponent>(entity));
            Assert.True(ctx.World.Has<IntComponent>(entity));
            Assert.True(ctx.World.Has<LargeComponent>(entity));
            Assert.Equal(INT_VALUE, ctx.World.Get<IntComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE_ARR, ctx.World.Get<LargeComponent>(entity).Span[346]);

            ctx.World.Unset<IntComponent>(entity);
            Assert.False(ctx.World.Has<FloatComponent>(entity));
            Assert.False(ctx.World.Has<IntComponent>(entity));
            Assert.True(ctx.World.Has<LargeComponent>(entity));
            Assert.Equal(FLOAT_VALUE_ARR, ctx.World.Get<LargeComponent>(entity).Span[346]);

            ctx.World.Unset<LargeComponent>(entity);
            Assert.False(ctx.World.Has<FloatComponent>(entity));
            Assert.False(ctx.World.Has<IntComponent>(entity));
            Assert.False(ctx.World.Has<LargeComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_ThreeComponent_Sparse()
        {
            const int INT_VALUE = 2;
            const float FLOAT_VALUE = 120.66f;
            const float FLOAT_VALUE_ARR = 0.0003215f;

            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Set(entity, new IntComponent());
            ctx.World.Set(entity, new LargeComponent());

            ctx.World.Get<FloatComponent>(entity).Value = FLOAT_VALUE;
            ctx.World.Get<IntComponent>(entity).Value = INT_VALUE;
            ctx.World.Get<LargeComponent>(entity).Span[346] = FLOAT_VALUE_ARR;

            Assert.True(ctx.World.Has<FloatComponent>(entity));
            Assert.True(ctx.World.Has<IntComponent>(entity));
            Assert.True(ctx.World.Has<LargeComponent>(entity));
            Assert.Equal(INT_VALUE, ctx.World.Get<IntComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE, ctx.World.Get<FloatComponent>(entity).Value);
            Assert.Equal(FLOAT_VALUE_ARR, ctx.World.Get<LargeComponent>(entity).Span[346]);

            ctx.World.Unset<LargeComponent>(entity);
            Assert.True(ctx.World.Has<FloatComponent>(entity));
            Assert.True(ctx.World.Has<IntComponent>(entity));
            Assert.False(ctx.World.Has<LargeComponent>(entity));
            Assert.Equal(FLOAT_VALUE, ctx.World.Get<FloatComponent>(entity).Value);
            Assert.Equal(INT_VALUE, ctx.World.Get<IntComponent>(entity).Value);

            ctx.World.Unset<IntComponent>(entity);
            Assert.True(ctx.World.Has<FloatComponent>(entity));
            Assert.False(ctx.World.Has<IntComponent>(entity));
            Assert.False(ctx.World.Has<LargeComponent>(entity));
            Assert.Equal(FLOAT_VALUE, ctx.World.Get<FloatComponent>(entity).Value);

            ctx.World.Unset<FloatComponent>(entity);
            Assert.True(!ctx.World.Has<FloatComponent>(entity));
            Assert.True(!ctx.World.Has<IntComponent>(entity));
            Assert.True(!ctx.World.Has<LargeComponent>(entity));
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

            using var ctx = new Context();
            var entity = ctx.World.Entity();

            for (int i = 0; i < times; i++)
            {
                ctx.World.Set(entity, new FloatComponent());
                ctx.World.Set(entity, new IntComponent());
                ctx.World.Set(entity, new LargeComponent());

                ctx.World.Get<FloatComponent>(entity).Value = FLOAT_VALUE;
                ctx.World.Get<IntComponent>(entity).Value = INT_VALUE;
                ctx.World.Get<LargeComponent>(entity).Span[346] = FLOAT_VALUE_ARR;

                Assert.True(ctx.World.Has<FloatComponent>(entity));
                Assert.True(ctx.World.Has<IntComponent>(entity));
                Assert.True(ctx.World.Has<LargeComponent>(entity));
                Assert.Equal(INT_VALUE, ctx.World.Get<IntComponent>(entity).Value);
                Assert.Equal(FLOAT_VALUE, ctx.World.Get<FloatComponent>(entity).Value);
                Assert.Equal(FLOAT_VALUE_ARR, ctx.World.Get<LargeComponent>(entity).Span[346]);

                ctx.World.Unset<FloatComponent>(entity);
                Assert.False(ctx.World.Has<FloatComponent>(entity));
                Assert.True(ctx.World.Has<IntComponent>(entity));
                Assert.True(ctx.World.Has<LargeComponent>(entity));
                Assert.Equal(INT_VALUE, ctx.World.Get<IntComponent>(entity).Value);
                Assert.Equal(FLOAT_VALUE_ARR, ctx.World.Get<LargeComponent>(entity).Span[346]);

                ctx.World.Unset<IntComponent>(entity);
                Assert.False(ctx.World.Has<FloatComponent>(entity));
                Assert.False(ctx.World.Has<IntComponent>(entity));
                Assert.True(ctx.World.Has<LargeComponent>(entity));
                Assert.Equal(FLOAT_VALUE_ARR, ctx.World.Get<LargeComponent>(entity).Span[346]);

                ctx.World.Unset<LargeComponent>(entity);
                Assert.False(ctx.World.Has<FloatComponent>(entity));
                Assert.False(ctx.World.Has<IntComponent>(entity));
                Assert.False(ctx.World.Has<LargeComponent>(entity));
            }
        }

        [Fact]
        public void Detach_Sequential_Components()
        {
            using var ctx = new Context();

            var e0 = ctx.World.Entity();
            var e1 = ctx.World.Entity();

            e0.Set(new IntComponent());
            e1.Set(new IntComponent());
            e0.Unset<IntComponent>();

            Assert.True(ctx.World.Has<IntComponent>(e1));
            Assert.False(ctx.World.Has<IntComponent>(e0));
        }
    }
}
