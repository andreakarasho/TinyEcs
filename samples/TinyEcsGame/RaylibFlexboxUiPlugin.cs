using System;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Bevy;

namespace TinyEcsGame;

/// <summary>
/// Plugin that renders TinyEcs.UI.Bevy components using Raylib.
/// Executes render commands from the UI stack in correct z-order.
/// </summary>
public struct RaylibFlexboxUiPlugin : IPlugin
{
	public required Stage RenderingStage { get; set; }

	public readonly void Build(App app)
	{
		// Execute render commands (built from UI stack in correct z-order)
		app.AddSystem((Res<UiRenderCommands> renderCommands) =>
		{
			ExecuteRenderCommands(renderCommands);
		})
		.InStage(RenderingStage)
		.Label("ui:render:execute-commands")
		.SingleThreaded()
		.Build();
	}

	/// <summary>
	/// Executes all rendering commands in order.
	/// Commands are already sorted by z-index (back to front).
	/// </summary>
	private static void ExecuteRenderCommands(Res<UiRenderCommands> renderCommands)
	{
		foreach (var cmd in renderCommands.Value.Commands)
		{
			switch (cmd.Type)
			{
				case RenderCommandType.DrawBackground:
					DrawBackground(cmd);
					break;

				case RenderCommandType.DrawBorder:
					DrawBorder(cmd);
					break;

				case RenderCommandType.DrawText:
					DrawText(cmd);
					break;

				case RenderCommandType.BeginClip:
					Raylib.BeginScissorMode((int)cmd.ClipX, (int)cmd.ClipY, (int)cmd.ClipWidth, (int)cmd.ClipHeight);
					break;

				case RenderCommandType.EndClip:
					Raylib.EndScissorMode();
					break;
			}
		}
	}

	private static void DrawBackground(RenderCommand cmd)
	{
		var color = new Color(
			(byte)(cmd.BackgroundColor.X * 255f),
			(byte)(cmd.BackgroundColor.Y * 255f),
			(byte)(cmd.BackgroundColor.Z * 255f),
			(byte)(cmd.BackgroundColor.W * 255f)
		);

		Raylib.DrawRectangle((int)cmd.X, (int)cmd.Y, (int)cmd.Width, (int)cmd.Height, color);
	}

	private static void DrawBorder(RenderCommand cmd)
	{
		var color = new Color(
			(byte)(cmd.BorderColor.X * 255f),
			(byte)(cmd.BorderColor.Y * 255f),
			(byte)(cmd.BorderColor.Z * 255f),
			(byte)(cmd.BorderColor.W * 255f)
		);

		// Default border width (can be customized later)
		float borderWidth = 1f;

		if (cmd.BorderRadius > 0f)
		{
			// Rounded rectangle border (approximation using multiple DrawRectangle calls)
			// TODO: Implement proper rounded corners
			Raylib.DrawRectangleLinesEx(
				new Rectangle(cmd.X, cmd.Y, cmd.Width, cmd.Height),
				borderWidth,
				color
			);
		}
		else
		{
			// Simple rectangle border
			Raylib.DrawRectangleLinesEx(
				new Rectangle(cmd.X, cmd.Y, cmd.Width, cmd.Height),
				borderWidth,
				color
			);
		}
	}

	private static void DrawText(RenderCommand cmd)
	{
		if (string.IsNullOrEmpty(cmd.Text))
			return;

		var color = new Color(
			(byte)(cmd.TextColor.X * 255f),
			(byte)(cmd.TextColor.Y * 255f),
			(byte)(cmd.TextColor.Z * 255f),
			(byte)(cmd.TextColor.W * 255f)
		);

		// Render text centered in the layout area
		var fontSize = (int)cmd.FontSize;
		var textWidth = Raylib.MeasureText(cmd.Text, fontSize);
		var x = cmd.X + (cmd.Width - textWidth) / 2f;
		var y = cmd.Y + (cmd.Height - fontSize) / 2f;

		Raylib.DrawText(cmd.Text, (int)x, (int)y, fontSize, color);
	}
}
