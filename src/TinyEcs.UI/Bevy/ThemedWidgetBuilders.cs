using System;
using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Helper methods for building widgets with UiTheme styling applied.
/// These builders provide a convenient way to create consistently-styled widgets
/// across your application.
///
/// Usage:
/// <code>
/// // In a startup system with access to theme
/// app.AddSystem((Commands commands, Res&lt;UiTheme&gt; theme) =>
/// {
///     var button = ThemedWidgetBuilders.CreateButton(commands, theme.Value, "Click Me");
///     var slider = ThemedWidgetBuilders.CreateSlider(commands, theme.Value, 0f, 100f, 50f);
/// });
/// </code>
/// </summary>
public static class ThemedWidgetBuilders
{
	// ============================================
	// Button
	// ============================================

	/// <summary>
	/// Creates a themed button with text label.
	/// </summary>
	public static ulong CreateButton(Commands commands, UiTheme theme, string label)
	{
		var buttonId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(theme.ButtonWidth),
				Height = FlexValue.Points(theme.ButtonHeight),
				JustifyContent = Justify.Center,
				AlignItems = Align.Center,
				PaddingLeft = FlexValue.Points(theme.Padding),
				PaddingRight = FlexValue.Points(theme.Padding),
			})
			.Insert(theme.Surface.ToBackgroundColor())
			.Insert(theme.Border.ToBorderColor())
			.Insert(new BorderRadius(theme.BorderRadius))
			.Insert(new Button())
			.Insert(new Interactive(focusable: false))
			.Insert(new InteractionState())
			.Insert(new UiText(label))
			.Insert(new TextStyle(fontSize: theme.ButtonFontSize, color: theme.Text))
			.Id;

		return buttonId;
	}

	// ============================================
	// Slider
	// ============================================

	/// <summary>
	/// Creates a themed slider with specified range and initial value.
	/// Returns the slider track entity ID.
	/// </summary>
	public static ulong CreateSlider(Commands commands, UiTheme theme, float min, float max, float initialValue, SliderDirection direction = SliderDirection.Horizontal)
	{
		var isHorizontal = direction == SliderDirection.Horizontal;

		// Create slider track (background)
		var trackId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = isHorizontal ? FlexValue.Points(200f) : FlexValue.Points(theme.SliderTrackSize),
				Height = isHorizontal ? FlexValue.Points(theme.SliderTrackSize) : FlexValue.Points(200f),
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Center,
				PositionType = PositionType.Relative,
				MarginBottom = FlexValue.Points(theme.Gap),
			})
			.Insert(theme.SliderTrackColor.ToBackgroundColor())
			.Insert(new BorderRadius(theme.SliderTrackSize / 2f))
			.Insert(new Interactive(focusable: false))
			.Insert(new InteractionState())
			.Insert(new Slider
			{
				Value = initialValue,
				Min = min,
				Max = max,
				Direction = direction,
			})
			.Id;

		// Create slider fill (active portion)
		// Uses SliderFill marker component so SliderPlugin can find it
		var fillId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = isHorizontal ? FlexValue.Percent(0f) : FlexValue.Percent(100f),
				Height = isHorizontal ? FlexValue.Percent(100f) : FlexValue.Percent(0f),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(0f),
				Bottom = FlexValue.Points(0f),
			})
			.Insert(theme.SliderFillColor.ToBackgroundColor())
			.Insert(new BorderRadius(theme.SliderTrackSize / 2f))
			.Insert(new SliderFill())  // Marker for SliderPlugin to find
			.Id;

		// Create slider thumb (draggable handle)
		var thumbId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(theme.SliderThumbSize),
				Height = FlexValue.Points(theme.SliderThumbSize),
				PositionType = PositionType.Absolute,
				Top = FlexValue.Points(isHorizontal ? -(theme.SliderThumbSize - theme.SliderTrackSize) / 2f : 0f),
				Left = FlexValue.Points(isHorizontal ? 0f : -(theme.SliderThumbSize - theme.SliderTrackSize) / 2f),
			})
			.Insert(theme.Primary.ToBackgroundColor())
			.Insert(new BorderRadius(theme.SliderThumbSize / 2f))
			.Insert(new SliderThumb())
			.Insert(new Interactive(focusable: false))
			.Insert(new InteractionState())
			.Id;

		// Add children to track
		commands.Entity(trackId).AddChild(fillId);
		commands.Entity(trackId).AddChild(thumbId);

		// Insert slider component (child entities found via marker components)
		commands.Entity(trackId).Insert(new Slider(min, max, initialValue, direction));

		return trackId;
	}

	// ============================================
	// Checkbox
	// ============================================

	/// <summary>
	/// Creates a themed checkbox.
	/// Returns the checkbox entity ID.
	/// </summary>
	public static ulong CreateCheckbox(Commands commands, UiTheme theme, bool initiallyChecked = false)
	{
		// Create checkbox box
		var checkboxId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(theme.CheckboxSize),
				Height = FlexValue.Points(theme.CheckboxSize),
				JustifyContent = Justify.Center,
				AlignItems = Align.Center,
			})
			.Insert(theme.Surface.ToBackgroundColor())
			.Insert(theme.Border.ToBorderColor())
			.Insert(new BorderRadius(theme.BorderRadiusSmall))
			.Insert(new Interactive(focusable: false))
			.Insert(new InteractionState())
			.Insert(new Checkbox(initiallyChecked))
			.Id;

		// Create checkmark (initially hidden if not checked)
		// Uses Checkmark marker component so CheckboxPlugin can find it
		var checkmarkId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(theme.CheckmarkSize),
				Height = FlexValue.Points(theme.CheckmarkSize),
				Display = initiallyChecked ? Display.Flex : Display.None,
			})
			.Insert(theme.Primary.ToBackgroundColor())
			.Insert(new BorderRadius(2f))
			.Insert(new Checkmark())  // Marker for CheckboxPlugin to find
			.Id;

		// Add checkmark as child of checkbox
		commands.Entity(checkboxId).AddChild(checkmarkId);

		return checkboxId;
	}

	// ============================================
	// Toggle/Switch
	// ============================================

	/// <summary>
	/// Creates a themed toggle/switch widget.
	/// Returns the toggle entity ID.
	/// </summary>
	public static ulong CreateToggle(Commands commands, UiTheme theme, bool initialValue = false)
	{
		// Create toggle track (background)
		var trackId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(theme.ToggleWidth),
				Height = FlexValue.Points(theme.ToggleHeight),
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Center,
				PositionType = PositionType.Relative,
			})
			.Insert((initialValue ? theme.ToggleOnColor : theme.ToggleOffColor).ToBackgroundColor())
			.Insert(new BorderRadius(theme.ToggleHeight / 2f))
			.Insert(new Interactive(focusable: false))
			.Insert(new InteractionState())
			.Insert(new Toggle(initialValue))
			.Id;

		// Create toggle thumb (draggable circle)
		// Uses ToggleThumb marker component so TogglePlugin can find it
		var thumbId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(theme.ToggleThumbSize),
				Height = FlexValue.Points(theme.ToggleThumbSize),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(initialValue ? theme.ToggleWidth - theme.ToggleThumbSize - 2f : 2f),
				Right = FlexValue.Auto(),
			})
			.Insert(theme.Surface.ToBackgroundColor())
			.Insert(new BorderRadius(theme.ToggleThumbSize / 2f))
			.Insert(new ToggleThumb())  // Marker for TogglePlugin to find
			.Id;

		// Add thumb as child
		commands.Entity(trackId).AddChild(thumbId);

		return trackId;
	}

	// ============================================
	// Radio Button
	// ============================================

	/// <summary>
	/// Creates a themed radio button.
	/// Returns the radio button entity ID.
	/// </summary>
	public static ulong CreateRadioButton(Commands commands, UiTheme theme, int groupId, int value, bool initiallySelected = false)
	{
		// Create radio button circle
		var radioId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(theme.RadioSize),
				Height = FlexValue.Points(theme.RadioSize),
				JustifyContent = Justify.Center,
				AlignItems = Align.Center,
			})
			.Insert(theme.Surface.ToBackgroundColor())
			.Insert(theme.Border.ToBorderColor())
			.Insert(new BorderRadius(theme.RadioSize / 2f))
			.Insert(new Interactive(focusable: false))
			.Insert(new InteractionState())
			.Insert(new RadioButton(value, initiallySelected))
			.Insert(new RadioGroup(groupId))
			.Id;

		// Create radio indicator (inner circle, initially hidden if not selected)
		// Uses RadioIndicator marker component so RadioButtonPlugin can find it
		var indicatorId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(theme.RadioIndicatorSize),
				Height = FlexValue.Points(theme.RadioIndicatorSize),
				Display = initiallySelected ? Display.Flex : Display.None,
			})
			.Insert(theme.Primary.ToBackgroundColor())
			.Insert(new BorderRadius(theme.RadioIndicatorSize / 2f))
			.Insert(new RadioIndicator())  // Marker for RadioButtonPlugin to find
			.Id;

		// Add indicator as child of radio button
		commands.Entity(radioId).AddChild(indicatorId);

		return radioId;
	}

	// ============================================
	// Text Input
	// ============================================

	/// <summary>
	/// Creates a themed text input field.
	/// Returns the text input entity ID.
	/// </summary>
	public static ulong CreateTextInput(Commands commands, UiTheme theme, string placeholder = "", int maxLength = 0)
	{
		// Create text input container
		var inputId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(200f),
				Height = FlexValue.Points(theme.TextInputHeight),
				PaddingLeft = FlexValue.Points(theme.Padding),
				PaddingRight = FlexValue.Points(theme.Padding),
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Center,
				PositionType = PositionType.Relative,
			})
			.Insert(theme.Surface.ToBackgroundColor())
			.Insert(theme.Border.ToBorderColor())
			.Insert(new BorderRadius(theme.BorderRadius))
			.Insert(new Interactive(focusable: true))
			.Insert(new InteractionState())
			.Insert(new TextInput(placeholder, maxLength))
			.Id;

		// Create text display element
		// Uses TextInputText marker component so TextInputPlugin can find it
		var textId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Auto(),
				Height = FlexValue.Auto(),
			})
			.Insert(new UiText(""))
			.Insert(new TextStyle(fontSize: theme.TextInputFontSize, color: theme.Text))
			.Insert(new TextInputText())  // Marker for TextInputPlugin to find
			.Id;

		// Create placeholder text element
		// Uses TextInputPlaceholder marker component so TextInputPlugin can find it
		var placeholderId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Auto(),
				Height = FlexValue.Auto(),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(theme.Padding),
			})
			.Insert(new UiText(placeholder))
			.Insert(new TextStyle(fontSize: theme.TextInputFontSize, color: theme.PlaceholderColor))
			.Insert(new TextInputPlaceholder())  // Marker for TextInputPlugin to find
			.Id;

		// Create cursor element
		// Uses TextInputCursor marker component so TextInputPlugin can find it
		var cursorId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(theme.TextCursorWidth),
				Height = FlexValue.Points(theme.TextInputFontSize),
				Display = Display.None, // Hidden until focused
			})
			.Insert(theme.Text.ToBackgroundColor())
			.Insert(new TextInputCursor())  // Marker for TextInputPlugin to find
			.Id;

		// Add children to input container
		commands.Entity(inputId).AddChild(textId);
		commands.Entity(inputId).AddChild(placeholderId);
		commands.Entity(inputId).AddChild(cursorId);

		return inputId;
	}

	// ============================================
	// Panel/Container
	// ============================================

	/// <summary>
	/// Creates a themed panel/container for grouping widgets.
	/// </summary>
	public static ulong CreatePanel(Commands commands, UiTheme theme, float width = 300f, float height = 200f)
	{
		return commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(width),
				Height = FlexValue.Points(height),
				FlexDirection = FlexDirection.Column,
				PaddingTop = FlexValue.Points(theme.Padding),
				PaddingBottom = FlexValue.Points(theme.Padding),
				PaddingLeft = FlexValue.Points(theme.Padding),
				PaddingRight = FlexValue.Points(theme.Padding),
			})
			.Insert(theme.Background.ToBackgroundColor())
			.Insert(theme.Border.ToBorderColor())
			.Insert(new BorderRadius(theme.BorderRadius))
			.Id;
	}

	/// <summary>
	/// Creates a themed label/text element.
	/// </summary>
	public static ulong CreateLabel(Commands commands, UiTheme theme, string text, float? fontSize = null)
	{
		return commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Auto(),
				Height = FlexValue.Auto(),
			})
			.Insert(new UiText(text))
			.Insert(new TextStyle(fontSize: fontSize ?? theme.ButtonFontSize, color: theme.Text))
			.Id;
	}

	// ============================================
	// Dropdown
	// ============================================

	/// <summary>
	/// Creates a themed dropdown with specified options.
	/// Returns the dropdown button entity ID.
	/// </summary>
	public static ulong CreateDropdown(Commands commands, UiTheme theme, List<string> options, int? selectedIndex = null)
	{
		// Create dropdown button
		var dropdownId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(150f),
				Height = FlexValue.Points(theme.ButtonHeight),
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Center,
				PaddingLeft = FlexValue.Points(theme.Padding),
				PaddingRight = FlexValue.Points(theme.Padding),
				MarginBottom = FlexValue.Points(theme.Gap),
			})
			.Insert(theme.Surface.ToBackgroundColor())
			.Insert(theme.Border.ToBorderColor())
			.Insert(new BorderRadius(theme.BorderRadius))
			.Insert(new Interactive(focusable: false))
			.Insert(new InteractionState())
			.Insert(new DropdownOptions(options))
			.Id;

		// Create label for selected value
		// Uses DropdownLabel marker component so DropdownPlugin can find it
		var labelId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Percent(100f),
				Height = FlexValue.Percent(100f),
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Center,
			})
			.Insert(new UiText(selectedIndex.HasValue && selectedIndex.Value < options.Count ? options[selectedIndex.Value] : "---"))
			.Insert(new TextStyle(fontSize: theme.ButtonFontSize, color: theme.Text))
			.Insert(new DropdownLabel())  // Marker for DropdownPlugin to find
			.Id;

		// Create dropdown panel as floating panel (hidden by default)
		// Uses proper flex layout: Row with [Viewport (flex-grow), Scrollbar (fixed width)]
		// This ensures content width = panel width - scrollbar width
		var scrollbarWidth = 8f;
		var panelId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(150f),
				Height = FlexValue.Points(200f),
				Display = Display.None, // Hidden initially
				FlexDirection = FlexDirection.Row, // Row layout: viewport | scrollbar
			})
			.Insert(theme.Surface.ToBackgroundColor())
			.Insert(theme.Border.ToBorderColor())
			.Insert(new BorderRadius(theme.BorderRadius))
			.Insert(new BorderThickness(1f))
			.Insert(new DropdownPanel(dropdownId))
			.Insert(new FloatingPanel
			{
				Size = new Vector2(150f, 200f),
				Position = Vector2.Zero, // Will be updated when dropdown opens
				Priority = false
			})
			.Id;

		// Create viewport (clips content and handles scrolling)
		// Uses flex-grow to take remaining horizontal space, Height = 100% for cross-axis
		var viewportId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Auto(),
				Height = FlexValue.Percent(100f), // Fill parent height (cross-axis in Row)
				FlexGrow = 1f, // Take remaining horizontal space after scrollbar
				FlexShrink = 1f,
				Overflow = Overflow.Scroll, // Enable clipping
				AlignItems = Align.FlexStart,
				JustifyContent = Justify.FlexStart,
			})
			.Insert(new Scrollable
			{
				EnableVertical = true,
				EnableHorizontal = false,
				ScrollSpeed = 20f,
			})
			.Insert(new ScrollViewViewport(panelId))
			.Id;

		// Create content container (holds options)
		// No padding adjustment needed - scrollbar is part of flex layout
		var contentId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Auto(),
				Height = FlexValue.Auto(),
				MinWidth = FlexValue.Percent(100f),
				MinHeight = FlexValue.Percent(100f),
				Display = Display.Flex,
				FlexDirection = FlexDirection.Column,
				AlignSelf = Align.FlexStart,
				PaddingBottom = FlexValue.Points(theme.Padding),
				PaddingLeft = FlexValue.Points(theme.Padding),
				PaddingRight = FlexValue.Points(theme.Padding),
				PaddingTop = FlexValue.Points(theme.Padding)
			})
			.Insert(new ScrollViewContent(panelId))
			.Id;

		// Create vertical scrollbar (fixed width, part of flex layout - not overlay)
		// Height = 100% to fill parent height in Row layout
		var verticalBarId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(scrollbarWidth),
				Height = FlexValue.Percent(100f), // Fill parent height
				FlexGrow = 0f,
				FlexShrink = 0f,
				Display = Display.Flex,
				FlexDirection = FlexDirection.Column,
				PositionType = PositionType.Relative // Part of flex layout, not absolute
			})
			.Insert(new BackgroundColor(new Vector4(theme.Border.X, theme.Border.Y, theme.Border.Z, 0.5f)))
			.Insert(new BorderRadius(scrollbarWidth / 2f)) // Rounded track
			.Insert(new Interactive()) // Track is interactive for page scroll on click
			.Id;

		// Create scrollbar handle/thumb (draggable)
		var verticalHandleId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Percent(100f),
				Height = FlexValue.Points(40f),
				PositionType = PositionType.Absolute // Thumb is absolute within the rail
			})
			.Insert(new BackgroundColor(new Vector4(theme.Text.X * 0.7f, theme.Text.Y * 0.7f, theme.Text.Z * 0.7f, 0.8f)))
			.Insert(new BorderRadius(scrollbarWidth / 2f)) // Rounded thumb
			.Insert(new ScrollbarThumb())
			.Insert(new ScrollBarHandle(ControlOrientation.Vertical, panelId))
			.Insert(new Draggable())
			.Insert(new Interactive())
			.Id;

		// Set up scrollbar component
		commands.Entity(verticalBarId).Insert(new Scrollbar(viewportId, ControlOrientation.Vertical, 20f));

		// Set up scrollbar hierarchy
		commands.Entity(verticalBarId).AddChild(verticalHandleId);

		// Create options
		for (int i = 0; i < options.Count; i++)
		{
			var optionId = commands.Spawn()
				.Insert(new UiNode
				{
					Width = FlexValue.Percent(100f),
					Height = FlexValue.Points(theme.ButtonHeight),
					JustifyContent = Justify.FlexStart,
					AlignItems = Align.Center,
				})
				.Insert(new BackgroundColor(new Vector4(0f, 0f, 0f, 0f))) // Transparent initially
				.Insert(new UiText(options[i]))
				.Insert(new TextStyle(fontSize: theme.ButtonFontSize, color: theme.Text))
				.Insert(new Interactive(focusable: false))
				.Insert(new InteractionState())
				.Insert(new DropdownOption(dropdownId, i))
				.Id;

			// Add option as child of content container
			commands.Entity(contentId).AddChild(optionId);
		}

		// Set up ScrollView component
		commands.Entity(panelId).Insert(new ScrollView(viewportId, contentId)
		{
			VerticalScrollBar = verticalBarId,
			VerticalScrollBarHandle = verticalHandleId,
			HorizontalScrollBar = null,
			HorizontalScrollBarHandle = null
		});

		// Set up hierarchy: panel (Row) -> [viewport (flex-grow) -> content -> options, scrollbar (fixed)]
		commands.Entity(viewportId).AddChild(contentId);
		commands.Entity(panelId).AddChild(viewportId);
		commands.Entity(panelId).AddChild(verticalBarId);

		// Set up hierarchy (panel is NOT a child - it's a floating panel)
		commands.Entity(dropdownId).AddChild(labelId);

		// Add Dropdown component to button (child entities found via marker components)
		commands.Entity(dropdownId).Insert(new Dropdown(selectedIndex));

		return dropdownId;
	}
}

/// <summary>
/// Extension methods for Vector4 color to component conversion.
/// </summary>
internal static class Vector4Extensions
{
	public static Vector4 Lerp(this Vector4 a, Vector4 b, float t)
	{
		return new Vector4(
			a.X + (b.X - a.X) * t,
			a.Y + (b.Y - a.Y) * t,
			a.Z + (b.Z - a.Z) * t,
			a.W + (b.W - a.W) * t
		);
	}
}
