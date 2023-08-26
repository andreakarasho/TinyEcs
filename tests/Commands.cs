using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyEcs.Tests
{
	public class CommandsTest
	{
		// [Fact]
		// public void Create_Entity_Buffered()
		// {
		// 	using var world = new World<TContext>();
		// 	var cmd = new Commands<TContext>(world);

		// 	var e = cmd.New();

		// 	Assert.True(e.IsAlive());
		// 	Assert.True(e.IsEnabled());
		// }

		// [Fact]
		// public void Destroy_Entity_Buffered()
		// {
		// 	using var world = new World<TContext>();
		// 	var cmd = new Commands<TContext>(world);

		// 	var e = cmd.New();
		// 	e.Delete();

		// 	Assert.False(e.IsAlive());
		// }

		[Fact]
		public void Merge_Create_Entity<TContext>()
		{
			using var world = new World<TContext>();
			var cmd = new Commands<TContext>(world);

			var e = cmd.Entity();

			cmd.Merge();

			Assert.True(world.Exists(e.ID));
			Assert.False(world.Has<EcsDisabled>(e.ID));
		}

		[Fact]
		public void Merge_Destroy_Entity<TContext>()
		{
			using var world = new World<TContext>();
			var cmd = new Commands<TContext>(world);

			var e = cmd.Entity();
			e.Delete();
			cmd.Merge();

			Assert.False(world.Exists(e.ID));
		}

		[Fact]
		public void Merge_SetComponent_Entity<TContext>()
		{
			using var world = new World<TContext>();
			var cmd = new Commands<TContext>(world);

			var e = world.Entity();

			const float VAL = 0.012344f;
			cmd.Set<FloatComponent>(e, new FloatComponent() { Value = VAL });
			cmd.Merge();

			Assert.True(e.Has<FloatComponent>());
			Assert.Equal(VAL, e.Get<FloatComponent>().Value);
		}

		[Fact]
		public void Merge_UnsetComponent_Entity<TContext>()
		{
			using var world = new World<TContext>();
			var cmd = new Commands<TContext>(world);

			const float VAL = 0.012344f;

			var e = world.Entity();
			e.Set<FloatComponent>(new FloatComponent() { Value = VAL });

			cmd.Unset<FloatComponent>(e);
			cmd.Merge();

			Assert.False(e.Has<FloatComponent>());
		}
	}
}
