using System.Numerics;
using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI;

/// <summary>
/// The window gestures, resolved through <see cref="UiPick"/> over the raw
/// <see cref="MouseInput"/>:
/// <list type="bullet">
/// <item>Drag (Stage.Update): a left press on any painted element of a
/// <see cref="UiMovable"/> window brings it to front and latches a move;
/// <see cref="UiNoWindowDrag"/> controls (by bounds), <see cref="UiDragYield"/>
/// hits and <see cref="UiMovableNoDrag"/> roots focus without moving.
/// <see cref="ForcedWindowDrag"/> hands a fresh window to the held cursor.</item>
/// <item>Right-click close (PreUpdate): press + release over the same window emits
/// <see cref="UiCloseRequested"/> on it (drag-off cancels); the right button is
/// consumed while parked over a window.</item>
/// <item>Popup dismissal (PreUpdate): see <see cref="UiPopup"/>.</item>
/// </list>
/// </summary>
public sealed class UiWindowPlugin : IPlugin
{
	/// <summary>Label of the Stage.Update drag system: a host that feeds
	/// <see cref="UiWindowModifiers.DragSuppressed"/> from same-frame Update state
	/// orders its feed <c>.Before(DragLabel)</c>.</summary>
	public const string DragLabel = "ui:window_drag";

	public void Build(App app)
	{
		app.AddPlugin(new UiZOrderPlugin());
		if (!app.HasResource<MouseInput>()) app.AddResource(new MouseInput());
		if (!app.HasResource<UiGestureGate>()) app.AddResource(new UiGestureGate());
		app.AddResource(new ForcedWindowDrag());
		app.AddResource(new ActiveWindowDrag());
		app.AddResource(new UiWindowModifiers());

		// Popups first: they must see the right press edge CloseOnRightClick consumes.
		var dismissFn = DismissPopups;
		app.AddSystem(dismissFn).InStage(Stage.PreUpdate).Build();
		// Every frame in PreUpdate: it keeps consuming the held right button while
		// it is parked over a window and fires the close on RELEASE.
		var closeFn = CloseOnRightClick;
		app.AddSystem(closeFn).InStage(Stage.PreUpdate).Build();
		var dragFn = Drag;
		app.AddSystem(dragFn).InStage(Stage.Update).Label(DragLabel).Build();

		app.AddObserver<On<UiCloseRequested>, Commands, Query<Data<UiManualClose>>>(DefaultClose);
	}

	private static void DefaultClose(On<UiCloseRequested> trigger, Commands commands, Query<Data<UiManualClose>> manual)
	{
		if (!manual.Contains(trigger.EntityId))
			commands.Entity(trigger.EntityId).Despawn();
	}

	// A press that does not land inside a popup's subtree despawns it (the Bevy
	// "marker + ancestor walk on press" idiom, on the raw press because pointer
	// events only reach Interaction-bearing elements). A press INSIDE is left alone
	// so the button's own press+release UiClick still lands. Any RIGHT press closes
	// it, inside or out: a context menu has no right-click meaning, and the row
	// under it wants the next one. The press is not consumed.
	private static void DismissPopups(
		Commands commands,
		Res<MouseInput> mouse,
		UiGesturePick pick,
		Query<Data<UiPopup>> popups,
		Query<Data<Widgets.Popover>> popovers,
		Query<Data<Widgets.MenuPopup>> menus)
	{
		bool left = mouse.Value.IsPressedOnce(MouseButton.Left);
		bool right = mouse.Value.IsPressedOnce(MouseButton.Right);
		if (!left && !right)
			return;
		bool any = false;
		foreach (var _ in popups)
		{
			any = true;
			break;
		}
		if (!any)
			return;

		ulong hitEntity = 0;
		if (left)
		{
			var hit = pick.Topmost(mouse.Value.Position);
			hitEntity = hit.Found ? hit.Entity : 0UL;
		}

		foreach (var (ent, _) in popups)
		{
			if (hitEntity != 0 && Within(hitEntity, ent.Ref, pick))
				continue;
			// A press on a popover's anchor / a menu's button is theirs to handle
			// (the button toggles its menu on release): dismissing here would reopen it.
			if (hitEntity != 0 && popovers.TryGet(ent.Ref, out var popRow))
			{
				var (_, pop) = popRow;
				if (pop.Ref.Anchor != 0 && Within(hitEntity, pop.Ref.Anchor, pick))
					continue;
			}
			if (hitEntity != 0 && menus.TryGet(ent.Ref, out var menuRow))
			{
				var (_, menu) = menuRow;
				if (menu.Ref.Owner != 0 && Within(hitEntity, menu.Ref.Owner, pick))
					continue;
			}
			commands.Entity(ent.Ref).Despawn();
		}
	}

