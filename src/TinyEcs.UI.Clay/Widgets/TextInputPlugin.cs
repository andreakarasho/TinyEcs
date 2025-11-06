using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds text input widget systems to the application.
/// </summary>
public struct TextInputPlugin : IPlugin
{
	public void Build(App app)
	{
		// System to update text input text display
		app.AddSystem((Commands commands, Query<Data<TextInputTextUpdate, ClayNode>> updates) =>
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
		.Label("textinput:update-text")
		.Build();
	}
}
