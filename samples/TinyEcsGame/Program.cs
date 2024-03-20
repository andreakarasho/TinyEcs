using System.Numerics;
using TinyEcs;
using Raylib_cs;


const int WINDOW_WIDTH = 800;
const int WINDOW_HEIGHT = 600;
const int VELOCITY = 250;
const int ENTITIES_TO_SPAWN = 1_000_00;


Raylib.InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "TinyEcs sample");

using var ecs = new World();

var scheduler = new Scheduler(ecs);
var wndSize = new WindowSize() { Value = { X = WINDOW_WIDTH, Y = WINDOW_HEIGHT } };
SpawnEntities(ecs, wndSize);

// bleh
var fn0 = MoveSystem;
var fn1 = CheckBounds;
var fn2 = BeginRenderer;
var fn3 = RenderEntities;
var fn4 = DrawText;
var fn5 = EndRenderer;

scheduler
	.AddSystem((Res<Time> time) => time.Value = new Time() { Value = Raylib.GetFrameTime() })
	.AddSystem(fn0)
	.AddSystem(fn1)
	.AddSystem(fn2)
	.AddSystem(fn3)
	.AddSystem(fn4)
	.AddSystem(fn5)

	.AddResource(wndSize)
	.AddResource(new Time());


while (!Raylib.WindowShouldClose())
{
	scheduler.Run();
}

Raylib.CloseWindow();



static void MoveSystem(Res<Time> time, Query<(Position, Velocity, Rotation)> query)
{
	query.Each((ref Position pos, ref Velocity vel, ref Rotation rot) =>
	{
		pos.Value += vel.Value * time.Value.Value;
		rot.Value += rot.Acceleration * time.Value.Value * Raylib.RAD2DEG;
	});
}

static void CheckBounds(Query<(Position, Velocity)> query, Res<WindowSize> windowSize)
{
	query.Each((ref Position pos, ref Velocity vel) =>
	{
		if (pos.Value.X < 0.0f)
		{
			pos.Value.X = 0;
			vel.Value.X *= -1;
		}
		else if (pos.Value.X > windowSize.Value.Value.X)
		{
			pos.Value.X = windowSize.Value.Value.X;
			vel.Value.X *= -1;
		}

		if (pos.Value.Y < 0.0f)
		{
			pos.Value.Y = 0;
			vel.Value.Y *= -1;
		}
		else if (pos.Value.Y > windowSize.Value.Value.Y)
		{
			pos.Value.Y = windowSize.Value.Value.Y;
			vel.Value.Y *= -1;
		}
	});
}

static void BeginRenderer()
{
	Raylib.BeginDrawing();
	Raylib.ClearBackground(Color.Black);
}

static void EndRenderer()
{
	Raylib.EndDrawing();
}

static void RenderEntities(Query<(Sprite, Position, Rotation)> query)
{
	query.Each((ref Sprite sprite, ref Position pos, ref Rotation rotation) =>
	{
		Raylib.DrawTextureEx(sprite.Texture, pos.Value, rotation.Value, sprite.Scale, sprite.Color);
	});
}

static void DrawText(World ecs)
{
	var deltaTime = Raylib.GetFrameTime();

	var dbgText =
		$"""
			[Debug]
			FPS: {Raylib.GetFPS()}
			Entities: {ecs.EntityCount}
			DeltaTime: {deltaTime}
			""".Replace("\r", "\n");
	var textSize = 24;
	Raylib.DrawText(dbgText, 15, 15, textSize, Color.White);
}

static void SpawnEntities(World ecs, WindowSize size)
{
	var rnd = new Random();
	var texture = Raylib.LoadTexture(Path.Combine(AppContext.BaseDirectory, "Content", "pepe.png"));

	for (var i = 0; i < ENTITIES_TO_SPAWN; ++i)
	{
		ecs!
			.Entity()
			.Set(
				new Position()
				{
					Value = new Vector2(rnd.Next(0, (int)size.Value.X), rnd.Next(0, (int)size.Value.Y))
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

struct Time
{
	public float Value;
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
    public Texture2D Texture;
}

struct Rotation
{
    public float Value;
    public float Acceleration;
}
