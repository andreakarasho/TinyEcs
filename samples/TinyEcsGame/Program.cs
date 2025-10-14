using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;

using var world = new World();
var app = new App(world, ThreadingMode.Single);

app.AddPlugin(new RaylibPlugin
{
	Title = "TinyEcs using raylib",
	WindowSize = new WindowSize { Value = new Vector2(800, 600) },
	VSync = true
});

app.AddPlugin(new GameRootPlugin
{
	EntitiesToSpawn = 100_000,
	Velocity = 250
});

app.RunStartup();

while (!Raylib.WindowShouldClose())
{
	app.Update();
}

Raylib.CloseWindow();

sealed class RaylibPlugin : IPlugin
{
	public string Title { get; set; } = string.Empty;
	public WindowSize WindowSize { get; set; }
	public bool VSync { get; set; }

	public void Build(App app)
	{
		app.AddResource(WindowSize);
		app.AddResource(new AssetsManager());
		app.AddResource(new TimeResource());

		app.AddSystem((Res<WindowSize> window) =>
		{
			var flags = VSync ? ConfigFlags.VSyncHint : 0;
			Raylib.SetConfigFlags(flags);
			Raylib.InitWindow((int)window.Value.Value.X, (int)window.Value.Value.Y, Title);
		})
		.InStage(Stage.Startup)
		.Label("raylib:create-window")
		.SingleThreaded()
		.Build();

		app.AddSystem((ResMut<TimeResource> time) =>
		{
			var t = time.Value;
			t.Frame = Raylib.GetFrameTime();
			t.Total += t.Frame;
		})
		.InStage(Stage.Update)
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();
	}
}

sealed class GameRootPlugin : IPlugin
{
	public int EntitiesToSpawn { get; set; }
	public int Velocity { get; set; }

	public void Build(App app)
	{
		app.AddPlugin(new GameplayPlugin
		{
			EntitiesToSpawn = EntitiesToSpawn,
			Velocity = Velocity
		});

		app.AddPlugin(new RenderingPlugin());
	}
}

sealed class GameplayPlugin : IPlugin
{
	public int EntitiesToSpawn { get; set; }
	public int Velocity { get; set; }

	public void Build(App app)
	{
		app.AddSystem((Commands commands, Res<WindowSize> size, Res<AssetsManager> assets) =>
		{
			var texturePath = Path.Combine(AppContext.BaseDirectory, "Content", "pepe.png");
			var texture = Raylib.LoadTexture(texturePath);
			assets.Value!.Register(texture);

			var rnd = Random.Shared;
			for (var i = 0; i < EntitiesToSpawn; ++i)
			{
				commands.SpawnBundle(new SpriteBundle
				{
					Position = new Position
					{
						Value = new Vector2(
							rnd.Next(0, (int)size.Value.Value.X),
							rnd.Next(0, (int)size.Value.Value.Y))
					},
					Velocity = new Velocity
					{
						Value = new Vector2(
							rnd.Next(-Velocity, Velocity),
							rnd.Next(-Velocity, Velocity))
					},
					Sprite = new Sprite
					{
						Color = new Color(rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256), 255),
						Scale = rnd.NextSingle(),
						TextureId = texture.Id
					},
					Rotation = new Rotation
					{
						Value = 0f,
						Acceleration = rnd.Next(45, 180) * (rnd.Next() % 2 == 0 ? -1 : 1)
					}
				});
			}
		})
		.InStage(Stage.Startup)
		.After("raylib:create-window")
		.SingleThreaded()
		.Build();

