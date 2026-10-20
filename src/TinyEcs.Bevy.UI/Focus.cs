using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI;

// Keyboard focus, bevy_input_focus-style: ONE resource names the entity that
// receives keyboard input; widgets react to the entity triggers fired on it.
//
//   InputFocus            the focused entity (0 = none). Anything may write it.
//   Focusable             a left press on the entity takes focus.
//   AutoFocus             the entity takes focus when spawned.
//   FocusGained/FocusLost entity triggers, fired when InputFocus changes hands
//                         (and the Focused marker moves with them).
//   UiKeyCapture          keys a focused widget (or its ancestor) handles itself.
//   FocusedKey/FocusedChar bubbling entity triggers: each key press edge and each
//                         composed char, dispatched to the focused entity.
//   TabIndex + TabGroup   Tab / Shift+Tab walk the focusable entities.
//
// Key capture: a focused entity that edits text (EditableText) claims the keys
// that type or move the caret — printable keys, Backspace/Delete, Left/Right/
// Home/End, plus Enter/Tab/Up/Down/PageUp/PageDown when it is multiline. The
// dispatcher consumes those on KeyboardInput in Stage.First, before the host's
// game readers (hotkeys, movement), so a key typed into a field never also
// fires a binding. Enter / Tab / Escape / function keys stay visible to the
// host in a single-line field (submit, next field, close). UiFocusPassthrough
// opts a focused entity out (an always-focused chat line, which shares its keys
// with the game). The editor itself reads the Raw edges.

/// <summary>The entity that receives keyboard input, 0 = none.</summary>
public sealed class InputFocus
{
	public ulong Entity { get; set; }

	public ulong Get() => Entity;
	public void Set(ulong entity) => Entity = entity;
	public void Clear() => Entity = 0;
}

/// <summary>A left press on this entity gives it <see cref="InputFocus"/>.</summary>
public struct Focusable;

/// <summary>Take <see cref="InputFocus"/> as soon as it is inserted (then removed —
/// a one-shot request).</summary>
public struct AutoFocus;

/// <summary>Entity trigger: the entity just became <see cref="InputFocus"/>.</summary>
public struct FocusGained;

/// <summary>Entity trigger: the entity stopped being <see cref="InputFocus"/>.</summary>
public struct FocusLost;

/// <summary>Bubbling entity trigger: a key press edge while the target holds focus.</summary>
public struct FocusedKey
{
	public KeyCode Key;
	public bool Shift, Ctrl, Alt;
}

/// <summary>Bubbling entity trigger: one composed character typed while the target holds focus.</summary>
public struct FocusedChar
{
	public char Value;
}

/// <summary>Focused, but the keyboard is shared with the host: the dispatcher
/// consumes nothing for it (triggers still fire).</summary>
public struct UiFocusPassthrough;

/// <summary>Key groups a focused widget owns (see <see cref="UiKeyCapture"/>).</summary>
[Flags]
public enum UiKeys : byte
{
	None = 0,
	/// <summary>Enter, Space.</summary>
	Activate = 1 << 0,
	/// <summary>Up, Down.</summary>
	Vertical = 1 << 1,
	/// <summary>Left, Right.</summary>
	Horizontal = 1 << 2,
	/// <summary>Home, End.</summary>
	HomeEnd = 1 << 3,
	/// <summary>PageUp, PageDown.</summary>
	Paging = 1 << 4,
	Escape = 1 << 5,
	Arrows = Vertical | Horizontal,
}

/// <summary>The keys a widget handles itself when it — or a descendant — holds
/// <see cref="InputFocus"/>: the focus dispatcher consumes them on
/// <see cref="KeyboardInput"/> in Stage.First, before the host's game readers, so a
/// focused button's Enter never also fires a hotkey and a list's arrows never walk
/// the player. The widget plugins insert it on their roles (Button: Activate,
/// ListBox: Vertical + HomeEnd, …) unless one is already there.</summary>
public struct UiKeyCapture
{
	public UiKeys Keys;
	public UiKeyCapture(UiKeys keys) => Keys = keys;
}

