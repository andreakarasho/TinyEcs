using System.Numerics;
using TinyEcs.Bevy;

namespace TinyEcs.UI;

public enum UiPointerEventType
{
	PointerEnter,
	PointerExit,
	PointerDown,
	PointerUp,
	PointerMove,
	PointerScroll
}

public readonly struct UiPointerEvent
{
	public UiPointerEvent(
		UiPointerEventType type,
		ulong target,
		ulong currentTarget,
		uint elementKey,
		Vector2 position,
		Vector2 moveDelta,
		Vector2 scrollDelta,
		bool isPrimaryButton)
	{
		Type = type;
		Target = target;
		CurrentTarget = currentTarget;
		ElementKey = elementKey;
		Position = position;
		MoveDelta = moveDelta;
		ScrollDelta = scrollDelta;
		IsPrimaryButton = isPrimaryButton;
	}

	public UiPointerEventType Type { get; }
	public ulong Target { get; }
	public ulong CurrentTarget { get; }
	public uint ElementKey { get; }
	public Vector2 Position { get; }
	public Vector2 MoveDelta { get; }
	public Vector2 ScrollDelta { get; }
	public bool IsPrimaryButton { get; }

	public bool IsOriginalTarget => Target == CurrentTarget;
}

/// <summary>
/// Trigger event data for pointer interactions.
/// Use with On&lt;UiPointerEvent&gt; in entity observers.
/// The entity ID is provided by the On&lt;T&gt; wrapper.
/// </summary>
public readonly record struct UiPointerTrigger(UiPointerEvent Event, bool Propagate = false) : IPropagatingTrigger
{
	public bool ShouldPropagate => Propagate;
}
