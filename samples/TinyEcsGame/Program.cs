using System.Numerics;
using TinyEcs;
using Raylib_cs;


const int WINDOW_WIDTH = 800;
const int WINDOW_HEIGHT = 600;
const int VELOCITY = 250;
const int ENTITIES_TO_SPAWN = 1_000_00;


Raylib.InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "TinyEcs sample");

using var ecs = new World();
SpawnEntities(ecs);

var systems = new List<IUpdateSystem<float>>();
systems.Add(new MoveSystem(ecs));
systems.Add(new CheckBorderSystem(ecs));

var rendering = new List<IUpdateSystem<float>>();
rendering.Add(new BeginRenderSystem());
rendering.Add(new RenderEntities(ecs));
rendering.Add(new RenderText(ecs));
rendering.Add(new EndRenderSystem());

while (!Raylib.WindowShouldClose())
{
	var deltaTime = Raylib.GetFrameTime();

	foreach (var system in systems)
		system.Update(in deltaTime);

	foreach (var system in rendering)
		system.Update(in deltaTime);
}

Raylib.CloseWindow();


void SpawnEntities(World ecs)
{
	var rnd = new Random();
	var texture = Raylib.LoadTexture(Path.Combine(AppContext.BaseDirectory, "Content", "pepe.png"));

	for (ulong i = 0; i < ENTITIES_TO_SPAWN; ++i)
	{
		ecs!
			.Entity()
			.Set(
				new Position()
				{
					Value = new Vector2(rnd.Next(0, WINDOW_WIDTH), rnd.Next(0, WINDOW_HEIGHT))
				}
			)
			.Set(
				new Velocity()
				{
					Value = new Vector2(
						rnd.Next(-VELOCITY, VELOCITY),
						rnd.Next(-VELOCITY, VELOCITY)
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


sealed class MoveSystem : UpdateSystem<float>
{
	public MoveSystem(World ecs) : base(ecs)
	{

	}

	public override void Update(in float data)
	{
		var deltaTime = data;

		Ecs.Query((ref Position pos, ref Velocity vel, ref Rotation rot) =>
		{
			pos.Value += vel.Value * deltaTime;
			rot.Value += rot.Acceleration * deltaTime * Raylib.RAD2DEG;
		});
	}
}

sealed class CheckBorderSystem : UpdateSystem<float>
{
	public CheckBorderSystem(World ecs) : base(ecs)
	{
	}

	public override void Update(in float data)
	{
		const int WINDOW_WIDTH = 800;
		const int WINDOW_HEIGHT = 600;

		Ecs.Query((ref Position pos, ref Velocity vel) =>
		{
			if (pos.Value.X < 0.0f)
			{
				pos.Value.X = 0;
				vel.Value.X *= -1;
			}
			else if (pos.Value.X > WINDOW_WIDTH)
			{
				pos.Value.X = WINDOW_WIDTH;
				vel.Value.X *= -1;
			}

			if (pos.Value.Y < 0.0f)
			{
				pos.Value.Y = 0;
				vel.Value.Y *= -1;
			}
			else if (pos.Value.Y > WINDOW_HEIGHT)
			{
				pos.Value.Y = WINDOW_HEIGHT;
				vel.Value.Y *= -1;
			}
		});
	}
}

sealed class BeginRenderSystem : IUpdateSystem<float>
{
	public void Update(in float gameTime)
	{
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.Black);
	}
}

sealed class EndRenderSystem : IUpdateSystem<float>
{
	public void Update(in float gameTime)
	{
		Raylib.EndDrawing();
	}
}

sealed class RenderEntities : UpdateSystem<float>
{
	public RenderEntities(World ecs) : base(ecs)
	{
	}

	public override void Update(in float gameTime)
	{
		Ecs.Query((ref Sprite sprite, ref Position pos, ref Rotation rotation) =>
		{
			Raylib.DrawTextureEx(sprite.Texture, pos.Value, rotation.Value, sprite.Scale, sprite.Color);
		});
	}
}

sealed class RenderText : UpdateSystem<float>
{
	public RenderText(World ecs) : base(ecs)
	{
	}

	public override void Update(in float gameTime)
	{
		var deltaTime = gameTime;

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

abstract class UpdateSystem<T> : IUpdateSystem<T>
{
	protected UpdateSystem(World ecs)
	{
		Ecs = ecs;
	}

	public World Ecs { get; }

	public abstract void Update(in T data);
}
interface IUpdateSystem<T>
{
	void Update(in T data);
}
