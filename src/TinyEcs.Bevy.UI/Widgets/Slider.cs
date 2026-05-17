using System.Numerics;
using TinyEcs.Bevy;

namespace TinyEcs.Bevy.UI.Widgets;

/// Track entity. Pair with a `SliderThumb` child + an `Interaction` on that
/// thumb. The `SliderPlugin` positions the thumb each frame from `Value` and
/// updates `Value` while the thumb (or rail) is being dragged.
public struct Slider
{
	public float Min;
	public float Max;
	public float Value;
	public float ThumbLength;
	public ScrollbarOrientation Orientation;
}

public struct SliderThumb
{
	public byte _padding;
}

public struct SliderDragState
{
	public bool Dragging;
	public float StartValue;
	public float StartPointer;
	public float TrackPixels;
}

public struct SliderChanged
{
	public float Value;
}

public sealed class SliderPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddSystem((WorldParam wp, Query<Data<Slider>> sliders) =>
		{
			PositionThumbs(wp.World, sliders);
		})
		.InStage(Stage.PreUpdate)
		.SingleThreaded()
		.Build();

		app.AddObserver<On<UiPointerDown>, WorldParam>(
			(trigger, wp) => OnPointerDown(wp.World, trigger.EntityId, trigger.Event.Position));

		app.AddObserver<On<UiPointerUp>, WorldParam>(
			(trigger, wp) => OnPointerUp(wp.World, trigger.EntityId));

		app.AddSystem((WorldParam wp, Res<UiPointer> pointer,
			Query<Data<SliderDragState>, Filter<With<SliderThumb>>> thumbs) =>
		{
			DragMotion(wp.World, pointer.Value, thumbs);
		})
		.InStage(Stage.Update)
		.SingleThreaded()
		.Build();
	}

	private static void PositionThumbs(World world, Query<Data<Slider>> sliders)
	{
		foreach (var (eid, sliderPtr) in sliders)
		{
			ref var slider = ref sliderPtr.Ref;
			var sliderView = world.Entity(eid.Ref);
			if (!sliderView.Has<ComputedNode>() || !sliderView.Has<TinyEcs.Children>())
				continue;

			ulong thumbId = 0;
			ref var children = ref sliderView.Get<TinyEcs.Children>();
			foreach (var cid in children)
			{
				var cv = world.Entity(cid);
				if (cv.Has<SliderThumb>()) { thumbId = cid; break; }
			}
			if (thumbId == 0)
				continue;

			var thumbView = world.Entity(thumbId);
			if (!thumbView.Has<Node>())
				continue;

			ref var barComputed = ref sliderView.Get<ComputedNode>();
			ref var thumbNode = ref thumbView.Get<Node>();

			bool vertical = slider.Orientation == ScrollbarOrientation.Vertical;
			float trackLen = vertical ? barComputed.Size.Y : barComputed.Size.X;
			float thumbLen = slider.ThumbLength > 0 ? slider.ThumbLength : 16f;
			float range = MathF.Max(0.0001f, slider.Max - slider.Min);
			float t = Math.Clamp((slider.Value - slider.Min) / range, 0f, 1f);
			float pos = t * MathF.Max(0f, trackLen - thumbLen);

			thumbNode.PositionType = PositionType.Absolute;
			if (vertical)
			{
				thumbNode.Top = Val.Px(pos);
				thumbNode.Left = Val.Px(0);
				thumbNode.Width = Val.Px(barComputed.Size.X);
				thumbNode.Height = Val.Px(thumbLen);
			}
			else
			{
				thumbNode.Top = Val.Px(0);
				thumbNode.Left = Val.Px(pos);
				thumbNode.Width = Val.Px(thumbLen);
				thumbNode.Height = Val.Px(barComputed.Size.Y);
			}
		}
	}

	private static void OnPointerDown(World world, ulong entityId, Vector2 pointer)
	{
		var view = world.Entity(entityId);

		// Thumb pressed: snap drag state from current value.
		if (view.Has<SliderThumb>() && view.Has<SliderDragState>())
		{
			if (!TryResolveSlider(world, entityId, out _, out var slider, out var trackPixels))
				return;
			ref var state = ref view.Get<SliderDragState>();
			state.Dragging = true;
			state.StartValue = slider.Value;
			state.StartPointer = slider.Orientation == ScrollbarOrientation.Vertical ? pointer.Y : pointer.X;
			state.TrackPixels = trackPixels;
			return;
		}

		// Track pressed: jump value + engage thumb drag.
		if (!view.Has<Slider>() || !view.Has<ComputedNode>())
			return;
		ref var trackSlider = ref view.Get<Slider>();
		ref var bc = ref view.Get<ComputedNode>();
		bool verticalT = trackSlider.Orientation == ScrollbarOrientation.Vertical;
		float origin = verticalT ? bc.Position.Y : bc.Position.X;
		float size   = verticalT ? bc.Size.Y     : bc.Size.X;
		if (size <= 0f)
			return;
		float ratio = Math.Clamp(((verticalT ? pointer.Y : pointer.X) - origin) / size, 0f, 1f);
		float range = trackSlider.Max - trackSlider.Min;
		float newValue = trackSlider.Min + ratio * range;
		if (newValue != trackSlider.Value)
		{
			trackSlider.Value = newValue;
			world.EmitTrigger(entityId, new SliderChanged { Value = newValue });
		}

		if (!view.Has<TinyEcs.Children>())
			return;
		float thumbLen = trackSlider.ThumbLength > 0 ? trackSlider.ThumbLength : 16f;
		float trackPx  = MathF.Max(0f, size - thumbLen);
		ref var children = ref view.Get<TinyEcs.Children>();
		foreach (var cid in children)
		{
			var cv = world.Entity(cid);
			if (!cv.Has<SliderThumb>() || !cv.Has<SliderDragState>())
				continue;
			ref var ds = ref cv.Get<SliderDragState>();
			ds.Dragging   = true;
			ds.StartValue = newValue;
			ds.StartPointer = verticalT ? pointer.Y : pointer.X;
			ds.TrackPixels = trackPx;
			return;
		}
	}

	private static void OnPointerUp(World world, ulong entityId)
	{
		var view = world.Entity(entityId);
		if (view.Has<SliderDragState>())
		{
			view.Get<SliderDragState>().Dragging = false;
			return;
		}
		if (!view.Has<TinyEcs.Children>())
			return;
		ref var children = ref view.Get<TinyEcs.Children>();
		foreach (var cid in children)
		{
			var cv = world.Entity(cid);
			if (cv.Has<SliderDragState>())
				cv.Get<SliderDragState>().Dragging = false;
		}
	}

	private static void DragMotion(
		World world,
		in UiPointer pointer,
		Query<Data<SliderDragState>, Filter<With<SliderThumb>>> thumbs)
	{
		if (!pointer.Down)
		{
			foreach (var (_, state) in thumbs)
				if (state.Ref.Dragging) state.Ref.Dragging = false;
			return;
		}

		foreach (var (thumbEid, state) in thumbs)
		{
			if (!state.Ref.Dragging)
				continue;
			if (!TryResolveSlider(world, thumbEid.Ref, out var sliderEid, out var slider, out _))
				continue;

			bool vertical = slider.Orientation == ScrollbarOrientation.Vertical;
			float now = vertical ? pointer.Position.Y : pointer.Position.X;
			float delta = now - state.Ref.StartPointer;
			if (state.Ref.TrackPixels <= 0f)
				continue;

			float range = slider.Max - slider.Min;
			float valueDelta = delta / state.Ref.TrackPixels * range;
			float newValue = Math.Clamp(state.Ref.StartValue + valueDelta, slider.Min, slider.Max);
			if (newValue == slider.Value)
				continue;

			var sliderView = world.Entity(sliderEid);
			ref var sliderRef = ref sliderView.Get<Slider>();
			sliderRef.Value = newValue;
			world.EmitTrigger(sliderEid, new SliderChanged { Value = newValue });
		}
	}

	private static bool TryResolveSlider(
		World world,
		ulong thumbEntity,
		out ulong sliderEntity,
		out Slider slider,
		out float trackPixels)
	{
		sliderEntity = 0; slider = default; trackPixels = 0;
		var thumbView = world.Entity(thumbEntity);
		if (!thumbView.Has<TinyEcs.Parent>())
			return false;
		sliderEntity = thumbView.Get<TinyEcs.Parent>().Id;
		var sliderView = world.Entity(sliderEntity);
		if (!sliderView.Has<Slider>() || !sliderView.Has<ComputedNode>())
			return false;
		slider = sliderView.Get<Slider>();
		ref var sliderComputed = ref sliderView.Get<ComputedNode>();
		bool vertical = slider.Orientation == ScrollbarOrientation.Vertical;
		float trackLen = vertical ? sliderComputed.Size.Y : sliderComputed.Size.X;
		float thumbLen = slider.ThumbLength > 0 ? slider.ThumbLength : 16f;
		trackPixels = MathF.Max(0f, trackLen - thumbLen);
		return true;
	}
}
