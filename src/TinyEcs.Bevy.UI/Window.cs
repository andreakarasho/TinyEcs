using System.Numerics;
using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI;

// Floating windows: the component vocabulary, the z-order counter, and the
// gestures every windowing layer needs (UiWindowPlugin): drag-to-move with
// bring-to-front, right-click close, popup dismissal. The gestures resolve the
// press through UiPick (the one hit-test), so a press on ANY painted child —
// pixel-perfect when the host sets UiClayContext.PixelHitTest — drags or
// closes its window; windows need no Interaction of their own.
//
// Input is the raw MouseInput (edges + consume, in UI-layout space): the close
// gesture consumes the right button so world systems under a window never see
// it. Hosts tune the gestures through UiWindowModifiers and opt elements out
// with UiNoWindowDrag / UiDragYield / UiMovableNoDrag / UiNoRightClickClose.

/// <summary>Tag for a floating, movable window ROOT. Only the root carries a
/// <see cref="GlobalZIndex"/> — the layout system threads that z down to every
/// descendant float, so bring-to-front is one in-place bump.</summary>
public struct UiMovable;

/// <summary>Root opt-out: still a window (close / click-capture semantics may
/// apply), but the drag gesture is suppressed.</summary>
public struct UiMovableNoDrag;

/// <summary>Marker for an interactive child of a <see cref="UiMovable"/> window
/// (button, resize handle, checkbox) whose press must NOT latch a window drag —
/// the gesture yields so the control's own click / drag handler runs.</summary>
public struct UiNoWindowDrag;

/// <summary>An element that owns its own press (a draggable item, a drag source):
/// a press on it still brings its window to front but never latches a window
/// drag. Checked on the topmost hit only — unlike <see cref="UiNoWindowDrag"/>,
/// which is found by bounds.</summary>
public struct UiDragYield;

/// <summary>Window root opt-out for the right-click-close gesture. The press edge
/// is NOT consumed while parked over an opted-out window (consuming it would
/// starve the window's own right-click gesture, e.g. a context menu); the held
/// press is still consumed so world systems don't act under the window.</summary>
public struct UiNoRightClickClose;

/// <summary>Popup root (context menu, dropdown, completion list): a press outside
/// its subtree despawns it, as does any right press. Layout is the root's own
/// Node; this marker is only the gesture.</summary>
public struct UiPopup;

/// <summary>
/// Entity event asking a window to close (the right-click gesture, a host
/// action). Emitted on the window root, not propagated. Without
/// <see cref="UiManualClose"/> UiWindowPlugin's default observer despawns the
/// root (and with it the subtree); other observers run too, so a host can react
/// (persist, notify a server) and still let the default close it.
/// </summary>
public struct UiCloseRequested;

/// <summary>The window closes itself in response to <see cref="UiCloseRequested"/>
/// (routes through its own teardown); the default despawn is skipped.</summary>
public struct UiManualClose;

/// <summary>Per-frame drag state published by UiWindowPlugin's drag for
/// late consumers (anchoring). Owner/FrameDelta are set every frame a window
/// moves; JustReleased only on the release frame.</summary>
public sealed class ActiveWindowDrag
{
	public ulong Owner;
	public Vector2 FrameDelta;
	public ulong JustReleased;
}

/// <summary>
/// Host-fed gesture modifiers for UiWindowPlugin, written each frame before
/// PreUpdate (the library reads no keyboard bindings itself).
/// </summary>
public sealed class UiWindowModifiers
{
	/// <summary>A press latches a window drag only while DragModifierHeld.</summary>
	public bool DragRequiresModifier;
	public bool DragModifierHeld;
	/// <summary>A drag-and-drop payload rides the cursor: no window drag latches, so
	/// the press drops onto the window instead of moving it.</summary>
	public bool DragSuppressed;
	/// <summary>Dragging an anchored window detaches it from its group instead of
	/// moving the group.</summary>
	public bool AnchorDetachHeld;
	/// <summary>Right-click close of an anchored window needs CloseAnchoredModifierHeld.</summary>
	public bool CloseAnchoredRequiresModifier;
	public bool CloseAnchoredModifierHeld;
	/// <summary>Right-click close of an anchored window closes its whole group.</summary>
	public bool CloseAnchoredGroup;
}

