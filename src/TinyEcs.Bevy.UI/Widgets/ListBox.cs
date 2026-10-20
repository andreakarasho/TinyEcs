using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI.Widgets;

/// <summary>
/// Single-selection list over its <see cref="ListItem"/> descendants; the chosen one
/// carries <see cref="Selected"/>. A click on an item, or Up / Down / Home / End /
/// PageUp / PageDown while the list (or an item in it) holds focus, emits
/// <see cref="ValueChange{T}"/> of ulong (the item) on the list. Controlled: the app
/// moves <see cref="Selected"/> (<see cref="SetSelected"/>); with
/// <see cref="SelfUpdate"/> on the list it moves itself. Whenever an item gains
/// <see cref="Selected"/>, the nearest scroll container scrolls it into view.
/// </summary>
public struct ListBox;

public struct ListItem;

/// <summary>Entity command on a <see cref="ListItem"/> or <see cref="Tab"/>: make it the
/// selected entry of its list / tab list. No <see cref="ValueChange{T}"/>.</summary>
public struct SetSelected;

public sealed class SelectionQueries : CompositeSystemParam
{
	public readonly Query<Data<ListBox>> Lists;
	public readonly Query<Data<ListItem>> Items;
	public readonly Query<Data<TabList>> TabLists;
	public readonly Query<Data<Tab>> Tabs;
	public readonly Query<Data<Selected>> SelectedQ;
	public readonly Query<Data<SelfUpdate>> SelfUpdating;
	public readonly Query<Data<InteractionDisabled>> Disabled;
	public readonly Query<Data<TinyEcs.Parent>> Parents;
	public readonly Query<Data<TinyEcs.Children>> Children;
	public readonly Query<Data<Node>> Nodes;

	public SelectionQueries()
	{
		Lists = Add(new Query<Data<ListBox>>());
		Items = Add(new Query<Data<ListItem>>());
		TabLists = Add(new Query<Data<TabList>>());
		Tabs = Add(new Query<Data<Tab>>());
		SelectedQ = Add(new Query<Data<Selected>>());
		SelfUpdating = Add(new Query<Data<SelfUpdate>>());
		Disabled = Add(new Query<Data<InteractionDisabled>>());
		Parents = Add(new Query<Data<TinyEcs.Parent>>());
		Children = Add(new Query<Data<TinyEcs.Children>>());
		Nodes = Add(new Query<Data<Node>>());
	}

	/// <summary>Make <paramref name="entry"/> the only <see cref="Selected"/> entry under
	/// <paramref name="root"/>.</summary>
	public void Select<T>(Commands commands, ulong root, ulong entry, Query<Data<T>> entries) where T : struct
	{
		var all = new List<ulong>();
		CollectAll(root, entries, all);
		foreach (var e in all)
		{
			var has = SelectedQ.Contains(e);
			if (e == entry && !has) commands.Entity(e).Insert(new Selected());
			else if (e != entry && has) commands.Entity(e).Remove<Selected>();
		}
	}

	private void CollectAll<T>(ulong root, Query<Data<T>> entries, List<ulong> into) where T : struct
	{
		if (!Children.TryGet(root, out var row)) return;
		var (_, kids) = row;
		foreach (var child in kids.Ref)
		{
			if (entries.Contains(child)) into.Add(child);
			else CollectAll(child, entries, into);
		}
	}
}

public sealed class ListBoxPlugin : IPlugin
{
	/// <summary>Items moved by PageUp / PageDown.</summary>
	public const int PageItems = 8;

