using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace TinyEcs.Serialization;

/// <summary>
/// Reflection-free JSON serializer for TinyEcs worlds and entities.
/// </summary>
/// <remarks>
/// <para>
/// The serializer is configured by registering each component or tag type that
/// should round-trip through JSON. Unregistered components are silently ignored on
/// the way out and on the way in. The built-in <see cref="Parent"/>, <see cref="Children"/>
/// and <see cref="Name"/> components are excluded by default — they are restored
/// implicitly through the <c>parent</c> and <c>name</c> fields written next to each
/// entity payload.
/// </para>
/// <para>
/// Serialization preserves a snapshot of the world: every entity that carries at
/// least one registered component, tag, name, or parent link is emitted with its
/// original 64-bit id. Deserialization always allocates fresh ids and tracks the
/// original→new mapping so that <see cref="EcsID"/> fields inside components can be
/// rewired via <see cref="IEntityIdRemapper"/>.
/// </para>
/// </remarks>
public sealed class WorldSerializer
{
	private const int FormatVersion = 1;

	private readonly Dictionary<EcsID, ComponentEntry> _byId = new();
	private readonly Dictionary<string, ComponentEntry> _byName = new();
	private readonly HashSet<EcsID> _autoSkip = new();

	public WorldSerializer()
	{
		// Hierarchy + naming are reconstructed from the dedicated parent/name fields
		// so the components themselves never round-trip as plain data.
		_autoSkip.Add(Lookup.Component<Parent>.Value.ID);
		_autoSkip.Add(Lookup.Component<Children>.Value.ID);
		_autoSkip.Add(Lookup.Component<Name>.Value.ID);
	}

	/// <summary>
	/// Registers a component type with explicit read/write callbacks.
	/// </summary>
	public WorldSerializer RegisterComponent<T>(JsonWriteAction<T> write, JsonReadAction<T> read, string? name = null) where T : struct
	{
		ArgumentNullException.ThrowIfNull(write);
		ArgumentNullException.ThrowIfNull(read);

		var id = Lookup.Component<T>.Value.ID;
		var size = Lookup.Component<T>.Size;
		if (size <= 0)
			throw new InvalidOperationException($"{typeof(T).Name} is a zero-sized type; call RegisterTag<T>() instead.");

		var entry = new ComponentEntry
		{
			Id = id,
			Name = name ?? Lookup.Component<T>.Name,
			Size = size,
			IsTag = false,
			Write = (writer, world, entity, resolver) =>
			{
				ref var value = ref world.Get<T>(entity);
				write(writer, value, resolver);
			},
			Read = (world, entity, element, remapper) =>
			{
				var value = read(element, remapper);
				world.Set(entity, value);
			},
		};

		Register(entry);
		return this;
	}

	/// <summary>
	/// Registers a component type using a source-generated <see cref="JsonTypeInfo{T}"/>.
	/// AOT-safe and reflection-free. Components containing <see cref="EcsID"/> fields
	/// won't be remapped — supply a custom <see cref="System.Text.Json.Serialization.JsonConverter{T}"/>
	/// inside the context or use the callback overload of <see cref="RegisterComponent{T}(JsonWriteAction{T}, JsonReadAction{T}, string?)"/>.
	/// </summary>
	public WorldSerializer RegisterComponent<T>(JsonTypeInfo<T> typeInfo, string? name = null) where T : struct
	{
		ArgumentNullException.ThrowIfNull(typeInfo);

		var id = Lookup.Component<T>.Value.ID;
		var size = Lookup.Component<T>.Size;
		if (size <= 0)
			throw new InvalidOperationException($"{typeof(T).Name} is a zero-sized type; call RegisterTag<T>() instead.");

		var entry = new ComponentEntry
		{
			Id = id,
			Name = name ?? Lookup.Component<T>.Name,
			Size = size,
			IsTag = false,
			Write = (writer, world, entity, _) =>
			{
				ref var value = ref world.Get<T>(entity);
				JsonSerializer.Serialize(writer, value, typeInfo);
			},
			Read = (world, entity, element, _) =>
			{
				var value = element.Deserialize(typeInfo)!;
				world.Set(entity, value);
			},
		};

		Register(entry);
		return this;
	}

