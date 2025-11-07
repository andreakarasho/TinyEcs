using TinyEcs.Bevy;
using Clay_cs;
using System;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds panel widget systems to the application.
/// Handles automatic scrollbar updates when panel content size changes.
/// </summary>
public struct PanelPlugin : IPlugin
{
	public void Build(App app)
	{
		// System to monitor content area size and update scrollbars
		app.AddSystem((Commands commands, Query<Data<PanelState>> panels, Query<Data<ClayComputedLayout>> layouts) =>
		{
			foreach (var (panelEntityId, statePtr) in panels)
			{
				var state = statePtr.Ref;

				if (!state.EnableVerticalScrolling && !state.EnableHorizontalScrolling)
					continue;

				// Get the content wrapper layout (viewport - visible area)
				if (!layouts.Contains(state.ContentWrapperEntityId))
					continue;

				// Get the content area layout (actual content size)
				if (!layouts.Contains(state.ContentAreaEntityId))
					continue;

				var (_, wrapperLayoutPtr) = layouts.Get(state.ContentWrapperEntityId);
				var (_, contentLayoutPtr) = layouts.Get(state.ContentAreaEntityId);

				var wrapperLayout = wrapperLayoutPtr.Ref;
				var contentLayout = contentLayoutPtr.Ref;

				// Visible size is the wrapper (viewport) size
				// Content size is the actual content area size (which fits its children)
				float visibleWidth = wrapperLayout.Width;
				float visibleHeight = wrapperLayout.Height;
				float contentWidth = contentLayout.Width;
				float contentHeight = contentLayout.Height;

				// Check if we need to update the scrollbars
				bool widthChanged = Math.Abs(state.ContentWidth - contentWidth) > 0.1f;
				bool heightChanged = Math.Abs(state.ContentHeight - contentHeight) > 0.1f;

				if (widthChanged || heightChanged)
				{
					// Update panel state
					state.ContentWidth = contentWidth;
					state.ContentHeight = contentHeight;
					commands.Entity(panelEntityId.Ref).Insert(state);

					// Update vertical scrollbar
					if (state.EnableVerticalScrolling && state.VerticalScrollbarId != 0)
					{
						commands.Entity(state.VerticalScrollbarId).Insert(new ScrollbarContentUpdate
						{
							ContentSize = contentHeight,
							VisibleSize = visibleHeight
						});
					}

					// Update horizontal scrollbar
					if (state.EnableHorizontalScrolling && state.HorizontalScrollbarId != 0)
					{
						commands.Entity(state.HorizontalScrollbarId).Insert(new ScrollbarContentUpdate
						{
							ContentSize = contentWidth,
							VisibleSize = visibleWidth
						});
					}
				}
			}
		})
		.InStage(Stage.PostUpdate)
		.Label("panel:update-scrollbars")
		.Build();
	}
}
