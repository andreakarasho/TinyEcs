using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track dropdown state and menu configuration.
/// </summary>
public struct DropdownState
{
	public ulong ButtonEntityId;      // The button that opens/closes dropdown
	public ulong ButtonTextEntityId;  // The button text element (for updates)
	public ulong MenuEntityId;        // The floating menu container (0 if not spawned)
	public bool IsOpen;               // Whether dropdown is currently open
	public int SelectedIndex;         // Currently selected option index
	public string[] Options;          // Available options
	public string Label;              // Dropdown label

	// Menu configuration
	public float Width;
	public Clay_Color BackgroundColor;
	public Clay_Color BorderColor;
	public Clay_Color TextColor;
	public Clay_Color ItemBackgroundColor;
	public Clay_Color ItemHoverColor;
}

/// <summary>
/// Marker component to update dropdown button text.
/// </summary>
public struct DropdownButtonUpdate
{
	public string Text;
}

/// <summary>
/// Event fired when dropdown selection changes.
/// </summary>
public struct DropdownValueChanged
{
	public int SelectedIndex;
	public string SelectedValue;
}

/// <summary>
/// Extension methods for creating dropdown widgets.
/// </summary>
public static class DropdownWidget
{
	/// <summary>
	/// Creates a dropdown/select widget using theme colors.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the dropdown to</param>
	/// <param name="theme">Theme resource for styling</param>
	/// <param name="label">Dropdown label</param>
	/// <param name="options">Available options</param>
	/// <param name="defaultIndex">Initially selected option index (default 0)</param>
	/// <param name="width">Dropdown width in pixels (0 = use default 200)</param>
	/// <returns>The dropdown container entity ID</returns>
	public static ulong CreateDropdown(
		this Commands commands,
		EntityCommands parent,
		ClayTheme theme,
		string label,
		string[] options,
		int defaultIndex = 0,
		float width = 0f)
	{
		if (options == null || options.Length == 0)
			throw new ArgumentException("Dropdown must have at least one option", nameof(options));

		defaultIndex = Math.Clamp(defaultIndex, 0, options.Length - 1);

		var dropdownTheme = theme.Dropdown;
		var actualWidth = width > 0 ? width : 200f;
		var height = dropdownTheme.Height;

		// Container for the entire dropdown
		var containerNode = ClayNode.Configure()
			.Size(actualWidth, height)
			.Column()
			.Gap(0)
			.Build();

		var container = commands.SpawnClayElement(containerNode);
		parent.AddChild(container);

		// Button that shows selected value and opens dropdown
		var buttonNode = ClayNode.Configure()
			.WidthGrow()
			.HeightGrow()
			.Row()
			.Padding(8)
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Gap(8)
			.Background(dropdownTheme.ButtonBackgroundColor)
			.CornerRadius(dropdownTheme.CornerRadius)
			.Border(dropdownTheme.MenuBorderColor, dropdownTheme.BorderWidth)
			.Build();

		var button = commands.SpawnClayElement(buttonNode);
		container.AddChild(button);

		// Button text (selected value)
		var buttonTextNode = ClayNode.Configure()
			.WidthGrow()
			.HeightGrow()
			.Text($"{label}: {options[defaultIndex]}", theme.Typography.DefaultFontSize, dropdownTheme.ButtonTextColor)
			.Build();

		var buttonText = commands.SpawnClayElement(buttonTextNode);
		button.AddChild(buttonText);

		// Dropdown arrow indicator
		var arrowNode = ClayNode.Configure()
			.Size(20, 20)
			.Text("▼", 12, dropdownTheme.ButtonTextColor)
			.Build();

		var arrow = commands.SpawnClayElement(arrowNode);
		button.AddChild(arrow);

		// Add dropdown state component (menu will be spawned when opened)
		commands.Entity(container.Id).Insert(new DropdownState
		{
			ButtonEntityId = button.Id,
			ButtonTextEntityId = buttonText.Id,
			MenuEntityId = 0, // Not spawned yet
			IsOpen = false,
			SelectedIndex = defaultIndex,
			Options = options,
			Label = label,
			Width = actualWidth,
			BackgroundColor = dropdownTheme.MenuBackgroundColor,
			BorderColor = dropdownTheme.MenuBorderColor,
			TextColor = dropdownTheme.ItemTextColor,
			ItemBackgroundColor = dropdownTheme.ItemBackgroundColor,
			ItemHoverColor = dropdownTheme.ItemHoverColor
		});

		// Capture IDs for observer closures
		var containerIdForObserver = container.Id;

		// Add click handler for button to toggle dropdown
		button.Observe<On<ClayPointerEvent>, Commands>((trigger, cmd) =>
		{
			var evt = trigger.Event;
			if (evt.EventType == ClayPointerEventType.Click)
			{
				// Stop propagation
				trigger.Propagate(false);

				// Toggle dropdown open state
				cmd.Entity(containerIdForObserver).EmitTrigger(new DropdownToggleRequested());
			}
		});

		return container.Id;
	}

