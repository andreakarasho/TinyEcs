using System.Collections;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

using var game = new TinyGame();
game.Run();


sealed unsafe class TinyGame : Game
{
	const int WINDOW_WIDTH = 800;
	const int WINDOW_HEIGHT = 600;

	const int VELOCITY = 250;
	const int ENTITIES_TO_SPAWN = 1000;

	private readonly GraphicsDeviceManager _graphicsDeviceManager;
	private SpriteBatch? _spriteBatch;
	private World _ecs;
	private FontSystem _fontSystem;

	public TinyGame()
	{
		_graphicsDeviceManager = new GraphicsDeviceManager(this);

		IsFixedTimeStep = false;
		IsMouseVisible = true;
	}

	protected override void Initialize()
	{
		base.Initialize();

		_spriteBatch = new SpriteBatch(GraphicsDevice);

		_graphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
		_graphicsDeviceManager.PreferredBackBufferWidth = WINDOW_WIDTH;
		_graphicsDeviceManager.PreferredBackBufferHeight = WINDOW_HEIGHT;
		_graphicsDeviceManager.ApplyChanges();

		_ecs = new World();

		_ = Assets<GraphicsDevice>.Register("device", GraphicsDevice);
		_ = Assets<SpriteBatch>.Register("batcher", _spriteBatch);


        _ecs.StartupSystem(&Setup);
		_ecs.StartupSystem(&SpawnEntities);

		var qry = _ecs.Query()
			.With<Position>()
			.With<Velocity>()
			.With<Rotation>();

		_ecs.System(&MoveSystem, qry);
		_ecs.System(&CheckBorderSystem, qry);
		_ecs.System(&PrintMessage, 1f);


		_ecs.System(&BeginRender)
			.Set<EcsPhase, RenderingPhase>();

		_ecs.System(&Render,
			_ecs.Query()
				.With<Position>()
				.With<Rotation>()
				.With<Sprite>()
			)
			.Set<EcsPhase, RenderingPhase>();

		_ecs.System(&RenderText)
			.Set<EcsPhase, RenderingPhase>();

		_ecs.System(&EndRender)
			.Set<EcsPhase, RenderingPhase>();
	}

	protected override void LoadContent()
	{
		_fontSystem = new FontSystem();
		var path = Path.Combine(AppContext.BaseDirectory, "Content", "fonts", "Roboto-Regular.ttf");
        _fontSystem.AddFont(File.ReadAllBytes(path));

		_ = Assets<FontSystem>.Register("fontSys", _fontSystem);
	}


	protected override void Update(GameTime gameTime)
	{
		_ecs!.Step((float)gameTime.ElapsedGameTime.TotalSeconds);

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		_ecs!.RunPhase<RenderingPhase>();

		base.Draw(gameTime);
	}

	static void PrintMessage(ref Iterator it)
	{
		Console.WriteLine("print!");
	}

    static void Setup(ref Iterator it)
    {
        var deviceHandle = Assets<GraphicsDevice>.Get("device");
        var batcherHandle = Assets<SpriteBatch>.Get("batcher");
		var fontSysHandle = Assets<FontSystem>.Get("fontSys");

        it.World.SetSingleton(new GameState(deviceHandle, batcherHandle, fontSysHandle));
    }