/// <summary>
/// Monotonic z-order counter for floating UI windows. Bump on focus/spawn so
/// the most recently interacted window draws and hit-tests on top. Clamped to
/// short.MaxValue inside the layout system; a session-long counter overflow is
/// unrealistic in practice.
/// </summary>
public sealed class UiZCounter
{
	private int _next = 1;

	/// <summary>
	/// GlobalZIndex values at or above this are PINNED overlays (tooltips,
	/// death screen, drag previews, modal boxes) — outside the interactive
	/// window stack. They never ratchet the counter: Clay's Floating.ZIndex is a
	/// short, so a ratchet to e.g. short.MaxValue+1 would push every later bump
	/// past the clamp, tie all windows at 32767 and freeze them in spawn order.
	/// </summary>
	public const int OverlayFloor = 10000;

	/// <summary>
	/// The value the next Bump() will issue. Ratchet: only ever moves up, and
	/// only for floors below <see cref="OverlayFloor"/>.
	/// Windows that spawn declaring a GlobalZIndex ABOVE the counter (the mod
	/// windows pick 50-100 to sit over the ordinary gump stack) would otherwise
	/// leave the counter behind, so every bump-issuing call site returns values
	/// that cannot beat their floor for dozens of presses and they look pinned
	/// in spawn order. The host drag plugin's OnInsert observer ratchets this
	/// to floor+1 on every insert.
	/// </summary>
	public int Next
	{
		get => _next;
		set { if (value > _next && value < OverlayFloor) _next = value; }
	}

	public int Bump() => _next++;
}

/// <summary>
/// Hand-off for a window that must start dragging without its own press edge —
/// set Owner to a just-spawned window root and the drag system latches it onto
/// the held cursor on the next frame (the spawn is a deferred command),
/// re-centered under the pointer.
/// </summary>
public sealed class ForcedWindowDrag
{
	public ulong Owner;
}

public static class UiHierarchy
{
	/// <summary>Despawn an entity and its whole child subtree (depth-first).</summary>
	public static void DespawnSubtree(Commands commands, ulong entity, Query<Data<TinyEcs.Children>> childrenQ)
	{
		if (childrenQ.Contains(entity))
		{
			var (_, kids) = childrenQ.Get(entity);
			foreach (var cid in kids.Ref)
				DespawnSubtree(commands, cid, childrenQ);
		}
		commands.Entity(entity).Despawn();
	}

	/// <summary>The stacking order of the window owning <paramref name="entity"/>: the
	/// nearest GlobalZIndex up the Parent chain (only window roots carry one), or
	/// int.MinValue when no ancestor has one.</summary>
	public static int ResolveWindowZ(
		ulong entity,
		Query<Data<TinyEcs.Parent>> parentQ,
		Query<Data<GlobalZIndex>> zQ)
	{
		var cur = entity;
		for (var guard = 0; guard < 64; guard++)
		{
			if (zQ.TryGet(cur, out var zRow))
			{
				var (_, z) = zRow;
				return z.Ref.Value;
			}
			if (!parentQ.TryGet(cur, out var parentRow)) break;
			var (_, parent) = parentRow;
			cur = parent.Ref.Id;
		}
		return int.MinValue;
	}
}

