// Window anchoring: drag a movable window near another of the same kind and it
// snaps flush to the nearest edge; anchored windows form a group that drags
// together (and, with UiWindowModifiers.CloseAnchoredGroup, closes together).
//
// A window stores only its group id (UiAnchorMember); snap is edge-to-edge
// against the nearest overlapping neighbour, and move-together replays the
// dragged window's per-frame delta (ActiveWindowDrag) onto its siblings.
// There is no per-cell occupancy: anchoring 3+ windows to the SAME side of one
// window can overlap them.

using System.Numerics;
using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.UI;

/// <summary>Tag on a movable window root that may anchor to windows of the same
/// <see cref="Kind"/> (a host-defined category).</summary>
public struct UiAnchorable { public int Kind; }

/// <summary>Group membership: every window of one anchored block shares a GroupId
/// (from <see cref="UiAnchorCounter"/>). Dies with the entity, so closing a window
/// leaves its group.</summary>
public struct UiAnchorMember { public int GroupId; }

public sealed class UiAnchorCounter { public int Next = 1; }

/// <summary>The live drop-preview entity (translucent ghost at the snap target while
/// dragging). One reused entity, despawned when the drag ends.</summary>
public sealed class UiAnchorPreview { public ulong Entity; }

public sealed class UiAnchorPlugin : IPlugin
{
	/// <summary>Fill of the drop-preview ghost.</summary>
	public ClayColor PreviewColor = new(192, 192, 192, 128);
	/// <summary>Pinned z of the ghost (above the window stack, see UiZCounter.OverlayFloor).</summary>
	public int PreviewZ = 40000;

	public void Build(App app)
	{
		app.AddResource(new UiAnchorCounter());
		app.AddResource(new UiAnchorPreview());
		var color = PreviewColor;
		var z = PreviewZ;
		// Stage.Last: after UiWindowPlugin's drag (Stage.Update) has written the
		// dragged window's position and published ActiveWindowDrag this frame.
		var moveFn = MoveGroupTogether;
		var snapFn = SnapOnDrop;
		app.AddSystem(moveFn).InStage(Stage.Last).Build();
		app.AddSystem(snapFn).InStage(Stage.Last).Build();
		app.AddSystem((
			Res<ActiveWindowDrag> drag,
			ResMut<UiAnchorPreview> preview,
			Commands commands,
			Query<Data<UiAnchorable, Node, ComputedNode>> anchorablesQ,
			Query<Data<UiAnchorMember>> membersQ,
			Query<Data<Node>> nodeQ) =>
			UpdateDropPreview(drag, preview, commands, anchorablesQ, membersQ, nodeQ, color, z))
			.InStage(Stage.Last).Build();
	}

	// While a grouped window is dragged, carry its siblings by the same per-frame
	// delta. The detach modifier detaches the dragged window instead.
	private static void MoveGroupTogether(
		Res<ActiveWindowDrag> drag,
		Res<UiWindowModifiers> mods,
		Commands commands,
		Query<Data<UiAnchorMember, Node>> membersQ,
		Query<Data<UiAnchorMember>> memberOnlyQ)
	{
		var owner = drag.Value.Owner;
		if (owner == 0 || !membersQ.TryGet(owner, out var ownerRow)) return;
		var (_, ownerMember, _) = ownerRow;
		int group = ownerMember.Ref.GroupId;

		if (mods.Value.AnchorDetachHeld)
		{
			// The dragged window keeps moving solo (the drag already wrote its position).
			Detach(commands, owner, memberOnlyQ);
			return;
		}

		var d = drag.Value.FrameDelta;
		if (d == Vector2.Zero) return;
		foreach (var (e, m, n) in membersQ)
		{
			if (e.Ref == owner || m.Ref.GroupId != group) continue;
			float x = n.Ref.Left.Type == ValType.Px ? n.Ref.Left.Value : 0f;
			float y = n.Ref.Top.Type == ValType.Px ? n.Ref.Top.Value : 0f;
			n.Ref.PositionType = PositionType.Absolute;
			n.Ref.Left = Val.Px(MathF.Round(x + d.X));
			n.Ref.Top = Val.Px(MathF.Round(y + d.Y));
			membersQ.SetChanged<Node>(e.Ref);
		}
	}

