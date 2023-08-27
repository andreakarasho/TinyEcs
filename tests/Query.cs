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
                ctx.World.Set<FloatComponent>(ctx.World.Entity());

            var query = ctx.World.Query()
                .With<FloatComponent>();

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
                ctx.World.Set<FloatComponent>(e);
                ctx.World.Set<IntComponent>(e);
            }

            var query = ctx.World.Query()
                .With<FloatComponent>()
                .With<IntComponent>();

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
                ctx.World.Set<FloatComponent>(e);
                ctx.World.Set<IntComponent>(e);
                ctx.World.Set<BoolComponent>(e);
            }

            var query = ctx.World.Query()
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
                ctx.World.Set<FloatComponent>(e);
                ctx.World.Set<IntComponent>(e);
                ctx.World.Set<BoolComponent>(e);
            }

            var query = ctx.World.Query()
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
                ctx.World.Set<FloatComponent>(e);
                ctx.World.Set<IntComponent>(e);
            }

            var query = ctx.World.Query()
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
                ctx.World.Set<FloatComponent>(e);
                ctx.World.Set<IntComponent>(e);
            }

            var query = ctx.World.Query()
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
            ctx.World.Set<FloatComponent>(e);
            ctx.World.Set<IntComponent>(e);

            var e2 = ctx.World.Entity();
            ctx.World.Set<FloatComponent>(e2);
            ctx.World.Set<IntComponent>(e2);
            ctx.World.Set<BoolComponent>(e2);

            var e3 = ctx.World.Entity();
            ctx.World.Set<FloatComponent>(e3);
            ctx.World.Set<IntComponent>(e3);
            ctx.World.Set<BoolComponent>(e3);
            good++;

            var e4 = ctx.World.Entity();
            ctx.World.Set<FloatComponent>(e4);
            ctx.World.Set<IntComponent>(e4);
            ctx.World.Set<BoolComponent>(e4);
            ctx.World.Set<NormalTag>(e4);

            var query = ctx.World.Query()
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
