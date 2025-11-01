using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Plugin that includes all UI widget systems.
/// Add this to get access to all built-in widgets:
/// - Buttons
/// - Checkboxes
/// - Scrollbars
/// - ScrollViews
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
	}
}
