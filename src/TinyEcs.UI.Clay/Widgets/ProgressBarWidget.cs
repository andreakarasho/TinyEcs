using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track progress bar state.
/// </summary>
public struct ProgressBarState
{
	public ulong FillEntityId;
	public ulong LabelEntityId;
	public float Value;
	public float Min;
	public float Max;
	public bool ShowLabel;
}

/// <summary>
/// Marker component to update progress bar fill width.
/// </summary>
public struct ProgressBarFillUpdate
{
	public float NormalizedValue;
}

/// <summary>
/// Marker component to update progress bar label text.
/// </summary>
public struct ProgressBarLabelUpdate
{
	public string Text;
}

/// <summary>
/// Extension methods for creating progress bar widgets.
/// </summary>
public static class ProgressBarWidget
{
	/// <summary>
	/// Creates a progress bar widget using theme colors.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the progress bar to</param>
	/// <param name="theme">Theme resource for styling</param>
	/// <param name="initialValue">Initial progress value</param>
	/// <param name="min">Minimum value</param>
	/// <param name="max">Maximum value</param>
	/// <param name="width">Progress bar width in pixels (0 = use default 200)</param>
	/// <param name="showLabel">Whether to show percentage label</param>
	/// <param name="fillColor">Optional fill color override</param>
	/// <param name="backgroundColor">Optional background color override</param>
	/// <returns>The progress bar container entity ID</returns>
	public static ulong CreateProgressBar(
		this Commands commands,
		EntityCommands parent,
		ClayTheme theme,
		float initialValue = 0f,
		float min = 0f,
		float max = 100f,
		float width = 0f,
		bool showLabel = true,
		Clay_Color? fillColor = null,
		Clay_Color? backgroundColor = null)
	{
		var progressBarTheme = theme.ProgressBar;
		var actualWidth = width > 0 ? width : 200f;
		var height = progressBarTheme.Height;

		var bgColor = backgroundColor ?? progressBarTheme.BackgroundColor;
		var barColor = fillColor ?? progressBarTheme.FillColor;

		// Container/Track
		var containerNode = ClayNode.Configure()
			.Size(actualWidth, height)
			.Row()
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Background(bgColor)
			.Border(progressBarTheme.BorderColor, progressBarTheme.BorderWidth)
			.CornerRadius(progressBarTheme.CornerRadius)
			.Build();

		var container = commands.SpawnClayElement(containerNode);
		parent.AddChild(container);

		// Fill bar
		// Note: Using ClayNode.Default with for percentage sizing (not supported in fluent API yet)
		float normalizedValue = (initialValue - min) / (max - min);
		var fillNode = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Percent(normalizedValue),
					Clay_SizingAxis.Grow()
				),
				childAlignment = new Clay_ChildAlignment(
					Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
					Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
				)
			},
			Rectangle = new Clay_RectangleRenderData
			{
				backgroundColor = barColor
			},
			CornerRadius = Clay_CornerRadius.All((ushort)(progressBarTheme.CornerRadius > 0 ? progressBarTheme.CornerRadius - 1 : 0))
		};

		var fill = commands.SpawnClayElement(fillNode);
		container.AddChild(fill);

		// Optional label
		ulong labelEntityId = 0;
		if (showLabel)
		{
			var percentage = (int)((normalizedValue) * 100);
			var labelNode = ClayNode.Configure()
				.WidthFit(0, 0)
				.HeightFit(0, 0)
				.Text($"{percentage}%", theme.Typography.DefaultFontSize, progressBarTheme.TextColor)
				.Build();

			var label = commands.SpawnClayElement(labelNode);
			fill.AddChild(label);
			labelEntityId = label.Id;
		}

		// Add state component
		commands.Entity(container.Id).Insert(new ProgressBarState
		{
			FillEntityId = fill.Id,
			LabelEntityId = labelEntityId,
			Value = initialValue,
			Min = min,
			Max = max,
			ShowLabel = showLabel
		});

		return container.Id;
	}

	/// <summary>
	/// Updates the progress bar value.
	/// </summary>
	public static void SetProgress(this Commands commands, ulong progressBarId, float value)
	{
		// This would be called from user code to update the progress
		// The actual update would happen in the ProgressBarPlugin systems
	}
}
