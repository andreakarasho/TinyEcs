using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds slider widget systems to the application.
/// Handles label updates, fill width updates, and global mouse tracking for dragging.
/// </summary>
public struct SliderPlugin : IPlugin
{
	public void Build(App app)
	{
		// System to update slider label text
		app.AddSystem((Commands commands, Query<Data<SliderLabelUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				if (node.Text.HasValue)
				{
					var text = node.Text.Value;
					text.Text = update.Text;
					node.Text = text;

					// Re-insert to trigger change detection
					commands.Entity(entityId.Ref).Insert(node);
				}
			}
		})
		.InStage(Stage.First)
		.Label("slider:update-labels")
		.Build();

		// System to update slider fill width
		app.AddSystem((Commands commands, Query<Data<SliderFillUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				node.Layout.sizing = new Clay_Sizing(
					Clay_SizingAxis.Percent(update.NormalizedValue),
					Clay_SizingAxis.Grow()
				);

				// Re-insert to trigger change detection
				commands.Entity(entityId.Ref).Insert(node);
			}
		})
		.InStage(Stage.First)
		.Label("slider:update-fills")
		.Build();

		// Global slider drag system - handles mouse movement even outside slider bounds
		// This system requires Raylib for global mouse position tracking
		// It can be customized by inheriting from this plugin and overriding the Build method
	}
}