	/// <summary>Detach <paramref name="owner"/> from its anchor group. A group left with a
	/// single member dissolves too.</summary>
	public static void Detach(Commands commands, ulong owner, Query<Data<UiAnchorMember>> membersQ)
	{
		if (!membersQ.TryGet(owner, out var ownerRow)) return;
		var (_, ownerMember) = ownerRow;
		int group = ownerMember.Ref.GroupId;

		commands.Entity(owner).Remove<UiAnchorMember>();

		ulong lone = 0; int remaining = 0;
		foreach (var (e, m) in membersQ)
		{
			if (e.Ref == owner || m.Ref.GroupId != group) continue;
			remaining++; lone = e.Ref;
		}
		if (remaining == 1) commands.Entity(lone).Remove<UiAnchorMember>();
	}

	// On release of an ungrouped anchorable window, snap it flush to the nearest
	// edge of the closest overlapping same-kind window and merge their groups.
	private static void SnapOnDrop(
		Res<ActiveWindowDrag> drag,
		Commands commands,
		ResMut<UiAnchorCounter> counter,
		Query<Data<UiAnchorable, Node, ComputedNode>> anchorablesQ,
		Query<Data<UiAnchorMember>> membersQ)
	{
		var owner = drag.Value.JustReleased;
		if (owner == 0) return;
		if (membersQ.Contains(owner)) return; // already in a group: stays put
		if (!anchorablesQ.TryGet(owner, out var ownerRow)) return;
		var (_, ownerKind, ownerNode, ownerBb) = ownerRow;

		if (!TryFindSnap(owner, ownerKind.Ref.Kind, ownerBb.Ref, anchorablesQ, out var pos, out var host))
			return;

		ownerNode.Ref.PositionType = PositionType.Absolute;
		ownerNode.Ref.Left = Val.Px(MathF.Round(pos.X));
		ownerNode.Ref.Top = Val.Px(MathF.Round(pos.Y));
		anchorablesQ.SetChanged<Node>(owner);

		// Merge: join the host's group, or mint a fresh one for the pair.
		int group;
		if (membersQ.TryGet(host, out var hostMember)) { var (_, hm) = hostMember; group = hm.Ref.GroupId; }
		else { group = counter.Value.Next++; commands.Entity(host).Insert(new UiAnchorMember { GroupId = group }); }
		commands.Entity(owner).Insert(new UiAnchorMember { GroupId = group });
	}

