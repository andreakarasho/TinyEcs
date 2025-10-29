using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using Flexbox;
using TinyEcs.UI.Bevy;
using TinyEcs.UI;

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

// Add Clay UI integration with REACTIVE SYSTEM (DISABLED - Using Flexbox UI instead)
// app.AddPlugin(new RaylibClayUiPlugin
// {
// 	RenderingStage = gameRoot.RenderingStage
// });

// Flexbox UI: core + Raylib bridge
app.AddPlugin(new FlexboxUiPlugin());
app.AddPlugin(new TinyEcsGame.RaylibFlexboxUiPlugin { RenderingStage = gameRoot.RenderingStage });

// UI Stack for z-order management
app.AddPlugin(new UiStackPlugin());

// Pointer input: renderer-agnostic hit testing + Raylib adapter
// Must run in PostUpdate after UI stack is built
app.AddPlugin(new UiPointerInputPlugin { InputStage = Stage.PostUpdate });
app.AddPlugin(new TinyEcsGame.RaylibPointerInputAdapter { InputStage = Stage.PostUpdate });

// Scroll input for scrollable containers
app.AddPlugin(new ScrollPlugin());

// Drag input for draggable UI elements
app.AddPlugin(new DragPlugin());

// Button widget functionality
app.AddPlugin(new ButtonPlugin());

// Keep Flexbox container size in sync with window
app.AddSystem((ResMut<WindowSize> window, ResMut<FlexboxUiState> ui) =>
{
	window.Value.Value.X = Raylib.GetRenderWidth();
	window.Value.Value.Y = Raylib.GetRenderHeight();
	ref var st = ref ui.Value;
	var size = window.Value.Value;
	st.CalculateLayout(size.X, size.Y);
})
.InStage(Stage.PreUpdate)
.Label("ui:flexbox:calculate-layout")
.After("flexbox:sync_hierarchy")  // Run AFTER syncing UiNode changes!
.Build();

// Hierarchical UI demo with root panel and child buttons
app.AddSystem((Commands commands) => CreateButtonPanel(commands))
	.InStage(Stage.Startup)
	.Label("ui:button-panel:spawn")
	.Build();

// Nested scroll container demo
app.AddSystem((Commands commands) => CreateNestedScrollPanel(commands))
	.InStage(Stage.Startup)
	.Label("ui:nested-scroll:spawn")
	.Build();

// Handle button activation
app.AddObserver((On<Activate> trigger, Commands commands, Query<Data<UiText>, With<Button>> qButtonTexts) =>
{
	if (qButtonTexts.Contains(trigger.EntityId))
	{
		var (_, text) = qButtonTexts.Get(trigger.EntityId);
		Console.WriteLine($"[Button] '{text.Ref.Value}' (Entity {trigger.EntityId}) was clicked!");
	}
	else
	{
		Console.WriteLine($"[Button] Button {trigger.EntityId} activated!");
	}
});


app.RunStartup();

while (!Raylib.WindowShouldClose())
{
	app.Update();
}

Raylib.CloseWindow();

