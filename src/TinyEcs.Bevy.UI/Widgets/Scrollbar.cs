using System.Numerics;
using Clay;
using TinyEcs.Bevy;

namespace TinyEcs.Bevy.UI.Widgets;

public enum ScrollbarOrientation : byte
{
	Horizontal = 0,
	Vertical = 1,
}

/// Tag the *track* entity. `Target` points at the scrollable container the bar
/// drives (an entity with `Overflow.Scroll` + `ScrollPosition`). The thumb is a
/// child entity carrying `ScrollbarThumb` + `ScrollbarDragState`.
public struct Scrollbar
{
	public ulong Target;
	public ScrollbarOrientation Orientation;
	public float MinThumbLength;
}

/// Marker for the thumb element. Non-empty so it can safely live on entities
/// (CLAUDE.md notes empty structs corrupt archetype storage when used in Data).
public struct ScrollbarThumb
{
	public byte _padding;
}

public struct ScrollbarDragState
{
	public bool Dragging;
	public float StartScroll;
	public float StartPointer;
	public float TrackPixels;
	public float MaxScroll;
}

public sealed class ScrollbarPlugin : IPlugin
{
	public void Build(App app)
	{
		// 1) Position thumbs each frame from target's scroll data.
		app.AddSystem((WorldParam wp, Res<UiClayContext> ctx, Query<Data<Scrollbar>> bars) =>
		{
			PositionThumbs(wp.World, ctx.Value, bars);
		})
		.InStage(Stage.PreUpdate)
		.SingleThreaded()
		.Build();

		// 2) Pointer-down observer: start drag (thumb or rail).
		app.AddObserver<On<UiPointerDown>, WorldParam, ResMut<UiClayContext>>(
			(trigger, wp, ctx) => OnPointerDown(wp.World, ctx.Value, trigger.EntityId, trigger.Event.Position));

		// 3) Pointer-up observer: clear drag state on the press-origin thumb (or any thumb).
		app.AddObserver<On<UiPointerUp>, WorldParam>(
			(trigger, wp) => OnPointerUp(wp.World, trigger.EntityId));

		// 4) Motion system: while pointer stays down, update target's scroll position.
		app.AddSystem((WorldParam wp, Res<UiPointer> pointer, ResMut<UiClayContext> ctx,
			Query<Data<ScrollbarDragState>, Filter<With<ScrollbarThumb>>> thumbs) =>
		{
			DragMotion(wp.World, pointer.Value, ctx.Value, thumbs);
		})
		.InStage(Stage.Update)
		.SingleThreaded()
		.Build();
	}

	private static void PositionThumbs(World world, UiClayContext ctx, Query<Data<Scrollbar>> bars)
	{
		foreach (var (barEid, bar) in bars)
		{
			var barView = world.Entity(barEid.Ref);
			if (!barView.Has<ComputedNode>() || !barView.Has<TinyEcs.Children>())
				continue;

			ulong thumbEid = 0;
			ref var children = ref barView.Get<TinyEcs.Children>();
			foreach (var cid in children)
			{
				var cv = world.Entity(cid);
				if (cv.Has<ScrollbarThumb>()) { thumbEid = cid; break; }
			}
			if (thumbEid == 0)
				continue;

			var data = ctx.GetScrollContainerData(ElementId.HashNumber((uint)bar.Ref.Target).Id);
			if (!data.Found)
				continue;

			ref var barComputed = ref barView.Get<ComputedNode>();
			var thumbView = world.Entity(thumbEid);
			if (!thumbView.Has<Node>())
				continue;
			ref var thumbNode = ref thumbView.Get<Node>();

			bool vertical = bar.Ref.Orientation == ScrollbarOrientation.Vertical;
			float trackLen = vertical ? barComputed.Size.Y : barComputed.Size.X;
			float visible  = vertical ? data.ScrollContainerDimensions.Height : data.ScrollContainerDimensions.Width;
			float content  = vertical ? data.ContentDimensions.Height : data.ContentDimensions.Width;
			float minThumb = bar.Ref.MinThumbLength > 0 ? bar.Ref.MinThumbLength : 16f;

			float thumbLen = content <= visible
				? trackLen
				: MathF.Max(minThumb, trackLen * visible / content);

			float maxScroll = vertical ? data.MaxScrollY : data.MaxScrollX;
			float scrollPos = vertical ? data.ScrollPosition.Y : data.ScrollPosition.X;
			float thumbPos  = maxScroll > 0f ? scrollPos / maxScroll * (trackLen - thumbLen) : 0f;

			thumbNode.PositionType = PositionType.Absolute;
			if (vertical)
			{
				thumbNode.Top = Val.Px(thumbPos);
				thumbNode.Left = Val.Px(0);
				thumbNode.Width = Val.Px(barComputed.Size.X);
				thumbNode.Height = Val.Px(thumbLen);
			}
			else
			{
				thumbNode.Top = Val.Px(0);
				thumbNode.Left = Val.Px(thumbPos);
				thumbNode.Width = Val.Px(thumbLen);
				thumbNode.Height = Val.Px(barComputed.Size.Y);
			}
		}
	}

