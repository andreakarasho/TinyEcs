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

			Assert.True(entity.ID != 0);
            Assert.True(entity.Exists());
        }

		[Fact]
		public void Entity_WithId()
		{
			using var ctx = new Context();

			var entity = ctx.World.Entity(4000);
			Assert.Equal(4000ul, entity.ID);
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
            ctx.World.Add<NormalTag>(entity);

            Assert.True(ctx.World.Has<FloatComponent>(entity));
            Assert.True(ctx.World.Has<NormalTag>(entity));
        }

        [Fact]
        public void Entity_Attach_ThreeComponent()
        {
            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Add<NormalTag>(entity);
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

            Assert.False(ctx.World.Has<FloatComponent>(entity));
        }

        [Fact]
        public void Entity_Detach_TwoComponent()
        {
            using var ctx = new Context();
            var entity = ctx.World.Entity();

            ctx.World.Set(entity, new FloatComponent());
            ctx.World.Add<NormalTag>(entity);

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
            Assert.False(ctx.World.Has<FloatComponent>(entity));
            Assert.False(ctx.World.Has<IntComponent>(entity));
            Assert.False(ctx.World.Has<LargeComponent>(entity));
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

		[Fact]
		public void UndeletableEntity()
		{
			using var ctx = new Context();
			var ent = ctx.World.Entity();

			ent.Delete();

			Assert.ThrowsAny<Exception>(() => ctx.World.Delete(ctx.World.Entity<DoNotDelete>()));
			Assert.ThrowsAny<Exception>(() => ctx.World.Delete(Wildcard.ID));
		}

		[Fact]
		public void Declare_Tag_Before_NamedEntity()
		{
			using var ctx = new Context();

			var b = ctx.World.Entity<NormalTag>();
			var a = ctx.World.Entity("NormalTag");

			Assert.Equal("NormalTag", a.Name());
			Assert.Equal("NormalTag", b.Name());
			Assert.True(a.ID == b.ID);
		}

		[Fact]
		public void Declare_NamedEntity_Before_Tag()
		{
			using var ctx = new Context();

			var a = ctx.World.Entity("NormalTag");

			Assert.Equal(ctx.World.Entity<NormalTag>(), a);
		}

		[Fact]
		public void Entity_Has_Wildcard()
		{
			using var ctx = new Context();

			var main = ctx.World.Entity();
			var likes = ctx.World.Entity();
			var dogs = ctx.World.Entity();

			main.Set<BoolComponent>(new ());
			main.Add(likes, dogs);

			Assert.True(main.Has<Wildcard>());
			Assert.True(main.Has(likes, Wildcard.ID));
			Assert.True(main.Has(Wildcard.ID, dogs));
			Assert.True(main.Has(Wildcard.ID, Wildcard.ID));
		}

		[Fact]
		public void Entity_Attach_ManagedComponent()
		{
			using var ctx = new Context();

			var ent = ctx.World.Entity();

			ent.Set(new FloatComponent() { Value = 99 });
			ent.Set(new ManagedComponent() { Obj = new { a = "asd", b = 3, c = new List<object>() }, Text = "hello" });
			ent.Set(new IntComponent() { Value = 69 });

			Assert.True(ent.Has<FloatComponent>());
			Assert.True(ent.Has<ManagedComponent>());
			Assert.True(ent.Has<IntComponent>());

			ref var managed = ref ent.Get<ManagedComponent>();
			ref var fl = ref ent.Get<FloatComponent>();
			Assert.Equal("hello", managed.Text);

			var target = ctx.World.Entity();
			ent.Set(new ManagedComponent() { Obj = new { a = "asd", b = 3, c = new List<object>() }, Text = "hello 2" }, target);
			ent.Set(new BoolComponent());

			managed = ref ent.Get<ManagedComponent>(target);
			Assert.Equal("hello 2", managed.Text);
		}
    }
}
