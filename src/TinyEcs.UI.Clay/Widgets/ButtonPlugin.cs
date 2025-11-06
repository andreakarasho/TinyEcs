using TinyEcs.Bevy;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds button widget systems to the application.
/// </summary>
public struct ButtonPlugin : IPlugin
{
	public void Build(App app)
	{
		// Button widget doesn't need any update systems currently
		// All interaction is handled via observers in the widget itself
		// This plugin is here for consistency and future extensions
	}
}
