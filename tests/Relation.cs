namespace TinyEcs.Tests
{
    public class Relation
    {
        [Fact]
        public void Relation_Child_Attach_To_Parent()
        {
            using var world = new World();

            var parent = world.Spawn();
            var child = world.Spawn();

            child.AttachTo(parent);

            Assert.Equal(child.Get<EcsChild>().Parent, parent);
        }

        [Fact]
        public void Relation_Child_Detach_From_Parent()
        {
            using var world = new World();

            var parent = world.Spawn();
            var child = world.Spawn();

            child.AttachTo(parent);
            child.Detach();

            Assert.False(child.Has<EcsChild>());
        }

        [Fact]
        public void Relation_Child_Switch_Parent()
        {
            using var world = new World();

            var parent1 = world.Spawn();
            var parent2 = world.Spawn();
            var child = world.Spawn();

            child.AttachTo(parent1);
            child.AttachTo(parent2);

            Assert.False(parent1.Has<EcsParent>());
            Assert.True(parent2.Has<EcsParent>());
            Assert.Equal(parent2.Get<EcsParent>().FirstChild, child);
        }

        [Fact]
        public void Relation_Attach_Multiple_Child_To_A_Parent_Then_Cleanup()
        {
            using var world = new World();

            var parent = world.Spawn();
            var count = 1000;

            for (int i = 0; i < count; ++i)
                world.Spawn().AttachTo(parent);

            Assert.Equal(parent.Get<EcsParent>().ChildrenCount, count);

            parent.RemoveChildren();

            Assert.False(parent.Has<EcsParent>());
        }
    }
}
