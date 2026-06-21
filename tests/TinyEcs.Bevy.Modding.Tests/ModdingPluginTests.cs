using System.Text.Json.Serialization;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Mirrors the counter mod's component shape: it spawns "cuo:test/counter" with
// JSON {"Value":N} and increments it every tick. The type-path string is just a
// registry key — the test registers its own struct against it. Top-level (not
// nested) so the System.Text.Json source generator augments the context.
public struct TestCounter { public int Value { get; set; } }

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(TestCounter))]
internal partial class TestJsonContext : JsonSerializerContext { }

// Standalone end-to-end test for the generic component-model modding runtime.
// Drives a real Rust guest component (ecs_counter.wasm, imports ONLY
// tinyecs:modding) through the real ModdingPlugin + real wasmtime fork + a real
// TinyEcs World — with NO game-specific host glue. References only the lib +
// TinyEcs, so a green run proves the runtime is reusable on its own.
public class ModdingPluginTests
{
    // Fresh isolated mod folder containing only the counter wasm, cwd pointed at
    // it (the plugin scans <cwd>/ecs-mods/*.wasm), and a bare App + lib plugin
    // configured with a registry that exposes just the counter.
    private static App BuildCounterApp()
    {
        var baseDir = AppContext.BaseDirectory;
        var src = Path.Combine(baseDir, "fixtures", "ecs_counter.wasm");
        Assert.True(File.Exists(src), $"guest component missing: {src}");

        var root = Path.Combine(Path.GetTempPath(), "tinyecs-bevy-modding-counter");
        var modDir = Path.Combine(root, "ecs-mods");
        if (Directory.Exists(modDir))
            Directory.Delete(modDir, recursive: true);
        Directory.CreateDirectory(modDir);
        File.Copy(src, Path.Combine(modDir, "ecs_counter.wasm"), overwrite: true);
        Directory.SetCurrentDirectory(root);

        var registry = new ModComponentRegistry();
        registry.Register("cuo:test/counter", new ModComponent<TestCounter>(TestJsonContext.Default.TestCounter));

        var app = new App(ThreadingMode.Single);
        app.AddResource(new ModdingConfig { Registry = registry });
        app.AddPlugin<ModdingPlugin>();
        return app;
    }

    [Fact]
    public void Counter_mod_round_trips_through_the_generic_plugin_with_no_host_glue()
    {
        var app = BuildCounterApp();

        app.RunStartup(); // load → setup() → mod-startup system (spawns counter)
        app.Update();     // mod-tick increments every counter each frame
        app.Update();
        app.Update();

        var world = app.GetWorld();
        var found = false;
        var max = int.MinValue;
        foreach ((var _, var c) in world.Query<Data<TestCounter>>())
        {
            found = true;
            max = Math.Max(max, c.Ref.Value);
        }

        Assert.True(found, "mod did not spawn a counter entity (generic spawn path failed)");
        Assert.True(max >= 3, $"counter did not increment across the wasm boundary: max={max}");
    }
}
