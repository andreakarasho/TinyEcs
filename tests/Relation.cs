namespace TinyEcs.Tests
{
    public class Relation
    {
		struct Likes { }
		struct Dogs { }
		struct Apples { public int Amount; }


		[Fact]
		public void Relation_SetComponentAndTag()
		{
			using var ctx = new Context();

			var root = ctx.World.Entity();

			root.Set<Likes, Dogs>();
			Assert.True(root.Has<Likes, Dogs>());

			root.Set<Likes, Apples>(new Apples() { Amount = 10 });
			Assert.True(root.Has<Likes, Apples>());
			Assert.Equal(10, root.Get<Likes, Apples>().Amount);

			root.Get<Likes, Apples>().Amount += 10;
			Assert.Equal(20, root.Get<Likes, Apples>().Amount);
		}

		[Fact]
		public void Relation_Unset()
		{
			using var ctx = new Context();

			var carl = ctx.World.Entity();
			var likes = ctx.World.Entity();
			var dogs = ctx.World.Entity();

			carl.Set(likes, dogs);
			carl.Unset(likes, dogs);
			Assert.False(carl.Has(likes, dogs));
		}

		[Fact]
		public void Relation_SetRelationAndComponent()
		{
			using var ctx = new Context();

			var root = ctx.World.Entity();

			root.Set(new Apples() { Amount = 99 });
			Assert.True(root.Has<Apples>());
			Assert.Equal(99, root.Get<Apples>().Amount);

			root.Set<Likes, Apples>(new Apples() { Amount = 10 });
			Assert.True(root.Has<Likes, Apples>());
			Assert.Equal(10, root.Get<Likes, Apples>().Amount);

			root.Get<Likes, Apples>().Amount += 10;
			Assert.Equal(20, root.Get<Likes, Apples>().Amount);
		}

		[Fact]
		public void Relation_GetTarget()
		{
			using var ctx = new Context();

			var carl = ctx.World.Entity();
			var likes = ctx.World.Entity();
			var dogs = ctx.World.Entity();
			var cats = ctx.World.Entity();

			carl.Set(likes, dogs);
			carl.Set(likes, cats);

			Assert.Equal(dogs, carl.Target(likes, 0));
			Assert.Equal(cats, carl.Target(likes, 1));
		}
    }
}
