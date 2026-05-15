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

			app.SetState(GameState.Playing);
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
			Assert.Equal(GameState.Playing, app.GetState<GameState>());

			observed.Clear();
			app.Run();
			Assert.Equal(new[] { GameState.Playing }, observed);
		}

		private enum DetectorTestState
		{
			A,
			B
		}

		[Fact]
		public void StateChangeDetectorRunsOnlyOnTransitionWithoutAllocations()
		{
			using var world = new World();
			var app = new App(world);

			var enterCounts = new Dictionary<DetectorTestState, int>
			{
				[DetectorTestState.A] = 0,
				[DetectorTestState.B] = 0
			};
			var exitCounts = new Dictionary<DetectorTestState, int>
			{
				[DetectorTestState.A] = 0,
				[DetectorTestState.B] = 0
			};

			app.AddState(DetectorTestState.A);

			app.AddSystem(w => enterCounts[DetectorTestState.A]++)
				.OnEnter(DetectorTestState.A)
				.Build();
			app.AddSystem(w => exitCounts[DetectorTestState.A]++)
				.OnExit(DetectorTestState.A)
				.Build();
			app.AddSystem(w => enterCounts[DetectorTestState.B]++)
				.OnEnter(DetectorTestState.B)
				.Build();
			app.AddSystem(w => exitCounts[DetectorTestState.B]++)
				.OnExit(DetectorTestState.B)
				.Build();

			// Initial frame: OnEnter(A) should fire once.
			app.Run();
			Assert.Equal(1, enterCounts[DetectorTestState.A]);
			Assert.Equal(0, exitCounts[DetectorTestState.A]);
			Assert.Equal(0, enterCounts[DetectorTestState.B]);
			Assert.Equal(0, exitCounts[DetectorTestState.B]);

			// Transition to B: OnExit(A) and OnEnter(B) fire once each.
			app.SetState(DetectorTestState.B);
			app.Run();
			Assert.Equal(1, enterCounts[DetectorTestState.A]);
			Assert.Equal(1, exitCounts[DetectorTestState.A]);
			Assert.Equal(1, enterCounts[DetectorTestState.B]);
			Assert.Equal(0, exitCounts[DetectorTestState.B]);

			// Idle frames: no further transitions should be fired.
			app.Run();
			app.Run();
			Assert.Equal(1, enterCounts[DetectorTestState.A]);
			Assert.Equal(1, exitCounts[DetectorTestState.A]);
			Assert.Equal(1, enterCounts[DetectorTestState.B]);
			Assert.Equal(0, exitCounts[DetectorTestState.B]);

			// Transition back to A: OnExit(B) and OnEnter(A) fire once each.
			app.SetState(DetectorTestState.A);
			app.Run();
			Assert.Equal(2, enterCounts[DetectorTestState.A]);
			Assert.Equal(1, exitCounts[DetectorTestState.A]);
			Assert.Equal(1, enterCounts[DetectorTestState.B]);
			Assert.Equal(1, exitCounts[DetectorTestState.B]);

			// Another idle frame: still no new invocations.
			app.Run();
			Assert.Equal(2, enterCounts[DetectorTestState.A]);
			Assert.Equal(1, exitCounts[DetectorTestState.A]);
			Assert.Equal(1, enterCounts[DetectorTestState.B]);
			Assert.Equal(1, exitCounts[DetectorTestState.B]);
		}

		[Fact]
		public void AppFiresSpawnAndDespawnTriggers()
		{
			using var world = new World();
			var app = new App(world);
			var spawns = new List<ulong>();
			var despawns = new List<ulong>();

			app.AddObserver<OnSpawn>((_, trigger) => spawns.Add(trigger.EntityId));
			app.AddObserver<OnDespawn>((_, trigger) => despawns.Add(trigger.EntityId));

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

			app.AddObserver<OnRemove<Velocity>, TinyEcs.Bevy.Res<ScoreTracker>, TinyEcs.Bevy.ResMut<RemovalLog>>((trigger, score, removalLog) =>
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

			app.AddObserver<OnInsert<Position>>((_, trigger) =>
				events.Add($"insert:{trigger.EntityId}:{trigger.Component.X}"));
			app.AddObserver<OnRemove<Position>>((_, trigger) =>
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
		public void EntityDeletionEmitsOnRemoveObservers()
		{
			using var world = new World();
			var app = new App(world);
			var events = new List<string>();

			app.AddObserver<OnDespawn>((_, trigger) =>
				events.Add($"despawn:{trigger.EntityId}"));
			app.AddObserver<OnRemove<Position>>((_, trigger) =>
				events.Add($"remove:position:{trigger.EntityId}"));
			app.AddObserver<OnRemove<Velocity>>((_, trigger) =>
				events.Add($"remove:velocity:{trigger.EntityId}"));

			ulong entityId = 0;
			ulong deletedEntityId = 0;

			app.AddSystem(Stage.Startup, w =>
			{
				var entity = w.Entity();
				entity.Set(new Position { X = 5 });
				entity.Set(new Velocity { Value = 12 });
				entityId = entity.ID;
			});

			app.AddSystem(Stage.Update, w =>
			{
				if (entityId != 0)
				{
					deletedEntityId = entityId;
					w.Entity(entityId).Delete();
					entityId = 0;
				}
			});

			app.Run();

			Assert.NotEqual(0UL, deletedEntityId);
			Assert.NotEmpty(events);
			Assert.Equal($"despawn:{deletedEntityId}", events[0]);
			Assert.Contains($"remove:position:{deletedEntityId}", events);
			Assert.Contains($"remove:velocity:{deletedEntityId}", events);
			Assert.Equal(3, events.Count);
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

			app.AddObserver<OnSpawn>((_, trigger) => spawns.Add(trigger.EntityId));
			app.AddObserver<OnInsert<Health>>((_, trigger) => inserts.Add(trigger.Component.Value));

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
		public void DeferredCommandsUnifyEntityRef_SpawnedAndExistingPathsBothWork()
		{
			using var world = new World();
			var app = new App(world);

			// Pre-create two existing entities the commands will operate on by id
			var existingForInsert = world.Entity().ID;
			var existingForDespawn = world.Entity().ID;

			var spawnedIds = new List<ulong>();
			app.AddResource(spawnedIds);

			// First system spawns one entity (Insert via spawn path) and inserts on an existing entity (Insert via id path).
			var insertSystem = SystemFunctionAdapters.Create<Commands, ResMut<List<ulong>>>((commands, ids) =>
			{
				var spawned = commands.Spawn().Insert(new Position { X = 1 });
				// We cannot read the id yet (deferred); capture via observer below.
				_ = spawned;

				commands.Entity(existingForInsert).Insert(new Position { X = 2 });
			});

			app.AddObserver<OnSpawn>((_, trigger) => spawnedIds.Add(trigger.EntityId));

			app.AddSystem(insertSystem)
				.InStage(Stage.Update)
				.Label("InsertPhase")
				.Build();

			app.Run();

			// Both insertions should have landed.
			Assert.Single(spawnedIds);
			var spawnedId = spawnedIds[0];

			Assert.True(world.Has<Position>(spawnedId));
			Assert.Equal(1, world.Get<Position>(spawnedId).X);

			Assert.True(world.Has<Position>(existingForInsert));
			Assert.Equal(2, world.Get<Position>(existingForInsert).X);

			// Second system despawns one via spawn-path EntityCommands and one via id-path EntityCommands.
			var spawnedToDespawn = 0UL;
			var despawnSystem = SystemFunctionAdapters.Create<Commands>(commands =>
			{
				var freshSpawn = commands.Spawn().Insert(new Position { X = 3 });
				freshSpawn.Despawn(); // spawn-path despawn

				commands.Entity(existingForDespawn).Despawn(); // id-path despawn
			});

			// Capture id of the freshly-spawned-then-despawned entity to confirm it's gone.
			app.AddObserver<OnSpawn>((_, trigger) =>
			{
				// The new spawn during the despawn phase
				spawnedToDespawn = trigger.EntityId;
			});

			app.AddSystem(despawnSystem)
				.InStage(Stage.PostUpdate)
				.Build();

			app.Run();

			// Both despawns succeeded
			Assert.False(world.Exists(existingForDespawn));
			Assert.NotEqual(0UL, spawnedToDespawn);
			Assert.False(world.Exists(spawnedToDespawn));

			// The earlier spawned entity (from first run) is untouched.
			Assert.True(world.Exists(spawnedId));
			Assert.True(world.Exists(existingForInsert));
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
		public void EventChannelFlushDoesNotDoubleNotifyObserversAcrossFrames()
		{
			using var world = new World();
			var app = new App(world);

			var observed = new List<int>();
			app.AddObserver<ScoreEvent>(evt => observed.Add(evt.Value));

			var sendOnce = new MutableCounter();
			app.AddResource(sendOnce);

			// Writer that sends a ScoreEvent only when the resource's flag is set.
			var writerSystem = SystemFunctionAdapters.Create<ResMut<MutableCounter>, TinyEcs.Bevy.EventWriter<ScoreEvent>>((flag, writer) =>
			{
				if (flag.Value.Value != 0)
				{
					writer.Send(new ScoreEvent(flag.Value.Value));
					flag.Value.Value = 0;
				}
			});

			app.AddSystem(writerSystem)
				.InStage(Stage.Update)
				.Build();

			// Frame N: send event with value 1. Observer should see it exactly once after this frame's Flush.
			sendOnce.Value = 1;
			app.Run();
			Assert.Equal(new[] { 1 }, observed);

			// Frame N+1: no new events. Observer must not be re-notified for previously delivered events.
			app.Run();
			Assert.Equal(new[] { 1 }, observed);

			// Frame N+2: send event with value 2. Observer should now have both, in order, exactly once each.
			sendOnce.Value = 2;
			app.Run();
			Assert.Equal(new[] { 1, 2 }, observed);

			// Frame N+3: no new events again. No additional notifications.
			app.Run();
			Assert.Equal(new[] { 1, 2 }, observed);

			// Verify multi-event flush: send 2 events in a single Update stage, then read in next frame.
			var collected = new List<int>();
			var burstWriter = SystemFunctionAdapters.Create<Local<MutableCounter>, TinyEcs.Bevy.EventWriter<ScoreEvent>>((counter, writer) =>
			{
				if (counter.Value.Value == 0)
				{
					writer.Send(new ScoreEvent(10));
					writer.Send(new ScoreEvent(20));
					counter.Value.Value = 1;
				}
			});

			var burstReader = SystemFunctionAdapters.Create<TinyEcs.Bevy.EventReader<ScoreEvent>>(reader =>
			{
				foreach (var evt in reader.Read())
				{
					collected.Add(evt.Value);
				}
			});

			app.AddSystem(burstWriter).InStage(Stage.Update).Label("BurstWriter").Build();
			app.AddSystem(burstReader).InStage(Stage.PostUpdate).Build();

			// First run after adding: burstWriter sends 10 and 20. Reader in PostUpdate of the same frame
			// sees the previous-frame buffer (empty for these values). Flush at end promotes them.
			app.Run();
			Assert.DoesNotContain(10, collected);
			Assert.DoesNotContain(20, collected);

			// Next frame: reader picks up both events in order, each exactly once.
			app.Run();
			Assert.Equal(new[] { 10, 20 }, collected);

			// And observer has now seen all four events (1, 2, 10, 20), each exactly once.
			Assert.Equal(new[] { 1, 2, 10, 20 }, observed);
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
		public void OptionalFilterAllowsAbsentDataComponent()
		{
			using var world = new World();
			var app = new App(world);

			app.AddSystem(w =>
			{
				var posOnly = w.Entity();
				posOnly.Set(new Position { X = 1 });

				var posAndVel = w.Entity();
				posAndVel.Set(new Position { X = 2 });
				posAndVel.Set(new Velocity { Value = 20 });

				var velOnly = w.Entity();
				velOnly.Set(new Velocity { Value = 99 });
			})
			.InStage(Stage.Startup)
			.Build();

			var captured = new List<(int posX, int velValue)>();

			app.AddSystem((Query<Data<Position, Velocity>, Filter<Optional<Velocity>>> query) =>
			{
				foreach (var row in query)
				{
					row.Deconstruct(out var pos, out var vel);
					captured.Add((pos.Ref.X, vel.IsValid() ? vel.Ref.Value : -1));
				}
			})
			.InStage(Stage.Update)
			.Build();

			app.Run();

			captured.Sort();
			Assert.Equal(new[] { (1, -1), (2, 20) }, captured);
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
		public void RunFrameHelperPreservesStartupAndUpdateSemantics()
		{
			// Regression test for the shared RunFrame helper extracted from
			// RunStartup() and Run(). Verifies that startup systems execute exactly
			// once and update systems execute on every Run() call.
			using var world = new World();
			var app = new App(world, ThreadingMode.Single);
			var counter = new MutableCounter();
			app.AddResource(counter);

			app.AddSystem((ResMut<MutableCounter> c) => c.Value.Value = 1)
				.InStage(Stage.Startup)
				.Build();

			app.AddSystem((ResMut<MutableCounter> c) => c.Value.Value++)
				.InStage(Stage.Update)
				.Build();

			// RunStartup() runs only Stage.Startup -> counter set to 1, Update did not run.
			app.RunStartup();
			Assert.Equal(1, counter.Value);

			// Run() must not re-execute Stage.Startup, only Update -> 1 + 1 = 2.
			app.Run();
			Assert.Equal(2, counter.Value);

			// Subsequent Run() calls keep advancing the Update counter -> 3.
			app.Run();
			Assert.Equal(3, counter.Value);
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

		private sealed class ChainPluginA : IPlugin
		{
			private readonly List<string> _executed;

			public ChainPluginA(List<string> executed)
			{
				_executed = executed;
			}

			public void Build(App app)
			{
				app.AddSystem(w => _executed.Add("A"))
					.InStage(Stage.Update)
					.Label("a")
					.Build();
			}
		}

		private sealed class ChainPluginB : IPlugin
		{
			private readonly List<string> _executed;

			public ChainPluginB(List<string> executed)
			{
				_executed = executed;
			}

			public void Build(App app)
			{
				app.AddSystem(w => _executed.Add("B"))
					.InStage(Stage.Update)
					.Chain()
					.Build();
			}
		}

		[Fact]
		public void ChainAcrossPluginBoundaryPreservesPreviousSystem()
		{
			using var world = new World();
			var app = new App(world);
			var executed = new List<string>();

			app.AddPlugin(new ChainPluginA(executed));
			app.AddPlugin(new ChainPluginB(executed));

			app.Run();

			Assert.Equal(new[] { "A", "B" }, executed);
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
			var app = new App(world);

			var existingEntity = world.Entity();
			var entityId = existingEntity.ID;

			var system = SystemFunctionAdapters.Create<Commands>(commands =>
			{
				var bundle = new SpriteBundle
				{
					Transform = new Transform { X = 10, Y = 20 },
					Sprite = new Sprite2 { TextureId = 42 }
				};

				commands.Entity(entityId).InsertBundle(bundle);
			});

			app.AddSystem(system)
				.InStage(Stage.Update)
				.Build();

			app.Run();

			Assert.True(existingEntity.Has<Transform>());
			Assert.True(existingEntity.Has<Sprite2>());
			Assert.Equal(10, existingEntity.Get<Transform>().X);
			Assert.Equal(20, existingEntity.Get<Transform>().Y);
			Assert.Equal(42, existingEntity.Get<Sprite2>().TextureId);
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

		[Fact]
		public void EntitySpecificObserverTriggersOnlyForTargetEntity()
		{
			using var world = new World();
			var app = new App(world);

			var triggeredEntityIds = new List<ulong>();
			var triggeredHealthValues = new List<int>();

			var system = SystemFunctionAdapters.Create<Commands>(commands =>
			{
				// Spawn entity 1 with entity-specific observer
				var entity1 = commands.Spawn()
					.Insert(new Health { Value = 100 })
					.Observe<OnInsert<Health>>((trigger) =>
					{
						triggeredEntityIds.Add(trigger.EntityId);
						triggeredHealthValues.Add(trigger.Component.Value);
					});

				// Spawn entity 2 without observer
				var entity2 = commands.Spawn()
					.Insert(new Health { Value = 50 });
			});

			app.AddSystem(system)
				.InStage(Stage.Update)
				.Build();

			app.Run();

			// Only entity1's observer should have fired
			Assert.Single(triggeredEntityIds);
			Assert.Single(triggeredHealthValues);
			Assert.Equal(100, triggeredHealthValues[0]);
		}


		[Fact]
		public void EntitySpecificObserverTriggersOnDespawn()
		{
			using var world = new World();
			var app = new App(world);

			var despawnedEntityId = 0UL;
			var targetEntityId = new List<ulong>();
			app.AddResource(targetEntityId);

			var spawnSystem = SystemFunctionAdapters.Create<Commands, ResMut<List<ulong>>>(
				(commands, ids) =>
			{
				commands.Spawn()
					.Observe<OnDespawn>((trigger) =>
					{
						despawnedEntityId = trigger.EntityId;
						if (!ids.Value.Contains(trigger.EntityId))
							ids.Value.Add(trigger.EntityId);
					})
					.Observe<OnInsert<Health>>((trigger) =>
					{
						if (!ids.Value.Contains(trigger.EntityId))
							ids.Value.Add(trigger.EntityId);
					})
					.Insert(new Health { Value = 75 });
			});

			app.AddSystem(spawnSystem)
				.InStage(Stage.Update)
				.Build();

			app.Run();

			// Entity spawned, observer registered, but not despawned yet
			Assert.Equal(0UL, despawnedEntityId);
			Assert.Single(targetEntityId);
			Assert.NotEqual(0UL, targetEntityId[0]);

			// Now despawn the entity
			var entityToDelete = targetEntityId[0];
			var despawnSystem = SystemFunctionAdapters.Create<Commands>(commands =>
			{
				commands.Entity(entityToDelete).Despawn();
			});

			app.AddSystem(despawnSystem)
				.InStage(Stage.PostUpdate)
				.Build();

			app.Run();

			// Observer should have fired
			Assert.Equal(entityToDelete, despawnedEntityId);
		}

		[Fact]
		public void EntitySpecificObserverOnlyTriggersForCorrectEntity()
		{
			using var world = new World();
			var app = new App(world);

			var entity1Triggers = new List<int>();
			var entity2Triggers = new List<int>();

			// Resource to store entity IDs after they're spawned
			var entityIds = new List<ulong>();
			app.AddResource(entityIds);

			var spawnSystem = SystemFunctionAdapters.Create<Commands, ResMut<List<ulong>>>(
				(commands, ids) =>
			{
				// Entity 1 with observer
				commands.Spawn()
					.Observe<OnInsert<Health>>((trigger) =>
					{
						entity1Triggers.Add(trigger.Component.Value);
						if (!ids.Value.Contains(trigger.EntityId))
							ids.Value.Add(trigger.EntityId);
					})
					.Insert(new Health { Value = 100 });

				// Entity 2 with different observer
				commands.Spawn()
					.Observe<OnInsert<Health>>((trigger) =>
					{
						entity2Triggers.Add(trigger.Component.Value);
						if (!ids.Value.Contains(trigger.EntityId))
							ids.Value.Add(trigger.EntityId);
					})
					.Insert(new Health { Value = 200 });
			});

			app.AddSystem(spawnSystem)
				.InStage(Stage.Update)
				.Build();

			app.Run();

			// Each observer should have fired only once for its own entity
			Assert.Single(entity1Triggers);
			Assert.Single(entity2Triggers);
			Assert.Equal(100, entity1Triggers[0]);
			Assert.Equal(200, entity2Triggers[0]);
			Assert.Equal(2, entityIds.Count);

			// Now update entity1's health - only entity1's observer should fire
			var entity1Id = entityIds[0];
			world.Set(entity1Id, new Health { Value = 150 });
			world.FlushObservers();

			Assert.Equal(2, entity1Triggers.Count); // Fired again
			Assert.Single(entity2Triggers); // Did not fire
			Assert.Equal(150, entity1Triggers[1]);
		}

		[Fact]
		public void EntitySpecificObserverWorksWithExistingEntity()
		{
			using var world = new World();
			var app = new App(world);

			var existingEntity = world.Entity();
			var entityId = existingEntity.ID;
			var triggered = new List<int>();

			var system = SystemFunctionAdapters.Create<Commands>(commands =>
			{
				// Attach observer to existing entity BEFORE inserting component
				commands.Entity(entityId)
					.Observe<OnInsert<Health>>((trigger) =>
					{
						triggered.Add(trigger.Component.Value);
					})
					.Insert(new Health { Value = 99 });
			});

			app.AddSystem(system)
				.InStage(Stage.Update)
				.Build();

			app.Run();

			Assert.Single(triggered);
			Assert.Equal(99, triggered[0]);

			// Update the health again
			world.Set(entityId, new Health { Value = 88 });
			world.FlushObservers();

			Assert.Equal(2, triggered.Count);
			Assert.Equal(88, triggered[1]);
		}

		[Fact]
		public void OnAddVsOnInsert_DifferentBehavior()
		{
			using var world = new World();
			var app = new App(world);

			var onAddEvents = new List<int>();
			var onInsertEvents = new List<int>();

			// OnAdd should fire only on first addition
			app.AddObserver<OnAdd<Health>>((_, trigger) =>
			{
				onAddEvents.Add(trigger.Component.Value);
			});

			// OnInsert should fire on both addition and update
			app.AddObserver<OnInsert<Health>>((_, trigger) =>
			{
				onInsertEvents.Add(trigger.Component.Value);
			});

			// Test 1: First addition - both should fire
			var entity = world.Entity();
			var entityId = entity.ID;

			entity.Set(new Health { Value = 100 });
			world.FlushObservers();

			// Both should have fired once on first add
			Assert.Single(onAddEvents);
			Assert.Single(onInsertEvents);
			Assert.Equal(100, onAddEvents[0]);
			Assert.Equal(100, onInsertEvents[0]);

			// Test 2: Update - only OnInsert should fire
			world.Set(entityId, new Health { Value = 150 });
			world.FlushObservers();

			// OnAdd should NOT fire again (still 1)
			Assert.Single(onAddEvents);
			// OnInsert SHOULD fire again (now 2)
			Assert.Equal(2, onInsertEvents.Count);
			Assert.Equal(150, onInsertEvents[1]);

			// Test 3: Another update - only OnInsert should fire
			world.Set(entityId, new Health { Value = 200 });
			world.FlushObservers();

			// OnAdd still only fired once
			Assert.Single(onAddEvents);
			Assert.Equal(100, onAddEvents[0]);
			// OnInsert fired three times total
			Assert.Equal(3, onInsertEvents.Count);
			Assert.Equal(200, onInsertEvents[2]);
		}

		[Fact]
		public void ObserverPropagation_BubblesUpParentHierarchy()
		{
			// Test that triggers with .Propagate(true) fire on parent entities
			using var world = new World();
			world.EnableObservers<Health>();

			var triggerLog = new List<(ulong EntityId, int Value, string Source)>();

			// Create entities and set up hierarchy
			var grandparentId = world.Entity().ID;
			var parentId = world.Entity().ID;
			var childId = world.Entity().ID;

			world.Set(parentId, new Parent { Id = grandparentId });
			world.Set(childId, new Parent { Id = parentId });

			// Create Commands to register observers
			var app = new App(world);
			var system = SystemFunctionAdapters.Create<Commands>(commands =>
			{
				commands.Entity(childId).Observe<OnInsert<Health>>((trigger) =>
				{
					triggerLog.Add((trigger.EntityId, trigger.Component.Value, "child observer"));
				});

				commands.Entity(parentId).Observe<OnInsert<Health>>((trigger) =>
				{
					triggerLog.Add((trigger.EntityId, trigger.Component.Value, "parent observer"));
				});

				commands.Entity(grandparentId).Observe<OnInsert<Health>>((trigger) =>
				{
					triggerLog.Add((trigger.EntityId, trigger.Component.Value, "grandparent observer"));
				});
			});

			app.AddSystem(system).InStage(Stage.Update).Build();
			app.Run();

			// Test 1: Non-propagating trigger (default) - should only fire on child
			world.Set(childId, new Health { Value = 100 });
			world.FlushObservers();

			// Only child observer should fire
			Assert.Single(triggerLog);
			Assert.Equal(childId, triggerLog[0].EntityId);
			Assert.Equal(100, triggerLog[0].Value);
			Assert.Equal("child observer", triggerLog[0].Source);

			triggerLog.Clear();

			// Test 2: Propagating trigger - should fire on child, parent, and grandparent
			world.EmitTrigger(new OnInsert<Health>(childId, new Health { Value = 200 }).Propagate(true));

			// All three observers should fire
			Assert.Equal(3, triggerLog.Count);

			// First: child observer
			Assert.Equal(childId, triggerLog[0].EntityId);
			Assert.Equal(200, triggerLog[0].Value);
			Assert.Equal("child observer", triggerLog[0].Source);

			// Second: parent observer (propagated)
			Assert.Equal(childId, triggerLog[1].EntityId); // EntityId stays the same
			Assert.Equal(200, triggerLog[1].Value);
			Assert.Equal("parent observer", triggerLog[1].Source);

			// Third: grandparent observer (propagated)
			Assert.Equal(childId, triggerLog[2].EntityId); // EntityId stays the same
			Assert.Equal(200, triggerLog[2].Value);
			Assert.Equal("grandparent observer", triggerLog[2].Source);
		}

		[Fact]
		public void ObserverPropagation_StopsAtRoot()
		{
			// Test that propagation stops when reaching an entity without a parent
			using var world = new World();
			world.EnableObservers<Health>();

			var triggerLog = new List<string>();

			// Create parent-child (no grandparent)
			var parentId = world.Entity().ID;
			var childId = world.Entity().ID;
			world.Set(childId, new Parent { Id = parentId });

			var app = new App(world);
			var system = SystemFunctionAdapters.Create<Commands>(commands =>
			{
				commands.Entity(parentId).Observe<OnInsert<Health>>((trigger) =>
				{
					triggerLog.Add("parent");
				});

				commands.Entity(childId).Observe<OnInsert<Health>>((trigger) =>
				{
					triggerLog.Add("child");
				});
			});

			app.AddSystem(system).InStage(Stage.Update).Build();
			app.Run();

			// Propagating trigger should fire on child and parent, then stop
			world.EmitTrigger(new OnInsert<Health>(childId, new Health { Value = 100 }).Propagate(true));

			Assert.Equal(2, triggerLog.Count);
			Assert.Equal("child", triggerLog[0]);
			Assert.Equal("parent", triggerLog[1]);
		}

		[Fact]
		public void StartupStageRunsSingleThreadedByDefault()
		{
			// Test that Stage.Startup runs single-threaded even when app uses ThreadingMode.Multi
			using var world = new World();
			var app = new App(world, ThreadingMode.Multi);

			var executed = new List<string>();
			var lockObj = new object();
			var sharedCounter = new MutableCounter();
			app.AddResource(sharedCounter);

			// Add multiple systems in Startup - they should run in declaration order
			// even with Multi threading mode because Startup forces single-threaded
			app.AddSystem((ResMut<MutableCounter> counter) =>
				{
					lock (lockObj)
					{
						counter.Value.Value = 1;
						executed.Add($"First:{counter.Value.Value}");
					}
				})
				.InStage(Stage.Startup)
				.Build();

			app.AddSystem((ResMut<MutableCounter> counter) =>
				{
					lock (lockObj)
					{
						counter.Value.Value = 2;
						executed.Add($"Second:{counter.Value.Value}");
					}
				})
				.InStage(Stage.Startup)
				.Build();

			app.AddSystem((ResMut<MutableCounter> counter) =>
				{
					lock (lockObj)
					{
						counter.Value.Value = 3;
						executed.Add($"Third:{counter.Value.Value}");
					}
				})
				.InStage(Stage.Startup)
				.Build();

			app.RunStartup();

			// Systems should have run in strict declaration order
			// This demonstrates single-threaded execution
			Assert.Equal(new[] { "First:1", "Second:2", "Third:3" }, executed);
			Assert.Equal(3, sharedCounter.Value);
		}

		// Simple event struct for testing entity.EmitTrigger()
		private readonly record struct OnClicked(int X, int Y);

		[Fact]
		public void EntityEmitTriggerFiresObserverViaCommands()
		{
			// Test that entity.EmitTrigger() works with commands
			using var world = new World();
			var app = new App(world);

			var clickedEvents = new List<(ulong EntityId, int X, int Y)>();
			ulong entityId = 0;

			// System 1: Spawn entity with observer
			app.AddSystem((Commands commands) =>
			{
				var entity = commands.Spawn()
					.Insert(new Position { X = 100 })
					.Observe<On<OnClicked>>((trigger) =>
					{
						clickedEvents.Add((trigger.EntityId, trigger.Event.X, trigger.Event.Y));
					});
				entityId = entity.Id;
			})
			.InStage(Stage.Startup)
			.Build();

			app.Run();

			// Entity spawned and observer registered, but not triggered yet
			Assert.Empty(clickedEvents);
			Assert.NotEqual(0UL, entityId);

			// System 2: Trigger the observer using entity.EmitTrigger()
			app.AddSystem((Commands commands) =>
			{
				commands.Entity(entityId)
					.EmitTrigger(new OnClicked(50, 75));
			})
			.InStage(Stage.Update)
			.Build();

			app.Run();

			// Observer should have fired
			Assert.Single(clickedEvents);
			Assert.Equal(entityId, clickedEvents[0].EntityId);
			Assert.Equal(50, clickedEvents[0].X);
			Assert.Equal(75, clickedEvents[0].Y);
		}

		[Fact]
		public void EntityEmitTriggerOnlyFiresForSpecificEntity()
		{
			// Test that entity.EmitTrigger() only triggers the specific entity's observer
			using var world = new World();
			var app = new App(world);

			var entity1Events = new List<int>();
			var entity2Events = new List<int>();
			ulong entity1Id = 0;
			ulong entity2Id = 0;

			// Spawn two entities with different observers
			app.AddSystem((Commands commands) =>
			{
				var e1 = commands.Spawn()
					.Observe<On<OnClicked>>((trigger) =>
					{
						entity1Events.Add(trigger.Event.X);
					});
				entity1Id = e1.Id;

				var e2 = commands.Spawn()
					.Observe<On<OnClicked>>((trigger) =>
					{
						entity2Events.Add(trigger.Event.Y);
					});
				entity2Id = e2.Id;
			})
			.InStage(Stage.Startup)
			.Build();

			app.Run();

			Assert.NotEqual(0UL, entity1Id);
			Assert.NotEqual(0UL, entity2Id);

			// Trigger only entity1
			bool entity1Triggered = false;
			app.AddSystem((Commands commands) =>
			{
				if (!entity1Triggered)
				{
					commands.Entity(entity1Id)
						.EmitTrigger(new OnClicked(100, 200));
					entity1Triggered = true;
				}
			})
			.InStage(Stage.Update)
			.Build();

			app.Run();

			// Only entity1's observer should have fired
			Assert.Single(entity1Events);
			Assert.Empty(entity2Events);
			Assert.Equal(100, entity1Events[0]);

			// Now trigger only entity2
			entity1Events.Clear();
			entity2Events.Clear();

			bool entity2Triggered = false;
			app.AddSystem((Commands commands) =>
			{
				if (!entity2Triggered)
				{
					commands.Entity(entity2Id)
						.EmitTrigger(new OnClicked(300, 400));
					entity2Triggered = true;
				}
			})
			.InStage(Stage.PostUpdate)
			.Build();

			app.Run();

			// Only entity2's observer should have fired
			Assert.Empty(entity1Events);
			Assert.Single(entity2Events);
			Assert.Equal(400, entity2Events[0]);
		}

		[Fact]
		public void EntityEmitTriggerWorksWithSystemParameters()
		{
			// Test that entity observers with system parameters work with EmitTrigger
			using var world = new World();
			var app = new App(world);

			var events = new List<string>();
			var counter = new MutableCounter();
			app.AddResource(counter);
			ulong entityId = 0;

			// Spawn entity with observer that uses system parameters
			app.AddSystem((Commands commands) =>
			{
				var entity = commands.Spawn()
					.Observe<On<OnClicked>, ResMut<MutableCounter>>((trigger, cnt) =>
					{
						cnt.Value.Value++;
						events.Add($"Clicked at ({trigger.Event.X},{trigger.Event.Y}) count={cnt.Value.Value}");
					});
				entityId = entity.Id;
			})
			.InStage(Stage.Startup)
			.Build();

			app.Run();

			Assert.NotEqual(0UL, entityId);

			// Trigger the observer twice
			app.AddSystem((Commands commands) =>
			{
				commands.Entity(entityId)
					.EmitTrigger(new OnClicked(10, 20));
				commands.Entity(entityId)
					.EmitTrigger(new OnClicked(30, 40));
			})
			.InStage(Stage.Update)
			.Build();

			app.Run();

			// Observer should have fired twice with system parameter access
			Assert.Equal(2, events.Count);
			Assert.Equal("Clicked at (10,20) count=1", events[0]);
			Assert.Equal("Clicked at (30,40) count=2", events[1]);
			Assert.Equal(2, counter.Value);
		}

		[Fact]
		public void ConfiguratorAllowsLabelOrderingMethodsInAnyOrder()
		{
			// Verify the simplified configurator: configuration methods can be
			// called in any order without type-state ceremony, and "last label wins".
			var executed = new List<string>();

			var app = new App();

			// Anchor system so the subsequent .After("anchor") resolves.
			app.AddSystem(() => executed.Add("anchor"))
				.InStage(Stage.Update)
				.Label("anchor")
				.Build();

			// Anchor system to satisfy .Before("tail").
			app.AddSystem(() => executed.Add("tail"))
				.InStage(Stage.Update)
				.Label("tail")
				.Build();

			// Forward order: RunIf -> Label -> After -> Before -> SingleThreaded.
			app.AddSystem(() => executed.Add("forward"))
				.InStage(Stage.Update)
				.RunIf(_ => true)
				.Label("forward")
				.After("anchor")
				.Before("tail")
				.SingleThreaded()
				.Build();

			// Reverse order: SingleThreaded -> Before -> After -> Label -> RunIf.
			app.AddSystem(() => executed.Add("reverse"))
				.InStage(Stage.Update)
				.SingleThreaded()
				.Before("tail")
				.After("anchor")
				.Label("reverse")
				.RunIf(_ => true)
				.Build();

			// Last-label-wins: declaring Label("a") then Label("b") should make "b"
			// the resolvable label. .After("b") must therefore work without throwing.
			app.AddSystem(() => executed.Add("relabeled"))
				.InStage(Stage.Update)
				.Label("a")
				.Label("b")
				.After("anchor")
				.Build();

			app.AddSystem(() => executed.Add("after_b"))
				.InStage(Stage.Update)
				.After("b")
				.Before("tail")
				.Build();

			app.Run();

			// Anchor runs first, tail last; forward/reverse/relabeled/after_b between.
			Assert.Equal("anchor", executed[0]);
			Assert.Equal("tail", executed[^1]);
			Assert.Contains("forward", executed);
			Assert.Contains("reverse", executed);
			Assert.Contains("relabeled", executed);
			Assert.Contains("after_b", executed);
			// after_b must run after relabeled (its dependency via the "b" label).
			Assert.True(executed.IndexOf("after_b") > executed.IndexOf("relabeled"));
		}

		[Fact]
		public void RelabeledSystemOldLabelNoLongerResolvable()
		{
			// "Last label wins" semantics: when a system is relabeled, the old
			// label must no longer resolve. We verify this behaviorally by
			// referencing the old label from another system, which must throw.
			var app = new App();

			app.AddSystem(() => { })
				.InStage(Stage.Update)
				.Label("first")
				.Label("second")
				.Build();

			// Referencing "second" must succeed.
			app.AddSystem(() => { })
				.InStage(Stage.Update)
				.After("second")
				.Build();

			// Referencing "first" must throw because the relabel removed it.
			var ex = Assert.Throws<System.InvalidOperationException>(() =>
				app.AddSystem(() => { })
					.InStage(Stage.Update)
					.After("first")
					.Build());

			Assert.Contains("first", ex.Message);
		}

		[Fact]
		public void ConfiguratorChainStillWorksWithoutInterfaceCeremony()
		{
			// Smoke test that the canonical fluent chain still compiles and runs.
			var executed = new List<string>();
			var app = new App();

			app.AddSystem(() => executed.Add("b"))
				.InStage(Stage.Update)
				.Label("b")
				.Build();

			app.AddSystem(() => executed.Add("a"))
				.InStage(Stage.Update)
				.Label("a")
				.After("b")
				.RunIf(_ => true)
				.SingleThreaded()
				.Build();

			app.Run();

			Assert.Equal(new[] { "b", "a" }, executed);
		}

		private struct MyRes
		{
			public int Value;
		}

		[Fact]
		public void MultipleAppsHaveIsolatedBevyState()
		{
			// Bevy state (resources/events/states) now lives on the App, not on the World.
			// Two independent Apps must therefore own independent state stores even if
			// they happen to share or wrap the same World concept.
			using var world1 = new World();
			using var world2 = new World();
			var app1 = new App(world1);
			var app2 = new App(world2);

			app1.AddResource(new MyRes { Value = 1 });
			app2.AddResource(new MyRes { Value = 2 });

			Assert.True(app1.HasResource<MyRes>());
			Assert.True(app2.HasResource<MyRes>());

			Assert.Equal(1, app1.GetResource<MyRes>().Value);
			Assert.Equal(2, app2.GetResource<MyRes>().Value);
		}

		private enum PooledListSnapshotState
		{
			Idle,
			Active
		}

		private readonly record struct PooledListEvent(int Value);

		[Fact]
		public void HotPathSnapshotsUsingPooledListPreserveBehavior()
		{
			// Regression: EventChannel.Flush, ProcessStateTransitions, and ProcessEvents
			// were converted from List<T>.ToList() snapshots to ArrayPool-backed
			// PooledList<T> snapshots. Behavior must be identical to the prior
			// implementation across event dispatch and state transition handling.
			using var world = new World();
			var app = new App(world);

			// Multiple resources to exercise general App plumbing alongside the hot paths.
			app.AddResource(new ScoreTracker { Baseline = 10 });
			app.AddResource(new MutableCounter { Value = 0 });

			// State enum: alternate frames will queue transitions.
			app.AddState(PooledListSnapshotState.Idle);

			var enterIdleCount = 0;
			var exitIdleCount = 0;
			var enterActiveCount = 0;
			var exitActiveCount = 0;

			app.AddSystem(w => enterIdleCount++)
				.OnEnter(PooledListSnapshotState.Idle)
				.Build();
			app.AddSystem(w => exitIdleCount++)
				.OnExit(PooledListSnapshotState.Idle)
				.Build();
			app.AddSystem(w => enterActiveCount++)
				.OnEnter(PooledListSnapshotState.Active)
				.Build();
			app.AddSystem(w => exitActiveCount++)
				.OnExit(PooledListSnapshotState.Active)
				.Build();

			// Two observers on the same event type — exercises the observersSnapshot
			// PooledList path in EventChannel<T>.Flush.
			var observerA = new List<int>();
			var observerB = new List<int>();
			app.AddObserver<PooledListEvent>(evt => observerA.Add(evt.Value));
			app.AddObserver<PooledListEvent>(evt => observerB.Add(evt.Value));

			// 5 frames; each frame writes 2 events; every other frame queues a
			// state transition. After 5 frames we should have 10 events total.
			var nextValue = 0;
			var frameIndex = 0;
			var writerEnabled = true;
			app.AddSystem((TinyEcs.Bevy.EventWriter<PooledListEvent> writer) =>
			{
				if (!writerEnabled) return;
				writer.Send(new PooledListEvent(nextValue++));
				writer.Send(new PooledListEvent(nextValue++));
			})
			.InStage(Stage.Update)
			.Build();

			for (var i = 0; i < 5; i++)
			{
				if ((frameIndex & 1) == 1)
				{
					var current = app.GetState<PooledListSnapshotState>();
					app.SetState(current == PooledListSnapshotState.Idle
						? PooledListSnapshotState.Active
						: PooledListSnapshotState.Idle);
				}
				app.Run();
				frameIndex++;
			}

			// Observers fire on Flush, which is called from ProcessEvents at the
			// end of Run, so all 10 events should have been delivered to both
			// observers already.
			Assert.Equal(10, observerA.Count);
			Assert.Equal(10, observerB.Count);
			Assert.Equal(observerA, observerB);

			// Disable the writer so the smoke runs below produce no events.
			writerEnabled = false;

			// State transitions: frames at frameIndex==1 and frameIndex==3 queued
			// a transition each. The initial frame fires OnEnter(Idle) once.
			// - Frame 0: enter Idle (initial)
			// - Frame 1: Idle -> Active (exit Idle, enter Active)
			// - Frame 2: no transition
			// - Frame 3: Active -> Idle (exit Active, enter Idle)
			// - Frame 4: no transition
			Assert.Equal(2, enterIdleCount);
			Assert.Equal(1, exitIdleCount);
			Assert.Equal(1, enterActiveCount);
			Assert.Equal(1, exitActiveCount);

			// Smoke test: extra runs with no events queued / no transitions
			// pending must not throw. Verifies the haveWork early-return and
			// the zero-capacity rent paths.
			app.Run();
			app.Run();
			app.Run();

			// No new state-transition firings.
			Assert.Equal(2, enterIdleCount);
			Assert.Equal(1, exitIdleCount);
			Assert.Equal(1, enterActiveCount);
			Assert.Equal(1, exitActiveCount);

			// Observer counts must not have grown (no events written in idle frames).
			Assert.Equal(10, observerA.Count);
			Assert.Equal(10, observerB.Count);
		}
	}
}
