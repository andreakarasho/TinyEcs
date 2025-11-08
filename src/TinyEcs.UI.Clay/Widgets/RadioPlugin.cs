using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds radio button widget systems to the application.
/// </summary>
public struct RadioPlugin : IPlugin
{
	public void Build(App app)
	{
		// Add radio group state resource
		app.AddResource(new RadioGroupState());

		// System to update radio button dot alpha values
		app.AddSystem((Commands commands, Query<Data<RadioDotUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				if (node.Rectangle.HasValue)
				{
					var rect = node.Rectangle.Value;
					// Use themed dot color with dynamic alpha
					rect.backgroundColor = new Clay_Color(
						update.DotColor.r,
						update.DotColor.g,
						update.DotColor.b,
						update.Alpha
					);
					node.Rectangle = rect;

					// Re-insert to trigger change detection
					commands.Entity(entityId.Ref).Insert(node);
				}

				if (node.Border.HasValue)
				{
					var border = node.Border.Value;
					// Use themed border color with dynamic alpha
					border.color = new Clay_Color(
						update.BorderColor.r,
						update.BorderColor.g,
						update.BorderColor.b,
						update.Alpha > 0 ? update.BorderColor.a : (byte)0
					);
					node.Border = border;

					// Re-insert to trigger change detection
					commands.Entity(entityId.Ref).Insert(node);
				}
			}
		})
		.InStage(Stage.First)
		.Label("radio:update-dots")
		.Build();
	}
}
