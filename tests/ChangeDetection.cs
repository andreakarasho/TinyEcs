using System.Collections.Generic;
using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests
{
    using Stage = TinyEcs.Bevy.Stage;

    public class ChangeDetectionTests
    {
        private struct Position
        {
            public int X;
            public int Y;
        }

        private struct Velocity
        {
            public int X;
            public int Y;
        }

        [Fact]
        public void AddedFilterDetectsNewlyAddedComponents()
        {
            using var world = new World();
            var app = new App(world);
            var addedEntities = new List<ulong>();

            ulong entityId = 0;

            app.AddSystem(Stage.Startup, w =>
            {
                var entity = w.Entity();
                entity.Set(new Position { X = 10, Y = 20 });
                entityId = entity.ID;
            });

            app.AddSystem(Stage.Update, w =>
            {
                foreach (var (entity, _) in w.Query<Data<Position>, Added<Position>>())
                {
                    addedEntities.Add(entity.Ref);
                }
            });

            // First run - entity created in Startup, Added<Position> checked in Update
            app.Run();
            Assert.Single(addedEntities);
            Assert.Equal(entityId, addedEntities[0]);

            addedEntities.Clear();

            // Second run - component not newly added, should not appear
            app.Run();
            Assert.Empty(addedEntities);
        }

        [Fact]
        public void ChangedFilterDetectsModifiedComponents()
        {
            using var world = new World();
            var app = new App(world);
            var changedEntities = new List<ulong>();

            ulong entityId = 0;
            bool shouldModify = false;

            app.AddSystem(Stage.Startup, w =>
            {
                var entity = w.Entity();
                entity.Set(new Position { X = 10, Y = 20 });
                entityId = entity.ID;
            });

            app.AddSystem(Stage.Update, w =>
            {
                if (shouldModify)
                {
                    var entity = w.Entity(entityId);
                    var pos = entity.Get<Position>();
                    pos.X = 100;
                    entity.Set(pos);
                    shouldModify = false;
                }
            });

            app.AddSystem(Stage.Update, w =>
            {
                foreach (var (entity, _) in w.Query<Data<Position>, Changed<Position>>())
                {
                    changedEntities.Add(entity.Ref);
                }
            });

            // First run - entity just created, Changed should detect it
            app.Run();
            Assert.Single(changedEntities);
            Assert.Equal(entityId, changedEntities[0]);

            changedEntities.Clear();

            // Second run - no modifications
            app.Run();
            Assert.Empty(changedEntities);

            // Third run - modify component
            shouldModify = true;
            app.Run();
            // Changes are not visible in the same frame
            Assert.Empty(changedEntities);

            // Fourth run - check detects changes from previous frame
            app.Run();
            Assert.Single(changedEntities);
            Assert.Equal(entityId, changedEntities[0]);
        }

        [Fact]
        public void AddedFilterDoesNotDetectExistingComponents()
        {
            using var world = new World();
            var app = new App(world);
            var addedCount = 0;

            app.AddSystem(Stage.Startup, w =>
            {
                w.Entity().Set(new Position { X = 1, Y = 2 });
            });

            app.AddSystem(Stage.Update, w =>
            {
                foreach (var _ in w.Query<Data<Position>, Added<Position>>())
                {
                    addedCount++;
                }
            });

            // First run - component added in Startup
            app.Run();
            Assert.Equal(1, addedCount);

            addedCount = 0;

            // Subsequent runs - component exists but not newly added
            app.Run();
            Assert.Equal(0, addedCount);

            app.Run();
            Assert.Equal(0, addedCount);
        }

        [Fact]
        public void ChangedFilterDetectsMultipleModifications()
        {
            using var world = new World();
            var app = new App(world);
            var changedCounts = new List<int>();

            var entities = new List<ulong>();

            app.AddSystem(Stage.Startup, w =>
            {
                for (int i = 0; i < 3; i++)
                {
                    var entity = w.Entity();
                    entity.Set(new Position { X = i, Y = i });
                    entities.Add(entity.ID);
                }
            });

            app.AddSystem(Stage.Update, w =>
            {
                int count = 0;
                foreach (var _ in w.Query<Data<Position>, Changed<Position>>())
                {
                    count++;
                }
                changedCounts.Add(count);
            });

            // First run - all 3 entities just created
            app.Run();
            Assert.Equal(3, changedCounts[0]);

            // Second run - no changes
            app.Run();
            Assert.Equal(0, changedCounts[1]);

            // Modify one entity
            var e1 = world.Entity(entities[1]);
            var pos = e1.Get<Position>();
            pos.X = 999;
            e1.Set(pos);

            // Third run - only modified entity detected
            app.Run();
            Assert.Equal(1, changedCounts[2]);
        }

        [Fact]
        public void CombinedAddedAndChangedFilters()
        {
            using var world = new World();
            var app = new App(world);
            var addedCount = 0;
            var changedCount = 0;

            ulong entityId = 0;

            app.AddSystem(Stage.Startup, w =>
            {
                var entity = w.Entity();
                entity.Set(new Position { X = 5, Y = 10 });
                entityId = entity.ID;
            });

            app.AddSystem(Stage.Update, w =>
            {
                addedCount = 0;
                changedCount = 0;

                foreach (var _ in w.Query<Data<Position>, Added<Position>>())
                {
                    addedCount++;
                }

                foreach (var _ in w.Query<Data<Position>, Changed<Position>>())
                {
                    changedCount++;
                }
            });

            // First run - component added and changed
            app.Run();
            Assert.Equal(1, addedCount);
            Assert.Equal(1, changedCount);

            // Second run - neither added nor changed
            app.Run();
            Assert.Equal(0, addedCount);
            Assert.Equal(0, changedCount);

            // Modify component
            var entity = world.Entity(entityId);
            var pos = entity.Get<Position>();
            pos.X = 100;
            entity.Set(pos);

            // Third run - changed but not added
            app.Run();
            Assert.Equal(0, addedCount);
            Assert.Equal(1, changedCount);
        }

        [Fact]
        public void SystemParamChangedSpansSkippedFrames()
        {
            // A Changed<T> consumer that is RunIf-gated out of the frame on
            // which the change lands must still observe it when it next runs.
            // The per-system change window must span the skipped frames rather
            // than collapse to "the single previous tick".
            using var world = new World();
            var app = new App(world);

            ulong id = 0;
            bool gateOpen = true;
            bool doModify = false;
            int detected = 0;
            int detectorRuns = 0;

            app.AddSystem(Stage.Startup, w =>
            {
                var e = w.Entity();
                e.Set(new Position { X = 1, Y = 1 });
                id = e.ID;
            });

            // Modifier — runs every frame, mutates only when asked.
            app.AddSystem(Stage.Update, w =>
            {
                if (!doModify) return;
                var e = w.Entity(id);
                var p = e.Get<Position>();
                p.X += 1;
                e.Set(p);
                doModify = false;
            });

            // Detector — system-param Query<> with Changed<>, gated by RunIf.
            app.AddSystem((Query<Data<Position>, Filter<Changed<Position>>> q) =>
            {
                detectorRuns++;
                int c = 0;
                foreach (var _ in q) c++;
                detected = c;
            })
            .InStage(Stage.Update)
            .RunIf(_ => gateOpen)
            .Build();

            // Frame 1: gate open, detector observes the startup creation.
            app.Run();
            Assert.Equal(1, detectorRuns);
            Assert.Equal(1, detected);

            // Gate the detector out, then modify while it's skipped.
            gateOpen = false;
            app.Run();          // frame 2: no modify, detector skipped
            doModify = true;
            app.Run();          // frame 3: modify lands here, detector skipped
            app.Run();          // frame 4: still gated
            Assert.Equal(1, detectorRuns); // never ran across frames 2-4

            // Reopen: the detector must catch the frame-3 change.
            gateOpen = true;
            detected = -1;
            app.Run();          // frame 5
            Assert.Equal(2, detectorRuns);
            Assert.Equal(1, detected); // would be 0 under the old [tick-1,tick) window
        }

        [Fact]
        public void SystemParamChangedEveryFrameFiresOnceAfterChange()
        {
            // Regression guard: for a detector that runs every frame, the new
            // per-system window must behave exactly like the legacy
            // [CurrentTick-1, CurrentTick) window — change visible one frame
            // later, for one frame only.
            using var world = new World();
            var app = new App(world);

            ulong id = 0;
            bool doModify = false;
            var fired = new List<int>();

            app.AddSystem(Stage.Startup, w =>
            {
                var e = w.Entity();
                e.Set(new Position { X = 0, Y = 0 });
                id = e.ID;
            });

            app.AddSystem(Stage.Update, w =>
            {
                if (!doModify) return;
                var e = w.Entity(id);
                var p = e.Get<Position>();
                p.X += 1;
                e.Set(p);
                doModify = false;
            });

            app.AddSystem((Query<Data<Position>, Filter<Changed<Position>>> q) =>
            {
                int c = 0;
                foreach (var _ in q) c++;
                fired.Add(c);
            })
            .InStage(Stage.Update)
            .Build();

            app.Run();                   // frame1: sees creation
            app.Run();                   // frame2: quiet
            doModify = true;
            app.Run();                   // frame3: modify lands (not visible same frame)
            app.Run();                   // frame4: change visible
            app.Run();                   // frame5: quiet again

            Assert.Equal(1, fired[0]);   // creation
            Assert.Equal(0, fired[1]);
            Assert.Equal(0, fired[2]);   // same-frame change not visible
            Assert.Equal(1, fired[3]);   // detected one frame later
            Assert.Equal(0, fired[4]);   // and only once
        }

        [Fact]
        public void MarkChangedFilterMarksComponentAsChanged()
        {
            using var world = new World();
            var app = new App(world);
            var changedDetections = new List<bool>();

            ulong entityId = 0;

            app.AddSystem(Stage.Startup, w =>
            {
                var entity = w.Entity();
                entity.Set(new Position { X = 1, Y = 2 });
                entityId = entity.ID;
            });

            app.AddSystem(Stage.Update, w =>
            {
                bool detected = false;
                foreach (var _ in w.Query<Data<Position>, Changed<Position>>())
                {
                    detected = true;
                }
                changedDetections.Add(detected);
            });

            // First run - just created
            app.Run();
            Assert.True(changedDetections[0]);

            // Second run - no changes
            app.Run();
            Assert.False(changedDetections[1]);

            // Mark as changed without modifying
            foreach (var _ in world.Query<Data<Position>, MarkChanged<Position>>())
            {
                // Just iterating marks it as changed
            }

            // Third run - should detect change
            app.Run();
            Assert.True(changedDetections[2]);
        }
    }
}
