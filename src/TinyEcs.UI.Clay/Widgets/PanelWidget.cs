using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track panel state for scrolling.
/// </summary>
public struct PanelState
{
	public ulong ContentWrapperEntityId;  // The viewport (visible area)
	public ulong ContentAreaEntityId;     // The scrollable content
	public ulong VerticalScrollbarId;
	public ulong HorizontalScrollbarId;
	public float ContentWidth;
	public float ContentHeight;
	public bool EnableVerticalScrolling;
	public bool EnableHorizontalScrolling;
}

/// <summary>
/// Extension methods for creating panel/container widgets.
/// </summary>
public static class PanelWidget
{
	/// <summary>
	/// Creates a panel/container widget with optional title and scrolling support.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the panel to</param>
	/// <param name="title">Optional panel title</param>
	/// <param name="width">Panel width (0 for Grow)</param>
	/// <param name="height">Panel height (0 for Grow)</param>
	/// <param name="backgroundColor">Panel background color</param>
	/// <param name="padding">Panel padding</param>
	/// <param name="cornerRadius">Corner radius for rounded corners</param>
	/// <param name="enableVerticalScrolling">Enable vertical scrollbar when content exceeds bounds</param>
	/// <param name="enableHorizontalScrolling">Enable horizontal scrollbar when content exceeds bounds</param>
	/// <returns>The panel content area entity ID for adding children</returns>
	public static EntityCommands CreatePanel(
		this Commands commands,
		EntityCommands parent,
		string? title = null,
		float width = 0f,
		float height = 0f,
		Clay_Color? backgroundColor = null,
		ushort padding = 12,
		ushort cornerRadius = 8,
		bool enableVerticalScrolling = false,
		bool enableHorizontalScrolling = false)
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

		// Content area (for scrolling support)
		EntityCommands contentArea;

		if (enableVerticalScrolling || enableHorizontalScrolling)
		{
			// Create container for content + scrollbars layout
			// Layout: Row with [Content Wrapper] [Vertical Scrollbar]
			var scrollableRowNode = ClayNode.Configure()
				.WidthGrow()
				.HeightGrow()
				.Row()
				.Gap(0)
				.Build();

			var scrollableRow = commands.SpawnClayElement(scrollableRowNode);
			panel.AddChild(scrollableRow);

			// Create a scrollable content wrapper with fixed viewport size
			// This is the visible area that shows part of the content
			var contentWrapperNode = ClayNode.Default with
			{
				Layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
					layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
				},
				Clip = new Clay_ClipElementConfig
				{
					horizontal = enableHorizontalScrolling,
					vertical = enableVerticalScrolling,
					childOffset = new Clay_Vector2 { x = 0, y = 0 }
				}
			};

			var contentWrapper = commands.SpawnClayElement(contentWrapperNode);
			scrollableRow.AddChild(contentWrapper);

			// Create the actual content area with scroll container
			// This will size to fit its children (potentially larger than wrapper)
			var contentNode = ClayNode.Configure()
				.WidthFit()
				.HeightFit()
				.Column()
				.Gap(4)
				.Build();

			contentArea = commands.SpawnClayElement(contentNode);
			contentWrapper.AddChild(contentArea);

			// Add scroll container component
			commands.Entity(contentArea.Id).Insert(new ClayScrollContainer
			{
				ScrollOffset = System.Numerics.Vector2.Zero
			});

			// Create scrollbars
			ulong verticalScrollbarId = 0;
			ulong horizontalScrollbarId = 0;

			if (enableVerticalScrolling)
			{
				// Vertical scrollbar as sibling to content wrapper (on the right)
				verticalScrollbarId = commands.CreateVerticalScrollbar(
					scrollableRow,
					contentWrapper.Id,  // Viewport for mouse wheel detection
					contentArea.Id,     // Content area to scroll
					contentSize: 1000,  // Large initial value to ensure visibility
					visibleSize: 100,   // Small viewport to ensure scrollbar shows
					initialScroll: 0f
				);
			}

			if (enableHorizontalScrolling)
			{
				// Horizontal scrollbar below the row (full width)
				horizontalScrollbarId = commands.CreateHorizontalScrollbar(
					panel,
					contentWrapper.Id,  // Viewport for mouse wheel detection
					contentArea.Id,     // Content area to scroll
					contentSize: 1000,  // Large initial value to ensure visibility
					visibleSize: 100,   // Small viewport to ensure scrollbar shows
					initialScroll: 0f
				);
			}

			// Add panel state to track the content area
			commands.Entity(panel.Id).Insert(new PanelState
			{
				ContentWrapperEntityId = contentWrapper.Id,
				ContentAreaEntityId = contentArea.Id,
				VerticalScrollbarId = verticalScrollbarId,
				HorizontalScrollbarId = horizontalScrollbarId,
				ContentWidth = 0,
				ContentHeight = 0,
				EnableVerticalScrolling = enableVerticalScrolling,
				EnableHorizontalScrolling = enableHorizontalScrolling
			});
		}
		else
		{
			// No scrolling - just return the panel itself
			contentArea = panel;
		}

		return contentArea;
	}
}
