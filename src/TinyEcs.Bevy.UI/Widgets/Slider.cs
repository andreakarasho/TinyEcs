using System.Numerics;

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

public sealed class SliderQueries : CompositeSystemParam
{
	public readonly Query<Data<Slider>> Sliders;
	public readonly Query<Data<SliderThumb>> Thumbs;
	public readonly Query<Data<SliderDragState>> DragStates;
	public readonly Query<Data<ComputedNode>> Computed;
	public readonly Query<Data<TinyEcs.Children>> Children;
	public readonly Query<Data<TinyEcs.Parent>> Parents;
	public readonly Query<Data<Node>> Nodes;

	public SliderQueries()
	{
		Sliders    = Add(new Query<Data<Slider>>());
		Thumbs     = Add(new Query<Data<SliderThumb>>());
		DragStates = Add(new Query<Data<SliderDragState>>());
		Computed   = Add(new Query<Data<ComputedNode>>());
		Children   = Add(new Query<Data<TinyEcs.Children>>());
		Parents    = Add(new Query<Data<TinyEcs.Parent>>());
		Nodes      = Add(new Query<Data<Node>>());
	}
}

public sealed class SliderPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddSystem((SliderQueries q) => PositionThumbs(q))
		.InStage(Stage.PreUpdate).SingleThreaded().Build();

		app.AddObserver<On<UiPointerDown>, Commands, SliderQueries>(
			(trigger, cmd, q) => OnPointerDown(cmd, q, trigger.EntityId, trigger.Event.Position));

		app.AddObserver<On<UiPointerUp>, SliderQueries>(
			(trigger, q) => OnPointerUp(q, trigger.EntityId));

		app.AddSystem((Commands cmd, Res<UiPointer> pointer, SliderQueries q) =>
			DragMotion(cmd, pointer.Value, q))
		.InStage(Stage.Update).SingleThreaded().Build();
	}

	private static void PositionThumbs(SliderQueries q)
	{
		foreach (var (eid, sliderPtr) in q.Sliders)
		{
			if (!q.Computed.Contains(eid.Ref) || !q.Children.Contains(eid.Ref))
				continue;

			var (_, kids) = q.Children.Get(eid.Ref);
			ulong thumbId = 0;
			foreach (var cid in kids.Ref)
			{
				if (q.Thumbs.Contains(cid)) { thumbId = cid; break; }
			}
			if (thumbId == 0 || !q.Nodes.Contains(thumbId))
				continue;

			var (_, barComputed) = q.Computed.Get(eid.Ref);
			var (_, thumbNode) = q.Nodes.Get(thumbId);
			ref var slider = ref sliderPtr.Ref;

			bool vertical = slider.Orientation == ScrollbarOrientation.Vertical;
			float trackLen = vertical ? barComputed.Ref.Size.Y : barComputed.Ref.Size.X;
			float thumbLen = slider.ThumbLength > 0 ? slider.ThumbLength : 16f;
			float range = MathF.Max(0.0001f, slider.Max - slider.Min);
			float t = Math.Clamp((slider.Value - slider.Min) / range, 0f, 1f);
			float pos = t * MathF.Max(0f, trackLen - thumbLen);

			thumbNode.Ref.PositionType = PositionType.Absolute;
			if (vertical)
			{
				thumbNode.Ref.Top = Val.Px(pos);
				thumbNode.Ref.Left = Val.Px(0);
				thumbNode.Ref.Width = Val.Px(barComputed.Ref.Size.X);
				thumbNode.Ref.Height = Val.Px(thumbLen);
			}
			else
			{
				thumbNode.Ref.Top = Val.Px(0);
				thumbNode.Ref.Left = Val.Px(pos);
				thumbNode.Ref.Width = Val.Px(thumbLen);
				thumbNode.Ref.Height = Val.Px(barComputed.Ref.Size.Y);
			}
		}
	}

	private static void OnPointerDown(Commands cmd, SliderQueries q, ulong entityId, Vector2 pointer)
	{
		if (q.Thumbs.Contains(entityId) && q.DragStates.Contains(entityId))
		{
			if (!TryResolveSlider(q, entityId, out _, out var slider, out var trackPixels))
				return;
			var (_, state) = q.DragStates.Get(entityId);
			state.Ref.Dragging = true;
			state.Ref.StartValue = slider.Value;
			state.Ref.StartPointer = slider.Orientation == ScrollbarOrientation.Vertical ? pointer.Y : pointer.X;
			state.Ref.TrackPixels = trackPixels;
			return;
		}

		if (!q.Sliders.Contains(entityId) || !q.Computed.Contains(entityId))
			return;
		var (_, sliderPtr) = q.Sliders.Get(entityId);
		var (_, computedPtr) = q.Computed.Get(entityId);
		ref var trackSlider = ref sliderPtr.Ref;
		ref var bc = ref computedPtr.Ref;
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
			cmd.Entity(entityId).EmitTrigger(new SliderChanged { Value = newValue }, propagate: true);
		}

		if (!q.Children.Contains(entityId))
			return;
		float thumbLen = trackSlider.ThumbLength > 0 ? trackSlider.ThumbLength : 16f;
		float trackPx  = MathF.Max(0f, size - thumbLen);
		var (_, kids) = q.Children.Get(entityId);
		foreach (var cid in kids.Ref)
		{
			if (!q.Thumbs.Contains(cid) || !q.DragStates.Contains(cid))
				continue;
			var (_, ds) = q.DragStates.Get(cid);
			ds.Ref.Dragging   = true;
			ds.Ref.StartValue = newValue;
			ds.Ref.StartPointer = verticalT ? pointer.Y : pointer.X;
			ds.Ref.TrackPixels = trackPx;
			return;
		}
	}

	private static void OnPointerUp(SliderQueries q, ulong entityId)
	{
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

	private static void DragMotion(Commands cmd, in UiPointer pointer, SliderQueries q)
	{
		if (!pointer.Down)
		{
			foreach (var (_, state) in q.DragStates)
				if (state.Ref.Dragging) state.Ref.Dragging = false;
			return;
		}

		foreach (var (thumbEid, state) in q.DragStates)
		{
			if (!state.Ref.Dragging || !q.Thumbs.Contains(thumbEid.Ref))
				continue;
			if (!TryResolveSlider(q, thumbEid.Ref, out var sliderEid, out var slider, out _))
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

			var (_, sliderPtr) = q.Sliders.Get(sliderEid);
			sliderPtr.Ref.Value = newValue;
			cmd.Entity(sliderEid).EmitTrigger(new SliderChanged { Value = newValue }, propagate: true);
		}
	}

	private static bool TryResolveSlider(
		SliderQueries q,
		ulong thumbEntity,
		out ulong sliderEntity,
		out Slider slider,
		out float trackPixels)
	{
		sliderEntity = 0; slider = default; trackPixels = 0;
		if (!q.Parents.Contains(thumbEntity))
			return false;
		var (_, parentPtr) = q.Parents.Get(thumbEntity);
		sliderEntity = parentPtr.Ref.Id;
		if (!q.Sliders.Contains(sliderEntity) || !q.Computed.Contains(sliderEntity))
			return false;
		var (_, sliderPtr) = q.Sliders.Get(sliderEntity);
		var (_, cnPtr) = q.Computed.Get(sliderEntity);
		slider = sliderPtr.Ref;
		bool vertical = slider.Orientation == ScrollbarOrientation.Vertical;
		float trackLen = vertical ? cnPtr.Ref.Size.Y : cnPtr.Ref.Size.X;
		float thumbLen = slider.ThumbLength > 0 ? slider.ThumbLength : 16f;
		trackPixels = MathF.Max(0f, trackLen - thumbLen);
		return true;
	}
}
