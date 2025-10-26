using System;
using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Flexbox;

/// <summary>
/// Scrollable container widget for the Flexbox UI system.
/// - Clips its content to the content rect
/// - Responds to PointerScroll events to adjust scroll offset
/// </summary>
public static class FlexboxScrollContainerWidget
{
	/// <summary>
	/// Create a vertical scroll container of a fixed size. Returns the container entity for adding children.
	/// </summary>
	public static EntityCommands CreateVertical(Commands commands, Vector2 size, ulong parent = 0, float scrollSpeed = 24f)
	{
		var id = commands.Spawn()
			.Insert(new FlexboxNode
			{
				FlexDirection = FlexDirection.Column,
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Stretch,
				Width = FlexValue.Points(size.X),
				Height = FlexValue.Points(size.Y),
				Overflow = Overflow.Hidden,
				BackgroundColor = Vector4.Zero
			})
			.Insert(FlexboxScrollContainer.VerticalOnly(scrollSpeed))
			.Id;

		if (parent != 0)
			commands.Entity(id).Insert(new FlexboxNodeParent(parent));

		// Handle wheel scroll by adjusting component Offset (per-entity observer)
		commands.Entity(id)
			.Observe<On<UiPointerTrigger>, Query<Data<FlexboxScrollContainer>>, Commands, Res<FlexboxUiState>>((trigger, scrollers, cmd, state) =>
			{
				var e = trigger.Event.Event;
				if (e.Type != UiPointerEventType.PointerScroll)
					return;

				if (!scrollers.Contains(trigger.EntityId))
					return;

				// Fetch current scroll container component
				var data = scrollers.Get(trigger.EntityId);
				data.Deconstruct(out var sPtr);
				var sc = sPtr.Ref;

				// Apply scroll (positive wheel up -> negative offset change to move content down)
				var delta = e.ScrollDelta * sc.ScrollSpeed;
				if (!sc.Vertical) delta.Y = 0;
				if (!sc.Horizontal) delta.X = 0;

				var newOffset = sc.Offset - delta; // invert so wheel up scrolls content down

				// Optional clamp using Flexbox node layouts if available
				if (state.Value.TryGetNode(trigger.EntityId, out var containerNode) && containerNode != null)
				{
					// Compute max bottom of descendants relative to container content top
					var containerLayout = containerNode.layout;
					float contentTop = containerLayout.content.y;
					float viewportH = containerLayout.content.height;
					float maxBottom = contentTop; // at least top

					void Traverse(global::Flexbox.Node node)
					{
						foreach (var child in node.Children)
						{
							var childLayout = child.layout;
							var b = childLayout.top + childLayout.height;
							if (b > maxBottom) maxBottom = b;
							Traverse(child);
						}
					}
					Traverse(containerNode);

					var contentHeight = MathF.Max(0f, maxBottom - contentTop);
					var maxScrollY = MathF.Max(0f, contentHeight - viewportH);
					if (maxScrollY <= 0f) newOffset.Y = 0f; else newOffset.Y = System.Math.Clamp(newOffset.Y, 0f, maxScrollY);
				}

				sc.Offset = newOffset;
				cmd.Entity(trigger.EntityId).Insert(sc);
			});

		return commands.Entity(id);
	}
}


