namespace TinyEcs.Tests
{
	public class ArchetypeTest
	{
		[Theory]
		[InlineData(10)]
        [InlineData(4096)]
		[InlineData(5000)]
        [InlineData(4096 * 2)]
		public void Archetype_Check_Linear_Entities_Adding_Only(int amount)
		{
			using var ctx = new Context();

			var arch = ctx.World.Root;

			for (var i = 0; i < amount; ++i)
				arch.Add((ulong)(i + 1));


			var total = 0;
			var chunks = arch.Chunks;
			for (var i = 0; i < chunks.Length - 1; ++i)
			{
				total += chunks[i].Count;

				Assert.Equal(Archetype.CHUNK_SIZE, chunks[i].Count);
			}

			total += chunks[^1].Count;

			Assert.Equal(arch.Count, total);
		}

		[Theory]
		[InlineData(10)]
        [InlineData(4096)]
		[InlineData(5000)]
        [InlineData(4096 * 2)]
		public void Archetype_Check_Linear_Entities_Adding_Removing(int amount)
		{
			using var ctx = new Context();

			var entities = new List<EntityView>();
			for (var i = 0; i < amount; ++i)
				entities.Add(ctx.World.Entity());


			entities.First().Delete();
			entities.Last().Delete();
			entities[entities.Count / 2].Delete();

			var arch = ctx.World.Root;
			var total = 0;
			var chunks = arch.Chunks;
			for (var i = 0; i < chunks.Length - 1; ++i)
			{
				total += chunks[i].Count;

				Assert.Equal(Archetype.CHUNK_SIZE, chunks[i].Count);
			}

			total += chunks[^1].Count;

			Assert.Equal(arch.Count, total);
		}
	}
}
