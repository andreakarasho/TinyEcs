using System.Text.Json;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// mod.json's `ruleset.replaces` — the rule that lets an installed mod stand a host
// feature down (a host asks ModControl.IsReplaced before building its own UI). Parsing
// is deliberately forgiving: a ruleset is a declaration, and a typo in one must not
// stop the mod loading, so every malformed shape reads as "replaces nothing".
public class ModRulesetTests
{
    private static ModManifest Manifest(string rulesetJson) => new()
    {
        Name = "journal",
        Ruleset = JsonDocument.Parse(rulesetJson).RootElement.Clone(),
    };

    [Fact]
    public void ReadReplaces_reads_the_feature_ids()
    {
        var manifest = Manifest("""{ "replaces": ["cuo:ui/system-log", "cuo:ui/other"] }""");
        Assert.Equal(new[] { "cuo:ui/system-log", "cuo:ui/other" }, manifest.ReadReplaces());
    }

    [Theory]
    // No rule at all — the common case, every mod that replaces nothing.
    [InlineData("{}")]
    // Present but the wrong shape, or carrying junk: still "replaces nothing".
    [InlineData("""{ "replaces": "cuo:ui/system-log" }""")]
    [InlineData("""{ "replaces": {} }""")]
    [InlineData("""{ "replaces": [] }""")]
    [InlineData("""{ "replaces": [1, null, true] }""")]
    [InlineData("""{ "replaces": [""] }""")]
    // A ruleset that isn't even an object.
    [InlineData("[]")]
    [InlineData("null")]
    public void ReadReplaces_is_empty_for_anything_malformed(string ruleset)
        => Assert.Empty(Manifest(ruleset).ReadReplaces());

    [Fact]
    public void ReadReplaces_keeps_the_strings_and_drops_the_rest()
    {
        var manifest = Manifest("""{ "replaces": [1, "cuo:ui/system-log", null] }""");
        Assert.Equal(new[] { "cuo:ui/system-log" }, manifest.ReadReplaces());
    }

    [Fact]
    public void ReadReplaces_ignores_a_default_Ruleset()
    {
        // Ruleset absent from the json entirely — JsonElement stays default(Undefined).
        Assert.Empty(new ModManifest { Name = "journal" }.ReadReplaces());
    }

    [Fact]
    public void IsReplaced_answers_for_enabled_mods_only()
    {
        var control = new ModControl();
        control.Mods.Add(new ModInfo { Name = "journal", Replaces = new[] { "cuo:ui/system-log" } });

        Assert.True(control.IsReplaced("cuo:ui/system-log"));
        // Exact match: a feature id is an id, not a prefix.
        Assert.False(control.IsReplaced("cuo:ui/system"));
        Assert.False(control.IsReplaced("CUO:UI/SYSTEM-LOG"));

        // Disabling the mod hands the feature straight back to the host — this is what
        // makes the host's per-frame guard reversible.
        control.Mods[0].Enabled = false;
        Assert.False(control.IsReplaced("cuo:ui/system-log"));
    }

    [Fact]
    public void IsReplaced_is_false_with_no_mods()
        => Assert.False(new ModControl().IsReplaced("cuo:ui/system-log"));
}
