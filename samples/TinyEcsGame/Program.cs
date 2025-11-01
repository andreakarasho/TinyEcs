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
using TinyEcs.UI.Easing;

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

// Core UI system (Flexbox, pointer input, drag/drop, scrolling)
app.AddPlugin(new TinyEcsUiPlugin { InputStage = Stage.PostUpdate });

// UI widgets (buttons, scrollbars, scrollviews)
app.AddPlugin(new TinyEcsUiWidgetsPlugin());

// Raylib-specific UI plugins (rendering and pointer input adapter)
app.AddPlugin(new TinyEcsGame.RaylibFlexboxUiPlugin { RenderingStage = gameRoot.RenderingStage });
app.AddPlugin(new TinyEcsGame.RaylibPointerInputAdapter { InputStage = Stage.PostUpdate });

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
.Build();

// Hierarchical UI demo with root panel and child buttons
app.AddSystem((Commands commands) => CreateButtonPanel(commands))
	.InStage(Stage.Startup)
	.Label("ui:button-panel:spawn")
	.Build();

// Phase 2 Demo: Animated button color transitions using AnimatedInteractionState
// Simplified: Lerp directly from base color to target based on progress
app.AddSystem((
	Commands commands,
	Query<Data<AnimatedInteractionState, ButtonBaseColor, BackgroundColor>> buttons) =>
{
	foreach (var (entityId, animState, baseColor, bgColor) in buttons)
	{
		ref readonly var anim = ref animState.Ref;
		ref readonly var originalColor = ref baseColor.Ref;

		// Calculate target color based on current interaction state
		var targetColor = anim.CurrentState switch
		{
			FluxInteractionState.PointerEnter => new Vector4(
				Math.Min(originalColor.Color.X * 1.3f, 1f),
				Math.Min(originalColor.Color.Y * 1.3f, 1f),
				Math.Min(originalColor.Color.Z * 1.3f, 1f),
				1f
			),
			FluxInteractionState.Pressed => new Vector4(
				originalColor.Color.X * 0.7f,
				originalColor.Color.Y * 0.7f,
				originalColor.Color.Z * 0.7f,
				1f
			),
			FluxInteractionState.Released => new Vector4(
				Math.Min(originalColor.Color.X * 1.5f, 1f),
				Math.Min(originalColor.Color.Y * 1.5f, 1f),
				Math.Min(originalColor.Color.Z * 1.5f, 1f),
				1f
			),
			_ => originalColor.Color
		};

		// Interpolate directly from base color to target using animation progress
		var t = anim.Progress.Value; // 0-1 based on easing function
		var newColor = originalColor.Color.Lerp(targetColor, t);

		// Update color with smooth animation
		if (originalColor.Color != newColor)
			commands.Entity(entityId.Ref).Insert(new BackgroundColor(newColor));
	}
})
.InStage(Stage.Update)
.Label("ui:button-animated-colors")
// .After("animated-interaction:update-progress")
.Build();

// Nested scroll container demo
app.AddSystem((Commands commands) => CreateNestedScrollPanel(commands))
	.InStage(Stage.Startup)
	.Label("ui:nested-scroll:spawn")
	.Build();

// Checkbox demo
app.AddSystem((Commands commands) => CreateCheckboxDemo(commands))
	.InStage(Stage.Startup)
	.Label("ui:checkbox:spawn")
	.Build();

