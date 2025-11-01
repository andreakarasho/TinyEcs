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
/// </summary>
public struct ProgressBar
{
	/// <summary>Current progress value (0.0 to 1.0)</summary>
	public float Progress;

	/// <summary>Direction of the progress bar fill</summary>
	public ProgressBarDirection Direction;

	/// <summary>Entity ID of the fill element</summary>
	public ulong FillEntity;

	/// <summary>Entity ID of the track/background element</summary>
	public ulong TrackEntity;

	/// <summary>Entity ID of the text label element (optional, shows percentage)</summary>
	public ulong LabelEntity;

	/// <summary>Whether to show percentage text</summary>
	public bool ShowPercentage;

	public ProgressBar(float initialProgress = 0f, ProgressBarDirection direction = ProgressBarDirection.LeftToRight, bool showPercentage = false)
	{
		Progress = Math.Clamp(initialProgress, 0f, 1f);
		Direction = direction;
		FillEntity = 0;
		TrackEntity = 0;
		LabelEntity = 0;
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
			Query<Data<UiNode>> allNodes,
			Query<Data<UiText>> textElements) =>
		{
			UpdateProgressBarVisuals(commands, changedProgressBars, allNodes, textElements);
		})
		.InStage(Stage.PreUpdate)
		.Label("progressbar:update-visuals")
		.Build();
	}

	/// <summary>
	/// Updates the fill size and text label based on progress value.
	/// </summary>
	private static void UpdateProgressBarVisuals(
		Commands commands,
		Query<Data<ProgressBar>, Filter<Changed<ProgressBar>>> changedProgressBars,
		Query<Data<UiNode>> allNodes,
		Query<Data<UiText>> textElements)
	{
		foreach (var (entityId, progressBar) in changedProgressBars)
		{
			ref readonly var pb = ref progressBar.Ref;

			// Update fill size based on direction
			if (pb.FillEntity != 0 && allNodes.Contains(pb.FillEntity))
			{
				var (_, fillNode) = allNodes.Get(pb.FillEntity);
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
				commands.Entity(pb.FillEntity).Insert(fill);
			}

			// Update text label if ShowPercentage is enabled
			if (pb.ShowPercentage && pb.LabelEntity != 0 && textElements.Contains(pb.LabelEntity))
			{
				var (_, textElement) = textElements.Get(pb.LabelEntity);
				ref var text = ref textElement.Ref;
				text.Value = $"{pb.GetPercentage()}%";
				commands.Entity(pb.LabelEntity).Insert(text);
			}
		}
	}
}
