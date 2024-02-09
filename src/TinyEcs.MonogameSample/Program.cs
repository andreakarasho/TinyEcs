using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

using var game = new TinyGame();
game.Run();

sealed class TinyGame : Game
{
    public const int WINDOW_WIDTH = 800;
    public const int WINDOW_HEIGHT = 600;

    public const int VELOCITY = 250;
    const int ENTITIES_TO_SPAWN = 4096;

    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private SpriteBatch? _spriteBatch;
    private World _ecs;
    private FontSystem _fontSystem;
    private readonly List<IUpdateSystem<GameTime>> _systems = new List<IUpdateSystem<GameTime>>();
    private readonly List<IUpdateSystem<GameTime>> _rendering = new List<IUpdateSystem<GameTime>>();

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

        SpawnEntities();

        _systems.Add(new MoveSystem(_ecs));
        _systems.Add(new CheckBorderSystem(_ecs));

        _rendering.Add(new BeginRenderSystem(_spriteBatch));
        _rendering.Add(new RenderEntities(_ecs, _spriteBatch));
        _rendering.Add(new RenderText(_ecs, _spriteBatch, _fontSystem));
        _rendering.Add(new EndRenderSystem(_spriteBatch));
    }

    protected override void LoadContent()
    {
        _fontSystem = new FontSystem();

        var path = Path.Combine(AppContext.BaseDirectory, "Content", "fonts");
        foreach (var file in Directory.GetFiles(path, "*.ttf"))
            _fontSystem.AddFont(File.ReadAllBytes(file));
    }

    protected override void Update(GameTime gameTime)
    {
        foreach (var system in _systems)
	        system.Update(in gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
	    foreach (var system in _rendering)
		    system.Update(in gameTime);

        base.Draw(gameTime);
    }

    private void SpawnEntities()
    {
	    var rnd = new Random();
	    var texture = Texture2D.FromFile(
		    GraphicsDevice,
		    Path.Combine(AppContext.BaseDirectory, "Content", "pepe.png")
	    );

	    for (ulong i = 0; i < ENTITIES_TO_SPAWN; ++i)
	    {
		    _ecs!
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
					    Color = new Color(rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)),
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


sealed class MoveSystem : IUpdateSystem<GameTime>
{
	private readonly Query _query;

	public MoveSystem(World ecs)
	{
		_query = ecs.Query()
			//.With<Position>().With<Rotation>().With<Velocity>()
			;
	}

	public void Update(in GameTime data)
	{
		var deltaTime = (float)data.ElapsedGameTime.TotalSeconds;

		_query.Each((ref Position pos, ref Velocity vel, ref Rotation rot) =>
		{
			Vector2.Multiply(ref vel.Value, deltaTime, out var res);
			Vector2.Add(ref pos.Value, ref res, out pos.Value);

			rot.Value = MathHelper.WrapAngle(rot.Value + (rot.Acceleration * deltaTime));
		});

		// foreach (var arch in _query)
		// {
		// 	var index0 = arch.GetComponentIndex<Position>();
		// 	var index1 = arch.GetComponentIndex<Velocity>();
		// 	var index2 = arch.GetComponentIndex<Rotation>();
		//
		// 	foreach (ref var chunk in arch.Chunks)
		// 	{
		// 		ref var pos = ref chunk.GetReference<Position>(index0);
		// 		ref var vel = ref chunk.GetReference<Velocity>(index1);
		// 		ref var rot = ref chunk.GetReference<Rotation>(index2);
		// 		ref var last = ref Unsafe.Add(ref pos, chunk.Count);
		//
		// 		while (Unsafe.IsAddressLessThan(ref pos, ref last))
		// 		{
		// 			Move((float)data.ElapsedGameTime.TotalSeconds, ref pos, ref vel, ref rot);
		//
		// 			pos = ref Unsafe.Add(ref pos, 1);
		// 			vel = ref Unsafe.Add(ref vel, 1);
		// 			rot = ref Unsafe.Add(ref rot, 1);
		// 		}
		// 	}
		// }
	}

	private static void Move(float deltaTime, ref Position pos, ref Velocity vel, ref Rotation rot)
	{
		Vector2.Multiply(ref vel.Value, deltaTime, out var res);
		Vector2.Add(ref pos.Value, ref res, out pos.Value);

		rot.Value = MathHelper.WrapAngle(rot.Value + (rot.Acceleration * deltaTime));
	}
}

sealed class CheckBorderSystem : IUpdateSystem<GameTime>
{
	private readonly Query _query;

	public CheckBorderSystem(World ecs)
	{
		_query = ecs.Query();
	}

	public void Update(in GameTime data)
	{
		_query.Each((ref Position pos, ref Velocity vel) =>
		{
			if (pos.Value.X < 0)
			{
				pos.Value.X = 0;
				vel.Value.X *= -1;
			}
			else if (pos.Value.X > TinyGame.WINDOW_WIDTH)
			{
				pos.Value.X = TinyGame.WINDOW_WIDTH;
				vel.Value.X *= -1;
			}

			if (pos.Value.Y < 0)
			{
				pos.Value.Y = 0;
				vel.Value.Y *= -1;
			}
			else if (pos.Value.Y > TinyGame.WINDOW_HEIGHT)
			{
				pos.Value.Y = TinyGame.WINDOW_HEIGHT;
				vel.Value.Y *= -1;
			}
		});
	}
}

sealed class BeginRenderSystem : IUpdateSystem<GameTime>
{
	private readonly SpriteBatch _batch;

	public BeginRenderSystem(SpriteBatch batch)
	{
		_batch = batch;
	}

	public void Update(in GameTime gameTime)
	{
		_batch.GraphicsDevice.Clear(Color.Black);
		_batch.Begin();
	}
}

sealed class EndRenderSystem : IUpdateSystem<GameTime>
{
	private readonly SpriteBatch _batch;

	public EndRenderSystem(SpriteBatch batch)
	{
		_batch = batch;
	}

	public void Update(in GameTime gameTime)
	{
		_batch.End();
	}
}

sealed class RenderEntities : IUpdateSystem<GameTime>
{
	private readonly SpriteBatch _batch;
	private readonly Query _query;

	public RenderEntities(World ecs, SpriteBatch batch)
	{
		_batch = batch;
		_query = ecs.Query();
	}

	public void Update(in GameTime gameTime)
	{
		_query.Each((ref Sprite sprite, ref Position pos, ref Rotation rotation) =>
		{
			_batch.Draw(
				sprite.Texture,
				pos.Value,
				null,
				sprite.Color,
				rotation.Value,
				new Vector2(0.5f),
				sprite.Scale,
				SpriteEffects.None,
				0f
			);
		});
	}
}

sealed class RenderText : IUpdateSystem<GameTime>
{
	private readonly SpriteBatch _batch;
	private readonly FontSystem _fontSystem;
	private readonly World _ecs;

	public RenderText(World ecs, SpriteBatch batch, FontSystem fontSystem)
	{
		_ecs = ecs;
		_batch = batch;
		_fontSystem = fontSystem;
	}

	public void Update(in GameTime gameTime)
	{
		var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		var dbgText =
			$@"[Debug]
					Entities: {_ecs.EntityCount}
					DeltaTime: {deltaTime}";
		var font18 = _fontSystem.GetFont(18);
		var size = font18.MeasureString(dbgText);
		size.X = _batch.GraphicsDevice.Viewport.Width - size.X - 15;
		size.Y = 15;
		_batch.DrawString(
			font18,
			dbgText,
			size,
			Color.White,
			effect: FontSystemEffect.Stroked,
			effectAmount: 1
		);

		var rotatingText = "Hello from TinyEcs!";
		var font32 = _fontSystem.GetFont(32);
		size = font32.MeasureString(rotatingText);
		size.X = _batch.GraphicsDevice.Viewport.Width / 2f - size.X / 2f;
		size.Y = _batch.GraphicsDevice.Viewport.Height / 2f - size.Y / 2f;
		_batch.DrawString(
			font32,
			rotatingText,
			size,
			Color.Yellow,
			effect: FontSystemEffect.Stroked,
			effectAmount: 1
		);
	}
}

interface IUpdateSystem<T>
{
	void Update(in T data);
}
