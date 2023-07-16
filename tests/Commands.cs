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
		// 	var world = World.New();
		// 	var cmd = new Commands(world);

		// 	var e = cmd.Spawn();

		// 	Assert.True(e.IsAlive());
		// 	Assert.True(e.IsEnabled());
		// }

		// [Fact]
		// public void Destroy_Entity_Buffered()
		// {
		// 	var world = World.New();
		// 	var cmd = new Commands(world);

		// 	var e = cmd.Spawn();
		// 	e.Despawn();

		// 	Assert.False(e.IsAlive());
		// }

		[Fact]
		public void Merge_Create_Entity()
		{
			var world = World.New();
			var cmd = new Commands(world);

			var e = cmd.Spawn();

			cmd.Merge();

			Assert.True(cmd.Main.IsAlive(e.ID));
			Assert.True(cmd.Main.Has<EcsEnabled>(e.ID));
		}

		[Fact]
		public void Merge_Destroy_Entity()
		{
			var world = World.New();
			var cmd = new Commands(world);

			var e = cmd.Spawn();
			e.Despawn();
			cmd.Merge();

			Assert.False(cmd.Main.IsAlive(e.ID));
		}

		[Fact]
		public void Merge_SetComponent_Entity()
		{
			var world = World.New();
			var cmd = new Commands(world);

			var e = world.Spawn();

			const float VAL = 0.012344f;
			cmd.Set<float>(e, VAL);
			cmd.Merge();

			Assert.True(e.Has<float>());
			Assert.Equal(VAL, e.Get<float>());
		}

		[Fact]
		public void Merge_UnsetComponent_Entity()
		{
			var world = World.New();
			var cmd = new Commands(world);

			const float VAL = 0.012344f;

			var e = world.Spawn();
			e.Set<float>(VAL);

			cmd.Unset<float>(e);
			cmd.Merge();

			Assert.False(e.Has<float>());
		}
	}
}
