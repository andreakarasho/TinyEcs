using System.Collections;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
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
	private Ecs _ecs;

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

		_ecs = new Ecs();

		_ = Assets<GraphicsDevice>.Register("device", GraphicsDevice);
		_ = Assets<SpriteBatch>.Register("batcher", _spriteBatch);


        _ecs.AddStartupSystem(&Setup);
		_ecs.AddStartupSystem(&SpawnEntities);

		var qry = _ecs.Query()
			.With<Position>()
			.With<Velocity>()
			.With<Rotation>();

		_ecs.AddSystem(&MoveSystem)
		    .SetQuery(qry);
		_ecs.AddSystem(&CheckBorderSystem)
			.SetQuery(qry);

		_ecs.AddSystem(&BeginRender);
		_ecs.AddSystem(&Render)
			.SetQuery(
				_ecs.Query()
				.With<Position>()
				.With<Rotation>()
				.With<Sprite>()
			);
		_ecs.AddSystem(&EndRender);

		_ecs.AddSystem(&PrintMessage)
			.SetTick(1f);
	}



	protected override void Update(GameTime gameTime)
	{
		_ecs!.Step((float)gameTime.ElapsedGameTime.TotalSeconds);

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		base.Draw(gameTime);
	}

	static void PrintMessage(Commands commands, Archetype it)
	{
		Console.WriteLine("print!");
	}

    static void Setup(Commands commands, Archetype it)
    {
        var deviceHandle = Assets<GraphicsDevice>.Get("device");
        var batcherHandle = Assets<SpriteBatch>.Get("batcher");

        it.World.SetSingleton(new GameState(deviceHandle, batcherHandle));
    }

	static void SpawnEntities(Commands commands, Archetype it)
	{
		var rnd = new Random();
		ref var gameState = ref it.World.GetSingleton<GameState>();
		var texture = Texture2D.FromFile(gameState.Device.GetValue(), Path.Combine(AppContext.BaseDirectory, "Content", "pepe.png"));
		var textureHandle = Assets<Texture2D>.Register("texture", texture);

		for (ulong i = 0; i < ENTITIES_TO_SPAWN; ++i)
		{
			commands.Spawn()
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

	static void MoveSystem(Commands commands, Archetype it)
	{
		var deltaTime = 0f;
		var p = it.Field<Position>();
		var v = it.Field<Velocity>();
		var r = it.Field<Rotation>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var pos = ref p[i];
			ref var vel = ref v[i];
			ref var rot = ref r[i];

			Vector2.Multiply(ref vel.Value, deltaTime, out var res);
			Vector2.Add(ref pos.Value, ref res, out pos.Value);

			rot.Value = MathHelper.WrapAngle(rot.Value + (rot.Acceleration * deltaTime));
		}
	}

	static void CheckBorderSystem(Commands commands, Archetype it)
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

	static void BeginRender(Commands commands, Archetype it)
	{
		ref var gameState = ref it.World.GetSingleton<GameState>();
		var batch = gameState.Batch.GetValue();

		batch.GraphicsDevice.Clear(Color.Black);
		batch.Begin();
	}

	static void EndRender(Commands commands, Archetype it)
	{
		ref var gameState = ref it.World.GetSingleton<GameState>();
		var batch = gameState.Batch.GetValue();

		batch.End();
	}

	static void Render(Commands commands, Archetype it)
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
}

struct Position { public Vector2 Value; }
struct Velocity { public Vector2 Value; }
struct Sprite { public Color Color; public float Scale; public Handle<Texture2D> Texture; }
struct Rotation { public float Value; public float Acceleration; }


readonly struct GameState
{
	public readonly Handle<GraphicsDevice> Device;
	public readonly Handle<SpriteBatch> Batch;

	public GameState(Handle<GraphicsDevice> device, Handle<SpriteBatch> batch)
	{
		Device = device;
		Batch = batch;
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

public readonly struct Handle<T>
{
	private readonly int slot;

	internal Handle(int slot)
	{
		this.slot = slot;
	}

	public readonly T GetValue() => Assets<T>.Get(slot);

	public static implicit operator T(Handle<T> handle) => handle.GetValue();
}
