using System;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Bevy;

namespace TinyEcsGame;

/// <summary>
/// Plugin that renders TinyEcs.UI.Bevy components using Raylib.
/// Converts ComputedLayout, BackgroundColor, BorderColor, BorderRadius, and UiText to Raylib draw calls.
/// </summary>
public struct RaylibFlexboxUiPlugin : IPlugin
{
	public required Stage RenderingStage { get; set; }

	public readonly void Build(App app)
	{
		// Render background rectangles with borders
		app.AddSystem((Query<Data<ComputedLayout, BackgroundColor>> backgrounds) =>
		{
			RenderBackgrounds(backgrounds);
		})
		.InStage(RenderingStage)
		.Label("ui:render:backgrounds")
		.SingleThreaded()
		.Build();

		// Render borders and border radius
		app.AddSystem((Query<Data<ComputedLayout, BorderColor, UiNode>> borders) =>
		{
			RenderBorders(borders);
		})
		.InStage(RenderingStage)
		.Label("ui:render:borders")
		.After("ui:render:backgrounds")
		.SingleThreaded()
		.Build();

		// Render text content
		app.AddSystem((Query<Data<ComputedLayout, UiText>> texts) =>
		{
			RenderTexts(texts);
		})
		.InStage(RenderingStage)
		.Label("ui:render:text")
		.After("ui:render:borders")
		.SingleThreaded()
		.Build();
	}

	/// <summary>
	/// Renders background colors for UI elements.
	/// </summary>
	private static void RenderBackgrounds(Query<Data<ComputedLayout, BackgroundColor>> backgrounds)
	{
		foreach (var (layout, bgColor) in backgrounds)
		{
			ref var l = ref layout.Ref;
			ref var color = ref bgColor.Ref;

			// Convert Vector4 (0-1) to Raylib Color (0-255)
			var raylibColor = new Color(
				(byte)(color.Color.X * 255f),
				(byte)(color.Color.Y * 255f),
				(byte)(color.Color.Z * 255f),
				(byte)(color.Color.W * 255f)
			);

			// Draw filled rectangle
			Raylib.DrawRectangle(
				(int)l.X,
				(int)l.Y,
				(int)l.Width,
				(int)l.Height,
				raylibColor
			);
		}
	}

	/// <summary>
	/// Renders borders for UI elements.
	/// Handles BorderColor and BorderRadius.
	/// </summary>
	private static void RenderBorders(Query<Data<ComputedLayout, BorderColor, UiNode>> borders)
	{
		foreach (var (layout, borderColor, uiNode) in borders)
		{
			ref var l = ref layout.Ref;
			ref var bColor = ref borderColor.Ref;
			ref var node = ref uiNode.Ref;

			// Convert Vector4 (0-1) to Raylib Color (0-255)
			var raylibColor = new Color(
				(byte)(bColor.Color.X * 255f),
				(byte)(bColor.Color.Y * 255f),
				(byte)(bColor.Color.Z * 255f),
				(byte)(bColor.Color.W * 255f)
			);

			// Get border widths (use BorderLeft as the thickness for now)
			float borderWidth = node.BorderLeft.IsDefined ? node.BorderLeft.Value : 1f;

			// Simple rectangle border (no rounded corners support yet)
			// Draw border as outline
			Raylib.DrawRectangleLinesEx(
				new Rectangle(l.X, l.Y, l.Width, l.Height),
				borderWidth,
				raylibColor
			);
		}
	}

	/// <summary>
	/// Renders text content for UI elements.
	/// Uses default white text at size 20 for now.
	/// </summary>
	private static void RenderTexts(Query<Data<ComputedLayout, UiText>> texts)
	{
		foreach (var (layout, text) in texts)
		{
			ref var l = ref layout.Ref;
			ref var t = ref text.Ref;

			if (string.IsNullOrEmpty(t.Value))
				continue;

			// Use default text style for now
			var fontSize = 20f;
			var textColor = Color.White;

			// Render text centered in the layout area
			var textWidth = Raylib.MeasureText(t.Value, (int)fontSize);
			var x = l.X + (l.Width - textWidth) / 2f;
			var y = l.Y + (l.Height - fontSize) / 2f;

			Raylib.DrawText(t.Value, (int)x, (int)y, (int)fontSize, textColor);
		}
	}
}
