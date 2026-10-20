using System.Numerics;
using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI.Widgets;

public enum ScrollbarOrientation : byte
{
	Horizontal = 0,
	Vertical = 1,
}

/// <summary>What a press on the track (not the thumb) does.</summary>
public enum SliderTrackClick : byte
{
	/// <summary>Jump so the thumb centres on the pressed point, then drag from there.</summary>
	Snap = 0,
	/// <summary>Drag from the current value, wherever the press lands.</summary>
	Drag,
	/// <summary>One <see cref="SliderStep"/> towards the pressed point, no drag.</summary>
	Step,
}

/// <summary>
/// Track entity of a slider; a <see cref="SliderThumb"/> child is positioned from
/// <see cref="SliderValue"/> within <see cref="SliderRange"/> (both inserted with
/// defaults 0 and 0..1 when missing). Dragging the thumb, pressing the track or —
/// while it holds focus — the arrows (one <see cref="SliderStep"/>) and Home / End
/// emit <see cref="ValueChange{T}"/> of float on the track: IsFinal=false during a
/// drag, true on its release and for discrete changes. Controlled: the app writes
/// <see cref="SliderValue"/> (or emits <see cref="SetSliderValue"/>); with
/// <see cref="SelfUpdate"/> the slider follows its own changes.
/// </summary>
public struct Slider
{
	/// <summary>Thumb extent along the track (px); 0 = 16.</summary>
	public float ThumbLength;
	public ScrollbarOrientation Orientation;
	public SliderTrackClick TrackClick;
}

public struct SliderValue
{
	public float Value;
	public SliderValue(float value) => Value = value;
}

public struct SliderRange
{
	public float Start, End;
	public SliderRange(float start, float end) { Start = start; End = end; }
	public readonly float Clamp(float v) => Math.Clamp(v, MathF.Min(Start, End), MathF.Max(Start, End));
	public readonly float Span => End - Start;
}

/// <summary>Keyboard / track-step increment; absent = 1.</summary>
public struct SliderStep
{
	public float Value;
	public SliderStep(float value) => Value = value;
}

/// <summary>The draggable child of a <see cref="Slider"/>. Positioned Absolute (with
/// <see cref="ClipToParent"/>, so a slider inside a scroll box clips with it).</summary>
public struct SliderThumb;

public enum SliderValueMode : byte { Absolute, Relative, Steps }

/// <summary>Entity command: set a slider's value (clamped to its range). No
/// <see cref="ValueChange{T}"/> is emitted.</summary>
public struct SetSliderValue
{
	public float Value;
	public SliderValueMode Mode;
	public SetSliderValue(float value, SliderValueMode mode = SliderValueMode.Absolute) { Value = value; Mode = mode; }
}

/// <summary>The slider being dragged (one pointer, one drag).</summary>
public sealed class SliderDrag
{
	public ulong Slider;
	internal float StartValue, StartPointer, TrackPixels, Last;
}

public sealed class SliderQueries : CompositeSystemParam
{
	public readonly Query<Data<Slider, SliderValue, SliderRange>> Sliders;
	public readonly Query<Data<Slider>> Tracks;
	public readonly Query<Data<SliderThumb>> Thumbs;
	public readonly Query<Data<SliderStep>> Steps;
	public readonly Query<Data<ComputedNode>> Computed;
	public readonly Query<Data<TinyEcs.Parent>> Parents;
	public readonly Query<Data<InteractionDisabled>> Disabled;

	public SliderQueries()
	{
		Sliders  = Add(new Query<Data<Slider, SliderValue, SliderRange>>());
		Tracks   = Add(new Query<Data<Slider>>());
		Thumbs   = Add(new Query<Data<SliderThumb>>());
		Steps    = Add(new Query<Data<SliderStep>>());
		Computed = Add(new Query<Data<ComputedNode>>());
		Parents  = Add(new Query<Data<TinyEcs.Parent>>());
		Disabled = Add(new Query<Data<InteractionDisabled>>());
	}

	public float StepOf(ulong slider)
	{
		if (!Steps.TryGet(slider, out var row)) return 1f;
		var (_, s) = row;
		return s.Ref.Value;
	}
}