	public void Build(App app)
	{
		app.AddPlugin(new UiFocusPlugin());
		app.AddPlugin(new SelectionPlugin());
		WidgetTree.CaptureKeysOn<ListBox>(app, UiKeys.Vertical | UiKeys.HomeEnd | UiKeys.Paging);

		app.AddObserver<On<UiClick>, Commands, SelectionQueries>((t, commands, q) =>
		{
			var item = WidgetTree.Nearest(t.EntityId, q.Items, q.Parents);
			if (item == 0 || q.Disabled.Contains(item)) return;
			var list = WidgetTree.Nearest(item, q.Lists, q.Parents);
			if (list == 0 || q.Disabled.Contains(list) || q.SelectedQ.Contains(item)) return;
			Emit(commands, list, item);
		});

		app.AddObserver<On<FocusedKey>, Commands, SelectionQueries>((t, commands, q) =>
		{
			var list = WidgetTree.Nearest(t.EntityId, q.Lists, q.Parents);
			if (list == 0 || q.Disabled.Contains(list)) return;
			var items = new List<ulong>();
			WidgetTree.Collect(list, q.Items, q.Children, q.Nodes, q.Disabled, items);
			var current = items.FindIndex(q.SelectedQ.Contains);
			int next;
			if (t.Event.Key == KeyCode.PageDown)
				next = items.Count == 0 ? -1 : Math.Min(items.Count - 1, Math.Max(0, current) + PageItems);
			else if (t.Event.Key == KeyCode.PageUp)
				next = items.Count == 0 ? -1 : Math.Max(0, current - PageItems);
			else
				next = WidgetTree.Step(current, items.Count, t.Event.Key, vertical: true, horizontal: false, wrap: false);
			if (next < 0 || next == current) return;
			Emit(commands, list, items[next]);
		});

		app.AddObserver<On<ValueChange<ulong>>, Commands, SelectionQueries>((t, commands, q) =>
		{
			var list = t.Event.Source;
			if (q.Lists.Contains(list) && q.SelfUpdating.Contains(list))
				q.Select(commands, list, t.Event.Value, q.Items);
		});

		app.AddObserver<OnAdd<Selected>, Res<UiClayContext>, Query<Data<ListItem>>, Query<Data<Node>>,
			Query<Data<ScrollPosition>>, Query<Data<TinyEcs.Parent>>>(
			(t, clay, items, nodes, scrolls, parents) =>
			{
				if (items.Contains(t.EntityId))
					ScrollIntoView(clay.Value, t.EntityId, nodes, scrolls, parents);
			});
	}

	private static void Emit(Commands commands, ulong list, ulong item)
		=> commands.Entity(list).EmitTrigger(new ValueChange<ulong> { Source = list, Value = item, IsFinal = true }, propagate: true);

	/// <summary>Scroll the nearest <see cref="Overflow.Scroll"/> ancestor just enough to
	/// show <paramref name="entity"/>'s laid-out box (last layout; Clay keeps the
	/// boxes of elements it culled from the render list).</summary>
	public static void ScrollIntoView(
		UiClayContext clay,
		ulong entity,
		Query<Data<Node>> nodes,
		Query<Data<ScrollPosition>> scrolls,
		Query<Data<TinyEcs.Parent>> parents)
	{
		if (!clay.TryGetElementBoundingBox(entity, out var item)) return;
		var e = entity;
		for (var depth = 0; depth < 64; depth++)
		{
			if (!parents.TryGet(e, out var p)) return;
			var (_, parent) = p;
			e = parent.Ref.Id;
			if (!nodes.TryGet(e, out var nodeRow)) continue;
			var (_, node) = nodeRow;
			if (node.Ref.Overflow != Overflow.Scroll || !scrolls.TryGet(e, out var scrollRow)
				|| !clay.TryGetElementBoundingBox(e, out var view))
				continue;
			var (_, sp) = scrollRow;
			float top = item.Y, bottom = item.Y + item.Height;
			float viewTop = view.Y, viewBottom = view.Y + view.Height;
			float offset = sp.Ref.OffsetY;
			if (top < viewTop) offset -= viewTop - top;
			else if (bottom > viewBottom) offset += MathF.Min(bottom - viewBottom, top - viewTop);
			offset = MathF.Max(0f, offset);
			if (offset != sp.Ref.OffsetY)
			{
				sp.Ref.OffsetY = offset;
				scrolls.SetChanged<ScrollPosition>(e);
			}
			return;
		}
	}
}

/// <summary><see cref="SetSelected"/> for list items and tabs.</summary>
internal sealed class SelectionPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddObserver<On<SetSelected>, Commands, SelectionQueries>((t, commands, q) =>
		{
			var e = t.EntityId;
			if (q.Items.Contains(e))
			{
				var list = WidgetTree.Nearest(e, q.Lists, q.Parents);
				if (list != 0) q.Select(commands, list, e, q.Items);
			}
			else if (q.Tabs.Contains(e))
			{
				var tabs = WidgetTree.Nearest(e, q.TabLists, q.Parents);
				if (tabs != 0) q.Select(commands, tabs, e, q.Tabs);
			}
		});

	}
}