	private static void OnPointerDown(World world, UiClayContext ctx, ulong entityId, Vector2 pointer)
	{
		var view = world.Entity(entityId);

		// Thumb pressed: start drag immediately.
		if (view.Has<ScrollbarThumb>() && view.Has<ScrollbarDragState>())
		{
			if (!TryResolveBar(world, entityId, out var bar, out var trackLen, out var maxScroll, out var startScroll))
				return;
			ref var state = ref view.Get<ScrollbarDragState>();
			state.Dragging    = true;
			state.StartScroll  = startScroll;
			state.StartPointer = bar.Orientation == ScrollbarOrientation.Vertical ? pointer.Y : pointer.X;
			state.TrackPixels  = trackLen;
			state.MaxScroll    = maxScroll;
			return;
		}

		// Track pressed: jump to clicked position AND latch the thumb into drag mode.
		if (!view.Has<Scrollbar>() || !view.Has<ComputedNode>())
			return;
		var barCmp = view.Get<Scrollbar>();
		ref var bc = ref view.Get<ComputedNode>();
		bool verticalT = barCmp.Orientation == ScrollbarOrientation.Vertical;
		float trackOrigin = verticalT ? bc.Position.Y : bc.Position.X;
		float trackSize   = verticalT ? bc.Size.Y     : bc.Size.X;
		if (trackSize <= 0f)
			return;
		float ratio = Math.Clamp(((verticalT ? pointer.Y : pointer.X) - trackOrigin) / trackSize, 0f, 1f);
		var scrollData = ctx.GetScrollContainerData(ElementId.HashNumber((uint)barCmp.Target).Id);
		if (!scrollData.Found)
			return;
		float maxS = verticalT ? scrollData.MaxScrollY : scrollData.MaxScrollX;
		float jumpedScroll = ratio * maxS;
		ctx.SetScrollPosition(barCmp.Target,
			verticalT ? new Vector2(0, jumpedScroll) : new Vector2(jumpedScroll, 0));

		if (!view.Has<TinyEcs.Children>())
			return;
		float visiblePx = verticalT ? scrollData.ScrollContainerDimensions.Height : scrollData.ScrollContainerDimensions.Width;
		float contentPx = verticalT ? scrollData.ContentDimensions.Height       : scrollData.ContentDimensions.Width;
		float minThumb  = barCmp.MinThumbLength > 0 ? barCmp.MinThumbLength : 16f;
		float thumbLen  = contentPx <= visiblePx ? trackSize : MathF.Max(minThumb, trackSize * visiblePx / contentPx);
		float trackPx   = MathF.Max(0f, trackSize - thumbLen);
		ref var children = ref view.Get<TinyEcs.Children>();
		foreach (var cid in children)
		{
			var cv = world.Entity(cid);
			if (!cv.Has<ScrollbarThumb>() || !cv.Has<ScrollbarDragState>())
				continue;
			ref var ds = ref cv.Get<ScrollbarDragState>();
			ds.Dragging    = true;
			ds.StartScroll = jumpedScroll;
			ds.StartPointer = verticalT ? pointer.Y : pointer.X;
			ds.TrackPixels = trackPx;
			ds.MaxScroll   = maxS;
			return;
		}
	}

