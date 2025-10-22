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

        // Create container
        var container = commands.Spawn();
        container.Insert(new UiNode
        {
            Declaration = new Clay_ElementDeclaration
            {
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(style.Width),
						Clay_SizingAxis.Fixed(Math.Max(style.TrackHeight, style.HandleSize))),
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
				}
			}
		});

        // Store style on the container for interaction logic
        container.Insert(style);

        if (parent.HasValue && parent.Value != 0)
        {
            container.Insert(UiNodeParent.For(parent.Value));
        }

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
		track.Insert(UiNodeParent.For(container.Id));

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
		fill.Insert(UiNodeParent.For(container.Id));

		// Create handle (draggable thumb)
		var handleX = (style.Width - style.HandleSize) * normalized;

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
				cornerRadius = style.HandleRadius,
				floating = new Clay_FloatingElementConfig
				{
					offset = new Clay_Vector2 { x = handleX, y = -(style.HandleSize - style.TrackHeight) / 2f },
					expand = new Clay_Dimensions(0, 0),
					zIndex = 1,
					parentId = container.Id.GetHashCode() > 0 ? (uint)container.Id.GetHashCode() : 0,
					attachPoints = new Clay_FloatingAttachPoints
					{
						element = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP,
						parent = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP
					}
				}
			}
		});
		handle.Insert(UiNodeParent.For(container.Id));

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
            HandleEntity = handle.Id
        });

        return container;
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
