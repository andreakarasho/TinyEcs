using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;

namespace TinyEcsGame;

/// <summary>
/// Plugin that creates a comprehensive UI demo showcasing all available widgets.
/// Now using the reactive UI system with Bevy-style observers!
/// </summary>
public sealed class UiDemoPlugin : IPlugin
{
	public bool ShowUI { get; set; } = true;

	// Store entity IDs for interaction
	private ulong _vsyncCheckbox;
	private ulong _debugCheckbox;
	private ulong _entityCountSlider;
	private ulong _velocitySlider;
	private ulong _spawnButton;
	private ulong _clearButton;
	private ulong _pauseButton;

	public void Build(App app)
	{
		// Spawn UI elements on startup
		app.AddSystem((Commands commands, Res<WindowSize> windowSize) =>
		{
			if (!ShowUI) return;

			CreateMainControlPanel(commands, windowSize.Value);
			CreateSettingsWindow(commands);
			CreateStatsWindow(commands);
			CreateToolPalette(commands);
		})
		.InStage(Stage.Startup)
		.Label("ui:demo:spawn")
		.After("raylib:create-window")
		.Build();

		// Add reactive UI interaction handlers
		AddReactiveInteractions(app);
	}

	private void CreateMainControlPanel(Commands commands, WindowSize windowSize)
	{
		var panelWidth = 350f;
		var panelHeight = 250f;
		var x = (windowSize.Value.X - panelWidth) / 2f;
		var y = 50f;

		var window = FloatingWindowWidget.Create(
			commands,
			ClayFloatingWindowStyle.Default with
			{
				InitialSize = new Vector2(panelWidth, panelHeight),
				TitleBarColor = new Clay_Color(79, 70, 229, 255), // Indigo
				TitleFontSize = 18
			},
			"Game Controls",
			new Vector2(x, y));

		// Add heading
		LabelWidget.CreateHeading(commands, "Particle System", level: 2, parent: window.Id);

		SeparatorWidget.CreateHorizontal(commands, parent: window.Id);

		// Entity count slider
		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"Entity Count: 100,000",
			parent: window.Id);

		_entityCountSlider = SliderWidget.CreateNormalized(commands,
			ClaySliderStyle.Default with { Width = 300f },
			initialValue: 1.0f,
			parent: window.Id).Id;

		SeparatorWidget.CreateSpacer(commands, 8f, parent: window.Id);

