using TinyEcs;
using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests
{
    using Stage = TinyEcs.Bevy.Stage;

    // SystemProfiler is a per-App resource (auto-registered by the App ctor). It publishes
    // each closed window's table to a SystemProfileReport resource so a host can drain it
    // through the ECS resource graph — e.g. a Res<ILogger> system. These exercise both arms
    // of the dump's output branch: report present (publish) and absent (Console fallback).
    // No global state to restore — each App owns its own SystemProfiler instance.
    public class SystemProfilingTests
    {
        [Fact]
        public void Profiler_publishes_table_to_report_resource()
        {
            using var world = new World();
            var app = new App(world);

            var profiler = app.GetResource<SystemProfiler>(); // auto-registered by App
            profiler.Enabled = true;
            profiler.ReportIntervalSeconds = 0; // close a window as soon as one can

            var report = new SystemProfileReport();
            app.AddResource(report);

            // A per-frame system so the profile has at least one timed row.
            app.AddSystem(Stage.Update, static _ => { });

            // First non-startup window arms the timer; a later one closes it and dumps.
            for (var i = 0; i < 5; i++)
                app.Update();

            Assert.True(report.TryRead(out var table), "profiler published no table to the report resource");
            Assert.Contains("[system-profile]", table);
        }

        [Fact]
        public void Profiler_without_report_resource_does_not_throw()
        {
            using var world = new World();
            var app = new App(world);

            var profiler = app.GetResource<SystemProfiler>();
            profiler.Enabled = true;
            profiler.ReportIntervalSeconds = 0;

            app.AddSystem(Stage.Update, static _ => { });

            // No SystemProfileReport registered → dump falls back to Console.WriteLine.
            for (var i = 0; i < 5; i++)
                app.Update();
        }
    }
}
