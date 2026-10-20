using System.Numerics;

namespace TinyEcs.Bevy.UI.Widgets;

public enum PopoverSide : byte { Bottom, Top, Right, Left }

public enum PopoverAlign : byte { Start, Center, End }

/// <summary>
/// Places this (Absolute) node next to <see cref="Anchor"/>'s laid-out box: on
/// <see cref="Side"/>, <see cref="Gap"/> px away, aligned by <see cref="Align"/>.
/// When it would leave its bounds — the surface, or <see cref="Within"/>'s box — it
/// flips to the opposite side (if that fits better, unless <see cref="NoFlip"/>),
/// then is clamped inside them.
/// Placement uses last frame's boxes, so a freshly spawned popover lands from its
/// declared size and settles on the next frame once it has its own box. A press on
/// the anchor never dismisses a <see cref="UiPopup"/> popover (the anchor toggles it).
/// </summary>
public struct Popover
{
	public ulong Anchor;
	public PopoverSide Side;
	public PopoverAlign Align;
	public float Gap;
	public bool NoFlip;
	/// <summary>Keep inside this entity's laid-out box instead of the surface (0 = surface).</summary>
	public ulong Within;
}

public sealed class PopoverPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddSystem((Res<UiSurface> surface, Res<UiScale> scale,
				Query<Data<Popover, Node>> popovers,
				Query<Data<ComputedNode>> computed,
				Query<Data<TinyEcs.Parent>> parents) => Place(surface, scale, popovers, computed, parents))
			.InStage(UiPlugin.UiPreLayoutStage).SingleThreaded().Build();
	}

	private static void Place(
		Res<UiSurface> surface,
		Res<UiScale> scaleRes,
		Query<Data<Popover, Node>> popovers,
		Query<Data<ComputedNode>> computed,
		Query<Data<TinyEcs.Parent>> parents)
	{
		var scale = scaleRes.Value.Value > 0 ? scaleRes.Value.Value : 1f;
		var bounds = surface.Value.LogicalSize;
		foreach (var (ent, pop, node) in popovers)
		{
			if (!computed.TryGet(pop.Ref.Anchor, out var anchorRow)) continue;
			var (_, anchorBox) = anchorRow;
			var aPos = anchorBox.Ref.Position / scale;
			var aSize = anchorBox.Ref.Size / scale;

			Vector2 size;
			if (computed.TryGet(ent.Ref, out var ownRow))
			{
				var (_, own) = ownRow;
				size = own.Ref.Size / scale;
			}
			else
				size = new Vector2(Px(node.Ref.Width), Px(node.Ref.Height));

			var min = Vector2.Zero;
			var max = bounds;
			if (pop.Ref.Within != 0 && computed.TryGet(pop.Ref.Within, out var withinRow))
			{
				var (_, wb) = withinRow;
				min = wb.Ref.Position / scale;
				max = min + wb.Ref.Size / scale;
			}
			var p = Compute(pop.Ref, aPos, aSize, size, min, max);

			// Absolute offsets are relative to the parent's box.
			if (parents.TryGet(ent.Ref, out var parentRow))
			{
				var (_, parent) = parentRow;
				if (computed.TryGet(parent.Ref.Id, out var parentBox))
				{
					var (_, pb) = parentBox;
					p -= pb.Ref.Position / scale;
				}
			}

			var left = Val.Px(MathF.Round(p.X));
			var top = Val.Px(MathF.Round(p.Y));
			ref var n = ref node.Ref;
			if (n.PositionType == PositionType.Absolute && n.Left == left && n.Top == top) continue;
			n.PositionType = PositionType.Absolute;
			n.Left = left;
			n.Top = top;
			popovers.SetChanged<Node>(ent.Ref);
		}
	}

	private static float Px(Val v) => v.Type == ValType.Px ? v.Value : 0f;

	/// <summary>Surface position of a popover of <paramref name="size"/> around the anchor box,
	/// kept inside the surface (0,0)-<paramref name="surface"/>.</summary>
	public static Vector2 Compute(in Popover pop, Vector2 anchorPos, Vector2 anchorSize, Vector2 size, Vector2 surface)
		=> Compute(pop, anchorPos, anchorSize, size, Vector2.Zero, surface);

	/// <summary>As above, kept inside the rect <paramref name="min"/>-<paramref name="max"/>.</summary>
	public static Vector2 Compute(in Popover pop, Vector2 anchorPos, Vector2 anchorSize, Vector2 size, Vector2 min, Vector2 max)
	{
		var side = pop.Side;
		var p = OnSide(side, pop, anchorPos, anchorSize, size);
		if (!pop.NoFlip && Overflow(side, p, size, min, max) > 0)
		{
			var flipped = Opposite(side);
			var q = OnSide(flipped, pop, anchorPos, anchorSize, size);
			if (Overflow(flipped, q, size, min, max) < Overflow(side, p, size, min, max))
				p = q;
		}
		p.X = Math.Clamp(p.X, min.X, MathF.Max(min.X, max.X - size.X));
		p.Y = Math.Clamp(p.Y, min.Y, MathF.Max(min.Y, max.Y - size.Y));
		return p;
	}

	private static PopoverSide Opposite(PopoverSide s) => s switch
	{
		PopoverSide.Bottom => PopoverSide.Top,
		PopoverSide.Top => PopoverSide.Bottom,
		PopoverSide.Right => PopoverSide.Left,
		_ => PopoverSide.Right,
	};

	private static Vector2 OnSide(PopoverSide side, in Popover pop, Vector2 a, Vector2 aSize, Vector2 size)
	{
		bool vertical = side is PopoverSide.Bottom or PopoverSide.Top;
		float main = side switch
		{
			PopoverSide.Bottom => a.Y + aSize.Y + pop.Gap,
			PopoverSide.Top => a.Y - size.Y - pop.Gap,
			PopoverSide.Right => a.X + aSize.X + pop.Gap,
			_ => a.X - size.X - pop.Gap,
		};
		float crossStart = vertical ? a.X : a.Y;
		float crossAnchor = vertical ? aSize.X : aSize.Y;
		float crossSize = vertical ? size.X : size.Y;
		float cross = pop.Align switch
		{
			PopoverAlign.Center => crossStart + (crossAnchor - crossSize) * 0.5f,
			PopoverAlign.End => crossStart + crossAnchor - crossSize,
			_ => crossStart,
		};
		return vertical ? new Vector2(cross, main) : new Vector2(main, cross);
	}

	// How far the box leaves the bounds along the side's axis.
	private static float Overflow(PopoverSide side, Vector2 p, Vector2 size, Vector2 min, Vector2 max) => side switch
	{
		PopoverSide.Bottom => MathF.Max(0f, p.Y + size.Y - max.Y),
		PopoverSide.Top => MathF.Max(0f, min.Y - p.Y),
		PopoverSide.Right => MathF.Max(0f, p.X + size.X - max.X),
		_ => MathF.Max(0f, min.X - p.X),
	};
}
