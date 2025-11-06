using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Extension methods for creating panel/container widgets.
/// </summary>
public static class PanelWidget
{
	/// <summary>
	/// Creates a panel/container widget with optional title.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the panel to</param>
	/// <param name="title">Optional panel title</param>
	/// <param name="width">Panel width (0 for Grow)</param>
	/// <param name="height">Panel height (0 for Grow)</param>
	/// <param name="backgroundColor">Panel background color</param>
	/// <param name="padding">Panel padding</param>
	/// <param name="cornerRadius">Corner radius for rounded corners</param>
	/// <returns>The panel entity ID for adding children</returns>
	public static EntityCommands CreatePanel(
		this Commands commands,
		EntityCommands parent,
		string? title = null,
		float width = 0f,
		float height = 0f,
		Clay_Color? backgroundColor = null,
		float padding = 12f,
		ushort cornerRadius = 8)
	{
		var bgColor = backgroundColor ?? new Clay_Color(45, 50, 55, 255);

		var sizing = new Clay_Sizing(
			width > 0 ? Clay_SizingAxis.Fixed(width) : Clay_SizingAxis.Grow(),
			height > 0 ? Clay_SizingAxis.Fixed(height) : Clay_SizingAxis.Grow()
		);

		// Panel container
		var panelNode = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = sizing,
				layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
				padding = Clay_Padding.All((ushort)padding),
				childGap = 8
			},
			Rectangle = new Clay_RectangleRenderData
			{
				backgroundColor = bgColor
			},
			Border = new Clay_BorderElementConfig
			{
				color = new Clay_Color(70, 75, 80, 255),
				width = new Clay_BorderWidth { left = 1, right = 1, top = 1, bottom = 1 }
			},
			CornerRadius = Clay_CornerRadius.All(cornerRadius)
		};

		var panel = commands.SpawnClayElement(panelNode);
		parent.AddChild(panel);

		// Optional title
		if (!string.IsNullOrEmpty(title))
		{
			var titleNode = ClayNode.Default with
			{
				Layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Grow(),
						Clay_SizingAxis.Fit(0, 0)
					)
				},
				Text = new ClayText
				{
					Text = title,
					Config = new Clay_TextElementConfig
					{
						fontSize = 18,
						textColor = new Clay_Color(220, 220, 230, 255)
					}
				}
			};

			var titleElement = commands.SpawnClayElement(titleNode);
			panel.AddChild(titleElement);
		}

		return panel;
	}
}