		// Velocity slider
		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"Velocity: 250",
			parent: window.Id);

		_velocitySlider = SliderWidget.CreatePercent(commands,
			ClaySliderStyle.Default with { Width = 300f },
			initialPercent: 50f,
			parent: window.Id).Id;

		SeparatorWidget.CreateSpacer(commands, 12f, parent: window.Id);

		// Checkboxes (now with reactive interaction)
		_vsyncCheckbox = CheckboxWidget.Create(commands,
			ClayCheckboxStyle.Default,
			initialChecked: true,
			label: "Enable VSync",
			parent: window.Id).Id;

		_debugCheckbox = CheckboxWidget.Create(commands,
			ClayCheckboxStyle.Default,
			initialChecked: true,
			label: "Show Debug Info",
			parent: window.Id).Id;
	}

	private void CreateSettingsWindow(Commands commands)
	{
		var window = FloatingWindowWidget.Create(
			commands,
			ClayFloatingWindowStyle.Default with
			{
				InitialSize = new Vector2(400f, 450f),
				TitleBarColor = new Clay_Color(16, 185, 129, 255) // Emerald
			},
			"Settings",
			new Vector2(50f, 100f));

		// Graphics section
		LabelWidget.CreateHeading(commands, "Graphics", level: 3, parent: window.Id);

		CheckboxWidget.Create(commands, ClayCheckboxStyle.Default,
			true, "Fullscreen", parent: window.Id);

		CheckboxWidget.Create(commands, ClayCheckboxStyle.Default,
			false, "Anti-aliasing", parent: window.Id);

		CheckboxWidget.Create(commands, ClayCheckboxStyle.Default,
			true, "Particle Effects", parent: window.Id);

		SeparatorWidget.CreateHorizontal(commands, parent: window.Id);

		// Audio section
		LabelWidget.CreateHeading(commands, "Audio", level: 3, parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"Master Volume",
			parent: window.Id);

		SliderWidget.CreatePercent(commands,
			ClaySliderStyle.Default with { Width = 350f },
			75f,
			parent: window.Id);

		SeparatorWidget.CreateSpacer(commands, 8f, parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"Music Volume",
			parent: window.Id);

		SliderWidget.CreatePercent(commands,
			ClaySliderStyle.Default with { Width = 350f },
			60f,
			parent: window.Id);

		SeparatorWidget.CreateSpacer(commands, 8f, parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"SFX Volume",
			parent: window.Id);

		SliderWidget.CreatePercent(commands,
			ClaySliderStyle.Default with { Width = 350f },
			80f,
			parent: window.Id);

		SeparatorWidget.CreateHorizontal(commands, parent: window.Id);

		// Buttons
		var buttonStyle = ClayButtonStyle.Default with { Size = new Vector2(350f, 44f) };

		ButtonWidget.Create(commands,
			buttonStyle with { Background = new Clay_Color(59, 130, 246, 255) },
			"Apply Settings",
			window.Id);

		SeparatorWidget.CreateSpacer(commands, 8f, parent: window.Id);

		ButtonWidget.Create(commands,
			buttonStyle with { Background = new Clay_Color(107, 114, 128, 255) },
			"Reset to Defaults",
			window.Id);
	}

	private void CreateStatsWindow(Commands commands)
	{
		var window = FloatingWindowWidget.CreateTool(
			commands,
			"Performance",
			new Vector2(50f, 400f));

		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"Frame Time: 16.67ms",
			parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"FPS: 60",
			parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"Entities: 100,000",
			parent: window.Id);

		SeparatorWidget.CreateHorizontal(commands, parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Caption,
			"Memory Usage",
			parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"RAM: 128 MB",
			parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"GPU: 45 MB",
			parent: window.Id);
	}

	private void CreateToolPalette(Commands commands)
	{
		var window = FloatingWindowWidget.CreateTool(
			commands,
			"Tools",
			new Vector2(700f, 100f));

		var buttonStyle = ClayButtonStyle.Default with
		{
			Size = new Vector2(200f, 36f),
			Background = new Clay_Color(124, 58, 237, 255) // Purple
		};

		_spawnButton = ButtonWidget.Create(commands, buttonStyle, "Spawn Entities", window.Id).Id;
		_clearButton = ButtonWidget.Create(commands, buttonStyle, "Clear All", window.Id).Id;
		_pauseButton = ButtonWidget.Create(commands, buttonStyle, "Pause Simulation", window.Id).Id;

		ButtonWidget.Create(commands, buttonStyle, "Reset Camera", window.Id);

		SeparatorWidget.CreateHorizontal(commands, parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Caption,
			"Click buttons to see reactive events!",
			parent: window.Id);
	}

	private void AddLoggingOnly(App app)
	{
		// Log UI pointer events for debugging
		app.AddSystem((EventReader<UiPointerEvent> events) =>
		{
			foreach (var evt in events.Read())
			{
				if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
				{
					Console.WriteLine($"[UI] Pointer down on element {evt.Target}");
				}
			}
		})
		.InStage(Stage.Update)
		.Label("ui:demo:log-events")
		.After("ui:clay:layout")
		.Build();
	}

	/// <summary>
	/// Adds reactive observers for UI interactions using the new Bevy-style pattern.
	/// </summary>
	private void AddReactiveInteractions(App app)
	{
		// React to ALL button clicks globally
		app.AddObserver((OnClick<UiWidgetObservers.Button> trigger) =>
		{
			Console.WriteLine($"[UI Reactive] Button {trigger.EntityId} clicked!");

			// Handle specific buttons
			if (trigger.EntityId == _spawnButton)
			{
				Console.WriteLine("  → Spawning 1000 new entities...");
				// In a real app, you'd emit a command or event to spawn entities
			}
			else if (trigger.EntityId == _clearButton)
			{
				Console.WriteLine("  → Clearing all entities...");
				// In a real app, you'd emit a command to clear entities
			}
			else if (trigger.EntityId == _pauseButton)
			{
				Console.WriteLine("  → Toggling pause...");
				// In a real app, you'd toggle a pause resource
			}
		});

		// React to ALL checkbox toggles globally
		app.AddObserver((OnToggle trigger) =>
		{
			Console.WriteLine($"[UI Reactive] Checkbox {trigger.EntityId} toggled to: {trigger.NewValue}");

			// Handle specific checkboxes
			if (trigger.EntityId == _vsyncCheckbox)
			{
				Console.WriteLine($"  → VSync: {(trigger.NewValue ? "ON" : "OFF")}");
				// In a real app: Raylib.SetConfigFlags(trigger.NewValue ? ConfigFlags.VSyncHint : 0);
			}
			else if (trigger.EntityId == _debugCheckbox)
			{
				Console.WriteLine($"  → Debug Info: {(trigger.NewValue ? "SHOWN" : "HIDDEN")}");
				// In a real app: world.SetResource(new ShowDebugInfo { Value = trigger.NewValue });
			}
		});

		// React to slider value changes and update corresponding labels
		app.AddObserver((OnValueChanged trigger, Query<Data<SliderState>> sliders) =>
		{
			if (!sliders.Contains(trigger.EntityId))
				return;

			var sliderData = sliders.Get(trigger.EntityId);
			sliderData.Deconstruct(out var state);
			var normalized = state.Ref.NormalizedValue;

			Console.WriteLine($"[UI Reactive] Slider {trigger.EntityId} changed to: {normalized:F2}");

			if (trigger.EntityId == _entityCountSlider)
			{
				var count = (int)(normalized * 100000);
				Console.WriteLine($"  → Entity count target: {count}");
				// In a real app, you'd update a resource or emit an event
			}
			else if (trigger.EntityId == _velocitySlider)
			{
				var velocity = (int)(normalized * 500);
				Console.WriteLine($"  → Velocity: {velocity}");
				// In a real app, you'd update the velocity configuration
			}
		});

		// Demonstration: React to interaction state changes on buttons for visual feedback
		// This shows how you can add custom effects when buttons are hovered/pressed
		app.AddSystem((Query<Data<Interaction, UiWidgetObservers.Button>, Filter<Changed<Interaction>>> changedButtons) =>
		{
			foreach (var (interaction, _) in changedButtons)
			{
				var state = interaction.Ref;
				// You could trigger sound effects here:
				// if (state == Interaction.Pressed)
				//     PlaySound("button_press");
				// else if (state == Interaction.Hovered)
				//     PlaySound("button_hover");
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("ui:demo:button-feedback")
		.After("ui:observers:button-visuals")
		.Build();

		Console.WriteLine("[UI Demo] Reactive interactions installed!");
		Console.WriteLine("  - OnClick<Button> observers will trigger on button clicks");
		Console.WriteLine("  - OnToggle observers will trigger on checkbox changes");
		Console.WriteLine("  - OnValueChanged observers will trigger on slider adjustments");
		Console.WriteLine("  - Button interaction state changes will be logged");
	}
}
