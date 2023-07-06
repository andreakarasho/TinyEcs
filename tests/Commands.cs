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
		public void Create_Entity_Buffered()
		{
			using var world = new World();
			using var cmd = new Commands(world);

			var e = cmd.Spawn();

			Assert.True(e.IsAlive());
			Assert.True(e.IsEnabled());
		}

		[Fact]
		public void Destroy_Entity_Buffered()
		{
			using var world = new World();
			using var cmd = new Commands(world);

			var e = cmd.Spawn();
			e.Destroy();

			Assert.False(e.IsAlive());
		}

		[Fact]
		public void Merge_Create_Entity()
		{
			using var world = new World();
			using var cmd = new Commands(world);

			var count = world.EntityCount;
			var e = cmd.Spawn();

			cmd.Merge();

			Assert.True(count < world.EntityCount);
		}

		[Fact]
		public void Merge_Destroy_Entity()
		{
			using var world = new World();
			using var cmd = new Commands(world);

			var count = world.EntityCount;
			var e = cmd.Spawn();
			e.Destroy();

			cmd.Merge();

			Assert.True(count >= world.EntityCount);
		}

		[Fact]
		public void Merge_SetComponent_Entity()
		{
			using var world = new World();
			using var cmd = new Commands(world);

			var e = world.Spawn();

			const float VAL = 0.012344f;
			cmd.Set<float>(e, VAL);
			cmd.Merge();

			Assert.True(e.Has<float>());
			Assert.True(e.Get<float>() == VAL);
		}

		[Fact]
		public void Merge_UnsetComponent_Entity()
		{
			using var world = new World();
			using var cmd = new Commands(world);

			const float VAL = 0.012344f;

			var e = world.Spawn();
			e.Set<float>(VAL);

			cmd.Unset<float>(e);
			cmd.Merge();

			Assert.True(!e.Has<float>());
		}
	}
}
