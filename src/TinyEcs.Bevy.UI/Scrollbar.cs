// Visible, draggable scrollbars for ANY Overflow.Scroll container.
//
// Why an overlay instead of track/thumb entities: a scroll container is often
// owned by someone else's tree (a reconciler, a window's rebuild cohort), so
// injecting two child entities into it invites clobbering and despawn races,
// and an Absolute thumb is an independent Clay float root (it captures the
// pointer for its whole box and escapes the owner's clip). The bar has no
// layout of its own — it is pure geometry derived from Clay's own scroll data,
// stamped into ScrollbarOverlays post-layout, painted by the host renderer when
// the container's ScissorEnd command comes up (so it sorts with its window
// instead of over every later one), and driven by a raw-mouse gesture that
// claims the shared UiGestureGate like the window gestures.
//
// The container reserves the bar's width as right padding (ReserveBarWidth), so
// flow children never run under the bar and the strip the bar paints on is
// empty — no element underneath to steal the press.
//
// Painting is the host's job (the library has no renderer): read
// ScrollbarOverlays.ByClayId on each ScissorEnd command. The Skinned style's art
// metrics (arrow heights, fixed thumb height) come from UiScrollbarSkin.

using System.Numerics;
using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI;

public enum UiScrollbarStyle : byte
{
	/// <summary>Art skin: arrow buttons + fixed-height thumb, metrics from <see cref="UiScrollbarSkin"/>.
	/// Falls back to the flat geometry when the skin supplies none.</summary>
	Skinned = 0,
	/// <summary>Flat track + scaled thumb.</summary>
	Flat = 1,
}

/// <summary>Put this on an Overflow.Scroll + ScrollPosition container to give it a
/// vertical bar on its right edge. Width 0 = <see cref="ScrollbarGeometry"/> default.</summary>
public struct UiScrollbar
{
	public float Width;
	public UiScrollbarStyle Style;
}

/// <summary>
/// Lane placement for a <see cref="UiScrollbar"/>. With it the lane sits OUTSIDE
/// the container — starting Gap px right of its right edge, no padding reserved —
/// and spans the box grown by ExtendTop / ExtendBottom. ThumbArt != 0 makes a
/// thumb-only bar: just that skin art rides the lane (a scroll flag), no track,
/// no arrows; its height comes from <see cref="UiScrollbarSkin.ThumbArtHeight"/>.
/// </summary>
public struct UiScrollbarLane
{
	public float Gap;
	public float ExtendTop, ExtendBottom;
	public ushort ThumbArt;
}

/// <summary>Skinned-style art metrics. Zeros = no arrows, scaled thumb.</summary>
public struct UiScrollbarArt
{
	public float ArrowUp, ArrowDown, Thumb;
}

/// <summary>
/// The host skin's metrics, read post-layout for every bar. Unset hooks mean the
/// flat geometry. Keeps the library asset-agnostic: the host knows how tall its
/// arrow / thumb art is, the bar only needs the numbers.
/// </summary>
public sealed class UiScrollbarSkin
{
	public Func<UiScrollbarArt>? Skinned;
	public Func<ushort, float>? ThumbArtHeight;
}

/// <summary>Per-frame bar geometry, in UI-layout space (same space as the render
/// commands and the pointer).</summary>
public struct ScrollbarBar
{
	public ulong Entity;
	public int WindowZ;
	public float TrackX, TrackY, TrackWidth, TrackHeight;
	public float ThumbTop, ThumbHeight;
	public float MaxScroll;
	public float PageHeight;
	public UiScrollbarStyle Style;
	/// <summary>The container's paint order: the bar draws right after its content.</summary>
	public int PaintOrder;
	public ushort ThumbArt;
	/// <summary>Arrows sit just outside the track: [TrackY - ArrowUp, TrackY) and
	/// [TrackY + TrackHeight, + ArrowDown). 0 = no arrows.</summary>
	public float ArrowUp, ArrowDown;
	/// <summary>-1 / +1 while that arrow is held (pressed art), else 0.</summary>
	public sbyte ArrowPressed;
	/// <summary>Resolved post-layout (the renderer has no mouse).</summary>
	public bool ThumbHovered;
}

/// <summary>Post-layout snapshot of every visible bar. Keyed by Clay element id for
/// the renderer (which only has the render command's Id), plus a flat list for the
/// gesture scan.</summary>
public sealed class ScrollbarOverlays
{
	public readonly Dictionary<uint, ScrollbarBar> ByClayId = new();
	public readonly List<ScrollbarBar> Bars = new();
	/// <summary>A bar owns the current left-button gesture (thumb drag, track page,
	/// arrow). While set the plugin holds <see cref="UiPointer.Down"/> low, so the
	/// Interaction pipeline never presses (and never clicks) whatever element lies
	/// under the bar's lane — a flow row wider than the reserved padding still
	/// reaches under it, and its UiClick would otherwise fire on release.</summary>
	public bool PressOwned;
	public ulong PressedArrowEntity;
	public sbyte PressedArrowDir;