	static void SpawnEntities(ref Iterator it)
	{
		var rnd = new Random();
		ref var gameState = ref it.World.GetSingleton<GameState>();
		var texture = Texture2D.FromFile(gameState.Device.GetValue(), Path.Combine(AppContext.BaseDirectory, "Content", "pepe.png"));
		var textureHandle = Assets<Texture2D>.Register("texture", texture);

		for (ulong i = 0; i < ENTITIES_TO_SPAWN; ++i)
		{
			it.Commands!.Spawn()
				.Set(new Position()
				{
					Value = new Vector2(rnd.Next(0, WINDOW_WIDTH), rnd.Next(0, WINDOW_HEIGHT))
				})
				.Set(new Velocity()
				{
					Value = new Vector2(rnd.Next(-VELOCITY, VELOCITY), rnd.Next(-VELOCITY, VELOCITY))
				})
				.Set(new Sprite()
				{
					Color = new Color(rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)),
					Scale = rnd.NextSingle(),
					Texture = textureHandle
				})
				.Set(new Rotation()
				{
					Value = 0f,
					Acceleration = rnd.Next(5, 20) * (rnd.Next() % 2 == 0 ? -1 : 1)
				});
		}
	}

	static void MoveSystem(ref Iterator it)
	{
		var p = it.Field<Position>();
		var v = it.Field<Velocity>();
		var r = it.Field<Rotation>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var pos = ref p[i];
			ref var vel = ref v[i];
			ref var rot = ref r[i];

			Vector2.Multiply(ref vel.Value, it.DeltaTime, out var res);
			Vector2.Add(ref pos.Value, ref res, out pos.Value);

			rot.Value = MathHelper.WrapAngle(rot.Value + (rot.Acceleration * it.DeltaTime));
		}
	}

	static void CheckBorderSystem(ref Iterator it)
	{
		var p = it.Field<Position>();
		var v = it.Field<Velocity>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var pos = ref p[i];
			ref var vel = ref v[i];

			if (pos.Value.X < 0)
			{
				pos.Value.X = 0;
				vel.Value.X *= -1;
			}
			else if (pos.Value.X > WINDOW_WIDTH)
			{
				pos.Value.X = WINDOW_WIDTH;
				vel.Value.X *= -1;
			}

			if (pos.Value.Y < 0)
			{
				pos.Value.Y = 0;
				vel.Value.Y *= -1;
			}
			else if (pos.Value.Y > WINDOW_HEIGHT)
			{
				pos.Value.Y = WINDOW_HEIGHT;
				vel.Value.Y *= -1;
			}
		}
	}

	static void BeginRender(ref Iterator it)
	{
		ref var gameState = ref it.World.GetSingleton<GameState>();
		var batch = gameState.Batch.GetValue();

		batch.GraphicsDevice.Clear(Color.Black);
		batch.Begin();
	}

	static void EndRender(ref Iterator it)
	{
		ref var gameState = ref it.World.GetSingleton<GameState>();
		var batch = gameState.Batch.GetValue();

		batch.End();
	}

	static void Render(ref Iterator it)
	{
		ref var gameState = ref it.World.GetSingleton<GameState>();
		var batch = gameState.Batch.GetValue();

		var p = it.Field<Position>();
		var s = it.Field<Sprite>();
		var r = it.Field<Rotation>();
		var origin = new Vector2(0.5f);

		for (int i = 0; i < it.Count; ++i)
		{
			ref var pos = ref p[i];
			ref var sprite = ref s[i];
			ref var rotation = ref r[i];

			batch.Draw(
				sprite.Texture,
				pos.Value,
				null,
				sprite.Color,
				rotation.Value,
				origin,
				sprite.Scale,
				SpriteEffects.None,
				0f
			);
		}
	}

	static void RenderText(ref Iterator it)
	{
		ref var gameState = ref it.World.GetSingleton<GameState>();
		var batch = gameState.Batch.GetValue();
		var fontSys = gameState.FontSystem.GetValue();


		var dbgText = $@"[Debug]
					Entities: {it.World.EntityCount}
					DeltaTime: {it.DeltaTime}";
		var font18 = fontSys.GetFont(18);
		var size = font18.MeasureString(dbgText);
		size.X = batch.GraphicsDevice.Viewport.Width - size.X - 15;
		size.Y = 15;
		batch.DrawString(font18, dbgText, size, Color.White, effect: FontSystemEffect.Stroked, effectAmount: 1);


		var rotatingText = "Hello from TinyEcs!";
		var font32 = fontSys.GetFont(32);
		size = font32.MeasureString(rotatingText);
		size.X = batch.GraphicsDevice.Viewport.Width / 2f - size.X / 2f;
		size.Y = batch.GraphicsDevice.Viewport.Height / 2f - size.Y / 2f;
		batch.DrawString(font32, rotatingText, size, Color.Yellow, effect: FontSystemEffect.Stroked, effectAmount: 1);
	}
}

struct Position : IComponent { public Vector2 Value; }
struct Velocity : IComponent { public Vector2 Value; }
struct Sprite : IComponent { public Color Color; public float Scale; public Handle<Texture2D> Texture; }
struct Rotation : IComponent { public float Value; public float Acceleration; }
struct RenderingPhase : ITag { }

readonly struct GameState : IComponent
{
	public readonly Handle<GraphicsDevice> Device;
	public readonly Handle<SpriteBatch> Batch;
	public readonly Handle<FontSystem> FontSystem;

	public GameState(Handle<GraphicsDevice> device, Handle<SpriteBatch> batch, Handle<FontSystem> fontSystem)
	{
		Device = device;
		Batch = batch;
		FontSystem = fontSystem;
	}
}

public static class Assets<T>
{
	private static T[] assets = new T[32];
	private static readonly Dictionary<string, int> identifierToSlot = new Dictionary<string, int>();
	private static int nextFreeSlot;

	public static int Count { get; private set; }

	public static Handle<T> Register(string identifier, T asset)
	{
		if (identifierToSlot.ContainsKey(identifier))
			throw new Exception("Cannot register multiple assets with the same name: " + identifier);

		int slot = nextFreeSlot++;

		if (slot >= assets.Length)
			Array.Resize(ref assets, assets.Length * 2);

		assets[slot] = asset;
		identifierToSlot[identifier] = slot;

		return new Handle<T>(slot);
	}

	public static Handle<T> Get(string identifier)
	{
		if (!identifierToSlot.TryGetValue(identifier, out int value))
			throw new Exception("Asset does not exist: " + identifier);

		return new Handle<T>(value);
	}

	internal static T Get(int slot) => assets[slot];

	internal static bool Has(string identifier) => identifierToSlot.ContainsKey(identifier);
}

public readonly struct Handle<T> : IComponent
{
	private readonly int slot;

	internal Handle(int slot)
	{
		this.slot = slot;
	}

	public readonly T GetValue() => Assets<T>.Get(slot);

	public static implicit operator T(Handle<T> handle) => handle.GetValue();
}
