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
	/// Creates a progress bar widget.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the progress bar to</param>
	/// <param name="initialValue">Initial progress value</param>
	/// <param name="min">Minimum value</param>
	/// <param name="max">Maximum value</param>
	/// <param name="width">Progress bar width in pixels</param>
	/// <param name="height">Progress bar height in pixels</param>
	/// <param name="showLabel">Whether to show percentage label</param>
	/// <param name="fillColor">Color of the fill bar</param>
	/// <param name="backgroundColor">Color of the background track</param>
	/// <returns>The progress bar container entity ID</returns>
	public static ulong CreateProgressBar(
		this Commands commands,
		EntityCommands parent,
		float initialValue = 0f,
		float min = 0f,
		float max = 100f,
		float width = 200f,
		float height = 24f,
		bool showLabel = true,
		Clay_Color? fillColor = null,
		Clay_Color? backgroundColor = null)
	{
		var bgColor = backgroundColor ?? new Clay_Color(40, 40, 45, 255);
		var barColor = fillColor ?? new Clay_Color(76, 175, 80, 255);

		// Container/Track
		var containerNode = ClayNode.Configure()
			.Size(width, height)
			.Row()
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Background(bgColor)
			.Border(new Clay_Color(80, 80, 90, 255), 1)
			.CornerRadius(4)
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
			CornerRadius = Clay_CornerRadius.All(3)
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
				.Text($"{percentage}%", 14, new Clay_Color(255, 255, 255, 255))
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