	/// <summary>
	/// Registers a zero-sized tag type. Tags carry no data and are restored just by
	/// re-attaching the tag at deserialization time.
	/// </summary>
	public WorldSerializer RegisterTag<T>(string? name = null) where T : struct
	{
		var id = Lookup.Component<T>.Value.ID;
		var size = Lookup.Component<T>.Size;
		if (size > 0)
			throw new InvalidOperationException($"{typeof(T).Name} is a sized component; call RegisterComponent<T>() instead.");

		var entry = new ComponentEntry
		{
			Id = id,
			Name = name ?? Lookup.Component<T>.Name,
			Size = 0,
			IsTag = true,
			Write = null,
			Read = static (world, entity, _, _) => world.Set(entity, default(T)),
		};

		Register(entry);
		return this;
	}

	private void Register(ComponentEntry entry)
	{
		_byId[entry.Id] = entry;
		_byName[entry.Name] = entry;
		// A user-registered Parent/Children/Name takes precedence over the auto skip.
		_autoSkip.Remove(entry.Id);
	}

	// === Serialize ============================================================

	/// <summary>
	/// Serialize the entire world to a JSON string.
	/// </summary>
	public string SerializeToString(World world, JsonWriterOptions options = default)
		=> SerializeCore(world, root: 0, includeDescendants: false, options);

	/// <summary>
	/// Serialize a single entity (and optionally all of its descendants) to JSON.
	/// </summary>
	public string SerializeEntityToString(World world, EcsID root, bool includeDescendants = true, JsonWriterOptions options = default)
	{
		if (!world.Exists(root))
			throw new ArgumentException($"Entity {root} does not exist in the world.", nameof(root));
		return SerializeCore(world, root, includeDescendants, options);
	}

	/// <summary>
	/// Serialize the entire world into <paramref name="writer"/>.
	/// </summary>
	public void Serialize(World world, Utf8JsonWriter writer)
		=> WriteDocument(world, writer, root: 0, includeDescendants: false);

	/// <summary>
	/// Serialize a single entity (and optionally all of its descendants) into <paramref name="writer"/>.
	/// </summary>
	public void SerializeEntity(World world, EcsID root, Utf8JsonWriter writer, bool includeDescendants = true)
	{
		if (!world.Exists(root))
			throw new ArgumentException($"Entity {root} does not exist in the world.", nameof(root));
		WriteDocument(world, writer, root, includeDescendants);
	}

