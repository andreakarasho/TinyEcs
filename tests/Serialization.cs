using System.Text.Json;
using System.Text.Json.Serialization;
using TinyEcs.Serialization;

using EcsID = ulong;

namespace TinyEcs.Tests;

[JsonSerializable(typeof(SerializationTests.HealthCtx))]
internal partial class HealthContext : JsonSerializerContext { }

public class SerializationTests
{
	private struct PositionS { public float X; public float Y; }
	private struct VelocityS { public float Dx; public float Dy; }
	private struct EnemyTag { }
	private struct EntityRef { public EcsID Target; }

	internal struct HealthCtx { public int Current { get; set; } public int Max { get; set; } }

	private static WorldSerializer NewSerializer()
	{
		return new WorldSerializer()
			.RegisterComponent<PositionS>(
				static (writer, p, _) =>
				{
					writer.WriteStartObject();
					writer.WriteNumber("x", p.X);
					writer.WriteNumber("y", p.Y);
					writer.WriteEndObject();
				},
				static (element, _) => new PositionS
				{
					X = element.GetProperty("x").GetSingle(),
					Y = element.GetProperty("y").GetSingle(),
				})
			.RegisterComponent<VelocityS>(
				static (writer, v, _) =>
				{
					writer.WriteStartObject();
					writer.WriteNumber("dx", v.Dx);
					writer.WriteNumber("dy", v.Dy);
					writer.WriteEndObject();
				},
				static (element, _) => new VelocityS
				{
					Dx = element.GetProperty("dx").GetSingle(),
					Dy = element.GetProperty("dy").GetSingle(),
				})
			.RegisterTag<EnemyTag>()
			.RegisterComponent<EntityRef>(
				static (writer, r, resolver) =>
				{
					writer.WriteStartObject();
					writer.WriteNumber("target", resolver.Resolve(r.Target));
					writer.WriteEndObject();
				},
				static (element, remapper) => new EntityRef
				{
					Target = remapper.Remap(element.GetProperty("target").GetUInt64()),
				});
	}

	[Fact]
	public void World_RoundTrip_PreservesComponents()
	{
		using var src = new World();
		var a = src.Entity();
		src.Set(a.ID, new PositionS { X = 1f, Y = 2f });
		src.Set(a.ID, new VelocityS { Dx = 3f, Dy = 4f });

		var b = src.Entity();
		src.Set(b.ID, new PositionS { X = 5f, Y = 6f });

		var serializer = NewSerializer();
		var json = serializer.SerializeToString(src, new JsonWriterOptions { Indented = false });

		using var dst = new World();
		var result = serializer.Deserialize(dst, json);

		Assert.Equal(2, result.IdMap.Count);

		var aNew = result.IdMap[a.ID];
		var bNew = result.IdMap[b.ID];

		Assert.True(dst.Has<PositionS>(aNew));
		Assert.True(dst.Has<VelocityS>(aNew));
		Assert.Equal(1f, dst.Get<PositionS>(aNew).X);
		Assert.Equal(2f, dst.Get<PositionS>(aNew).Y);
		Assert.Equal(3f, dst.Get<VelocityS>(aNew).Dx);
		Assert.Equal(4f, dst.Get<VelocityS>(aNew).Dy);

		Assert.True(dst.Has<PositionS>(bNew));
		Assert.False(dst.Has<VelocityS>(bNew));
		Assert.Equal(5f, dst.Get<PositionS>(bNew).X);
	}

	[Fact]
	public void World_RoundTrip_PreservesTagsAndName()
	{
		using var src = new World();
		var named = src.Entity("hero");
		src.Set(named.ID, new PositionS { X = 10f, Y = 20f });
		src.Set(named.ID, default(EnemyTag));

		var serializer = NewSerializer();
		var json = serializer.SerializeToString(src);

		using var dst = new World();
		var result = serializer.Deserialize(dst, json);
		var newId = result.IdMap[named.ID];

		Assert.Equal("hero", dst.Name(newId));
		Assert.True(dst.Has<EnemyTag>(newId));
		Assert.Equal(10f, dst.Get<PositionS>(newId).X);
	}

