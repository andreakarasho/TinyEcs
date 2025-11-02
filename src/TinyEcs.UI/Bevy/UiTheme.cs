using System.Numerics;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Comprehensive UI theme configuration for all widgets.
/// Store as a resource: app.AddResource(UiTheme.Dark());
/// Access in systems: Res&lt;UiTheme&gt; theme
/// </summary>
public struct UiTheme
{
	// ============================================
	// Core Colors
	// ============================================

	/// <summary>Primary accent color (used for active states, selections)</summary>
	public Vector4 Primary;

	/// <summary>Secondary color (used for alternate elements)</summary>
	public Vector4 Secondary;

	/// <summary>Background color for panels and containers</summary>
	public Vector4 Background;

	/// <summary>Surface color for widgets and UI elements</summary>
	public Vector4 Surface;

	/// <summary>Border color for outlines</summary>
	public Vector4 Border;

	/// <summary>Text color</summary>
	public Vector4 Text;

	/// <summary>Muted/disabled text color</summary>
	public Vector4 TextMuted;

	/// <summary>Accent color for highlights and focus states</summary>
	public Vector4 Accent;

	/// <summary>Error/danger color</summary>
	public Vector4 Error;

	/// <summary>Success color</summary>
	public Vector4 Success;

	/// <summary>Warning color</summary>
	public Vector4 Warning;

	// ============================================
	// Widget State Colors
	// ============================================

	/// <summary>Color when widget is hovered</summary>
	public Vector4 Hover;

	/// <summary>Color when widget is pressed/active</summary>
	public Vector4 Active;

	/// <summary>Color when widget is disabled</summary>
	public Vector4 Disabled;

	// ============================================
	// Spacing & Layout
	// ============================================

	/// <summary>Default border radius for rounded corners</summary>
	public float BorderRadius;

	/// <summary>Small border radius (for compact widgets)</summary>
	public float BorderRadiusSmall;

	/// <summary>Large border radius (for prominent widgets)</summary>
	public float BorderRadiusLarge;

	/// <summary>Default padding inside widgets</summary>
	public float Padding;

	/// <summary>Small padding</summary>
	public float PaddingSmall;

	/// <summary>Large padding</summary>
	public float PaddingLarge;

	/// <summary>Gap between elements</summary>
	public float Gap;

	// ============================================
	// Widget-Specific Dimensions
	// ============================================

	// Button
	/// <summary>Default button height</summary>
	public float ButtonHeight;
	/// <summary>Default button width</summary>
	public float ButtonWidth;
	/// <summary>Button text size</summary>
	public float ButtonFontSize;

	// Slider
	/// <summary>Slider track height (horizontal) or width (vertical)</summary>
	public float SliderTrackSize;
	/// <summary>Slider thumb diameter</summary>
	public float SliderThumbSize;
	/// <summary>Slider fill/active track color</summary>
	public Vector4 SliderFillColor;
	/// <summary>Slider track background color</summary>
	public Vector4 SliderTrackColor;

	// Checkbox
	/// <summary>Checkbox size (width and height)</summary>
	public float CheckboxSize;
	/// <summary>Checkmark icon size</summary>
	public float CheckmarkSize;

	// Toggle/Switch
	/// <summary>Toggle switch width</summary>
	public float ToggleWidth;
	/// <summary>Toggle switch height</summary>
	public float ToggleHeight;
	/// <summary>Toggle thumb size</summary>
	public float ToggleThumbSize;
	/// <summary>Toggle ON color</summary>
	public Vector4 ToggleOnColor;
	/// <summary>Toggle OFF color</summary>
	public Vector4 ToggleOffColor;

	// Radio Button
	/// <summary>Radio button size (outer circle diameter)</summary>
	public float RadioSize;
	/// <summary>Radio indicator size (inner circle diameter)</summary>
	public float RadioIndicatorSize;

	// Text Input
	/// <summary>Text input height</summary>
	public float TextInputHeight;
	/// <summary>Text input font size</summary>
	public float TextInputFontSize;
	/// <summary>Text cursor width</summary>
	public float TextCursorWidth;
	/// <summary>Placeholder text color</summary>
	public Vector4 PlaceholderColor;

	// Scrollbar
	/// <summary>Scrollbar width/height</summary>
	public float ScrollbarSize;
	/// <summary>Scrollbar thumb color</summary>
	public Vector4 ScrollbarThumbColor;
	/// <summary>Scrollbar track color</summary>
	public Vector4 ScrollbarTrackColor;

	// ============================================
	// Preset Themes
	// ============================================

