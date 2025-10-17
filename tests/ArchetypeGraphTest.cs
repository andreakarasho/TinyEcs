using TinyEcs;
using Xunit;

namespace TinyEcs.Tests
{
	public class ArchetypeGraphTest
	{
		private struct ComponentA { public int Value; }
		private struct ComponentB { public int Value; }
		private struct ComponentC { public int Value; }

		[Fact]
		public void ArchetypeGraph_FindsArchetypeAfterComponentRemoval()
		{
			// This test verifies that archetypes created by component removal
			// are properly inserted into the graph and findable by queries.

			using var world = new World();

			// Step 1: Create entity with A, B, C
			var entity = world.Entity();
			world.Set(entity, new ComponentA { Value = 1 });
			world.Set(entity, new ComponentB { Value = 2 });
			world.Set(entity, new ComponentC { Value = 3 });

			// Step 2: Build query for A, B (should find entity)
			var query = world.QueryBuilder()
				.With<ComponentA>()
				.With<ComponentB>()
				.Build();

			Assert.Equal(1, query.Count());

			// Step 3: Remove C - entity moves to archetype {A, B}
			world.Unset<ComponentC>(entity);

			// BUG: The archetype {A, B} created by removing C might not be
			// properly inserted into the graph, making it unfindable by queries
			// that traverse from the root.

			// Step 4: Query should still find the entity
			Assert.Equal(1, query.Count()); // This might FAIL if graph insertion is broken
			Assert.True(world.Has<ComponentA>(entity));
			Assert.True(world.Has<ComponentB>(entity));
			Assert.False(world.Has<ComponentC>(entity));
		}

		[Fact]
		public void ArchetypeGraph_ComplexRemovalPattern()
		{
			// Test a complex pattern of adds and removes to stress test graph structure

			using var world = new World();

			// Create entity with A, B, C
			var entity = world.Entity();
			world.Set(entity, new ComponentA { Value = 1 });
			world.Set(entity, new ComponentB { Value = 2 });
			world.Set(entity, new ComponentC { Value = 3 });

			// Query for just A
			var queryA = world.QueryBuilder().With<ComponentA>().Build();
			Assert.Equal(1, queryA.Count());

			// Remove B (archetype {A, C} created by removal)
			world.Unset<ComponentB>(entity);
			Assert.Equal(1, queryA.Count()); // Should still find via {A, C}

			// Remove C (archetype {A} created by removal)
			world.Unset<ComponentC>(entity);
			Assert.Equal(1, queryA.Count()); // Should still find via {A}

			// Add B back (archetype {A, B} might exist or be created)
			world.Set(entity, new ComponentB { Value = 4 });
			Assert.Equal(1, queryA.Count()); // Should find via {A, B}

			// Add C back (archetype {A, B, C} already exists)
			world.Set(entity, new ComponentC { Value = 5 });
			Assert.Equal(1, queryA.Count()); // Should find via {A, B, C}
		}
	}
}
