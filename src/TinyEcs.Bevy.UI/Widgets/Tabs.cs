namespace TinyEcs.Bevy.UI.Widgets;

/// <summary>
/// A row of <see cref="Tab"/> descendants; the open one carries <see cref="Selected"/>.
/// A click on a tab, or the arrows / Home / End while the list (or a tab in it)
/// holds focus (Left / Up = previous, Right / Down = next, wrapping), emits <see cref="ValueChange{T}"/> of ulong (the tab) on the list.
/// Controlled: the app moves <see cref="Selected"/> (<see cref="SetSelected"/>) and
/// swaps the page; with <see cref="SelfUpdate"/> the list moves the marker itself.
/// </summary>
public struct TabList;

public struct Tab;

public sealed class TabsPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddPlugin(new UiFocusPlugin());
		app.AddPlugin(new SelectionPlugin());
		WidgetTree.CaptureKeysOn<TabList>(app, UiKeys.Arrows | UiKeys.HomeEnd);

		app.AddObserver<On<UiClick>, Commands, SelectionQueries>((t, commands, q) =>
		{
			var tab = WidgetTree.Nearest(t.EntityId, q.Tabs, q.Parents);
			if (tab == 0 || q.Disabled.Contains(tab)) return;
			var list = WidgetTree.Nearest(tab, q.TabLists, q.Parents);
			if (list == 0 || q.Disabled.Contains(list) || q.SelectedQ.Contains(tab)) return;
			Emit(commands, list, tab);
		});

		app.AddObserver<On<FocusedKey>, Commands, SelectionQueries>((t, commands, q) =>
		{
			var list = WidgetTree.Nearest(t.EntityId, q.TabLists, q.Parents);
			if (list == 0 || q.Disabled.Contains(list)) return;
			var tabs = new List<ulong>();
			WidgetTree.Collect(list, q.Tabs, q.Children, q.Nodes, q.Disabled, tabs);
			var current = tabs.FindIndex(q.SelectedQ.Contains);
			var next = WidgetTree.Step(current, tabs.Count, t.Event.Key, vertical: true, horizontal: true);
			if (next < 0 || next == current) return;
			Emit(commands, list, tabs[next]);
		});

		app.AddObserver<On<ValueChange<ulong>>, Commands, SelectionQueries>((t, commands, q) =>
		{
			var list = t.Event.Source;
			if (q.TabLists.Contains(list) && q.SelfUpdating.Contains(list))
				q.Select(commands, list, t.Event.Value, q.Tabs);
		});
	}

	private static void Emit(Commands commands, ulong list, ulong tab)
		=> commands.Entity(list).EmitTrigger(new ValueChange<ulong> { Source = list, Value = tab, IsFinal = true }, propagate: true);
}
