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
		})
		.InStage(Stage.Startup)
		.Label("ui:demo:spawn")
		.After("raylib:create-window")
		.Build();

		// Add UI interaction systems
		AddInteractionSystems(app);
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
				TitleFontSize = 18,
				ZIndex = 200
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

		SliderWidget.CreateNormalized(commands,
			ClaySliderStyle.Default with { Width = 300f },
			initialValue: 1.0f,
			parent: window.Id);

		SeparatorWidget.CreateSpacer(commands, 8f, parent: window.Id);

		// Velocity slider
		LabelWidget.Create(commands, ClayLabelStyle.Body,
			"Velocity: 250",
			parent: window.Id);

		SliderWidget.CreatePercent(commands,
			ClaySliderStyle.Default with { Width = 300f },
			initialPercent: 50f,
			parent: window.Id);

		SeparatorWidget.CreateSpacer(commands, 12f, parent: window.Id);

		// Checkboxes
		CheckboxWidget.Create(commands,
			ClayCheckboxStyle.Default,
			initialChecked: true,
			label: "Enable VSync",
			parent: window.Id);

		CheckboxWidget.Create(commands,
			ClayCheckboxStyle.Default,
			initialChecked: true,
			label: "Show Debug Info",
			parent: window.Id);
	}

	private void CreateSettingsWindow(Commands commands)
	{
		var window = FloatingWindowWidget.Create(
			commands,
			ClayFloatingWindowStyle.Default with
			{
				InitialSize = new Vector2(400f, 450f),
				TitleBarColor = new Clay_Color(16, 185, 129, 255), // Emerald
				ZIndex = 150
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

		ButtonWidget.Create(commands, buttonStyle, "Spawn Entities", window.Id);
		ButtonWidget.Create(commands, buttonStyle, "Clear All", window.Id);
		ButtonWidget.Create(commands, buttonStyle, "Pause Simulation", window.Id);
		ButtonWidget.Create(commands, buttonStyle, "Reset Camera", window.Id);

		SeparatorWidget.CreateHorizontal(commands, parent: window.Id);

		LabelWidget.Create(commands, ClayLabelStyle.Caption,
			"Click to toggle tools",
			parent: window.Id);
	}

	private void AddInteractionSystems(App app)
	{
		// Update checkbox visuals based on state (runs every frame before layout)
		app.AddSystem((Query<Data<CheckboxState, UiNode>> checkboxes) =>
		{
			foreach (var (state, node) in checkboxes)
			{
				ref var nodeRef = ref node.Ref;
				var style = ClayCheckboxStyle.Default;

				// Update background color based on checked state
				nodeRef.Declaration.backgroundColor = state.Ref.Checked
					? style.CheckedColor
					: style.BoxColor;
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("ui:demo:update-checkbox-visuals")
		.Build();

		// Handle checkbox interactions - toggle state when clicked
		app.AddSystem((EventReader<UiPointerEvent> events, Query<Data<CheckboxState>> checkboxes) =>
		{
			foreach (var evt in events.Read())
			{
				if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
				{
					// Check if the clicked entity is a checkbox - use entity ID deconstruction
					foreach (var (entityId, state) in checkboxes)
					{
						// Match the clicked element ID with the entity
						if (entityId.Ref == evt.Target)
						{
							ref var stateRef = ref state.Ref;
							stateRef.Checked = !stateRef.Checked;
							Console.WriteLine($"[UI] Checkbox {evt.Target} toggled: {stateRef.Checked}");
							break;
						}
					}
				}
			}
		})
		.InStage(Stage.Update)
		.Label("ui:demo:checkbox-toggle")
		.After("ui:clay:layout")
		.Build();

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
		.After("ui:demo:checkbox-toggle")
		.Build();
	}
}
