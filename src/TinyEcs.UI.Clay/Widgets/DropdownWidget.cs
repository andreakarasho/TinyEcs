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
	public float OptionHeight;
	public Clay_Color BackgroundColor;
	public Clay_Color TextColor;
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
	/// Creates a dropdown/select widget.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the dropdown to</param>
	/// <param name="label">Dropdown label</param>
	/// <param name="options">Available options</param>
	/// <param name="defaultIndex">Initially selected option index (default 0)</param>
	/// <param name="width">Dropdown width in pixels (default 200)</param>
	/// <param name="height">Dropdown button height in pixels (default 36)</param>
	/// <param name="backgroundColor">Button background color</param>
	/// <param name="textColor">Text color</param>
	/// <returns>The dropdown container entity ID</returns>
	public static ulong CreateDropdown(
		this Commands commands,
		EntityCommands parent,
		string label,
		string[] options,
		int defaultIndex = 0,
		float width = 200f,
		float height = 36f,
		Clay_Color? backgroundColor = null,
		Clay_Color? textColor = null)
	{
		if (options == null || options.Length == 0)
			throw new ArgumentException("Dropdown must have at least one option", nameof(options));

		defaultIndex = Math.Clamp(defaultIndex, 0, options.Length - 1);

		var bgColor = backgroundColor ?? new Clay_Color(60, 65, 70, 255);
		var txtColor = textColor ?? new Clay_Color(220, 220, 220, 255);

		// Container for the entire dropdown
		var containerNode = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Fixed(width),
					Clay_SizingAxis.Fixed(height)
				),
				layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
				childGap = 0
			}
		};

		var container = commands.SpawnClayElement(containerNode);
		parent.AddChild(container);

		// Button that shows selected value and opens dropdown
		var buttonNode = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Grow(),
					Clay_SizingAxis.Grow()
				),
				layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
				padding = Clay_Padding.All(8),
				childAlignment = new Clay_ChildAlignment(
					Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
					Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
				),
				childGap = 8
			},
			Rectangle = new Clay_RectangleRenderData
			{
				backgroundColor = bgColor
			},
			CornerRadius = Clay_CornerRadius.All(4),
			Border = new Clay_BorderElementConfig
			{
				color = new Clay_Color(100, 105, 110, 255),
				width = new Clay_BorderWidth { left = 1, right = 1, top = 1, bottom = 1 }
			}
		};

		var button = commands.SpawnClayElement(buttonNode);
		container.AddChild(button);

		// Button text (selected value)
		var buttonTextNode = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Grow(),
					Clay_SizingAxis.Grow()
				)
			},
			Text = new ClayText
			{
				Text = $"{label}: {options[defaultIndex]}",
				Config = new Clay_TextElementConfig
				{
					fontSize = 16,
					textColor = txtColor
				}
			}
		};

		var buttonText = commands.SpawnClayElement(buttonTextNode);
		button.AddChild(buttonText);

		// Dropdown arrow indicator
		var arrowNode = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Fixed(20),
					Clay_SizingAxis.Fixed(20)
				)
			},
			Text = new ClayText
			{
				Text = "▼",
				Config = new Clay_TextElementConfig
				{
					fontSize = 12,
					textColor = txtColor
				}
			}
		};

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
			Width = width,
			OptionHeight = 32f,
			BackgroundColor = bgColor,
			TextColor = txtColor
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
		var menuNode = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Fixed(state.Width),
					Clay_SizingAxis.Fit(0, 0)
				),
				layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
				childGap = 0
			},
			Rectangle = new Clay_RectangleRenderData
			{
				backgroundColor = state.BackgroundColor
			},
			CornerRadius = Clay_CornerRadius.All(4),
			Border = new Clay_BorderElementConfig
			{
				color = new Clay_Color(100, 105, 110, 255),
				width = new Clay_BorderWidth { left = 1, right = 1, top = 1, bottom = 1 }
			},
			Floating = new Clay_FloatingElementConfig
			{
				offset = new Clay_Vector2 { x = 0, y = 2 },
				zIndex = 100,
				attachPoints = new Clay_FloatingAttachPoints
				{
					element = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP,
					parent = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_BOTTOM
				},
				attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT
			}
		};

		var menu = commands.SpawnClayElement(menuNode);
		commands.Entity(containerEntityId).AddChild(menu);

		// Create option items
		for (int i = 0; i < state.Options.Length; i++)
		{
			int optionIndex = i; // Capture for closure
			var isSelected = i == state.SelectedIndex;

			var optionNode = ClayNode.Default with
			{
				Layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Grow(),
						Clay_SizingAxis.Grow()
					),
					layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
					padding = Clay_Padding.All(8),
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
					)
				},
				Rectangle = new Clay_RectangleRenderData
				{
					backgroundColor = isSelected
						? new Clay_Color(80, 120, 200, 255)
						: new Clay_Color(60, 65, 70, 255)
				}
			};

			var option = commands.SpawnClayElement(optionNode);
			menu.AddChild(option);

			// Option text
			var optionTextNode = ClayNode.Default with
			{
				Layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Grow(),
						Clay_SizingAxis.Grow()
					)
				},
				Text = new ClayText
				{
					Text = state.Options[i],
					Config = new Clay_TextElementConfig
					{
						fontSize = 16,
						textColor = state.TextColor
					}
				}
			};

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
