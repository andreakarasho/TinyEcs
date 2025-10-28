using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Bevy;

namespace TinyEcsGame;

/// <summary>
/// Represents a scissor rectangle for clipping.
/// </summary>
public struct ScissorRect
{
	public int X;
	public int Y;
	public int Width;
	public int Height;

	public ScissorRect(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	/// <summary>
	/// Intersects this scissor rect with another, returning the intersection.
	/// </summary>
	public readonly ScissorRect Intersect(ScissorRect other)
	{
		int x1 = Math.Max(X, other.X);
		int y1 = Math.Max(Y, other.Y);
		int x2 = Math.Min(X + Width, other.X + other.Width);
		int y2 = Math.Min(Y + Height, other.Y + other.Height);

		return new ScissorRect(
			x1,
			y1,
			Math.Max(0, x2 - x1),
			Math.Max(0, y2 - y1)
		);
	}
}

/// <summary>
/// Plugin that renders TinyEcs.UI.Bevy components using Raylib.
/// Executes render commands from the UI stack in correct z-order.
/// Handles nested scissor clipping using a stack.
/// </summary>
public struct RaylibFlexboxUiPlugin : IPlugin
{
	public required Stage RenderingStage { get; set; }

	public readonly void Build(App app)
	{
		// Execute render commands (built from UI stack in correct z-order)
		app.AddSystem((Res<UiRenderCommands> renderCommands, Local<Stack<ScissorRect>> scissorStack) =>
		{
			ExecuteRenderCommands(renderCommands, scissorStack);
		})
		.InStage(RenderingStage)
		.Label("ui:render:execute-commands")
		.SingleThreaded()
		.Build();
	}

	/// <summary>
	/// Executes all rendering commands in order.
	/// Commands are already sorted by z-index (back to front).
	/// Uses a scissor stack to handle nested clipping properly.
	/// </summary>
	private static void ExecuteRenderCommands(Res<UiRenderCommands> renderCommands, Local<Stack<ScissorRect>> scissorStack)
	{
		// Clear the stack at the start of each frame (reuse the same stack instance)
		scissorStack.Value.Clear();

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
					{
						var newScissor = new ScissorRect(
							(int)cmd.ClipX,
							(int)cmd.ClipY,
							(int)cmd.ClipWidth,
							(int)cmd.ClipHeight
						);

						// If there's already a scissor active, intersect with it
						if (scissorStack.Value.Count > 0)
						{
							var currentScissor = scissorStack.Value.Peek();
							newScissor = currentScissor.Intersect(newScissor);
						}

						// Push the new scissor onto the stack
						scissorStack.Value.Push(newScissor);

						// Apply the intersected scissor
						Raylib.BeginScissorMode(newScissor.X, newScissor.Y, newScissor.Width, newScissor.Height);
						break;
					}

				case RenderCommandType.EndClip:
					{
						if (scissorStack.Value.Count > 0)
						{
							// End the current scissor
							Raylib.EndScissorMode();
							scissorStack.Value.Pop();

							// If there's a parent scissor, reapply it
							if (scissorStack.Value.Count > 0)
							{
								var parentScissor = scissorStack.Value.Peek();
								Raylib.BeginScissorMode(parentScissor.X, parentScissor.Y, parentScissor.Width, parentScissor.Height);
							}
						}
						break;
					}
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
