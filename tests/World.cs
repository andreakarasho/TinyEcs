namespace TinyEcs.Tests
{
    public class WorldTest
    {
        [Fact]
        public void World_Create_Destroy<TContext>()
        {
            using var world = new World<TContext>();
            world.Spawn();
            world.Dispose();

            Assert.Equal(0, world.EntityCount);
        }

        //[Theory]
        //[InlineData(1)]
        //[InlineData(10_000)]
        //[InlineData(1_000_000)]
        //public async void World_Create_Destroy_Threading(int times)
        //{
        //    using var world = new World<TContext>();

        //    var list = new List<Task>();
        //    for (var i = 0; i < times;i++)
        //    {
        //        list.Add(Task.Run(world.CreateEntity));
        //    }

        //    await Task.WhenAll(list.ToArray());

        //    world.Dispose();

        //    Assert.Equal(0, world.EntityCount);
        //}
    }
}