// Handle checkbox changes
app.AddObserver((On<CheckboxChanged> trigger, Commands commands, Query<Data<UiText>> qTexts) =>
{
	Console.WriteLine($"[Checkbox] Checkbox {trigger.EntityId} changed to: {trigger.Event.Checked}");

	// Update text label if there's one associated
	if (qTexts.Contains(trigger.EntityId))
	{
		var (_, text) = qTexts.Get(trigger.EntityId);
		var currentText = text.Ref.Value;
		// Toggle between checked/unchecked text
		var newText = trigger.Event.Checked ? currentText.Replace("[ ]", "[X]") : currentText.Replace("[X]", "[ ]");
		commands.Entity(trigger.EntityId).Insert(new UiText(newText));
	}
});

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
	// Create button panel using ScrollView widget
	var (scrollViewId, contentId) = ScrollViewHelpers.CreateScrollView(
		commands,
		enableVertical: true,
		enableHorizontal: false,
		width: FlexValue.Points(400f),
		height: FlexValue.Points(500f),
		scrollbarWidth: 12f
	);

	// Style the scroll view root
	commands.Entity(scrollViewId)
		.Insert(new UiNode
		{
			FlexDirection = FlexDirection.Column,
			Width = FlexValue.Points(400f),
			Height = FlexValue.Points(500f),
			MarginTop = FlexValue.Auto(),
			MarginBottom = FlexValue.Auto(),
			MarginLeft = FlexValue.Auto(),
			MarginRight = FlexValue.Auto(),
		})
		.Insert(BackgroundColor.FromRgba(40, 40, 50, 255))
		.Insert(BorderColor.FromRgba(100, 100, 120, 255))
		.Insert(new BorderRadius(12f))
		.Insert(new Draggable())
		.Insert(new Interactive(focusable: false));

	// Style the content container
	commands.Entity(contentId).Insert(new UiNode
	{
		Width = FlexValue.Auto(),
		Height = FlexValue.Auto(),
		MinWidth = FlexValue.Percent(100f),
		MinHeight = FlexValue.Percent(100f),
		Display = Display.Flex,
		FlexDirection = FlexDirection.Column,
		JustifyContent = Justify.FlexStart,
		AlignItems = Align.Center,
		PaddingTop = FlexValue.Points(20f),
		PaddingBottom = FlexValue.Points(20f),
		PaddingLeft = FlexValue.Points(20f),
		PaddingRight = FlexValue.Points(32f) // Extra padding for scrollbar
	});

	Console.WriteLine($"[UI] Created button panel ScrollView {scrollViewId}, content: {contentId}");

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
			.Insert(new TextStyle(fontSize: 24, horizontalAlign: TextAlign.Center, verticalAlign: TextVerticalAlign.Middle))
			.Insert(new Button())
			.Insert(new Interactive(focusable: true))
			// Phase 1: Add FluxInteraction components for state tracking
			.Insert(new InteractionState())
			.Insert(new FluxInteraction())
			.Insert(new PrevInteraction())
			.Insert(new FluxInteractionStopwatch())
			// Phase 2: Add animation components for smooth color transitions
			.Insert(new AnimatedInteractionState()
				.WithBase(0.15f, Ease.OutQuad)       // Default: 150ms OutQuad
				.WithHover(0.2f, Ease.OutCubic)      // Hover: 200ms OutCubic
				.WithPress(0.05f, Ease.InQuad, 0.1f, Ease.OutQuad)) // Press: 50ms in, 100ms out
			.Insert(new ButtonBaseColor { Color = color })  // Store original color
			.Insert(new ButtonColorTransition
			{
				StartColor = color,
				TargetColor = color,
				LastState = FluxInteractionState.None
			}); // Track color transitions

		var buttonId = button.Id;

		// Attach button as child of scroll view content
		commands.Entity(contentId).AddChild(button.Id);

		Console.WriteLine($"[UI] Created button '{name}' ({buttonId}) as child of content {contentId}");
	}

	Console.WriteLine($"[UI] Button panel hierarchy created - {buttonColors.Length} buttons attached to ScrollView");
}

