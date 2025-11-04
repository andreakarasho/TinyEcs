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

	public static unsafe void Main()
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
			EnableCulling = true,
			MeasureTextFunction = MeasureText,
			ErrorHandler = HandleClayError
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
				Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP
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

		// Create description
		var description = commands.SpawnClayText(
			"Scroll the list with mouse wheel!",
			new Clay_TextElementConfig
			{
				fontSize = 24,
				textColor = new Clay_Color(200, 200, 200, 255)
			}
		);
		root.AddChild(description);

		// Create scrollable container with scroll configuration
		var scrollContainerNode = ClayNode.Default;
		scrollContainerNode.Layout = new Clay_LayoutConfig
		{
			sizing = new Clay_Sizing(
				Clay_SizingAxis.Fixed(600),
				Clay_SizingAxis.Fixed(400)
			),
			layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
			padding = Clay_Padding.All(16),
			childGap = 8
		};
		scrollContainerNode.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = new Clay_Color(40, 45, 50, 255)
		};
		scrollContainerNode.CornerRadius = Clay_CornerRadius.All(8);
		// Enable vertical scrolling with clip (childOffset is automatically managed by Clay)
		scrollContainerNode.Clip = new Clay_ClipElementConfig
		{
			horizontal = false,
			vertical = true
		};

		// Enable scrolling with Clay's scroll container
		var scrollContainer = commands.SpawnClayElement(scrollContainerNode);
		root.AddChild(scrollContainer);

		// Create first nested scrollable container
		var scroll1Node = ClayNode.Default;
		scroll1Node.Layout = new Clay_LayoutConfig
		{
			sizing = new Clay_Sizing(
				Clay_SizingAxis.Grow(),
				Clay_SizingAxis.Fixed(200)
			),
			layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
			padding = Clay_Padding.All(8),
			childGap = 4
		};
		scroll1Node.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = new Clay_Color(60, 65, 70, 255)
		};
		scroll1Node.CornerRadius = Clay_CornerRadius.All(4);
		scroll1Node.Clip = new Clay_ClipElementConfig
		{
			horizontal = false,
			vertical = true
		};

		var scroll1 = commands.SpawnClayElement(scroll1Node);
		scrollContainer.AddChild(scroll1);

		// Add title for first scroll container
		var title1 = commands.SpawnClayText("Scroll Container 1", new Clay_TextElementConfig
		{
			fontSize = 16,
			textColor = new Clay_Color(255, 255, 255, 255)
		});
		scroll1.AddChild(title1);

		// Create 20 buttons in first scrollable area
		var colors = new[]
		{
			new Clay_Color(70, 130, 180, 255),
			new Clay_Color(180, 70, 130, 255),
			new Clay_Color(130, 180, 70, 255),
			new Clay_Color(180, 130, 70, 255),
			new Clay_Color(70, 180, 130, 255)
		};

		for (int i = 0; i < 20; i++)
		{
			var color = colors[i % colors.Length];
			CreateButton(commands, scroll1, $"S1 Button {i + 1}", color);
		}

		// Create second nested scrollable container
		var scroll2Node = ClayNode.Default;
		scroll2Node.Layout = new Clay_LayoutConfig
		{
			sizing = new Clay_Sizing(
				Clay_SizingAxis.Grow(),
				Clay_SizingAxis.Fixed(200)
			),
			layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
			padding = Clay_Padding.All(8),
			childGap = 4
		};
		scroll2Node.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = new Clay_Color(60, 65, 70, 255)
		};
		scroll2Node.CornerRadius = Clay_CornerRadius.All(4);
		scroll2Node.Clip = new Clay_ClipElementConfig
		{
			horizontal = false,
			vertical = true
		};

		var scroll2 = commands.SpawnClayElement(scroll2Node);
		scrollContainer.AddChild(scroll2);

		// Add title for second scroll container
		var title2 = commands.SpawnClayText("Scroll Container 2", new Clay_TextElementConfig
		{
			fontSize = 16,
			textColor = new Clay_Color(255, 255, 255, 255)
		});
		scroll2.AddChild(title2);

		// Create 20 buttons in second scrollable area
		for (int i = 0; i < 20; i++)
		{
			var color = colors[i % colors.Length];
			CreateButton(commands, scroll2, $"S2 Button {i + 1}", color);
		}
	}

	/// <summary>
	/// Measure text dimensions using Raylib's text measurement.
	/// This function is called by Clay during layout calculation.
	/// </summary>
	private static unsafe Clay_Dimensions MeasureText(Clay_StringSlice text, Clay_TextElementConfig* config, void* userData)
	{
		// Extract text string from Clay string slice
		var textPtr = text.chars;
		var textLength = text.length;
		var textString = new string((sbyte*)textPtr, 0, textLength, System.Text.Encoding.UTF8);

		// Measure text using Raylib
		var textSize = Raylib.MeasureTextEx(
			Raylib.GetFontDefault(),
			textString,
			config->fontSize,
			config->letterSpacing
		);

		return new Clay_Dimensions(textSize.X, textSize.Y);
	}

	/// <summary>
	/// Handle Clay errors by logging to console.
	/// This function is called when Clay encounters errors like capacity exceeded or invalid state.
	/// </summary>
	private static unsafe void HandleClayError(Clay_ErrorData errorData)
	{
		// Convert error text from Clay string slice to C# string
		var errorTextPtr = errorData.errorText.chars;
		var errorTextLength = errorData.errorText.length;
		var errorText = new string((sbyte*)errorTextPtr, 0, errorTextLength, System.Text.Encoding.UTF8);

		// Get error type name
		var errorType = errorData.errorType switch
		{
			Clay_ErrorType.CLAY_ERROR_TYPE_TEXT_MEASUREMENT_FUNCTION_NOT_PROVIDED => "TEXT_MEASUREMENT_FUNCTION_NOT_PROVIDED",
			Clay_ErrorType.CLAY_ERROR_TYPE_ARENA_CAPACITY_EXCEEDED => "ARENA_CAPACITY_EXCEEDED",
			Clay_ErrorType.CLAY_ERROR_TYPE_ELEMENTS_CAPACITY_EXCEEDED => "ELEMENTS_CAPACITY_EXCEEDED",
			Clay_ErrorType.CLAY_ERROR_TYPE_TEXT_MEASUREMENT_CAPACITY_EXCEEDED => "TEXT_MEASUREMENT_CAPACITY_EXCEEDED",
			Clay_ErrorType.CLAY_ERROR_TYPE_DUPLICATE_ID => "DUPLICATE_ID",
			Clay_ErrorType.CLAY_ERROR_TYPE_FLOATING_CONTAINER_PARENT_NOT_FOUND => "FLOATING_CONTAINER_PARENT_NOT_FOUND",
			Clay_ErrorType.CLAY_ERROR_TYPE_PERCENTAGE_OVER_1 => "PERCENTAGE_OVER_1",
			Clay_ErrorType.CLAY_ERROR_TYPE_INTERNAL_ERROR => "INTERNAL_ERROR",
			Clay_ErrorType.CLAY_ERROR_TYPE_UNBALANCED_OPEN_CLOSE => "UNBALANCED_OPEN_CLOSE",
			_ => "UNKNOWN"
		};

		// Log error to console with color
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine($"[Clay Error] {errorType}: {errorText}");
		Console.ResetColor();
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
		app.AddSystem((Res<ClayUiState> state, Local<Stack<Rectangle>> scissorStack) => RenderClayUI(state, scissorStack))
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
		if (mouseWheel != 0)
		{
			pointer.Value.AddScroll(new Vector2(0, mouseWheel * 20f));
		}

		pointer.Value.DeltaTime = Raylib.GetFrameTime();
	}

	private static unsafe void RenderClayUI(Res<ClayUiState> state, Local<Stack<Rectangle>> scissorStack)
	{
		var commands = state.Value.RenderCommands;

		// Clear scissor stack at the start of each frame
		scissorStack.Value.Clear();

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
					var newScissor = new Rectangle(
						scissorBox.x,
						scissorBox.y,
						scissorBox.width,
						scissorBox.height
					);

					// If there's already a scissor region active, intersect with it
					if (scissorStack.Value.Count > 0)
					{
						var currentScissor = scissorStack.Value.Peek();
						newScissor = IntersectRectangles(currentScissor, newScissor);
					}

					// Push the new scissor region onto the stack
					scissorStack.Value.Push(newScissor);

					// Apply the scissor region
					Raylib.BeginScissorMode(
						(int)newScissor.X,
						(int)newScissor.Y,
						(int)newScissor.Width,
						(int)newScissor.Height
					);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
					// End current scissor mode
					Raylib.EndScissorMode();

					// Pop the current scissor region
					if (scissorStack.Value.Count > 0)
					{
						scissorStack.Value.Pop();
					}

					// If there's a parent scissor region, restore it
					if (scissorStack.Value.Count > 0)
					{
						var parentScissor = scissorStack.Value.Peek();
						Raylib.BeginScissorMode(
							(int)parentScissor.X,
							(int)parentScissor.Y,
							(int)parentScissor.Width,
							(int)parentScissor.Height
						);
					}
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_CUSTOM:
					// Not implemented in this sample
					break;
			}
		}
	}

	/// <summary>
	/// Calculate the intersection of two rectangles for nested scissor regions.
	/// </summary>
	private static Rectangle IntersectRectangles(Rectangle a, Rectangle b)
	{
		var x1 = Math.Max(a.X, b.X);
		var y1 = Math.Max(a.Y, b.Y);
		var x2 = Math.Min(a.X + a.Width, b.X + b.Width);
		var y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

		// If rectangles don't intersect, return empty rectangle
		if (x2 <= x1 || y2 <= y1)
		{
			return new Rectangle(0, 0, 0, 0);
		}

		return new Rectangle(x1, y1, x2 - x1, y2 - y1);
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
