using System.Numerics;
using TinyEcs;
using Raylib_cs;
using System.Runtime.CompilerServices;


const int WINDOW_WIDTH = 800;
const int WINDOW_HEIGHT = 600;
const int VELOCITY = 250;
const int ENTITIES_TO_SPAWN = 1_000_00;


Raylib.InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "TinyEcs sample");

using var ecs = new World();

var scheduler = new Scheduler(ecs);
var wndSize = new WindowSize() { Value = { X = WINDOW_WIDTH, Y = WINDOW_HEIGHT } };

// bleh
var init = SpawnEntities;
var fn0 = MoveSystem;
var fn1 = CheckBounds;
var fn2 = BeginRenderer;
var fn3 = RenderEntities;
var fn4 = DrawText;
var fn5 = EndRenderer;


scheduler.AddSystem(init, Stages.Startup);
scheduler.AddSystem((Res<Time> time) => time.Value.Value = Raylib.GetFrameTime(), Stages.BeforeUpdate);
scheduler.AddPlugin<RaylibPlugin>();
scheduler.AddSystem(fn0);
scheduler.AddSystem(fn1);
scheduler.AddSystem(fn2, stage: Stages.FrameEnd, threadingType: ThreadingMode.Single);
scheduler.AddSystem(fn3, stage: Stages.FrameEnd, threadingType: ThreadingMode.Single);
scheduler.AddSystem(fn4, stage: Stages.FrameEnd, threadingType: ThreadingMode.Single);
scheduler.AddSystem(fn5, stage: Stages.FrameEnd, threadingType: ThreadingMode.Single);

scheduler.AddResource(wndSize);
scheduler.AddResource(new Time());


while (!Raylib.WindowShouldClose())
{
	scheduler.Run();
}

Raylib.CloseWindow();



static void MoveSystem(Res<Time> time, Query<(Position, Velocity, Rotation)> query)
{
	query.EachJob((ref Position pos, ref Velocity vel, ref Rotation rot) =>
	{
		pos.Value += vel.Value * time.Value.Value;
		rot.Value = (rot.Value + (rot.Acceleration * time.Value.Value)) % 360;
	});
}

static void CheckBounds(Query<(Position, Velocity)> query, Res<WindowSize> windowSize)
{
	query.EachJob((ref Position pos, ref Velocity vel) =>
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

static void RenderEntities(Query<(Sprite, Position, Rotation)> query, Res<Texture2D> texture)
{
	query.Each((ref Sprite sprite, ref Position pos, ref Rotation rotation) =>
	{
		Raylib.DrawTextureEx(texture.Value, pos.Value, rotation.Value, sprite.Scale, sprite.Color);
	});
}

static void DrawText(World ecs, Res<Time> time)
{
	var dbgText =
		$"""
			[Debug]
			FPS: {Raylib.GetFPS()}
			Entities: {ecs.EntityCount}
			DeltaTime: {time.Value.Value}
			""".Replace("\r", "\n");
	var textSize = 24;
	Raylib.DrawText(dbgText, 15, 15, textSize, Color.White);
}

static void SpawnEntities(World ecs, Res<WindowSize> size, SchedulerState scheduler)
{
	var rnd = new Random();
	var texture = Raylib.LoadTexture(Path.Combine(AppContext.BaseDirectory, "Content", "pepe.png"));
	scheduler.AddResource(texture);

	for (var i = 0; i < ENTITIES_TO_SPAWN; ++i)
	{
		ecs
			.Entity()
			.Set(
				new Position()
				{
					Value = new Vector2(rnd.Next(0, (int)size.Value.Value.X), rnd.Next(0, (int)size.Value.Value.Y))
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
					Texture = texture.Id
				}
			)
			.Set(
				new Rotation()
				{
					Value = 0f,
					Acceleration = rnd.Next(45, 180) * (rnd.Next() % 2 == 0 ? -1 : 1)
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
	public uint Texture;
    //public Texture2D Texture;
}

struct Rotation
{
    public float Value;
    public float Acceleration;
}

readonly struct RaylibPlugin : IPlugin
{
	public readonly void Build(Scheduler scheduler)
	{
		scheduler.AddSystem((Res<Input> input) => {
			foreach (ref var v in input.Value)
				v = KeyboardKey.Null;

			var key = Raylib.GetKeyPressed();
			while (key != 0)
			{
				input.Value[key] = (KeyboardKey) key;
				key = Raylib.GetKeyPressed();
			}
		});
		scheduler.AddSystem((Res<Input> input) => {
			if (input.Value[(int)KeyboardKey.A] == KeyboardKey.A)
			{
				Console.WriteLine("pressed {0}", KeyboardKey.A);
			}
		});
		scheduler.AddResource(new Input());
	}

	[InlineArray(512)]
	struct Input
	{
		private KeyboardKey _k0;
	}
}