	public void Clear()
	{
		ByClayId.Clear();
		Bars.Clear();
	}
}

public static class ScrollbarGeometry
{
	public const float DefaultWidth = 14f;
	public const float FlatWidth = 8f;
	public const float MinThumbHeight = 12f;
	public const float FlatMinThumbHeight = 28f;
	/// <summary>One arrow click scrolls this far.</summary>
	public const float ArrowStep = 40f;

	public static float ResolveWidth(float requested, UiScrollbarStyle style)
		=> requested > 0f ? requested : (style == UiScrollbarStyle.Flat ? FlatWidth : DefaultWidth);

	public static float MinThumb(UiScrollbarStyle style)
		=> style == UiScrollbarStyle.Flat ? FlatMinThumbHeight : MinThumbHeight;

	/// <summary>Thumb size/offset for a viewport of <paramref name="viewportHeight"/>
	/// showing <paramref name="contentHeight"/> of content, scrolled to
	/// <paramref name="offset"/>. False when the content fits (no bar).</summary>
	public static bool Compute(
		float trackHeight,
		float viewportHeight,
		float contentHeight,
		float offset,
		out float thumbTop,
		out float thumbHeight,
		float minThumbHeight = MinThumbHeight,
		float fixedThumbHeight = 0f)
	{
		thumbTop = 0f;
		thumbHeight = 0f;
		if (trackHeight <= 0f || viewportHeight <= 0f || contentHeight <= viewportHeight)
			return false;

		// Min first, then cap: a track shorter than the minimum would make
		// Math.Clamp's min exceed its max and throw. A fixed (art) thumb keeps its
		// height instead of scaling.
		thumbHeight = fixedThumbHeight > 0f
			? MathF.Min(trackHeight, fixedThumbHeight)
			: MathF.Min(trackHeight, MathF.Max(minThumbHeight, trackHeight * viewportHeight / contentHeight));
		var travel = trackHeight - thumbHeight;
		var maxScroll = contentHeight - viewportHeight;
		var ratio = maxScroll > 0f ? Math.Clamp(Math.Abs(offset) / maxScroll, 0f, 1f) : 0f;
		thumbTop = travel * ratio;
		return true;
	}
}

