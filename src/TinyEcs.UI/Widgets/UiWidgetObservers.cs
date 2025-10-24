using System;
using Clay_cs;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Observers for reactive widget behavior based on Interaction state changes.
/// Similar to Bevy's observer-driven UI pattern where visual updates happen
/// automatically when component state changes.
/// </summary>
public static class UiWidgetObservers
{
	/// <summary>
	/// Marker component for button widgets.
	/// </summary>
	public struct Button { }

	/// <summary>
	/// Marker component for checkbox widgets.
	/// </summary>
	public struct Checkbox { }

	/// <summary>
	/// Observes button interaction changes and updates background color reactively.
	/// This replaces the manual event handling in the old ButtonWidget implementation.
	///
	/// Triggered automatically when Interaction component changes on any button entity.
	/// </summary>
	public static void OnButtonInteractionChanged(
		Query<Data<Interaction, ClayButtonStyle, UiNode>, Filter<Changed<Interaction>, With<Button>>> changedButtons)
	{
		foreach (var (interaction, style, node) in changedButtons)
		{
			ref var nodeRef = ref node.Ref;
			var styleRef = style.Ref;
			var interactionVal = interaction.Ref;

			var newColor = interactionVal switch
			{
				Interaction.Pressed => styleRef.PressedBackground,
				Interaction.Hovered => styleRef.HoverBackground,
				_ => styleRef.Background
			};

			nodeRef.Declaration.backgroundColor = newColor;

			// Note: Color changes don't affect layout geometry, only visual appearance.
			// We don't request layout here to avoid unnecessary rebuilds that could reset scroll state.
			// The changed color will be picked up on the next layout pass (triggered by actual UI changes).
		}
	}

	/// <summary>
	/// Observes button clicks and emits high-level OnClick events.
	/// This enables user code to react to button clicks via observers:
	///
	/// app.AddObserver&lt;OnClick&lt;Button&gt;&gt;((trigger) => Console.WriteLine("Clicked!"));
	/// </summary>
	public static void OnButtonClicked(
		EventReader<UiPointerEvent> events,
		Query<Data<Button>> buttons,
		Commands commands,
		Local<ulong> pressedButton)
	{
		foreach (var evt in events.Read())
		{
			if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
			{
				if (buttons.Contains(evt.Target))
				{
					pressedButton.Value = evt.Target;
				}
			}
			else if (evt.Type == UiPointerEventType.PointerUp && evt.IsPrimaryButton)
			{
				// Click = press and release on same button
				if (pressedButton.Value == evt.Target && buttons.Contains(evt.Target))
				{
					commands.EmitTrigger(new OnClick<Button>(evt.Target));
				}
				pressedButton.Value = 0;
			}
		}
	}