	// Live translucent ghost at the snap target while an ungrouped anchorable
	// window is dragged over a same-kind neighbour.
	private static void UpdateDropPreview(
		Res<ActiveWindowDrag> drag,
		ResMut<UiAnchorPreview> preview,
		Commands commands,
		Query<Data<UiAnchorable, Node, ComputedNode>> anchorablesQ,
		Query<Data<UiAnchorMember>> membersQ,
		Query<Data<Node>> nodeQ,
		ClayColor color,
		int z)
	{
		var owner = drag.Value.Owner;
		bool show = false;
		Vector2 pos = default;
		float sw = 0f, sh = 0f;
		if (owner != 0 && !membersQ.Contains(owner) && anchorablesQ.TryGet(owner, out var ownerRow))
		{
			var (_, ownerKind, _, ownerBb) = ownerRow;
			if (TryFindSnap(owner, ownerKind.Ref.Kind, ownerBb.Ref, anchorablesQ, out pos, out _))
			{
				show = true;
				sw = ownerBb.Ref.Size.X;
				sh = ownerBb.Ref.Size.Y;
			}
		}

		if (!show)
		{
			// No target this frame: drop the ghost once the drag ends, otherwise just
			// hide it (reused on the next overlapping frame).
			if (preview.Value.Entity != 0)
			{
				if (owner == 0) { commands.Entity(preview.Value.Entity).Despawn(); preview.Value.Entity = 0; }
				else UiNodeOps.SetDisplay(nodeQ, preview.Value.Entity, Display.None);
			}
			return;
		}

		if (preview.Value.Entity == 0 || !nodeQ.Contains(preview.Value.Entity))
		{
			// Spawn once; positioned next frame (Commands is deferred).
			var ghost = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
					Width = Val.Px(sw), Height = Val.Px(sh),
				})
				.Insert(new BackgroundColor(color))
				.Insert(new GlobalZIndex(z));
			preview.Value.Entity = ghost.Id;
			return;
		}

		if (nodeQ.TryGet(preview.Value.Entity, out var prow))
		{
			var (_, pn) = prow;
			float gx = MathF.Round(pos.X), gy = MathF.Round(pos.Y);
			// Compare-then-write: this runs every frame of the drag.
			if (pn.Ref.Display != Display.Flex
				|| pn.Ref.PositionType != PositionType.Absolute
				|| pn.Ref.Left != Val.Px(gx) || pn.Ref.Top != Val.Px(gy)
				|| pn.Ref.Width != Val.Px(sw) || pn.Ref.Height != Val.Px(sh))
			{
				pn.Ref.Display = Display.Flex;
				pn.Ref.PositionType = PositionType.Absolute;
				pn.Ref.Left = Val.Px(gx);
				pn.Ref.Top = Val.Px(gy);
				pn.Ref.Width = Val.Px(sw);
				pn.Ref.Height = Val.Px(sh);
				nodeQ.SetChanged<Node>(preview.Value.Entity);
			}
		}
	}

	// Nearest-edge snap target for `owner` against the closest overlapping
	// same-kind window. Shared by drop (commit) and preview (ghost).
	private static bool TryFindSnap(
		ulong owner, int kind, ComputedNode ob,
		Query<Data<UiAnchorable, Node, ComputedNode>> anchorablesQ,
		out Vector2 pos, out ulong host)
	{
		pos = default; host = 0;
		ulong best = 0; float bestDist = float.MaxValue;
		ComputedNode hbb = default;
		foreach (var (e, k, _, bb) in anchorablesQ)
		{
			if (e.Ref == owner || k.Ref.Kind != kind) continue;
			var h = bb.Ref;
			// AABB overlap: any touch counts.
			if (ob.Position.X > h.Position.X + h.Size.X || ob.Position.X + ob.Size.X < h.Position.X) continue;
			if (ob.Position.Y > h.Position.Y + h.Size.Y || ob.Position.Y + ob.Size.Y < h.Position.Y) continue;
			float dist = MathF.Abs(ob.Position.X - h.Position.X) + MathF.Abs(ob.Position.Y - h.Position.Y);
			if (dist < bestDist) { bestDist = dist; best = e.Ref; hbb = h; }
		}
		if (best == 0) return false;

		// Edge pick: scale each axis by the host size so a wide host prefers
		// vertical stacking and a tall one horizontal.
		float dx = ob.Position.X - hbb.Position.X;
		float dy = ob.Position.Y - hbb.Position.Y;
		float xs = MathF.Abs(dx) * 100f / MathF.Max(1f, hbb.Size.X);
		float ys = MathF.Abs(dy) * 100f / MathF.Max(1f, hbb.Size.Y);
		float nx, ny;
		if (xs > ys)
		{
			if (dx > 0) { nx = hbb.Position.X + hbb.Size.X; ny = hbb.Position.Y; }   // right
			else        { nx = hbb.Position.X - ob.Size.X;  ny = hbb.Position.Y; }   // left
		}
		else
		{
			if (dy > 0) { nx = hbb.Position.X;              ny = hbb.Position.Y + hbb.Size.Y; } // bottom
			else        { nx = hbb.Position.X;              ny = hbb.Position.Y - ob.Size.Y;  } // top
		}

		pos = new Vector2(nx, ny);
		host = best;
		return true;
	}
}
