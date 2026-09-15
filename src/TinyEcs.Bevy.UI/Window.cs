using System.Numerics;

namespace TinyEcs.Bevy.UI;

// Floating-window primitives: the component vocabulary + z-order counter any
// windowing layer needs, plus an optional Interaction-driven drag plugin.
//
// Two integration levels:
//   * Components only — a host with its own gesture resolution (custom hit
//     testing, pixel-perfect masks) tags roots with UiMovable, registers
//     UiZCounter / ForcedWindowDrag itself and runs its own drag systems.
//   * UiWindowPlugin — full drag + bring-to-front handling driven by the
//     library's own pointer/Interaction pipeline. Requires windows (or their
//     children) to carry Interaction so the pointer can land on them.
//
// Right-click-close is NOT provided here: UiPointer models a single button, so
// secondary-button gestures stay host-side.

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
	/// The value the next Bump() will issue. Ratchet: only ever moves up.
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
		set { if (value > _next) _next = value; }
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

/// <summary>
/// Interaction-driven window drag + bring-to-front. The press must land on an
/// Interaction-bearing element inside the window (the pointer pipeline only
/// sees those); the system walks the Parent chain to the owning UiMovable
/// root. Hosts with custom hit-testing skip this plugin and keep the
/// components.
/// </summary>
public sealed class UiWindowPlugin : IPlugin
{
	private struct DragAnchor
	{
		public bool Active;
		public ulong Owner;
		public Vector2 Pointer;
		public float OriginX, OriginY;
	}

	public void Build(App app)
	{
		app.AddPlugin(new UiZOrderPlugin());
		app.AddResource(new ForcedWindowDrag());

		// Runs in UiPostLayoutStage after InteractionSystem.PostLayout
		// (declaration order — UiPlugin must be added first), so PressedEntity
		// is fresh for the frame the press edge fired.
		app.AddSystem((
			Res<UiPointer> pointer,
			Res<UiClayContext> ctx,
			Res<UiZCounter> zCounter,
			Res<ForcedWindowDrag> forced,
			Local<DragAnchor> anchor,
			Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>> movables,
			Query<Data<TinyEcs.Parent>> parents,
			Query<Data<UiMovableNoDrag>> noDrag,
			Query<Data<UiNoWindowDrag>> noChildDrag) =>
			Drag(pointer, ctx, zCounter, forced, anchor, movables, parents, noDrag, noChildDrag))
			.InStage(UiPlugin.UiPostLayoutStage).SingleThreaded().Build();
	}

	private static void Drag(
		Res<UiPointer> pointer,
		Res<UiClayContext> ctx,
		Res<UiZCounter> zCounter,
		Res<ForcedWindowDrag> forced,
		Local<DragAnchor> anchor,
		Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>> movables,
		Query<Data<TinyEcs.Parent>> parents,
		Query<Data<UiMovableNoDrag>> noDrag,
		Query<Data<UiNoWindowDrag>> noChildDrag)
	{
		ref readonly var p = ref pointer.Value;

		if (!p.Down)
		{
			anchor.Value.Active = false;
			anchor.Value.Owner = 0;
			forced.Value.Owner = 0;
			return;
		}

		// Forced drag: latch the frame the requested entity materialises,
		// re-centered under the pointer.
		if (forced.Value.Owner != 0 && movables.Contains(forced.Value.Owner))
		{
			var ownerF = forced.Value.Owner;
			var (_, nodeF, zF) = movables.Get(ownerF);
			float wF = nodeF.Ref.Width.Type == ValType.Px ? nodeF.Ref.Width.Value : 0f;
			float hF = nodeF.Ref.Height.Type == ValType.Px ? nodeF.Ref.Height.Value : 0f;
			anchor.Value = new DragAnchor
			{
				Active = true,
				Owner = ownerF,
				Pointer = p.Position,
				OriginX = p.Position.X - wF / 2f,
				OriginY = p.Position.Y - hF / 2f,
			};
			zF.Ref.Value = zCounter.Value.Bump();
			movables.SetChanged<GlobalZIndex>(ownerF);
			forced.Value.Owner = 0;
		}

		// Latch on the gesture's press target. PressedEntity stays set while
		// held, so a failed walk retries cheaply; anchor.Active stops re-latch.
		if (!anchor.Value.Active && ctx.Value.PressedEntity != 0)
		{
			var owner = ResolveWindow(ctx.Value.PressedEntity, movables, parents, noChildDrag);
			if (owner == 0 || noDrag.Contains(owner))
				return;

			var (_, node, z) = movables.Get(owner);
			float ox = node.Ref.Left.Type == ValType.Px ? node.Ref.Left.Value : 0f;
			float oy = node.Ref.Top.Type == ValType.Px ? node.Ref.Top.Value : 0f;
			anchor.Value = new DragAnchor
			{
				Active = true,
				Owner = owner,
				Pointer = p.Position,
				OriginX = ox,
				OriginY = oy,
			};
			z.Ref.Value = zCounter.Value.Bump();
			movables.SetChanged<GlobalZIndex>(owner);
		}

		if (!anchor.Value.Active)
			return;

		if (!movables.Contains(anchor.Value.Owner))
		{
			anchor.Value.Active = false;
			anchor.Value.Owner = 0;
			return;
		}

		var delta = p.Position - anchor.Value.Pointer;
		var (_, ownerNode, _) = movables.Get(anchor.Value.Owner);
		var left = Val.Px(anchor.Value.OriginX + delta.X);
		var top = Val.Px(anchor.Value.OriginY + delta.Y);
		ref var n = ref ownerNode.Ref;
		// Held-but-stationary frames must not mark the window changed, or the
		// relayout gate stays open for the whole gesture.
		if (n.PositionType == PositionType.Absolute && n.Left == left && n.Top == top)
			return;
		n.PositionType = PositionType.Absolute;
		n.Left = left;
		n.Top = top;
		movables.SetChanged<Node>(anchor.Value.Owner);
	}

	// Walk from the pressed element up the Parent chain to the owning UiMovable
	// root. A UiNoWindowDrag anywhere on the path (the control itself or a
	// wrapper) yields the gesture.
	private static ulong ResolveWindow(
		ulong entity,
		Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>> movables,
		Query<Data<TinyEcs.Parent>> parents,
		Query<Data<UiNoWindowDrag>> noChildDrag)
	{
		var current = entity;
		// Depth-capped against a cyclic or malformed parent link.
		for (var i = 0; i < 32 && current != 0; i++)
		{
			if (noChildDrag.Contains(current))
				return 0;
			if (movables.Contains(current))
				return current;
			if (!parents.Contains(current))
				return 0;
			var (_, parent) = parents.Get(current);
			current = (ulong)parent.Ref.Id;
		}
		return 0;
	}
}
