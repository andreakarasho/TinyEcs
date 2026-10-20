using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI.Widgets;

/// <summary>
/// <see cref="Button"/> behaviour: a <see cref="UiClick"/> on the button or any of its
/// descendants, or Enter / Space while it holds <see cref="InputFocus"/>, emits a
/// bubbling <see cref="Activate"/> on it. <see cref="InteractionDisabled"/> buttons
/// never activate. The keys are claimed through <see cref="UiKeyCapture"/>, so a
/// focused button's Enter does not also reach the host's hotkeys.
/// </summary>
public sealed class ButtonPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddPlugin(new UiFocusPlugin());
		WidgetTree.CaptureKeysOn<Button>(app, UiKeys.Activate);

		app.AddObserver<On<UiClick>, Commands, Query<Data<Button>>, Query<Data<InteractionDisabled>>, Query<Data<TinyEcs.Parent>>>(
			(t, commands, buttons, disabled, parents) =>
			{
				var button = WidgetTree.Nearest(t.EntityId, buttons, parents);
				if (button != 0 && !disabled.Contains(button))
					EmitActivate(commands, button);
			});

		app.AddObserver<On<FocusedKey>, Commands, Query<Data<Button>>, Query<Data<InteractionDisabled>>>(
			(t, commands, buttons, disabled) =>
			{
				if (IsActivateKey(t.Event) && buttons.Contains(t.EntityId) && !disabled.Contains(t.EntityId))
					EmitActivate(commands, t.EntityId);
			});
	}

	// ActivateDone is queued right behind Activate: it runs once every Activate
	// observer (entity-scoped ones included) has, so a follow-up that despawns
	// the button — a menu closing on its item — cannot pre-empt the handlers.
	private static void EmitActivate(Commands commands, ulong button)
	{
		var e = commands.Entity(button);
		e.EmitTrigger(new Activate { Entity = button }, propagate: true);
		e.EmitTrigger(new ActivateDone());
	}

	internal static bool IsActivateKey(in FocusedKey key)
		=> (key.Key == KeyCode.Enter || key.Key == KeyCode.Space) && !key.Ctrl && !key.Alt;
}

/// <summary>Follows every <see cref="Activate"/> once all its observers ran.</summary>
internal struct ActivateDone;
