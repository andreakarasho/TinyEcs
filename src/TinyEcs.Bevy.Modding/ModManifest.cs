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

    /// Reserved per-mod rules / capability grants — what the HOST permits this mod to
    /// do. Empty object for now.
    // ponytail: raw JsonElement placeholder so it round-trips any future shape
    // with no model change; promote to a typed Ruleset class once the rules exist.
    public JsonElement Ruleset { get; set; }

    /// Host features this mod takes over: `"replaces": ["cuo:ui/system-log"]`. A host
    /// feature that has a mod-facing equivalent stands down while such a mod is loaded
    /// (see ModControl.IsReplaced) instead of leaving both stacked and asking the
    /// player to go find the toggle.
    ///
    /// Top level and deliberately NOT inside Ruleset: the trust direction is opposite.
    /// A ruleset is authority handed DOWN to the mod; this is a claim the mod makes
    /// about ITSELF. Keeping them apart stops a self-asserted claim from reading like
    /// a granted capability once real grants land.
    public string[] Replaces { get; set; } = Array.Empty<string>();

    /// Drop the nulls and blanks a hand-written manifest can carry. A wrong-TYPE
    /// `replaces` (a string, an object) instead fails the manifest parse outright and
    /// the loader skips the mod with a message — the mods folder validates rather than
    /// trusts, so the author sees the mistake immediately.
    internal static string[] CleanFeatures(string[]? features)
    {
        if (features is null || features.Length == 0)
            return Array.Empty<string>();

        var kept = new List<string>(features.Length);
        foreach (var feature in features)
            if (!string.IsNullOrWhiteSpace(feature))
                kept.Add(feature);
        return kept.Count == 0 ? Array.Empty<string>() : kept.ToArray();
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ModManifest))]
// Array variant for IJsModChannel.ListMods() — the Jco backend's discovery
// returns every available mod's manifest in one JSON array (no filesystem scan).
[JsonSerializable(typeof(ModManifest[]))]
internal partial class ModManifestJsonContext : JsonSerializerContext;
