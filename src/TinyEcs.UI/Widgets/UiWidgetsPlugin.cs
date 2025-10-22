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
		// System 1: Handle button hover/press interactions
		app.AddSystem((
			EventReader<UiPointerEvent> events,
			Query<Data<ClayButtonStyle, ButtonState, UiNode>> buttons) =>
		{
			foreach (var evt in events.Read())
			{
				var id = evt.CurrentTarget;
				foreach (var (entityId, style, state, node) in buttons)
				{
					if (entityId.Ref != id) continue;

					ref var stateRef = ref state.Ref;
					ref var nodeRef = ref node.Ref;
					var styleRef = style.Ref;

					switch (evt.Type)
					{
						case UiPointerEventType.PointerEnter:
							stateRef.IsHovered = true;
							nodeRef.Declaration.backgroundColor = styleRef.HoverBackground;
							break;
						case UiPointerEventType.PointerExit:
							stateRef.IsHovered = false;
							nodeRef.Declaration.backgroundColor = stateRef.IsPressed
								? styleRef.PressedBackground
								: styleRef.Background;
							break;
						case UiPointerEventType.PointerDown:
							if (evt.IsPrimaryButton)
							{
								stateRef.IsPressed = true;
								nodeRef.Declaration.backgroundColor = styleRef.PressedBackground;
							}
							break;
						case UiPointerEventType.PointerUp:
							stateRef.IsPressed = false;
							nodeRef.Declaration.backgroundColor = stateRef.IsHovered
								? styleRef.HoverBackground
								: styleRef.Background;
							break;
					}
					break;
				}
			}
        })
        .InStage(Stage.Update)
        .Label("ui:widgets:buttons")
        .Before("ui:clay:layout")
        .Build();

		// System 2: Handle checkbox toggle interactions
		app.AddSystem((
			EventReader<UiPointerEvent> events,
			Query<Data<CheckboxLinks>> checkboxContainers,
			Query<Data<CheckboxState, UiNode, ClayCheckboxStyle>> checkboxBoxes,
			Commands commands) =>
		{
			foreach (var evt in events.Read())
			{
				if (evt.Type != UiPointerEventType.PointerDown || !evt.IsPrimaryButton)
					continue;

				var id = evt.CurrentTarget;

				// Find the checkbox container that was clicked
				foreach (var (containerEntityId, links) in checkboxContainers)
				{
					if (containerEntityId.Ref != id) continue;

					var boxEntity = links.Ref.BoxEntity;
					if (boxEntity == 0) continue;

					// Find the box entity and toggle it
					foreach (var (boxEntityId, checkboxState, boxNode, checkboxStyle) in checkboxBoxes)
					{
						if (boxEntityId.Ref != boxEntity) continue;

						ref var stateRef = ref checkboxState.Ref;
						ref var nodeRef = ref boxNode.Ref;
						var styleRef = checkboxStyle.Ref;

						// Toggle checked state
						stateRef.Checked = !stateRef.Checked;

						// Update background color
						nodeRef.Declaration.backgroundColor = stateRef.Checked
							? styleRef.CheckedColor
							: styleRef.BoxColor;

						// Update checkmark text
						if (stateRef.Checked)
						{
							commands.Entity(boxEntity).Insert(UiText.From("✓", new Clay_TextElementConfig
							{
								textColor = new Clay_Color(255, 255, 255, 255),
								fontSize = (ushort)(styleRef.BoxSize * 0.8f),
								textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
							}));
						}
						else
						{
							// Remove checkmark by inserting empty text
							commands.Entity(boxEntity).Insert(UiText.From("", new Clay_TextElementConfig()));
						}
						break;
					}
					break;
				}
			}
        })
        .InStage(Stage.Update)
        .Label("ui:widgets:checkboxes")
        .Build();

		// System 3: Handle floating window dragging
		app.AddSystem((
			EventReader<UiPointerEvent> events,
			Query<Data<FloatingWindowState, FloatingWindowLinks, UiNode>> windows,
			Query<Data<Parent>> hierarchy) =>
		{
			foreach (var evt in events.Read())
			{
				switch (evt.Type)
				{
					case UiPointerEventType.PointerDown:
						if (!evt.IsPrimaryButton) break;

						// Try to find a window to start dragging
						foreach (var (windowEntityId, winState, winLinks, winNode) in windows)
						{
							ref var win = ref winState.Ref;
							var links = winLinks.Ref;
							var windowId = windowEntityId.Ref;

							// Check if event is on this window or its descendants
							bool isRelevant = evt.CurrentTarget == windowId ||
							                  IsDescendantOf(evt.CurrentTarget, windowId, hierarchy);

							if (!isRelevant) continue;

							// Check if clicking on title bar or its descendants
							bool isOnTitleBar = evt.Target == links.TitleBarId ||
							                   IsDescendantOf(evt.Target, links.TitleBarId, hierarchy);

							if (isOnTitleBar && win.CanDrag)
							{
								win.IsDragging = true;
								win.DragOffset = evt.Position - win.Position;
								Console.WriteLine($"[Window] Started dragging window {windowId} at {win.Position}, pointer at {evt.Position}, offset {win.DragOffset}");
							}
						}
						break;

					case UiPointerEventType.PointerUp:
						// Stop all dragging windows
						foreach (var (windowEntityId, winState, winLinks, winNode) in windows)
						{
							ref var win = ref winState.Ref;
							if (win.IsDragging)
							{
								Console.WriteLine($"[Window] Stopped dragging window {windowEntityId.Ref}");
								win.IsDragging = false;
							}
						}
						break;

					case UiPointerEventType.PointerMove:
						// Update all dragging windows using absolute pointer position
						foreach (var (windowEntityId, winState, winLinks, winNode) in windows)
						{
							ref var win = ref winState.Ref;
							ref var node = ref winNode.Ref;

							if (win.IsDragging)
							{
								// Calculate absolute position: pointer position minus the drag offset
								win.Position = evt.Position - win.DragOffset;

								node.Declaration.floating.offset = new Clay_Vector2
								{
									x = win.Position.X,
									y = win.Position.Y
								};
								Console.WriteLine($"[Window] Moving window {windowEntityId.Ref} to {win.Position}, pointer at {evt.Position}");
							}
						}
						break;
				}
			}
        })
        .InStage(Stage.Update)
        .Label("ui:widgets:windows")
        .Before("ui:clay:layout")  // Run BEFORE layout so position updates are applied this frame
        .Build();

		// System 4: Handle slider dragging
		app.AddSystem((
			EventReader<UiPointerEvent> events,
			Query<Data<SliderState, SliderLinks, ClaySliderStyle>> sliders,
			Query<Data<UiNode>> nodes) =>
		{
			foreach (var evt in events.Read())
			{
				var id = evt.CurrentTarget;

				foreach (var (entityId, sliderState, sliderLinks, sliderStyle) in sliders)
				{
					if (entityId.Ref != id) continue;

					ref var st = ref sliderState.Ref;
					var links = sliderLinks.Ref;
					var style = sliderStyle.Ref;

					switch (evt.Type)
					{
						case UiPointerEventType.PointerDown:
							if (evt.IsPrimaryButton)
								st.IsDragging = true;
							break;

						case UiPointerEventType.PointerUp:
							st.IsDragging = false;
							break;

						case UiPointerEventType.PointerMove:
							if (!st.IsDragging) break;

							// Adjust normalized value by horizontal motion
							var normalized = st.NormalizedValue + (style.Width <= 0 ? 0 : (evt.MoveDelta.X / style.Width));
							normalized = Math.Clamp(normalized, 0f, 1f);
							st.SetNormalizedValue(normalized);

							// Update fill width
							if (links.FillEntity != 0)
							{
								foreach (var (fillEntityId, fillNode) in nodes)
								{
									if (fillEntityId.Ref != links.FillEntity) continue;

									ref var fillNodeRef = ref fillNode.Ref;
									var fillWidth = style.Width * normalized;
									fillNodeRef.Declaration.layout.sizing = new Clay_Sizing(
										Clay_SizingAxis.Fixed(fillWidth),
										Clay_SizingAxis.Fixed(style.TrackHeight));
									break;
								}
							}

							// Update handle position
							if (links.HandleEntity != 0)
							{
								foreach (var (handleEntityId, handleNode) in nodes)
								{
									if (handleEntityId.Ref != links.HandleEntity) continue;

									ref var handleNodeRef = ref handleNode.Ref;
									var handleX = (style.Width - style.HandleSize) * normalized;
									var yOffset = -(style.HandleSize - style.TrackHeight) / 2f;
									handleNodeRef.Declaration.floating.offset = new Clay_Vector2 { x = handleX, y = yOffset };
									break;
								}
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
		.Before("ui:clay:layout")
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
