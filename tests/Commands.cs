using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyEcs.Tests
{
	public class CommandsTest
	{
		[Fact]
		public void Merge_Create_Entity()
		{
			using var ctx = new Context();
			var cmd = new Commands(ctx.World);

			var e = cmd.Entity();

			cmd.Merge();

			Assert.True(ctx.World.Exists(e.ID));
			Assert.False(ctx.World.Has<EcsDisabled>(e.ID));
		}

		[Fact]
		public void Merge_Destroy_Entity()
		{
			using var ctx = new Context();
			var cmd = new Commands(ctx.World);

			var e = cmd.Entity();
			e.Delete();
			cmd.Merge();

			Assert.False(ctx.World.Exists(e.ID));
		}

		[Fact]
		public void Merge_SetComponent_Entity()
		{
			using var ctx = new Context();
			var cmd = new Commands(ctx.World);

			var e = ctx.World.Entity();

			const float VAL = 0.012344f;
			cmd.Set<FloatComponent>(e, new FloatComponent() { Value = VAL });
			cmd.Merge();

			Assert.True(e.Has<FloatComponent>());
			Assert.Equal(VAL, e.Get<FloatComponent>().Value);
		}

		[Fact]
		public void Merge_UnsetComponent_Entity()
		{
			using var ctx = new Context();
			var cmd = new Commands(ctx.World);

			const float VAL = 0.012344f;

			var e = ctx.World.Entity();
			e.Set<FloatComponent>(new FloatComponent() { Value = VAL });

			cmd.Unset<FloatComponent>(e);
			cmd.Merge();

			Assert.False(e.Has<FloatComponent>());
		}
	}
}
