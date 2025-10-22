using System;
using System.Numerics;
using Clay_cs;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;

namespace TinyEcsGame;

/// <summary>
/// Plugin that integrates Clay UI with Raylib rendering.
/// Handles UI rendering, pointer input, and provides a complete UI system.
/// </summary>
public sealed class RaylibClayUiPlugin : IPlugin
{
	public required Stage RenderingStage { get; set; }

	public void Build(App app)
	{
		// Add Clay UI with configuration
		app.AddClayUi(new ClayUiOptions
		{
			LayoutDimensions = new Clay_Dimensions(1280f, 720f),
			ArenaSize = 512 * 1024,
			EnableDebugMode = false,
			UseEntityHierarchy = true,
			AutoCreatePointerState = true,
			AutoRegisterDefaultSystems = true
		});

		// Add Raylib-specific systems
		RegisterRaylibSystems(app);
	}

	private void RegisterRaylibSystems(App app)
	{
		// Update pointer state from Raylib mouse input
		app.AddSystem((ResMut<ClayPointerState> pointer, Res<WindowSize> windowSize) =>
		{
			ref var state = ref pointer.Value;

			// Get mouse position
			var mousePos = Raylib.GetMousePosition();
			state.Position = mousePos;

			// Get mouse button state
			state.PrimaryDown = Raylib.IsMouseButtonDown(MouseButton.Left);

			// Get scroll wheel delta
			var scrollY = Raylib.GetMouseWheelMove();
			if (scrollY != 0f)
			{
				state.AddScroll(new Vector2(0, scrollY * 20f)); // Multiply for better feel
			}

			// Update delta time
			state.DeltaTime = Raylib.GetFrameTime();
		})
		.InStage(Stage.PreUpdate)
		.Label("ui:raylib:update-pointer")
		.Before("ui:clay:pointer")
		.SingleThreaded()
		.RunIfResourceExists<ClayPointerState>()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		// Render Clay UI with Raylib (after debug text, before EndDrawing)
		app.AddSystem((Res<ClayUiState> uiState) =>
		{
			RenderClayUI(uiState.Value);
		})
		.InStage(RenderingStage)
		.Label("ui:raylib:render")
		.After("render:debug")
		.SingleThreaded()
		.RunIfResourceExists<ClayUiState>()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();
	}

	private void RenderClayUI(ClayUiState uiState)
	{
		var commands = uiState.RenderCommands;

		for (int i = 0; i < commands.Length; i++)
		{
			ref readonly var cmd = ref commands[i];

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

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
					RenderImage(cmd);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
					StartScissor(cmd);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
					EndScissor();
					break;
			}
		}
	}

	private void RenderRectangle(Clay_RenderCommand cmd)
	{
		var config = cmd.renderData.rectangle;
		var bounds = cmd.boundingBox;

		var color = ToRaylibColor(config.backgroundColor);
		var rect = new Rectangle(bounds.x, bounds.y, bounds.width, bounds.height);

		if (config.cornerRadius.topLeft > 0 || config.cornerRadius.topRight > 0 ||
		    config.cornerRadius.bottomLeft > 0 || config.cornerRadius.bottomRight > 0)
		{
			// Use average corner radius for rounded rectangle
			var avgRadius = (config.cornerRadius.topLeft + config.cornerRadius.topRight +
			                config.cornerRadius.bottomLeft + config.cornerRadius.bottomRight) / 4f;
			Raylib.DrawRectangleRounded(rect, avgRadius / Math.Min(bounds.width, bounds.height), 8, color);
		}
		else
		{
			Raylib.DrawRectangleRec(rect, color);
		}
	}

	private void RenderBorder(Clay_RenderCommand cmd)
	{
		var config = cmd.renderData.border;
		var bounds = cmd.boundingBox;
		var color = ToRaylibColor(config.color);

		// Draw border lines
		if (config.width.left > 0)
		{
			Raylib.DrawRectangle(
				(int)bounds.x,
				(int)bounds.y,
				(int)config.width.left,
				(int)bounds.height,
				color);
		}

		if (config.width.right > 0)
		{
			Raylib.DrawRectangle(
				(int)(bounds.x + bounds.width - config.width.right),
				(int)bounds.y,
				(int)config.width.right,
				(int)bounds.height,
				color);
		}

		if (config.width.top > 0)
		{
			Raylib.DrawRectangle(
				(int)bounds.x,
				(int)bounds.y,
				(int)bounds.width,
				(int)config.width.top,
				color);
		}

		if (config.width.bottom > 0)
		{
			Raylib.DrawRectangle(
				(int)bounds.x,
				(int)(bounds.y + bounds.height - config.width.bottom),
				(int)bounds.width,
				(int)config.width.bottom,
				color);
		}
	}

	private unsafe void RenderText(Clay_RenderCommand cmd)
	{
		var textData = cmd.renderData.text;
		var bounds = cmd.boundingBox;

		// Convert Clay_String to C# string
		var text = new string(textData.stringContents.chars, 0, textData.stringContents.length);
		var color = ToRaylibColor(textData.textColor);

		// Calculate text position
		var fontSize = textData.fontSize;
		var textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), text, fontSize, 1f);

		float x = bounds.x;
		float y = bounds.y + (bounds.height - textSize.Y) / 2f; // Vertically center

		Raylib.DrawText(text, (int)x, (int)y, fontSize, color);
	}

	private unsafe void RenderImage(Clay_RenderCommand cmd)
	{
		var imageData = cmd.renderData.image;
		var bounds = cmd.boundingBox;

		// imageData.imageData would contain a Texture2D ID or pointer
		// For this example, we'll skip actual image rendering
		// In a real implementation, you'd cast imageData to your texture type

		// Placeholder: draw a colored rectangle to show where image would be
		Raylib.DrawRectangleLinesEx(
			new Rectangle(bounds.x, bounds.y, bounds.width, bounds.height),
			2f,
			new Raylib_cs.Color(200, 200, 200, 255));
	}

	private void StartScissor(Clay_RenderCommand cmd)
	{
		var bounds = cmd.boundingBox;
		Raylib.BeginScissorMode(
			(int)bounds.x,
			(int)bounds.y,
			(int)bounds.width,
			(int)bounds.height);
	}

	private void EndScissor()
	{
		Raylib.EndScissorMode();
	}

	private Raylib_cs.Color ToRaylibColor(Clay_Color clayColor)
	{
		return new Raylib_cs.Color(
			(byte)clayColor.r,
			(byte)clayColor.g,
			(byte)clayColor.b,
			(byte)clayColor.a);
	}
}
