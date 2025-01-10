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

			var done = ctx.World.QueryBuilder()
				.With<FloatComponent>()
				.Build()
				.Count();

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

            var done = ctx.World.QueryBuilder()
	            .With<FloatComponent>()
	            .With<IntComponent>()
	            .Build()
	            .Count();

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

            var done = ctx.World.QueryBuilder()
	            .With<FloatComponent>()
	            .With<IntComponent>()
	            .With<BoolComponent>()
	            .Build()
	            .Count();

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

            var done = ctx.World.QueryBuilder()
	            .With<FloatComponent>()
	            .With<IntComponent>()
	            .Without<BoolComponent>()
	            .Build()
	            .Count();

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

            var done = ctx.World.QueryBuilder()
	            .With<FloatComponent>()
	            .With<IntComponent>()
	            .Without<BoolComponent>()
	            .Build()
	            .Count();

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void Query_AttachTwoComponents_WithOneComponent_WithoutTwoComponent(int amount)
        {
            using var ctx = new Context();

            for (int i = 0; i < amount; i++)
            {
                var e = ctx.World.Entity();
                ctx.World.Set(e, new FloatComponent());
                ctx.World.Set(e, new IntComponent());
            }

            var done = ctx.World.QueryBuilder()
	            .With<FloatComponent>()
	            .Without<IntComponent>()
	            .Without<BoolComponent>()
	            .Build()
	            .Count();

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
            ctx.World.Add<NormalTag>(e4);

            var done = ctx.World.QueryBuilder()
	            .With<FloatComponent>()
	            .With<IntComponent>()
	            .Without<BoolComponent>()
	            .Without<NormalTag>()
	            .Build()
	            .Count();

            Assert.Equal(good, done);
        }

		// [Fact]
		// public void Query_RelationalData()
		// {
		// 	using var ctx = new Context();

		// 	var ent = ctx.World.Entity()
		// 		.Set<NormalTag, FloatComponent>(new FloatComponent() { Value = 23f });

		// 	var query = ctx.World.QueryBuilder()
		// 		.With<NormalTag, FloatComponent>()
		// 		.Build();

		// 	ref var single = ref query.Single<Pair<NormalTag, FloatComponent>>();

		// 	Assert.Equal(23f, single.Target.Value);
		// }

		[Fact]
		public void Query_Single()
		{
			using var ctx = new Context();

			var singleton = ctx.World.Entity()
				.Set(new FloatComponent())
				.Set(new IntComponent())
				.Add<NormalTag>();

			var other = ctx.World.Entity()
				.Set(new FloatComponent())
				.Set(new IntComponent());

			var result = ctx.World.QueryBuilder()
				.With<FloatComponent>()
				.With<IntComponent>()
				.With<NormalTag>()
				.Build()
				.Single();

			Assert.Equal(singleton.ID, result.ID);

			var singleton2 = ctx.World.Entity()
				.Set(new FloatComponent())
				.Set(new IntComponent())
				.Add<NormalTag>();

			var query = ctx.World.QueryBuilder()
				.With<FloatComponent>()
				.With<IntComponent>()
				.With<NormalTag>()
				.Build();

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
			var it = ctx.World.QueryBuilder()
				.Optional<IntComponent>()
				.With<FloatComponent>()
				.Build()
				.Iter();

			while (it.Next())
			{
				var span0 = it.Data<IntComponent>(0);
				var span1 = it.Data<FloatComponent>(1);

				for (var i = 0; i < it.Count; ++i)
				{
					Assert.True(span0.IsEmpty || span0[i].Value == 10);
					count += 1;
				}
			}

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
				.Add<NormalTag>();
			ctx.World.Entity()
				.Set(new FloatComponent() { Value = 0.5f })
				.Add<NormalTag>();
			ctx.World.Entity()
				.Set(new FloatComponent() { Value = 0.5f })
				.Set(new IntComponent() { Value = 10 })
				.Add<NormalTag>();

			var count = 0;
			var it = ctx.World.QueryBuilder()
				.Optional<IntComponent>()
				.Optional<FloatComponent>()
				.With<NormalTag>()
				.Build()
				.Iter();


			while (it.Next())
			{
				var span0 = it.Data<IntComponent>(0);
				var span1 = it.Data<FloatComponent>(1);

				for (var i = 0; i < it.Count; ++i)
					{
						Assert.True(span0.IsEmpty || span0[i].Value == 10);
						Assert.True(span1.IsEmpty || span1[i].Value == 0.5f);
						count += 1;
					}
			}

			Assert.Equal(3, count);
		}

		// [Fact]
		// public void Query_AtLeast()
		// {
		// 	using var ctx = new Context();
		//
		// 	ctx.World.Entity();
		// 	ctx.World.Entity().Add<NormalTag>();
		//
		// 	var ent0 = ctx.World.Entity()
		// 		.Add<NormalTag2>()
		// 		.Set(new FloatComponent());
		//
		// 	var ent1 = ctx.World.Entity()
		// 		.Set(new BoolComponent());
		//
		// 	var entRes = ctx.World
		// 		.Query<ValueTuple, AtLeast<(IntComponent, BoolComponent)>>()
		// 		.Single();
		//
		// 	Assert.Equal(ent1.ID, entRes.ID);
		// }
		//
		// [Fact]
		// public void Query_Exactly()
		// {
		// 	using var ctx = new Context();
		//
		// 	ctx.World.Entity();
		// 	ctx.World.Entity()
		// 		.Add<NormalTag>()
		// 		.Set(new BoolComponent())
		// 		.Set(new IntComponent());
		//
		// 	var ent0 = ctx.World.Entity()
		// 		.Add<NormalTag2>()
		// 		.Set(new IntComponent());
		//
		// 	var ent1 = ctx.World.Entity()
		// 		.Set(new IntComponent())
		// 		.Set(new BoolComponent());
		//
		// 	var entRes = ctx.World
		// 		.Query<Exactly<(IntComponent, BoolComponent)>>()
		// 		.Single();
		//
		// 	Assert.Equal(ent1.ID, entRes.ID);
		// }
		//
		// [Fact]
		// public void Query_None()
		// {
		// 	using var ctx = new Context();
		//
		// 	ctx.World.Entity()
		// 		.Add<NormalTag>()
		// 		.Add<NormalTag2>()
		// 		.Set(new BoolComponent());
		//
		// 	ctx.World.Entity()
		// 		.Add<NormalTag2>()
		// 		.Set(new BoolComponent());
		//
		// 	ctx.World.Entity()
		// 		.Add<NormalTag>()
		// 		.Set(new BoolComponent());
		//
		// 	var count = ctx.World
		// 		.Query<ValueTuple, None<(With<NormalTag>, With<NormalTag2>)>>()
		// 		.Count();
		//
		// 	var total = ctx.World.EntityCount - 3;
		//
		// 	Assert.Equal(total, count);
		// }
		//
		// [Fact]
		// public void Query_Or()
		// {
		// 	using var ctx = new Context();
		//
		// 	var ent0 = ctx.World.Entity()
		// 		.Add<NormalTag>()
		// 		.Add<NormalTag2>();
		//
		// 	var ent1 = ctx.World.Entity()
		// 		.Set(new BoolComponent())
		// 		.Add<NormalTag>();
		//
		// 	var ent2 = ctx.World.Entity()
		// 		.Set(new IntComponent())
		// 		.Set(new BoolComponent());
		//
		// 	var query = ctx.World
		// 		.Query<ValueTuple, (Or<(With<NormalTag>, With<NormalTag2>)>, Or<(With<BoolComponent>, With<NormalTag>)>)>();
		//
		// 	var resEnt = query.Single();
		// 	Assert.Equal(ent0.ID, resEnt.ID);
		//
		// 	ent0.Delete();
		// 	resEnt = query.Single();
		// 	Assert.Equal(ent1.ID, resEnt.ID);
		// }
		//
		// [Fact]
		// public void Query_Adjacent_Or()
		// {
		// 	using var ctx = new Context();
		//
		// 	var ent0 = ctx.World.Entity()
		// 		.Add<NormalTag>()
		// 		.Add<NormalTag2>();
		//
		// 	var ent1 = ctx.World.Entity()
		// 		.Set(new BoolComponent())
		// 		.Add<NormalTag>();
		//
		// 	var ent2 = ctx.World.Entity()
		// 		.Set(new IntComponent())
		// 		.Set(new BoolComponent());
		//
		// 	var query = ctx.World
		// 		.Query<ValueTuple,
		// 			(Or<(With<NormalTag>, With<NormalTag2>)>,
		// 			Or<(With<BoolComponent>, With<NormalTag>)>,
		// 			Or<(With<IntComponent>, With<BoolComponent>)>)>();
		//
		// 	var resEnt = query.Single();
		// 	Assert.Equal(ent0.ID, resEnt.ID);
		//
		// 	ent0.Delete();
		// 	resEnt = query.Single();
		// 	Assert.Equal(ent1.ID, resEnt.ID);
		//
		// 	ent1.Delete();
		// 	resEnt = query.Single();
		// 	Assert.Equal(ent2.ID, resEnt.ID);
		// }
		//
		// [Fact]
		// public void Query_Nested_Or()
		// {
		// 	using var ctx = new Context();
		//
		// 	var ent0 = ctx.World.Entity()
		// 		.Add<NormalTag>()
		// 		.Add<NormalTag2>();
		//
		// 	var ent1 = ctx.World.Entity()
		// 		.Set(new BoolComponent())
		// 		.Add<NormalTag>();
		//
		// 	var ent2 = ctx.World.Entity()
		// 		.Set(new IntComponent())
		// 		.Set(new BoolComponent());
		//
		// 	var query = ctx.World
		// 		.Query<ValueTuple,
		// 			Or<(With<NormalTag>, With<NormalTag2>,
		// 				Or<(With<BoolComponent>, With<NormalTag>,
		// 					Or<(With<IntComponent>, With<BoolComponent>)>)>
		// 				)>
		// 			>();
		//
		// 	var resEnt = query.Single();
		// 	Assert.Equal(ent0.ID, resEnt.ID);
		//
		// 	ent0.Delete();
		// 	resEnt = query.Single();
		// 	Assert.Equal(ent1.ID, resEnt.ID);
		//
		// 	ent1.Delete();
		// 	resEnt = query.Single();
		// 	Assert.Equal(ent2.ID, resEnt.ID);
		// }
    }
}
