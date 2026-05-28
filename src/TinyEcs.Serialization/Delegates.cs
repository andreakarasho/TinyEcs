using System.Text.Json;

namespace TinyEcs.Serialization;

/// <summary>
/// Writes a single component value to the supplied JSON writer. The resolver maps
/// any <see cref="EcsID"/> field inside the component to a stable original id so it
/// can be remapped at deserialization time.
/// </summary>
public delegate void JsonWriteAction<T>(Utf8JsonWriter writer, T value, IEntityIdResolver resolver) where T : struct;

/// <summary>
/// Reads a single component value from the supplied JSON element. The element
/// contains the contents previously emitted by the matching <see cref="JsonWriteAction{T}"/>.
/// </summary>
public delegate T JsonReadAction<T>(JsonElement element, IEntityIdRemapper remapper) where T : struct;
