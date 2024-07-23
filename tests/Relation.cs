﻿namespace TinyEcs.Tests
{
    public class Relation
    {
		struct Likes { }
		struct Dogs { }
		struct Apples { public int Amount; }
		struct EquippedItem { public byte Layer; }


		[Fact]
		public void Relation_SetComponentAndTag()
		{
			using var ctx = new Context();

			var root = ctx.World.Entity();

			root.Add<Likes, Dogs>();
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

			carl.Add(likes, dogs);
			carl.Unset(likes, dogs);
			Assert.False(carl.Has(likes, dogs));

			carl.Add<Likes, Dogs>();
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


			var item = ctx.World.Entity();
			item.Set<EquippedItem>(action: new EquippedItem() { Layer = 2 }, target: root);
			Assert.True(item.Has<EquippedItem>(root));
			Assert.Equal(2, item.Get<EquippedItem>(root).Layer);
			item.Get<EquippedItem>(root).Layer += 2;
			Assert.Equal(4, item.Get<EquippedItem>(root).Layer);
		}

		[Fact]
		public void Relation_GetTarget()
		{
			using var ctx = new Context();

			var carl = ctx.World.Entity();
			var likes = ctx.World.Entity();
			var dogs = ctx.World.Entity();
			var cats = ctx.World.Entity();

			carl.Add(likes, dogs);
			carl.Add(likes, cats);

			Assert.Equal(dogs, carl.Target(likes, 0));
			Assert.Equal(cats, carl.Target(likes, 1));
		}

		[Fact]
		public void Relation_GetAlive()
		{
			using var ctx = new Context();

			var a0 = ctx.World.Entity();
			var b0 = ctx.World.Entity();
			a0.AddChild(b0);
			a0.Delete();

			var a1 = ctx.World.Entity();
			var b1 = ctx.World.Entity();
			a1.AddChild(b1);

			var target = b1.Target<ChildOf>();
			var targetRaw = IDOp.GetPairSecond(IDOp.Pair(ctx.World.Entity<ChildOf>(), a1));

			Assert.True(ctx.World.Exists(target));
			Assert.False(ctx.World.Exists(targetRaw));
		}

		[Fact]
		public void Relation_ComponentWildcard()
		{
			using var ctx = new Context();

			var carl = ctx.World.Entity();
			var likes = ctx.World.Entity();
			var dogs = ctx.World.Entity();
			var cats = ctx.World.Entity();

			carl.Add(likes, dogs);
			carl.Add(likes, cats);

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

			carl.Add(likes, dogs);
			bob.Add(likes, cats);

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

			carl.Add(likes, dogs);
			bob.Add(likes, cats);

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
			Assert.True(josh.Exists());

			ctx.World.BeginDeferred();
			var carl = ctx.World.Entity();
			var likes = ctx.World.Entity().Rule<Unique>();
			var dogs = ctx.World.Entity();
			var cats = ctx.World.Entity();
			var pasta = ctx.World.Entity();

			Assert.True(carl.Exists());
			Assert.True(likes.Exists());
			Assert.True(dogs.Exists());
			Assert.True(cats.Exists());
			Assert.True(pasta.Exists());

			carl.Add(likes, dogs);
			Assert.False(carl.Has(likes, dogs));

			carl.Add(likes, pasta);
			Assert.False(carl.Has(likes, dogs));
			Assert.False(carl.Has(likes, pasta));

			josh.Add(likes, dogs);
			Assert.False(josh.Has(likes, dogs));

			ctx.World.EndDeferred();

			carl.Add(likes, cats);
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
			var tradeWith = ctx.World.Entity().Rule<Symmetric>();
			var bob = ctx.World.Entity();

			carl.Add(tradeWith, bob);
			Assert.True(carl.Has(tradeWith, bob));
			Assert.True(bob.Has(tradeWith, carl));
		}

		[Fact]
		public void Relation_SymmetricDeferred()
		{
			using var ctx = new Context();

			ctx.World.BeginDeferred();
			var carl = ctx.World.Entity();
			var tradeWith = ctx.World.Entity().Rule<Symmetric>();
			var bob = ctx.World.Entity();

			Assert.True(carl.Exists());
			Assert.True(tradeWith.Exists());
			Assert.True(bob.Exists());

			carl.Add(tradeWith, bob);
			Assert.False(carl.Has(tradeWith, bob));
			Assert.False(bob.Has(tradeWith, carl));
			ctx.World.EndDeferred();

			Assert.True(carl.Has(tradeWith, bob));
			Assert.True(bob.Has(tradeWith, carl));
		}

		[Fact]
		public void Relation_OnDelete_Delete()
		{
			using var ctx = new Context();

			var a = ctx.World.Entity();
			var relation = ctx.World.Entity().Add<OnDelete, Delete>();
			var b = ctx.World.Entity();

			a.Add(relation, b);
			b.Delete();

			Assert.False(a.Exists());
			Assert.False(b.Exists());
		}

		[Fact]
		public void Relation_OnDelete_Unset()
		{
			using var ctx = new Context();

			var a = ctx.World.Entity();
			var relation = ctx.World.Entity().Add<OnDelete, Unset>();
			var b = ctx.World.Entity();

			a.Add(relation, b);
			b.Delete();

			Assert.True(a.Exists());
			Assert.False(a.Has(relation, b));
		}

		[Fact]
		public void Relation_OnDelete_Panic()
		{
			using var ctx = new Context();

			var a = ctx.World.Entity();
			var relation = ctx.World.Entity().Add<OnDelete, Panic>();
			var b = ctx.World.Entity();

			a.Add(relation, b);
			Assert.ThrowsAny<Exception>(() => b.Delete());
		}
    }
}