	[Fact]
	public void World_RoundTrip_PreservesParentChildHierarchy()
	{
		using var src = new World();
		var root = src.Entity();
		src.Set(root.ID, new PositionS { X = 0f, Y = 0f });

		var child1 = src.Entity();
		src.Set(child1.ID, new PositionS { X = 1f, Y = 1f });
		src.AddChild(root.ID, child1.ID);

		var child2 = src.Entity();
		src.Set(child2.ID, new PositionS { X = 2f, Y = 2f });
		src.AddChild(root.ID, child2.ID);

		var grandchild = src.Entity();
		src.Set(grandchild.ID, new PositionS { X = 3f, Y = 3f });
		src.AddChild(child1.ID, grandchild.ID);

		var serializer = NewSerializer();
		var json = serializer.SerializeToString(src);

		using var dst = new World();
		var result = serializer.Deserialize(dst, json);

		var rootNew = result.IdMap[root.ID];
		var child1New = result.IdMap[child1.ID];
		var child2New = result.IdMap[child2.ID];
		var grandchildNew = result.IdMap[grandchild.ID];

		Assert.Equal(rootNew, dst.GetParent(child1New));
		Assert.Equal(rootNew, dst.GetParent(child2New));
		Assert.Equal(child1New, dst.GetParent(grandchildNew));

		var rootChildren = dst.RelationshipEntityMapper.GetChildren(rootNew);
		Assert.NotNull(rootChildren);
		Assert.Equal(2, rootChildren.Count);
		Assert.Contains(child1New, rootChildren);
		Assert.Contains(child2New, rootChildren);

		Assert.Equal(3f, dst.Get<PositionS>(grandchildNew).X);
	}

	[Fact]
	public void Entity_Serialize_IncludesDescendantsAndRemapsRefs()
	{
		using var src = new World();
		var root = src.Entity();
		src.Set(root.ID, new PositionS { X = 100f, Y = 200f });

		var leaf = src.Entity();
		src.Set(leaf.ID, new PositionS { X = 300f, Y = 400f });
		src.AddChild(root.ID, leaf.ID);

		// EntityRef inside a component must be remapped on the way back.
		src.Set(leaf.ID, new EntityRef { Target = root.ID });

		var serializer = NewSerializer();
		var json = serializer.SerializeEntityToString(src, root.ID, includeDescendants: true);

		using var dst = new World();
		var result = serializer.Deserialize(dst, json);

		Assert.NotEqual(0ul, result.RootEntity);

		var rootNew = result.RootEntity;
		var leafNew = result.IdMap[leaf.ID];

		Assert.Equal(rootNew, dst.GetParent(leafNew));
		Assert.Equal(rootNew, dst.Get<EntityRef>(leafNew).Target);
		Assert.Equal(300f, dst.Get<PositionS>(leafNew).X);
	}

	[Fact]
	public void Entity_Serialize_WithoutDescendants_OnlyEmitsRoot()
	{
		using var src = new World();
		var root = src.Entity();
		src.Set(root.ID, new PositionS { X = 1f, Y = 2f });
		var child = src.Entity();
		src.AddChild(root.ID, child.ID);

		var serializer = NewSerializer();
		var json = serializer.SerializeEntityToString(src, root.ID, includeDescendants: false);

		using var dst = new World();
		var result = serializer.Deserialize(dst, json);

		Assert.Single(result.IdMap);
		Assert.True(result.IdMap.ContainsKey(root.ID));
		Assert.False(result.IdMap.ContainsKey(child.ID));
	}

