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
        public void SystemParamChangedEveryFrameSeesSameFrameWriteExactlyOnce()
        {
            // bevy_ecs window (lastRun, thisRun] with a per-system-run tick: a
            // producer that ran EARLIER in this same stage is visible to the
            // detector on the producing frame (no one-frame lag), and the
            // exclusive lower bound means exactly once.
            //
            // The ordering is EXPLICIT on purpose. Same-frame visibility is a
            // statement about tick order, so the two systems must not share a
            // parallel batch — exactly as Bevy requires an explicit
            // `.before/.after` for same-frame data flow.
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

            app.AddSystem((TinyEcs.World w) =>
            {
                if (!doModify) return;
                var e = w.Entity(id);
                var p = e.Get<Position>();
                p.X += 1;
                e.Set(p);
                doModify = false;
            })
            .InStage(Stage.Update)
            .Label("modifier")
            .Build();

            app.AddSystem((Query<Data<Position>, Filter<Changed<Position>>> q) =>
            {
                int c = 0;
                foreach (var _ in q) c++;
                fired.Add(c);
            })
            .InStage(Stage.Update)
            .After("modifier")
            .Build();

            app.Run();                   // frame1: sees creation
            app.Run();                   // frame2: quiet
            doModify = true;
            app.Run();                   // frame3: modify lands AND is seen
            app.Run();                   // frame4: not repeated
            app.Run();                   // frame5: quiet

            Assert.Equal(1, fired[0]);   // creation
            Assert.Equal(0, fired[1]);
            Assert.Equal(1, fired[2]);   // same frame as the write
            Assert.Equal(0, fired[3]);   // and only once
            Assert.Equal(0, fired[4]);
        }

        [Fact]
        public void LaterStageConsumerSeesASetChangedFromThisFrameExactlyOnce()
        {
            // (a) + (b): the producer runs in Stage.Update and marks a row;
            // SetChanged is deferred, so the mark is stamped by the Update
            // stage's command flush -- which takes a tick of its own, after
            // every Update system and before any PostUpdate system. The
            // PostUpdate consumer therefore sees it the SAME frame, and the
            // window's exclusive lower bound means it sees it only once.
            using var world = new World();
            var app = new App(world, ThreadingMode.Single);

            ulong id = 0;
            app.AddSystem(Stage.Startup, w =>
            {
                var e = w.Entity();
                e.Set(new Position { X = 1 });
                id = e.ID;
            });

            var produce = false;
            app.AddSystem((Query<Data<Position>> q) =>
            {
                if (!produce) return;
                produce = false;
                var (_, pos) = q.Get(id);
                pos.Ref.X += 1;
                q.SetChanged<Position>(id);
            })
            .InStage(Stage.Update).Build();

            var seen = new List<int>();
            app.AddSystem((Query<Data<Position>, Filter<Changed<Position>>> q) =>
            {
                var c = 0;
                foreach (var _ in q) c++;
                seen.Add(c);
            })
            .InStage(Stage.PostUpdate).Build();

            app.Run();      // startup + first frame (sees the creation)
            app.Update();   // quiet
            seen.Clear();

            produce = true;
            app.Update();   // the mark lands AND is observed
            app.Update();
            app.Update();

            Assert.Equal(new[] { 1, 0, 0 }, seen);
        }

        [Fact]
        public void TwoConsumersInTheSameStageEachSeeAChangeOnce()
        {
            // (c): last_run is per SYSTEM, not global -- one consumer observing
            // a change must not consume it on the other's behalf, and neither
            // may see it twice.
            using var world = new World();
            var app = new App(world, ThreadingMode.Single);

            ulong id = 0;
            app.AddSystem(Stage.Startup, w =>
            {
                var e = w.Entity();
                e.Set(new Position { X = 1 });
                id = e.ID;
            });

            var produce = false;
            app.AddSystem((Query<Data<Position>> q) =>
            {
                if (!produce) return;
                produce = false;
                var (_, pos) = q.Get(id);
                pos.Ref.X += 1;
                q.SetChanged<Position>(id);
            })
            .InStage(Stage.Update).Build();

            var first = new List<int>();
            var second = new List<int>();
            app.AddSystem((Query<Data<Position>, Filter<Changed<Position>>> q) =>
            {
                var c = 0;
                foreach (var _ in q) c++;
                first.Add(c);
            })
            .InStage(Stage.PostUpdate).Build();
            app.AddSystem((Query<Data<Position>, Filter<Changed<Position>>> q) =>
            {
                var c = 0;
                foreach (var _ in q) c++;
                second.Add(c);
            })
            .InStage(Stage.PostUpdate).Build();

            app.Run();
            app.Update();
            first.Clear();
            second.Clear();

            produce = true;
            app.Update();
            app.Update();

            Assert.Equal(new[] { 1, 0 }, first);
            Assert.Equal(new[] { 1, 0 }, second);
        }

        [Fact]
        public void GatedConsumerStillSeesASetChangedThatLandedWhileItWasSkipped()
        {
            // (d): a RunIf-gated consumer does not Fetch, so its last_run stays
            // put and the window it opens on its next run spans every skipped
            // frame. The change must survive the gap -- exactly once.
            using var world = new World();
            var app = new App(world, ThreadingMode.Single);

            ulong id = 0;
            app.AddSystem(Stage.Startup, w =>
            {
                var e = w.Entity();
                e.Set(new Position { X = 1 });
                id = e.ID;
            });

            var produce = false;
            app.AddSystem((Query<Data<Position>> q) =>
            {
                if (!produce) return;
                produce = false;
                var (_, pos) = q.Get(id);
                pos.Ref.X += 1;
                q.SetChanged<Position>(id);
            })
            .InStage(Stage.Update).Build();

            var gateOpen = true;
            var seen = new List<int>();
            app.AddSystem((Query<Data<Position>, Filter<Changed<Position>>> q) =>
            {
                var c = 0;
                foreach (var _ in q) c++;
                seen.Add(c);
            })
            .InStage(Stage.PostUpdate)
            .RunIf(_ => gateOpen)
            .Build();

            app.Run();
            app.Update();
            seen.Clear();

            gateOpen = false;
            produce = true;
            app.Update();   // mark lands here, consumer gated out
            app.Update();
            app.Update();
            Assert.Empty(seen); // never ran across those three frames

            gateOpen = true;
            app.Update();   // catches the change from three frames ago
            app.Update();   // and does not repeat it

            Assert.Equal(new[] { 1, 0 }, seen);
        }

        [Fact]
        public void ChangeDetectionSurvivesTheThirtyTwoBitTickWrap()
        {
            // (e): the tick now advances per system run, so uint32 wraps in
            // hours of uptime, not years. Park the counter just below the wrap
            // and drive frames across it: every real change must be reported
            // exactly once, and a quiet frame must report nothing. A raw
            // magnitude comparison (tick >= lastRun) fails this immediately --
            // after the wrap every stored tick looks "newer than now".
            using var world = new World();
            var app = new App(world, ThreadingMode.Single);

            ulong id = 0;
            app.AddSystem(Stage.Startup, w =>
            {
                var e = w.Entity();
                e.Set(new Position { X = 0 });
                id = e.ID;
            });

            var produce = false;
            app.AddSystem((Query<Data<Position>> q) =>
            {
                if (!produce) return;
                produce = false;
                var (_, pos) = q.Get(id);
                pos.Ref.X += 1;
                q.SetChanged<Position>(id);
            })
            .InStage(Stage.Update).Build();

            var runs = 0;
            var fired = 0;
            app.AddSystem((Query<Data<Position>, Filter<Changed<Position>>> q) =>
            {
                runs++;
                foreach (var _ in q) fired++;
            })
            .InStage(Stage.PostUpdate).Build();

            app.Run();          // startup + first frame: observes the creation
            app.Update();       // settle
            runs = 0;
            fired = 0;

            // Each frame burns a handful of ticks (one per system run, one per
            // stage flush), so 60 frames from here crosses the wrap and keeps
            // going well past it.
            world.SetTicksForTesting(uint.MaxValue - 30);

            var expected = 0;
            for (var frame = 0; frame < 60; frame++)
            {
                produce = frame % 5 == 0;
                if (produce)
                    expected++;

                app.Update();

                Assert.Equal(expected, fired);
            }

            Assert.Equal(60, runs);
            Assert.Equal(12, expected);
            Assert.True(world.CurrentTick < uint.MaxValue - 30, "the tick never wrapped; the test proves nothing");
        }

        [Fact]
        public void FrameCountIsFrameGranularUnlikeTheChangeTick()
        {
            // The counter to hand to scripting/modding guests. CurrentTick moves
            // per system run and is useless as a frame number.
            using var world = new World();
            var app = new App(world, ThreadingMode.Single);
            app.AddSystem(Stage.Update, _ => { });
            app.AddSystem(Stage.Update, _ => { });

            app.Run();          // startup frame + first frame
            var frames = world.FrameCount;
            var ticks = world.CurrentTick;

            app.Update();
            app.Update();

            Assert.Equal(frames + 2, world.FrameCount);
            Assert.True(world.CurrentTick - ticks > 2, "the change tick must advance faster than the frame count");
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

        [Fact]
        public void QuerySetChangedMarksARowImmediatelyAndWhileDeferred()
        {
            using var world = new World();
            var app = new App(world);

            ulong entityId = 0;
            app.AddSystem(Stage.Startup, w =>
            {
                var e = w.Entity();
                e.Set(new Position { X = 1 });
                entityId = e.ID;
            });

            var deferred = false;
            var mark = false;
            app.AddSystem((TinyEcs.Bevy.Query<Data<Position>> q) =>
            {
                if (!mark) return;
                mark = false;
                // In-place write: bumps no tick on its own.
                var (_, pos) = q.Get(entityId);
                pos.Ref.X += 1;

                if (deferred)
                {
                    // Mid-system deferral (what an observer or a nested
                    // structural change opens): the mark must survive the
                    // queue and land on Merge.
                    world.BeginDeferred();
                    q.SetChanged<Position>(entityId);
                    Assert.True(world.IsDeferred);
                    world.EndDeferred();
                }
                else
                {
                    q.SetChanged<Position>(entityId);
                }
            })
            .InStage(Stage.Update).Build();

            var detected = new List<bool>();
            app.AddSystem((TinyEcs.Bevy.Query<Data<Position>, Filter<Changed<Position>>> q) =>
            {
                var any = false;
                foreach (var _ in q) any = true;
                detected.Add(any);
            })
            .InStage(Stage.PostUpdate).Build();

            app.Run();      // spawn frame
            app.Update();   // quiet frame
            detected.Clear();

            mark = true;
            app.Update();   // mark in Stage.Update, observed in Stage.PostUpdate
            app.Update();   // not repeated
            // SetChanged inside a system is deferred; the Stage.Update command
            // flush stamps it with a tick of its own, which is why the
            // PostUpdate consumer sees it on the SAME frame — and once.
            Assert.Equal(new[] { true, false }, detected);

            detected.Clear();
            deferred = true;
            mark = true;
            app.Update();
            app.Update();
            Assert.Equal(new[] { true, false }, detected);
        }

        // A run condition is a system in its own right: it must get a fresh
        // change tick before its Query params Fetch, or it reads the stale
        // thread-static left by the last system on that worker and misses the
        // producer's same-frame mark.
        [Fact]
        public void RunIfChangedConditionSeesSameFrameProducerExactlyOnce()
        {
            using var world = new World();
            var app = new App(world);

            ulong entityId = 0;
            app.AddSystem(Stage.Startup, w =>
            {
                var e = w.Entity();
                e.Set(new Position { X = 1 });
                entityId = e.ID;
            });

            var produce = false;
            app.AddSystem((TinyEcs.Bevy.Query<Data<Position>> q) =>
            {
                if (!produce) return;
                produce = false;
                var (_, pos) = q.Get(entityId);
                pos.Ref.X += 1;
                q.SetChanged<Position>(entityId);
            })
            .InStage(Stage.Update).Build();

            var gatedRuns = 0;
            app.AddSystem(() => gatedRuns++)
                .InStage(Stage.PostUpdate)
                .RunIf((TinyEcs.Bevy.Query<Data<Position>, Filter<Changed<Position>>> q) =>
                {
                    foreach (var _ in q) return true;
                    return false;
                })
                .Build();

            app.Run();      // spawn frame
            app.Update();   // quiet frame
            gatedRuns = 0;

            produce = true;
            app.Update();
            Assert.Equal(1, gatedRuns);  // same-frame: mark in Update, gate in PostUpdate

            app.Update();
            Assert.Equal(1, gatedRuns);  // and not re-fired once the window moved on
        }
    }
}
