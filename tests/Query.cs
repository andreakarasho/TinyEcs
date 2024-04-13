﻿using System.Runtime.InteropServices;

namespace TinyEcs.Tests
{
    public class QueryTest
    {
        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(1_000_000)]
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
        [InlineData(1_000_000)]
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
        [InlineData(1_000_000)]
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
        [InlineData(1_000_000)]
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

            var done = ctx.World.Query<(FloatComponent, IntComponent), Not<BoolComponent>>().Count();

            Assert.Equal(0, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(1_000_000)]
        public void Query_AttachTwoComponents_WithTwoComponents_WithoutOneComponent(int amount)
        {
            using var ctx = new Context();

            for (int i = 0; i < amount; i++)
            {
                var e = ctx.World.Entity();
                ctx.World.Set(e, new FloatComponent());
                ctx.World.Set(e, new IntComponent());
            }

            var done = ctx.World.Query<(FloatComponent, IntComponent), Not<BoolComponent>>().Count();

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(1_000_000)]
        public void Query_AttachTwoComponents_WithOneComponents_WithoutTwoComponent(int amount)
        {
            using var ctx = new Context();

            for (int i = 0; i < amount; i++)
            {
                var e = ctx.World.Entity();
                ctx.World.Set(e, new FloatComponent());
                ctx.World.Set(e, new IntComponent());
            }

            var done = ctx.World.Query<FloatComponent, (Not<IntComponent>, Not<IntComponent>)>().Count();

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

            var done = ctx.World.Query<(FloatComponent, IntComponent), (Not<BoolComponent>, Not<NormalTag>)>().Count();

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

			var result = ctx.World.Query<(With<FloatComponent>, With<IntComponent>, With<NormalTag>)>()
				.Single();

			Assert.Equal(singleton.ID, result.ID);

			var singleton2 = ctx.World.Entity()
				.Set(new FloatComponent())
				.Set(new IntComponent())
				.Set<NormalTag>();

			Assert.Throws<Exception>(() =>
				ctx.World.Query<(With<FloatComponent>, With<IntComponent>, With<NormalTag>)>().Single()
			);
		}
    }
}
