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
                world.Set<float>(world.CreateEntity());

            var query = world.Query()
                .With<float>();

            int done = 0;
            foreach (var it in query)
            {
                done += it.Count;

                Assert.True(it.Has<float>());
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
                world.Set<float>(e);
                world.Set<int>(e);
            }

            var query = world.Query()
                .With<float>()
                .With<int>();

            int done = 0;
            foreach (var it in query)
            {
                done += it.Count;

                Assert.True(it.Has<float>());
                Assert.True(it.Has<int>());
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
                world.Set<float>(e);
                world.Set<int>(e);
                world.Set<bool>(e);
            }

            var query = world.Query()
                .With<float>()
                .With<int>()
                .With<bool>();

            int done = 0;
            foreach (var it in query)
            {
                done += it.Count;

                Assert.True(it.Has<float>());
                Assert.True(it.Has<int>());
                Assert.True(it.Has<bool>());
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
                world.Set<float>(e);
                world.Set<int>(e);
                world.Set<bool>(e);
            }

            var query = world.Query()
                .With<float>()
                .With<int>()
                .Without<bool>();

            int done = 0;
            foreach (var it in query)
            {
                done += it.Count;

                Assert.True(it.Has<float>());
                Assert.True(it.Has<int>());
                Assert.False(it.Has<bool>());
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
                world.Set<float>(e);
                world.Set<int>(e);
            }

            var query = world.Query()
                .With<float>()
                .With<int>()
                .Without<bool>();

            int done = 0;
            foreach (var it in query)
            {
                done += it.Count;

                Assert.True(it.Has<float>());
                Assert.True(it.Has<int>());
                Assert.False(it.Has<bool>());
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
                world.Set<float>(e);
                world.Set<int>(e);
            }

            var query = world.Query()
                .With<float>()
                .Without<int>()
                .Without<bool>();

            int done = 0;
            foreach (var it in query)
            {
                done += it.Count;

                Assert.True(it.Has<float>());
                Assert.False(it.Has<int>());
                Assert.False(it.Has<bool>());  
            }

            Assert.Equal(0, done);
        }

        [Fact]
        public void Query_EdgeValidation()
        {
            using var world = new World();

            var good = 0;

            var e = world.CreateEntity();
            world.Set<float>(e);
            world.Set<int>(e);

            var e2 = world.CreateEntity();
            world.Set<float>(e2);
            world.Set<int>(e2);
            world.Set<bool>(e2);

            var e3 = world.CreateEntity();
            world.Set<float>(e3);
            world.Set<int>(e3);
            world.Set<byte>(e3);
            good++;

            var e4 = world.CreateEntity();
            world.Set<float>(e4);
            world.Set<int>(e4);
            world.Set<byte>(e4);
            world.Set<decimal>(e4);

            var query = world.Query()
                .With<float>()
                .With<byte>()
                .Without<bool>()
                .Without<decimal>();

            int done = 0;
            foreach (var it in query)
            {
                done += it.Count;

                Assert.True(it.Has<float>());
                Assert.True(it.Has<byte>());
                Assert.False(it.Has<bool>());
            }

            Assert.Equal(good, done);
        }
    }
}
