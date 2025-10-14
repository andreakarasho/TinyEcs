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

		// Custom trigger for testing entity.EmitTrigger()
		private readonly record struct OnClicked(ulong EntityId, int X, int Y, bool ShouldPropagate = false)
			: ITrigger, IEntityTrigger, IPropagatingTrigger
		{
#if NET9_0_OR_GREATER
			public static void Register(World world) { }
#else
			public readonly void Register(World world) { }
#endif

			public OnClicked Propagate(bool propagate = true) => this with { ShouldPropagate = propagate };
		}

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
					.Observe<OnClicked>((trigger) =>
					{
						clickedEvents.Add((trigger.EntityId, trigger.X, trigger.Y));
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
					.EmitTrigger(new OnClicked(entityId, 50, 75));
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
					.Observe<OnClicked>((trigger) =>
					{
						entity1Events.Add(trigger.X);
					});
				entity1Id = e1.Id;

				var e2 = commands.Spawn()
					.Observe<OnClicked>((trigger) =>
					{
						entity2Events.Add(trigger.Y);
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
						.EmitTrigger(new OnClicked(entity1Id, 100, 200));
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
						.EmitTrigger(new OnClicked(entity2Id, 300, 400));
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
					.Observe<OnClicked, ResMut<MutableCounter>>((trigger, cnt) =>
					{
						cnt.Value.Value++;
						events.Add($"Clicked at ({trigger.X},{trigger.Y}) count={cnt.Value.Value}");
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
					.EmitTrigger(new OnClicked(entityId, 10, 20));
				commands.Entity(entityId)
					.EmitTrigger(new OnClicked(entityId, 30, 40));
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
	}
}