	/// <summary>
	/// Internal helper to spawn the dropdown menu entity with all options.
	/// </summary>
	internal static ulong SpawnDropdownMenu(
		Commands commands,
		ulong containerEntityId,
		DropdownState state)
	{
		// Floating menu container
		var menuNode = ClayNode.Configure()
			.Width(state.Width)
			.HeightFit(0, 0)
			.Column()
			.Gap(0)
			.Background(state.BackgroundColor)
			.CornerRadius(4)
			.Border(state.BorderColor, 1)
			.Floating(100)
			.FloatingOffset(0, 2)
			.FloatingAttachPoints(
				Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP,
				Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_BOTTOM)
			.Build();

		var menu = commands.SpawnClayElement(menuNode);
		commands.Entity(containerEntityId).AddChild(menu);

		// Create option items
		for (int i = 0; i < state.Options.Length; i++)
		{
			int optionIndex = i; // Capture for closure
			var isSelected = i == state.SelectedIndex;

			var optionNode = ClayNode.Configure()
				.WidthGrow()
				.HeightGrow()
				.Row()
				.Padding(8)
				.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
				.Background(isSelected
					? state.ItemHoverColor        // Use themed hover color for selected item
					: state.ItemBackgroundColor)  // Use themed background color for normal items
				.Build();

			var option = commands.SpawnClayElement(optionNode);
			menu.AddChild(option);

			// Option text
			var optionTextNode = ClayNode.Configure()
				.WidthGrow()
				.HeightGrow()
				.Text(state.Options[i], 16, state.TextColor)
				.Build();

			var optionText = commands.SpawnClayElement(optionTextNode);
			option.AddChild(optionText);

			// Add click handler to BOTH option and text to ensure clicks are captured
			// regardless of where the user clicks (padding or text area)
			option.Observe<On<ClayPointerEvent>, Commands>((trigger, cmd) =>
			{
				var evt = trigger.Event;
				if (evt.EventType == ClayPointerEventType.Click)
				{
					trigger.Propagate(false);
					cmd.Entity(containerEntityId).EmitTrigger(new DropdownValueChanged
					{
						SelectedIndex = optionIndex,
						SelectedValue = state.Options[optionIndex]
					});
				}
			});

			optionText.Observe<On<ClayPointerEvent>, Commands>((trigger, cmd) =>
			{
				var evt = trigger.Event;
				if (evt.EventType == ClayPointerEventType.Click)
				{
					trigger.Propagate(false);
					cmd.Entity(containerEntityId).EmitTrigger(new DropdownValueChanged
					{
						SelectedIndex = optionIndex,
						SelectedValue = state.Options[optionIndex]
					});
				}
			});
		}

		return menu.Id;
	}
}

/// <summary>
/// Internal event to request dropdown toggle.
/// </summary>
public struct DropdownToggleRequested { }
