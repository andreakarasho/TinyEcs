using System;
using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;

namespace TinyEcs.UI.Examples;

/// <summary>
/// Example demonstrating the reactive UI system with Bevy-style observers.
/// This shows how widgets automatically update their visuals when state changes,
/// and how to react to high-level events like button clicks and checkbox toggles.
/// </summary>
public static class ReactiveUiExample
{
	public static void Run()
	{
		var app = new App(ThreadingMode.Single);

		// Add the complete reactive UI system
		app.AddReactiveUi();

		// Add a startup system to create UI
		app.AddSystem((Commands commands) => SetupUI(commands))
			.InStage(Stage.Startup)
			.Build();

		// React to button clicks globally
		app.AddObserver((OnClick<UiWidgetObservers.Button> trigger) =>
		{
			Console.WriteLine($"Button {trigger.EntityId} was clicked!");
		});

		// React to checkbox toggles globally
		app.AddObserver((OnToggle trigger) =>
		{
			Console.WriteLine($"Checkbox {trigger.EntityId} toggled to: {trigger.NewValue}");
		});

		// Run the app (in a real app, this would be in a loop)
		app.RunStartup();
		app.Update(); // First frame - UI created and laid out

		Console.WriteLine("\nReactive UI Example complete!");
		Console.WriteLine("In a real app, you would:");
		Console.WriteLine("- Run app.Update() in a game loop");
		Console.WriteLine("- Feed pointer input via ClayPointerState");
		Console.WriteLine("- Render using ClayUiState.GetRenderCommands()");
	}

	private static void SetupUI(Commands commands)
	{
		Console.WriteLine("Setting up reactive UI...");

		// Create a root panel
		var panel = PanelWidget.CreateContainer(
			commands,
			new Vector2(400, 300),
			parent: default,
			padding: 16,
			gap: 12);

		// Create a button with automatic interaction handling
		var button1 = ButtonWidget.Create(
			commands,
			ClayButtonStyle.Default,
			"Click Me!",
			panel.Id);

		// Create another button
		var button2 = ButtonWidget.Create(
			commands,
			ClayButtonStyle.Default with
			{
				Background = new Clay_cs.Clay_Color(229, 68, 109, 255),
				HoverBackground = new Clay_cs.Clay_Color(249, 88, 129, 255),
				PressedBackground = new Clay_cs.Clay_Color(209, 48, 89, 255)
			},
			"Press Me!",
			panel.Id);

		// Create checkboxes with automatic toggle handling
		var checkbox1 = CheckboxWidget.Create(
			commands,
			ClayCheckboxStyle.Default,
			initialChecked: false,
			label: "Option 1",
			parent: panel.Id);

		var checkbox2 = CheckboxWidget.Create(
			commands,
			ClayCheckboxStyle.Default,
			initialChecked: true,
			label: "Option 2 (initially checked)",
			parent: panel.Id);

		Console.WriteLine($"Created UI elements:");
		Console.WriteLine($"  Panel: {panel.Id}");
		Console.WriteLine($"  Button 1: {button1.Id}");
		Console.WriteLine($"  Button 2: {button2.Id}");
		Console.WriteLine($"  Checkbox 1: {checkbox1.Id}");
		Console.WriteLine($"  Checkbox 2: {checkbox2.Id}");
		Console.WriteLine();
		Console.WriteLine("How it works:");
		Console.WriteLine("1. Buttons have Interaction component (None/Hovered/Pressed)");
		Console.WriteLine("2. UiInteractionSystems updates Interaction based on pointer events");
		Console.WriteLine("3. UiWidgetObservers reacts to Changed<Interaction> and updates colors");
		Console.WriteLine("4. OnClick<Button> events fire when clicked");
		Console.WriteLine("5. Checkboxes toggle state and emit OnToggle events");
	}
}

/// <summary>
/// Example showing how to customize widget behavior with observers.
/// </summary>
public static class CustomWidgetObserverExample
{
	// Custom marker component for a special button type
	public struct SpecialButton { }

	public static void Run()
	{
		var app = new App(ThreadingMode.Single);
		app.AddReactiveUi();

		// Create a custom button type
		app.AddSystem((Commands commands) =>
		{
			var button = ButtonWidget.Create(
				commands,
				ClayButtonStyle.Default,
				"Special Button",
				parent: default);

			// Add custom marker
			button.Insert(new SpecialButton());
		})
		.InStage(Stage.Startup)
		.Build();

		// Add custom observer for this button type
		app.AddObserver((OnClick<UiWidgetObservers.Button> trigger, Query<Data<SpecialButton>> specialButtons) =>
		{
			// Only react if this is a special button
			if (specialButtons.Contains(trigger.EntityId))
			{
				Console.WriteLine("Special button clicked! Doing special action...");
			}
		});

		// Add system that reacts to interaction changes for special buttons
		app.AddSystem((Query<Data<Interaction, SpecialButton>, Filter<Changed<Interaction>>> specialButtons) =>
		{
			foreach (var (interaction, _) in specialButtons)
			{
				var state = interaction.Ref;
				Console.WriteLine($"Special button interaction changed to: {state}");
			}
		})
		.InStage(Stage.PreUpdate)
		.After("ui:interaction:update")
		.Build();

		app.RunStartup();
		app.Update();

		Console.WriteLine("\nCustom widget observer example complete!");
	}
}

/// <summary>
/// Example showing focus management.
/// </summary>
public static class FocusManagementExample
{
	public static void Run()
	{
		var app = new App(ThreadingMode.Single);
		app.AddReactiveUi();

		ulong focusableButton = 0;

		// Create focusable widgets
		app.AddSystem((Commands commands) =>
		{
			var button = ButtonWidget.Create(
				commands,
				ClayButtonStyle.Default,
				"Focusable Button",
				parent: default);

			// Mark as focusable
			button.Insert(Interactive.WithFocus());

			focusableButton = button.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		// React to focus changes
		app.AddObserver((OnFocusGained trigger) =>
		{
			Console.WriteLine($"Element {trigger.EntityId} gained focus via {trigger.Source}");
		});

		app.AddObserver((OnFocusLost trigger) =>
		{
			Console.WriteLine($"Element {trigger.EntityId} lost focus");
		});

		// Programmatically set focus
		app.AddSystem((ResMut<FocusManager> focusManager, Local<bool> once) =>
		{
			if (!once.Value && focusableButton != 0)
			{
				Console.WriteLine("Programmatically setting focus...");
				focusManager.Value.RequestFocus(focusableButton, FocusSource.Programmatic);
				once.Value = true;
			}
		})
		.InStage(Stage.Update)
		.Build();

		app.RunStartup();
		app.Update();
		app.Update(); // Second frame to apply focus request

		Console.WriteLine("\nFocus management example complete!");
	}
}