	private static bool Within(ulong entity, ulong ancestor, UiGesturePick pick)
	{
		var e = entity;
		for (var depth = 0; depth < 64 && e != 0; depth++)
		{
			if (e == ancestor) return true;
			if (!pick.Parents.TryGet(e, out var row)) return false;
			var (_, parent) = row;
			e = (ulong)parent.Ref.Id;
		}
		return false;
	}

	private static void CloseOnRightClick(
		Commands commands,
		ResMut<MouseInput> mouse,
		Res<UiWindowModifiers> mods,
		Local<ulong> pressTarget,
		UiGesturePick pick,
		Query<Data<UiNoRightClickClose>> noCloseQ,
		Query<Data<UiAnchorMember>> anchorQ)
	{
		bool once = mouse.Value.IsPressedOnce(MouseButton.Right);
		bool held = mouse.Value.IsPressed(MouseButton.Right);
		bool released = mouse.Value.IsReleased(MouseButton.Right);

		if (!once && !held && !released && pressTarget.Value == 0)
			return;

		// Press: latch the window under the cursor (don't close yet) and consume so
		// world systems don't see the right press — EXCEPT for windows that own a
		// right-click gesture of their own (UiNoRightClickClose), which latch on
		// this same press: consuming their edge would starve it.
		if (once)
		{
			pressTarget.Value = pick.TopmostMovable(mouse.Value.Position);
			if (pressTarget.Value != 0 && !noCloseQ.Contains(pressTarget.Value))
				mouse.Value.Consume(MouseButton.Right);
			return;
		}

		// Held: keep the press consumed while parked over the latched window.
		if (held)
		{
			if (pressTarget.Value != 0)
				mouse.Value.Consume(MouseButton.Right);
			return;
		}

		if (!released)
			return;

		var target = pressTarget.Value;
		pressTarget.Value = 0;
		if (target == 0)
			return;

		// Opt-out windows stay open. Return BEFORE consuming so a window that owns
		// a right-click gesture opening on THIS release still sees it.
		if (noCloseQ.Contains(target))
			return;

		mouse.Value.Consume(MouseButton.Right);

		if (pick.TopmostMovable(mouse.Value.Position) != target)
			return; // dragged off: cancel, like UiClick

		if (anchorQ.TryGet(target, out var anchorRow))
		{
			var (_, member) = anchorRow;
			if (mods.Value.CloseAnchoredRequiresModifier && !mods.Value.CloseAnchoredModifierHeld)
				return;
			if (mods.Value.CloseAnchoredGroup)
			{
				int group = member.Ref.GroupId;
				foreach (var (ent, m) in anchorQ)
					if (m.Ref.GroupId == group && !noCloseQ.Contains(ent.Ref))
						commands.Entity(ent.Ref).EmitTrigger(new UiCloseRequested());
				return;
			}
		}

		commands.Entity(target).EmitTrigger(new UiCloseRequested());
	}

	private struct DragAnchor
	{
		public bool Active;
		public ulong Owner;
		public Vector2 Mouse;
		public float OriginX;
		public float OriginY;
	}

