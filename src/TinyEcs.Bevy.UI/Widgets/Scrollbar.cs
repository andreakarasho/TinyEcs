using System.Numerics;
using Clay;

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

/// Composite param bundling the queries the scrollbar plugin reads. Lets observer
/// callbacks stay short while keeping read access scheduler-visible.
public sealed class ScrollbarQueries : CompositeSystemParam
{
	public readonly Query<Data<Scrollbar>> Bars;
	public readonly Query<Data<ScrollbarThumb>> Thumbs;
	public readonly Query<Data<ScrollbarDragState>> DragStates;
	public readonly Query<Data<ComputedNode>> Computed;
	public readonly Query<Data<TinyEcs.Children>> Children;
	public readonly Query<Data<TinyEcs.Parent>> Parents;
	public readonly Query<Data<Node>> Nodes;

	public ScrollbarQueries()
	{
		Bars       = Add(new Query<Data<Scrollbar>>());
		Thumbs     = Add(new Query<Data<ScrollbarThumb>>());
		DragStates = Add(new Query<Data<ScrollbarDragState>>());
		Computed   = Add(new Query<Data<ComputedNode>>());
		Children   = Add(new Query<Data<TinyEcs.Children>>());
		Parents    = Add(new Query<Data<TinyEcs.Parent>>());
		Nodes      = Add(new Query<Data<Node>>());
	}
}

public sealed class ScrollbarPlugin : IPlugin
{
	public void Build(App app)
	{
		// 1) Position thumbs each frame from target's scroll data.
		app.AddSystem((Res<UiClayContext> ctx, ScrollbarQueries q) =>
			PositionThumbs(ctx.Value, q))
		.InStage(Stage.PreUpdate).SingleThreaded().Build();

		// 2) Pointer-down observer: start drag (thumb or rail).
		app.AddObserver<On<UiPointerDown>, ResMut<UiClayContext>, ScrollbarQueries>(
			(trigger, ctx, q) => OnPointerDown(ctx.Value, q, trigger.EntityId, trigger.Event.Position));

		// 3) Pointer-up observer: clear drag state.
		app.AddObserver<On<UiPointerUp>, ScrollbarQueries>(
			(trigger, q) => OnPointerUp(q, trigger.EntityId));

		// 4) Motion system: while pointer stays down, update target's scroll position.
		app.AddSystem((Res<UiPointer> pointer, ResMut<UiClayContext> ctx, ScrollbarQueries q) =>
			DragMotion(pointer.Value, ctx.Value, q))
		.InStage(Stage.Update).SingleThreaded().Build();
	}

	private static void PositionThumbs(UiClayContext ctx, ScrollbarQueries q)
	{
		foreach (var (barEid, bar) in q.Bars)
		{
			if (!q.Computed.Contains(barEid.Ref) || !q.Children.Contains(barEid.Ref))
				continue;

			var (_, barChildren) = q.Children.Get(barEid.Ref);
			ulong thumbEid = 0;
			foreach (var cid in barChildren.Ref)
			{
				if (q.Thumbs.Contains(cid)) { thumbEid = cid; break; }
			}
			if (thumbEid == 0 || !q.Nodes.Contains(thumbEid))
				continue;

			var data = ctx.GetScrollContainerData(UiClayId.Of(bar.Ref.Target).Id);
			if (!data.Found)
				continue;

			var (_, barComputed) = q.Computed.Get(barEid.Ref);
			var (_, thumbNode) = q.Nodes.Get(thumbEid);

			bool vertical = bar.Ref.Orientation == ScrollbarOrientation.Vertical;
			float trackLen = vertical ? barComputed.Ref.Size.Y : barComputed.Ref.Size.X;
			float visible  = vertical ? data.ScrollContainerDimensions.Height : data.ScrollContainerDimensions.Width;
			float content  = vertical ? data.ContentDimensions.Height : data.ContentDimensions.Width;
			float minThumb = bar.Ref.MinThumbLength > 0 ? bar.Ref.MinThumbLength : 16f;
			float thumbLen = content <= visible ? trackLen : MathF.Max(minThumb, trackLen * visible / content);

			float maxScroll = vertical ? data.MaxScrollY : data.MaxScrollX;
			float scrollPos = vertical ? data.ScrollPosition.Y : data.ScrollPosition.X;
			float thumbPos  = maxScroll > 0f ? scrollPos / maxScroll * (trackLen - thumbLen) : 0f;

			thumbNode.Ref.PositionType = PositionType.Absolute;
			if (vertical)
			{
				thumbNode.Ref.Top = Val.Px(thumbPos);
				thumbNode.Ref.Left = Val.Px(0);
				thumbNode.Ref.Width = Val.Px(barComputed.Ref.Size.X);
				thumbNode.Ref.Height = Val.Px(thumbLen);
			}
			else
			{
				thumbNode.Ref.Top = Val.Px(0);
				thumbNode.Ref.Left = Val.Px(thumbPos);
				thumbNode.Ref.Width = Val.Px(thumbLen);
				thumbNode.Ref.Height = Val.Px(barComputed.Ref.Size.Y);
			}
		}
	}