	private static void OnPointerUp(World world, ulong entityId)
	{
		// The release fires on the press-origin entity. Walk down to its thumb (or
		// the entity itself if it IS a thumb) and clear the drag latch.
		var view = world.Entity(entityId);
		if (view.Has<ScrollbarDragState>())
		{
			view.Get<ScrollbarDragState>().Dragging = false;
			return;
		}
		if (!view.Has<TinyEcs.Children>())
			return;
		ref var children = ref view.Get<TinyEcs.Children>();
		foreach (var cid in children)
		{
			var cv = world.Entity(cid);
			if (cv.Has<ScrollbarDragState>())
				cv.Get<ScrollbarDragState>().Dragging = false;
		}
	}

	private static void DragMotion(
		World world,
		in UiPointer pointer,
		UiClayContext ctx,
		Query<Data<ScrollbarDragState>, Filter<With<ScrollbarThumb>>> thumbs)
	{
		if (!pointer.Down)
		{
			// Safety net: pointer lost while drag was active (e.g. moved off-window).
			foreach (var (_, state) in thumbs)
				if (state.Ref.Dragging) state.Ref.Dragging = false;
			return;
		}

		foreach (var (eid, state) in thumbs)
		{
			if (!state.Ref.Dragging)
				continue;
			if (!TryResolveBar(world, eid.Ref, out var bar, out _, out _, out _))
				continue;

			bool vertical = bar.Orientation == ScrollbarOrientation.Vertical;
			float now = vertical ? pointer.Position.Y : pointer.Position.X;
			float delta = now - state.Ref.StartPointer;
			if (state.Ref.TrackPixels <= 0f)
				continue;

			float scrollDelta = delta / state.Ref.TrackPixels * state.Ref.MaxScroll;
			float newScroll = Math.Clamp(state.Ref.StartScroll + scrollDelta, 0f, state.Ref.MaxScroll);
			ctx.SetScrollPosition(bar.Target,
				vertical ? new Vector2(0, newScroll) : new Vector2(newScroll, 0));
		}
	}

	private static bool TryResolveBar(
		World world,
		ulong thumbEntity,
		out Scrollbar bar,
		out float trackLen,
		out float maxScroll,
		out float startScroll)
	{
		bar = default; trackLen = 0; maxScroll = 0; startScroll = 0;
		var thumbView = world.Entity(thumbEntity);
		if (!thumbView.Has<TinyEcs.Parent>())
			return false;
		var parentId = thumbView.Get<TinyEcs.Parent>().Id;
		var barView = world.Entity(parentId);
		if (!barView.Has<Scrollbar>() || !barView.Has<ComputedNode>())
			return false;
		bar = barView.Get<Scrollbar>();
		ref var barComputed = ref barView.Get<ComputedNode>();
		bool vertical = bar.Orientation == ScrollbarOrientation.Vertical;
		trackLen = vertical ? barComputed.Size.Y : barComputed.Size.X;

		var clayId = ElementId.HashNumber((uint)bar.Target);
		var sd = global::Clay.Clay.Context!.GetScrollContainerData(clayId);
		if (!sd.Found)
			return false;
		maxScroll = vertical ? sd.MaxScrollY : sd.MaxScrollX;
		startScroll = vertical ? sd.ScrollPosition.Y : sd.ScrollPosition.X;
		float visible = vertical ? sd.ScrollContainerDimensions.Height : sd.ScrollContainerDimensions.Width;
		float content = vertical ? sd.ContentDimensions.Height : sd.ContentDimensions.Width;
		float minThumb = bar.MinThumbLength > 0 ? bar.MinThumbLength : 16f;
		float thumbLen = content <= visible ? trackLen : MathF.Max(minThumb, trackLen * visible / content);
		trackLen = MathF.Max(0f, trackLen - thumbLen);
		return true;
	}
}
