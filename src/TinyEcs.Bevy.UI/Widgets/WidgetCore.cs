using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI.Widgets;

// Headless widgets, bevy_ui_widgets-style: each one is a behaviour on marker
// components and emits semantic events; the look (sprites, colours, fonts) is
// the host's. Controlled by default — a widget REPORTS a change through
// ValueChange<T> and the app writes the state back; tag the widget SelfUpdate
// to let it apply its own ValueChange (Bevy's checkbox_self_update & co).

/// <summary>Bubbling entity event: the widget was activated (click release over
/// it, or Enter / Space while it holds <see cref="InputFocus"/>).</summary>
public struct Activate
{
	public ulong Entity;
}

/// <summary>Bubbling entity event, emitted on <see cref="Source"/>: the user asked
/// the widget for <see cref="Value"/>. A controlled widget does not change on its
/// own. <see cref="IsFinal"/> is false for the intermediate values of a gesture
/// (a slider drag) and true for the value it settles on.</summary>
public struct ValueChange<T>
{
	public ulong Source;
	public T Value;
	public bool IsFinal;
}

/// <summary>The widget applies its own <see cref="ValueChange{T}"/> (an
/// uncontrolled widget). Without it, the app writes the state back.</summary>
public struct SelfUpdate;

/// <summary>Adds every headless widget plugin.</summary>
public sealed class UiWidgetsPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddPlugin(new ButtonPlugin());
		app.AddPlugin(new CheckboxPlugin());
		app.AddPlugin(new RadioPlugin());
		app.AddPlugin(new SliderPlugin());
		app.AddPlugin(new PopoverPlugin());
		app.AddPlugin(new MenuPlugin());
		app.AddPlugin(new ListBoxPlugin());
		app.AddPlugin(new TabsPlugin());
	}
}

internal static class WidgetTree
{
	/// <summary>The nearest entity, self included, that <paramref name="query"/> matches; 0 if none.</summary>
	public static ulong Nearest<T>(ulong entity, Query<Data<T>> query, Query<Data<TinyEcs.Parent>> parents)
		where T : struct
	{
		var e = entity;
		for (var depth = 0; depth < 64 && e != 0; depth++)
		{
			if (query.Contains(e)) return e;
			if (!parents.TryGet(e, out var row)) return 0;
			var (_, p) = row;
			e = p.Ref.Id;
		}
		return 0;
	}

	public static bool IsDescendantOrSelf(ulong entity, ulong ancestor, Query<Data<TinyEcs.Parent>> parents)
	{
		var e = entity;
		for (var depth = 0; depth < 64 && e != 0; depth++)
		{
			if (e == ancestor) return true;
			if (!parents.TryGet(e, out var row)) return false;
			var (_, p) = row;
			e = p.Ref.Id;
		}
		return false;
	}

	/// <summary>Descendants of <paramref name="root"/> matching <paramref name="query"/>, in
	/// tree (paint) order, skipping disabled ones and Display.None subtrees. Does
	/// not descend into a match.</summary>
	public static void Collect<T>(
		ulong root,
		Query<Data<T>> query,
		Query<Data<TinyEcs.Children>> children,
		Query<Data<Node>> nodes,
		Query<Data<InteractionDisabled>> disabled,
		List<ulong> into)
		where T : struct
	{
		if (!children.TryGet(root, out var row)) return;
		var (_, kids) = row;
		foreach (var child in kids.Ref)
		{
			if (nodes.TryGet(child, out var nodeRow))
			{
				var (_, n) = nodeRow;
				if (n.Ref.Display == Display.None) continue;
			}
			if (query.Contains(child))
			{
				if (!disabled.Contains(child)) into.Add(child);
				continue;
			}
			Collect(child, query, children, nodes, disabled, into);
		}
	}

	/// <summary>Next stop for a keyboard walk over <paramref name="items"/> from
	/// <paramref name="current"/> (-1 = none): Up/Left = previous, Down/Right = next
	/// (wrapping), Home/End = ends. -1 when the key is not a navigation key.</summary>
	public static int Step(int current, int count, KeyCode key, bool vertical, bool horizontal, bool wrap = true)
	{
		if (count == 0) return -1;
		bool prev = (vertical && key == KeyCode.Up) || (horizontal && key == KeyCode.Left);
		bool next = (vertical && key == KeyCode.Down) || (horizontal && key == KeyCode.Right);
		if (key == KeyCode.Home) return 0;
		if (key == KeyCode.End) return count - 1;
		if (next)
		{
			if (current < 0) return 0;
			return current + 1 < count ? current + 1 : wrap ? 0 : current;
		}
		if (prev)
		{
			if (current < 0) return count - 1;
			return current > 0 ? current - 1 : wrap ? count - 1 : current;
		}
		return -1;
	}

	/// <summary>Give every <typeparamref name="T"/> widget <paramref name="keys"/> as its
	/// <see cref="UiKeyCapture"/>, unless it already declares one.</summary>
	public static void CaptureKeysOn<T>(App app, UiKeys keys) where T : struct
		=> app.AddObserver<OnAdd<T>, Commands, Query<Data<UiKeyCapture>>>((t, commands, captures) =>
		{
			if (!captures.Contains(t.EntityId))
				commands.Entity(t.EntityId).Insert(new UiKeyCapture(keys));
		});
}
