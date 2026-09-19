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

    /// The WASM file to load, relative to the mod's own folder.
    public string Wasm { get; set; } = "";

    /// Reserved. Mods are core-wasm modules (abi/mod-abi.fbs); the field is kept for
    /// manifest forward-compat but is no longer read — the loader sniffs the wasm
    /// preamble and rejects component-model binaries outright.
    public string Abi { get; set; } = "";

    /// Per-mod rules / capability grants. One rule is read today (`replaces`, see
    /// ReadReplaces); the rest round-trips untouched.
    // ponytail: raw JsonElement placeholder so it round-trips any future shape
    // with no model change; promote to a typed Ruleset class once there are
    // enough rules to be worth one.
    public JsonElement Ruleset { get; set; }

    /// Host features this mod declares it takes over: `"ruleset": { "replaces":
    /// ["cuo:ui/system-log"] }`. A mod that replaces a built-in window is installed
    /// INSTEAD of it — the host hides its own rather than leaving two stacked and
    /// asking the player to find the toggle.
    ///
    /// Absent / malformed reads as "replaces nothing": a ruleset is a declaration,
    /// and a typo in it must not stop the mod loading.
    public string[] ReadReplaces()
    {
        if (Ruleset.ValueKind != JsonValueKind.Object
            || !Ruleset.TryGetProperty("replaces", out var arr)
            || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var found = new List<string>();
        foreach (var item in arr.EnumerateArray())
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } feature)
                found.Add(feature);
        return found.Count == 0 ? Array.Empty<string>() : found.ToArray();
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ModManifest))]
// Array variant for IJsModChannel.ListMods() — the Jco backend's discovery
// returns every available mod's manifest in one JSON array (no filesystem scan).
[JsonSerializable(typeof(ModManifest[]))]
internal partial class ModManifestJsonContext : JsonSerializerContext;
