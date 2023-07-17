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
    private Texture2D? _texture;
    private Ecs _ecs;

    public static SpriteBatch Batch { get; private set; }
    public static Texture2D Texture { get; private set; }


    public TinyGame()
    {
        _graphicsDeviceManager = new GraphicsDeviceManager(this);

        IsFixedTimeStep = false;
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();

        Batch = _spriteBatch = new SpriteBatch(GraphicsDevice);

        _graphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
        _graphicsDeviceManager.PreferredBackBufferWidth = WINDOW_WIDTH;
        _graphicsDeviceManager.PreferredBackBufferHeight = WINDOW_HEIGHT;
        _graphicsDeviceManager.ApplyChanges();

        _ecs = new Ecs();
        
        var qry = _ecs.Query().With<Position>().With<Velocity>().With<Rotation>();
        _ecs.AddStartupSystem(&SpawnEntities);
        _ecs.AddSystem(&MoveSystem)
            .SetQuery(qry.ID);
        _ecs.AddSystem(&CheckBorderSystem)
            .SetQuery(qry.ID);

        _ecs.AddSystem(&BeginRender);
        _ecs.AddSystem(&Render)
            .SetQuery(_ecs.Query().With<Position>().With<Rotation>().With<Sprite>().ID);
        _ecs.AddSystem(&EndRender);

        _ecs.AddSystem(&PrintMessage)
            .SetTick(1f);
    }

    protected override void LoadContent()
    {
        // Texture = _texture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
        // _texture.SetData(new Color[] { Color.White });

        Texture = _texture = Texture2D.FromFile(GraphicsDevice, "Content/pepe.png");
    }

    protected override void Update(GameTime gameTime)
    {
        _ecs!.Step((float) gameTime.ElapsedGameTime.TotalSeconds);       

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
    }

    static void PrintMessage(Commands commands, ref EntityIterator it)
    {
        Console.WriteLine("print!");
    }

    static void SpawnEntities(Commands commands, ref EntityIterator it)
    {
        var rnd = new Random();

        for (ulong i = 0; i < ENTITIES_TO_SPAWN; ++i)
        {
            commands.Spawn()
                .Set(new Position() { 
                    Value = new Vector2(rnd.Next(0, WINDOW_WIDTH), rnd.Next(0, WINDOW_HEIGHT))
                })
                .Set(new Velocity() {
                    Value = new Vector2(rnd.Next(-VELOCITY, VELOCITY), rnd.Next(-VELOCITY, VELOCITY))
                })
                .Set(new Sprite() { 
                    Color = new Color(rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)),
                    Scale = rnd.NextSingle()
                })
                .Set(new Rotation() { 
                    Value = 0f, 
                    Acceleration = rnd.Next(5, 20) * (rnd.Next() % 2 == 0 ? -1 : 1)
                });
        }
    }

    static void MoveSystem(Commands commands, ref EntityIterator it)
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

    static void CheckBorderSystem(Commands commands, ref EntityIterator it)
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

    static void BeginRender(Commands commands, ref EntityIterator it)
    {
        Batch.GraphicsDevice.Clear(Color.Black);
        Batch.Begin();
    }

    static void EndRender(Commands commands, ref EntityIterator it)
    {
        Batch.End();
    }

    static void Render(Commands commands, ref EntityIterator it)
    {
        var batch = Batch;
        var texture = Texture;

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
                texture,
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
struct Sprite { public Color Color; public float Scale; }
struct Rotation { public float Value; public float Acceleration; }
