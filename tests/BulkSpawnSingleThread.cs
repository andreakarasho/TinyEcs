using TinyEcs;
using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests
{
    using Stage = TinyEcs.Bevy.Stage;

    // Repro for the wasm-guest terrain bug: bulk Spawn+Insert into one archetype in a
    // single system run keeps only one entity in the guest. This test checks whether the
    // same fails on the desktop JIT with ThreadingMode.Single (→ logic bug) or passes
    // (→ NativeAOT-LLVM codegen bug).
    public class BulkSpawnSingleThreadTests
    {
        private struct Marker { public int V; }

        [Fact]
        public void BulkSpawnSingleThreadedKeepsAllEntities()
        {
            using var world = new World();
            var app = new App(world, ThreadingMode.Single);

            app.AddSystem(Stage.Update, (Commands c, Local<bool> done) =>
            {
                if (done.Value) return;
                done.Value = true;
                for (int i = 0; i < 500; i++)
                    c.Spawn().Insert(new Marker { V = i });
            });

            app.Update();
            app.Update();

            int count = 0;
            foreach (var _ in world.Query<Data<Marker>>())
                count++;

            Assert.Equal(500, count);
        }
    }
}
