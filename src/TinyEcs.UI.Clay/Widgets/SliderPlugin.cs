using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds slider widget systems to the application.
/// Handles label updates, fill width updates, and global mouse tracking for dragging.
/// </summary>
public struct SliderPlugin : IPlugin
{
	public void Build(App app)
	{
		// System to update slider label text
		app.AddSystem((Commands commands, Query<Data<SliderLabelUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				if (node.Text.HasValue)
				{
					var text = node.Text.Value;
					text.Text = update.Text;
					node.Text = text;

					// Re-insert to trigger change detection
					commands.Entity(entityId.Ref).Insert(node);
				}
			}
		})
		.InStage(Stage.First)
		.Label("slider:update-labels")
		.Build();

		// System to update slider fill width
		app.AddSystem((Commands commands, Query<Data<SliderFillUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				node.Layout.sizing.width = Clay_SizingAxis.Percent(update.NormalizedValue);

				// Re-insert to trigger change detection
				commands.Entity(entityId.Ref).Insert(node);
			}
		})
		.InStage(Stage.First)
		.Label("slider:update-fills")
		.Build();

		// Global slider drag system - handles mouse movement even outside slider bounds
		// Uses ClayPointerState for platform-agnostic input
		app.AddSystem((Commands commands, Res<ClayPointerState> pointer, Query<Data<SliderState>> stateQuery, Query<Data<ClayComputedLayout>> layoutQuery) =>
		{
			// Check if any slider is being dragged
			foreach (var (entityId, statePtr) in stateQuery)
			{
				var state = statePtr.Ref;

				if (!state.IsDragging)
					continue;

				// Check if mouse button is still down
				if (!pointer.Value.PrimaryDown)
				{
					// Mouse released - stop dragging
					state.IsDragging = false;
					commands.Entity(entityId.Ref).Insert(state);
					continue;
				}

				// Get global mouse position from pointer state
				var mouseX = pointer.Value.Position.X;
				var mouseY = pointer.Value.Position.Y;

				// Get layout information for the slider components
				if (!layoutQuery.Contains(state.TrackEntityId) || !layoutQuery.Contains(state.ThumbEntityId))
					continue;

				var (_, trackLayoutPtr) = layoutQuery.Get(state.TrackEntityId);
				var (_, thumbLayoutPtr) = layoutQuery.Get(state.ThumbEntityId);
				var trackLayout = trackLayoutPtr.Ref;
				var thumbLayout = thumbLayoutPtr.Ref;

				// Calculate slider value using simple Lua-style calculation
				var trackWidth = trackLayout.Width;
				var trackLocalX = mouseX - trackLayout.X;
				var normalized = System.Math.Clamp(trackLocalX / trackWidth, 0f, 1f);
				var newValue = state.Min + normalized * (state.Max - state.Min);

				// Apply step if specified
				if (state.Step > 0)
				{
					newValue = System.MathF.Round(newValue / state.Step) * state.Step;
				}

				// Update value if it changed
				var clampedValue = System.Math.Clamp(newValue, state.Min, state.Max);
				if (System.Math.Abs(clampedValue - state.Value) > 0.0001f)
				{
					state.Value = clampedValue;
					commands.Entity(entityId.Ref).Insert(state);

					// Update visuals
					var label = state.Label;
					var value = state.Value;
					var min = state.Min;
					var max = state.Max;

					commands.Entity(state.LabelEntityId).Insert(new SliderLabelUpdate { Text = $"{label}: {value:F2}" });
					float normalizedFill = (value - min) / (max - min);
					commands.Entity(state.FillEntityId).Insert(new SliderFillUpdate { NormalizedValue = normalizedFill });
				}
			}
		})
		.InStage(Stage.First)
		.Label("slider:drag")
		.Build();
	}
}
