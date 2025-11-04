using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Clay;
using Clay_cs;

namespace ClayRaylibSample;

/// <summary>
/// Sample demonstrating Clay UI rendering with Raylib.
/// </summary>
public static class Program
{
	private const int WINDOW_WIDTH = 1280;
	private const int WINDOW_HEIGHT = 720;

	public static void Main()
	{
		// Initialize Raylib
		Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
		Raylib.InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "Clay UI + Raylib Sample");
		Raylib.SetTargetFPS(-1);

		// Create ECS world and app
		using var world = new World();
		var app = new App(world, ThreadingMode.Single); // Use single-threaded for Raylib

		// Add Clay UI plugin
		app.AddClayUi(new ClayUiOptions
		{
			LayoutDimensions = new Clay_Dimensions(WINDOW_WIDTH, WINDOW_HEIGHT),
			ArenaSize = 1024 * 1024,  // 1MB
			MaxElementCount = 1024,
			EnableDebugMode = false,
			EnableCulling = true
		});

		// Add rendering plugin
		app.AddPlugin(new ClayRaylibRenderPlugin());

		// Create UI in startup
		app.AddSystem((Commands commands) => CreateUI(commands))
			.InStage(Stage.Startup)
			.Label("app:create-ui")
			.Build();

		// Run startup
		app.RunStartup();

		// Main loop
		while (!Raylib.WindowShouldClose())
		{
			// Update ECS (which will update Clay pointer state and run systems)
			app.Update();
		}

		Raylib.CloseWindow();
	}

	private static void CreateUI(Commands commands)
	{
		// Create root container
		var rootNode = ClayNode.Default;
		rootNode.Layout = new Clay_LayoutConfig
		{
			sizing = new Clay_Sizing(
				Clay_SizingAxis.Grow(),
				Clay_SizingAxis.Grow()
			),
			layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
			padding = Clay_Padding.All(32),
			childGap = 16,
			childAlignment = new Clay_ChildAlignment(
				Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
				Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
			)
		};
		rootNode.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = new Clay_Color(20, 25, 30, 255)
		};

		var root = commands.SpawnClayElement(rootNode);

		// Create title
		var title = commands.SpawnClayText("Clay UI with Raylib", new Clay_TextElementConfig
		{
			fontSize = 48,
			textColor = new Clay_Color(255, 255, 255, 255)
		});
		root.AddChild(title);

		// Create button container
		var buttonContainer = commands.SpawnClayElement(new ClayNode
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Grow(),
					Clay_SizingAxis.Fixed(0)
				),
				layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
				childGap = 16,
				childAlignment = new Clay_ChildAlignment(
					Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
					Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
				)
			}
		});
		root.AddChild(buttonContainer);

		// Create buttons
		CreateButton(commands, buttonContainer, "Button 1", new Clay_Color(70, 130, 180, 255));
		CreateButton(commands, buttonContainer, "Button 2", new Clay_Color(180, 70, 130, 255));
		CreateButton(commands, buttonContainer, "Button 3", new Clay_Color(130, 180, 70, 255));

		// Create description
		var description = commands.SpawnClayText(
			"Click the buttons to see interactions!",
			new Clay_TextElementConfig
			{
				fontSize = 24,
				textColor = new Clay_Color(200, 200, 200, 255)
			}
		);
		root.AddChild(description);
	}

	private static void CreateButton(Commands commands, EntityCommands parent, string text, Clay_Color color)
	{
		var buttonNode = ClayNode.Default;
		buttonNode.Layout = new Clay_LayoutConfig
		{
			sizing = new Clay_Sizing(
				Clay_SizingAxis.Fixed(200),
				Clay_SizingAxis.Fixed(60)
			),
			padding = Clay_Padding.All(8),
			childAlignment = new Clay_ChildAlignment(
				Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
				Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
			)
		};
		buttonNode.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = color
		};
		buttonNode.CornerRadius = Clay_CornerRadius.All(8);

		var button = commands.SpawnClayElement(buttonNode);
		parent.AddChild(button);

		// Add button text
		var buttonText = commands.SpawnClayText(text, new Clay_TextElementConfig
		{
			fontSize = 20,
			textColor = new Clay_Color(255, 255, 255, 255)
		});
		button.AddChild(buttonText);

		// Add click observer
		button.Observe<On<ClayPointerTrigger>>(trigger =>
		{
			var evt = trigger.Event.Event;
			if (evt.EventType == ClayPointerEventType.Click)
			{
				Console.WriteLine($"Button clicked: {text}");
			}
		});
	}
}

