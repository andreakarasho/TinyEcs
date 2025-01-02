using System.Numerics;
using TinyEcs;
using Raylib_cs;


using var app = new App();
app.AddPlugin(new RaylibPlugin() {
	WindowSize = new () { Value = { X = 800, Y = 600 } },
	Title = "TinyEcs using raylib",
	VSync = true
});

app.AddPlugin(new GameRootPlugin() {
	EntitiesToSpawn = 1_000_00,
	Velocity = 250
});

app.Run(() => Raylib.WindowShouldClose(), Raylib.CloseWindow);



// =================================================================================
sealed class App : Scheduler, IDisposable
{
	public App() : base(new ()) { }

	public void Dispose() => World?.Dispose();
}
// =================================================================================
sealed class Time : SystemParam<World>, IIntoSystemParam<World>
{
	public float Frame;
	public float Total;

	public static ISystemParam<World> Generate(World world)
	{
		if (world.Entity<Placeholder<Time>>().Has<Placeholder<Time>>())
			return world.Entity<Placeholder<Time>>().Get<Placeholder<Time>>().Value;

		var ev = new Time();
		world.Entity<Placeholder<Time>>().Set(new Placeholder<Time>() { Value = ev });
		return ev;
	}

	struct Placeholder<T> { public T Value; }
}
// =================================================================================
struct RaylibPlugin : IPlugin
{
	public string Title { get; set; }
	public WindowSize WindowSize { get; set; }
	public bool VSync { get; set; }

	public readonly void Build(Scheduler scheduler)
	{
		ConfigFlags flags = 0;
		if (VSync)
			flags |= ConfigFlags.VSyncHint;

		Raylib.SetConfigFlags(flags);
		Raylib.InitWindow((int)WindowSize.Value.X, (int)WindowSize.Value.Y, Title);

		scheduler.AddSystemParam(new Time());
		scheduler.AddResource(WindowSize);
		scheduler.AddResource(new AssetsManager());

		scheduler.AddSystem((Time time) => {
			time.Frame = Raylib.GetFrameTime();
			time.Total += time.Frame;
		}, Stages.BeforeUpdate);
	}
}
// =================================================================================
struct GameRootPlugin : IPlugin
{
	public int EntitiesToSpawn { get; set; }
	public int Velocity { get; set; }

	public void Build(Scheduler scheduler)
	{
		scheduler.AddPlugin(new GameplayPlugin() { EntitiesToSpawn = EntitiesToSpawn, Velocity = Velocity });
		scheduler.AddPlugin<RenderingPlugin>();
	}
}
// =================================================================================
struct GameplayPlugin : IPlugin
{
	public int EntitiesToSpawn { get; set; }
	public int Velocity { get; set; }

	public void Build(Scheduler scheduler)
	{
		var init = SpawnEntities;
		var fn0 = MoveSystem;
		var fn1 = CheckBounds;

		scheduler.AddSystem(init, Stages.Startup);
		scheduler.AddSystem(fn0);
		scheduler.AddSystem(fn1);
	}

	static void MoveSystem(Time time, Query<Data<Position, Velocity, Rotation>> query)
	{
		foreach ((var pos, var vel, var rot) in query)
		{
			pos.Ref.Value += vel.Ref.Value * time.Frame;
			rot.Ref.Value = (rot.Ref.Value + (rot.Ref.Acceleration * time.Frame)) % 360;
		}
	}

	static void CheckBounds(Query<Data<Position, Velocity>> query, Res<WindowSize> windowSize)
	{
		foreach ((var pos, var vel) in query)
		{
			if (pos.Ref.Value.X < 0.0f)
			{
				pos.Ref.Value.X = 0;
				vel.Ref.Value.X *= -1;
			}
			else if (pos.Ref.Value.X > windowSize.Value.Value.X)
			{
				pos.Ref.Value.X = windowSize.Value.Value.X;
				vel.Ref.Value.X *= -1;
			}

			if (pos.Ref.Value.Y < 0.0f)
			{
				pos.Ref.Value.Y = 0;
				vel.Ref.Value.Y *= -1;
			}
			else if (pos.Ref.Value.Y > windowSize.Value.Value.Y)
			{
				pos.Ref.Value.Y = windowSize.Value.Value.Y;
				vel.Ref.Value.Y *= -1;
			}
		}
	}

	void SpawnEntities(World ecs, SchedulerState scheduler, Res<WindowSize> size, Res<AssetsManager> assetsManager)
	{
		var rnd = Random.Shared;
		var texture = Raylib.LoadTexture(Path.Combine(AppContext.BaseDirectory, "Content", "pepe.png"));
		assetsManager.Value!.Register(texture);

		for (var i = 0; i < EntitiesToSpawn; ++i)
		{
			ecs
				.Entity()
				.Set(
					new Position()
					{
						Value = new Vector2(
							rnd.Next(0, (int)size.Value.Value.X),
							rnd.Next(0, (int)size.Value.Value.Y))
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
						TextureId = texture.Id
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
}
// =================================================================================
readonly struct RenderingPlugin : IPlugin
{
	public void Build(Scheduler scheduler)
	{
		var fn2 = BeginRenderer;
		var fn3 = RenderEntities;
		var fn4 = DrawText;
		var fn5 = EndRenderer;

		var begin = scheduler.AddSystem(fn2, stage: Stages.FrameEnd, threadingType: ThreadingMode.Single);
		var renderEntities = scheduler.AddSystem(fn3, stage: Stages.FrameEnd, threadingType: ThreadingMode.Single);
		var renderText = scheduler.AddSystem(fn4, stage: Stages.FrameEnd, threadingType: ThreadingMode.Single);
		var end = scheduler.AddSystem(fn5, stage: Stages.FrameEnd, threadingType: ThreadingMode.Single);
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

	static void RenderEntities(Query<Data<Sprite, Position, Rotation>> query, Res<AssetsManager> assetsManager)
	{
		var currTextureId = 0u;
		Texture2D? texture = null;

		foreach ((var sprite, var pos, var rot) in query)
		{
			if (sprite.Ref.TextureId != currTextureId)
			{
				currTextureId = sprite.Ref.TextureId;
				texture = assetsManager.Value!.Get(currTextureId);
			}

			if (texture.HasValue)
				Raylib.DrawTextureEx(texture.Value, pos.Ref.Value, rot.Ref.Value, sprite.Ref.Scale, sprite.Ref.Color);
		}
	}

	static void DrawText(World ecs, Time time, Local<string> text, Local<float> timeout)
	{
		if (time.Total > timeout)
		{
			timeout.Value = time.Total + 0.25f;
			text.Value = $"""
				[Debug]
				FPS: {Raylib.GetFPS()}
				Entities: {ecs.EntityCount}
				DeltaTime: {time.Frame}
				""".Replace("\r", "\n");
		}

		var textSize = 24;
		Raylib.DrawText(text.Value ?? "", 15, 15, textSize, Color.White);
	}
}
// =================================================================================
sealed class AssetsManager
{
	private readonly Dictionary<uint, Texture2D> _ids = new ();

	public void Register(Texture2D texture)
	{
		_ids[texture.Id] = texture;
	}

	public Texture2D? Get(uint id)
	{
		if (!_ids.TryGetValue(id, out var texture))
			return null;
		return texture;
	}
}
// =================================================================================
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
