using System;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Registers default system-driven interactions for UI widgets using proper system parameters.
/// Uses EventReader, Queries, and Commands instead of direct World access (reflection-free).
/// - Button: hover/press background updates
/// - Checkbox: toggle on click, visual update + checkmark
/// - FloatingWindow: drag by title bar
/// - Slider: drag to adjust value
/// </summary>
public sealed class UiWidgetsPlugin : IPlugin
{
	public void Build(App app)
	{
		// Maintain window stacking without z-index using an order resource
		var worldInit = app.GetWorld();
		if (!worldInit.HasResource<UiWindowOrder>())
			worldInit.AddResource(new UiWindowOrder());
		// (Removed) Button hover/press system — handled by entity observers

		// (Removed) Checkbox toggle system — handled by entity observers

		// (Removed) Floating window drag system — handled by entity observers; fallback remains below

		// System 4: Handle slider dragging
		app.AddSystem((
			EventReader<UiPointerEvent> events,
			Query<Data<SliderState, SliderLinks, ClaySliderStyle>> sliders,
			Query<Data<UiNode>> nodes,
			ResMut<ClayUiState> uiState) =>
		{
			foreach (var evt in events.Read())
			{
				var id = evt.CurrentTarget;

				foreach (var (entityId, sliderState, sliderLinks, sliderStyle) in sliders)
				{
					ref var st = ref sliderState.Ref;
					var links = sliderLinks.Ref;
					var style = sliderStyle.Ref;

					// Accept events coming from the slider container or any of its parts,
					// OR if this slider is currently being dragged (so it keeps updating outside its bounds)
					bool isThisSlider = entityId.Ref == id ||
										links.TrackEntity == id ||
										links.FillEntity == id ||
										links.HandleEntity == id ||
										links.HandleLayerEntity == id;
					bool acceptEvent = isThisSlider || st.IsDragging;
					if (!acceptEvent) continue;

						switch (evt.Type)
						{
						case UiPointerEventType.PointerDown:
							if (!isThisSlider) break; // only start drag when pressing this slider
							if (evt.IsPrimaryButton)
							{
								st.IsDragging = true;
							}
							break;

							case UiPointerEventType.PointerUp:
								st.IsDragging = false;
								break;

						case UiPointerEventType.PointerMove:
							if (!st.IsDragging) break;

							// Compute normalized value from absolute pointer X relative to container bounds
							var normalized = st.NormalizedValue;
								unsafe
								{
									var ctx = uiState.Value.Context;
									if (ctx is not null)
									{
										Clay.SetCurrentContext(ctx);
										var containerElemIdMove = ClayId.Global($"slider-container-{entityId.Ref}").ToElementId();
										var elemMove = Clay.GetElementData(containerElemIdMove);
										if (elemMove.found && elemMove.boundingBox.width > 0)
										{
											normalized = (evt.Position.X - elemMove.boundingBox.x) / Math.Max(1f, style.Width);
											normalized = Math.Clamp(normalized, 0f, 1f);
											st.SetNormalizedValue(normalized);
										}
									}
								}

							var changed = false;
                    // Update fill width
                    if (links.FillEntity != 0 && nodes.Contains(links.FillEntity))
                    {
                        var fillData = nodes.Get(links.FillEntity);
                        fillData.Deconstruct(out var fillNode);
                        ref var fillNodeRef = ref fillNode.Ref;
                        var fillWidth = style.Width * normalized;
                        fillNodeRef.Declaration.layout.sizing = new Clay_Sizing(
                            Clay_SizingAxis.Fixed(fillWidth),
                            Clay_SizingAxis.Fixed(style.TrackHeight));
                        changed = true;
                    }

							// Update handle position via handleLayer padding
                    if (links.HandleLayerEntity != 0 && nodes.Contains(links.HandleLayerEntity))
                    {
                        var layerData = nodes.Get(links.HandleLayerEntity);
                        layerData.Deconstruct(out var layerNode);
                        ref var layerNodeRef = ref layerNode.Ref;
                        var handleX = (style.Width - style.HandleSize) * normalized;
                        layerNodeRef.Declaration.layout.padding = new Clay_Padding
                        {
                            left = (ushort)handleX,
                            right = 0,
                            top = 0,
                            bottom = 0
                        };
                        changed = true;
                    }

							if (changed)
							{
								// Force a layout pass so the graphical position updates this frame
								uiState.Value.RequestLayoutPass();
							}
							break;
					}
					break;
				}
			}
		})
		.InStage(Stage.Update)
		.Label("ui:widgets:sliders")
		.Build();

		// System 5: Fallback for window dragging when pointer leaves Clay elements
		// This system ensures windows keep updating even when mouse is outside UI bounds
		app.AddSystem((
			Res<ClayPointerState> pointerState,
			Query<Data<FloatingWindowState, UiNode>> windows) =>
		{
			var pointerPos = pointerState.Value.Position;

			foreach (var (entityId, winState, winNode) in windows)
			{
				ref var win = ref winState.Ref;
				ref var node = ref winNode.Ref;

				if (win.IsDragging)
				{
					// Calculate absolute position using current pointer position
					win.Position = pointerPos - win.DragOffset;

					node.Declaration.floating.offset = new Clay_Vector2
					{
						x = win.Position.X,
						y = win.Position.Y
					};
				}
			}
		})
		.InStage(Stage.Update)
		.Label("ui:widgets:windows:fallback")
		// No explicit dependency to allow running without Clay
		.RunIfResourceExists<ClayPointerState>()
		.Build();

		// System 6: Fallback for slider dragging when pointer leaves slider/current target
		app.AddSystem((
			Res<ClayPointerState> pointerState,
			ResMut<ClayUiState> uiState,
			Query<Data<SliderState, SliderLinks, ClaySliderStyle>> sliders,
			Query<Data<UiNode>> nodes) =>
		{
			var pointerPos = pointerState.Value.Position;

			foreach (var (entityId, sliderState, sliderLinks, sliderStyle) in sliders)
			{
				ref var st = ref sliderState.Ref;
				if (!st.IsDragging) continue;

				var links = sliderLinks.Ref;
				var style = sliderStyle.Ref;

				// Compute normalized from absolute pointer position relative to container bounds
				float normalized = st.NormalizedValue;
				unsafe
				{
					var ctx = uiState.Value.Context;
					if (ctx is not null)
					{
						Clay.SetCurrentContext(ctx);
						var containerElemId = ClayId.Global($"slider-container-{entityId.Ref}").ToElementId();
						var elem = Clay.GetElementData(containerElemId);
						if (elem.found && elem.boundingBox.width > 0)
						{
							normalized = (pointerPos.X - elem.boundingBox.x) / Math.Max(1f, style.Width);
							normalized = Math.Clamp(normalized, 0f, 1f);
							st.SetNormalizedValue(normalized);
						}
					}
				}

        // Update visuals (fill width and handle position)
        var changed = false;
        if (links.FillEntity != 0 && nodes.Contains(links.FillEntity))
        {
            var fillData = nodes.Get(links.FillEntity);
            fillData.Deconstruct(out var fillNode);
            ref var fillNodeRef = ref fillNode.Ref;
            var fillWidth = style.Width * normalized;
            fillNodeRef.Declaration.layout.sizing = new Clay_Sizing(
                Clay_SizingAxis.Fixed(fillWidth),
                Clay_SizingAxis.Fixed(style.TrackHeight));
            changed = true;
        }

        if (links.HandleLayerEntity != 0 && nodes.Contains(links.HandleLayerEntity))
        {
            var layerData = nodes.Get(links.HandleLayerEntity);
            layerData.Deconstruct(out var layerNode);
            ref var layerNodeRef = ref layerNode.Ref;
            var handleX = (style.Width - style.HandleSize) * normalized;
            layerNodeRef.Declaration.layout.padding = new Clay_Padding
            {
                left = (ushort)handleX,
                right = 0,
                top = 0,
                bottom = 0
            };
            changed = true;
        }

				if (changed)
				{
					uiState.Value.RequestLayoutPass();
				}
			}
		})
		.InStage(Stage.Update)
		.Label("ui:widgets:sliders:fallback")
		.RunIfResourceExists<ClayPointerState>()
		.Build();
	}

	private static bool IsDescendantOf(ulong child, ulong ancestor, Query<Data<Parent>> hierarchy)
	{
		if (child == 0 || ancestor == 0) return false;
		var current = child;
		var safety = 0;

		while (current != 0 && safety++ < 256)
		{
			if (current == ancestor) return true;

			// Search for parent
			bool foundParent = false;
			foreach (var (entityId, parent) in hierarchy)
			{
				if (entityId.Ref == current)
				{
					current = parent.Ref.Id;
					foundParent = true;
					break;
				}
			}

			if (!foundParent) break;
			if (current == 0) break;
		}
		return false;
	}
}

public static class UiWidgetsAppExtensions
{
	public static App AddUiWidgets(this App app)
	{
		app.AddPlugin(new UiWidgetsPlugin());
		return app;
	}
}
// Window order resource moved to TinyEcs.UI namespace (UiWindowOrder.cs)

