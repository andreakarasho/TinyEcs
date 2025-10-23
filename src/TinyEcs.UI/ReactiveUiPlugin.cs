using TinyEcs.Bevy;
using TinyEcs.UI.Widgets;

namespace TinyEcs.UI;

/// <summary>
/// Comprehensive UI plugin that sets up the complete reactive UI system.
/// Includes Clay layout, interaction detection, widget observers, and window ordering.
///
/// This is the recommended way to set up the UI system - it provides a Bevy-like
/// reactive architecture where widget behavior is driven by component state changes
/// rather than manual event handling.
///
/// Usage:
/// app.AddPlugin(new ReactiveUiPlugin { Options = ClayUiOptions.Default });
/// </summary>
public sealed class ReactiveUiPlugin : IPlugin
{
	/// <summary>
	/// Clay UI configuration options.
	/// </summary>
	public ClayUiOptions Options { get; set; } = ClayUiOptions.Default;

	/// <summary>
	/// Whether to include widget observer systems for automatic visual updates.
	/// Default: true (enables reactive button/checkbox/slider behavior).
	/// </summary>
	public bool EnableWidgetObservers { get; set; } = true;

	/// <summary>
	/// Whether to include interaction detection systems (Interaction component updates).
	/// Default: true (required for reactive widgets).
	/// </summary>
	public bool EnableInteractionDetection { get; set; } = true;

	/// <summary>
	/// Whether to include focus management systems.
	/// Default: true (enables keyboard navigation and focus tracking).
	/// </summary>
	public bool EnableFocusManagement { get; set; } = true;

	/// <summary>
	/// Whether to compute Z-indices for rendering order.
	/// Default: true (enables hierarchical depth-based ordering).
	/// </summary>
	public bool EnableZIndexComputation { get; set; } = true;

	public void Build(App app)
	{
		// Core Clay UI system (always required)
		app.AddPlugin(new ClayUiPlugin { Options = Options });

		// Widget systems (window ordering, slider/window drag handling)
		app.AddUiWidgets();

		// Interaction detection (enables Interaction component updates)
		if (EnableInteractionDetection)
		{
			app.AddUiInteraction();
		}

		// Widget observers (enables reactive visual updates)
		if (EnableWidgetObservers)
		{
			app.AddUiWidgetObservers();
		}

		// Focus management is part of UiInteraction, controlled by that flag
		// Z-index computation is part of UiInteraction, controlled by that flag
	}
}

public static class ReactiveUiAppExtensions
{
	/// <summary>
	/// Adds the complete reactive UI system with default configuration.
	/// Equivalent to adding ClayUiPlugin + UiInteractionPlugin + UiWidgetObserversPlugin.
	/// </summary>
	public static App AddReactiveUi(this App app)
	{
		app.AddPlugin(new ReactiveUiPlugin());
		return app;
	}

	/// <summary>
	/// Adds the complete reactive UI system with custom configuration.
	/// </summary>
	public static App AddReactiveUi(this App app, ClayUiOptions options)
	{
		app.AddPlugin(new ReactiveUiPlugin { Options = options });
		return app;
	}
}
