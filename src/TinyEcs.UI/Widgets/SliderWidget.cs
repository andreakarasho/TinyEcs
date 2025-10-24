using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Component to track slider state.
/// </summary>
public struct SliderState
{
	public float Value;
	public float MinValue;
	public float MaxValue;
	public bool IsDragging;

	public readonly float NormalizedValue => (Value - MinValue) / (MaxValue - MinValue);

	public void SetNormalizedValue(float normalized)
	{
		Value = MinValue + normalized * (MaxValue - MinValue);
		Value = Math.Clamp(Value, MinValue, MaxValue);
	}
}

/// <summary>
/// Links to parts of the slider for interaction updates.
/// Stored on the container entity.
/// </summary>
public struct SliderLinks
{
	public EcsID TrackEntity;
	public EcsID FillEntity;
	public EcsID HandleEntity;
	public EcsID HandleLayerEntity;
}

/// <summary>
/// Style configuration for slider widgets.
/// </summary>
public readonly record struct ClaySliderStyle(
	float Width,
	float TrackHeight,
	float HandleSize,
	Clay_Color TrackColor,
	Clay_Color FillColor,
	Clay_Color HandleColor,
	Clay_Color HandleHoverColor,
	Clay_CornerRadius TrackRadius,
	Clay_CornerRadius HandleRadius)
{
	public static ClaySliderStyle Default => new(
		200f,
		8f,
		20f,
		new Clay_Color(55, 65, 81, 255),
		new Clay_Color(59, 130, 246, 255),
		new Clay_Color(96, 165, 250, 255),
		new Clay_Color(147, 197, 253, 255),
		Clay_CornerRadius.All(4),
		Clay_CornerRadius.All(10));

	public static ClaySliderStyle Compact => Default with
	{
		Width = 150f,
		TrackHeight = 6f,
		HandleSize = 16f
	};

	public static ClaySliderStyle Large => Default with
	{
		Width = 300f,
		TrackHeight = 10f,
		HandleSize = 24f
	};
}

