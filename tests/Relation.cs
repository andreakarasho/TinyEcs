// namespace TinyEcs.Tests
// {
//     public class Relation
//     {
// 		[Fact]
// 		public void Attach_ChildOf()
// 		{
// 			using var ctx = new Context();
//
// 			var root = ctx.World.Entity();
// 			var child = ctx.World.Entity();
//
// 			child.ChildOf(root);
//
// 			Assert.True(ctx.World.Has<EcsChildOf>(child, root));
// 		}
//
// 		[Fact]
// 		public void Detach_ChildOf()
// 		{
// 			using var ctx = new Context();
//
// 			var root = ctx.World.Entity();
// 			var child = ctx.World.Entity();
//
// 			child.ChildOf(root);
// 			child.Unset<EcsChildOf>(root);
//
// 			Assert.False(ctx.World.Has<EcsChildOf>(child, root));
// 		}
//
// 		[Fact]
// 		public void Count_Children()
// 		{
// 			using var ctx = new Context();
//
// 			var root = ctx.World.Entity();
//
// 			var count = 100;
// 			for (int i = 0; i < count; ++i)
// 				ctx.World.Entity().ChildOf(root);
//
// 			var done = 0;
// 			root.Children(s => done += 1);
//
// 			Assert.Equal(count, done);
// 		}
//
// 		[Fact]
// 		public void Clear_Children()
// 		{
// 			using var ctx = new Context();
//
// 			var root = ctx.World.Entity();
//
// 			var count = 100;
// 			for (int i = 0; i < count; ++i)
// 				ctx.World.Entity().ChildOf(root);
//
// 			root.ClearChildren();
// 			var done = 0;
// 			root.Children(s => done += 1);
//
// 			Assert.Equal(0, done);
// 		}
//
// 		[Fact]
// 		public void Exclusive_Relation()
// 		{
// 			using var ctx = new Context();
//
// 			var root = ctx.World.Entity();
// 			var platoonCmp = ctx.World.Entity().Set<EcsExclusive>();
// 			var platoon1 = ctx.World.Entity();
// 			var platoon2 = ctx.World.Entity();
// 			var unit = ctx.World.Entity();
//
// 			unit.Set(platoonCmp, platoon1);
// 			Assert.True(ctx.World.Has(unit.ID, platoonCmp.ID, platoon1.ID));
//
// 			unit.Set(platoonCmp, platoon2);
// 			Assert.False(ctx.World.Has(unit.ID, platoonCmp.ID, platoon1.ID));
// 			Assert.True(ctx.World.Has(unit.ID, platoonCmp.ID, platoon2.ID));
// 		}
//     }
// }