	private static void OnPointerDown(UiClayContext ctx, ScrollbarQueries q, ulong entityId, Vector2 pointer)
	{
		// Thumb pressed: start drag immediately.
		if (q.Thumbs.Contains(entityId) && q.DragStates.Contains(entityId))
		{
			if (!TryResolveBar(q, entityId, out var bar, out var trackLen, out var maxScroll, out var startScroll))
				return;
			var (_, state) = q.DragStates.Get(entityId);
			state.Ref.Dragging    = true;
			state.Ref.StartScroll = startScroll;
			state.Ref.StartPointer = bar.Orientation == ScrollbarOrientation.Vertical ? pointer.Y : pointer.X;
			state.Ref.TrackPixels = trackLen;
			state.Ref.MaxScroll   = maxScroll;
			return;
		}

		// Track pressed: jump to clicked position + latch thumb into drag mode.
		if (!q.Bars.Contains(entityId) || !q.Computed.Contains(entityId))
			return;
		var (_, barPtr) = q.Bars.Get(entityId);
		var (_, computedPtr) = q.Computed.Get(entityId);
		ref var barCmp = ref barPtr.Ref;
		ref var bc = ref computedPtr.Ref;
		bool verticalT = barCmp.Orientation == ScrollbarOrientation.Vertical;
		float trackOrigin = verticalT ? bc.Position.Y : bc.Position.X;
		float trackSize   = verticalT ? bc.Size.Y     : bc.Size.X;
		if (trackSize <= 0f)
			return;
		float ratio = Math.Clamp(((verticalT ? pointer.Y : pointer.X) - trackOrigin) / trackSize, 0f, 1f);
		var scrollData = ctx.GetScrollContainerData(UiClayId.Of(barCmp.Target).Id);
		if (!scrollData.Found)
			return;
		float maxS = verticalT ? scrollData.MaxScrollY : scrollData.MaxScrollX;
		float jumpedScroll = ratio * maxS;
		ctx.SetScrollPosition(barCmp.Target,
			verticalT ? new Vector2(0, jumpedScroll) : new Vector2(jumpedScroll, 0));

		if (!q.Children.Contains(entityId))
			return;
		float visiblePx = verticalT ? scrollData.ScrollContainerDimensions.Height : scrollData.ScrollContainerDimensions.Width;
		float contentPx = verticalT ? scrollData.ContentDimensions.Height       : scrollData.ContentDimensions.Width;
		float minThumb  = barCmp.MinThumbLength > 0 ? barCmp.MinThumbLength : 16f;
		float thumbLen  = contentPx <= visiblePx ? trackSize : MathF.Max(minThumb, trackSize * visiblePx / contentPx);
		float trackPx   = MathF.Max(0f, trackSize - thumbLen);

		var (_, kids) = q.Children.Get(entityId);
		foreach (var cid in kids.Ref)
		{
			if (!q.Thumbs.Contains(cid) || !q.DragStates.Contains(cid))
				continue;
			var (_, ds) = q.DragStates.Get(cid);
			ds.Ref.Dragging    = true;
			ds.Ref.StartScroll = jumpedScroll;
			ds.Ref.StartPointer = verticalT ? pointer.Y : pointer.X;
			ds.Ref.TrackPixels = trackPx;
			ds.Ref.MaxScroll   = maxS;
			return;
		}
	}

	private static void OnPointerUp(ScrollbarQueries q, ulong entityId)
	{
		// Release fires on press-origin. Clear drag on entity itself or its first thumb child.
		if (q.DragStates.Contains(entityId))
		{
			var (_, state) = q.DragStates.Get(entityId);
			state.Ref.Dragging = false;
			return;
		}
		if (!q.Children.Contains(entityId))
			return;
		var (_, kids) = q.Children.Get(entityId);
		foreach (var cid in kids.Ref)
		{
			if (!q.DragStates.Contains(cid))
				continue;
			var (_, state) = q.DragStates.Get(cid);
			state.Ref.Dragging = false;
		}
	}

	private static void DragMotion(in UiPointer pointer, UiClayContext ctx, ScrollbarQueries q)
	{
		if (!pointer.Down)
		{
			foreach (var (_, state) in q.DragStates)
				if (state.Ref.Dragging) state.Ref.Dragging = false;
			return;
		}

		foreach (var (eid, state) in q.DragStates)
		{
			if (!state.Ref.Dragging || !q.Thumbs.Contains(eid.Ref))
				continue;
			if (!TryResolveBar(q, eid.Ref, out var bar, out _, out _, out _))
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
		ScrollbarQueries q,
		ulong thumbEntity,
		out Scrollbar bar,
		out float trackLen,
		out float maxScroll,
		out float startScroll)
	{
		bar = default; trackLen = 0; maxScroll = 0; startScroll = 0;
		if (!q.Parents.Contains(thumbEntity))
			return false;
		var (_, parentPtr) = q.Parents.Get(thumbEntity);
		var parentId = parentPtr.Ref.Id;
		if (!q.Bars.Contains(parentId) || !q.Computed.Contains(parentId))
			return false;

		var (_, barPtr) = q.Bars.Get(parentId);
		var (_, cnPtr) = q.Computed.Get(parentId);
		bar = barPtr.Ref;
		bool vertical = bar.Orientation == ScrollbarOrientation.Vertical;
		trackLen = vertical ? cnPtr.Ref.Size.Y : cnPtr.Ref.Size.X;

		var clayId = UiClayId.Of(bar.Target);
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