public sealed class UiScrollbarPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddResource(new ScrollbarOverlays());
		if (!app.HasResource<UiScrollbarSkin>()) app.AddResource(new UiScrollbarSkin());
		if (!app.HasResource<MouseInput>()) app.AddResource(new MouseInput());

		var reserveFn = ReserveBarWidth;
		app.AddSystem(reserveFn).InStage(Stage.PreUpdate).Build();

		// PreUpdate (before UiWindowPlugin's drag in Update) so a press on the bar
		// claims the UiGestureGate first and the window doesn't move instead.
		// Reads the previous frame's geometry — one frame of lag, invisible.
		var dragFn = DragBar;
		app.AddSystem(dragFn).InStage(Stage.PreUpdate).Build();

		// After Clay solved the layout, before the render stage reads it.
		var buildFn = BuildOverlays;
		app.AddSystem(buildFn).InStage(UiPlugin.UiPostLayoutStage).Build();
	}

	// Keep the bar's strip clear of flow children. Idempotent: only widens, and
	// only when the current padding is narrower, so the relayout gate settles
	// after one frame instead of thrashing.
	private static void ReserveBarWidth(Query<Data<Node, UiScrollbar>, Filter<Without<UiScrollbarLane>>> q)
	{
		foreach (var (ent, node, bar) in q)
		{
			var w = ScrollbarGeometry.ResolveWidth(bar.Ref.Width, bar.Ref.Style);
			ref var pad = ref node.Ref.Padding.Right;
			if (pad.Type != ValType.Px || pad.Value < w)
			{
				pad = Val.Px(w);
				q.SetChanged<Node>(ent.Ref);
			}
		}
	}

	private static void BuildOverlays(
		Res<UiClayContext> ctx,
		Res<UiPointer> pointer,
		ResMut<ScrollbarOverlays> overlays,
		Res<UiScrollbarSkin> skin,
		UiGesturePick pick,
		Query<Data<ScrollPosition, UiScrollbar, UiScrollbarLane>, Filter<Optional<UiScrollbarLane>>> barsQ,
		Query<Data<GlobalZIndex>> zQ)
	{
		var store = overlays.Value;
		store.Clear();

		foreach (var (ent, scroll, bar, lane) in barsQ)
		{
			var clayId = UiClayId.Of(ent.Ref).Id;
			var data = ctx.Value.GetScrollContainerData(clayId);
			if (!data.Found) continue;
			if (!ctx.Value.TryGetElementBoundingBox(ent.Ref, out var box)) continue;

			var outside = lane.IsValid();
			var gap = outside ? lane.Ref.Gap : 0f;
			var extendTop = outside ? lane.Ref.ExtendTop : 0f;
			var extendBottom = outside ? lane.Ref.ExtendBottom : 0f;
			var art = outside ? lane.Ref.ThumbArt : (ushort)0;

			var width = ScrollbarGeometry.ResolveWidth(bar.Ref.Width, bar.Ref.Style);
			UiScrollbarArt metrics = default;
			if (art != 0)
				metrics.Thumb = skin.Value.ThumbArtHeight?.Invoke(art) ?? 0f;
			else if (bar.Ref.Style == UiScrollbarStyle.Skinned && skin.Value.Skinned != null)
				metrics = skin.Value.Skinned();
			// The track (thumb travel, track clicks, drag) is the span BETWEEN the
			// arrows; the renderer draws the arrows just outside it.
			var laneY = box.Y - extendTop;
			var laneHeight = box.Height + extendTop + extendBottom;
			var trackY = laneY + metrics.ArrowUp;
			var trackHeight = laneHeight - metrics.ArrowUp - metrics.ArrowDown;
			if (!ScrollbarGeometry.Compute(
					trackHeight,
					data.ScrollContainerDimensions.Height,
					data.ContentDimensions.Height,
					scroll.Ref.OffsetY,
					out var thumbTop,
					out var thumbHeight,
					ScrollbarGeometry.MinThumb(bar.Ref.Style),
					metrics.Thumb))
			{
				// Content fits. A track bar hides; a thumb-only flag stays parked at
				// the top (nothing to drag: MaxScroll is 0).
				if (art == 0) continue;
				thumbTop = 0f;
				thumbHeight = MathF.Min(trackHeight, metrics.Thumb);
			}

			var trackX = outside ? box.X + box.Width + gap : box.X + box.Width - width;
			var paintOrder = int.MaxValue;
			if (pick.Rendered.TryGet(ent.Ref, out var cnRow))
			{
				var (_, cn, _, _, _, _) = cnRow;
				paintOrder = cn.Ref.PaintOrder;
			}
			var pos = pointer.Value.Position;
			var hovered = pos.X >= trackX && pos.X < trackX + width
					   && pos.Y >= trackY + thumbTop && pos.Y < trackY + thumbTop + thumbHeight
					   && OwnsPress(pick, pos, ent.Ref, paintOrder);

			var entry = new ScrollbarBar
			{
				Entity = ent.Ref,
				WindowZ = UiHierarchy.ResolveWindowZ(ent.Ref, pick.Parents, zQ),
				PaintOrder = paintOrder,
				TrackX = trackX,
				TrackY = trackY,
				TrackWidth = width,
				TrackHeight = trackHeight,
				ThumbTop = trackY + thumbTop,
				ThumbHeight = thumbHeight,
				MaxScroll = data.MaxScrollY,
				PageHeight = data.ScrollContainerDimensions.Height,
				Style = bar.Ref.Style,
				ThumbArt = art,
				ArrowUp = metrics.ArrowUp,
				ArrowDown = metrics.ArrowDown,
				ArrowPressed = store.PressedArrowEntity == ent.Ref ? store.PressedArrowDir : (sbyte)0,
				ThumbHovered = hovered,
			};
			store.ByClayId[clayId] = entry;
			store.Bars.Add(entry);
		}
	}

	private struct BarDragAnchor
	{
		public bool Active;
		public bool ClaimedGate;
		public ulong ScrollEntity;
		public float MouseY0;
		public float OffsetY0;
		public float Travel;
		public float MaxScroll;
	}

	private static void DragBar(
		Res<MouseInput> mouse,
		ResMut<UiGestureGate> gate,
		ResMut<ScrollbarOverlays> overlays,
		ResMut<UiPointer> pointer,
		Local<BarDragAnchor> anchor,
		UiGesturePick pick,
		Query<Data<ScrollPosition>> scrollQ)
	{
		var held = mouse.Value.IsPressed(MouseButton.Left)
				|| mouse.Value.IsPressedOnce(MouseButton.Left);
		if (!held)
		{
			if (anchor.Value.ClaimedGate)
				gate.Value.Release(UiGestureMode.Window);
			anchor.Value.Active = false;
			anchor.Value.ClaimedGate = false;
			overlays.Value.PressOwned = false;
			overlays.Value.PressedArrowEntity = 0;
			overlays.Value.PressedArrowDir = 0;
			return;
		}

		if (mouse.Value.IsPressedOnce(MouseButton.Left))
			Latch(mouse.Value.Position, gate.Value, overlays.Value, ref anchor.Value, pick, scrollQ);

		// The host fed the raw button into the pointer earlier this frame; hold it
		// low for the whole bar gesture (see ScrollbarOverlays.PressOwned).
		if (overlays.Value.PressOwned)
			pointer.Value.Down = false;

		if (anchor.Value.Active
			&& anchor.Value.Travel > 0f
			&& scrollQ.TryGet(anchor.Value.ScrollEntity, out var dragRow))
		{
			var (_, sp) = dragRow;
			var delta = mouse.Value.Position.Y - anchor.Value.MouseY0;
			var want = Math.Clamp(
				anchor.Value.OffsetY0 + delta * (anchor.Value.MaxScroll / anchor.Value.Travel),
				0f, anchor.Value.MaxScroll);
			if (sp.Ref.OffsetY != want)
			{
				sp.Ref.OffsetY = want;
				scrollQ.SetChanged<ScrollPosition>(anchor.Value.ScrollEntity);
			}
		}
	}

	private static void Latch(
		Vector2 pos,
		UiGestureGate gate,
		ScrollbarOverlays overlays,
		ref BarDragAnchor anchor,
		UiGesturePick pick,
		Query<Data<ScrollPosition>> scrollQ)
	{
		if (!gate.IsFree) return; // another gesture owns the press

		// Only a bar of the window actually on top at the press (a window with no
		// bar there still covers the ones behind); among those, the last bar at
		// the highest z is the one declared latest (drawn on top).
		var hit = -1;
		var hitZ = int.MinValue;
		var bars = overlays.Bars;
		for (var i = 0; i < bars.Count; i++)
		{
			var b = bars[i];
			if (pos.X < b.TrackX || pos.X >= b.TrackX + b.TrackWidth) continue;
			if (pos.Y < b.TrackY - b.ArrowUp || pos.Y >= b.TrackY + b.TrackHeight + b.ArrowDown) continue;
			if (b.WindowZ < hitZ) continue;
			if (!OwnsPress(pick, pos, b.Entity, b.PaintOrder)) continue;
			hit = i;
			hitZ = b.WindowZ;
		}
		if (hit < 0) return;

		var bar = bars[hit];
		if (!scrollQ.TryGet(bar.Entity, out var scrollRow)) return;
		var (_, sp) = scrollRow;
		gate.TryClaim(UiGestureMode.Window);
		overlays.PressOwned = true;

		if (pos.Y < bar.TrackY || pos.Y >= bar.TrackY + bar.TrackHeight)
		{
			// Arrow: one step per press.
			sbyte dir = pos.Y < bar.TrackY ? (sbyte)-1 : (sbyte)1;
			sp.Ref.OffsetY = Math.Clamp(sp.Ref.OffsetY + dir * ScrollbarGeometry.ArrowStep, 0f, bar.MaxScroll);
			scrollQ.SetChanged<ScrollPosition>(bar.Entity);
			overlays.PressedArrowEntity = bar.Entity;
			overlays.PressedArrowDir = dir;
			anchor = new BarDragAnchor { ClaimedGate = true };
			return;
		}

		if (pos.Y < bar.ThumbTop || pos.Y >= bar.ThumbTop + bar.ThumbHeight)
		{
			// Track click above/below the thumb pages by one viewport. No drag
			// latch — a page is a one-shot; the gate is released on button-up.
			var step = pos.Y < bar.ThumbTop ? -bar.PageHeight : bar.PageHeight;
			sp.Ref.OffsetY = Math.Clamp(sp.Ref.OffsetY + step, 0f, bar.MaxScroll);
			scrollQ.SetChanged<ScrollPosition>(bar.Entity);
			anchor = new BarDragAnchor { ClaimedGate = true };
			return;
		}

		anchor = new BarDragAnchor
		{
			Active = true,
			ClaimedGate = true,
			ScrollEntity = bar.Entity,
			MouseY0 = pos.Y,
			OffsetY0 = sp.Ref.OffsetY,
			Travel = Math.Max(0f, bar.TrackHeight - bar.ThumbHeight),
			MaxScroll = bar.MaxScroll,
		};
	}

	// The bar has no entity, so the pick can't return it. It paints right after
	// its container's content, so it ranks at the container's paint order: the
	// press is the bar's when the topmost hit is drawn below that (the window's
	// own bg, a window behind, a backdrop — the lane may sit over transparent art
	// or overhang the edge) or belongs to the bar's own window. A window drawn
	// over the lane takes it.
	private static bool OwnsPress(UiGesturePick pick, Vector2 pos, ulong container, int paintOrder)
	{
		var hit = pick.Topmost(pos);
		return !hit.Found || hit.PaintOrder < paintOrder || pick.IsTopmostWindow(pos, container);
	}
}
