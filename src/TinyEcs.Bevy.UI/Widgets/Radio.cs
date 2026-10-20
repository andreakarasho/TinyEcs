namespace TinyEcs.Bevy.UI.Widgets;

/// <summary>Single choice among its <see cref="RadioButton"/> descendants; the chosen
/// one carries <see cref="Checked"/>. A click on an unchecked button, Enter / Space
/// on a focused one, or the arrows / Home / End while the group (or a button in it)
/// holds focus emit <see cref="ValueChange{T}"/> of ulong (the button) on the group.
/// Controlled: the app moves <see cref="Checked"/> (<see cref="SetChecked"/>); with
/// <see cref="SelfUpdate"/> on the group it moves itself.</summary>
public struct RadioGroup;

/// <summary>One option of the nearest ancestor <see cref="RadioGroup"/>.</summary>
public struct RadioButton;

public sealed class RadioQueries : CompositeSystemParam
{
	public readonly Query<Data<RadioGroup>> Groups;
	public readonly Query<Data<RadioButton>> Buttons;
	public readonly Query<Data<Checked>> CheckedQ;
	public readonly Query<Data<InteractionDisabled>> Disabled;
	public readonly Query<Data<TinyEcs.Parent>> Parents;
	public readonly Query<Data<TinyEcs.Children>> Children;
	public readonly Query<Data<Node>> Nodes;
	public readonly Query<Data<SelfUpdate>> SelfUpdating;

	public RadioQueries()
	{
		Groups = Add(new Query<Data<RadioGroup>>());
		Buttons = Add(new Query<Data<RadioButton>>());
		CheckedQ = Add(new Query<Data<Checked>>());
		Disabled = Add(new Query<Data<InteractionDisabled>>());
		Parents = Add(new Query<Data<TinyEcs.Parent>>());
		Children = Add(new Query<Data<TinyEcs.Children>>());
		Nodes = Add(new Query<Data<Node>>());
		SelfUpdating = Add(new Query<Data<SelfUpdate>>());
	}
}

public sealed class RadioPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddPlugin(new UiFocusPlugin());
		WidgetTree.CaptureKeysOn<RadioGroup>(app, UiKeys.Arrows | UiKeys.HomeEnd);
		WidgetTree.CaptureKeysOn<RadioButton>(app, UiKeys.Activate);

		app.AddObserver<On<UiClick>, Commands, RadioQueries>((t, commands, q) =>
		{
			var button = WidgetTree.Nearest(t.EntityId, q.Buttons, q.Parents);
			if (button != 0) Choose(commands, q, button);
		});

		app.AddObserver<On<FocusedKey>, Commands, RadioQueries>((t, commands, q) =>
		{
			if (q.Buttons.Contains(t.EntityId) && ButtonPlugin.IsActivateKey(t.Event))
			{
				Choose(commands, q, t.EntityId);
				return;
			}
			var group = WidgetTree.Nearest(t.EntityId, q.Groups, q.Parents);
			if (group == 0 || q.Disabled.Contains(group)) return;
			var items = new List<ulong>();
			WidgetTree.Collect(group, q.Buttons, q.Children, q.Nodes, q.Disabled, items);
			var current = items.FindIndex(q.CheckedQ.Contains);
			var next = WidgetTree.Step(current, items.Count, t.Event.Key, vertical: true, horizontal: true);
			if (next < 0 || next == current) return;
			Emit(commands, group, items[next]);
		});

		app.AddObserver<On<ValueChange<ulong>>, Commands, RadioQueries>((t, commands, q) =>
		{
			var group = t.Event.Source;
			if (!q.Groups.Contains(group) || !q.SelfUpdating.Contains(group)) return;
			Select(commands, q, group, t.Event.Value);
		});
	}

	private static void Choose(Commands commands, RadioQueries q, ulong button)
	{
		if (q.Disabled.Contains(button) || q.CheckedQ.Contains(button)) return;
		var group = WidgetTree.Nearest(button, q.Groups, q.Parents);
		if (group == 0 || q.Disabled.Contains(group)) return;
		Emit(commands, group, button);
	}

	private static void Emit(Commands commands, ulong group, ulong button)
		=> commands.Entity(group).EmitTrigger(new ValueChange<ulong> { Source = group, Value = button, IsFinal = true }, propagate: true);

	/// <summary>Make <paramref name="button"/> the only checked <see cref="RadioButton"/> of
	/// <paramref name="group"/>.</summary>
	private static void Select(Commands commands, RadioQueries q, ulong group, ulong button)
	{
		var all = new List<ulong>();
		CollectAll(group, q, all);
		foreach (var b in all)
			CheckboxPlugin.Apply(commands, b, b == button, q.CheckedQ);
	}

	private static void CollectAll(ulong root, RadioQueries q, List<ulong> into)
	{
		if (!q.Children.TryGet(root, out var row)) return;
		var (_, kids) = row;
		foreach (var child in kids.Ref)
		{
			if (q.Buttons.Contains(child)) into.Add(child);
			else CollectAll(child, q, into);
		}
	}
}
