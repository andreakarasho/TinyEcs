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

			var e = cmd.Entity();

			Assert.True(e.IsAlive());
			Assert.True(e.IsEnabled());
		}

		[Fact]
		public void Destroy_Entity_Buffered()
		{
			using var world = new World();
			using var cmd = new Commands(world);

			var e = cmd.Entity();
			e.Destroy();

			Assert.False(e.IsAlive());
		}

		[Fact]
		public void Merge_Create_Entity()
		{
			using var world = new World();
			using var cmd = new Commands(world);

			var count = world.EntityCount;
			var e = cmd.Entity();

			cmd.MergeChanges();

			Assert.True(count < world.EntityCount);
		}

		[Fact]
		public void Merge_Destroy_Entity()
		{
			using var world = new World();
			using var cmd = new Commands(world);

			var count = world.EntityCount;
			var e = cmd.Entity();
			e.Destroy();

			cmd.MergeChanges();

			Assert.True(count >= world.EntityCount);
		}
	}
}
