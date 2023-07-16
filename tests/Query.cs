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
            var world = new World();

            for (int i = 0; i < amount; i++)
                world.Set<float>(world.Spawn());

            var query = world.Query()
                .With<float>();

            int done = 0;
            foreach (var it in query)
            {
                done += it.Count;

                _ = it.Field<float>();
            }

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(1_000_000)]
        public void Query_AttachTwoComponents_WithTwoComponents(int amount)
        {
            var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Spawn();
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

                _ = it.Field<float>();
                _ = it.Field<int>();
            }

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(1_000_000)]
        public void Query_AttachThreeComponents_WithThreeComponents(int amount)
        {
            var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Spawn();
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

                _ = it.Field<float>();
                _ = it.Field<int>();
                _ = it.Field<bool>();
            }

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(1_000_000)]
        public void Query_AttachThreeComponents_WithTwoComponents_WithoutOneComponent(int amount)
        {
            var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Spawn();
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

                _ = it.Field<float>();
                _ = it.Field<int>();
            }

            Assert.Equal(0, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(1_000_000)]
        public void Query_AttachTwoComponents_WithTwoComponents_WithoutOneComponent(int amount)
        {
            var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Spawn();
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

                _ = it.Field<float>();
                _ = it.Field<int>();
            }

            Assert.Equal(amount, done);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        [InlineData(1_000_000)]
        public void Query_AttachTwoComponents_WithOneComponents_WithoutTwoComponent(int amount)
        {
            var world = new World();

            for (int i = 0; i < amount; i++)
            {
                var e = world.Spawn();
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

                _ = it.Field<float>();
            }

            Assert.Equal(0, done);
        }

        [Fact]
        public void Query_EdgeValidation()
        {
            var world = new World();

            var good = 0;

            var e = world.Spawn();
            world.Set<float>(e);
            world.Set<int>(e);

            var e2 = world.Spawn();
            world.Set<float>(e2);
            world.Set<int>(e2);
            world.Set<bool>(e2);

            var e3 = world.Spawn();
            world.Set<float>(e3);
            world.Set<int>(e3);
            world.Set<byte>(e3);
            good++;

            var e4 = world.Spawn();
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

                _ = it.Field<float>();
                _ = it.Field<byte>();
            }

            Assert.Equal(good, done);
        }
    }
}