	private static void Drag(
		Res<MouseInput> mouse,
		Res<UiZCounter> zCounter,
		ResMut<UiGestureGate> gate,
		ResMut<ForcedWindowDrag> forced,
		ResMut<ActiveWindowDrag> activeDrag,
		Res<UiWindowModifiers> mods,
		Res<UiClayContext> clay,
		Local<DragAnchor> anchor,
		UiGesturePick pick,
		Query<Data<UiDragYield>> yieldQ,
		Query<Data<UiMovableNoDrag>> noDragQ,
		Query<Data<Node>, Filter<With<UiNoWindowDrag>>> noWindowDragBoundsQ)
	{
		// One-shot signals for late consumers this same frame. Reset every frame.
		activeDrag.Value.Owner = 0;
		activeDrag.Value.FrameDelta = Vector2.Zero;
		activeDrag.Value.JustReleased = 0;

		// IsPressed is false on the press-once frame, so include IsPressedOnce or
		// the latch below would be skipped on the very frame it needs to fire.
		bool held = mouse.Value.IsPressed(MouseButton.Left)
				 || mouse.Value.IsPressedOnce(MouseButton.Left);
		if (!held)
		{
			if (anchor.Value.Active && anchor.Value.Owner != 0)
				activeDrag.Value.JustReleased = anchor.Value.Owner;
			anchor.Value.Active = false;
			anchor.Value.Owner = 0;
			forced.Value.Owner = 0;
			gate.Value.Release(UiGestureMode.Window);
			return;
		}

		// A payload rides the cursor: dragging it over a window must drop onto the
		// window, not latch a move that drags the window along.
		if (mods.Value.DragSuppressed)
		{
			anchor.Value.Active = false;
			anchor.Value.Owner = 0;
			forced.Value.Owner = 0;
			return;
		}

		// Forced drag: a window spawned mid-gesture asks to ride the cursor without
		// a fresh press. Latch it the frame its entity materialises (spawn is
		// deferred), re-centered under the cursor. Exempt from the modifier rule.
		if (forced.Value.Owner != 0 && pick.Movables.TryGet(forced.Value.Owner, out var forcedRow))
		{
			var ownerF = forced.Value.Owner;
			var (_, nodeF, _) = forcedRow;
			var pos = mouse.Value.Position;
			float wF = nodeF.Ref.Width.Type == ValType.Px ? nodeF.Ref.Width.Value : 0f;
			float hF = nodeF.Ref.Height.Type == ValType.Px ? nodeF.Ref.Height.Value : 0f;
			anchor.Value = new DragAnchor
			{
				Active = true,
				Owner = ownerF,
				Mouse = pos,
				OriginX = pos.X - wF / 2f,
				OriginY = pos.Y - hF / 2f,
			};
			// A forced hand-off outranks whatever latched the press.
			gate.Value.Mode = UiGestureMode.Window;
			BringToFront(ownerF, pick, zCounter.Value);
			forced.Value.Owner = 0;
		}

		if (mouse.Value.IsPressedOnce(MouseButton.Left) && gate.Value.IsFree)
			TryLatch(mouse.Value.Position, gate.Value, zCounter.Value, mods.Value, clay.Value,
				ref anchor.Value, pick, yieldQ, noDragQ, noWindowDragBoundsQ);

		if (!anchor.Value.Active) return;

		if (!pick.Movables.TryGet(anchor.Value.Owner, out var anchorRow))
		{
			gate.Value.Release(UiGestureMode.Window);
			anchor.Value.Active = false;
			anchor.Value.Owner = 0;
			return;
		}

		var delta = mouse.Value.Position - anchor.Value.Mouse;
		var (_, ownerNode, _) = anchorRow;
		// Snap to integer: descendants floor their own absolute box independently
		// at render. A fractional window origin makes each child cross the integer
		// boundary at a different sub-pixel offset, so the gaps between children
		// wobble by a pixel while dragging.
		float prevLeft = ownerNode.Ref.Left.Type == ValType.Px ? ownerNode.Ref.Left.Value : 0f;
		float prevTop = ownerNode.Ref.Top.Type == ValType.Px ? ownerNode.Ref.Top.Value : 0f;
		float newLeft = MathF.Round(anchor.Value.OriginX + delta.X);
		float newTop = MathF.Round(anchor.Value.OriginY + delta.Y);
		ref var n = ref ownerNode.Ref;
		// Held-but-stationary frames must not mark the window changed, or the
		// relayout gate stays open for the whole gesture.
		if (n.PositionType != PositionType.Absolute
			|| n.Left != Val.Px(newLeft) || n.Top != Val.Px(newTop))
		{
			n.PositionType = PositionType.Absolute;
			n.Left = Val.Px(newLeft);
			n.Top = Val.Px(newTop);
			pick.Movables.SetChanged<Node>(anchor.Value.Owner);
		}

		// Publish the EXACT whole-pixel move applied this frame so anchored
		// siblings shift by the same integer amount and the group stays rigid.
		activeDrag.Value.Owner = anchor.Value.Owner;
		activeDrag.Value.FrameDelta = new Vector2(newLeft - prevLeft, newTop - prevTop);
	}