/// <summary>
/// Plugin for rendering Clay UI with Raylib.
/// </summary>
public struct ClayRaylibRenderPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddSystem((Res<ClayUiOptions> options) =>
		{
			options.Value.LayoutDimensions.width = Raylib.GetRenderWidth();
			options.Value.LayoutDimensions.height = Raylib.GetRenderHeight();
		})
		.InStage(Stage.First)
		.SingleThreaded()
		.Build();

		// System to update Clay pointer state from Raylib input
		app.AddSystem((ResMut<ClayPointerState> pointer) => UpdatePointerInput(pointer))
			.InStage(Stage.First)
			.Label("raylib:update-input")
			.SingleThreaded()
			.Build();

		// System to begin Raylib drawing
		app.AddSystem((World _) =>
		{
			Raylib.BeginDrawing();
			Raylib.ClearBackground(Raylib_cs.Color.Black);
		})
		.InStage(Stage.PostUpdate)
		.Label("raylib:begin-draw")
		.After("clay:interaction")
		.SingleThreaded()
		.Build();

		// System to render Clay UI
		app.AddSystem((Res<ClayUiState> state) => RenderClayUI(state))
			.InStage(Stage.PostUpdate)
			.Label("raylib:render-clay")
			.After("raylib:begin-draw")
			.SingleThreaded()
			.Build();

		// System to end Raylib drawing
		app.AddSystem((World _) =>
		{
			// Draw FPS
			Raylib.DrawFPS(10, 10);
			Raylib.EndDrawing();
		})
		.InStage(Stage.PostUpdate)
		.Label("raylib:end-draw")
		.After("raylib:render-clay")
		.SingleThreaded()
		.Build();
	}

	private static void UpdatePointerInput(ResMut<ClayPointerState> pointer)
	{
		var mousePos = Raylib.GetMousePosition();
		pointer.Value.Position = new Vector2(mousePos.X, mousePos.Y);

		var wasDown = pointer.Value.PrimaryDown;
		var isDown = Raylib.IsMouseButtonDown(MouseButton.Left);

		pointer.Value.PrimaryDown = isDown;
		pointer.Value.PrimaryPressed = isDown && !wasDown;
		pointer.Value.PrimaryReleased = !isDown && wasDown;

		var mouseWheel = Raylib.GetMouseWheelMove();
		pointer.Value.ScrollDelta = new Vector2(0, mouseWheel * 20f);

		pointer.Value.DeltaTime = Raylib.GetFrameTime();
	}

	private static unsafe void RenderClayUI(Res<ClayUiState> state)
	{
		var commands = state.Value.RenderCommands;

		foreach (ref readonly var cmd in commands)
		{
			switch (cmd.commandType)
			{
				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
					RenderRectangle(cmd);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER:
					RenderBorder(cmd);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
					RenderText(cmd);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
					// Start scissor test (clipping)
					var scissorBox = cmd.boundingBox;
					Raylib.BeginScissorMode(
						(int)scissorBox.x,
						(int)scissorBox.y,
						(int)scissorBox.width,
						(int)scissorBox.height
					);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
					Raylib.EndScissorMode();
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_CUSTOM:
					// Not implemented in this sample
					break;
			}
		}
	}

	private static unsafe void RenderRectangle(Clay_RenderCommand cmd)
	{
		var bounds = cmd.boundingBox;
		var rect = cmd.renderData.rectangle;
		var clayColor = rect.backgroundColor;
		var color = new Raylib_cs.Color(
			(byte)Math.Clamp(clayColor.r, 0, 255),
			(byte)Math.Clamp(clayColor.g, 0, 255),
			(byte)Math.Clamp(clayColor.b, 0, 255),
			(byte)Math.Clamp(clayColor.a, 0, 255)
		);

		// Check if we have corner radius
		if (cmd.renderData.rectangle.cornerRadius.topLeft > 0)
		{
			var radius = cmd.renderData.rectangle.cornerRadius.topLeft;
			Raylib.DrawRectangleRounded(
				new Rectangle(bounds.x, bounds.y, bounds.width, bounds.height),
				radius / Math.Min(bounds.width, bounds.height),
				8,  // segments
				color
			);
		}
		else
		{
			Raylib.DrawRectangle(
				(int)bounds.x,
				(int)bounds.y,
				(int)bounds.width,
				(int)bounds.height,
				color
			);
		}
	}

	private static unsafe void RenderBorder(Clay_RenderCommand cmd)
	{
		var bounds = cmd.boundingBox;
		var border = cmd.renderData.border;
		var clayColor = border.color;
		var color = new Raylib_cs.Color(
			(byte)Math.Clamp(clayColor.r, 0, 255),
			(byte)Math.Clamp(clayColor.g, 0, 255),
			(byte)Math.Clamp(clayColor.b, 0, 255),
			(byte)Math.Clamp(clayColor.a, 0, 255)
		);

		// Draw border rectangles for each side
		if (border.width.left > 0)
		{
			Raylib.DrawRectangle(
				(int)bounds.x,
				(int)bounds.y,
				(int)border.width.left,
				(int)bounds.height,
				color
			);
		}

		if (border.width.right > 0)
		{
			Raylib.DrawRectangle(
				(int)(bounds.x + bounds.width - border.width.right),
				(int)bounds.y,
				(int)border.width.right,
				(int)bounds.height,
				color
			);
		}

		if (border.width.top > 0)
		{
			Raylib.DrawRectangle(
				(int)bounds.x,
				(int)bounds.y,
				(int)bounds.width,
				(int)border.width.top,
				color
			);
		}

		if (border.width.bottom > 0)
		{
			Raylib.DrawRectangle(
				(int)bounds.x,
				(int)(bounds.y + bounds.height - border.width.bottom),
				(int)bounds.width,
				(int)border.width.bottom,
				color
			);
		}
	}

	private static unsafe void RenderText(Clay_RenderCommand cmd)
	{
		var bounds = cmd.boundingBox;
		var textData = cmd.renderData.text;
		var clayColor = textData.textColor;
		var color = new Raylib_cs.Color(
			(byte)Math.Clamp(clayColor.r, 0, 255),
			(byte)Math.Clamp(clayColor.g, 0, 255),
			(byte)Math.Clamp(clayColor.b, 0, 255),
			(byte)Math.Clamp(clayColor.a, 0, 255)
		);

		// Extract text string from Clay string slice
		var textPtr = textData.stringContents.chars;
		var textLength = textData.stringContents.length;
		var text = new string((sbyte*)textPtr, 0, textLength, System.Text.Encoding.UTF8);

		// Draw text at position
		Raylib.DrawText(
			text,
			(int)bounds.x,
			(int)bounds.y,
			textData.fontSize,
			color
		);
	}
}