public sealed class SliderPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddPlugin(new UiFocusPlugin());
		app.AddResource(new SliderDrag());

		app.AddObserver<OnAdd<Slider>, Commands, Query<Data<SliderValue>>, Query<Data<SliderRange>>, Query<Data<UiKeyCapture>>>(
			(t, commands, values, ranges, captures) =>
			{
				var e = commands.Entity(t.EntityId);
				if (!values.Contains(t.EntityId)) e.Insert(new SliderValue(0));
				if (!ranges.Contains(t.EntityId)) e.Insert(new SliderRange(0, 1));
				if (!captures.Contains(t.EntityId)) e.Insert(new UiKeyCapture(UiKeys.Arrows | UiKeys.HomeEnd));
			});
		app.AddObserver<OnAdd<SliderThumb>, Commands>((t, commands) => commands.Entity(t.EntityId).Insert(new ClipToParent()));

		// PostUpdate: after the frame's value writes, before the layout pass.
		app.AddSystem((Query<Data<Slider, SliderValue, SliderRange, ComputedNode, TinyEcs.Children>> sliders,
				Query<Data<SliderThumb>> thumbs, Query<Data<Node>> nodes) => PositionThumbs(sliders, thumbs, nodes))
			.InStage(Stage.PostUpdate).SingleThreaded().Build();

		app.AddObserver<On<UiPointerDown>, Commands, ResMut<SliderDrag>, SliderQueries>(
			(t, commands, drag, q) => OnPointerDown(commands, drag.Value, q, t.EntityId, t.Event.Position));

		app.AddSystem((Commands commands, Res<UiPointer> pointer, ResMut<SliderDrag> drag, SliderQueries q) =>
				DragMotion(commands, pointer.Value, drag.Value, q))
			.InStage(Stage.Update).SingleThreaded().Build();

		app.AddObserver<On<FocusedKey>, Commands, SliderQueries>((t, commands, q) =>
		{
			var e = t.EntityId;
			if (!q.Sliders.TryGet(e, out var row) || q.Disabled.Contains(e)) return;
			var (_, _, value, range) = row;
			var step = q.StepOf(e);
			var v = value.Ref.Value;
			float want;
			switch (t.Event.Key)
			{
				case KeyCode.Left: case KeyCode.Down: want = v - step; break;
				case KeyCode.Right: case KeyCode.Up: want = v + step; break;
				case KeyCode.Home: want = MathF.Min(range.Ref.Start, range.Ref.End); break;
				case KeyCode.End: want = MathF.Max(range.Ref.Start, range.Ref.End); break;
				default: return;
			}
			want = range.Ref.Clamp(want);
			if (want != v) Emit(commands, e, want, true);
		});

		app.AddObserver<On<ValueChange<float>>, Query<Data<SliderValue, SliderRange>, Filter<With<Slider>, With<SelfUpdate>>>>(
			(t, sliders) => Write(sliders, t.Event.Source, t.Event.Value));

		app.AddObserver<On<SetSliderValue>, Query<Data<SliderValue, SliderRange>, Filter<With<Slider>>>, Query<Data<SliderStep>>>(
			(t, sliders, steps) =>
			{
				if (!sliders.TryGet(t.EntityId, out var row)) return;
				var (_, value, _) = row;
				var step = 1f;
				if (steps.TryGet(t.EntityId, out var sr)) { var (_, s) = sr; step = s.Ref.Value; }
				var want = t.Event.Mode switch
				{
					SliderValueMode.Relative => value.Ref.Value + t.Event.Value,
					SliderValueMode.Steps => value.Ref.Value + t.Event.Value * step,
					_ => t.Event.Value,
				};
				Write(sliders, t.EntityId, want);
			});
	}

	private static void Write<TFilter>(Query<Data<SliderValue, SliderRange>, TFilter> sliders, ulong slider, float want)
		where TFilter : struct, IFilter<TFilter>, IQueryFilterAccess, allows ref struct
	{
		if (!sliders.TryGet(slider, out var row)) return;
		var (_, value, range) = row;
		want = range.Ref.Clamp(want);
		if (value.Ref.Value == want) return;
		value.Ref.Value = want;
		sliders.SetChanged<SliderValue>(slider);
	}

	private static void Emit(Commands commands, ulong slider, float value, bool final)
		=> commands.Entity(slider).EmitTrigger(new ValueChange<float> { Source = slider, Value = value, IsFinal = final }, propagate: true);

	private static float Along(Vector2 v, bool vertical) => vertical ? v.Y : v.X;

	private static void PositionThumbs(
		Query<Data<Slider, SliderValue, SliderRange, ComputedNode, TinyEcs.Children>> sliders,
		Query<Data<SliderThumb>> thumbs,
		Query<Data<Node>> nodes)
	{
		foreach (var (_, slider, value, range, computed, kids) in sliders)
		{
			ulong thumbId = 0;
			foreach (var cid in kids.Ref)
				if (thumbs.Contains(cid)) { thumbId = cid; break; }
			if (thumbId == 0 || !nodes.TryGet(thumbId, out var thumbRow))
				continue;

			bool vertical = slider.Ref.Orientation == ScrollbarOrientation.Vertical;
			var size = computed.Ref.Size;
			float thumbLen = ThumbLength(slider.Ref);
			var span = range.Ref.Span;
			float t = span == 0 ? 0f : Math.Clamp((value.Ref.Value - range.Ref.Start) / span, 0f, 1f);
			float pos = t * MathF.Max(0f, Along(size, vertical) - thumbLen);

			var top    = vertical ? Val.Px(pos) : Val.Px(0);
			var left   = vertical ? Val.Px(0) : Val.Px(pos);
			var width  = vertical ? Val.Px(size.X) : Val.Px(thumbLen);
			var height = vertical ? Val.Px(thumbLen) : Val.Px(size.Y);

			// Every frame: write (and mark for the relayout gate) only on a real move.
			var (_, thumbNode) = thumbRow;
			ref var tn = ref thumbNode.Ref;
			if (tn.PositionType == PositionType.Absolute
				&& tn.Top == top && tn.Left == left && tn.Width == width && tn.Height == height)
				continue;

			tn.PositionType = PositionType.Absolute;
			tn.Top = top;
			tn.Left = left;
			tn.Width = width;
			tn.Height = height;
			nodes.SetChanged<Node>(thumbId);
		}
	}

	private static float ThumbLength(in Slider s) => s.ThumbLength > 0 ? s.ThumbLength : 16f;

	private static void OnPointerDown(Commands commands, SliderDrag drag, SliderQueries q, ulong target, Vector2 pointer)
	{
		var thumb = WidgetTree.Nearest(target, q.Thumbs, q.Parents);
		var slider = WidgetTree.Nearest(thumb != 0 ? thumb : target, q.Tracks, q.Parents);
		if (slider == 0 || q.Disabled.Contains(slider) || !q.Sliders.Contains(slider) || !q.Computed.TryGet(slider, out var cRow))
			return;
		var (_, sl, value, range) = q.Sliders.Get(slider);
		var (_, computed) = cRow;
		bool vertical = sl.Ref.Orientation == ScrollbarOrientation.Vertical;
		float origin = Along(computed.Ref.Position, vertical);
		float size = Along(computed.Ref.Size, vertical);
		float at = Along(pointer, vertical);
		if (size <= 0f)
			return;

		var current = value.Ref.Value;
		var start = current;
		if (thumb == 0)
		{
			switch (sl.Ref.TrackClick)
			{
				case SliderTrackClick.Snap:
				{
					// The thumb centres under the pointer.
					var travel = size - ThumbLength(sl.Ref);
					var ratio = travel > 0f ? Math.Clamp((at - origin - ThumbLength(sl.Ref) * 0.5f) / travel, 0f, 1f) : 0f;
					start = range.Ref.Clamp(range.Ref.Start + ratio * range.Ref.Span);
					break;
				}
				case SliderTrackClick.Step:
				{
					var span = range.Ref.Span;
					float thumbPos = origin + (span == 0 ? 0 : (current - range.Ref.Start) / span) * MathF.Max(0f, size - ThumbLength(sl.Ref));
					var dir = at < thumbPos ? -1f : 1f;
					var stepped = range.Ref.Clamp(current + dir * q.StepOf(slider));
					if (stepped != current) Emit(commands, slider, stepped, true);
					return;
				}
			}
		}

		drag.Slider = slider;
		drag.StartValue = start;
		drag.Last = start;
		drag.StartPointer = at;
		drag.TrackPixels = MathF.Max(0f, size - ThumbLength(sl.Ref));
		if (start != current)
			Emit(commands, slider, start, false);
	}

	private static void DragMotion(Commands commands, in UiPointer pointer, SliderDrag drag, SliderQueries q)
	{
		if (drag.Slider == 0) return;
		var slider = drag.Slider;
		if (!q.Sliders.TryGet(slider, out var row))
		{
			drag.Slider = 0;
			return;
		}
		if (!pointer.Down)
		{
			drag.Slider = 0;
			Emit(commands, slider, drag.Last, true);
			return;
		}
		var (_, sl, _, range) = row;
		if (drag.TrackPixels <= 0f) return;
		bool vertical = sl.Ref.Orientation == ScrollbarOrientation.Vertical;
		float delta = Along(pointer.Position, vertical) - drag.StartPointer;
		float want = range.Ref.Clamp(drag.StartValue + delta / drag.TrackPixels * range.Ref.Span);
		if (want == drag.Last) return;
		drag.Last = want;
		Emit(commands, slider, want, false);
	}
}