static void CreateNestedScrollPanel(Commands commands)
{
	// return;
	// Create outer scroll view container (verticalok  scrolling with integrated scrollbar)
	var (outerScrollViewId, outerContentId) = ScrollViewHelpers.CreateScrollView(
		commands,
		enableVertical: true,
		enableHorizontal: false,
		width: FlexValue.Points(450f),
		height: FlexValue.Points(400f),
		scrollbarWidth: 12f
	);

	// Style the outer scroll view root
	commands.Entity(outerScrollViewId)
		.Insert(new UiNode
		{
			FlexDirection = FlexDirection.Column,
			Width = FlexValue.Points(450f),
			Height = FlexValue.Points(400f),
			MarginTop = FlexValue.Auto(),
			MarginBottom = FlexValue.Auto(),
			MarginLeft = FlexValue.Auto(),
			MarginRight = FlexValue.Auto(),
		})
		.Insert(BackgroundColor.FromRgba(30, 50, 40, 255))
		.Insert(BorderColor.FromRgba(80, 120, 100, 255))
		.Insert(new BorderRadius(12f))
		.Insert(new Draggable())
		.Insert(new Interactive(focusable: false));

	Console.WriteLine($"[UI] Created outer ScrollView {outerScrollViewId}, content: {outerContentId}");

	// Add padding to content
	commands.Entity(outerContentId).Insert(new UiNode
	{
		Width = FlexValue.Auto(),
		Height = FlexValue.Auto(),
		MinWidth = FlexValue.Percent(100f),
		MinHeight = FlexValue.Percent(100f),
		Display = Display.Flex,
		FlexDirection = FlexDirection.Column,
		PaddingTop = FlexValue.Points(20f),
		PaddingBottom = FlexValue.Points(20f),
		PaddingLeft = FlexValue.Points(20f),
		PaddingRight = FlexValue.Points(32f) // Extra padding for scrollbar
	});

	// Add title text to content
	var titleText = commands.Spawn()
		.Insert(new UiNode
		{
			Width = FlexValue.Auto(),
			Height = FlexValue.Points(30f),
			MarginBottom = FlexValue.Points(10f),
			AlignSelf = Align.Center, // Center this element within parent
			JustifyContent = Justify.Center,
			AlignItems = Align.Center,
		})
		.Insert(new UiText("Nested Scroll Demo (ScrollView Widget)"))
		.Insert(new TextStyle(fontSize: 22f, horizontalAlign: TextAlign.Center, verticalAlign: TextVerticalAlign.Middle))
;

	commands.Entity(outerContentId).AddChild(titleText.Id);

	// Create inner scrollable panels (horizontal scrolling) using ScrollView
	for (int i = 0; i < 5; i++)
	{
		var (innerScrollViewId, innerContentId) = ScrollViewHelpers.CreateScrollView(
			commands,
			enableVertical: false,
			enableHorizontal: true,
			width: FlexValue.Points(380f),
			height: FlexValue.Points(120f),
			scrollbarWidth: 8f
		);

		// Style the inner scroll view root
		commands.Entity(innerScrollViewId)
			.Insert(new UiNode
			{
				FlexDirection = FlexDirection.Row,
				Width = FlexValue.Points(380f),
				Height = FlexValue.Points(120f),
				MarginLeft = FlexValue.Auto(),
				MarginRight = FlexValue.Auto(),
				MarginBottom = FlexValue.Points(15f),
			})
			.Insert(BackgroundColor.FromRgba(50, 70, 60, 255))
			.Insert(BorderColor.FromRgba(100, 140, 120, 255))
			.Insert(new BorderRadius(8f))
			.Insert(new Interactive(focusable: true));

		// Style the inner content container
		commands.Entity(innerContentId).Insert(new UiNode
		{
			Width = FlexValue.Auto(),
			Height = FlexValue.Auto(),
			MinWidth = FlexValue.Percent(100f),
			MinHeight = FlexValue.Percent(100f),
			Display = Display.Flex,
			FlexDirection = FlexDirection.Row,
			PaddingTop = FlexValue.Points(10f),
			PaddingBottom = FlexValue.Points(18f), // Extra padding for horizontal scrollbar
			PaddingLeft = FlexValue.Points(10f),
			PaddingRight = FlexValue.Points(10f)
		});

		Console.WriteLine($"[UI] Created inner ScrollView {innerScrollViewId}, content: {innerContentId} (#{i})");

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
				.Insert(new TextStyle(horizontalAlign: TextAlign.Center, verticalAlign: TextVerticalAlign.Middle))
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

			// Add box to inner scroll view content
			commands.Entity(innerContentId).AddChild(box.Id);
		}

		// Add inner scroll view to outer scroll view content
		commands.Entity(outerContentId).AddChild(innerScrollViewId);
	}

	// Note: Outer panel vertical scrollbar is now integrated in ScrollView widget!
	Console.WriteLine($"[UI] Nested scroll panel hierarchy created - 5 inner panels with 8 boxes each + scrollbars");
}

