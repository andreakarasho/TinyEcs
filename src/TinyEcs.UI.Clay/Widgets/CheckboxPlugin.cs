using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds checkbox widget systems to the application.
/// Handles box color updates based on state.
/// </summary>
public struct CheckboxPlugin : IPlugin
{
	public void Build(App app)
	{
		// System to update checkbox box colors
		app.AddSystem((Commands commands, Query<Data<CheckboxBoxUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				if (node.Rectangle.HasValue)
				{
					var rect = node.Rectangle.Value;
					rect.backgroundColor = update.Color;
					node.Rectangle = rect;

					// Re-insert to trigger change detection
					commands.Entity(entityId.Ref).Insert(node);
				}

				// Remove the marker component (no Unset available, marker will be removed on next frame)
			}
		})
		.InStage(Stage.First)
		.Label("checkbox:update-colors")
		.Build();

		// System to update checkbox fill alpha values
		app.AddSystem((Commands commands, Query<Data<CheckboxFillUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				if (node.Rectangle.HasValue)
				{
					var rect = node.Rectangle.Value;
					rect.backgroundColor = new Clay_Color(120, 190, 255, update.Alpha);
					node.Rectangle = rect;

					// Re-insert to trigger change detection
					commands.Entity(entityId.Ref).Insert(node);
				}

				if (node.Border.HasValue)
				{
					var border = node.Border.Value;
					border.color = new Clay_Color(255, 255, 255, update.Alpha > 0 ? (byte)90 : (byte)0);
					node.Border = border;

					// Re-insert to trigger change detection
					commands.Entity(entityId.Ref).Insert(node);
				}
			}
		})
		.InStage(Stage.First)
		.Label("checkbox:update-fills")
		.Build();
	}
}
