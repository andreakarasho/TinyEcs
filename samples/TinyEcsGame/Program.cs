using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcsGame;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;
using TinyEcs.UI.Flexbox;
using Flexbox;

using var world = new World();
var app = new App(world, ThreadingMode.Single);

app.AddPlugin(new RaylibPlugin
{
	Title = "TinyEcs with Clay UI Demo",
	WindowSize = new WindowSize { Value = new Vector2(1280, 720) },
	VSync = true
});

var gameRoot = new GameRootPlugin
{
	EntitiesToSpawn = 100,
	Velocity = 250
};
app.AddPlugin(gameRoot);

// Add Clay UI integration with REACTIVE SYSTEM
app.AddPlugin(new RaylibClayUiPlugin
{
	RenderingStage = gameRoot.RenderingStage
});

// Flexbox UI: core + Raylib bridge
app.AddPlugin(new FlexboxUiPlugin { AutoCreatePointerState = true, ContainerWidth = 1280f, ContainerHeight = 720f });
app.AddPlugin(new RaylibFlexboxUiPlugin { RenderingStage = gameRoot.RenderingStage });

// Keep Clay reactive UI; also add interaction update after Flexbox pointer for Flexbox widgets
app.AddSystem((EventReader<UiPointerEvent> events, Query<Data<Interaction, Interactive>> interactives, Local<HashSet<ulong>> touched, Commands commands) =>
	UiInteractionSystems.UpdateInteractionState(events, interactives, touched, commands))
	.InStage(Stage.PreUpdate)
	.Label("ui:flexbox:interaction")
	.After("ui:flexbox:pointer")
	.Build();

// Update Flexbox button visuals on Interaction changes
app.AddSystem((Query<Data<Interaction, FlexboxNode>, Filter<Changed<Interaction>>> buttons) =>
	FlexboxButtonSystems.UpdateButtonVisuals(buttons))
	.InStage(Stage.PreUpdate)
	.After("ui:flexbox:interaction")
	.Build();

// Keep Flexbox container size in sync with window
app.AddSystem((Res<WindowSize> window, ResMut<FlexboxUiState> ui) =>
{
	ref var st = ref ui.Value;
	var size = window.Value.Value;
	if (st.ContainerWidth != size.X || st.ContainerHeight != size.Y)
	{
		st.ContainerWidth = size.X;
		st.ContainerHeight = size.Y;
		st.MarkDirty();
	}
})
.InStage(Stage.PreUpdate)
.Label("ui:flexbox:update-container")
.Build();

// Enable reactive UI system (Bevy-style observers)
app.AddUiWidgets();        // Window ordering and slider/window drag handling
app.AddUiInteraction();    // Interaction state tracking
app.AddUiWidgetObservers(); // Reactive visual updates

// Add comprehensive UI demo (now with reactive interactions!)
app.AddPlugin(new UiDemoPlugin { ShowUI = true });

// Add a small Flexbox UI demo
app.AddSystem((Commands commands) => BuildFlexboxUI(commands))
	.InStage(Stage.Startup)
	.Label("ui:flexbox:demo:spawn")
	.Build();

app.RunStartup();

while (!Raylib.WindowShouldClose())
{
	app.Update();
}

Raylib.CloseWindow();

