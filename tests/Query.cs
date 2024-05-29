using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs.Tests
{
    public class QueryTest
    {
        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void Query_AttachOneComponent_WithOneComponent(int amount)
        {
            using var ctx = new Context();

            for (var i = 0; i < amount; i++)
                ctx.World.Set(ctx.World.Entity(), new FloatComponent());

            var done = 0;
            foreach (var arch in ctx.World.Query<FloatComponent>())
            foreach (ref readonly var chunk in arch)
	            done += chunk.Count;

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void Query_AttachTwoComponents_WithTwoComponents(int amount)
        {
            using var ctx = new Context();

            for (int i = 0; i < amount; i++)
            {
                var e = ctx.World.Entity();
                ctx.World.Set(e, new FloatComponent());
                ctx.World.Set(e, new IntComponent());
            }

            var done = ctx.World.Query<(FloatComponent, IntComponent)>().Count();

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void Query_AttachThreeComponents_WithThreeComponents(int amount)
        {
            using var ctx = new Context();

            for (var i = 0; i < amount; i++)
            {
                var e = ctx.World.Entity();
                ctx.World.Set(e, new FloatComponent());
                ctx.World.Set(e, new IntComponent());
                ctx.World.Set(e, new BoolComponent());
            }

            var done = ctx.World.Query<(FloatComponent, IntComponent, BoolComponent)>().Count();

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void Query_AttachThreeComponents_WithTwoComponents_WithoutOneComponent(int amount)
        {
            using var ctx = new Context();

            for (int i = 0; i < amount; i++)
            {
                var e = ctx.World.Entity();
                ctx.World.Set(e, new FloatComponent());
                ctx.World.Set(e, new IntComponent());
                ctx.World.Set(e, new BoolComponent());
            }

            var done = ctx.World.Query<(FloatComponent, IntComponent), Without<BoolComponent>>().Count();

            Assert.Equal(0, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void Query_AttachTwoComponents_WithTwoComponents_WithoutOneComponent(int amount)
        {
            using var ctx = new Context();

            for (int i = 0; i < amount; i++)
            {
                var e = ctx.World.Entity();
                ctx.World.Set(e, new FloatComponent());
                ctx.World.Set(e, new IntComponent());
            }

            var done = ctx.World.Query<(FloatComponent, IntComponent), Without<BoolComponent>>().Count();

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void Query_AttachTwoComponents_WithOneComponents_WithoutTwoComponent(int amount)
        {
            using var ctx = new Context();

            for (int i = 0; i < amount; i++)
            {
                var e = ctx.World.Entity();
                ctx.World.Set(e, new FloatComponent());
                ctx.World.Set(e, new IntComponent());
            }

            var done = ctx.World.Query<FloatComponent, (Without<IntComponent>, Without<IntComponent>)>().Count();

            Assert.Equal(0, done);
        }

        [Fact]
        public void Query_EdgeValidation()
        {
            using var ctx = new Context();

            var good = 0;

            var e = ctx.World.Entity();
            ctx.World.Set(e, new FloatComponent());
            ctx.World.Set(e, new IntComponent());
			good++;

            var e2 = ctx.World.Entity();
            ctx.World.Set(e2, new FloatComponent());
            ctx.World.Set(e2, new IntComponent());
            ctx.World.Set(e2, new BoolComponent());

            var e3 = ctx.World.Entity();
            ctx.World.Set(e3, new FloatComponent());
            ctx.World.Set(e3, new IntComponent());
            ctx.World.Set(e3, new BoolComponent());

            var e4 = ctx.World.Entity();
            ctx.World.Set(e4, new FloatComponent());
            ctx.World.Set(e4, new IntComponent());
            ctx.World.Set(e4, new BoolComponent());
            ctx.World.Set<NormalTag>(e4);

            var done = ctx.World.Query<(FloatComponent, IntComponent), (Without<BoolComponent>, Without<NormalTag>)>().Count();

            Assert.Equal(good, done);
        }

		[Fact]
		public void Query_Single()
		{
			using var ctx = new Context();

			var singleton = ctx.World.Entity()
				.Set(new FloatComponent())
				.Set(new IntComponent())
				.Set<NormalTag>();

			var other = ctx.World.Entity()
				.Set(new FloatComponent())
				.Set(new IntComponent());

			var result = ctx.World.Query<ValueTuple, (With<FloatComponent>, With<IntComponent>, With<NormalTag>)>()
				.Single();

			Assert.Equal(singleton.ID, result.ID);

			var singleton2 = ctx.World.Entity()
				.Set(new FloatComponent())
				.Set(new IntComponent())
				.Set<NormalTag>();

			var query = ctx.World.Query<ValueTuple, (With<FloatComponent>, With<IntComponent>, With<NormalTag>)>();

			Assert.ThrowsAny<Exception>(() => _ = query.Single());
		}

		[Fact]
		public void Query_Optional()
		{
			using var ctx = new Context();

			ctx.World.Entity();
			ctx.World.Entity().Set(new IntComponent() { Value = -10 });
			ctx.World.Entity().Set(new FloatComponent());
			ctx.World.Entity().Set(new FloatComponent()).Set(new IntComponent() { Value = 10 });

			var count = 0;
			ctx.World.Query<Optional<IntComponent>, With<FloatComponent>>()
				.Each((ref IntComponent maybeInt) => {
					Assert.True(Unsafe.IsNullRef(ref maybeInt) || maybeInt.Value == 10);
					count += 1;
				});

			Assert.Equal(2, count);
		}

		[Fact]
		public void Query_Multiple_Optional()
		{
			using var ctx = new Context();

			ctx.World.Entity();
			ctx.World.Entity().Set(new IntComponent() { Value = -10 });
			ctx.World.Entity()
				.Set(new IntComponent() { Value = 10 })
				.Set<NormalTag>();
			ctx.World.Entity()
				.Set(new FloatComponent() { Value = 0.5f })
				.Set<NormalTag>();
			ctx.World.Entity()
				.Set(new FloatComponent() { Value = 0.5f })
				.Set(new IntComponent() { Value = 10 })
				.Set<NormalTag>();;

			var count = 0;
			ctx.World.Query<(Optional<IntComponent>, Optional<FloatComponent>), With<NormalTag>>()
				.Each((ref IntComponent maybeInt, ref FloatComponent maybeFloat) => {
					Assert.True(Unsafe.IsNullRef(ref maybeInt) || maybeInt.Value == 10);
					Assert.True(Unsafe.IsNullRef(ref maybeFloat) || maybeFloat.Value == 0.5f);
					count += 1;
				});

			Assert.Equal(3, count);
		}

		[Fact]
		public void Query_AtLeast()
		{
			using var ctx = new Context();

			ctx.World.Entity();
			ctx.World.Entity().Set<NormalTag>();

			var ent0 = ctx.World.Entity()
				.Set<NormalTag2>()
				.Set(new FloatComponent());

			var ent1 = ctx.World.Entity()
				.Set(new BoolComponent());

			var entRes = ctx.World
				.Query<ValueTuple, AtLeast<(IntComponent, BoolComponent)>>()
				.Single();

			Assert.Equal(ent1.ID, entRes.ID);
		}

		[Fact]
		public void Query_Exactly()
		{
			using var ctx = new Context();

			ctx.World.Entity();
			ctx.World.Entity()
				.Set<NormalTag>()
				.Set(new BoolComponent())
				.Set(new IntComponent());

			var ent0 = ctx.World.Entity()
				.Set<NormalTag2>()
				.Set(new IntComponent());

			var ent1 = ctx.World.Entity()
				.Set(new IntComponent())
				.Set(new BoolComponent());

			var entRes = ctx.World
				.Query<Exactly<(IntComponent, BoolComponent)>>()
				.Single();

			Assert.Equal(ent1.ID, entRes.ID);
		}

		[Fact]
		public void Query_None()
		{
			using var ctx = new Context();

			ctx.World.Entity()
				.Set<NormalTag>()
				.Set<NormalTag2>()
				.Set(new BoolComponent());

			ctx.World.Entity()
				.Set<NormalTag2>()
				.Set(new BoolComponent());

			ctx.World.Entity()
				.Set<NormalTag>()
				.Set(new BoolComponent());

			var count = ctx.World
				.Query<ValueTuple, None<(With<NormalTag>, With<NormalTag2>)>>()
				.Count();

			var total = ctx.World.EntityCount - 3;

			Assert.Equal(total, count);
		}

		[Fact]
		public void Query_Or()
		{
			using var ctx = new Context();

			var ent0 = ctx.World.Entity()
				.Set<NormalTag>()
				.Set<NormalTag2>();

			var ent1 = ctx.World.Entity()
				.Set(new BoolComponent())
				.Set<NormalTag>();

			var ent2 = ctx.World.Entity()
				.Set(new IntComponent())
				.Set(new BoolComponent());

			var query = ctx.World
				.Query<ValueTuple, (Or<(With<NormalTag>, With<NormalTag2>)>, Or<(With<BoolComponent>, With<NormalTag>)>)>();

			var resEnt = query.Single();
			Assert.Equal(ent0.ID, resEnt.ID);

			ent0.Delete();
			resEnt = query.Single();
			Assert.Equal(ent1.ID, resEnt.ID);
		}

		[Fact]
		public void Query_Nested_Or()
		{
			using var ctx = new Context();

			var ent0 = ctx.World.Entity()
				.Set<NormalTag>()
				.Set<NormalTag2>();

			var ent1 = ctx.World.Entity()
				.Set(new BoolComponent())
				.Set<NormalTag>();

			var ent2 = ctx.World.Entity()
				.Set(new IntComponent())
				.Set(new BoolComponent());

			var query = ctx.World
				.Query<ValueTuple,
					(Or<(With<NormalTag>, With<NormalTag2>)>,
					Or<(With<BoolComponent>, With<NormalTag>)>,
					Or<(With<IntComponent>, With<BoolComponent>)>)>();

			var resEnt = query.Single();
			Assert.Equal(ent0.ID, resEnt.ID);

			ent0.Delete();
			resEnt = query.Single();
			Assert.Equal(ent1.ID, resEnt.ID);

			ent1.Delete();
			resEnt = query.Single();
			Assert.Equal(ent2.ID, resEnt.ID);
		}

		[Fact]
		public void Query_FilterTypes_In_QueryData()
		{
			using var ctx = new Context();

			ctx.World.Entity<IntComponent>();
			ctx.World.Entity<FloatComponent>();

			Assert.ThrowsAny<Exception>(ctx.World.Query<With<IntComponent>>);
			Assert.ThrowsAny<Exception>(ctx.World.Query<(FloatComponent, With<IntComponent>)>);
		}

		[Fact]
		public void Query_QueryData_In_FilterType()
		{
			using var ctx = new Context();

			ctx.World.Entity<IntComponent>();
			ctx.World.Entity<FloatComponent>();

			Assert.ThrowsAny<Exception>(ctx.World.Query<IntComponent, FloatComponent>);
			Assert.ThrowsAny<Exception>(ctx.World.Query<(FloatComponent, (IntComponent, With<IntComponent>))>);
		}
    }
}
