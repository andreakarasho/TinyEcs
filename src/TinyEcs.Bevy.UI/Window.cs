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
		app.AddResource(new UiZCounter());
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
		ownerNode.Ref.PositionType = PositionType.Absolute;
		ownerNode.Ref.Left = Val.Px(anchor.Value.OriginX + delta.X);
		ownerNode.Ref.Top = Val.Px(anchor.Value.OriginY + delta.Y);
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