/// <summary>Tab-navigation order inside the nearest <see cref="TabGroup"/>
/// (ascending; ties keep spawn order). Negative = focusable but skipped by Tab.</summary>
public struct TabIndex
{
	public int Value;
	public TabIndex(int value) => Value = value;
}

/// <summary>A Tab-navigation scope. Groups are visited by ascending Order.
/// Modal: Tab from inside the group cycles within it and never leaves.</summary>
public struct TabGroup
{
	public int Order;
	public bool Modal;
}

public sealed class UiFocusPlugin : IPlugin
{
	/// <summary>Stage.First: key capture + FocusedKey/FocusedChar dispatch + Tab
	/// navigation. Hosts order their input feed before it and their game key
	/// readers after it.</summary>
	public const string DispatchLabel = "ui:focus_dispatch";

	public void Build(App app)
	{
		if (!app.HasResource<InputFocus>()) app.AddResource(new InputFocus());
		app.AddPlugin(new InputPlugin());

		app.AddSystem((
				Commands commands,
				ResMut<InputFocus> focus,
				ResMut<KeyboardInput> kb,
				EventReader<CharInput> chars,
				Query<Data<EditableText>> editables,
				Query<Data<MultilineText>> multilines,
				Query<Data<UiFocusPassthrough>> passthrough,
				Query<Data<TabIndex>> tabbable,
				Query<Data<TabGroup>> groups,
				Query<Data<TinyEcs.Parent>> parents,
				Query<Data<Node>> nodes,
				Query<Data<InteractionDisabled>> disabled,
				Query<Data<UiKeyCapture>> keyCapture) =>
				Dispatch(commands, focus, kb, chars, editables, multilines, passthrough, tabbable, groups, parents, nodes, disabled, keyCapture))
			.InStage(Stage.First)
			.Label(DispatchLabel)
			.SingleThreaded()
			.Build();

		app.AddSystem((Commands commands, Res<InputFocus> focus, Local<ulong> previous) =>
				FireFocusChange(commands, focus, previous))
			.InStage(Stage.PostUpdate)
			.SingleThreaded()
			.Build();

		app.AddObserver<On<UiPointerDown>, ResMut<InputFocus>, Query<Data<Focusable>>>((t, focus, focusables) =>
		{
			if (focusables.Contains(t.EntityId))
				focus.Value.Entity = t.EntityId;
		});

		app.AddObserver<OnAdd<AutoFocus>, Commands, ResMut<InputFocus>>((t, commands, focus) =>
		{
			focus.Value.Entity = t.EntityId;
			commands.Entity(t.EntityId).Remove<AutoFocus>();
		});
	}

	private static void FireFocusChange(Commands commands, Res<InputFocus> focus, Local<ulong> previous)
	{
		var now = focus.Value.Entity;
		if (now == previous.Value) return;
		if (previous.Value != 0 && commands.Exists(previous.Value))
		{
			commands.Entity(previous.Value).Remove<Focused>();
			commands.Entity(previous.Value).EmitTrigger(new FocusLost());
		}
		if (now != 0 && commands.Exists(now))
		{
			commands.Entity(now).Insert(new Focused());
			commands.Entity(now).EmitTrigger(new FocusGained());
		}
		previous.Value = now;
	}

