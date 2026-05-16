namespace TinyEcs.Tests
{
	public class TypedRelationshipsTest
	{
		private struct Owns { }
		private struct LikesEntity { }

		[Fact]
		public void MultipleKindsLiveSideBySide()
		{
			using var ctx = new Context();
			var world = ctx.World;

			var a = world.Entity();
			var b = world.Entity();
			var c = world.Entity();

			world.AddChild<Owns>(a.ID, b.ID);
			world.AddChild<LikesEntity>(a.ID, c.ID);

			var ownsChildren = world.GetChildren<Owns>(a.ID);
			var likesChildren = world.GetChildren<LikesEntity>(a.ID);

			Assert.NotNull(ownsChildren);
			Assert.NotNull(likesChildren);
			Assert.Single(ownsChildren);
			Assert.Single(likesChildren);
			Assert.Equal(b.ID, ownsChildren[0]);
			Assert.Equal(c.ID, likesChildren[0]);

			Assert.Equal(a.ID, world.GetParent<Owns>(b.ID));
			Assert.Equal(a.ID, world.GetParent<LikesEntity>(c.ID));

			// default (non-generic) mapper is independent
			Assert.Equal(0ul, world.GetParent(b.ID));
			Assert.Equal(0ul, world.GetParent(c.ID));
		}

		[Fact]
		public void TypedRelationshipUnsetsOnRemove()
		{
			using var ctx = new Context();
			var world = ctx.World;

			var parent = world.Entity();
			var child = world.Entity();

			world.AddChild<Owns>(parent.ID, child.ID);
			Assert.Equal(parent.ID, world.GetParent<Owns>(child.ID));
			Assert.True(world.Has<Parent<Owns>>(child.ID));
			Assert.True(world.Has<Children<Owns>>(parent.ID));

			world.RemoveChild<Owns>(child.ID);

			Assert.Equal(0ul, world.GetParent<Owns>(child.ID));
			Assert.False(world.Has<Parent<Owns>>(child.ID));
			// Children component on parent should be unset since list is empty
			Assert.False(world.Has<Children<Owns>>(parent.ID));
		}

		[Fact]
		public void DeferredTypedRelationshipAppliesAtMerge()
		{
			using var ctx = new Context();
			var world = ctx.World;

			var parent = world.Entity();
			var child = world.Entity();

			world.BeginDeferred();
			world.AddChild<Owns>(parent.ID, child.ID);

			// During deferred scope the mapper hasn't applied yet
			Assert.Equal(0ul, world.GetParent<Owns>(child.ID));

			world.EndDeferred();

			// After merge, mapping is visible
			Assert.Equal(parent.ID, world.GetParent<Owns>(child.ID));
			var children = world.GetChildren<Owns>(parent.ID);
			Assert.NotNull(children);
			Assert.Single(children);
			Assert.Equal(child.ID, children[0]);
		}
	}
}