	/// <summary>
	/// Observes checkbox interaction and updates visuals when state changes.
	/// This is a hybrid approach - visual updates are reactive, but toggle logic
	/// still requires explicit event handling (see OnCheckboxClicked).
	/// </summary>
	public static void OnCheckboxStateChanged(
		Query<Data<CheckboxState, CheckboxLinks, ClayCheckboxStyle>, Filter<Changed<CheckboxState>, With<Checkbox>>> changedCheckboxes,
		Query<Data<UiNode>> nodes,
		Commands commands,
		ResMut<ClayUiState> uiState,
		Local<Dictionary<ulong, bool>> lastStates)
	{
		if (lastStates.Value == null)
		{
			lastStates.Value = new Dictionary<ulong, bool>();
		}

		var actualChanges = 0; var checkboxIndex = 0;
		foreach (var (state, links, style) in changedCheckboxes)
		{
			var stateRef = state.Ref;
			var linksRef = links.Ref;
			var styleRef = style.Ref;

			// Use the checkbox container entity ID (we need to get it from somewhere)
			// For now, use the BoxEntity as a proxy to track state
			var trackingId = linksRef.BoxEntity;

			// Check if this is an ACTUAL change
			bool lastState;
			bool isActualChange = !lastStates.Value.TryGetValue(trackingId, out lastState) || lastState != stateRef.Checked;

			if (!isActualChange)
			{
				// False positive from Changed<> filter
				checkboxIndex++;
				continue;
			}

			actualChanges++;
			lastStates.Value[trackingId] = stateRef.Checked;

			// Update box background color
			if (linksRef.BoxEntity != 0 && nodes.Contains(linksRef.BoxEntity))
			{
				var boxData = nodes.Get(linksRef.BoxEntity);
				boxData.Deconstruct(out var boxNode);
				ref var boxNodeRef = ref boxNode.Ref;

				var newColor = stateRef.Checked ? styleRef.CheckedColor : styleRef.BoxColor;
				boxNodeRef.Declaration.backgroundColor = newColor;
			}

			// Update checkmark text
			if (linksRef.BoxEntity != 0)
			{
				var checkmarkText = stateRef.Checked ? "✓" : "";
				commands.Entity(linksRef.BoxEntity).Insert(UiText.From(checkmarkText, new Clay_TextElementConfig
				{
					textColor = new Clay_Color(255, 255, 255, 255),
					fontSize = (ushort)(styleRef.BoxSize * 0.8f),
					textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
				}));
			}

			checkboxIndex++;
		}
	}   /// <summary>
		/// Handles checkbox toggle on click.
		/// Emits OnToggle event for user observers to react to.
		/// </summary>
	public static void OnCheckboxClicked(
		EventReader<UiPointerEvent> events,
		Query<Data<CheckboxState, CheckboxLinks, Checkbox>> checkboxes,
		Commands commands,
		Local<ulong> pressedCheckbox)
	{
		var toggleHappened = false;

		foreach (var evt in events.Read())
		{
			// Track which checkbox container was pressed
			if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
			{
				// Check if clicking on a checkbox container (use CurrentTarget for event bubbling)
				foreach (var (entityId, state, links, _) in checkboxes)
				{
					var id = entityId.Ref;

					// Use CurrentTarget which is the interactive element in the propagation chain
					if (id == evt.CurrentTarget)
					{
						pressedCheckbox.Value = id;
						break;
					}
				}
			}
			else if (evt.Type == UiPointerEventType.PointerUp && evt.IsPrimaryButton)
			{
				// Check if released on same checkbox
				foreach (var (entityId, statePtr, linksPtr, _) in checkboxes)
				{
					var id = entityId.Ref;

					if (id != pressedCheckbox.Value)
					{
						continue;
					}

					// Use CurrentTarget which is the interactive element in the propagation chain
					if (id == evt.CurrentTarget)
					{
						// Toggle the state
						var currentState = statePtr.Ref;
						var newState = new CheckboxState { Checked = !currentState.Checked };

						// Use Commands to trigger change detection
						commands.Entity(id).Insert(newState);

						// Emit event for observers
						commands.EmitTrigger(new OnToggle(id, newState.Checked));

						toggleHappened = true;
					}
				}
			}
		}   // Reset pressed checkbox after processing all events in this frame
		if (toggleHappened)
		{
			pressedCheckbox.Value = 0;
		}
	}   /// <summary>
		/// Observes slider state changes and updates visual position.
		/// Triggered when SliderState.NormalizedValue changes.
		/// </summary>
	public static void OnSliderValueChanged(
		Query<Data<SliderState, SliderLinks, ClaySliderStyle>, Filter<Changed<SliderState>>> changedSliders,
		Query<Data<UiNode>> nodes,
		Commands commands)
	{
		foreach (var (state, links, style) in changedSliders)
		{
			var stateRef = state.Ref;
			var linksRef = links.Ref;
			var styleRef = style.Ref;
			var normalized = stateRef.NormalizedValue;

			// Update fill width
			if (linksRef.FillEntity != 0 && nodes.Contains(linksRef.FillEntity))
			{
				var fillData = nodes.Get(linksRef.FillEntity);
				fillData.Deconstruct(out var fillNode);
				ref var fillNodeRef = ref fillNode.Ref;
				var fillWidth = styleRef.Width * normalized;
				fillNodeRef.Declaration.layout.sizing = new Clay_Sizing(
					Clay_SizingAxis.Fixed(fillWidth),
					Clay_SizingAxis.Fixed(styleRef.TrackHeight));
			}

			// Update handle position via handleLayer padding
			if (linksRef.HandleLayerEntity != 0 && nodes.Contains(linksRef.HandleLayerEntity))
			{
				var layerData = nodes.Get(linksRef.HandleLayerEntity);
				layerData.Deconstruct(out var layerNode);
				ref var layerNodeRef = ref layerNode.Ref;
				var handleX = (styleRef.Width - styleRef.HandleSize) * normalized;
				layerNodeRef.Declaration.layout.padding = new Clay_Padding
				{
					left = (ushort)handleX,
					right = 0,
					top = 0,
					bottom = 0
				};
			}

			// Emit value changed event
			// We need the entity ID, but we can't get it from the query result directly in a Changed filter
			// So we'll emit it in the slider drag system instead
		}
	}

