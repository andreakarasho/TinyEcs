using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TextAlign = TinyEcs.UI.Bevy.TextAlign;
using TextVerticalAlign = TinyEcs.UI.Bevy.TextVerticalAlign;

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
		// Register Raylib text measurement function in startup
		app.AddSystem((ResMut<TinyEcs.UI.Bevy.TextMeasureContext> measureContext) =>
		{
			// Set up the measurement callback
			measureContext.Value.MeasureText = (text, style) =>
			{
				if (string.IsNullOrEmpty(text))
					return (0f, 0f);

				var fontSize = (int)style.FontSize;
				var width = Raylib.MeasureText(text, fontSize);
				var height = fontSize;
				return (width, height);
			};
		})
		.InStage(Stage.Startup)
		.Label("raylib:setup-text-measure")
		.Build();

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

		// Check if any corner has a radius
		bool hasRoundedCorners = cmd.BorderRadiusTopLeft > 0f || cmd.BorderRadiusTopRight > 0f ||
		                         cmd.BorderRadiusBottomRight > 0f || cmd.BorderRadiusBottomLeft > 0f;

		if (hasRoundedCorners)
		{
			// For now, use uniform radius (average of all corners) for Raylib rendering
			// TODO: Implement proper individual corner radius rendering
			float avgRadius = (cmd.BorderRadiusTopLeft + cmd.BorderRadiusTopRight +
			                   cmd.BorderRadiusBottomRight + cmd.BorderRadiusBottomLeft) / 4f;

			Raylib.DrawRectangleRounded(
				new Rectangle(cmd.X, cmd.Y, cmd.Width, cmd.Height),
				avgRadius / Math.Min(cmd.Width, cmd.Height), // Normalize to 0-1 range
				8, // segments
				color
			);
		}
		else
		{
			Raylib.DrawRectangle((int)cmd.X, (int)cmd.Y, (int)cmd.Width, (int)cmd.Height, color);
		}
	}

	private static void DrawBorder(RenderCommand cmd)
	{
		var color = new Color(
			(byte)(cmd.BorderColor.X * 255f),
			(byte)(cmd.BorderColor.Y * 255f),
			(byte)(cmd.BorderColor.Z * 255f),
			(byte)(cmd.BorderColor.W * 255f)
		);

		// Get border thickness - use maximum value if edges differ (Raylib limitation)
		// Raylib only supports uniform border thickness, so we take the max to ensure all borders are visible
		float borderWidth = Math.Max(
			Math.Max(cmd.BorderThicknessTop, cmd.BorderThicknessRight),
			Math.Max(cmd.BorderThicknessBottom, cmd.BorderThicknessLeft)
		);

		// Check if any corner has a radius
		bool hasRoundedCorners = cmd.BorderRadiusTopLeft > 0f || cmd.BorderRadiusTopRight > 0f ||
		                         cmd.BorderRadiusBottomRight > 0f || cmd.BorderRadiusBottomLeft > 0f;

		if (hasRoundedCorners)
		{
			// For now, use uniform radius (average of all corners) for Raylib rendering
			// TODO: Implement proper individual corner radius rendering
			float avgRadius = (cmd.BorderRadiusTopLeft + cmd.BorderRadiusTopRight +
			                   cmd.BorderRadiusBottomRight + cmd.BorderRadiusBottomLeft) / 4f;

			// DrawRectangleRoundedLines takes: (rect, roundness, segments, color)
			// Roundness is 0.0-1.0, so normalize the radius
			float roundness = Math.Min(avgRadius / Math.Min(cmd.Width, cmd.Height) * 2f, 1.0f);

			// DrawRectangleRoundedLines in Raylib-cs 7.0.1 doesn't support lineThick parameter
			// Workaround: Draw multiple rounded rectangles with decreasing sizes for thickness
			if (borderWidth > 1f)
			{
				// For thicker borders, draw filled rounded rect and inset it
				// This is not perfect but works for most cases
				for (int i = 0; i < (int)borderWidth; i++)
				{
					float offset = i * 0.5f;
					Raylib.DrawRectangleRoundedLines(
						new Rectangle(cmd.X + offset, cmd.Y + offset, cmd.Width - offset * 2, cmd.Height - offset * 2),
						roundness,
						8, // segments
						color
					);
				}
			}
			else
			{
				Raylib.DrawRectangleRoundedLines(
					new Rectangle(cmd.X, cmd.Y, cmd.Width, cmd.Height),
					roundness,
					8, // segments
					color
				);
			}
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

	private static unsafe void DrawText(RenderCommand cmd)
	{
		if (string.IsNullOrEmpty(cmd.Text))
			return;

		var color = new Color(
			(byte)(cmd.TextColor.X * 255f),
			(byte)(cmd.TextColor.Y * 255f),
			(byte)(cmd.TextColor.Z * 255f),
			(byte)(cmd.TextColor.W * 255f)
		);

		var fontSize = (int)cmd.FontSize;
		var textWidth = Raylib.MeasureText(cmd.Text, fontSize);

		// Calculate horizontal position based on alignment
		float x = cmd.TextHorizontalAlign switch
		{
			TextAlign.Left => cmd.X,
			TextAlign.Center => cmd.X + (cmd.Width - textWidth) / 2f,
			TextAlign.Right => cmd.X + cmd.Width - textWidth,
			_ => cmd.X
		};

		// Calculate vertical position based on alignment
		float y = cmd.TextVerticalAlign switch
		{
			TextVerticalAlign.Top => cmd.Y,
			TextVerticalAlign.Middle => cmd.Y + (cmd.Height - fontSize) / 2f,
			TextVerticalAlign.Bottom => cmd.Y + cmd.Height - fontSize,
			_ => cmd.Y
		};


		var font = Raylib.GetFontDefault();
		var utf8Bytes = System.Text.Encoding.UTF8.GetByteCount(cmd.Text) + 1;
		var buf = ArrayPool<byte>.Shared.Rent(utf8Bytes);
		try
		{
			System.Text.Encoding.UTF8.TryGetBytes(cmd.Text, buf.AsSpan(0, utf8Bytes - 1), out int bytesWritten);
			buf[bytesWritten] = 0; // Null-terminate

			fixed (byte* ptr = buf)
			{
				Raylib.DrawTextEx(font, (sbyte*)ptr, new Vector2(x, y), fontSize, 0f, color);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buf);
		}
	}
}
