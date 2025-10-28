using System.Numerics;
using Raylib_cs;
using TinyEcs.Bevy;
using TinyEcs.UI.Bevy;

namespace TinyEcsGame;

/// <summary>
/// Adapter that updates PointerInputState from Raylib input.
/// This is renderer-specific - each rendering backend (Raylib, SDL, etc.) needs its own adapter.
/// </summary>
public struct RaylibPointerInputAdapter : IPlugin
{
	/// <summary>
	/// Stage where Raylib input should be read (typically First or Update).
	/// Must run before UiPointerInputPlugin's hit testing.
	/// </summary>
	public Stage InputStage { get; set; }

	public RaylibPointerInputAdapter()
	{
		InputStage = Stage.Update;
	}

	public readonly void Build(App app)
	{
		// System to read Raylib input and update PointerInputState resource
		app.AddSystem((ResMut<PointerInputState> pointerState, ResMut<ScrollInputState> scrollState) =>
		{
			UpdatePointerStateFromRaylib(pointerState, scrollState);
		})
		.InStage(InputStage)
		.Label("raylib:update-pointer-input")
		.Before("ui:pointer:hit-test") // Run before hit testing
		.SingleThreaded() // Raylib calls must be single-threaded
		.Build();
	}

	/// <summary>
	/// Reads Raylib mouse input and updates the PointerInputState and ScrollInputState resources.
	/// </summary>
	private static void UpdatePointerStateFromRaylib(ResMut<PointerInputState> pointerState, ResMut<ScrollInputState> scrollState)
	{
		ref var state = ref pointerState.Value;

		// Update pointer position
		state.Position = Raylib.GetMousePosition();

		// Update button states
		state.IsPrimaryButtonDown = Raylib.IsMouseButtonDown(MouseButton.Left);
		state.IsPrimaryButtonPressed = Raylib.IsMouseButtonPressed(MouseButton.Left);
		state.IsPrimaryButtonReleased = Raylib.IsMouseButtonReleased(MouseButton.Left);

		// Update scroll delta (for both pointer and scroll state)
		var scrollDelta = new Vector2(0, Raylib.GetMouseWheelMove());
		state.ScrollDelta = scrollDelta;
		scrollState.Value.ScrollDelta = scrollDelta;
	}
}
