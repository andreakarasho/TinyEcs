namespace TinyEcs.Tests
{
    public class WorldTest
    {
        [Fact]
        public void World_Create_Destroy()
        {
            var world = new World();
            world.CreateEntity();
            world.Dispose();

            Assert.Equal(0, world.EntityCount);
        }
    }
}
