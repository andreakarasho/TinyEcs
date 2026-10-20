namespace TinyEcs.Bevy.UI.Widgets;

/// <summary>A toggle. Its state is the <see cref="Checked"/> marker. A click on it
/// (or a descendant, or a <see cref="CheckboxLabel"/>), or Enter / Space while it
/// holds focus, emits <see cref="ValueChange{T}"/> of bool — the flipped state — on
/// it. Controlled: the app writes <see cref="Checked"/> back (or emits
/// <see cref="SetChecked"/>); with <see cref="SelfUpdate"/> it flips itself.
/// <see cref="InteractionDisabled"/> checkboxes ignore input.</summary>
public struct Checkbox;

/// <summary>A clickable proxy (a checkbox's caption) for <see cref="Target"/>: a click
/// on it acts like a click on the checkbox. It must be hittable itself
/// (Interaction + a pick surface).</summary>
public struct CheckboxLabel
{
	public ulong Target;
}

/// <summary>Entity command: set the <see cref="Checked"/> state of a checkbox or
/// radio button. No <see cref="ValueChange{T}"/> is emitted (the app is writing).</summary>
public struct SetChecked
{
	public bool Value;
	public SetChecked(bool value) => Value = value;
}

/// <summary>Entity command: flip the <see cref="Checked"/> state.</summary>
public struct ToggleChecked;

public sealed class CheckboxPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddPlugin(new UiFocusPlugin());
		WidgetTree.CaptureKeysOn<Checkbox>(app, UiKeys.Activate);

		app.AddObserver<On<UiClick>, Commands, Query<Data<Checkbox>>, Query<Data<CheckboxLabel>>, Query<Data<InteractionDisabled>>, Query<Data<Checked>>, Query<Data<TinyEcs.Parent>>>(
			(t, commands, boxes, labels, disabled, checkedQ, parents) =>
			{
				var target = t.EntityId;
				if (labels.TryGet(target, out var row))
				{
					var (_, lbl) = row;
					target = lbl.Ref.Target;
				}
				else
					target = WidgetTree.Nearest(target, boxes, parents);
				if (target == 0 || !boxes.Contains(target) || disabled.Contains(target))
					return;
				Emit(commands, target, !checkedQ.Contains(target));
			});

		app.AddObserver<On<FocusedKey>, Commands, Query<Data<Checkbox>>, Query<Data<InteractionDisabled>>, Query<Data<Checked>>>(
			(t, commands, boxes, disabled, checkedQ) =>
			{
				if (ButtonPlugin.IsActivateKey(t.Event) && boxes.Contains(t.EntityId) && !disabled.Contains(t.EntityId))
					Emit(commands, t.EntityId, !checkedQ.Contains(t.EntityId));
			});

		app.AddObserver<On<ValueChange<bool>>, Commands, Query<Data<Checkbox>, Filter<With<SelfUpdate>>>, Query<Data<Checked>>>(
			(t, commands, selfUpdating, checkedQ) =>
			{
				if (selfUpdating.Contains(t.Event.Source))
					Apply(commands, t.Event.Source, t.Event.Value, checkedQ);
			});

		app.AddObserver<On<SetChecked>, Commands, Query<Data<Checked>>>(
			(t, commands, checkedQ) => Apply(commands, t.EntityId, t.Event.Value, checkedQ));

		app.AddObserver<On<ToggleChecked>, Commands, Query<Data<Checked>>>(
			(t, commands, checkedQ) => Apply(commands, t.EntityId, !checkedQ.Contains(t.EntityId), checkedQ));
	}

	private static void Emit(Commands commands, ulong box, bool value)
		=> commands.Entity(box).EmitTrigger(new ValueChange<bool> { Source = box, Value = value, IsFinal = true }, propagate: true);

	/// <summary>Insert / remove <see cref="Checked"/> on a real change only.</summary>
	public static void Apply(Commands commands, ulong entity, bool value, Query<Data<Checked>> checkedQ)
	{
		var has = checkedQ.Contains(entity);
		if (value && !has) commands.Entity(entity).Insert(new Checked());
		else if (!value && has) commands.Entity(entity).Remove<Checked>();
	}
}