	private static void TryLatch(
		Vector2 pos,
		UiGestureGate gate,
		UiZCounter zCounter,
		UiWindowModifiers mods,
		UiClayContext clay,
		ref DragAnchor anchor,
		UiGesturePick pick,
		Query<Data<UiDragYield>> yieldQ,
		Query<Data<UiMovableNoDrag>> noDragQ,
		Query<Data<Node>, Filter<With<UiNoWindowDrag>>> noWindowDragBoundsQ)
	{
		// Topmost element under the cursor: ALL painted elements, not just window
		// roots, so the drag starts on an opaque child where the window's own
		// background is transparent.
		var hit = pick.Topmost(pos);
		if (!hit.Found) return;

		var owner = pick.MovableRoot(hit.Entity);
		if (owner == 0) return;

		// Focus = bring to front on ANY press over the window, before the drag
		// exclusions below: a press on a control or a no-drag window still focuses.
		BringToFront(owner, pick, zCounter);

		if (mods.DragRequiresModifier && !mods.DragModifierHeld) return;
		if (yieldQ.Contains(hit.Entity)) return;

		// Interactive in-window controls opt out by BOUNDS: when a window root
		// paints an opaque background its paint order (inflated by the root's
		// GlobalZIndex) can outrank its own flow children, so the pick may return
		// the ROOT for a press on a control. The box comes from Clay's layout, not
		// ComputedNode: a control that paints only through a text child has no
		// ComputedNode of its own; Clay's map covers every laid-out element and is
		// rebuilt per layout, so hidden subtrees drop out instead of lingering.
		foreach (var (e, nd) in noWindowDragBoundsQ)
		{
			if (nd.Ref.Display == Display.None) continue;
			if (!clay.TryGetElementBoundingBox(e.Ref, out var b)) continue;
			if (pos.X < b.X || pos.X >= b.X + b.Width
				|| pos.Y < b.Y || pos.Y >= b.Y + b.Height) continue;
			// A control taller than its scroll viewport keeps its FULL box in Clay
			// while only the visible slice can be pressed.
			if (UiPick.HiddenOrClippedOut(e.Ref, pos, pick.Rendered, pick.Parents, pick.Nodes))
				continue;
			if (pick.MovableRoot(e.Ref) == owner)
				return;
		}

		if (noDragQ.Contains(owner)) return;
		if (!gate.TryClaim(UiGestureMode.Window)) return;

		var (_, node, _) = pick.Movables.Get(owner);
		anchor = new DragAnchor
		{
			Active = true,
			Owner = owner,
			Mouse = pos,
			OriginX = node.Ref.Left.Type == ValType.Px ? node.Ref.Left.Value : 0f,
			OriginY = node.Ref.Top.Type == ValType.Px ? node.Ref.Top.Value : 0f,
		};
	}

	/// <summary>
	/// Lift a window ROOT to the top of the stack. Only the root carries a z; the
	/// layout threads it down to every descendant float, so one in-place bump lifts
	/// the whole window. A declared z is a FLOOR, not a starting point: the counter
	/// may sit below a window that asked to live above the ordinary stack, and an
	/// unconditional assignment would demote it on its first press.
	/// </summary>
	public static void BringToFront(ulong owner, UiGesturePick pick, UiZCounter zCounter)
	{
		if (!pick.Movables.TryGet(owner, out var row)) return;
		var (_, _, rootZ) = row;
		var next = zCounter.Bump();
		if (next > rootZ.Ref.Value)
		{
			rootZ.Ref.Value = next;
			pick.Movables.SetChanged<GlobalZIndex>(owner);
		}
	}
}
