using System.Numerics;
using TinyEcs;
using Raylib_cs;


const int WINDOW_WIDTH = 800;
const int WINDOW_HEIGHT = 600;
const int VELOCITY = 250;
const int ENTITIES_TO_SPAWN = 1_000_00;


Raylib.InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "TinyEcs sample");

using var ecs = new World();
var systems = new SystemManager(ecs);

var spawner = systems.Add<SpawnEntities>();
spawner.EntitiesToSpawn = ENTITIES_TO_SPAWN;
spawner.WindowSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
spawner.Velocity = VELOCITY;

systems.Add<MoveSystem>();
systems.Add<CheckBorderSystem>().WindowSize = spawner.WindowSize;

systems.Add<BeginRenderSystem>();
systems.Add<RenderEntities>();
systems.Add<RenderText>();
systems.Add<EndRenderSystem>();


while (!Raylib.WindowShouldClose())
{
	systems.Update();
}

systems.Clear();
Raylib.CloseWindow();





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
    public Texture2D Texture;
}

struct Rotation
{
    public float Value;
    public float Acceleration;
}


sealed class MoveSystem : TinyEcs.EcsSystem
{
	public override void OnUpdate()
	{
		var deltaTime = Raylib.GetFrameTime();

		Ecs.Query((ref Position pos, ref Velocity vel, ref Rotation rot) =>
		{
			pos.Value += vel.Value * deltaTime;
			rot.Value += rot.Acceleration * deltaTime * Raylib.RAD2DEG;
		});
	}
}

sealed class CheckBorderSystem : TinyEcs.EcsSystem
{
	public Vector2 WindowSize { get; set; }

	public override void OnUpdate()
	{
		Ecs.Query((ref Position pos, ref Velocity vel) =>
		{
			if (pos.Value.X < 0.0f)
			{
				pos.Value.X = 0;
				vel.Value.X *= -1;
			}
			else if (pos.Value.X > WindowSize.X)
			{
				pos.Value.X = WindowSize.X;
				vel.Value.X *= -1;
			}

			if (pos.Value.Y < 0.0f)
			{
				pos.Value.Y = 0;
				vel.Value.Y *= -1;
			}
			else if (pos.Value.Y > WindowSize.Y)
			{
				pos.Value.Y = WindowSize.Y;
				vel.Value.Y *= -1;
			}
		});
	}
}

sealed class BeginRenderSystem : TinyEcs.EcsSystem
{
	public override void OnUpdate()
	{
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.Black);
	}
}

sealed class EndRenderSystem : TinyEcs.EcsSystem
{
	public override void OnUpdate()
	{
		Raylib.EndDrawing();
	}
}

sealed class RenderEntities : TinyEcs.EcsSystem
{
	public override void OnUpdate()
	{
		Ecs.Query((ref Sprite sprite, ref Position pos, ref Rotation rotation) =>
		{
			Raylib.DrawTextureEx(sprite.Texture, pos.Value, rotation.Value, sprite.Scale, sprite.Color);
		});
	}
}

sealed class RenderText : TinyEcs.EcsSystem
{
	public override void OnUpdate()
	{
		var deltaTime = Raylib.GetFrameTime();

		var dbgText =
			$"""
			 [Debug]
			 FPS: {Raylib.GetFPS()}
			 Entities: {Ecs.EntityCount}
			 DeltaTime: {deltaTime}
			 """.Replace("\r", "\n");
		var textSize = 24;
		Raylib.DrawText(dbgText, 15, 15, textSize, Color.White);
	}
}

sealed class SpawnEntities : TinyEcs.EcsSystem
{
	public int EntitiesToSpawn { get; set; }
	public Vector2 WindowSize { get; set; }
	public int Velocity { get; set; }

	public override void OnCreate()
	{
		// This system is just one shot
		Disable();

		var rnd = new Random();
		var texture = Raylib.LoadTexture(Path.Combine(AppContext.BaseDirectory, "Content", "pepe.png"));

		for (var i = 0; i < EntitiesToSpawn; ++i)
		{
			Ecs!
				.Entity()
				.Set(
					new Position()
					{
						Value = new Vector2(rnd.Next(0, (int)WindowSize.X), rnd.Next(0, (int)WindowSize.Y))
					}
				)
				.Set(
					new Velocity()
					{
						Value = new Vector2(
							rnd.Next(-Velocity, Velocity),
							rnd.Next(-Velocity, Velocity)
						)
					}
				)
				.Set(
					new Sprite()
					{
						Color = new Color(rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256), 255),
						Scale = rnd.NextSingle(),
						Texture = texture
					}
				)
				.Set(
					new Rotation()
					{
						Value = 0f,
						Acceleration = rnd.Next(5, 20) * (rnd.Next() % 2 == 0 ? -1 : 1)
					}
				);
		}
	}
}