	internal static void Dispatch(
		Commands commands,
		ResMut<InputFocus> focus,
		ResMut<KeyboardInput> kb,
		EventReader<CharInput> chars,
		Query<Data<EditableText>> editables,
		Query<Data<MultilineText>> multilines,
		Query<Data<UiFocusPassthrough>> passthrough,
		Query<Data<TabIndex>> tabbable,
		Query<Data<TabGroup>> groups,
		Query<Data<TinyEcs.Parent>> parents,
		Query<Data<Node>> nodes,
		Query<Data<InteractionDisabled>> disabled,
		Query<Data<UiKeyCapture>> keyCapture)
	{
		var target = focus.Value.Entity;
		var k = kb.Value;
		if (target == 0 || !commands.Exists(target))
		{
			foreach (var _ in chars.Read()) { }
			NavigateTab(k, focus.Value, tabbable, groups, parents, nodes, disabled);
			return;
		}

		var capture = KeyCaptureMode.None;
		if (editables.TryGet(target, out var editRow) && !passthrough.Contains(target))
		{
			var (_, edit) = editRow;
			capture = edit.Ref.Multiline || multilines.Contains(target) ? KeyCaptureMode.Multiline : KeyCaptureMode.Text;
		}
		var widgetKeys = passthrough.Contains(target) ? UiKeys.None : WidgetKeys(target, keyCapture, parents);

		bool shift = Held(k, KeyCode.LeftShift) || Held(k, KeyCode.RightShift);
		bool ctrl = Held(k, KeyCode.LeftControl) || Held(k, KeyCode.RightControl);
		bool alt = Held(k, KeyCode.LeftAlt) || Held(k, KeyCode.RightAlt);
		var ec = commands.Entity(target);
		foreach (var key in k.PressedKeys)
		{
			if (k.IsPressedOnceRaw(key) && !k.IsConsumed(key))
				ec.EmitTrigger(new FocusedKey { Key = key, Shift = shift, Ctrl = ctrl, Alt = alt }, propagate: true);
			if ((capture != KeyCaptureMode.None && Captures(capture, key)) || Captures(widgetKeys, key))
				k.Consume(key);
		}
		foreach (var ev in chars.Read())
			ec.EmitTrigger(new FocusedChar { Value = ev.Value }, propagate: true);

		NavigateTab(k, focus.Value, tabbable, groups, parents, nodes, disabled);
	}

	private static bool Held(KeyboardInput k, KeyCode key) => k.IsPressedRaw(key) || k.IsPressedOnceRaw(key);

	// The focused entity's own capture plus its ancestors': a FocusedKey bubbles,
	// so a menu item's Up/Down is the menu's to handle (and to claim).
	private static UiKeys WidgetKeys(ulong target, Query<Data<UiKeyCapture>> keyCapture, Query<Data<TinyEcs.Parent>> parents)
	{
		var keys = UiKeys.None;
		var e = target;
		for (var depth = 0; depth < 64 && e != 0; depth++)
		{
			if (keyCapture.TryGet(e, out var row))
			{
				var (_, kc) = row;
				keys |= kc.Ref.Keys;
			}
			if (!parents.TryGet(e, out var p)) break;
			var (_, parent) = p;
			e = parent.Ref.Id;
		}
		return keys;
	}

	private static bool Captures(UiKeys keys, KeyCode key) => key switch
	{
		KeyCode.Enter or KeyCode.Space => (keys & UiKeys.Activate) != 0,
		KeyCode.Up or KeyCode.Down => (keys & UiKeys.Vertical) != 0,
		KeyCode.Left or KeyCode.Right => (keys & UiKeys.Horizontal) != 0,
		KeyCode.Home or KeyCode.End => (keys & UiKeys.HomeEnd) != 0,
		KeyCode.PageUp or KeyCode.PageDown => (keys & UiKeys.Paging) != 0,
		KeyCode.Escape => (keys & UiKeys.Escape) != 0,
		_ => false,
	};

	private enum KeyCaptureMode : byte { None, Text, Multiline }

	private static bool Captures(KeyCaptureMode mode, KeyCode key)
	{
		switch (key)
		{
			case KeyCode.Back:
			case KeyCode.Delete:
			case KeyCode.Left:
			case KeyCode.Right:
			case KeyCode.Home:
			case KeyCode.End:
			case KeyCode.Space:
				return true;
			case KeyCode.Up:
			case KeyCode.Down:
			case KeyCode.PageUp:
			case KeyCode.PageDown:
			case KeyCode.Enter:
			case KeyCode.Tab:
				return mode == KeyCaptureMode.Multiline;
		}
		var v = (int)key;
		return v is >= (int)KeyCode.D0 and <= (int)KeyCode.D9
			or >= (int)KeyCode.A and <= (int)KeyCode.Z
			or >= (int)KeyCode.NumPad0 and <= (int)KeyCode.Divide
			or >= (int)KeyCode.OemSemicolon and <= (int)KeyCode.OemTilde
			or >= (int)KeyCode.OemOpenBrackets and <= (int)KeyCode.Oem8
			or (int)KeyCode.OemBackslash;
	}

	// ── Tab navigation ────────────────────────────────────────────────────

