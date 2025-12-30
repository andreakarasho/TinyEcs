using System.Collections.Generic;
using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Dropdown widget plugin - Provides a button that opens a list of selectable options.
/// Similar to Sickle UI's Dropdown widget.
/// </summary>
public struct DropdownPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		app.AddSystem((
			Commands commands,
			Query<Data<Dropdown, DropdownOptions>, Filter<Changed<Dropdown>>> dropdowns,
			Query<Data<Parent, UiText>, Filter<With<DropdownLabel>>> labels) =>
		{
			UpdateDropdownLabel(commands, dropdowns, labels);
		})
		.InStage(Stage.Update)
		.Label("ui:dropdown:update_label")
		.Build();

		app.AddSystem((
			Commands commands,
			Query<Data<Dropdown, InteractionState>, Filter<Changed<InteractionState>>> dropdowns) =>
		{
			HandleDropdownClick(commands, dropdowns);
		})
		.InStage(Stage.Update)
		.Label("ui:dropdown:handle_click")
		.After("ui:dropdown:update_label")
		.Build();

		app.AddSystem((
			Commands commands,
			Query<Data<DropdownOption, InteractionState>, Filter<Changed<InteractionState>>> options,
			Query<Data<Dropdown>> dropdowns) =>
		{
			HandleOptionClick(commands, options, dropdowns);
		})
		.InStage(Stage.Update)
		.Label("ui:dropdown:handle_option")
		.After("ui:dropdown:handle_click")
		.Build();

		app.AddSystem((
			Commands commands,
			Res<PointerInputState> pointerInput,
			Query<Data<Dropdown>> dropdowns,
			Query<Data<DropdownPanel, ComputedLayout>> panels,
			Query<Data<ComputedLayout>> layouts) =>
		{
			HandleClickOutside(commands, pointerInput, dropdowns, panels, layouts);
		})
		.InStage(Stage.Update)
		.Label("ui:dropdown:click_outside")
		.After("ui:pointer:hit-test")
		.Build();

		app.AddSystem((
			Commands commands,
			Query<Data<DropdownPanel, FloatingPanel>> panels,
			Query<Data<Dropdown>, Filter<Changed<Dropdown>>> dropdowns,
			Query<Data<UiNode>> nodes,
			Query<Data<ComputedLayout>> layouts) =>
		{
			UpdateDropdownPanelVisibility(commands, panels, dropdowns, nodes, layouts);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:dropdown:update_visibility")
		.Build();

		// System to update floating panel position when dropdown button is scrolled
		app.AddSystem((
			Commands commands,
			Query<Data<Dropdown>> dropdowns,
			Query<Data<DropdownPanel, FloatingPanel>> panels,
			Query<Data<ComputedLayout>, Filter<Changed<ComputedLayout>>> changedLayouts) =>
		{
			UpdateDropdownPanelPosition(commands, dropdowns, panels, changedLayouts);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:dropdown:update_position_on_scroll")
		.After("flexbox:read_layout")
		.Build();
	}

	/// <summary>
	/// Updates the dropdown button label when the selected value changes.
	/// Finds label by looking for child entities with DropdownLabel marker.
	/// </summary>
	private static void UpdateDropdownLabel(
		Commands commands,
		Query<Data<Dropdown, DropdownOptions>, Filter<Changed<Dropdown>>> dropdowns,
		Query<Data<Parent, UiText>, Filter<With<DropdownLabel>>> labels)
	{
		foreach (var (dropdownEntityId, dropdown, options) in dropdowns)
		{
			ref var d = ref dropdown.Ref;
			var dropdownId = dropdownEntityId.Ref;

			// Validate selected value
			if (d.Value.HasValue && d.Value.Value >= options.Ref.Options.Count)
			{
				d.Value = null;
			}

			// Find and update the label
			foreach (var (labelEntityId, parent, text) in labels)
			{
				if (parent.Ref.Id != dropdownId)
					continue;

				ref var t = ref text.Ref;

				// Update label text
				t.Value = d.Value.HasValue
					? options.Ref.Options[d.Value.Value]
					: "---";

				// Re-insert to trigger change detection
				commands.Entity(labelEntityId.Ref).Insert(t);
				break;
			}
		}
	}

	/// <summary>
	/// Handles dropdown button clicks to toggle open/closed state.
	/// </summary>
	private static void HandleDropdownClick(
		Commands commands,
		Query<Data<Dropdown, InteractionState>, Filter<Changed<InteractionState>>> dropdowns)
	{
		ulong? openDropdownId = null;

		// Find which dropdown was pressed
		foreach (var (entityId, dropdown, interaction) in dropdowns)
		{
			if (interaction.Ref.State == Interaction.Pressed)
			{
				openDropdownId = entityId.Ref;
				break;
			}
		}

		// If no dropdown was clicked, nothing to do
		if (!openDropdownId.HasValue)
			return;

		// Toggle dropdowns
		foreach (var (entityId, dropdown, _) in dropdowns)
		{
			ref var d = ref dropdown.Ref;

			if (entityId.Ref == openDropdownId.Value)
			{
				// Toggle the clicked dropdown
				d.IsOpen = !d.IsOpen;
				commands.Entity(entityId.Ref).Insert(d);
			}
			else if (d.IsOpen)
			{
				// Close other dropdowns
				d.IsOpen = false;
				commands.Entity(entityId.Ref).Insert(d);
			}
		}
	}

	/// <summary>
	/// Handles option selection when an option is pressed.
	/// </summary>
	private static void HandleOptionClick(
		Commands commands,
		Query<Data<DropdownOption, InteractionState>, Filter<Changed<InteractionState>>> options,
		Query<Data<Dropdown>> dropdowns)
	{
		foreach (var (optionEntityId, option, interaction) in options)
		{
			if (interaction.Ref.State != Interaction.Pressed)
				continue;

			if (!dropdowns.Contains(option.Ref.DropdownEntity))
				continue;

			var (_, dropdown) = dropdowns.Get(option.Ref.DropdownEntity);
			ref var d = ref dropdown.Ref;

			// Update dropdown value and close panel
			d.Value = option.Ref.OptionIndex;
			d.IsOpen = false;

			commands.Entity(option.Ref.DropdownEntity).Insert(d);
		}
	}

	/// <summary>
	/// Closes open dropdowns when clicking outside their bounds.
	/// </summary>
	private static void HandleClickOutside(
		Commands commands,
		Res<PointerInputState> pointerInput,
		Query<Data<Dropdown>> dropdowns,
		Query<Data<DropdownPanel, ComputedLayout>> panels,
		Query<Data<ComputedLayout>> layouts)
	{
		// Only check on mouse press (button down)
		if (!pointerInput.Value.IsPrimaryButtonPressed)
			return;

		var clickPos = pointerInput.Value.Position;

		// Check each open dropdown
		foreach (var (dropdownEntityId, dropdown) in dropdowns)
		{
			if (!dropdown.Ref.IsOpen)
				continue;

			// Check if click was inside the dropdown button
			if (layouts.Contains(dropdownEntityId.Ref))
			{
				var (_, buttonLayout) = layouts.Get(dropdownEntityId.Ref);
				if (IsPointInRect(clickPos, buttonLayout.Ref.X, buttonLayout.Ref.Y,
					buttonLayout.Ref.Width, buttonLayout.Ref.Height))
				{
					// Click was on the button itself, let HandleDropdownClick handle it
					continue;
				}
			}

			// Check if click was inside the dropdown panel
			bool clickedInside = false;
			foreach (var (panelEntityId, panel, panelLayout) in panels)
			{
				if (panel.Ref.DropdownEntity == dropdownEntityId.Ref)
				{
					if (IsPointInRect(clickPos, panelLayout.Ref.X, panelLayout.Ref.Y,
						panelLayout.Ref.Width, panelLayout.Ref.Height))
					{
						clickedInside = true;
						break;
					}
				}
			}

			// If click was outside both button and panel, close the dropdown
			if (!clickedInside)
			{
				ref var d = ref dropdown.Ref;
				d.IsOpen = false;
				commands.Entity(dropdownEntityId.Ref).Insert(d);
			}
		}
	}

	/// <summary>
	/// Helper method to check if a point is inside a rectangle.
	/// </summary>
	private static bool IsPointInRect(Vector2 point, float x, float y, float width, float height)
	{
		return point.X >= x && point.X <= x + width &&
		       point.Y >= y && point.Y <= y + height;
	}

	/// <summary>
	/// Updates the visibility of dropdown panels based on open state.
	/// Uses FloatingPanel to show/hide panels and set priority.
	/// </summary>
	private static void UpdateDropdownPanelVisibility(
		Commands commands,
		Query<Data<DropdownPanel, FloatingPanel>> panels,
		Query<Data<Dropdown>, Filter<Changed<Dropdown>>> dropdowns,
		Query<Data<UiNode>> nodes,
		Query<Data<ComputedLayout>> layouts)
	{
		// Iterate through changed dropdowns and update their panels
		foreach (var (dropdownEntityId, dropdown) in dropdowns)
		{
			// Find the panel for this dropdown
			ulong? panelId = null;
			foreach (var (panelEntityId, panel, _) in panels)
			{
				if (panel.Ref.DropdownEntity == dropdownEntityId.Ref)
				{
					panelId = panelEntityId.Ref;
					break;
				}
			}

			if (!panelId.HasValue)
				continue;

			// Get the panel's FloatingPanel component
			var (_, _, floatingPanel) = panels.Get(panelId.Value);
			ref var fp = ref floatingPanel.Ref;

			// Update visibility and priority
			if (dropdown.Ref.IsOpen)
			{
				// Show panel with priority (highest Z-index)
				fp.Priority = true;

				// Get dropdown layout to position panel below it
				if (layouts.Contains(dropdownEntityId.Ref))
				{
					var (_, dropdownLayout) = layouts.Get(dropdownEntityId.Ref);
					ref readonly var layout = ref dropdownLayout.Ref;

					// Position panel below the dropdown button
					fp.Position = new Vector2(layout.X, layout.Y + layout.Height);
				}

				// Re-insert to trigger FloatingPanel update
				commands.Entity(panelId.Value).Insert(fp);

				// Set display to visible
				if (nodes.Contains(panelId.Value))
				{
					var (_, node) = nodes.Get(panelId.Value);
					ref var n = ref node.Ref;
					n.Display = Display.Flex;
					commands.Entity(panelId.Value).Insert(n);
				}
			}
			else
			{
				// Hide panel and remove priority
				fp.Priority = false;
				commands.Entity(panelId.Value).Insert(fp);

				// Set display to hidden
				if (nodes.Contains(panelId.Value))
				{
					var (_, node) = nodes.Get(panelId.Value);
					ref var n = ref node.Ref;
					n.Display = Display.None;
					commands.Entity(panelId.Value).Insert(n);
				}
			}
		}
	}

	/// <summary>
	/// Updates the position of open dropdown panels when their button's layout changes (e.g., due to scrolling).
	/// This ensures the floating panel follows the dropdown button.
	/// </summary>
	private static void UpdateDropdownPanelPosition(
		Commands commands,
		Query<Data<Dropdown>> dropdowns,
		Query<Data<DropdownPanel, FloatingPanel>> panels,
		Query<Data<ComputedLayout>, Filter<Changed<ComputedLayout>>> changedLayouts)
	{
		// For each entity whose layout changed
		foreach (var (layoutEntityId, layout) in changedLayouts)
		{
			// Check if this entity is a dropdown button
			if (!dropdowns.Contains(layoutEntityId.Ref))
				continue;

			var (_, dropdown) = dropdowns.Get(layoutEntityId.Ref);

			// Only update position if dropdown is currently open
			if (!dropdown.Ref.IsOpen)
				continue;

			// Find the floating panel for this dropdown
			foreach (var (panelEntityId, panel, floatingPanel) in panels)
			{
				if (panel.Ref.DropdownEntity == layoutEntityId.Ref)
				{
					// Update panel position to follow the button
					ref var fp = ref floatingPanel.Ref;
					ref readonly var dropdownLayout = ref layout.Ref;

					// Position panel below the dropdown button using current screen position
					fp.Position = new Vector2(dropdownLayout.X, dropdownLayout.Y + dropdownLayout.Height);

					// Re-insert to trigger FloatingPanel update
					commands.Entity(panelEntityId.Ref).Insert(fp);
					break;
				}
			}
		}
	}
}

/// <summary>
/// Component that stores the list of options for a dropdown.
/// </summary>
public struct DropdownOptions
{
	public List<string> Options;

	public DropdownOptions(List<string> options)
	{
		Options = options ?? new List<string>();
	}

	public DropdownOptions(params string[] options)
	{
		Options = new List<string>(options);
	}
}

/// <summary>
/// Component that marks an entity as a dropdown option and links it to its parent dropdown.
/// </summary>
public struct DropdownOption
{
	public ulong DropdownEntity;
	public int OptionIndex;

	public DropdownOption(ulong dropdownEntity, int optionIndex)
	{
		DropdownEntity = dropdownEntity;
		OptionIndex = optionIndex;
	}
}

/// <summary>
/// Component that marks an entity as a dropdown panel (the popup containing options).
/// </summary>
public struct DropdownPanel
{
	public ulong DropdownEntity;

	public DropdownPanel(ulong dropdownEntity)
	{
		DropdownEntity = dropdownEntity;
	}
}

/// <summary>
/// Component that represents a dropdown widget.
/// Contains the selected value and open state.
/// Child elements are identified by marker components:
/// - DropdownLabel: the label text showing the selected value
/// Panel and options use back-references via DropdownPanel.DropdownEntity and DropdownOption.DropdownEntity
/// </summary>
public struct Dropdown
{
	/// <summary>Selected option index (null if no selection)</summary>
	public int? Value;

	/// <summary>Whether the dropdown is currently open</summary>
	public bool IsOpen;

	public Dropdown(int? initialValue = null)
	{
		Value = initialValue;
		IsOpen = false;
	}
}

/// <summary>
/// Marker component for the dropdown button label.
/// Used to identify the text element that shows the selected value.
/// </summary>
public struct DropdownLabel { }
