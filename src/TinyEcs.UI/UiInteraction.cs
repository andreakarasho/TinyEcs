using System.Numerics;
using TinyEcs.Bevy;

namespace TinyEcs.UI;

/// <summary>
/// Describes the current interaction state of a UI element, similar to Bevy's Interaction enum.
/// This component is automatically updated by the interaction detection system based on pointer events.
/// </summary>
public enum Interaction
{
	/// <summary>No interaction is occurring.</summary>
	None,
	/// <summary>The pointer is hovering over the element.</summary>
	Hovered,
	/// <summary>The element is being pressed (pointer down).</summary>
	Pressed
}

/// <summary>
/// Component marking a UI element as interactive.
/// Elements without this component won't receive interaction state updates.
/// </summary>
public struct Interactive
{
	/// <summary>
	/// Whether this element can receive focus via keyboard/gamepad navigation.
	/// </summary>
	public bool IsFocusable;

	public static Interactive Default => new() { IsFocusable = false };
	public static Interactive WithFocus() => new() { IsFocusable = true };
}

/// <summary>
/// Component marking the currently focused UI element.
/// Only one element should have this component at a time.
/// </summary>
public struct Focused
{
	/// <summary>
	/// How the element gained focus (pointer, keyboard, programmatic).
	/// </summary>
	public FocusSource Source;
}

/// <summary>
/// Describes how an element gained focus.
/// </summary>
public enum FocusSource
{
	/// <summary>Focus gained through pointer interaction.</summary>
	Pointer,
	/// <summary>Focus gained through keyboard navigation (Tab, arrows).</summary>
	Keyboard,
	/// <summary>Focus set programmatically via code.</summary>
	Programmatic
}

/// <summary>
/// Resource tracking the global focus state.
/// </summary>
public struct FocusManager
{
	/// <summary>The entity ID that currently has focus (0 if none).</summary>
	public ulong FocusedEntity;

	/// <summary>Previous frame's focused entity.</summary>
	public ulong PreviousFocusedEntity;

	/// <summary>
	/// Request focus change for the next frame.
	/// Set to 0 to clear focus.
	/// </summary>
	public ulong RequestFocusEntity;

	public FocusSource RequestFocusSource;

	public readonly bool HasFocus => FocusedEntity != 0;
	public readonly bool FocusChanged => FocusedEntity != PreviousFocusedEntity;

	public void RequestFocus(ulong entityId, FocusSource source = FocusSource.Programmatic)
	{
		RequestFocusEntity = entityId;
		RequestFocusSource = source;
	}

	public void ClearFocus()
	{
		RequestFocusEntity = 0;
		RequestFocusSource = FocusSource.Programmatic;
	}
}

/// <summary>
/// Component to track computed Z-index for rendering order.
/// Computed via depth-first hierarchy traversal, similar to Bevy's ui_z_system.
/// </summary>
public struct ComputedZIndex
{
	/// <summary>
	/// Computed depth value. Higher values render on top.
	/// Based on:
	/// - Hierarchical depth (parents before children)
	/// - Sibling order (later siblings on top)
	/// - Manual ZIndex component if present
	/// </summary>
	public float Value;
}

/// <summary>
/// Optional component to manually override rendering order.
/// </summary>
public struct ZIndex
{
	/// <summary>
	/// Local z-index within parent context.
	/// Default is 0. Positive values render on top of siblings with lower values.
	/// </summary>
	public int Local;

	/// <summary>
	/// Global z-index that overrides all hierarchical ordering.
	/// Elements with Global set will render above/below all normal elements.
	/// </summary>
	public int? Global;

	public static ZIndex FromLocal(int local) => new() { Local = local, Global = null };
	public static ZIndex FromGlobal(int global) => new() { Local = 0, Global = global };
}

/// <summary>
/// Trigger event data for click interactions.
/// Use with On&lt;ClickEvent&lt;TMarker&gt;&gt; in entity observers.
/// The entity ID is provided by the On&lt;T&gt; wrapper.
/// </summary>
public readonly record struct ClickEvent<TMarker>
	where TMarker : struct
{
	// No data needed - the entity ID comes from On<T>
}

/// <summary>
/// Trigger event data for toggle/checkbox state changes.
/// Use with On&lt;ToggleEvent&gt; in entity observers.
/// </summary>
public readonly record struct ToggleEvent(bool NewValue)
{
}

/// <summary>
/// Trigger event data for slider value changes.
/// Use with On&lt;ValueChangedEvent&gt; in entity observers.
/// </summary>
public readonly record struct ValueChangedEvent(float NewValue)
{
}

/// <summary>
/// Trigger event data for focus gain.
/// Use with On&lt;FocusGainedEvent&gt; in entity observers.
/// </summary>
public readonly record struct FocusGainedEvent(FocusSource Source)
{
}

/// <summary>
/// Trigger event data for focus loss.
/// Use with On&lt;FocusLostEvent&gt; in entity observers.
/// </summary>
public readonly record struct FocusLostEvent
{
	// No data needed - the entity ID comes from On<T>
}
