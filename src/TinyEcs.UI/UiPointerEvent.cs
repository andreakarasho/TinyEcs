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

public readonly record struct UiPointerTrigger(UiPointerEvent Event, bool Propagate = false) : ITrigger, IEntityTrigger, IPropagatingTrigger
{
	public ulong EntityId => Event.CurrentTarget;
	public bool ShouldPropagate => Propagate;

#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
	{
	}
#else
	public void Register(TinyEcs.World world)
	{
	}
#endif
}
