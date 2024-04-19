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

			carl.Set<Likes, Dogs>();
			carl.Unset<Likes, Dogs>();
			Assert.False(carl.Has<Likes, Dogs>());
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

		[Fact]
		public void Relation_ComponentWildcard()
		{
			using var ctx = new Context();

			var carl = ctx.World.Entity();
			var likes = ctx.World.Entity();
			var dogs = ctx.World.Entity();
			var cats = ctx.World.Entity();

			carl.Set(likes, dogs);
			carl.Set(likes, cats);

			using var query = ctx.World.QueryBuilder()
				.With(likes, Wildcard.ID)
				.Build();

			Assert.Equal(1, query.Count());

			query.Each((EntityView ent) => {
				Assert.Equal(carl.ID, ent.ID);
			});
		}

		[Fact]
		public void Relation_WildcardComponent()
		{
			using var ctx = new Context();

			var carl = ctx.World.Entity();
			var bob = ctx.World.Entity();
			var likes = ctx.World.Entity();
			var dogs = ctx.World.Entity();
			var cats = ctx.World.Entity();

			carl.Set(likes, dogs);
			bob.Set(likes, cats);

			using var query = ctx.World.QueryBuilder()
				.With(Wildcard.ID, dogs)
				.Build();

			Assert.Equal(1, query.Count());

			query.Each((EntityView ent) => {
				Assert.Equal(carl.ID, ent.ID);
			});
		}

		[Fact]
		public void Relation_WildcardWildcard()
		{
			using var ctx = new Context();

			var query = ctx.World.QueryBuilder()
				.With(Wildcard.ID, Wildcard.ID)
				.Build();

			var initCount = query.Count();

			var carl = ctx.World.Entity();
			var bob = ctx.World.Entity();
			var likes = ctx.World.Entity();
			var dogs = ctx.World.Entity();
			var cats = ctx.World.Entity();

			carl.Set(likes, dogs);
			bob.Set(likes, cats);

			query = ctx.World.QueryBuilder()
				.With(Wildcard.ID, Wildcard.ID)
				.Build();

			Assert.Equal(2 + initCount, query.Count());

			query.Dispose();
		}

		[Fact]
		public void Relation_Unique()
		{
			using var ctx = new Context();

			var josh = ctx.World.Entity();

			ctx.World.BeginDeferred();
			var carl = ctx.World.Entity();
			var likes = ctx.World.Entity().Set<Unique>();
			var dogs = ctx.World.Entity();
			var cats = ctx.World.Entity();
			var pasta = ctx.World.Entity();

			carl.Set(likes, dogs);
			Assert.True(carl.Has(likes, dogs));

			carl.Set(likes, pasta);
			Assert.False(carl.Has(likes, dogs));
			Assert.True(carl.Has(likes, pasta));

			josh.Set(likes, dogs);
			Assert.True(josh.Has(likes, dogs));

			ctx.World.EndDeferred();

			carl.Set(likes, cats);
			Assert.False(carl.Has(likes, dogs));
			Assert.False(carl.Has(likes, pasta));
			Assert.True(carl.Has(likes, cats));
			Assert.True(josh.Has(likes, dogs));
		}

		[Fact]
		public void Relation_Symmetric()
		{
			using var ctx = new Context();

			var carl = ctx.World.Entity();
			var tradeWith = ctx.World.Entity().Set<Symmetric>();
			var bob = ctx.World.Entity();

			carl.Set(tradeWith, bob);
			Assert.True(carl.Has(tradeWith, bob));
			Assert.True(bob.Has(tradeWith, carl));
		}

		[Fact]
		public void Relation_SymmetricDeferred()
		{
			using var ctx = new Context();

			ctx.World.BeginDeferred();
			var carl = ctx.World.Entity();
			var tradeWith = ctx.World.Entity().Set<Symmetric>();
			var bob = ctx.World.Entity();

			carl.Set(tradeWith, bob);
			Assert.True(carl.Has(tradeWith, bob));
			Assert.True(bob.Has(tradeWith, carl));
			ctx.World.EndDeferred();

			Assert.True(carl.Has(tradeWith, bob));
			Assert.True(bob.Has(tradeWith, carl));
		}
    }
}
