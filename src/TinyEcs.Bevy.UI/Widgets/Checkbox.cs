using TinyEcs.Bevy;

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
		app.AddObserver<On<UiClick>, WorldParam>((trigger, wp) =>
		{
			var view = wp.World.Entity(trigger.EntityId);
			if (!view.Has<Checkbox>())
				return;
			ref var cb = ref view.Get<Checkbox>();
			cb.Checked = !cb.Checked;
			wp.World.EmitTrigger(trigger.EntityId, new CheckboxChanged { Checked = cb.Checked });
		});
	}
}