	/// <summary>
	/// Dark theme with green accents (similar to current design).
	/// Recommended for reduced eye strain and modern aesthetics.
	/// </summary>
	public static UiTheme Dark() => new UiTheme
	{
		// Core colors
		Primary = new Vector4(0.3f, 0.69f, 0.31f, 1f),      // Green #4CAF50
		Secondary = new Vector4(0.62f, 0.62f, 0.62f, 1f),   // Gray #9E9E9E
		Background = new Vector4(0.16f, 0.16f, 0.2f, 1f),   // Dark gray #282832
		Surface = new Vector4(0.2f, 0.24f, 0.28f, 1f),      // Slightly lighter #333D47
		Border = new Vector4(0.4f, 0.4f, 0.47f, 1f),        // Medium gray #666678
		Text = new Vector4(0.9f, 0.9f, 0.9f, 1f),           // Light gray #E6E6E6
		TextMuted = new Vector4(0.6f, 0.6f, 0.6f, 1f),      // Muted gray #999999
		Accent = new Vector4(0.2f, 0.6f, 0.86f, 1f),        // Blue #3399DB
		Error = new Vector4(0.96f, 0.26f, 0.21f, 1f),       // Red #F44336
		Success = new Vector4(0.3f, 0.69f, 0.31f, 1f),      // Green #4CAF50
		Warning = new Vector4(1f, 0.76f, 0.03f, 1f),        // Amber #FFC107

		// State colors
		Hover = new Vector4(0.25f, 0.29f, 0.33f, 1f),       // Lighter surface
		Active = new Vector4(0.15f, 0.19f, 0.23f, 1f),      // Darker surface
		Disabled = new Vector4(0.3f, 0.3f, 0.3f, 0.5f),     // Semi-transparent gray

		// Spacing
		BorderRadius = 8f,
		BorderRadiusSmall = 4f,
		BorderRadiusLarge = 12f,
		Padding = 8f,
		PaddingSmall = 4f,
		PaddingLarge = 16f,
		Gap = 8f,

		// Button
		ButtonHeight = 40f,
		ButtonWidth = 120f,
		ButtonFontSize = 16f,

		// Slider
		SliderTrackSize = 4f,
		SliderThumbSize = 20f,
		SliderFillColor = new Vector4(0.3f, 0.69f, 0.31f, 1f),   // Green
		SliderTrackColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),    // Dark gray

		// Checkbox
		CheckboxSize = 20f,
		CheckmarkSize = 14f,

		// Toggle
		ToggleWidth = 44f,
		ToggleHeight = 24f,
		ToggleThumbSize = 20f,
		ToggleOnColor = new Vector4(0.3f, 0.69f, 0.31f, 1f),     // Green
		ToggleOffColor = new Vector4(0.62f, 0.62f, 0.62f, 1f),   // Gray

		// Radio
		RadioSize = 20f,
		RadioIndicatorSize = 10f,

		// Text Input
		TextInputHeight = 36f,
		TextInputFontSize = 14f,
		TextCursorWidth = 2f,
		PlaceholderColor = new Vector4(0.5f, 0.5f, 0.5f, 1f),    // Muted gray

