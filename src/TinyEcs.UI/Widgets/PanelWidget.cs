using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI.Widgets;

public static class PanelWidget
{
	public static EntityCommands CreateContainer(
		Commands commands,
		Vector2 size,
		EcsID? parent = default,
		byte padding = 12,
		byte gap = 8,
		Clay_Color? background = default)
	{
		var panel = commands.Spawn();
		panel.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(size.X),
						Clay_SizingAxis.Fixed(size.Y)),
					padding = Clay_Padding.All(padding),
					childGap = gap,
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP),
					layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
				},
				backgroundColor = background ?? new Clay_Color(42, 49, 61, 255)
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			panel.Insert(UiNodeParent.For(parent.Value));
		}

		return panel;
	}
}
