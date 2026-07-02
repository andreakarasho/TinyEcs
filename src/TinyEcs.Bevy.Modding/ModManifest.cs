// Per-mod manifest, read from `<ModFolder>/<mod>/mod.json`. Each mod lives in
// its own folder alongside the WASM component the manifest names. Parsed through
// a System.Text.Json source-gen context (AOT-safe; no reflection).

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TinyEcs.Bevy.Modding;

public sealed class ModManifest
{
    /// Stable id / display name of the mod (also the dedup key across folders).
    public string Name { get; set; } = "";

    /// Mod version (semver string; recorded, not enforced yet).
    public string Version { get; set; } = "";

    /// The WASM component file to load, relative to the mod's own folder.
    public string Wasm { get; set; } = "";

    /// Reserved per-mod rules / capability grants. Empty object for now.
    // ponytail: raw JsonElement placeholder so it round-trips any future shape
    // with no model change; promote to a typed Ruleset class once the rules exist.
    public JsonElement Ruleset { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ModManifest))]
// Array variant for IJsModChannel.ListMods() — the Jco backend's discovery
// returns every available mod's manifest in one JSON array (no filesystem scan).
[JsonSerializable(typeof(ModManifest[]))]
internal partial class ModManifestJsonContext : JsonSerializerContext;
