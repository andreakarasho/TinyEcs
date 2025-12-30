using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Direction of the progress bar fill.
/// </summary>
public enum ProgressBarDirection
{
	LeftToRight,
	RightToLeft,
	TopToBottom,
	BottomToTop
}

/// <summary>
/// Component that represents a progress bar widget.
/// Shows visual progress from 0% to 100%.
/// The progress bar entity itself is the track - child elements are identified by marker components:
/// - ProgressBarFill: the fill element
/// - ProgressBarLabel: the text label element (optional, shows percentage)
/// </summary>
public struct ProgressBar
{
	/// <summary>Current progress value (0.0 to 1.0)</summary>
	public float Progress;

	/// <summary>Direction of the progress bar fill</summary>
	public ProgressBarDirection Direction;

	/// <summary>Whether to show percentage text</summary>
	public bool ShowPercentage;

	public ProgressBar(float initialProgress = 0f, ProgressBarDirection direction = ProgressBarDirection.LeftToRight, bool showPercentage = false)
	{
		Progress = Math.Clamp(initialProgress, 0f, 1f);
		Direction = direction;
		ShowPercentage = showPercentage;
	}

	/// <summary>
	/// Sets the progress value (automatically clamped to 0-1)
	/// </summary>
	public void SetProgress(float value)
	{
		Progress = Math.Clamp(value, 0f, 1f);
	}

	/// <summary>
	/// Gets the progress as a percentage (0-100)
	/// </summary>
	public readonly int GetPercentage()
	{
		return (int)(Progress * 100f);
	}
}

/// <summary>
/// Marker component for the fill element inside a progress bar.
/// </summary>
public struct ProgressBarFill { }

/// <summary>
/// Marker component for the text label element inside a progress bar.
/// </summary>
public struct ProgressBarLabel { }

/// <summary>
/// Event triggered when progress bar value changes.
/// Use with On&lt;ProgressBarChanged&gt; in observers.
/// </summary>
public readonly struct ProgressBarChanged
{
	public readonly float Progress;
	public readonly int Percentage;

	public ProgressBarChanged(float progress, int percentage)
	{
		Progress = progress;
		Percentage = percentage;
	}
}

/// <summary>
/// Plugin that adds progress bar widget functionality.
/// Updates visual state when progress changes.
///
/// Usage:
/// <code>
/// app.AddPlugin(new ProgressBarPlugin());
/// </code>
/// </summary>
public struct ProgressBarPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System to update visual state when progress changes
		app.AddSystem((
			Commands commands,
			Query<Data<ProgressBar>, Filter<Changed<ProgressBar>>> changedProgressBars,
			Query<Data<Parent, UiNode>, Filter<With<ProgressBarFill>>> fills,
			Query<Data<Parent, UiText>, Filter<With<ProgressBarLabel>>> labels) =>
		{
			UpdateProgressBarVisuals(commands, changedProgressBars, fills, labels);
		})
		.InStage(Stage.PreUpdate)
		.Label("progressbar:update-visuals")
		.Build();
	}

	/// <summary>
	/// Updates the fill size and text label based on progress value.
	/// Finds child elements by looking for entities with marker components.
	/// </summary>
	private static void UpdateProgressBarVisuals(
		Commands commands,
		Query<Data<ProgressBar>, Filter<Changed<ProgressBar>>> changedProgressBars,
		Query<Data<Parent, UiNode>, Filter<With<ProgressBarFill>>> fills,
		Query<Data<Parent, UiText>, Filter<With<ProgressBarLabel>>> labels)
	{
		foreach (var (progressBarEntityId, progressBar) in changedProgressBars)
		{
			ref readonly var pb = ref progressBar.Ref;
			var pbId = progressBarEntityId.Ref;

			// Find and update fill size based on direction
			foreach (var (fillEntityId, parent, fillNode) in fills)
			{
				if (parent.Ref.Id != pbId)
					continue;

				ref var fill = ref fillNode.Ref;

				switch (pb.Direction)
				{
					case ProgressBarDirection.LeftToRight:
						// Fill from left, expand to right
						fill.Width = FlexValue.Percent(pb.Progress * 100f);
						fill.Height = FlexValue.Percent(100f);
						fill.Left = FlexValue.Points(0);
						fill.Right = FlexValue.Auto();
						break;

					case ProgressBarDirection.RightToLeft:
						// Fill from right, expand to left
						fill.Width = FlexValue.Percent(pb.Progress * 100f);
						fill.Height = FlexValue.Percent(100f);
						fill.Left = FlexValue.Auto();
						fill.Right = FlexValue.Points(0);
						break;

					case ProgressBarDirection.TopToBottom:
						// Fill from top, expand to bottom
						fill.Width = FlexValue.Percent(100f);
						fill.Height = FlexValue.Percent(pb.Progress * 100f);
						fill.Top = FlexValue.Points(0);
						fill.Bottom = FlexValue.Auto();
						break;

					case ProgressBarDirection.BottomToTop:
						// Fill from bottom, expand to top
						fill.Width = FlexValue.Percent(100f);
						fill.Height = FlexValue.Percent(pb.Progress * 100f);
						fill.Top = FlexValue.Auto();
						fill.Bottom = FlexValue.Points(0);
						break;
				}

				fill.PositionType = Flexbox.PositionType.Absolute;
				commands.Entity(fillEntityId.Ref).Insert(fill);
				break;
			}

			// Find and update text label if ShowPercentage is enabled
			if (pb.ShowPercentage)
			{
				foreach (var (labelEntityId, parent, textElement) in labels)
				{
					if (parent.Ref.Id != pbId)
						continue;

					ref var text = ref textElement.Ref;
					text.Value = $"{pb.GetPercentage()}%";
					commands.Entity(labelEntityId.Ref).Insert(text);
					break;
				}
			}
		}
	}
}
