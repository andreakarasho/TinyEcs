using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Core UI plugin that includes all necessary UI systems (without widgets).
/// Add this plugin to get a complete UI framework with:
/// - Flexbox layout engine
/// - Pointer input and interaction
/// - Drag and drop
/// - Scrolling system
/// - UI stack management
/// - Animated interactions
///
/// For widgets (buttons, scrollbars, scrollviews), add TinyEcsUiWidgetsPlugin separately.
///
/// Usage:
/// <code>
/// app.AddPlugin(new TinyEcsUiPlugin());
/// app.AddPlugin(new TinyEcsUiWidgetsPlugin());  // Optional: adds Button, Scrollbar, ScrollView
/// </code>
/// </summary>
public struct TinyEcsUiPlugin : IPlugin
{
	/// <summary>
	/// Stage where pointer input processing should run.
	/// Defaults to Stage.PostUpdate to run after UI layout is complete.
	/// </summary>
	public Stage InputStage { get; set; }

	public TinyEcsUiPlugin()
	{
		InputStage = Stage.PostUpdate;
	}

	public readonly void Build(App app)
	{
		// Core UI layout and rendering (includes DragPlugin and ScrollPlugin)
		app.AddPlugin(new FlexboxUiPlugin());

		// UI stack for managing render order and hit testing
		app.AddPlugin(new UiStackPlugin());

		// Pointer input system (renderer-agnostic)
		app.AddPlugin(new UiPointerInputPlugin { InputStage = InputStage });

		// Note: Platform-specific pointer input adapters (e.g., RaylibPointerInputAdapter)
		// must be added separately by the user since they depend on the specific rendering backend

		// Note: Widget plugins (Button, Scrollbar, ScrollView) are available via TinyEcsUiWidgetsPlugin
	}
}
