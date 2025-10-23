using System;
using System.Numerics;
using Clay_cs;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;

namespace TinyEcs.UI.Examples;

/// <summary>
/// Example showing how rendering integrates with the reactive UI system.
/// The key insight: The reactive system handles STATE UPDATES, while rendering
/// just reads the final Clay render commands - no changes needed!
/// </summary>
public static class RenderingIntegrationExample
{
	public static void Main()
	{
		var app = new App(ThreadingMode.Single);

		// Step 1: Add reactive UI (handles state updates)
		app.AddReactiveUi(new ClayUiOptions
		{
			LayoutDimensions = new Clay_Dimensions(800f, 600f),
			ArenaSize = 256 * 1024,
			UseEntityHierarchy = true,
			AutoCreatePointerState = true
		});

		// Step 2: Create UI in startup
		app.AddSystem((Commands commands) => CreateUI(commands))
			.InStage(Stage.Startup)
			.Build();

		// Step 3: Simulate pointer input (in a real app, this comes from Raylib/SDL/etc)
		app.AddSystem((ResMut<ClayPointerState> pointer) => SimulatePointerInput(pointer))
			.InStage(Stage.PreUpdate)
			.Before("ui:clay:pointer")
			.Build();

		// Step 4: Render Clay commands (SAME AS BEFORE - no changes needed!)
		app.AddSystem((Res<ClayUiState> uiState) => RenderUI(uiState))
			.InStage(Stage.Update)
			.After("ui:clay:layout")
			.Build();

		// Run
		app.RunStartup();

		Console.WriteLine("\n=== Frame 1: Hover button ===");
		app.Update();

		Console.WriteLine("\n=== Frame 2: Press button ===");
		app.Update();

		Console.WriteLine("\n=== Frame 3: Release button (click!) ===");
		app.Update();
	}

	private static void CreateUI(Commands commands)
	{
		Console.WriteLine("Creating reactive UI...");

		// Create buttons with reactive interaction
		var button = ButtonWidget.Create(
			commands,
			ClayButtonStyle.Default,
			"Click Me!",
			parent: default);

		Console.WriteLine($"Button created: {button.Id}");
		Console.WriteLine("Button has Interaction component - will update automatically!");
	}

	private static int frameCount = 0;

	private static void SimulatePointerInput(ResMut<ClayPointerState> pointer)
	{
		ref var state = ref pointer.Value;

		frameCount++;

		// Simulate mouse movement and clicks
		switch (frameCount)
		{
			case 1:
				// Hover over button
				state.Position = new Vector2(80, 24); // Center of button
				state.PrimaryDown = false;
				Console.WriteLine("[Input] Mouse at button center, not pressed");
				break;

			case 2:
				// Press button
				state.Position = new Vector2(80, 24);
				state.PrimaryDown = true;
				Console.WriteLine("[Input] Mouse pressed on button");
				break;

			case 3:
				// Release button (triggers click)
				state.Position = new Vector2(80, 24);
				state.PrimaryDown = false;
				Console.WriteLine("[Input] Mouse released on button");
				break;
		}

		state.DeltaTime = 0.016f; // 60 FPS
	}

	private static void RenderUI(Res<ClayUiState> uiState)
	{
		// THIS IS THE KEY: Rendering is UNCHANGED from before!
		// The reactive system just updates the component state,
		// which causes Clay layout to use the updated colors.

		var commands = uiState.Value.RenderCommands;

		Console.WriteLine($"\n[Render] Processing {commands.Length} Clay render commands:");

		for (int i = 0; i < commands.Length; i++)
		{
			ref readonly var cmd = ref commands[i];

			switch (cmd.commandType)
			{
				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
					var rect = cmd.renderData.rectangle;
					var bounds = cmd.boundingBox;
					var color = rect.backgroundColor;
					Console.WriteLine($"  Rectangle: pos=({bounds.x:F0},{bounds.y:F0}) " +
									$"size=({bounds.width:F0}x{bounds.height:F0}) " +
									$"color=({color.r},{color.g},{color.b},{color.a})");
					// In real app: Raylib.DrawRectangle(...) or your renderer
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
					var text = cmd.renderData.text;
					// Convert Clay_StringSlice to string
					unsafe
					{
						var bytes = new ReadOnlySpan<byte>((byte*)text.stringContents.chars, text.stringContents.length);
						var textString = bytes.Length == 0 ? string.Empty : System.Text.Encoding.UTF8.GetString(bytes);
						Console.WriteLine($"  Text: '{textString}' " +
										$"color=({text.textColor.r},{text.textColor.g},{text.textColor.b})");
					}
					// In real app: Raylib.DrawText(...) or your renderer
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER:
					Console.WriteLine("  Border");
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
					Console.WriteLine("  Scissor Start (clipping)");
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
					Console.WriteLine("  Scissor End");
					break;
			}
		}
	}
}