static void CreateButtonPanel(Commands commands)
{
	// Create a root panel container (similar to Bevy's root UI node)
	var panel = commands.Spawn()
		.Insert(new UiNode
		{
			FlexDirection = FlexDirection.Column, // Vertical layout for buttons stacked vertically
			JustifyContent = Justify.FlexStart, // Start from top (not centered) for proper scrolling
			AlignItems = Align.Center, // Center buttons horizontally
			Width = FlexValue.Points(400f), // Wider to fit buttons horizontally
			Height = FlexValue.Points(500f), // Shorter since buttons are in a row
											 // Center the panel on screen
			MarginTop = FlexValue.Auto(),
			MarginBottom = FlexValue.Auto(),
			MarginLeft = FlexValue.Auto(),
			MarginRight = FlexValue.Auto(),
			PaddingTop = FlexValue.Points(20f),
			PaddingBottom = FlexValue.Points(20f),
			PaddingLeft = FlexValue.Points(20f),
			PaddingRight = FlexValue.Points(20f),
		})
		.Insert(BackgroundColor.FromRgba(40, 40, 50, 255))
		.Insert(BorderColor.FromRgba(100, 100, 120, 255))
		.Insert(new BorderRadius(12f))
		.Insert(new Scrollable(vertical: true, horizontal: false, scrollSpeed: 15f)) // Make panel scrollable horizontally
		.Insert(new Draggable()) // Make panel draggable
		.Insert(new Interactive(focusable: false)); // Enable pointer events for dragging

	var panelId = panel.Id;
	Console.WriteLine($"[UI] Created scrollable panel {panelId}");

	// Create child buttons attached to the panel (more than fit in the panel to enable scrolling)
	var buttonColors = new[]
	{
		(Name: "Red Button", Color: new Vector4(0.8f, 0.2f, 0.2f, 1f)),
		(Name: "Green Button", Color: new Vector4(0.2f, 0.8f, 0.2f, 1f)),
		(Name: "Blue Button", Color: new Vector4(0.2f, 0.2f, 0.8f, 1f)),
		(Name: "Yellow Button", Color: new Vector4(0.9f, 0.9f, 0.2f, 1f)),
		(Name: "Orange Button", Color: new Vector4(1f, 0.5f, 0f, 1f)),
		(Name: "Purple Button", Color: new Vector4(0.5f, 0f, 0.5f, 1f)),
		(Name: "Cyan Button", Color: new Vector4(0f, 0.8f, 0.8f, 1f)),
		(Name: "Pink Button", Color: new Vector4(1f, 0.4f, 0.7f, 1f))
	};

	foreach (var (name, color) in buttonColors)
	{
		var button = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(300f), // Narrower for row layout
				Height = FlexValue.Points(60f),
				JustifyContent = Justify.Center,
				AlignItems = Align.Center,
				MarginLeft = FlexValue.Points(10f), // Horizontal margins for row layout
				MarginRight = FlexValue.Points(10f),
				MarginBottom = FlexValue.Points(10f),
				MarginTop = FlexValue.Points(10f)
			})
			.Insert(new BackgroundColor(color))
			.Insert(BorderColor.FromRgba(255, 255, 255, 200))
			.Insert(new BorderRadius(8f))
			.Insert(new UiText(name))
			.Insert(new Button())
			.Insert(new Interactive(focusable: true))
			.Observe((On<UiPointerTrigger> trigger, Commands cmd) =>
			{
				if (trigger.Event.Event.Type == UiPointerEventType.PointerEnter)
				{
					// Brighten on hover
					cmd.Entity(trigger.EntityId).Insert(new BackgroundColor(
						new Vector4(
							Math.Min(color.X * 1.3f, 1f),
							Math.Min(color.Y * 1.3f, 1f),
							Math.Min(color.Z * 1.3f, 1f),
							1f)));
				}
				else if (trigger.Event.Event.Type == UiPointerEventType.PointerExit)
				{
					// Restore original color
					cmd.Entity(trigger.EntityId).Insert(new BackgroundColor(color));
				}
			});

		var buttonId = button.Id;

		// Attach button as child of panel (correct Bevy-style API)
		panel.AddChild(button);

		Console.WriteLine($"[UI] Created button '{name}' ({buttonId}) as child of panel {panelId}");
	}

	Console.WriteLine($"[UI] Button panel hierarchy created - {buttonColors.Length} buttons attached to panel");
}

