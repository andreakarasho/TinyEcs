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
			CreateScrollContainerDemo(commands);
		})
		.InStage(Stage.Startup)
		.Label("ui:demo:spawn")
		.After("raylib:create-window")
		.Build();
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

		var entityCountSlider = SliderWidget.CreateNormalized(commands,
			ClaySliderStyle.Default with { Width = 300f },
			initialValue: 1.0f,
			parent: window.Id);

		entityCountSlider.Observe((On<ValueChangedEvent> trigger, Query<Data<SliderState>> sliders) =>
		{
			if (!sliders.Contains(trigger.EntityId))
				return;
			var sliderData = sliders.Get(trigger.EntityId);
			sliderData.Deconstruct(out var state);
			var normalized = state.Ref.NormalizedValue;
			var count = (int)(normalized * 100000);
			// Entity count target: count
		}); SeparatorWidget.CreateSpacer(commands, 8f, parent: window.Id);

		// Velocity slider
		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"Velocity: 250",
			parent: window.Id);

		var velocitySlider = SliderWidget.CreatePercent(commands,
			ClaySliderStyle.Default with { Width = 300f },
			initialPercent: 50f,
			parent: window.Id);

		velocitySlider.Observe((On<ValueChangedEvent> trigger, Query<Data<SliderState>> sliders) =>
		{
			if (!sliders.Contains(trigger.EntityId))
				return;
			var sliderData = sliders.Get(trigger.EntityId);
			sliderData.Deconstruct(out var state);
			var normalized = state.Ref.NormalizedValue;
			var velocity = (int)(normalized * 500);
			// Velocity: velocity
		}); SeparatorWidget.CreateSpacer(commands, 12f, parent: window.Id);

		// Checkboxes with entity-specific observers
		var vsyncCheckbox = CheckboxWidget.Create(commands,
			ClayCheckboxStyle.Default,
			initialChecked: true,
			label: "Enable VSync",
			parent: window.Id);

		vsyncCheckbox.Observe((On<ToggleEvent> trigger) =>
		{
			// VSync: trigger.Event.NewValue
			// In a real app: Raylib.SetConfigFlags(trigger.Event.NewValue ? ConfigFlags.VSyncHint : 0);
		}); var debugCheckbox = CheckboxWidget.Create(commands,
			ClayCheckboxStyle.Default,
			initialChecked: true,
			label: "Show Debug Info",
		parent: window.Id);

		debugCheckbox.Observe((On<ToggleEvent> trigger) =>
		{
			// Debug Info: trigger.Event.NewValue
			// In a real app: world.SetResource(new ShowDebugInfo { Value = trigger.Event.NewValue });
		});
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

		var spawnButton = ButtonWidget.Create(commands, buttonStyle, "Spawn Entities", window.Id);
		spawnButton.Observe((On<ClickEvent<UiWidgetObservers.Button>> trigger) =>
		{
			// Spawning 1000 new entities
			// In a real app, you'd emit a command or event to spawn entities
		});

		var clearButton = ButtonWidget.Create(commands, buttonStyle, "Clear All", window.Id);
		clearButton.Observe((On<ClickEvent<UiWidgetObservers.Button>> trigger) =>
		{
			// Clearing all entities
			// In a real app, you'd emit a command to clear entities
		});

		var pauseButton = ButtonWidget.Create(commands, buttonStyle, "Pause Simulation", window.Id);
		pauseButton.Observe((On<ClickEvent<UiWidgetObservers.Button>> trigger) =>
		{
			// Toggling pause
			// In a real app, you'd toggle a pause resource
		}); ButtonWidget.Create(commands, buttonStyle, "Reset Camera", window.Id);

		SeparatorWidget.CreateHorizontal(commands, parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Caption,
			"Click buttons to see reactive events!",
			parent: window.Id);
	}

	private void CreateScrollContainerDemo(Commands commands)
	{
		var window = FloatingWindowWidget.Create(
			commands,
			ClayFloatingWindowStyle.Default with
			{
				InitialSize = new Vector2(350f, 400f),
				TitleBarColor = new Clay_Color(236, 72, 153, 255) // Pink
			},
			"Scroll Container Demo",
			new Vector2(800f, 100f));

		LabelWidget.CreateHeading(commands, "Scrollable Content", level: 3, parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"This demonstrates a ScrollContainerWidget with draggable scrollbar:",
			parent: window.Id);

		SeparatorWidget.CreateSpacer(commands, 8f, parent: window.Id);

		// Create the scroll container
		var scrollContainer = ScrollContainerWidget.CreateVertical(
			commands,
			new Vector2(300f, 200f),
			parent: window.Id);

		// Add lots of content to make it scrollable
		for (int i = 1; i <= 20; i++)
		{
			LabelWidget.Create(commands, ClayLabelStyle.Body,
				$"Item {i} - This is scrollable content",
				parent: scrollContainer.Id);

			if (i % 5 == 0)
			{
				SeparatorWidget.CreateHorizontal(commands, parent: scrollContainer.Id);
			}
		}

		SeparatorWidget.CreateSpacer(commands, 8f, parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Caption,
			"Try scrolling with mouse wheel or dragging the scrollbar!",
			parent: window.Id);
	}

}