/// <summary>
/// Example showing how to integrate with a real rendering backend (Raylib pattern).
/// </summary>
public static class RaylibRenderingExample
{
	// This is pseudocode - actual implementation in samples/TinyEcsGame/RaylibClayUiPlugin.cs

	public static void SetupWithRaylib()
	{
		var app = new App(ThreadingMode.Single);

		// Add reactive UI
		app.AddReactiveUi();

		// Update pointer from Raylib
		app.AddSystem((ResMut<ClayPointerState> pointer) =>
		{
			// ref var state = ref pointer.Value;
			// state.Position = Raylib.GetMousePosition();
			// state.PrimaryDown = Raylib.IsMouseButtonDown(MouseButton.Left);
			// state.AddScroll(new Vector2(0, Raylib.GetMouseWheelMove() * 20f));
			// state.DeltaTime = Raylib.GetFrameTime();
		})
		.InStage(Stage.PreUpdate)
		.Before("ui:clay:pointer")
		.Build();

		// Render Clay commands with Raylib
		app.AddSystem((Res<ClayUiState> uiState) =>
		{
			var commands = uiState.Value.RenderCommands;

			foreach (ref readonly var cmd in commands)
			{
				switch (cmd.commandType)
				{
					case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
						// Raylib.DrawRectangle(...);
						break;

					case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
						// Raylib.DrawTextEx(font, text, ...);
						break;

					case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER:
						// Raylib.DrawRectangleLines(...);
						break;

					case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
						// Raylib.BeginScissorMode(...);
						break;

					case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
						// Raylib.EndScissorMode();
						break;
				}
			}
		})
		.InStage(Stage.Update)
		.After("ui:clay:layout")
		.Build();

		// Game loop
		// while (!Raylib.WindowShouldClose())
		// {
		//     Raylib.BeginDrawing();
		//     Raylib.ClearBackground(Color.Black);
		//
		//     app.Update(); // Runs all systems including rendering
		//
		//     Raylib.EndDrawing();
		// }
	}
}

/// <summary>
/// SUMMARY: How Reactive UI Works with Rendering
///
/// OLD APPROACH (Manual Entity Observers):
/// 1. User hovers button
/// 2. UiPointerEvent fired
/// 3. Button's entity observer runs
/// 4. Observer manually updates UiNode.Declaration.backgroundColor
/// 5. Layout pass uses updated color
/// 6. Render commands have new color
///
/// NEW APPROACH (Reactive Components):
/// 1. User hovers button
/// 2. UiPointerEvent fired
/// 3. UpdateInteractionState system runs (updates Interaction = Hovered)
/// 4. OnButtonInteractionChanged observer runs (reacts to Changed<Interaction>)
/// 5. Observer updates UiNode.Declaration.backgroundColor
/// 6. Layout pass uses updated color
/// 7. Render commands have new color
///
/// KEY INSIGHT: Rendering is IDENTICAL in both approaches!
/// The only difference is HOW the UiNode.Declaration gets updated:
/// - Old: Per-entity observer per widget instance
/// - New: Centralized system + batched observer per widget TYPE
///
/// RENDERING CODE UNCHANGED:
/// - Still read ClayUiState.RenderCommands
/// - Still iterate and render each command type
/// - Still use Raylib/SDL/your renderer the same way
///
/// The reactive system is purely about STATE MANAGEMENT, not rendering.
/// </summary>
public static class HowItWorks { }
