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
                world.Attach<float>(world.CreateEntity());

            var query = world.Query()
                .With<float>()
                .End();

            int done = 0;
            foreach (var _ in query)
            {
                ++done;
            }

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
                var e = world.CreateEntity();
                world.Attach<float>(e);
                world.Attach<int>(e);
            }

            var query = world.Query()
                .With<float>()
                .With<int>()
                .End();

            int done = 0;
            foreach (var _ in query)
            {
                ++done;
            }

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
                var e = world.CreateEntity();
                world.Attach<float>(e);
                world.Attach<int>(e);
                world.Attach<bool>(e);
            }

            var query = world.Query()
                .With<float>()
                .With<int>()
                .With<bool>()
                .End();

            int done = 0;
            foreach (var _ in query)
            {
                ++done;
            }

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
                var e = world.CreateEntity();
                world.Attach<float>(e);
                world.Attach<int>(e);
                world.Attach<bool>(e);
            }

            var query = world.Query()
                .With<float>()
                .With<int>()
                .Without<bool>()
                .End();

            int done = 0;
            foreach (var _ in query)
            {
                ++done;
            }

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
                var e = world.CreateEntity();
                world.Attach<float>(e);
                world.Attach<int>(e);
            }

            var query = world.Query()
                .With<float>()
                .With<int>()
                .Without<bool>()
                .End();

            int done = 0;
            foreach (var _ in query)
            {
                ++done;
            }

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
                var e = world.CreateEntity();
                world.Attach<float>(e);
                world.Attach<int>(e);
            }

            var query = world.Query()
                .With<float>()
                .Without<int>()
                .Without<bool>()
                .End();

            int done = 0;
            foreach (var _ in query)
            {
                ++done;
            }

            Assert.Equal(0, done);
        }
    }
}