		app.AddSystem((Res<TimeResource> time, Query<Data<Position, Velocity, Rotation>> query) =>
		{
			foreach (var (pos, vel, rot) in query)
			{
				pos.Ref.Value += vel.Ref.Value * time.Value.Frame;
				rot.Ref.Value = (rot.Ref.Value + rot.Ref.Acceleration * time.Value.Frame) % 360f;
			}
		})
		.InStage(Stage.Update)
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		app.AddSystem((Query<Data<Position, Velocity>> query, Res<WindowSize> windowSize) =>
		{
			var bounds = windowSize.Value.Value;
			foreach (var (pos, vel) in query)
			{
				ref var position = ref pos.Ref.Value;
				ref var velocity = ref vel.Ref.Value;

				if (position.X < 0f)
				{
					position.X = 0f;
					velocity.X *= -1f;
				}
				else if (position.X > bounds.X)
				{
					position.X = bounds.X;
					velocity.X *= -1f;
				}

				if (position.Y < 0f)
				{
					position.Y = 0f;
					velocity.Y *= -1f;
				}
				else if (position.Y > bounds.Y)
				{
					position.Y = bounds.Y;
					velocity.Y *= -1f;
				}
			}
		})
		.InStage(Stage.Update)
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();
	}
}

sealed class RenderingPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddSystem((World _) =>
		{
			Raylib.BeginDrawing();
			Raylib.ClearBackground(Color.Black);
		})
		.InStage(Stage.Last)
		.Label("render:begin")
		.SingleThreaded()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		app.AddSystem((Query<Data<Sprite, Position, Rotation>> query, Res<AssetsManager> assets) =>
		{
			var currentTextureId = 0u;
			Texture2D? texture = null;

			foreach (var (sprite, pos, rot) in query)
			{
				if (sprite.Ref.TextureId != currentTextureId)
				{
					currentTextureId = sprite.Ref.TextureId;
					texture = assets.Value!.Get(currentTextureId);
				}

				if (texture.HasValue)
				{
					Raylib.DrawTextureEx(texture.Value, pos.Ref.Value, rot.Ref.Value, sprite.Ref.Scale, sprite.Ref.Color);
				}
			}
		})
		.InStage(Stage.Last)
		.Label("render:draw")
		.After("render:begin")
		.SingleThreaded()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		app.AddSystem((Query<Data<Position>> query, Res<TimeResource> time, Local<DebugOverlay> overlay) =>
		{
			var entityCount = 0;
			foreach (var _ in query)
			{
				entityCount++;
			}

			var data = overlay.Value;
			data.Text = $"""
                [Debug]
                FPS: {Raylib.GetFPS()}
                Entities: {entityCount}
                DeltaTime: {time.Value.Frame:F4}
                """.Replace("\r", "\n");

			Raylib.DrawText(data.Text, 15, 15, 24, Color.White);
		})
		.InStage(Stage.Last)
		.Label("render:debug")
		.After("render:draw")
		.SingleThreaded()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		app.AddSystem((World _) => Raylib.EndDrawing())
		.InStage(Stage.Last)
		.After("render:debug")
		.SingleThreaded()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();
	}
}

sealed class AssetsManager
{
	private readonly Dictionary<uint, Texture2D> _textures = new();

	public void Register(Texture2D texture) => _textures[texture.Id] = texture;

	public Texture2D? Get(uint id) => _textures.TryGetValue(id, out var texture) ? texture : null;
}

sealed class TimeResource
{
	public float Frame;
	public float Total;
}

sealed class DebugOverlay
{
	public string Text { get; set; } = string.Empty;
}

struct WindowSize
{
	public Vector2 Value;
}

struct Position
{
	public Vector2 Value;
}

struct Velocity
{
	public Vector2 Value;
}

struct Sprite
{
	public Color Color;
	public float Scale;
	public uint TextureId;
}

struct Rotation
{
	public float Value;
	public float Acceleration;
}

/// <summary>
/// Bundle for spawning a sprite entity with position, velocity, and rotation
/// </summary>
struct SpriteBundle : IBundle
{
	public Position Position;
	public Velocity Velocity;
	public Sprite Sprite;
	public Rotation Rotation;

	public readonly void Insert(EntityView entity)
	{
		entity.Set(Position);
		entity.Set(Velocity);
		entity.Set(Sprite);
		entity.Set(Rotation);
	}

	public readonly void Insert(EntityCommands entity)
	{
		entity.Insert(Position);
		entity.Insert(Velocity);
		entity.Insert(Sprite);
		entity.Insert(Rotation);
	}
}
