namespace TinyEcs.Tests
{
    public class Relation
    {
        [Fact]
        public void Relation_Child_Attach_To_Parent()
        {
            using var world = new World();

            var parent = world.Entity();
            var child = world.Entity();

            child.ChildOf(parent);

            Assert.True(child.Get<EcsChildOf, EntityView>().Target == parent);
        }

        [Fact]
        public void Relation_Child_Detach_From_Parent()
        {
            using var world = new World();

            var parent = world.Entity();
            var child = world.Entity();

            child.ChildOf(parent);

            child.Unset<EcsChildOf, EntityView>();

            Assert.False(child.Has<EcsChildOf, EntityView>());
        }

        [Fact]
        public void Relation_Child_Switch_Parent()
        {
            using var world = new World();

            var parent = world.Entity();
            var child0 = world.Entity();
            var child1 = world.Entity();

            child0.ChildOf(parent);
            child0.Unset<EcsChildOf, EntityView>();

            child1.ChildOf(parent);

            Assert.False(child0.Has<EcsChildOf, EntityView>());
            Assert.True(child1.Has<EcsChildOf, EntityView>());
        }
    }
}
