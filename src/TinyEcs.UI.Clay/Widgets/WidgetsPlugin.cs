using TinyEcs.Bevy;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Main plugin that registers all Clay UI widgets.
/// Add this plugin to your app to enable all widget systems.
/// </summary>
public struct WidgetsPlugin : IPlugin
{
	public void Build(App app)
	{
		// Register widget plugins
		app.AddPlugin(new ButtonPlugin());
		app.AddPlugin(new SliderPlugin());
		app.AddPlugin(new CheckboxPlugin());
		app.AddPlugin(new RadioPlugin());
		app.AddPlugin(new TextInputPlugin());
		app.AddPlugin(new ProgressBarPlugin());
		app.AddPlugin(new ScrollbarPlugin());
		app.AddPlugin(new PanelPlugin());
		app.AddPlugin(new DropdownPlugin());
		app.AddPlugin(new FloatingWindowPlugin());

		// Future widgets to be added:
		// app.AddPlugin(new TooltipPlugin());
	}
}
