using System.Runtime.InteropServices;
using Clay_cs;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Clay;

/// <summary>
/// Main Clay UI plugin that composes all Clay sub-plugins.
/// Provides a complete Clay-based UI system for TinyEcs.Bevy.
/// </summary>
public struct ClayUiPlugin : IPlugin
{
	/// <summary>
	/// Configuration options for Clay UI.
	/// </summary>
	public required ClayUiOptions Options { get; init; }

	public unsafe void Build(App app)
	{
		// Calculate minimum required memory based on max element count
		var minSize = Clay_cs.Clay.MinMemorySize();
		var arenaSize = Math.Max(Options.ArenaSize, minSize);

		if (arenaSize > Options.ArenaSize)
		{
			Console.WriteLine($"[Clay] Arena size increased from {Options.ArenaSize} to {arenaSize} (minimum required)");
		}

		var arena = Clay_cs.Clay.CreateArena(arenaSize);

		// Get error handler function pointer if provided
		var errorHandlerPtr = IntPtr.Zero;
		if (Options.ErrorHandler != null)
		{
			errorHandlerPtr = Marshal.GetFunctionPointerForDelegate(Options.ErrorHandler);
		}

		var context = Clay_cs.Clay.Initialize(
			arena,
			Options.LayoutDimensions,
			errorHandlerPtr);

		// Set text measurement function if provided
		if (Options.MeasureTextFunction != null)
		{
			var functionPtr = Marshal.GetFunctionPointerForDelegate(Options.MeasureTextFunction);
			Clay_cs.Clay.SetMeasureTextFunction(functionPtr);
		}

		Clay_cs.Clay.SetLayoutDimensions(Options.LayoutDimensions);
		Clay_cs.Clay.SetCullingEnabled(Options.EnableCulling);
		Clay_cs.Clay.SetDebugModeEnabled(Options.EnableDebugMode);

		// Create and insert resources
		var uiState = new ClayUiState
		{
			Arena = arena,
			Context = context,
			LayoutDimensions = Options.LayoutDimensions,
			DebugModeEnabled = Options.EnableDebugMode,
		};

		var pointerState = new ClayPointerState
		{
			Position = System.Numerics.Vector2.Zero,
			PrimaryDown = false,
			PrimaryPressed = false,
			PrimaryReleased = false,
			ScrollDelta = System.Numerics.Vector2.Zero,
			DeltaTime = 1f / 60f,
			EnableDragScrolling = true
		};

		var textInputState = new ClayTextInputState();

		app.AddResource(uiState);
		app.AddResource(pointerState);
		app.AddResource(textInputState);

		// Add sub-plugins
		app.AddPlugin(new ClayLayoutPlugin());
		app.AddPlugin(new ClayInteractionPlugin());
		app.AddPlugin(new ClayHierarchyPlugin());
		app.AddPlugin(new Widgets.WidgetsPlugin());

		// Add cleanup system to reset transient state
		app.AddSystem((ResMut<ClayPointerState> pointer, ResMut<ClayTextInputState> textInput) =>
		{
			pointer.Value.ResetTransientState();
			textInput.Value.ResetTransientState();
		})
			.InStage(Stage.First)
			.Label("clay:reset-input-state")
			.Build();
	}
}

/// <summary>
/// Plugin responsible for managing Clay hierarchy (parent-child relationships).
/// Since TinyEcs.Bevy doesn't have Parent/Children components yet, we define our own.
/// </summary>
public struct ClayHierarchyPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddObserver((OnAdd<ClayNode> trigger, Commands commands) =>
		{
			commands.Entity(trigger.EntityId).Insert(ClayElementId.From(trigger.EntityId));
		});
	}
}

/// <summary>
/// Extension methods for convenient Clay UI setup.
/// </summary>
public static class ClayUiExtensions
{
	/// <summary>
	/// Add Clay UI plugin with default options.
	/// </summary>
	public static App AddClayUi(this App app)
	{
		return app.AddClayUi(new ClayUiOptions());
	}

	/// <summary>
	/// Add Clay UI plugin with custom options.
	/// </summary>
	public static App AddClayUi(this App app, ClayUiOptions options)
	{
		app.AddPlugin(new ClayUiPlugin { Options = options });
		return app;
	}

	/// <summary>
	/// Spawn a Clay element entity with default configuration.
	/// </summary>
	public static EntityCommands SpawnClayElement(this Commands commands, ClayNode node)
	{
		var entity = commands.Spawn();
		entity.Insert(node);
		// entity.Insert(ClayElementId.From(entity.Id));
		entity.Insert(new ClayComputedLayout());
		return entity;
	}
}