static void CreateCheckboxDemo(Commands commands)
{
	// Create a panel to hold checkboxes
	var panelId = commands.Spawn()
		.Insert(new UiNode
		{
			PositionType = PositionType.Absolute,
			FlexDirection = FlexDirection.Column,
			Width = FlexValue.Points(300f),
			Height = FlexValue.Auto(),
			Top = FlexValue.Points(100f),
			Left = FlexValue.Points(200f),
			PaddingTop = FlexValue.Points(20f),
			PaddingBottom = FlexValue.Points(20f),
			PaddingLeft = FlexValue.Points(20f),
			PaddingRight = FlexValue.Points(20f),
		})
		.Insert(BackgroundColor.FromRgba(60, 50, 70, 255))
		.Insert(BorderColor.FromRgba(120, 100, 140, 255))
		.Insert(new BorderRadius(10f))
		.Id;

	Console.WriteLine($"[UI] Created checkbox panel {panelId}");

	// Create checkboxes with labels
	var checkboxOptions = new[]
	{
		"Enable Sound",
		"Show FPS",
		"Fullscreen Mode",
		"Enable Particles"
	};

	foreach (var option in checkboxOptions)
	{
		// Create checkbox container (row layout with checkbox + label)
		var rowId = commands.Spawn()
			.Insert(new UiNode
			{
				FlexDirection = FlexDirection.Row,
				Width = FlexValue.Percent(100f),
				Height = FlexValue.Auto(),
				AlignItems = Align.Center,
				MarginBottom = FlexValue.Points(15f),
			})
			.Insert(BorderColor.FromRgba(255, 0, 0, 255))
			.Id;

		// Create checkbox box (the clickable square)
		var checkboxId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(24f),
				Height = FlexValue.Points(24f),
				JustifyContent = Justify.Center,
				AlignItems = Align.Center,
				BorderTop = FlexValue.Points(2f),
				BorderRight = FlexValue.Points(2f),
				BorderBottom = FlexValue.Points(2f),
				BorderLeft = FlexValue.Points(2f),
			})
			.Insert(BackgroundColor.FromRgba(240, 240, 240, 255))
			.Insert(BorderColor.FromRgba(100, 100, 100, 255))
			.Insert(new BorderRadius(4f))
			.Insert(new Interactive(focusable: true))
			// Add FluxInteraction components for checkbox interaction
			.Insert(new InteractionState())
			.Insert(new FluxInteraction())
			.Insert(new PrevInteraction())
			.Insert(new FluxInteractionStopwatch())
			.Id;

		// Create checkmark visual (green check)
		var checkmarkId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(16f),
				Height = FlexValue.Points(16f),
				Display = Flexbox.Display.None, // Hidden by default
			})
			.Insert(BackgroundColor.FromRgba(0, 200, 0, 255)) // Green checkmark
			.Insert(new BorderRadius(2f))
			.Id;

		// Add checkmark as child of checkbox
		commands.Entity(checkboxId).AddChild(checkmarkId);

		// Link checkmark to checkbox and set initial state
		commands.Entity(checkboxId).Insert(new Checkbox
		{
			Checked = false,
			CheckmarkEntity = checkmarkId
		});

		// Create label text with automatic text measurement
		var labelId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Auto(), // Auto width - measured from text
				Height = FlexValue.Auto(),  // Auto height - measured from text
				MarginLeft = FlexValue.Points(10f),  // Space between checkbox and label
				AlignSelf = Align.Center,
				BorderTop = FlexValue.Points(2f),
				BorderRight = FlexValue.Points(2f),
				BorderBottom = FlexValue.Points(2f),
				BorderLeft = FlexValue.Points(2f),
			})
			.Insert(BorderColor.FromRgba(0, 255, 0, 255))
			.Insert(new UiText(option))
			.Insert(new TextStyle(fontSize: 16f, color: new Vector4(1f, 1f, 1f, 1f)))
			.Id;

		// Add checkbox and label to row (checkbox first = left, label second = right)
		// Use explicit index to ensure correct order
		commands.AddChild(rowId, checkboxId);
		commands.AddChild(rowId, labelId);

		// Add row to panel
		commands.Entity(panelId).AddChild(rowId);
	}

	Console.WriteLine($"[UI] Created {checkboxOptions.Length} checkboxes");
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

		// Update TimeResource FIRST (must run before DeltaTime update)
		app.AddSystem((ResMut<TimeResource> time) =>
		{
			var t = time.Value;
			t.Frame = Raylib.GetFrameTime();
			t.Total += t.Frame;
		})
		.InStage(Stage.First)
		.Label("raylib:update-time")
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		// Update DeltaTime for UI systems (Phase 2 requirement)
		// MUST run in Stage.First, AFTER TimeResource update, BEFORE Stage.PreUpdate where stopwatches are ticked!
		app.AddSystem((ResMut<DeltaTime> deltaTime, Res<TimeResource> time) =>
		{
			deltaTime.Value.Seconds = time.Value.Frame;
		})
		.InStage(Stage.First)
		.After("raylib:update-time")
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
/// Component that stores the original base color of a button for FluxInteraction color changes.
/// Phase 1 demo component.
/// </summary>
struct ButtonBaseColor
{
	public Vector4 Color;
}

/// <summary>
/// Tracks the animation state for color transitions.
/// Stores the start and target colors for smooth interpolation.
/// </summary>
struct ButtonColorTransition
{
	public Vector4 StartColor;
	public Vector4 TargetColor;
	public FluxInteractionState LastState;
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