	private string SerializeCore(World world, EcsID root, bool includeDescendants, JsonWriterOptions options)
	{
		using var buffer = new ArrayPoolBufferWriter();
		using (var writer = new Utf8JsonWriter(buffer, options))
		{
			WriteDocument(world, writer, root, includeDescendants);
		}
		return Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	private void WriteDocument(World world, Utf8JsonWriter writer, EcsID root, bool includeDescendants)
	{
		var entities = root == 0
			? CollectAllEntities(world)
			: CollectEntityAndDescendants(world, root, includeDescendants);

		writer.WriteStartObject();
		writer.WriteNumber("version", FormatVersion);
		if (root != 0)
			writer.WriteNumber("root", root);

		writer.WritePropertyName("entities");
		writer.WriteStartArray();
		foreach (var entity in entities)
			WriteEntity(world, writer, entity);
		writer.WriteEndArray();

		writer.WriteEndObject();
		writer.Flush();
	}

	private void WriteEntity(World world, Utf8JsonWriter writer, EcsID entity)
	{
		writer.WriteStartObject();
		writer.WriteNumber("id", entity);

		var name = world.NamingEntityMapper.GetName(entity);
		if (!string.IsNullOrEmpty(name))
			writer.WriteString("name", name);

		var parent = world.GetParent(entity);
		if (parent != 0)
			writer.WriteNumber("parent", parent);

		var resolver = IdentityResolver.Instance;
		var components = world.GetType(entity);

		// Components ----------------------------------------------------------
		var hasComponentSection = false;
		foreach (ref readonly var info in components)
		{
			if (info.Size <= 0) continue;
			if (_autoSkip.Contains(info.ID)) continue;
			if (!_byId.TryGetValue(info.ID, out var entry) || entry.IsTag) continue;

			if (!hasComponentSection)
			{
				writer.WritePropertyName("components");
				writer.WriteStartArray();
				hasComponentSection = true;
			}

			writer.WriteStartObject();
			writer.WriteString("type", entry.Name);
			writer.WritePropertyName("data");
			entry.Write!(writer, world, entity, resolver);
			writer.WriteEndObject();
		}
		if (hasComponentSection)
			writer.WriteEndArray();

		// Tags ----------------------------------------------------------------
		var hasTagSection = false;
		foreach (ref readonly var info in components)
		{
			if (info.Size > 0) continue;
			if (_autoSkip.Contains(info.ID)) continue;
			if (!_byId.TryGetValue(info.ID, out var entry) || !entry.IsTag) continue;

			if (!hasTagSection)
			{
				writer.WritePropertyName("tags");
				writer.WriteStartArray();
				hasTagSection = true;
			}

			writer.WriteStringValue(entry.Name);
		}
		if (hasTagSection)
			writer.WriteEndArray();

		writer.WriteEndObject();
	}

	private static List<EcsID> CollectAllEntities(World world)
	{
		var result = new List<EcsID>();
		var query = world.QueryBuilder().Build();
		var iter = query.Iter();
		while (iter.Next())
		{
			var entities = iter.Entities();
			for (var i = 0; i < entities.Length; i++)
				result.Add(entities[i].ID);
		}
		return result;
	}

	private static List<EcsID> CollectEntityAndDescendants(World world, EcsID root, bool includeDescendants)
	{
		var result = new List<EcsID> { root };
		if (!includeDescendants)
			return result;

		var stack = new Stack<EcsID>();
		stack.Push(root);
		while (stack.Count > 0)
		{
			var current = stack.Pop();
			var children = world.RelationshipEntityMapper.GetChildren(current);
			if (children == null) continue;
			foreach (var child in children)
			{
				result.Add(child);
				stack.Push(child);
			}
		}
		return result;
	}

	// === Deserialize ==========================================================

	/// <summary>
	/// Deserialize the supplied JSON payload into <paramref name="world"/>.
	/// </summary>
	public DeserializeResult Deserialize(World world, string json, DeserializeOptions options = default)
		=> Deserialize(world, Encoding.UTF8.GetBytes(json), options);

	/// <summary>
	/// Deserialize the supplied UTF-8 JSON payload into <paramref name="world"/>.
	/// </summary>
	public DeserializeResult Deserialize(World world, ReadOnlySpan<byte> json, DeserializeOptions options = default)
	{
		using var doc = JsonDocument.Parse(json.ToArray());
		return Deserialize(world, doc.RootElement, options);
	}

	/// <summary>
	/// Deserialize the supplied JSON document root into <paramref name="world"/>.
	/// </summary>
	/// <remarks>
	/// When <see cref="DeserializeOptions.PreserveIds"/> is set, each serialized id
	/// is reused verbatim. This rebuilds the world identically to its source state
	/// provided the destination world has matching component-id registration (i.e.
	/// the same component types were registered in the same order at process start).
	/// If the original id is already alive in the destination world an exception is
	/// thrown to avoid silent data corruption.
	/// </remarks>
	public DeserializeResult Deserialize(World world, JsonElement root, DeserializeOptions options = default)
	{
		if (root.ValueKind != JsonValueKind.Object)
			throw new JsonException("Root element must be an object.");

		var entitiesArr = root.GetProperty("entities");
		if (entitiesArr.ValueKind != JsonValueKind.Array)
			throw new JsonException("Missing 'entities' array.");

		var map = new Dictionary<ulong, EcsID>();

		// Pass 1: allocate every entity so parent/component references can resolve.
		foreach (var element in entitiesArr.EnumerateArray())
		{
			if (!element.TryGetProperty("id", out var idElem))
				throw new JsonException("Entity entry missing 'id'.");
			var originalId = idElem.GetUInt64();

			EntityView newEntity;
			if (options.PreserveIds)
			{
				if (world.Exists(originalId))
					throw new InvalidOperationException(
						$"Cannot preserve id {originalId}: destination world already has an entity with that id.");
				newEntity = world.Entity(originalId);
			}
			else
			{
				newEntity = world.Entity();
			}
			map[originalId] = newEntity.ID;

			if (element.TryGetProperty("name", out var nameElem) && nameElem.ValueKind == JsonValueKind.String)
				world.NamingEntityMapper.SetName(newEntity.ID, nameElem.GetString()!);
		}

		var remapper = new DictionaryRemapper(map);

		// Pass 2: re-attach components, tags and parent links using the remapper.
		foreach (var element in entitiesArr.EnumerateArray())
		{
			var originalId = element.GetProperty("id").GetUInt64();
			var newId = map[originalId];

			if (element.TryGetProperty("components", out var compsElem) && compsElem.ValueKind == JsonValueKind.Array)
			{
				foreach (var comp in compsElem.EnumerateArray())
				{
					var typeName = comp.GetProperty("type").GetString();
					if (typeName is null) continue;
					if (!_byName.TryGetValue(typeName, out var entry) || entry.IsTag) continue;
					var data = comp.TryGetProperty("data", out var dataElem) ? dataElem : default;
					entry.Read!(world, newId, data, remapper);
				}
			}

			if (element.TryGetProperty("tags", out var tagsElem) && tagsElem.ValueKind == JsonValueKind.Array)
			{
				foreach (var tag in tagsElem.EnumerateArray())
				{
					var tagName = tag.GetString();
					if (tagName is null) continue;
					if (!_byName.TryGetValue(tagName, out var entry) || !entry.IsTag) continue;
					entry.Read!(world, newId, default, remapper);
				}
			}

			if (element.TryGetProperty("parent", out var parentElem) && parentElem.ValueKind == JsonValueKind.Number)
			{
				var originalParent = parentElem.GetUInt64();
				if (map.TryGetValue(originalParent, out var newParent))
					world.AddChild(newParent, newId);
			}
		}

		EcsID rootEntity = 0;
		if (root.TryGetProperty("root", out var rootElem) && rootElem.ValueKind == JsonValueKind.Number)
		{
			var originalRoot = rootElem.GetUInt64();
			map.TryGetValue(originalRoot, out rootEntity);
		}

		return new DeserializeResult(map, rootEntity);
	}

	private sealed class ComponentEntry
	{
		public EcsID Id;
		public string Name = string.Empty;
		public int Size;
		public bool IsTag;
		public Action<Utf8JsonWriter, World, EcsID, IEntityIdResolver>? Write;
		public Action<World, EcsID, JsonElement, IEntityIdRemapper>? Read;
	}
}

/// <summary>
/// Tweaks deserialization behavior.
/// </summary>
public readonly struct DeserializeOptions
{
	/// <summary>
	/// Reuse each original entity id instead of allocating a fresh one. The
	/// destination world must not already contain those ids, and component-id
	/// registration must match the source process for inter-entity references to
	/// stay valid.
	/// </summary>
	public bool PreserveIds { get; init; }
}

/// <summary>
/// Result of a deserialization pass. <see cref="IdMap"/> maps each original (serialized)
/// id to the freshly allocated id in the destination world; <see cref="RootEntity"/>
/// is set only when the payload was produced by a single-entity serialization.
/// </summary>
public readonly struct DeserializeResult
{
	public DeserializeResult(IReadOnlyDictionary<ulong, EcsID> idMap, EcsID rootEntity)
	{
		IdMap = idMap;
		RootEntity = rootEntity;
	}

	public IReadOnlyDictionary<ulong, EcsID> IdMap { get; }
	public EcsID RootEntity { get; }
}

internal sealed class ArrayPoolBufferWriter : IBufferWriter<byte>, IDisposable
{
	private byte[] _buffer = ArrayPool<byte>.Shared.Rent(1024);
	private int _written;

	public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

	public void Advance(int count) => _written += count;

	public Memory<byte> GetMemory(int sizeHint = 0)
	{
		EnsureCapacity(sizeHint);
		return _buffer.AsMemory(_written);
	}

	public Span<byte> GetSpan(int sizeHint = 0)
	{
		EnsureCapacity(sizeHint);
		return _buffer.AsSpan(_written);
	}

	private void EnsureCapacity(int sizeHint)
	{
		if (sizeHint <= 0) sizeHint = 1;
		var available = _buffer.Length - _written;
		if (available >= sizeHint) return;

		var newSize = Math.Max(_buffer.Length * 2, _written + sizeHint);
		var next = ArrayPool<byte>.Shared.Rent(newSize);
		_buffer.AsSpan(0, _written).CopyTo(next);
		ArrayPool<byte>.Shared.Return(_buffer);
		_buffer = next;
	}

	public void Dispose()
	{
		if (_buffer.Length > 0)
		{
			ArrayPool<byte>.Shared.Return(_buffer);
			_buffer = Array.Empty<byte>();
		}
	}
}
