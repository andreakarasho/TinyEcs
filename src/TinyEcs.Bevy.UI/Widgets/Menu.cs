using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI.Widgets;

/// <summary>
/// Opens a menu. Activating it (click, Enter / Space, or Down while focused) closes
/// its open <see cref="MenuPopup"/> if there is one, otherwise emits
/// <see cref="MenuOpen"/> on it — the app spawns the popup in response (a
/// <see cref="MenuPopup"/> with <c>Owner</c> = the button, usually also a
/// <see cref="Popover"/> anchored to it).
/// </summary>
public struct MenuButton;

/// <summary>Bubbling entity event on a <see cref="MenuButton"/>: spawn its menu.</summary>
public struct MenuOpen;

/// <summary>
/// A menu root. It is a <see cref="UiPopup"/> (a press outside it, or any right
/// press, despawns it). When its <see cref="Owner"/> held focus (opened from the
/// keyboard) it takes <see cref="InputFocus"/>, walks its <see cref="MenuItem"/>
/// descendants with Up / Down / Home / End, closes on Escape, and hands focus back
/// to the owner when it closes. Opened by the mouse, it leaves focus alone.
/// </summary>
public struct MenuPopup
{
	public ulong Owner;
}

/// <summary>A menu entry: emits <see cref="Activate"/> on click or Enter / Space, then
/// closes its <see cref="MenuPopup"/> (unless the item or the popup carries
/// <see cref="MenuKeepOpen"/>).</summary>
public struct MenuItem;

/// <summary>On a <see cref="MenuItem"/> or <see cref="MenuPopup"/>: activating an item
/// leaves the menu open (toggle rows).</summary>
public struct MenuKeepOpen;

/// <summary>Entity command on a <see cref="MenuPopup"/>: close it (focus goes back to
/// its owner).</summary>
public struct MenuClose;

public sealed class MenuQueries : CompositeSystemParam
{
	public readonly Query<Data<MenuButton>> Buttons;
	public readonly Query<Data<MenuPopup>> Popups;
	public readonly Query<Data<MenuItem>> Items;
	public readonly Query<Data<MenuKeepOpen>> KeepOpen;
	public readonly Query<Data<InteractionDisabled>> Disabled;
	public readonly Query<Data<TinyEcs.Parent>> Parents;
	public readonly Query<Data<TinyEcs.Children>> Children;
	public readonly Query<Data<Node>> Nodes;

	public MenuQueries()
	{
		Buttons = Add(new Query<Data<MenuButton>>());
		Popups = Add(new Query<Data<MenuPopup>>());
		Items = Add(new Query<Data<MenuItem>>());
		KeepOpen = Add(new Query<Data<MenuKeepOpen>>());
		Disabled = Add(new Query<Data<InteractionDisabled>>());
		Parents = Add(new Query<Data<TinyEcs.Parent>>());
		Children = Add(new Query<Data<TinyEcs.Children>>());
		Nodes = Add(new Query<Data<Node>>());
	}

	public ulong PopupOf(ulong owner)
	{
		foreach (var (ent, popup) in Popups)
			if (popup.Ref.Owner == owner) return ent.Ref;
		return 0;
	}
}

