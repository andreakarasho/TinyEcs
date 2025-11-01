using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Plugin that includes all UI widget systems.
/// Add this to get access to all built-in widgets:
/// - Buttons
/// - Checkboxes
/// - Scrollbars
/// - ScrollViews
/// - Sliders
/// - TextInputs
/// - RadioButtons
/// - Toggles/Switches
/// - ProgressBars
///
/// Note: This plugin requires TinyEcsUiPlugin to be added first.
///
/// Usage:
/// <code>
/// app.AddPlugin(new TinyEcsUiPlugin());
/// app.AddPlugin(new TinyEcsUiWidgetsPlugin());
/// </code>
/// </summary>
public struct TinyEcsUiWidgetsPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Button widget functionality
		app.AddPlugin(new ButtonPlugin());

		// Checkbox widget
		app.AddPlugin(new CheckboxPlugin());

		// Scrollbar widget
		app.AddPlugin(new ScrollbarPlugin());

		// ScrollView compound widget
		app.AddPlugin(new ScrollViewPlugin());

		// Slider widget
		app.AddPlugin(new SliderPlugin());

		// TextInput widget
		app.AddPlugin(new TextInputPlugin());

		// RadioButton widget
		app.AddPlugin(new RadioButtonPlugin());

		// Toggle/Switch widget
		app.AddPlugin(new TogglePlugin());

		// ProgressBar widget
		app.AddPlugin(new ProgressBarPlugin());
	}
}
