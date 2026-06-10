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

/// <summary>Marks a clickable entity (e.g. a checkbox's caption) as a proxy
/// for a checkbox (Target): clicking it toggles that checkbox exactly like a
/// direct hit, so a checkbox + label read as a single control. The entity must
/// be hittable itself (Interaction + a background or UiContainsByBounds).</summary>
public struct CheckboxLabel
{
	public ulong Target;
}

public sealed class CheckboxPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddObserver<On<UiClick>, Commands, Query<Data<Checkbox>>, Query<Data<CheckboxLabel>>>((trigger, cmd, boxes, labels) =>
		{
			var target = trigger.EntityId;
			if (labels.Contains(target))
			{
				var (_, lbl) = labels.Get(target);
				target = lbl.Ref.Target;
			}
			if (!boxes.Contains(target))
				return;
			var (_, cb) = boxes.Get(target);
			cb.Ref.Checked = !cb.Ref.Checked;
			cmd.Entity(target).EmitTrigger(new CheckboxChanged { Checked = cb.Ref.Checked }, propagate: true);
		});
	}
}