public sealed class MenuPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddPlugin(new UiFocusPlugin());
		app.AddPlugin(new ButtonPlugin());
		app.AddPlugin(new PopoverPlugin());
		WidgetTree.CaptureKeysOn<MenuButton>(app, UiKeys.Activate | UiKeys.Vertical);
		WidgetTree.CaptureKeysOn<MenuItem>(app, UiKeys.Activate);
		WidgetTree.CaptureKeysOn<MenuPopup>(app, UiKeys.Vertical | UiKeys.HomeEnd | UiKeys.Escape);

		// Keyboard flow only: a menu opened from a FOCUSED button takes focus (its
		// arrows / Escape work); one opened by the mouse leaves focus alone, so a
		// game that shares the keyboard (arrows = walk, Enter = chat) keeps it.
		app.AddObserver<OnAdd<MenuPopup>, Commands, ResMut<InputFocus>, Query<Data<UiPopup>>>((t, commands, focus, popups) =>
		{
			if (!popups.Contains(t.EntityId)) commands.Entity(t.EntityId).Insert(new UiPopup());
			if (focus.Value.Entity != 0 && focus.Value.Entity == t.Component.Owner)
				focus.Value.Entity = t.EntityId;
		});

		// Closing by any path (Escape, an item, an outside press, the app) hands
		// focus back to the owner when it was inside the menu.
		// (A despawn may take the focused item down before its popup: a dead focus
		// counts as inside.)
		app.AddObserver<OnRemove<MenuPopup>, Commands, ResMut<InputFocus>, Query<Data<TinyEcs.Parent>>>((t, commands, focus, parents) =>
		{
			var f = focus.Value.Entity;
			if (f != 0 && (!commands.Exists(f) || WidgetTree.IsDescendantOrSelf(f, t.EntityId, parents)))
				focus.Value.Entity = t.Component.Owner;
		});

		app.AddObserver<On<Activate>, Commands, MenuQueries>((t, commands, q) =>
		{
			if (q.Buttons.Contains(t.EntityId))
				Toggle(commands, q, t.EntityId);
		});

		app.AddObserver<On<ActivateDone>, Commands, MenuQueries>((t, commands, q) =>
		{
			var item = t.EntityId;
			if (!q.Items.Contains(item) || q.KeepOpen.Contains(item)) return;
			var popup = WidgetTree.Nearest(item, q.Popups, q.Parents);
			if (popup != 0 && !q.KeepOpen.Contains(popup))
				commands.Entity(popup).EmitTrigger(new MenuClose());
		});

		app.AddObserver<On<MenuClose>, Commands, Query<Data<MenuPopup>>>((t, commands, popups) =>
		{
			if (popups.Contains(t.EntityId)) commands.Entity(t.EntityId).Despawn();
		});

		// MenuItem is a Button underneath: click / Enter / Space -> Activate.
		app.AddObserver<OnAdd<MenuItem>, Commands, Query<Data<Button>>>((t, commands, buttons) =>
		{
			if (!buttons.Contains(t.EntityId)) commands.Entity(t.EntityId).Insert(new Button());
		});
		app.AddObserver<OnAdd<MenuButton>, Commands, Query<Data<Button>>>((t, commands, buttons) =>
		{
			if (!buttons.Contains(t.EntityId)) commands.Entity(t.EntityId).Insert(new Button());
		});

		app.AddObserver<On<FocusedKey>, Commands, ResMut<InputFocus>, MenuQueries>((t, commands, focus, q) =>
		{
			var key = t.Event.Key;
			var focused = t.EntityId;
			if (q.Buttons.Contains(focused))
			{
				if (key == KeyCode.Down && !q.Disabled.Contains(focused) && q.PopupOf(focused) == 0)
					commands.Entity(focused).EmitTrigger(new MenuOpen(), propagate: true);
				return;
			}
			var popup = WidgetTree.Nearest(focused, q.Popups, q.Parents);
			if (popup == 0) return;
			if (key == KeyCode.Escape)
			{
				commands.Entity(popup).EmitTrigger(new MenuClose());
				return;
			}
			var items = new List<ulong>();
			WidgetTree.Collect(popup, q.Items, q.Children, q.Nodes, q.Disabled, items);
			var current = items.IndexOf(focused);
			var next = WidgetTree.Step(current, items.Count, key, vertical: true, horizontal: false);
			if (next >= 0) focus.Value.Entity = items[next];
		});
	}

	private static void Toggle(Commands commands, MenuQueries q, ulong button)
	{
		var open = q.PopupOf(button);
		if (open != 0)
			commands.Entity(open).EmitTrigger(new MenuClose());
		else
			commands.Entity(button).EmitTrigger(new MenuOpen(), propagate: true);
	}
}