static void CreateNestedScrollPanel(Commands commands)
{
	// Create outer scrollable panel (vertical scrolling)
	var outerPanel = commands.Spawn()
		.Insert(new UiNode
		{
			FlexDirection = FlexDirection.Column,
			Width = FlexValue.Points(450f),
			Height = FlexValue.Points(400f),
			// Position on the right side
			// MarginTop = FlexValue.Points(50f),
			// MarginLeft = FlexValue.Auto(),
			// MarginRight = FlexValue.Points(50f),
			// PaddingTop = FlexValue.Points(20f),
			// PaddingBottom = FlexValue.Points(20f),
			// PaddingLeft = FlexValue.Points(20f),
			// PaddingRight = FlexValue.Points(20f),

			MarginTop = FlexValue.Auto(),
			MarginBottom = FlexValue.Auto(),
			MarginLeft = FlexValue.Auto(),
			MarginRight = FlexValue.Auto(),
			PaddingTop = FlexValue.Points(20f),
			PaddingBottom = FlexValue.Points(20f),
			PaddingLeft = FlexValue.Points(20f),
			PaddingRight = FlexValue.Points(20f),
		})
		.Insert(BackgroundColor.FromRgba(30, 50, 40, 255))
		.Insert(BorderColor.FromRgba(80, 120, 100, 255))
		.Insert(new BorderRadius(12f))
		.Insert(new Scrollable(vertical: true, horizontal: false, scrollSpeed: 15f))
		.Insert(new Draggable())
		.Insert(new Interactive(focusable: false));

	var outerPanelId = outerPanel.Id;
	Console.WriteLine($"[UI] Created outer scrollable panel {outerPanelId}");

	// Add title text
	var titleText = commands.Spawn()
		.Insert(new UiNode
		{
			Width = FlexValue.Percent(100f),
			Height = FlexValue.Points(30f),
			MarginBottom = FlexValue.Points(10f),
			JustifyContent = Justify.Center,
			AlignItems = Align.Center,
		})
		.Insert(new UiText("Nested Scroll Demo"));

	outerPanel.AddChild(titleText);

	// Create inner scrollable panels (horizontal scrolling)
	for (int i = 0; i < 5; i++)
	{
		var innerPanel = commands.Spawn()
			.Insert(new UiNode
			{
				FlexDirection = FlexDirection.Row, // Horizontal layout
				Width = FlexValue.Points(380f),
				Height = FlexValue.Points(120f),

				MarginLeft = FlexValue.Auto(),
				MarginRight = FlexValue.Auto(),
				MarginBottom = FlexValue.Points(15f),
				PaddingTop = FlexValue.Points(10f),
				PaddingBottom = FlexValue.Points(10f),
				PaddingLeft = FlexValue.Points(10f),
				PaddingRight = FlexValue.Points(10f),
			})
			.Insert(BackgroundColor.FromRgba(50, 70, 60, 255))
			.Insert(BorderColor.FromRgba(100, 140, 120, 255))
			.Insert(new BorderRadius(8f))
			.Insert(new Scrollable(vertical: false, horizontal: true, scrollSpeed: 10f))
			.Insert(new Interactive(focusable: true));

		var innerPanelId = innerPanel.Id;
		Console.WriteLine($"[UI] Created inner scrollable panel {innerPanelId} (#{i})");

		// Add colored boxes to inner panel (more than fit to enable scrolling)
		var colors = new[]
		{
			new Vector4(0.9f, 0.3f, 0.3f, 1f), // Red
			new Vector4(0.3f, 0.9f, 0.3f, 1f), // Green
			new Vector4(0.3f, 0.3f, 0.9f, 1f), // Blue
			new Vector4(0.9f, 0.9f, 0.3f, 1f), // Yellow
			new Vector4(0.9f, 0.5f, 0.2f, 1f), // Orange
			new Vector4(0.7f, 0.3f, 0.9f, 1f), // Purple
			new Vector4(0.3f, 0.9f, 0.9f, 1f), // Cyan
			new Vector4(0.9f, 0.5f, 0.7f, 1f)  // Pink
		};

		foreach (var (color, index) in colors.Select((c, idx) => (c, idx)))
		{
			var box = commands.Spawn()
				.Insert(new UiNode
				{
					Width = FlexValue.Points(80f),
					Height = FlexValue.Points(80f),
					MarginLeft = FlexValue.Points(5f),
					MarginRight = FlexValue.Points(5f),
					JustifyContent = Justify.Center,
					AlignItems = Align.Center,
				})
				.Insert(new BackgroundColor(color))
				.Insert(BorderColor.FromRgba(255, 255, 255, 200))
				.Insert(new BorderRadius(6f))
				.Insert(new UiText($"{i}-{index}"))
				.Insert(new Interactive(focusable: true))
				.Observe((On<UiPointerTrigger> trigger, Commands cmd) =>
				{
					if (trigger.Event.Event.Type == UiPointerEventType.PointerEnter)
					{
						// Brighten on hover
						cmd.Entity(trigger.EntityId).Insert(new BackgroundColor(
							new Vector4(
								Math.Min(color.X * 1.2f, 1f),
								Math.Min(color.Y * 1.2f, 1f),
								Math.Min(color.Z * 1.2f, 1f),
								1f)));
					}
					else if (trigger.Event.Event.Type == UiPointerEventType.PointerExit)
					{
						// Restore original color
						cmd.Entity(trigger.EntityId).Insert(new BackgroundColor(color));
					}
				});

			innerPanel.AddChild(box);
		}

		outerPanel.AddChild(innerPanel);
	}

	Console.WriteLine($"[UI] Nested scroll panel hierarchy created - 5 inner panels with 8 boxes each");
}

class DebugLayoutState
{
	public bool Printed;
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



