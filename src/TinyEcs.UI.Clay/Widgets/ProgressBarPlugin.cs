using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds progress bar widget systems to the application.
/// </summary>
public struct ProgressBarPlugin : IPlugin
{
	public void Build(App app)
	{
		// System to update progress bar fill width
		app.AddSystem((Commands commands, Query<Data<ProgressBarFillUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				var layout = node.Layout;
				layout.sizing = new Clay_Sizing(
					Clay_SizingAxis.Percent(update.NormalizedValue),
					Clay_SizingAxis.Grow()
				);
				node.Layout = layout;

				// Re-insert to trigger change detection
				commands.Entity(entityId.Ref).Insert(node);
			}
		})
		.InStage(Stage.First)
		.Label("progressbar:update-fill")
		.Build();

		// System to update progress bar label text
		app.AddSystem((Commands commands, Query<Data<ProgressBarLabelUpdate, ClayNode>> updates) =>
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
		.Label("progressbar:update-label")
		.Build();
	}
}
