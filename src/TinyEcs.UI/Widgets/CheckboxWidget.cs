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
/// Creates checkbox widgets using the reactive Interaction-based pattern.
/// Checkboxes automatically update their visuals via the UiWidgetObservers system
/// when their CheckboxState component changes.
///
/// To react to checkbox toggles, add an observer:
/// app.AddObserver&lt;OnToggle&gt;((trigger) => Console.WriteLine($"Checkbox {trigger.EntityId} = {trigger.NewValue}"));
/// </summary>
public static class CheckboxWidget
{
	/// <summary>
	/// Creates a checkbox entity with an optional label.
	/// Visual updates happen automatically via observers when state changes.
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
		var containerNode = new UiNode
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
		};

		// Assign Clay ID so the checkbox can receive pointer events
		containerNode.SetId(ClayId.Global($"checkbox-{container.Id}"));
		container.Insert(containerNode);

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
						Clay_SizingAxis.Fixed(style.BoxSize)),
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
				},
				backgroundColor = boxColor,
				cornerRadius = style.CornerRadius,
				border = style.Border
			}
		});

		box.Insert(UiNodeParent.For(container.Id));

		// Add checkmark text when checked
		if (initialChecked)
		{
			box.Insert(UiText.From("✓", new Clay_TextElementConfig
			{
				textColor = new Clay_Color(255, 255, 255, 255),
				fontSize = (ushort)(style.BoxSize * 0.8f),
				textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
			}));
		}

		// Add state and style to container for observer-driven updates
		container.Insert(new CheckboxState { Checked = initialChecked });
		container.Insert(style);
		container.Insert(new CheckboxLinks { BoxEntity = box.Id });

		// Mark as interactive
		container.Insert(Interactive.Default);
		container.Insert(Interaction.None);

		// Add marker for checkbox-specific observers
		container.Insert(new UiWidgetObservers.Checkbox());

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

		return container;
	}
}


