using System.Collections.Generic;
using TinyEcs;
using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests
{
    using Stage = TinyEcs.Bevy.Stage;

    public class BevyAppTests
    {
        private enum GameState
        {
            Menu,
            Playing
        }

        private readonly record struct ScoreEvent(int Value);

        private struct Position
        {
            public int X;
        }

        private struct Velocity
        {
            public int Value;
        }

        private struct Health
        {
            public int Value;
        }

        private sealed class ScoreTracker
        {
            public int Baseline;
        }

        private sealed class RemovalLog
        {
            public int Count;
            public ulong LastEntity;
        }

        private sealed class MutableCounter
        {
            public int Value;
        }

        private struct ParentTag { }
        private struct ChildTag { }

        [Fact]
        public void AppExecutesStagesInConfiguredOrder()
        {
            using var world = new World();
            var app = new App(world);
            var customStage = Stage.Custom("Custom");
            app.AddStage(customStage)
                .After(Stage.PreUpdate)
                .Before(Stage.Update)
                .Build();

            var executed = new List<string>();

            app.AddSystem(Stage.PreUpdate, w => executed.Add("PreUpdate"));
            app.AddSystem(customStage, w => executed.Add("Custom"));
            app.AddSystem(Stage.Update, w => executed.Add("Update"));

            app.Run();

            Assert.Equal(new[] { "PreUpdate", "Custom", "Update" }, executed);
        }

        [Fact]
        public void AppRunsStateTransitionSystems()
        {
            using var world = new World();
            var app = new App(world);
            var transitions = new List<string>();

            app.AddState(GameState.Menu);

            app.AddSystem(w => transitions.Add("EnterMenu"))
                .OnEnter(GameState.Menu)
                .Build();

            app.AddSystem(w => transitions.Add("ExitMenu"))
                .OnExit(GameState.Menu)
                .Build();

            app.AddSystem(w => transitions.Add("EnterPlaying"))
                .OnEnter(GameState.Playing)
                .Build();

            app.Run();

            Assert.Contains("EnterMenu", transitions);

            transitions.Clear();

            world.SetState(GameState.Playing);
            app.Run();

            Assert.Equal(new[] { "ExitMenu", "EnterPlaying" }, transitions);
        }

        [Fact]
        public void NextStateQueuesTransitionsUntilEndOfFrame()
        {
            using var world = new World();
            var app = new App(world);
            app.AddState(GameState.Menu);

            var observed = new List<GameState>();

            app.AddSystem((Res<State<GameState>> state, ResMut<NextState<GameState>> next) =>
            {
                observed.Add(state.Value.Current);
                ref var nextState = ref next.Value;
                if (state.Value.Current == GameState.Menu && !nextState.IsQueued)
                {
                    nextState.Set(GameState.Playing);
                }
            })
            .InStage(Stage.Update)
            .Build();

            app.Run();

            Assert.Equal(new[] { GameState.Menu }, observed);
            Assert.Equal(GameState.Playing, world.GetState<GameState>());

            observed.Clear();
            app.Run();
            Assert.Equal(new[] { GameState.Playing }, observed);
        }

        [Fact]
        public void AppFiresSpawnAndDespawnTriggers()
        {
            using var world = new World();
            var app = new App(world);
            var spawns = new List<ulong>();
            var despawns = new List<ulong>();

            app.Observe<OnSpawn>((_, trigger) => spawns.Add(trigger.EntityId));
            app.Observe<OnDespawn>((_, trigger) => despawns.Add(trigger.EntityId));

            ulong spawnedEntity = 0;
            bool deleted = false;

            app.AddSystem(Stage.Startup, w =>
            {
                if (spawnedEntity == 0)
                {
                    spawnedEntity = w.Entity().ID;
                }
            });

            app.AddSystem(Stage.Update, w =>
            {
                if (!deleted && spawnedEntity != 0)
                {
                    w.Entity(spawnedEntity).Delete();
                    deleted = true;
                }
            });

            app.Run();
            app.Run();

            Assert.Single(spawns);
            Assert.Equal(spawnedEntity, spawns[0]);
            Assert.Single(despawns);
            Assert.Equal(spawnedEntity, despawns[0]);
        }

        [Fact]
        public void ObserverWithSystemParamsCanAccessResources()
        {
            using var world = new World();
            var app = new App(world);

            var tracker = new ScoreTracker { Baseline = 99 };
            var log = new RemovalLog();

            app.AddResource(tracker)
               .AddResource(log);

            app.Observe<OnRemove<Velocity>, TinyEcs.Bevy.Res<ScoreTracker>, TinyEcs.Bevy.ResMut<RemovalLog>>((trigger, score, removalLog) =>
            {
                Assert.Equal(99, score.Value.Baseline);
                removalLog.Value.Count++;
                removalLog.Value.LastEntity = trigger.EntityId;
            });

            ulong entityWithVelocity = 0;
            ulong removedEntity = 0;

            app.AddSystem(Stage.Startup, w =>
            {
                var entity = w.Entity();
                entity.Set(new Velocity { Value = 42 });
                entityWithVelocity = entity.ID;
            });

            app.AddSystem(Stage.Update, w =>
            {
                if (entityWithVelocity != 0)
                {
                    removedEntity = entityWithVelocity;
                    w.Entity(entityWithVelocity).Unset<Velocity>();
                    entityWithVelocity = 0;
                }
            });

            app.Run();

            Assert.NotEqual(0UL, removedEntity);
            Assert.Equal(1, log.Count);
            Assert.Equal(removedEntity, log.LastEntity);
        }

        [Fact]
        public void ComponentObserversFireOnInsertAndRemove()
        {
            using var world = new World();
            var app = new App(world);
            var events = new List<string>();

            app.Observe<OnInsert<Position>>((_, trigger) =>
                events.Add($"insert:{trigger.EntityId}:{trigger.Component.X}"));
            app.Observe<OnRemove<Position>>((_, trigger) =>
                events.Add($"remove:{trigger.EntityId}"));

            ulong entityId = 0;
            ulong createdEntityId = 0;

            app.AddSystem(Stage.Startup, w =>
            {
                var entity = w.Entity();
                entity.Set(new Position { X = 42 });
                entityId = entity.ID;
                createdEntityId = entity.ID;
            });

            app.AddSystem(Stage.Update, w =>
            {
                if (entityId != 0)
                {
                    w.Entity(entityId).Unset<Position>();
                    entityId = 0;
                }
            });

            app.Run();

            Assert.Equal(new[]
            {
                $"insert:{createdEntityId}:42",
                $"remove:{createdEntityId}"
            }, events);
        }

        [Fact]
        public void ParameterizedSystemWithCommandsAppliesDeferredWork()
        {
            using var world = new World();
            var app = new App(world);

            var counter = new MutableCounter();
            app.AddResource(counter);

            var spawns = new List<ulong>();
            var inserts = new List<int>();

            app.Observe<OnSpawn>((_, trigger) => spawns.Add(trigger.EntityId));
            app.Observe<OnInsert<Health>>((_, trigger) => inserts.Add(trigger.Component.Value));

            var system = SystemFunctionAdapters.Create<TinyEcs.Bevy.ResMut<MutableCounter>, TinyEcs.Bevy.Commands>((counterParam, commands) =>
            {
                counterParam.Value.Value++;
                var entity = commands.Spawn();
                entity.Insert(new Health { Value = counterParam.Value.Value });
            });

            app.AddSystem(system)
                .InStage(Stage.Update)
                .Build();

            app.Run();

            Assert.Equal(1, counter.Value);
            Assert.Single(spawns);
            Assert.Single(inserts);
            Assert.Equal(counter.Value, inserts[0]);
        }

        [Fact]
        public void EventReaderReceivesEventsOnFollowingFrame()
        {
            using var world = new World();
            var app = new App(world);
            var collected = new List<int>();

            var writerSystem = SystemFunctionAdapters.Create<TinyEcs.Bevy.EventWriter<ScoreEvent>>(writer =>
            {
                writer.Send(new ScoreEvent(7));
            });

            app.AddSystem(writerSystem)
                .InStage(Stage.Update)
                .Label("Writer")
                .Build();

            var readerSystem = SystemFunctionAdapters.Create<TinyEcs.Bevy.EventReader<ScoreEvent>>(reader =>
            {
                foreach (var evt in reader.Read())
                {
                    collected.Add(evt.Value);
                }
            });

            app.AddSystem(readerSystem)
                .InStage(Stage.Update)
                .After("Writer")
                .Build();

            app.Run();
            Assert.Empty(collected);

            app.Run();
            Assert.Equal(new[] { 7 }, collected);
        }

        [Fact]
        public void EventQueuesRemainVisibleAcrossAllStagesForSingleFrame()
        {
            using var world = new World();
            var app = new App(world);

            var preUpdateEvents = new List<int>();
            var postUpdateEvents = new List<int>();

            var writerSystem = SystemFunctionAdapters.Create<Local<MutableCounter>, TinyEcs.Bevy.EventWriter<ScoreEvent>>((counter, writer) =>
            {
                writer.Send(new ScoreEvent(counter.Value.Value));
                counter.Value.Value++;
            });

            app.AddSystem(writerSystem)
                .InStage(Stage.Update)
                .Build();

            var preReader = SystemFunctionAdapters.Create<TinyEcs.Bevy.EventReader<ScoreEvent>>(reader =>
            {
                foreach (var evt in reader.Read())
                {
                    preUpdateEvents.Add(evt.Value);
                }
            });

            app.AddSystem(preReader)
                .InStage(Stage.PreUpdate)
                .Build();

            var postReader = SystemFunctionAdapters.Create<TinyEcs.Bevy.EventReader<ScoreEvent>>(reader =>
            {
                foreach (var evt in reader.Read())
                {
                    postUpdateEvents.Add(evt.Value);
                }
            });

            app.AddSystem(postReader)
                .InStage(Stage.PostUpdate)
                .Build();

            app.Run();
            Assert.Empty(preUpdateEvents);
            Assert.Empty(postUpdateEvents);

            app.Run();
            Assert.Equal(new[] { 0 }, preUpdateEvents);
            Assert.Equal(new[] { 0 }, postUpdateEvents);

            app.Run();
            Assert.Equal(new[] { 0, 1 }, preUpdateEvents);
            Assert.Equal(new[] { 0, 1 }, postUpdateEvents);
        }

        [Fact]
        public void FilterCombinatorSelectsEntitiesMatchingAllPredicates()
        {
            using var world = new World();
            var app = new App(world);

            app.AddSystem(w =>
            {
                var includeBoth = w.Entity();
                includeBoth.Set(new Position { X = 42 });
                includeBoth.Set(new Velocity { Value = 3 });

                var onlyPosition = w.Entity();
                onlyPosition.Set(new Position { X = 11 });

                var onlyVelocity = w.Entity();
                onlyVelocity.Set(new Velocity { Value = 7 });
            })
            .InStage(Stage.Startup)
            .Build();

            var captured = new List<float>();

            app.AddSystem((Query<Data<Position>, Filter<With<Position>, With<Velocity>>> query) =>
            {
                foreach (var row in query)
                {
                    row.Deconstruct(out var pos);
                    captured.Add(pos.Ref.X);
                }
            })
            .InStage(Stage.Update)
            .Build();

            app.Run();

            Assert.Equal(new[] { 42f }, captured);
        }

        [Fact]
        public void SingleSystemParamRetrievesMatchingEntity()
        {
            using var world = new World();
            var app = new App(world);

            app.AddSystem(w =>
            {
                var include = w.Entity();
                include.Set(new Position { X = 7 });
                include.Set(new Velocity { Value = 1 });

                var other = w.Entity();
                other.Set(new Position { X = 99 });
            })
            .InStage(Stage.Startup)
            .Build();

            int captured = 0;

            app.AddSystem((Single<Data<Position>, Filter<With<Position>, With<Velocity>>> single) =>
            {
                Assert.True(single.TryGet(out var row));
                row.Deconstruct(out var pos);
                captured = pos.Ref.X;

                var direct = single.Get();
                direct.Deconstruct(out var posDirect);
                Assert.Equal(captured, posDirect.Ref.X);
            })
            .InStage(Stage.Update)
            .Build();

            app.Run();

            Assert.Equal(7, captured);
        }

        [Fact]
        public void CommandsAddChildCreatesHierarchy()
        {
            using var world = new World();
            var app = new App(world);

            app.AddSystem((Commands commands) =>
            {
                var parent = commands.Spawn().Insert(new ParentTag());
                var child = commands.Spawn().Insert(new ChildTag());
                commands.AddChild(parent, child);
            })
            .InStage(Stage.Startup)
            .Build();

            app.Run();

            var parentQuery = world.QueryBuilder().With<ParentTag>().Build();
            var parentIter = parentQuery.Iter();
            var parentIds = new List<ulong>();
            while (parentIter.Next())
            {
                foreach (var entity in parentIter.Entities())
                {
                    parentIds.Add(entity.ID);
                }
            }

            var childQuery = world.QueryBuilder().With<ChildTag>().Build();
            var childIter = childQuery.Iter();
            var childIds = new List<ulong>();
            while (childIter.Next())
            {
                foreach (var entity in childIter.Entities())
                {
                    childIds.Add(entity.ID);
                }
            }

            Assert.Single(parentIds);
            Assert.Single(childIds);

            var parentId = parentIds[0];
            var childId = childIds[0];

            ref var children = ref world.Get<Children>(parentId);
            var enumerator = children.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(childId, enumerator.Current);
            Assert.False(enumerator.MoveNext());

            ref var parent = ref world.Get<Parent>(childId);
            Assert.Equal(parentId, parent.Id);
        }

        [Fact]
        public void SystemsRunInDeclarationOrderWithNoDependencies()
        {
            using var world = new World();
            var app = new App(world);
            var executed = new List<string>();

            // Add systems without any explicit ordering - they should run in declaration order
            app.AddSystem(w => executed.Add("First"))
                .InStage(Stage.Update)
                .Build();

            app.AddSystem(w => executed.Add("Second"))
                .InStage(Stage.Update)
                .Build();

            app.AddSystem(w => executed.Add("Third"))
                .InStage(Stage.Update)
                .Build();

            app.Run();

            Assert.Equal(new[] { "First", "Second", "Third" }, executed);
        }

        [Fact]
        public void SystemsRespectAfterOrdering()
        {
            using var world = new World();
            var app = new App(world);
            var executed = new List<string>();

            app.AddSystem(w => executed.Add("First"))
                .InStage(Stage.Update)
                .Label("first")
                .Build();

            app.AddSystem(w => executed.Add("Second"))
                .InStage(Stage.Update)
                .Label("second")
                .After("first")
                .Build();

            app.AddSystem(w => executed.Add("Third"))
                .InStage(Stage.Update)
                .After("second")
                .Build();

            app.Run();

            Assert.Equal(new[] { "First", "Second", "Third" }, executed);
        }

        [Fact]
        public void SystemsRespectBeforeOrdering()
        {
            using var world = new World();
            var app = new App(world);
            var executed = new List<string>();

            app.AddSystem(w => executed.Add("Third"))
                .InStage(Stage.Update)
                .Label("third")
                .Build();

            app.AddSystem(w => executed.Add("Second"))
                .InStage(Stage.Update)
                .Label("second")
                .Before("third")
                .Build();

            app.AddSystem(w => executed.Add("First"))
                .InStage(Stage.Update)
                .Before("second")
                .Build();

            app.Run();

            Assert.Equal(new[] { "First", "Second", "Third" }, executed);
        }

        [Fact]
        public void SingleThreadedSystemsRunInOrder()
        {
            using var world = new World();
            var app = new App(world, ThreadingMode.Multi); // Use multi-threaded mode
            var executed = new List<string>();
            var lockObj = new object();

            // All systems are marked single-threaded and should run in declaration order
            app.AddSystem(w =>
                {
                    lock (lockObj) executed.Add("First");
                })
                .InStage(Stage.Update)
                .SingleThreaded()
                .Build();

            app.AddSystem(w =>
                {
                    lock (lockObj) executed.Add("Second");
                })
                .InStage(Stage.Update)
                .SingleThreaded()
                .Build();

            app.AddSystem(w =>
                {
                    lock (lockObj) executed.Add("Third");
                })
                .InStage(Stage.Update)
                .SingleThreaded()
                .Build();

            app.Run();

            Assert.Equal(new[] { "First", "Second", "Third" }, executed);
        }

        [Fact]
        public void ChainedSystemsRunInCorrectOrder()
        {
            using var world = new World();
            var app = new App(world);
            var executed = new List<string>();

            app.AddSystem(w => executed.Add("First"))
                .InStage(Stage.Update)
                .Build();

            app.AddSystem(w => executed.Add("Second"))
                .InStage(Stage.Update)
                .Chain() // Runs after the previous system
                .Build();

            app.AddSystem(w => executed.Add("Third"))
                .InStage(Stage.Update)
                .Chain() // Runs after "Second"
                .Build();

            app.Run();

            Assert.Equal(new[] { "First", "Second", "Third" }, executed);
        }

        [Fact]
        public void ComplexDependencyGraphRespectsAllConstraints()
        {
            using var world = new World();
            var app = new App(world);
            var executed = new List<string>();

            // Create a dependency graph:
            // A -> B -> D
            // A -> C
            // Should execute as: A, then B and C (in some order), then D

            app.AddSystem(w => executed.Add("A"))
                .InStage(Stage.Update)
                .Label("A")
                .Build();

            app.AddSystem(w => executed.Add("B"))
                .InStage(Stage.Update)
                .Label("B")
                .After("A")
                .Build();

            app.AddSystem(w => executed.Add("C"))
                .InStage(Stage.Update)
                .After("A")
                .Build();

            app.AddSystem(w => executed.Add("D"))
                .InStage(Stage.Update)
                .After("B")
                .Build();

            app.Run();

            // A must be first
            Assert.Equal("A", executed[0]);
            // D must be last
            Assert.Equal("D", executed[3]);
            // B and C must be between A and D
            Assert.Contains("B", executed);
            Assert.Contains("C", executed);
            Assert.True(executed.IndexOf("B") > executed.IndexOf("A"));
            Assert.True(executed.IndexOf("C") > executed.IndexOf("A"));
            Assert.True(executed.IndexOf("D") > executed.IndexOf("B"));
        }

        [Fact]
        public void InvalidLabelThrowsException()
        {
            using var world = new World();
            var app = new App(world);

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                app.AddSystem(w => { })
                    .InStage(Stage.Update)
                    .After("nonexistent-label")
                    .Build();
            });

            Assert.Contains("nonexistent-label", exception.Message);
        }

        [Fact]
        public void SystemsWithLabelAndAfterPreserveDeclarationOrder()
        {
            using var world = new World();
            var app = new App(world);
            var executed = new List<string>();

            // First system labeled
            app.AddSystem(w => executed.Add("Window"))
                .InStage(Stage.Startup)
                .Label("create-window")
                .SingleThreaded()
                .Build();

            // Second system depends on first
            app.AddSystem(w => executed.Add("Load"))
                .InStage(Stage.Startup)
                .After("create-window")
                .SingleThreaded()
                .Build();

            app.RunStartup();

            Assert.Equal(new[] { "Window", "Load" }, executed);
        }

        // Bundle tests
        private struct Transform
        {
            public int X;
            public int Y;
        }

        private struct Sprite2
        {
            public int TextureId;
        }

        private struct SpriteBundle : IBundle
        {
            public Transform Transform;
            public Sprite2 Sprite;

            public readonly void Insert(EntityView entity)
            {
                entity.Set(Transform);
                entity.Set(Sprite);
            }

            public readonly void Insert(EntityCommands entity)
            {
                entity.Insert(Transform);
                entity.Insert(Sprite);
            }
        }

        [Fact]
        public void BundleCanBeInsertedIntoEntity()
        {
            using var world = new World();
            var entity = world.Entity();

            var bundle = new SpriteBundle
            {
                Transform = new Transform { X = 10, Y = 20 },
                Sprite = new Sprite2 { TextureId = 42 }
            };

            entity.InsertBundle(bundle);

            Assert.True(entity.Has<Transform>());
            Assert.True(entity.Has<Sprite2>());
            Assert.Equal(10, entity.Get<Transform>().X);
            Assert.Equal(20, entity.Get<Transform>().Y);
            Assert.Equal(42, entity.Get<Sprite2>().TextureId);
        }

        [Fact]
        public void BundleCanBeSpawnedWithCommands()
        {
            using var world = new World();
            var app = new App(world);

            var system = SystemFunctionAdapters.Create<Commands>(commands =>
            {
                var bundle = new SpriteBundle
                {
                    Transform = new Transform { X = 100, Y = 200 },
                    Sprite = new Sprite2 { TextureId = 999 }
                };

                commands.SpawnBundle(bundle);
            });

            app.AddSystem(system)
                .InStage(Stage.Update)
                .Build();

            app.Run();

            // Find the spawned entity by querying for the components
            var query = world.Query<Data<Transform, Sprite2>>();
            var found = false;
            foreach (var (transform, sprite) in query)
            {
                Assert.Equal(100, transform.Ref.X);
                Assert.Equal(200, transform.Ref.Y);
                Assert.Equal(999, sprite.Ref.TextureId);
                found = true;
            }
            Assert.True(found, "Entity with bundle components should exist");
        }

        [Fact]
        public void BundleCanBeInsertedIntoExistingEntityWithCommands()
        {
            using var world = new World();
            var app = new App(world);

            var existingEntity = world.Entity();
            var entityId = existingEntity.ID;

            var system = SystemFunctionAdapters.Create<Commands>(commands =>
            {
                var bundle = new SpriteBundle
                {
                    Transform = new Transform { X = 50, Y = 75 },
                    Sprite = new Sprite2 { TextureId = 123 }
                };

                commands.Entity(entityId).InsertBundle(bundle);
            });

            app.AddSystem(system)
                .InStage(Stage.Update)
                .Build();

            app.Run();

            Assert.True(existingEntity.Has<Transform>());
            Assert.True(existingEntity.Has<Sprite2>());
            Assert.Equal(50, existingEntity.Get<Transform>().X);
            Assert.Equal(75, existingEntity.Get<Transform>().Y);
            Assert.Equal(123, existingEntity.Get<Sprite2>().TextureId);
        }
    }
}
