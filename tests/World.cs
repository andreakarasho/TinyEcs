namespace TinyEcs.Tests
{
    public class WorldTest
    {
        [Fact]
        public void World_Create_Destroy()
        {
            using var ctx = new Context();
            ctx.World.Entity();
            ctx.World.Dispose();

            Assert.Equal(0, ctx.World.EntityCount);
        }

        //[Theory]
        //[InlineData(1)]
        //[InlineData(10_000)]
        //[InlineData(1_000_000)]
        //public async void World_Create_Destroy_Threading(int times)
        //{
        //    using var ctx = new Context();

        //    var list = new List<Task>();
        //    for (var i = 0; i < times;i++)
        //    {
        //        list.Add(Task.Run(ctx.World.CreateEntity));
        //    }

        //    await Task.WhenAll(list.ToArray());

        //    ctx.World.Dispose();

        //    Assert.Equal(0, ctx.World.EntityCount);
        //}
    }
}
