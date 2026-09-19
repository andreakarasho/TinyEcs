using System.Text.Json;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// mod.json's top-level `replaces` — the declaration that lets an installed mod stand a
// host feature down (a host asks ModControl.IsReplaced before building its own UI).
//
// It sits beside name/version/wasm and NOT inside `ruleset` on purpose: a ruleset is
// authority handed down TO a mod (allow/deny), while `replaces` is a claim a mod makes
// about itself. Opposite trust directions, so they stay in separate fields.
public class ModRulesetTests
{
    private static ModManifest Parse(string json)
        => JsonSerializer.Deserialize(json, ModManifestJsonContext.Default.ModManifest)!;

    [Fact]
    public void Replaces_deserializes_from_the_top_level()
    {
        var manifest = Parse("""
            {
              "name": "journal",
              "version": "0.2.0",
              "wasm": "mod.wasm",
              "replaces": ["cuo:ui/system-log"],
              "ruleset": {}
            }
            """);

        Assert.Equal("journal", manifest.Name);
        Assert.Equal(new[] { "cuo:ui/system-log" }, ModManifest.CleanFeatures(manifest.Replaces));
        // A `replaces` claim must never be mistaken for a granted capability.
        Assert.Equal(JsonValueKind.Object, manifest.Ruleset.ValueKind);
        Assert.False(manifest.Ruleset.TryGetProperty("replaces", out _));
    }

    [Fact]
    public void Replaces_is_empty_when_the_manifest_omits_it()
    {
        // The common case: every mod that replaces nothing.
        var manifest = Parse("""{ "name": "autoheal", "version": "1.0.0", "wasm": "mod.wasm" }""");
        Assert.Empty(ModManifest.CleanFeatures(manifest.Replaces));
    }

    [Fact]
    public void A_ruleset_replaces_is_NOT_read()
    {
        // Guards the move: the old spelling must not keep working by accident, or the
        // two fields quietly merge again.
        var manifest = Parse("""
            { "name": "journal", "version": "0.2.0", "wasm": "mod.wasm",
              "ruleset": { "replaces": ["cuo:ui/system-log"] } }
            """);
        Assert.Empty(ModManifest.CleanFeatures(manifest.Replaces));
    }

    [Theory]
    [InlineData("""{ "replaces": null }""")]
    [InlineData("""{ "replaces": [] }""")]
    [InlineData("""{ "replaces": [null] }""")]
    [InlineData("""{ "replaces": ["", "   "] }""")]
    public void CleanFeatures_drops_the_blanks_a_hand_written_manifest_carries(string json)
        => Assert.Empty(ModManifest.CleanFeatures(Parse(json).Replaces));

    [Fact]
    public void CleanFeatures_keeps_the_real_ids_and_drops_the_rest()
        => Assert.Equal(
            new[] { "cuo:ui/system-log" },
            ModManifest.CleanFeatures(Parse("""{ "replaces": [null, "cuo:ui/system-log", " "] }""").Replaces));

    [Fact]
    public void A_wrong_typed_replaces_fails_the_whole_manifest()
    {
        // Deliberate: the mods folder validates rather than trusts, so a malformed
        // manifest is skipped with a message (ModdingPlugin.LoadManifest catches this)
        // and the author sees the mistake at once instead of silently losing the rule.
        Assert.Throws<JsonException>(() => Parse("""{ "replaces": "cuo:ui/system-log" }"""));
        Assert.Throws<JsonException>(() => Parse("""{ "replaces": { "0": "cuo:ui/system-log" } }"""));
    }

    [Fact]
    public void IsReplaced_answers_for_enabled_mods_only()
    {
        var control = new ModControl();
        control.Mods.Add(new ModInfo { Name = "journal", Replaces = new[] { "cuo:ui/system-log" } });

        Assert.True(control.IsReplaced("cuo:ui/system-log"));
        // Exact match: a feature id is an id, not a prefix, and not case-insensitive.
        Assert.False(control.IsReplaced("cuo:ui/system"));
        Assert.False(control.IsReplaced("CUO:UI/SYSTEM-LOG"));

        // Disabling hands the feature straight back to the host — this is what makes
        // the host's per-frame guard reversible.
        control.Mods[0].Enabled = false;
        Assert.False(control.IsReplaced("cuo:ui/system-log"));
    }

    [Fact]
    public void IsReplaced_is_false_with_no_mods()
        => Assert.False(new ModControl().IsReplaced("cuo:ui/system-log"));
}
