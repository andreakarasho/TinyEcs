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

            for (int i = 0; i < amount; i++)
                ctx.World.Set(ctx.World.Entity(), new FloatComponent());

            var query = ctx.World.Query().With<FloatComponent>();

            int done = 0;
            query.Iterate((ref Iterator it) => done += it.Count);

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

            var query = ctx.World.Query().With<FloatComponent>().With<IntComponent>();

            int done = 0;
            query.Iterate((ref Iterator it) => done += it.Count);

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(1_000_000)]
        public void Query_AttachThreeComponents_WithThreeComponents(int amount)
        {
            using var ctx = new Context();

            for (int i = 0; i < amount; i++)
            {
                var e = ctx.World.Entity();
                ctx.World.Set(e, new FloatComponent());
                ctx.World.Set(e, new IntComponent());
                ctx.World.Set(e, new BoolComponent());
            }

            var query = ctx.World
                .Query()
                .With<FloatComponent>()
                .With<IntComponent>()
                .With<BoolComponent>();

            int done = 0;
            query.Iterate((ref Iterator it) => done += it.Count);

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

            var query = ctx.World
                .Query()
                .With<FloatComponent>()
                .With<IntComponent>()
                .Without<BoolComponent>();

            int done = 0;
            query.Iterate((ref Iterator it) => done += it.Count);

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

            var query = ctx.World
                .Query()
                .With<FloatComponent>()
                .With<IntComponent>()
                .Without<BoolComponent>();

            int done = 0;
            query.Iterate((ref Iterator it) => done += it.Count);

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

            var query = ctx.World
                .Query()
                .With<FloatComponent>()
                .Without<IntComponent>()
                .Without<BoolComponent>();

            int done = 0;
            query.Iterate((ref Iterator it) => done += it.Count);

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

            var e2 = ctx.World.Entity();
            ctx.World.Set(e2, new FloatComponent());
            ctx.World.Set(e2, new IntComponent());
            ctx.World.Set(e2, new BoolComponent());

            var e3 = ctx.World.Entity();
            ctx.World.Set(e3, new FloatComponent());
            ctx.World.Set(e3, new IntComponent());
            ctx.World.Set(e3, new BoolComponent());
            good++;

            var e4 = ctx.World.Entity();
            ctx.World.Set(e4, new FloatComponent());
            ctx.World.Set(e4, new IntComponent());
            ctx.World.Set(e4, new BoolComponent());
            ctx.World.Set<NormalTag>(e4);

            var query = ctx.World
                .Query()
                .With<FloatComponent>()
                .With<IntComponent>()
                .Without<BoolComponent>()
                .Without<NormalTag>();

            int done = 0;
            query.Iterate((ref Iterator it) => done += it.Count);

            Assert.Equal(good, done);
        }
    }
}
