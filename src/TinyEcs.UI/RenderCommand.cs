using System.Numerics;
using TinyEcs.UI.Bevy;

namespace TinyEcs.UI;

/// <summary>
/// Types of UI rendering commands
/// </summary>
public enum RenderCommandType
{
	DrawBackground,
	DrawBorder,
	DrawText,
	DrawImage,
	BeginClip,
	EndClip
}

/// <summary>
/// A single rendering command for UI elements.
/// Commands are collected from the UI stack and executed in layer order.
/// </summary>
public struct RenderCommand
{
	public RenderCommandType Type;
	public ulong EntityId;

	// Layout (used by all draw commands)
	public float X, Y, Width, Height;

	// Background
	public Vector4 BackgroundColor;

	// Border
	public Vector4 BorderColor;
	public float BorderRadius;

	// Text
	public string Text;
	public float FontSize;
	public Vector4 TextColor;
	public TextAlign TextHorizontalAlign;
	public TextVerticalAlign TextVerticalAlign;

	// Image
	public string ImagePath;

	// Clip region
	public float ClipX, ClipY, ClipWidth, ClipHeight;

	public static RenderCommand DrawBackground(ulong entityId, float x, float y, float w, float h, Vector4 color)
	{
		return new RenderCommand
		{
			Type = RenderCommandType.DrawBackground,
			EntityId = entityId,
			X = x, Y = y, Width = w, Height = h,
			BackgroundColor = color
		};
	}

	public static RenderCommand DrawBorder(ulong entityId, float x, float y, float w, float h, Vector4 color, float radius)
	{
		return new RenderCommand
		{
			Type = RenderCommandType.DrawBorder,
			EntityId = entityId,
			X = x, Y = y, Width = w, Height = h,
			BorderColor = color,
			BorderRadius = radius
		};
	}

	public static RenderCommand DrawText(
		ulong entityId,
		float x, float y, float w, float h,
		string text,
		float fontSize,
		Vector4 color,
		TextAlign horizontalAlign = TextAlign.Left,
		TextVerticalAlign verticalAlign = TextVerticalAlign.Top)
	{
		return new RenderCommand
		{
			Type = RenderCommandType.DrawText,
			EntityId = entityId,
			X = x, Y = y, Width = w, Height = h,
			Text = text,
			FontSize = fontSize,
			TextColor = color,
			TextHorizontalAlign = horizontalAlign,
			TextVerticalAlign = verticalAlign
		};
	}

	public static RenderCommand BeginClip(float x, float y, float w, float h)
	{
		return new RenderCommand
		{
			Type = RenderCommandType.BeginClip,
			ClipX = x, ClipY = y, ClipWidth = w, ClipHeight = h
		};
	}

	public static RenderCommand EndClip()
	{
		return new RenderCommand
		{
			Type = RenderCommandType.EndClip
		};
	}
}

/// <summary>
/// Resource containing all rendering commands for the current frame.
/// Commands are populated by walking the UI stack and executed by the renderer.
/// </summary>
public class UiRenderCommands
{
	public List<RenderCommand> Commands { get; private set; } = new();

	public void Clear() => Commands.Clear();
	public void Add(RenderCommand command) => Commands.Add(command);
}