/// <summary>
/// The z-order bookkeeping every windowing layer needs: registers the
/// <see cref="UiZCounter"/> and resolves declared <see cref="GlobalZIndex"/>
/// values against it. A NON-NEGATIVE value is a floor the counter must respect:
/// it ratchets up to floor+1 (never down). Windows that spawn declaring a z above
/// the counter (the mod windows pick 50-100 to sit over the ordinary window
/// stack) would otherwise leave the counter behind — every press/focus bump then
/// returns a value that cannot beat the floor, the windows look pinned in spawn
/// order, and the forced-drag path even demotes them. In-place bump writes never
/// exceed the counter by construction, so no feedback loop.
/// A NEGATIVE value is a topmost REQUEST: resolved here, at insert time, to the
/// counter's current top. A fixed floor can never do that job — the counter climbs
/// one per click, so any constant loses to the windows a player keeps clicking, and
/// a freshly opened menu would draw UNDER the very window it was opened on.
/// </summary>
public sealed class UiZOrderPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddResource(new UiZCounter());

		app.AddObserver((OnInsert<GlobalZIndex> trig, ResMut<UiZCounter> counter, Query<Data<GlobalZIndex>> zQ) =>
		{
			var v = trig.Component.Value;
			if (v < 0)
			{
				var top = counter.Value.Bump();
				var (_, gz) = zQ.Get(trig.EntityId);
				gz.Ref.Value = top;   // in-place: OnInsert does not re-fire on updates
				zQ.SetChanged<GlobalZIndex>(trig.EntityId);
			}
			else
			{
				counter.Value.Next = v + 1;
			}
		});

		// A window ROOT spawning with a declared floor that some OTHER window
		// already sits at or above (the mod's main frame at 60 got focus-bumped
		// to 61+, then its detail frame spawned declaring 61) would tie with or
		// sit under the window it was meant to cover — a tie makes Clay's stable
		// sort pick tree order and the pixel pick resolve the wrong window. Give
		// such a root the counter's top instead: never below its floor, and a
		// freshly opened window landing on top is what gumps do anyway. A value
		// taken from Bump() is already above every window, so host spawns are
		// untouched. OnAdd (first insert only) so a mod re-sending the same
		// GlobalZIndex every tick doesn't climb the counter or yank the window
		// over the one the player just clicked. Pinned overlays are left alone.
		app.AddObserver((OnAdd<GlobalZIndex> trig, ResMut<UiZCounter> counter, Query<Data<GlobalZIndex>, Filter<With<UiMovable>>> movZ) =>
		{
			var v = trig.Component.Value;
			if (v < 0 || v >= UiZCounter.OverlayFloor) return;
			if (!movZ.Contains(trig.EntityId)) return;
			var covered = false;
			foreach (var (e, z) in movZ)
				if (e.Ref != trig.EntityId && z.Ref.Value >= v && z.Ref.Value < UiZCounter.OverlayFloor) { covered = true; break; }
			if (!covered) return;
			var top = counter.Value.Bump();
			var (_, gz) = movZ.Get(trig.EntityId);
			gz.Ref.Value = top;
			movZ.SetChanged<GlobalZIndex>(trig.EntityId);
		});
	}
}

/// <summary>
/// Keeps floating windows inside the UI surface when it SHRINKS (a ui-scale
/// setting, the OS window resizing): stranding a window off-screen with no way
/// to drag it back is the failure this corrects. Each <see cref="UiMovable"/>
/// root's Left/Top is clamped to [0, surface - size], the rendered extent being
/// read from the previous frame's ComputedNode (one frame of layout lag is
/// acceptable here). A GROWING surface never moves anything. Runs only when the
/// size actually changed, so it is quiet at steady state.
/// </summary>
public sealed class UiSurfaceBoundsPlugin : IPlugin
{
	public void Build(App app)
	{
		var fn = ClampToSurface;
		app.AddSystem(fn).InStage(Stage.Update).Build();
	}

	private static void ClampToSurface(
		Res<UiSurface> surface,
		Local<Vector2> lastSize,
		Query<Data<Node, ComputedNode>, Filter<With<UiMovable>>> movables)
	{
		var size = surface.Value.LogicalSize;
		// Idempotent, but only walk the windows on an actual change. lastSize starts
		// (0,0), so the first frame runs the pass once and then goes quiet.
		if (MathF.Abs(size.X - lastSize.Value.X) < 0.5f
			&& MathF.Abs(size.Y - lastSize.Value.Y) < 0.5f)
			return;
		lastSize.Value = size;

		float sw = size.X;
		float sh = size.Y;

		foreach (var (ent, node, comp) in movables)
		{
			float w = comp.Ref.Size.X;
			float h = comp.Ref.Size.Y;

			if (node.Ref.Left.Type == ValType.Px)
			{
				float left = node.Ref.Left.Value;
				float clamped = Math.Clamp(left, 0f, MathF.Max(0f, sw - w));
				if (clamped != left)
				{
					node.Ref.Left = Val.Px(clamped);
					movables.SetChanged<Node>(ent.Ref);
				}
			}
			if (node.Ref.Top.Type == ValType.Px)
			{
				float top = node.Ref.Top.Value;
				float clamped = Math.Clamp(top, 0f, MathF.Max(0f, sh - h));
				if (clamped != top)
				{
					node.Ref.Top = Val.Px(clamped);
					movables.SetChanged<Node>(ent.Ref);
				}
			}
		}
	}
}