	private static void NavigateTab(
		KeyboardInput k,
		InputFocus focus,
		Query<Data<TabIndex>> tabbable,
		Query<Data<TabGroup>> groups,
		Query<Data<TinyEcs.Parent>> parents,
		Query<Data<Node>> nodes,
		Query<Data<InteractionDisabled>> disabled)
	{
		if (!k.IsPressedOnce(KeyCode.Tab)) return;
		bool back = k.IsPressedRaw(KeyCode.LeftShift) || k.IsPressedOnceRaw(KeyCode.LeftShift)
			|| k.IsPressedRaw(KeyCode.RightShift) || k.IsPressedOnceRaw(KeyCode.RightShift);
		var next = TabNavigation.Next(focus.Entity, back, tabbable, groups, parents, nodes, disabled);
		if (next == 0) return;
		focus.Entity = next;
		k.Consume(KeyCode.Tab);
	}
}

/// <summary>The Tab-order walk, exposed for hosts that drive focus from their own gesture.</summary>
public static class TabNavigation
{
	private struct Stop
	{
		public ulong Entity, Group;
		public int GroupOrder, Index;
	}

	/// <summary>The entity Tab (or Shift+Tab when <paramref name="back"/>) moves focus
	/// to from <paramref name="current"/>; 0 when there is nowhere to go.</summary>
	public static ulong Next(
		ulong current, bool back,
		Query<Data<TabIndex>> tabbable,
		Query<Data<TabGroup>> groups,
		Query<Data<TinyEcs.Parent>> parents,
		Query<Data<Node>> nodes,
		Query<Data<InteractionDisabled>> disabled)
	{
		var stops = new List<Stop>();
		foreach (var (ent, idx) in tabbable)
		{
			if (idx.Ref.Value < 0 || disabled.Contains(ent.Ref) || Hidden(ent.Ref, parents, nodes)) continue;
			var group = GroupOf(ent.Ref, groups, parents, out var order);
			stops.Add(new Stop { Entity = ent.Ref, Group = group, GroupOrder = order, Index = idx.Ref.Value });
		}
		if (stops.Count == 0) return 0;

		var currentGroup = current != 0 ? GroupOf(current, groups, parents, out _) : 0;
		if (currentGroup != 0 && groups.TryGet(currentGroup, out var groupRow))
		{
			var (_, g) = groupRow;
			if (g.Ref.Modal) stops.RemoveAll(s => s.Group != currentGroup);
			if (stops.Count == 0) return 0;
		}

		stops.Sort(static (a, b) =>
		{
			var c = a.GroupOrder.CompareTo(b.GroupOrder);
			if (c != 0) return c;
			c = a.Group.CompareTo(b.Group);
			if (c != 0) return c;
			c = a.Index.CompareTo(b.Index);
			return c != 0 ? c : a.Entity.CompareTo(b.Entity);
		});

		var at = stops.FindIndex(s => s.Entity == current);
		if (at < 0) return back ? stops[^1].Entity : stops[0].Entity;
		var n = stops.Count;
		return stops[(at + (back ? n - 1 : 1)) % n].Entity;
	}

	private static ulong GroupOf(ulong entity, Query<Data<TabGroup>> groups, Query<Data<TinyEcs.Parent>> parents, out int order)
	{
		var e = entity;
		for (var depth = 0; depth < 64 && e != 0; depth++)
		{
			if (groups.TryGet(e, out var row))
			{
				var (_, g) = row;
				order = g.Ref.Order;
				return e;
			}
			if (!parents.TryGet(e, out var p)) break;
			var (_, parent) = p;
			e = parent.Ref.Id;
		}
		order = 0;
		return 0;
	}

	private static bool Hidden(ulong entity, Query<Data<TinyEcs.Parent>> parents, Query<Data<Node>> nodes)
	{
		var e = entity;
		for (var depth = 0; depth < 64 && e != 0; depth++)
		{
			if (nodes.TryGet(e, out var row))
			{
				var (_, node) = row;
				if (node.Ref.Display == Display.None) return true;
			}
			if (!parents.TryGet(e, out var p)) return false;
			var (_, parent) = p;
			e = parent.Ref.Id;
		}
		return false;
	}
}
