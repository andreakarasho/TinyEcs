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
            using var world = new World();

            for (int i = 0; i < amount; i++)
                world.Set<FloatComponent>(world.Entity());

            var query = world.Query()
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
            using var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Entity();
                world.Set<FloatComponent>(e);
                world.Set<IntComponent>(e);
            }

            var query = world.Query()
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
            using var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Entity();
                world.Set<FloatComponent>(e);
                world.Set<IntComponent>(e);
                world.Set<BoolComponent>(e);
            }

            var query = world.Query()
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
            using var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Entity();
                world.Set<FloatComponent>(e);
                world.Set<IntComponent>(e);
                world.Set<BoolComponent>(e);
            }

            var query = world.Query()
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
            using var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Entity();
                world.Set<FloatComponent>(e);
                world.Set<IntComponent>(e);
            }

            var query = world.Query()
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
            using var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Entity();
                world.Set<FloatComponent>(e);
                world.Set<IntComponent>(e);
            }

            var query = world.Query()
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
            using var world = new World();

            var good = 0;

            var e = world.Entity();
            world.Set<FloatComponent>(e);
            world.Set<IntComponent>(e);

            var e2 = world.Entity();
            world.Set<FloatComponent>(e2);
            world.Set<IntComponent>(e2);
            world.Set<BoolComponent>(e2);

            var e3 = world.Entity();
            world.Set<FloatComponent>(e3);
            world.Set<IntComponent>(e3);
            world.Set<BoolComponent>(e3);
            good++;

            var e4 = world.Entity();
            world.Set<FloatComponent>(e4);
            world.Set<IntComponent>(e4);
            world.Set<BoolComponent>(e4);
            world.Set<NormalTag>(e4);

            var query = world.Query()
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
