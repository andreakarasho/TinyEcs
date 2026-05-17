namespace TinyEcs.Bevy.UI.Widgets;

/// Toggle widget. Place on an entity that also carries `Interaction`. The
/// `CheckboxPlugin` flips `Checked` on every `UiClick` targeting this entity
/// and fires `CheckboxChanged` as an entity-targeted trigger.
public struct Checkbox
{
	public bool Checked;
}

public struct CheckboxChanged
{
	public bool Checked;
}

public sealed class CheckboxPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddObserver<On<UiClick>, Commands, Query<Data<Checkbox>>>((trigger, cmd, boxes) =>
		{
			if (!boxes.Contains(trigger.EntityId))
				return;
			var (_, cb) = boxes.Get(trigger.EntityId);
			cb.Ref.Checked = !cb.Ref.Checked;
			cmd.Entity(trigger.EntityId).EmitTrigger(new CheckboxChanged { Checked = cb.Ref.Checked }, propagate: true);
		});
	}
}