static void BuildFlexboxUI(Commands commands)
{
	// Root full-screen container
	var root = FlexboxPanelWidget.CreateColumn(commands, FlexboxPanelStyle.Transparent())
		.Insert(new FlexboxNode
		{
			FlexDirection = FlexDirection.Column,
			JustifyContent = Justify.FlexStart,
			AlignItems = Align.Stretch,
			Width = FlexValue.Percent(100f),
			Height = FlexValue.Percent(100f),
			BackgroundColor = new Vector4(0.08f, 0.08f, 0.12f, 1f),
		})
		.Id;

	// Card panel centered horizontally
	var card = FlexboxPanelWidget.CreateColumn(commands, FlexboxPanelStyle.Card())
		.Insert(new FlexboxNode
		{
			Width = FlexValue.Points(380f),
			Height = FlexValue.Auto(),
			MarginTop = 20f,
			MarginLeft = FlexValue.Auto(),
			MarginRight = FlexValue.Auto(),
			BorderColor = new Vector4(0.6f, 0.6f, 0.65f, 1f)
		})
		.Insert(new FlexboxNodeParent(root))
		.Id;

	FlexboxLabelWidget.CreateHeading2(commands, "Flexbox UI in TinyEcsGame")
		.Insert(new FlexboxNodeParent(card, index: 0))
		.Insert(new FlexboxNode { MarginBottom = 12f });

	// Row of buttons
	var row = FlexboxPanelWidget.CreateRow(commands, FlexboxPanelStyle.Transparent())
		.Insert(new FlexboxNode
		{
			FlexDirection = FlexDirection.Row,
			JustifyContent = Justify.SpaceBetween,
			AlignItems = Align.Center,
			Width = FlexValue.Percent(100f),
		})
		.Insert(new FlexboxNodeParent(card, index: 1))
		.Id;

	FlexboxButtonWidget.Create(commands, "Primary", (On<UiPointerTrigger> _) => Console.WriteLine("[Flexbox] Primary clicked"))
		.Insert(new FlexboxNodeParent(row, index: 0));

	FlexboxButtonWidget.Create(commands, "Secondary", (On<UiPointerTrigger> _) => Console.WriteLine("[Flexbox] Secondary clicked"))
		.Insert(new FlexboxNodeParent(row, index: 1));
	// Add a vertical scroll container with sample items
	var scroll = FlexboxScrollContainerWidget.CreateVertical(commands, new Vector2(320f, 160f), card)
		.Id;
	for (int i = 1; i <= 12; i++)
	{
		FlexboxLabelWidget.CreateBody(commands, $"Item {i}")
			.Insert(new FlexboxNodeParent(scroll, index: i - 1))
			.Insert(new FlexboxNode { MarginBottom = 8f });
	}
}

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
			flags |= ConfigFlags.ResizableWindow;
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

	public Stage RenderingStage { get; private set; } = default!;

	public void Build(App app)
	{
		// Create custom rendering stages
		var beginRendering = Stage.Custom("BeginRendering");
		RenderingStage = Stage.Custom("Rendering");
		var endRendering = Stage.Custom("EndRendering");

		// Add stages in order: Update -> BeginRendering -> Rendering -> EndRendering -> Last
		app.AddStage(beginRendering).After(Stage.Update);
		app.AddStage(RenderingStage).After(beginRendering);
		app.AddStage(endRendering).After(RenderingStage);

		app.AddPlugin(new GameplayPlugin
		{
			EntitiesToSpawn = EntitiesToSpawn,
			Velocity = Velocity
		});

		app.AddPlugin(new RenderingPlugin
		{
			BeginRenderingStage = beginRendering,
			RenderingStage = RenderingStage,
			EndRenderingStage = endRendering
		});
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
	public required Stage BeginRenderingStage { get; set; }
	public required Stage RenderingStage { get; set; }
	public required Stage EndRenderingStage { get; set; }

	public void Build(App app)
	{
		// BeginRendering Stage: BeginDrawing + ClearBackground
		app.AddSystem((World _) =>
		{
			Raylib.BeginDrawing();
			Raylib.ClearBackground(Color.Black);
		})
		.InStage(BeginRenderingStage)
		.Label("render:begin")
		.SingleThreaded()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		// Rendering Stage: Draw all game content
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
		.InStage(RenderingStage)
		.Label("render:sprites")
		.SingleThreaded()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		app.AddSystem((Query<Data<Position>> query, Res<TimeResource> time, Local<DebugOverlay> overlay) =>
		{
			var entityCount = query.Count();

			if (overlay.Value == null)
				overlay.Value = new DebugOverlay();

			var data = overlay.Value;
			data.Text = $"""
                [Debug]
                FPS: {Raylib.GetFPS()}
                Entities: {entityCount}
                DeltaTime: {time.Value.Frame:F4}
                """.Replace("\r", "\n");

			Raylib.DrawText(data.Text, 15, 15, 24, Color.White);
		})
		.InStage(RenderingStage)
		.Label("render:debug")
		.After("render:sprites")
		.SingleThreaded()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		// EndRendering Stage: EndDrawing
		app.AddSystem((World _) =>
		{
			Raylib.EndDrawing();
		})
		.InStage(EndRenderingStage)
		.Label("render:end")
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