	[Fact]
	public void UnregisteredComponent_IsSilentlySkipped()
	{
		using var src = new World();
		var e = src.Entity();
		src.Set(e.ID, new PositionS { X = 1f, Y = 2f });
		// VelocityS is set but the serializer below does not register it.
		src.Set(e.ID, new VelocityS { Dx = 9f, Dy = 9f });

		var serializer = new WorldSerializer().RegisterComponent<PositionS>(
			static (writer, p, _) =>
			{
				writer.WriteStartObject();
				writer.WriteNumber("x", p.X);
				writer.WriteNumber("y", p.Y);
				writer.WriteEndObject();
			},
			static (element, _) => new PositionS
			{
				X = element.GetProperty("x").GetSingle(),
				Y = element.GetProperty("y").GetSingle(),
			});

		var json = serializer.SerializeToString(src);

		using var dst = new World();
		var result = serializer.Deserialize(dst, json);
		var newId = result.IdMap[e.ID];

		Assert.True(dst.Has<PositionS>(newId));
		Assert.False(dst.Has<VelocityS>(newId));
	}

	[Fact]
	public void RegisterComponent_JsonTypeInfo_RoundTrips()
	{
		using var src = new World();
		var e = src.Entity();
		src.Set(e.ID, new HealthCtx { Current = 30, Max = 100 });

		var serializer = new WorldSerializer()
			.RegisterComponent(HealthContext.Default.HealthCtx);

		var json = serializer.SerializeToString(src);

		using var dst = new World();
		var result = serializer.Deserialize(dst, json);
		var newId = result.IdMap[e.ID];

		Assert.True(dst.Has<HealthCtx>(newId));
		Assert.Equal(30, dst.Get<HealthCtx>(newId).Current);
		Assert.Equal(100, dst.Get<HealthCtx>(newId).Max);
	}

	[Fact]
	public void Deserialize_PreserveIds_RebuildsWorldVerbatim()
	{
		using var src = new World();
		var a = src.Entity("alice");
		src.Set(a.ID, new PositionS { X = 1f, Y = 2f });

		var b = src.Entity();
		src.Set(b.ID, new PositionS { X = 3f, Y = 4f });
		src.Set(b.ID, new EntityRef { Target = a.ID });
		src.AddChild(a.ID, b.ID);

		var serializer = NewSerializer();
		var json = serializer.SerializeToString(src);

		using var dst = new World();
		var result = serializer.Deserialize(dst, json, new DeserializeOptions { PreserveIds = true });

		Assert.Equal(a.ID, result.IdMap[a.ID]);
		Assert.Equal(b.ID, result.IdMap[b.ID]);
		Assert.True(dst.Exists(a.ID));
		Assert.True(dst.Exists(b.ID));
		Assert.Equal("alice", dst.Name(a.ID));
		Assert.Equal(a.ID, dst.GetParent(b.ID));
		Assert.Equal(a.ID, dst.Get<EntityRef>(b.ID).Target);
		Assert.Equal(1f, dst.Get<PositionS>(a.ID).X);
		Assert.Equal(3f, dst.Get<PositionS>(b.ID).X);
	}

	[Fact]
	public void Deserialize_PreserveIds_ThrowsOnCollision()
	{
		using var src = new World();
		var a = src.Entity();
		src.Set(a.ID, new PositionS { X = 1f, Y = 2f });

		var serializer = NewSerializer();
		var json = serializer.SerializeToString(src);

		using var dst = new World();
		dst.Entity(a.ID); // occupy the same slot

		Assert.Throws<InvalidOperationException>(() =>
			serializer.Deserialize(dst, json, new DeserializeOptions { PreserveIds = true }));
	}

	[Fact]
	public void RegisterTag_ThrowsForSizedComponent()
	{
		var serializer = new WorldSerializer();
		Assert.Throws<InvalidOperationException>(() => serializer.RegisterTag<PositionS>());
	}

	[Fact]
	public void RegisterComponent_ThrowsForZeroSizedTag()
	{
		var serializer = new WorldSerializer();
		Assert.Throws<InvalidOperationException>(() =>
			serializer.RegisterComponent<EnemyTag>(
				static (_, _, _) => { },
				static (_, _) => default));
	}
}