	/// <summary>
	/// Plugin that registers all widget observers with the app.
	/// This enables reactive, Bevy-style widget behavior.
	/// </summary>
	public sealed class UiWidgetObserversPlugin : IPlugin
	{
		public void Build(App app)
		{
			// Button observers
			app.AddSystem((Query<Data<Interaction, ClayButtonStyle, UiNode>, Filter<Changed<Interaction>, With<Button>>> changedButtons) =>
				OnButtonInteractionChanged(changedButtons))
				.InStage(Stage.PreUpdate)
				.Label("ui:observers:button-visuals")
				.After("ui:interaction:update")
				.Build();

			app.AddSystem((EventReader<UiPointerEvent> events, Query<Data<Button>> buttons, Commands commands, Local<ulong> pressedButton) =>
				OnButtonClicked(events, buttons, commands, pressedButton))
				.InStage(Stage.Update)
				.Label("ui:observers:button-click")
				.Build();

			// Checkbox observers
			app.AddSystem((Query<Data<CheckboxState, CheckboxLinks, ClayCheckboxStyle>, Filter<Changed<CheckboxState>, With<Checkbox>>> changedCheckboxes, Query<Data<UiNode>> nodes, Commands commands, ResMut<ClayUiState> uiState, Local<Dictionary<ulong, bool>> lastStates) =>
				OnCheckboxStateChanged(changedCheckboxes, nodes, commands, uiState, lastStates))
				.InStage(Stage.PreUpdate)
				.Label("ui:observers:checkbox-visuals")
				.After("ui:interaction:update")
				.Build(); app.AddSystem((EventReader<UiPointerEvent> events, Query<Data<CheckboxState, CheckboxLinks, Checkbox>> checkboxes, Commands commands, Local<ulong> pressedCheckbox) =>
					OnCheckboxClicked(events, checkboxes, commands, pressedCheckbox))
					.InStage(Stage.Update)
					.Label("ui:observers:checkbox-toggle")
					.Build();

			// Slider observers
			app.AddSystem((Query<Data<SliderState, SliderLinks, ClaySliderStyle>, Filter<Changed<SliderState>>> changedSliders, Query<Data<UiNode>> nodes, Commands commands) =>
				OnSliderValueChanged(changedSliders, nodes, commands))
				.InStage(Stage.PreUpdate)
				.Label("ui:observers:slider-visuals")
				.After("ui:interaction:update")
				.Build();
		}
	}
}

public static class UiWidgetObserversAppExtensions
{
	public static App AddUiWidgetObservers(this App app)
	{
		app.AddPlugin(new UiWidgetObservers.UiWidgetObserversPlugin());
		return app;
	}
}
