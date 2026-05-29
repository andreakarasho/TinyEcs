using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests;

public class ComponentIdIsolationTests
{
	private struct IsoA { public int Value; }
	private struct IsoB { public int Value; }

	[Fact]
	public void ComponentIds_StartFromOne_PerWorld()
	{
		var world = new World();

		// First component registered in a fresh world gets id 1; the per-world
		// counter then advances densely (internal components such as Name may
		// interleave, so we only assert ordering, not exact later values).
		var a = world.Entity<IsoA>().ID;
		var b = world.Entity<IsoB>().ID;

		Assert.Equal(1ul, (ulong)a);
		Assert.True((ulong)b > (ulong)a);
	}

	[Fact]
	public void ComponentIds_AreIndependentPerWorld_OrderDependent()
	{
		var worldA = new World();
		var worldB = new World();

		// Register in opposite order in each world.
		var aIdInA = worldA.Entity<IsoA>().ID;
		var bIdInA = worldA.Entity<IsoB>().ID;

		var bIdInB = worldB.Entity<IsoB>().ID;
		var aIdInB = worldB.Entity<IsoA>().ID;

		// Each world assigns ids by its own registration order, independently.
		Assert.Equal(aIdInA, bIdInB); // both first-registered in their world
		Assert.Equal(bIdInA, aIdInB); // both second-registered
		Assert.NotEqual(aIdInA, aIdInB); // same type, different id across worlds
	}

	[Fact]
	public void Storage_IsCorrect_AcrossWorldsWithSameType()
	{
		var worldA = new World();
		var worldB = new World();

		// Skew the id assignment so IsoA has a different id in each world.
		worldB.Entity<IsoB>(); // bumps worldB's counter first

		var ea = worldA.Entity().Set(new IsoA { Value = 111 });
		var eb = worldB.Entity().Set(new IsoA { Value = 222 });

		Assert.NotEqual(worldA.Entity<IsoA>().ID, worldB.Entity<IsoA>().ID);
		Assert.Equal(111, worldA.Get<IsoA>(ea.ID).Value);
		Assert.Equal(222, worldB.Get<IsoA>(eb.ID).Value);
	}

	[Fact]
	public void Query_Works_WithPerWorldIds()
	{
		var world = new World();

		world.Entity().Set(new IsoA { Value = 5 }).Set(new IsoB { Value = 7 });
		world.Entity().Set(new IsoA { Value = 9 });

		var sum = 0;
		var count = 0;
		var query = world.Query<Data<IsoA>>();
		foreach (var row in query)
		{
			row.Deconstruct(out var a);
			sum += a.Ref.Value;
			count++;
		}

		Assert.Equal(2, count);
		Assert.Equal(14, sum);
	}
}
