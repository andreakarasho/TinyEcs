using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Clay;

/// <summary>
/// Example demonstrating Clay UI integration with TinyEcs.Bevy.
/// Shows how to create UI elements, handle interactions, and render.
/// </summary>
public static class ClayExample
{
	public static void Run()
	{
		Console.WriteLine("=== TinyEcs.UI.Clay Example ===\n");

		using var world = new World();
		var app = new App(world, ThreadingMode.Single);

		// Add Clay UI plugin
		app.AddClayUi(new ClayUiOptions
		{
			LayoutDimensions = new Clay_Dimensions(800, 600),
			ArenaSize = 1024 * 1024,
			MaxElementCount = 1024,
			EnableDebugMode = false,
			EnableCulling = true
		});

		// Startup system: Create UI hierarchy
		app.AddSystem((Commands commands) => CreateUI(commands))
			.InStage(Stage.Startup)
			.Label("example:create-ui")
			.Build();

		// Update system: Handle pointer events
		app.AddSystem((EventReader<ClayPointerEvent> events) => HandlePointerEvents(events))
			.InStage(Stage.Update)
			.Label("example:handle-events")
			.After("clay:interaction")
			.Build();

		// Update system: Render UI (print render commands)
		app.AddSystem((Res<ClayUiState> state) => RenderUI(state))
			.InStage(Stage.Update)
			.Label("example:render")
			.After("example:handle-events")
			.Build();

		// Add system to simulate pointer input
		app.AddSystem((ResMut<ClayPointerState> pointer) => SimulatePointerInput(pointer))
			.InStage(Stage.First)
			.Label("example:simulate-input")
			.Build();

		// Run startup
		app.RunStartup();

		// Run 3 frames to simulate interactions
		Console.WriteLine("\n--- Frame 1: Hover over button ---");
		app.Update();

		Console.WriteLine("\n--- Frame 2: Click button ---");
		app.Update();

		Console.WriteLine("\n--- Frame 3: Release button ---");
		app.Update();

		Console.WriteLine("\n=== Example Complete ===\n");
	}

	private static int _frameCount = 0;

	private static void SimulatePointerInput(ResMut<ClayPointerState> pointer)
	{
		_frameCount++;

		// Simulate different pointer states per frame
		switch (_frameCount)
		{
			case 1:
				// Hover
				pointer.Value.Position = new Vector2(100, 100);
				pointer.Value.PrimaryDown = false;
				pointer.Value.PrimaryPressed = false;
				pointer.Value.PrimaryReleased = false;
				break;
			case 2:
				// Click
				pointer.Value.Position = new Vector2(100, 100);
				pointer.Value.PrimaryDown = true;
				pointer.Value.PrimaryPressed = true;
				pointer.Value.PrimaryReleased = false;
				break;
			case 3:
				// Release
				pointer.Value.Position = new Vector2(100, 100);
				pointer.Value.PrimaryDown = false;
				pointer.Value.PrimaryPressed = false;
				pointer.Value.PrimaryReleased = true;
				break;
		}

		pointer.Value.DeltaTime = 1f / 60f;
	}

	private static void CreateUI(Commands commands)
	{
		Console.WriteLine("[CreateUI] Building UI hierarchy...");

		// Create root container
		var rootNode = ClayNode.Default;
		rootNode.Layout = new Clay_LayoutConfig
		{
			sizing = new Clay_Sizing(
				Clay_SizingAxis.Grow(),
				Clay_SizingAxis.Grow()
			),
			layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
			padding = Clay_Padding.All(16),
			childGap = 16,
			childAlignment = new Clay_ChildAlignment(
				Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
				Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
			)
		};
		rootNode.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = new Clay_Color(40, 44, 52, 255)
		};

		var root = commands.SpawnClayElement(rootNode);
		Console.WriteLine($"  Created root container (entity {root.Id})");

		// Create title text
		var titleConfig = new Clay_TextElementConfig
		{
			fontSize = 32,
			textColor = new Clay_Color(255, 255, 255, 255)
		};

		var title = commands.SpawnClayText("Hello Clay!", titleConfig);
		root.AddChild(title.Id);
		Console.WriteLine($"  Created title text (entity {title.Id})");

		// Create button
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
			backgroundColor = new Clay_Color(70, 130, 180, 255)
		};
		buttonNode.CornerRadius = Clay_CornerRadius.All(8);

		var button = commands.SpawnClayElement(buttonNode);
		root.AddChild(button.Id);
		Console.WriteLine($"  Created button (entity {button.Id})");

		// Add button text
		var buttonTextConfig = new Clay_TextElementConfig
		{
			fontSize = 18,
			textColor = new Clay_Color(255, 255, 255, 255)
		};

		var buttonText = commands.SpawnClayText("Click Me!", buttonTextConfig);
		button.AddChild(buttonText.Id);
		Console.WriteLine($"  Created button text (entity {buttonText.Id})");

		// Add observer to button for click events
		button.Observe<On<ClayPointerTrigger>, Commands>((trigger, commands) =>
		{
			var evt = trigger.Event;
			Console.WriteLine($"  [Observer] Button event: {evt.Event.EventType} at ({evt.Event.Position.X:F0}, {evt.Event.Position.Y:F0})");

			if (evt.Event.EventType == ClayPointerEventType.Click)
			{
				Console.WriteLine("  [Observer] BUTTON CLICKED!");
			}
		});

		Console.WriteLine("[CreateUI] UI hierarchy created successfully\n");
	}

	private static void HandlePointerEvents(EventReader<ClayPointerEvent> events)
	{
		foreach (var evt in events.Read())
		{
			Console.WriteLine($"[Event] {evt.EventType} on entity {evt.EntityId} at ({evt.Position.X:F0}, {evt.Position.Y:F0})");
		}
	}

	private static void RenderUI(Res<ClayUiState> state)
	{
		var commands = state.Value.RenderCommands;

		if (commands.Length == 0)
		{
			Console.WriteLine("[Render] No render commands");
			return;
		}

		Console.WriteLine($"[Render] {commands.Length} render commands:");

		for (int i = 0; i < commands.Length; i++)
		{
			ref readonly var cmd = ref commands[i];
			var bounds = cmd.boundingBox;

			var cmdType = cmd.commandType switch
			{
				Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE => "Rectangle",
				Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER => "Border",
				Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT => "Text",
				Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE => "Image",
				Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START => "ScissorStart",
				Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END => "ScissorEnd",
				Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_CUSTOM => "Custom",
				_ => "Unknown"
			};

			Console.WriteLine($"  [{i}] {cmdType,-15} bounds=({bounds.x:F0}, {bounds.y:F0}, {bounds.width:F0}, {bounds.height:F0})");

			// Print additional details for text commands
			if (cmd.commandType == Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT)
			{
				unsafe
				{
					var textData = cmd.renderData.text;
					var textLength = textData.stringContents.length;
					Console.WriteLine($"      Text: length={textLength} fontSize={textData.fontSize}");
				}
			}
		}
	}
}
