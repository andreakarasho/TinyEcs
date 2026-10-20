using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.UI;

// Bevy-style interaction state markers (bevy_ui / bevy_ui_widgets). The
// Interaction enum stays as the per-frame field; these are sparse markers
// inserted / removed through Commands only on transitions, so a style system
// can filter on With<Hovered> / Added<Pressed> instead of polling every row.

/// <summary>On the <see cref="Interaction"/> entity the pointer is over (the pick's
/// topmost hit, resolved up to its nearest Interaction ancestor-or-self).</summary>
public struct Hovered;

/// <summary>On the <see cref="Interaction"/> entity a left press began on, for as
/// long as the button is held (it stays when the pointer drifts off).</summary>
public struct Pressed;

/// <summary>A disabled widget: it still paints and still blocks the pick (a press
/// on it does not reach whatever is behind or around it), but it gets no
/// Interaction state, no Hovered / Pressed marker and no pointer triggers
/// (UiClick, UiPointerDown, …).</summary>
public struct InteractionDisabled;

/// <summary>The on state of a checkbox / radio button: the marker IS the state
/// (Widgets.CheckboxPlugin / RadioPlugin read and, with SelfUpdate, write it).</summary>
public struct Checked;

/// <summary>The chosen entry of a single-selection widget (a ListBox's ListItem, a
/// TabList's Tab). The marker IS the state.</summary>
public struct Selected;

/// <summary>On the entity that holds <see cref="InputFocus"/> (kept by UiFocusPlugin
/// in PostUpdate), for styling the keyboard target like a hover.</summary>
public struct Focused;

/// <summary>Make a node a pick surface at its own paint position although it paints
/// nothing (an invisible hit or drag area): the plugin gives it an empty
/// <see cref="UiCustom"/> when it has none, so it emits its own (data-less) custom
/// render command and gets a ComputedNode. Renderers skip a null-data custom
/// command; a host PixelHitTest sees null data and should answer by bounds.</summary>
public struct UiPickable;

/// <summary>The usual in-window control: <see cref="Interaction"/>,
/// <see cref="UiNoWindowDrag"/> (a press works the control, not the window) and
/// <see cref="UiContainsByBounds"/> (its whole box is the hit).</summary>
public struct UiControl : IBundle
{
	public readonly void Insert(EntityCommands e)
	{
		e.Insert(Interaction.None);
		e.Insert(new UiNoWindowDrag());
		e.Insert(new UiContainsByBounds());
	}
}

/// <summary>Swap the entity's <see cref="BackgroundColor"/> between Normal and Hover
/// from its <see cref="Interaction"/> state (any non-None = Hover); a
/// <see cref="Focused"/> entity also shows Hover (keyboard navigation).</summary>
public struct UiHoverTint
{
	public ClayColor Normal;
	public ClayColor Hover;
}

/// <summary>Applies <see cref="UiHoverTint"/>. Runs in Stage.Last: after the
/// frame's handlers wrote Normal / Hover (a selected row swaps its own Normal)
/// and after the interaction pass stamped Interaction.</summary>
public sealed class UiHoverTintPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddSystem((Query<Data<Interaction, BackgroundColor, UiHoverTint>> q, Query<Data<Focused>> focused) => Apply(q, focused))
			.InStage(Stage.Last).Build();
	}

	private static void Apply(Query<Data<Interaction, BackgroundColor, UiHoverTint>> q, Query<Data<Focused>> focused)
	{
		foreach (var (ent, it, bg, hv) in q)
		{
			var want = it.Ref == Interaction.None && !focused.Contains(ent.Ref) ? hv.Ref.Normal : hv.Ref.Hover;
			if (bg.Ref.Value.R != want.R || bg.Ref.Value.G != want.G
				|| bg.Ref.Value.B != want.B || bg.Ref.Value.A != want.A)
			{
				bg.Ref = new BackgroundColor(want);
				q.SetChanged<BackgroundColor>(ent.Ref);
			}
		}
	}
}

/// <summary>Which kind of pointer gesture owns the current press.</summary>
public enum UiGestureMode : byte
{
	None,
	/// <summary>A non-window surface is being moved (e.g. a viewport frame).</summary>
	Move,
	/// <summary>A resize grip owns the press.</summary>
	Resize,
	/// <summary>A window or widget gesture owns the press (window drag, scrollbar
	/// thumb, slider, a widget's own press handler).</summary>
	Window,
}

/// <summary>
/// Cross-system drag arbitration. The first gesture system to see a press and
/// find the gate free claims it (sets <see cref="Mode"/>); every other gesture
/// skips latching until the owner releases it on button-up. Without it one
/// press can start two drags (a scrollbar thumb AND the window under it).
/// </summary>
public sealed class UiGestureGate
{
	public UiGestureMode Mode;

	public bool IsFree => Mode == UiGestureMode.None;

	/// <summary>Claim the gate for <paramref name="mode"/> if it is free.</summary>
	public bool TryClaim(UiGestureMode mode)
	{
		if (Mode != UiGestureMode.None) return false;
		Mode = mode;
		return true;
	}

	/// <summary>Release the gate if <paramref name="mode"/> holds it.</summary>
	public void Release(UiGestureMode mode)
	{
		if (Mode == mode) Mode = UiGestureMode.None;
	}
}