		// Scrollbar
		ScrollbarSize = 12f,
		ScrollbarThumbColor = new Vector4(0.4f, 0.4f, 0.4f, 1f),
		ScrollbarTrackColor = new Vector4(0.2f, 0.2f, 0.2f, 1f),
	};

	/// <summary>
	/// Light theme with blue accents.
	/// Clean and professional appearance for daytime use.
	/// </summary>
	public static UiTheme Light() => new UiTheme
	{
		// Core colors
		Primary = new Vector4(0.13f, 0.59f, 0.95f, 1f),     // Blue #2196F3
		Secondary = new Vector4(0.62f, 0.62f, 0.62f, 1f),   // Gray #9E9E9E
		Background = new Vector4(0.96f, 0.96f, 0.96f, 1f),  // Light gray #F5F5F5
		Surface = new Vector4(1f, 1f, 1f, 1f),              // White #FFFFFF
		Border = new Vector4(0.74f, 0.74f, 0.74f, 1f),      // Light gray #BDBDBD
		Text = new Vector4(0.13f, 0.13f, 0.13f, 1f),        // Dark gray #212121
		TextMuted = new Vector4(0.46f, 0.46f, 0.46f, 1f),   // Medium gray #757575
		Accent = new Vector4(0.4f, 0.23f, 0.72f, 1f),       // Purple #673AB7
		Error = new Vector4(0.96f, 0.26f, 0.21f, 1f),       // Red #F44336
		Success = new Vector4(0.3f, 0.69f, 0.31f, 1f),      // Green #4CAF50
		Warning = new Vector4(1f, 0.6f, 0f, 1f),            // Orange #FF9800

		// State colors
		Hover = new Vector4(0.96f, 0.96f, 0.96f, 1f),       // Light gray
		Active = new Vector4(0.88f, 0.88f, 0.88f, 1f),      // Slightly darker gray
		Disabled = new Vector4(0.7f, 0.7f, 0.7f, 0.5f),     // Semi-transparent gray

		// Spacing (same as dark)
		BorderRadius = 8f,
		BorderRadiusSmall = 4f,
		BorderRadiusLarge = 12f,
		Padding = 8f,
		PaddingSmall = 4f,
		PaddingLarge = 16f,
		Gap = 8f,

		// Button
		ButtonHeight = 40f,
		ButtonWidth = 120f,
		ButtonFontSize = 16f,

		// Slider
		SliderTrackSize = 4f,
		SliderThumbSize = 20f,
		SliderFillColor = new Vector4(0.13f, 0.59f, 0.95f, 1f),  // Blue
		SliderTrackColor = new Vector4(0.88f, 0.88f, 0.88f, 1f), // Light gray

		// Checkbox
		CheckboxSize = 20f,
		CheckmarkSize = 14f,

		// Toggle
		ToggleWidth = 44f,
		ToggleHeight = 24f,
		ToggleThumbSize = 20f,
		ToggleOnColor = new Vector4(0.13f, 0.59f, 0.95f, 1f),    // Blue
		ToggleOffColor = new Vector4(0.74f, 0.74f, 0.74f, 1f),   // Gray

		// Radio
		RadioSize = 20f,
		RadioIndicatorSize = 10f,

		// Text Input
		TextInputHeight = 36f,
		TextInputFontSize = 14f,
		TextCursorWidth = 2f,
		PlaceholderColor = new Vector4(0.62f, 0.62f, 0.62f, 1f),

		// Scrollbar
		ScrollbarSize = 12f,
		ScrollbarThumbColor = new Vector4(0.62f, 0.62f, 0.62f, 1f),
		ScrollbarTrackColor = new Vector4(0.88f, 0.88f, 0.88f, 1f),
	};

	/// <summary>
	/// High contrast theme for accessibility.
	/// Maximum readability with stark color differences.
	/// </summary>
	public static UiTheme HighContrast() => new UiTheme
	{
		// Core colors - maximum contrast
		Primary = new Vector4(1f, 1f, 0f, 1f),              // Yellow #FFFF00
		Secondary = new Vector4(0f, 1f, 1f, 1f),            // Cyan #00FFFF
		Background = new Vector4(0f, 0f, 0f, 1f),           // Black #000000
		Surface = new Vector4(0.1f, 0.1f, 0.1f, 1f),        // Very dark gray
		Border = new Vector4(1f, 1f, 1f, 1f),               // White #FFFFFF
		Text = new Vector4(1f, 1f, 1f, 1f),                 // White #FFFFFF
		TextMuted = new Vector4(0.8f, 0.8f, 0.8f, 1f),      // Light gray
		Accent = new Vector4(0f, 1f, 0f, 1f),               // Lime #00FF00
		Error = new Vector4(1f, 0f, 0f, 1f),                // Red #FF0000
		Success = new Vector4(0f, 1f, 0f, 1f),              // Green #00FF00
		Warning = new Vector4(1f, 1f, 0f, 1f),              // Yellow #FFFF00

		// State colors
		Hover = new Vector4(0.3f, 0.3f, 0.3f, 1f),
		Active = new Vector4(0.5f, 0.5f, 0.5f, 1f),
		Disabled = new Vector4(0.3f, 0.3f, 0.3f, 0.7f),

		// Spacing (same as others)
		BorderRadius = 4f,
		BorderRadiusSmall = 2f,
		BorderRadiusLarge = 8f,
		Padding = 8f,
		PaddingSmall = 4f,
		PaddingLarge = 16f,
		Gap = 8f,

		// Button
		ButtonHeight = 40f,
		ButtonWidth = 120f,
		ButtonFontSize = 16f,

		// Slider
		SliderTrackSize = 6f,  // Thicker for visibility
		SliderThumbSize = 24f, // Larger for visibility
		SliderFillColor = new Vector4(1f, 1f, 0f, 1f),      // Yellow
		SliderTrackColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),

		// Checkbox
		CheckboxSize = 24f,    // Larger
		CheckmarkSize = 18f,

		// Toggle
		ToggleWidth = 50f,
		ToggleHeight = 28f,
		ToggleThumbSize = 24f,
		ToggleOnColor = new Vector4(0f, 1f, 0f, 1f),        // Green
		ToggleOffColor = new Vector4(0.5f, 0.5f, 0.5f, 1f),

		// Radio
		RadioSize = 24f,
		RadioIndicatorSize = 14f,

		// Text Input
		TextInputHeight = 40f,
		TextInputFontSize = 16f,
		TextCursorWidth = 3f,
		PlaceholderColor = new Vector4(0.7f, 0.7f, 0.7f, 1f),

		// Scrollbar
		ScrollbarSize = 16f,   // Wider
		ScrollbarThumbColor = new Vector4(0.8f, 0.8f, 0.8f, 1f),
		ScrollbarTrackColor = new Vector4(0.2f, 0.2f, 0.2f, 1f),
	};
}

/// <summary>
/// Helper extension methods for working with UiTheme.
/// </summary>
public static class UiThemeExtensions
{
	/// <summary>
	/// Converts Vector4 color to BackgroundColor component.
	/// </summary>
	public static BackgroundColor ToBackgroundColor(this Vector4 color)
	{
		return new BackgroundColor(color);
	}

	/// <summary>
	/// Converts Vector4 color to BorderColor component.
	/// </summary>
	public static BorderColor ToBorderColor(this Vector4 color)
	{
		return new BorderColor(color);
	}

	/// <summary>
	/// Creates a TextStyle with theme colors.
	/// </summary>
	public static TextStyle ToTextStyle(this UiTheme theme, float? fontSize = null)
	{
		return new TextStyle(
			fontSize: fontSize ?? theme.ButtonFontSize,
			color: theme.Text
		);
	}
}
