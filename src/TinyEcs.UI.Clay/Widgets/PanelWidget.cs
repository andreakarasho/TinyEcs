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
		ushort padding = 12,
		ushort cornerRadius = 8)
	{
		var bgColor = backgroundColor ?? new Clay_Color(45, 50, 55, 255);

		// Panel container
		var panelBuilder = ClayNode.Configure();

		if (width > 0)
			panelBuilder = panelBuilder.Width(width);
		else
			panelBuilder = panelBuilder.WidthGrow();

		if (height > 0)
			panelBuilder = panelBuilder.Height(height);
		else
			panelBuilder = panelBuilder.HeightGrow();

		panelBuilder = panelBuilder
			.Column()
			.Padding(padding)
			.Gap(8)
			.Background(bgColor)
			.Border(new Clay_Color(70, 75, 80, 255), 1)
			.CornerRadius(cornerRadius);

		var panelNode = panelBuilder.Build();

		var panel = commands.SpawnClayElement(panelNode);
		parent.AddChild(panel);

		// Optional title
		if (!string.IsNullOrEmpty(title))
		{
			var titleNode = ClayNode.Configure()
				.WidthGrow()
				.HeightFit(0, 0)
				.Text(title, 18, new Clay_Color(220, 220, 230, 255))
				.Build();

			var titleElement = commands.SpawnClayElement(titleNode);
			panel.AddChild(titleElement);
		}

		return panel;
	}
}