/// <summary>
/// Creates horizontal slider widgets for numeric value input.
/// </summary>
public static class SliderWidget
{
	/// <summary>
	/// Creates a slider entity with the specified range and initial value.
	/// </summary>
	public static EntityCommands Create(
		Commands commands,
		ClaySliderStyle style,
		float minValue,
		float maxValue,
		float initialValue,
		EcsID? parent = default)
	{
		// Clamp initial value
		initialValue = Math.Clamp(initialValue, minValue, maxValue);

		// Create container - holds both track layer and handle layer
		var container = commands.Spawn();
		var containerNode = new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(style.Width),
						Clay_SizingAxis.Fixed(Math.Max(style.TrackHeight, style.HandleSize))),
					layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
				}
			}
		};
		// Give container a stable Clay ID for drag systems to query bounds
		containerNode.SetId(Clay_cs.ClayId.Global($"slider-container-{container.Id}"));
		container.Insert(containerNode);

		// Store style on the container for interaction logic
		container.Insert(style);

		if (parent.HasValue && parent.Value != 0)
		{
			container.Insert(UiNodeParent.For(parent.Value));
		}

		// Create track layer (background + fill, overlapped via child positioning)
		var trackLayer = commands.Spawn();
		trackLayer.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(style.Width),
						Clay_SizingAxis.Fixed(style.TrackHeight)),
					layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT
				}
			}
		});
		trackLayer.Insert(UiNodeParent.For(container.Id, index: 0));

		// Create track background
		var track = commands.Spawn();
		track.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(style.Width),
						Clay_SizingAxis.Fixed(style.TrackHeight))
				},
				backgroundColor = style.TrackColor,
				cornerRadius = style.TrackRadius
			}
		});
		track.Insert(UiNodeParent.For(trackLayer.Id));

		// Create fill track (shows current value)
		var normalized = (initialValue - minValue) / (maxValue - minValue);
		var fillWidth = style.Width * normalized;

		var fill = commands.Spawn();
		fill.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(fillWidth),
						Clay_SizingAxis.Fixed(style.TrackHeight))
				},
				backgroundColor = style.FillColor,
				cornerRadius = style.TrackRadius
			}
		});
		fill.Insert(UiNodeParent.For(track.Id));

		// Create handle layer - uses padding to position handle
		var handleX = (style.Width - style.HandleSize) * normalized;
		var handleLayer = commands.Spawn();
		handleLayer.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(style.Width),
						Clay_SizingAxis.Fixed(style.HandleSize)),
					layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
					padding = new Clay_Padding
					{
						left = (ushort)handleX,
						right = 0,
						top = 0,
						bottom = 0
					}
				}
			}
		});
		handleLayer.Insert(UiNodeParent.For(container.Id, index: 1));

		// Create handle (draggable thumb) - now uses layout-based positioning
		var handle = commands.Spawn();
		handle.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(style.HandleSize),
						Clay_SizingAxis.Fixed(style.HandleSize))
				},
				backgroundColor = style.HandleColor,
				cornerRadius = style.HandleRadius
			}
		});
		handle.Insert(UiNodeParent.For(handleLayer.Id));

		// Add slider state
		container.Insert(new SliderState
		{
			Value = initialValue,
			MinValue = minValue,
			MaxValue = maxValue,
			IsDragging = false
		});

		// Link parts for observer-driven updates
		container.Insert(new SliderLinks
		{
			TrackEntity = track.Id,
			FillEntity = fill.Id,
			HandleEntity = handle.Id,
			HandleLayerEntity = handleLayer.Id
		});

		// Attach entity observer for drag interaction (replaces global system iteration)
		container.Observe<On<UiPointerTrigger>,
			Query<Data<SliderState, SliderLinks, ClaySliderStyle>>,
			Query<Data<UiNode>>,
			ResMut<ClayUiState>>((trigger, sliders, nodes, uiState) =>
		{
			var evt = trigger.Event.Event;
			var sliderId = trigger.EntityId;

			// Get this slider's components
			if (!sliders.Contains(sliderId))
				return;

			var sliderData = sliders.Get(sliderId);
			sliderData.Deconstruct(out var statePtr, out var linksPtr, out var stylePtr);
			ref var state = ref statePtr.Ref;
			var links = linksPtr.Ref;
			var style = stylePtr.Ref;

			// Accept events from slider container or its child parts, OR when dragging
			bool isTargetingSlider = evt.CurrentTarget == sliderId ||
									 links.TrackEntity == evt.CurrentTarget ||
									 links.FillEntity == evt.CurrentTarget ||
									 links.HandleEntity == evt.CurrentTarget ||
									 links.HandleLayerEntity == evt.CurrentTarget;
			bool acceptEvent = isTargetingSlider || state.IsDragging;
			if (!acceptEvent)
				return;

			switch (evt.Type)
			{
				case UiPointerEventType.PointerDown:
					if (!isTargetingSlider) break; // Only start drag when pressing this slider
					if (evt.IsPrimaryButton)
					{
						state.IsDragging = true;
					}
					break;

				case UiPointerEventType.PointerUp:
					state.IsDragging = false;
					break;

				case UiPointerEventType.PointerMove:
					if (!state.IsDragging) break;

					// Compute normalized value from absolute pointer X relative to container bounds
					var normalized = state.NormalizedValue;
					unsafe
					{
						var ctx = uiState.Value.Context;
						if (ctx is not null)
						{
							Clay.SetCurrentContext(ctx);
							var containerElemId = ClayId.Global($"slider-container-{sliderId}").ToElementId();
							var elem = Clay.GetElementData(containerElemId);
							if (elem.found && elem.boundingBox.width > 0)
							{
								normalized = (evt.Position.X - elem.boundingBox.x) / Math.Max(1f, style.Width);
								normalized = Math.Clamp(normalized, 0f, 1f);
								state.SetNormalizedValue(normalized);
							}
						}
					}

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
					}
					break;
			}
		});

		return container;
	}

	/// <summary>
	/// System to update slider visual state when SliderState changes.
	/// Must run in PreUpdate before layout to update UiNode declarations.
	/// </summary>
	public static void UpdateSliderVisuals(
		Query<Data<SliderState, SliderLinks, ClaySliderStyle>, Filter<Changed<SliderState>>> changedSliders,
		Query<Data<UiNode>> allNodes)
	{
		foreach (var (statePtr, linksPtr, stylePtr) in changedSliders)
		{
			ref readonly var state = ref statePtr.Ref;
			ref readonly var links = ref linksPtr.Ref;
			ref readonly var style = ref stylePtr.Ref;

			var normalized = state.NormalizedValue;

			// Update fill width
			if (allNodes.Contains(links.FillEntity))
			{
				var fillData = allNodes.Get(links.FillEntity);
				fillData.Deconstruct(out _, out var fillNodePtr);
				ref var fillNode = ref fillNodePtr.Ref;

				var fillWidth = style.Width * normalized;
				fillNode.Declaration.layout.sizing = new Clay_Sizing(
					Clay_SizingAxis.Fixed(fillWidth),
					Clay_SizingAxis.Fixed(style.TrackHeight));
			}

			// Update handle position via handleLayer padding
			if (allNodes.Contains(links.HandleLayerEntity))
			{
				var layerData = allNodes.Get(links.HandleLayerEntity);
				layerData.Deconstruct(out _, out var layerNodePtr);
				ref var layerNode = ref layerNodePtr.Ref;

				var handleX = (style.Width - style.HandleSize) * normalized;
				layerNode.Declaration.layout.padding = new Clay_Padding
				{
					left = (ushort)handleX,
					right = 0,
					top = 0,
					bottom = 0
				};
			}
		}
	}

	/// <summary>
	/// Creates a percentage slider (0 to 100).
	/// </summary>
	public static EntityCommands CreatePercent(
		Commands commands,
		ClaySliderStyle style,
		float initialPercent,
		EcsID? parent = default)
	{
		return Create(commands, style, 0f, 100f, initialPercent, parent);
	}

	/// <summary>
	/// Creates a normalized slider (0.0 to 1.0).
	/// </summary>
	public static EntityCommands CreateNormalized(
		Commands commands,
		ClaySliderStyle style,
		float initialValue,
		EcsID? parent = default)
	{
		return Create(commands, style, 0f, 1f, initialValue, parent);
	}

	/// <summary>
	/// System to handle slider dragging interaction.
	/// Use this as a reference for implementing slider interactions.
	/// </summary>
	public static void HandleSliderDrag(
		EventReader<UiPointerEvent> events,
		Query<Data<SliderState, UiNode>> sliders,
		Res<ClayPointerState> pointer)
	{
		foreach (var evt in events.Read())
		{
			// This is a simplified example - you would need to:
			// 1. Match evt.Target with slider entity IDs
			// 2. Calculate normalized position from pointer.Value.Position
			// 3. Update SliderState.Value
			// 4. Update child entities (fill width, handle position)

			if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
			{
				// Start dragging
				foreach (var (state, node) in sliders)
				{
					ref var stateRef = ref state.Ref;
					stateRef.IsDragging = true;
				}
			}
			else if (evt.Type == UiPointerEventType.PointerUp)
			{
				// Stop dragging
				foreach (var (state, node) in sliders)
				{
					ref var stateRef = ref state.Ref;
					stateRef.IsDragging = false;
				}
			}
		}

		// Update slider value while dragging
		foreach (var (state, node) in sliders)
		{
			ref var stateRef = ref state.Ref;
			if (stateRef.IsDragging)
			{
				// Calculate normalized position based on pointer position
				// This requires access to the slider's screen-space bounds
				// which would come from Clay's render commands
			}
		}
	}
}
