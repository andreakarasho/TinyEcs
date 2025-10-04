using System.Collections.Generic;
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
    }
}
