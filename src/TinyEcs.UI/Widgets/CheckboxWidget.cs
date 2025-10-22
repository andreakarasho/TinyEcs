using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Component to track checkbox state.
/// </summary>
public struct CheckboxState
{
    public bool Checked;
}

/// <summary>
/// Helper links to locate parts of a composed checkbox.
/// Stored on the container entity.
/// </summary>
public struct CheckboxLinks
{
    public EcsID BoxEntity;
}

/// <summary>
/// Style configuration for checkbox widgets.
/// </summary>
public readonly record struct ClayCheckboxStyle(
	float BoxSize,
	Clay_Color BoxColor,
	Clay_Color CheckedColor,
	Clay_Color HoverColor,
	Clay_CornerRadius CornerRadius,
	Clay_BorderElementConfig Border,
	ushort LabelFontSize,
	Clay_Color LabelColor,
	ushort Spacing)
{
	public static ClayCheckboxStyle Default => new(
		20f,
		new Clay_Color(55, 65, 81, 255),
		new Clay_Color(59, 130, 246, 255),
		new Clay_Color(75, 85, 99, 255),
		Clay_CornerRadius.All(4),
		new Clay_BorderElementConfig
		{
			color = new Clay_Color(107, 114, 128, 255),
			width = new Clay_BorderWidth
			{
				left = 2,
				right = 2,
				top = 2,
				bottom = 2
			}
		},
		14,
		new Clay_Color(229, 231, 235, 255),
		8);
}

/// <summary>
/// Creates checkbox widgets with toggleable boolean state.
/// </summary>
public static class CheckboxWidget
{
	/// <summary>
	/// Creates a checkbox entity with an optional label.
	/// </summary>
    public static EntityCommands Create(
        Commands commands,
        ClayCheckboxStyle style,
        bool initialChecked = false,
        ReadOnlySpan<char> label = default,
        EcsID? parent = default)
    {
        // Create container for checkbox + label
        var container = commands.Spawn();
        container.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fit(0, float.MaxValue),
						Clay_SizingAxis.Fixed(style.BoxSize)),
					layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER),
					childGap = style.Spacing
				}
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			container.Insert(UiNodeParent.For(parent.Value));
        }

        // Create the checkbox box itself
        var box = commands.Spawn();
        var boxColor = initialChecked ? style.CheckedColor : style.BoxColor;

        box.Insert(new UiNode
        {
            Declaration = new Clay_ElementDeclaration
            {
                layout = new Clay_LayoutConfig
                {
                    sizing = new Clay_Sizing(
                        Clay_SizingAxis.Fixed(style.BoxSize),
                        Clay_SizingAxis.Fixed(style.BoxSize))
                },
                backgroundColor = boxColor,
                cornerRadius = style.CornerRadius,
                border = style.Border
            }
        });

        // Style on the box for hover/toggle visuals
        box.Insert(style);
        box.Insert(new CheckboxState { Checked = initialChecked });
        box.Insert(UiNodeParent.For(container.Id));

        // Link parts on the container for observer logic
        container.Insert(new CheckboxLinks { BoxEntity = box.Id });

        // Add checkmark text when checked
        if (initialChecked)
        {
            box.Insert(UiText.From("x", new Clay_TextElementConfig
            {
                textColor = new Clay_Color(255, 255, 255, 255),
                fontSize = (ushort)(style.BoxSize * 0.8f),
                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
            }));
        }

		// Add label if provided
		if (label.Length > 0)
		{
			var labelEntity = commands.Spawn();
			labelEntity.Insert(new UiNode
			{
				Declaration = new Clay_ElementDeclaration
				{
					layout = new Clay_LayoutConfig
					{
						sizing = new Clay_Sizing(
							Clay_SizingAxis.Fit(0, float.MaxValue),
							Clay_SizingAxis.Fixed(style.BoxSize)),
						childAlignment = new Clay_ChildAlignment(
							Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
							Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
					}
				}
			});

			labelEntity.Insert(UiText.From(label, new Clay_TextElementConfig
			{
				textColor = style.LabelColor,
				fontSize = style.LabelFontSize,
				textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
				wrapMode = Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE
			}));

			labelEntity.Insert(UiNodeParent.For(container.Id));
		}

        // Toggle behavior via entity-specific observer on the container so tests using EmitTrigger work
        var boxId = box.Id;
        container.Observe<UiPointerTrigger, Query<Data<CheckboxState, UiNode, ClayCheckboxStyle>>, Commands>((trigger, boxes, commands) =>
        {
            var evt = trigger.Event;
            if (evt.Type != UiPointerEventType.PointerDown || !evt.IsPrimaryButton)
                return;

            if (!boxes.Contains(boxId))
                return;

            var boxed = boxes.Get(boxId);
            boxed.Deconstruct(out var stateParam, out var nodeParam, out var styleParam);

            ref var stateRef = ref stateParam.Ref;
            ref var nodeRef = ref nodeParam.Ref;
            var styleRef = styleParam.Ref;

            // Toggle state
            stateRef.Checked = !stateRef.Checked;

            // Update visuals on the box
            nodeRef.Declaration.backgroundColor = stateRef.Checked
                ? styleRef.CheckedColor
                : styleRef.BoxColor;

            // Update checkmark text
            if (stateRef.Checked)
            {
                commands.Entity(boxId).Insert(UiText.From("x", new Clay_TextElementConfig
                {
                    textColor = new Clay_Color(255, 255, 255, 255),
                    fontSize = (ushort)(styleRef.BoxSize * 0.8f),
                    textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                }));
            }
            else
            {
                commands.Entity(boxId).Insert(UiText.From("", new Clay_TextElementConfig()));
            }
        });

        return container;
	}

	/// <summary>
	/// System to toggle checkbox state on click.
	/// Use this as a reference for implementing checkbox interactions.
	/// </summary>
	public static void ToggleCheckboxOnClick(
		EventReader<UiPointerEvent> events,
		Query<Data<CheckboxState, UiNode>> checkboxes,
		Commands commands)
	{
		foreach (var evt in events.Read())
		{
			if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
			{
				foreach (var (state, node) in checkboxes)
				{
					// You would need to match evt.Target with the entity ID
					// This is a simplified example
					ref var stateRef = ref state.Ref;
					stateRef.Checked = !stateRef.Checked;

					// Update the visual appearance
					ref var nodeRef = ref node.Ref;
					nodeRef.Declaration.backgroundColor = stateRef.Checked
						? ClayCheckboxStyle.Default.CheckedColor
						: ClayCheckboxStyle.Default.BoxColor;
				}
			}
		}
	}
}


