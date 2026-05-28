using System.IO;
using System.Text.Json;
using TinyEcs.Serialization;

using EcsID = ulong;

namespace TinyEcs.Tests;

public class SerializationOutputDemo
{
	private struct Position { public float X; public float Y; }
	private struct Velocity { public float Dx; public float Dy; }
	private struct Loot { public int Gold; }
	private struct Enemy { }
	private struct Boss { }

	[Fact]
	public void DumpHierarchyJson()
	{
		using var world = new World();

		var parent = world.Entity("kingdom");
		world.Set(parent.ID, new Position { X = 0f, Y = 0f });
		world.Set(parent.ID, default(Boss));

		var child1 = world.Entity("knight");
		world.Set(child1.ID, new Position { X = 1f, Y = 2f });
		world.Set(child1.ID, new Velocity { Dx = 0.5f, Dy = 0f });
		world.Set(child1.ID, default(Enemy));
		world.AddChild(parent.ID, child1.ID);

		var child2 = world.Entity();
		world.Set(child2.ID, new Position { X = 3f, Y = 4f });
		world.Set(child2.ID, new Loot { Gold = 42 });
		world.Set(child2.ID, default(Enemy));
		world.AddChild(parent.ID, child2.ID);

		var grandchild = world.Entity();
		world.Set(grandchild.ID, new Position { X = 5f, Y = 5f });
		world.AddChild(child1.ID, grandchild.ID);

		var serializer = new WorldSerializer()
			.RegisterComponent<Position>(
				static (w, p, _) => { w.WriteStartObject(); w.WriteNumber("x", p.X); w.WriteNumber("y", p.Y); w.WriteEndObject(); },
				static (e, _) => new Position { X = e.GetProperty("x").GetSingle(), Y = e.GetProperty("y").GetSingle() })
			.RegisterComponent<Velocity>(
				static (w, v, _) => { w.WriteStartObject(); w.WriteNumber("dx", v.Dx); w.WriteNumber("dy", v.Dy); w.WriteEndObject(); },
				static (e, _) => new Velocity { Dx = e.GetProperty("dx").GetSingle(), Dy = e.GetProperty("dy").GetSingle() })
			.RegisterComponent<Loot>(
				static (w, l, _) => { w.WriteStartObject(); w.WriteNumber("gold", l.Gold); w.WriteEndObject(); },
				static (e, _) => new Loot { Gold = e.GetProperty("gold").GetInt32() })
			.RegisterTag<Enemy>("Enemy")
			.RegisterTag<Boss>("Boss");

		var worldJson = serializer.SerializeToString(world, new JsonWriterOptions { Indented = true });
		var subtreeJson = serializer.SerializeEntityToString(world, parent.ID, includeDescendants: true,
			new JsonWriterOptions { Indented = true });

		var outPath = Path.Combine(Path.GetTempPath(), "tinyecs-serialization-demo.txt");
		File.WriteAllText(outPath,
			"=== SerializeWorld ===\n" + worldJson +
			"\n\n=== SerializeEntity(parent, includeDescendants=true) ===\n" + subtreeJson + "\n");

		// Re-import sanity check.
		using var dst = new World();
		var result = serializer.Deserialize(dst, worldJson);
		Assert.Equal(4, result.IdMap.Count);
		Assert.Equal(result.IdMap[parent.ID], dst.GetParent(result.IdMap[child1.ID]));
	}
}
