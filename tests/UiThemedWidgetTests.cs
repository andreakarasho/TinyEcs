using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI.Bevy;
using Flexbox;

namespace TinyEcs.Tests;

/// <summary>
/// Comprehensive tests for themed Bevy-style UI widgets.
/// Tests Button, Slider, Checkbox, Toggle, RadioButton, and TextInput widgets
/// with simulated pointer input.
/// </summary>
public class UiThemedWidgetTests
{
	/// <summary>
	/// Helper method to simulate pointer input by updating PointerInputState resource.
	/// </summary>
	private static void SimulatePointerClick(App app, Vector2 position)
	{
		var world = app.GetWorld();

		// Get the pointer input state resource
		var pointerState = world.GetResource<PointerInputState>();

		// Simulate mouse press
		pointerState.Position = position;
		pointerState.IsPrimaryButtonPressed = true;
		pointerState.IsPrimaryButtonDown = true;
		app.Update(); // Process press

		// Simulate mouse hold
		pointerState.IsPrimaryButtonPressed = false;
		pointerState.IsPrimaryButtonDown = true;
		app.Update(); // Process hold

		// Simulate mouse release
		pointerState.IsPrimaryButtonDown = false;
		pointerState.IsPrimaryButtonReleased = true;
		app.Update(); // Process release

		// Clear state
		pointerState.IsPrimaryButtonReleased = false;
	}

	/// <summary>
	/// Helper to get widget center position from ComputedLayout.
	/// </summary>
	private static Vector2 GetWidgetCenter(World world, ulong entityId)
	{
		var layout = world.Get<ComputedLayout>(entityId);
		return new Vector2(layout.X + layout.Width / 2f, layout.Y + layout.Height / 2f);
	}

	[Fact]
	public void ThemedButton_Click_EmitsActivateEvent()
	{
		var app = new App();
		app.AddPlugin(new TinyEcsUiPlugin());
		app.AddPlugin(new TinyEcsUiWidgetsPlugin());
		app.AddResource(UiTheme.Dark());

		ulong buttonId = 0;
		bool buttonClicked = false;

		// Create button using ThemedWidgetBuilders
		app.AddSystem((Commands commands, Res<UiTheme> theme) =>
		{
			buttonId = ThemedWidgetBuilders.CreateButton(commands, theme.Value, "Test Button");

			// Set a fixed position for testing
			commands.Entity(buttonId).Insert(new UiNode
			{
				Width = FlexValue.Points(120f),
				Height = FlexValue.Points(40f),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(100f),
				Top = FlexValue.Points(50f),
			});
		})
		.InStage(Stage.Startup)
		.Build();

		// Observer to track button activation
		app.AddObserver<On<Activate>>((trigger) =>
		{
			if (trigger.EntityId == buttonId)
			{
				buttonClicked = true;
			}
		});

		app.RunStartup();
		var world = app.GetWorld();

		// Verify button was created
		Assert.True(world.Has<Button>(buttonId));
		Assert.True(world.Has<FluxInteraction>(buttonId));
		Assert.True(world.Has<Interactive>(buttonId));

		// Run layout calculation
		app.Update();

		// Simulate click on button center
		var center = GetWidgetCenter(world, buttonId);
		SimulatePointerClick(app, center);

		// Run one more update to process deferred commands and trigger observers
		app.Update();

		// Verify button was clicked
		Assert.True(buttonClicked, "Button should emit Activate event when clicked");
	}

	[Fact]
	public void ThemedSlider_Drag_UpdatesValue()
	{
		var app = new App();
		app.AddPlugin(new TinyEcsUiPlugin());
		app.AddPlugin(new TinyEcsUiWidgetsPlugin());
		app.AddResource(UiTheme.Dark());

		ulong sliderId = 0;
		float? newValue = null;

		// Create slider
		app.AddSystem((Commands commands, Res<UiTheme> theme) =>
		{
			sliderId = ThemedWidgetBuilders.CreateSlider(commands, theme.Value, 0f, 100f, 50f, SliderDirection.Horizontal);

			// Set fixed position
			commands.Entity(sliderId).Insert(new UiNode
			{
				Width = FlexValue.Points(200f),
				Height = FlexValue.Points(10f),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(100f),
				Top = FlexValue.Points(100f),
			});
		})
		.InStage(Stage.Startup)
		.Build();

		// Observer to track slider changes
		app.AddObserver<On<SliderChanged>>((trigger) =>
		{
			if (trigger.EntityId == sliderId)
			{
				newValue = trigger.Event.Value;
			}
		});

		app.RunStartup();
		var world = app.GetWorld();

		// Verify slider was created
		Assert.True(world.Has<Slider>(sliderId));
		var slider = world.Get<Slider>(sliderId);
		Assert.Equal(50f, slider.Value);

		// Run layout calculation
		app.Update();

		// Simulate drag on slider (at 75% position)
		var layout = world.Get<ComputedLayout>(sliderId);
		var dragPos = new Vector2(layout.X + layout.Width * 0.75f, layout.Y + layout.Height / 2f);
		SimulatePointerClick(app, dragPos);

		// Verify slider value changed
		Assert.NotNull(newValue);
		Assert.True(newValue > 50f, $"Slider value should increase (was {newValue})");
	}

	[Fact]
	public void ThemedCheckbox_Click_TogglesState()
	{
		var app = new App();
		app.AddPlugin(new TinyEcsUiPlugin());
		app.AddPlugin(new TinyEcsUiWidgetsPlugin());
		app.AddResource(UiTheme.Dark());

		ulong checkboxId = 0;
		bool? checkedState = null;
		bool interactionStateChanged = false;
		bool fluxInteractionChanged = false;

		// Create checkbox (initially unchecked)
		app.AddSystem((Commands commands, Res<UiTheme> theme) =>
		{
			checkboxId = ThemedWidgetBuilders.CreateCheckbox(commands, theme.Value, initiallyChecked: false);

			// Set fixed position
			commands.Entity(checkboxId).Insert(new UiNode
			{
				Width = FlexValue.Points(20f),
				Height = FlexValue.Points(20f),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(100f),
				Top = FlexValue.Points(150f),
			});
		})
		.InStage(Stage.Startup)
		.Build();

		// Observer to track checkbox changes
		app.AddObserver<On<CheckboxChanged>>((trigger) =>
		{
			if (trigger.EntityId == checkboxId)
			{
				checkedState = trigger.Event.Checked;
				Console.WriteLine($"[TEST] CheckboxChanged event fired: {trigger.Event.Checked}");
			}
		});

		// Debug observer for InteractionState changes AFTER flux update
		app.AddSystem((Query<Data<PrevInteraction, InteractionState, FluxInteraction>> entities) =>
		{
			foreach (var (entityId, prev, state, flux) in entities)
			{
				if (entityId.Ref == checkboxId)
				{
					Console.WriteLine($"[TEST-AFTER-FLUX] Prev={prev.Ref.State}, Curr={state.Ref.State}, Flux={flux.Ref.State}");
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("test:debug-after-flux")
		.After("flux:update-interaction")
		.Build();

		// Debug observer AFTER prev update
		app.AddSystem((Query<Data<PrevInteraction, InteractionState, FluxInteraction>> entities) =>
		{
			foreach (var (entityId, prev, state, flux) in entities)
			{
				if (entityId.Ref == checkboxId)
				{
					Console.WriteLine($"[TEST-AFTER-PREV] Prev={prev.Ref.State}, Curr={state.Ref.State}, Flux={flux.Ref.State}");
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("test:debug-after-prev")
		.After("flux:update-prev")
		.Build();

		// Debug observer for FluxInteraction changes
		app.AddSystem((Query<Data<FluxInteraction>, Filter<Changed<FluxInteraction>>> changed) =>
		{
			foreach (var (entityId, flux) in changed)
			{
				if (entityId.Ref == checkboxId)
				{
					fluxInteractionChanged = true;
					Console.WriteLine($"[TEST] FluxInteraction changed to: {flux.Ref.State}");
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("test:debug-flux")
		.After("flux:update-interaction")
		.Build();

		app.RunStartup();
		var world = app.GetWorld();

		// Set up delta time for flux interaction
		var deltaTime = world.GetResource<DeltaTime>();
		deltaTime.Seconds = 0.016f; // 60 FPS

		// Verify checkbox was created
		Assert.True(world.Has<Checkbox>(checkboxId));
		var checkbox = world.Get<Checkbox>(checkboxId);
		Assert.False(checkbox.Checked);

		// Run layout calculation
		app.Update();

		// Simulate click on checkbox
		var center = GetWidgetCenter(world, checkboxId);
		SimulatePointerClick(app, center);

		// Run update to process the pointer events and trigger FluxInteraction transition
		app.Update();

		// Run one more update to process deferred commands and let Changed<FluxInteraction> systems run
		app.Update();

		// Verify checkbox toggled to checked
		Assert.NotNull(checkedState);
		Assert.True(checkedState.Value, "Checkbox should be checked after click");

		// Click again to uncheck
		checkedState = null;
		SimulatePointerClick(app, center);

		// Run update to process the pointer events and trigger FluxInteraction transition
		app.Update();

		// Run one more update to process deferred commands and let Changed<FluxInteraction> systems run
		app.Update();

		// Verify checkbox toggled back to unchecked
		Assert.NotNull(checkedState);
		Assert.False(checkedState.Value, "Checkbox should be unchecked after second click");
	}

	[Fact]
	public void ThemedToggle_Click_TogglesState()
	{
		var app = new App();
		app.AddPlugin(new TinyEcsUiPlugin());
		app.AddPlugin(new TinyEcsUiWidgetsPlugin());
		app.AddResource(UiTheme.Dark());

		ulong toggleId = 0;
		bool? toggleState = null;

		// Create toggle (initially off)
		app.AddSystem((Commands commands, Res<UiTheme> theme) =>
		{
			toggleId = ThemedWidgetBuilders.CreateToggle(commands, theme.Value, initialValue: false);

			// Set fixed position
			commands.Entity(toggleId).Insert(new UiNode
			{
				Width = FlexValue.Points(44f),
				Height = FlexValue.Points(24f),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(100f),
				Top = FlexValue.Points(200f),
			});
		})
		.InStage(Stage.Startup)
		.Build();

		// Observer to track toggle changes
		app.AddObserver<On<ToggleChanged>>((trigger) =>
		{
			if (trigger.EntityId == toggleId)
			{
				toggleState = trigger.Event.IsOn;
			}
		});

		app.RunStartup();
		var world = app.GetWorld();

		// Set up delta time for flux interaction
		var deltaTime = world.GetResource<DeltaTime>();
		deltaTime.Seconds = 0.016f; // 60 FPS

		// Verify toggle was created
		Assert.True(world.Has<Toggle>(toggleId));
		var toggle = world.Get<Toggle>(toggleId);
		Assert.False(toggle.IsOn);

		// Run layout calculation
		app.Update();

		// Simulate click on toggle
		var center = GetWidgetCenter(world, toggleId);
		SimulatePointerClick(app, center);

		// Run update to process the pointer events and trigger FluxInteraction transition
		app.Update();

		// Run one more update to process deferred commands and let Changed<FluxInteraction> systems run
		app.Update();

		// Verify toggle switched on
		Assert.NotNull(toggleState);
		Assert.True(toggleState.Value, "Toggle should be ON after click");

		// Click again to turn off
		toggleState = null;
		SimulatePointerClick(app, center);

		// Run update to process the pointer events and trigger FluxInteraction transition
		app.Update();

		// Run one more update to process deferred commands and let Changed<FluxInteraction> systems run
		app.Update();

		// Verify toggle switched off
		Assert.NotNull(toggleState);
		Assert.False(toggleState.Value, "Toggle should be OFF after second click");
	}

	[Fact]
	public void ThemedRadioButton_Click_SelectsInGroup()
	{
		var app = new App();
		app.AddPlugin(new TinyEcsUiPlugin());
		app.AddPlugin(new TinyEcsUiWidgetsPlugin());
		app.AddResource(UiTheme.Dark());

		ulong radio1Id = 0;
		ulong radio2Id = 0;
		ulong radio3Id = 0;
		int? selectedValue = null;

		const int groupId = 1;

		// Create radio button group
		app.AddSystem((Commands commands, Res<UiTheme> theme) =>
		{
			radio1Id = ThemedWidgetBuilders.CreateRadioButton(commands, theme.Value, groupId, value: 1, initiallySelected: true);
			commands.Entity(radio1Id).Insert(new UiNode
			{
				Width = FlexValue.Points(20f),
				Height = FlexValue.Points(20f),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(100f),
				Top = FlexValue.Points(250f),
			});

			radio2Id = ThemedWidgetBuilders.CreateRadioButton(commands, theme.Value, groupId, value: 2, initiallySelected: false);
			commands.Entity(radio2Id).Insert(new UiNode
			{
				Width = FlexValue.Points(20f),
				Height = FlexValue.Points(20f),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(130f),
				Top = FlexValue.Points(250f),
			});

			radio3Id = ThemedWidgetBuilders.CreateRadioButton(commands, theme.Value, groupId, value: 3, initiallySelected: false);
			commands.Entity(radio3Id).Insert(new UiNode
			{
				Width = FlexValue.Points(20f),
				Height = FlexValue.Points(20f),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(160f),
				Top = FlexValue.Points(250f),
			});
		})
		.InStage(Stage.Startup)
		.Build();

		// Observer to track radio button selection
		app.AddObserver<On<RadioButtonSelected>>((trigger) =>
		{
			if (trigger.Event.GroupId == groupId)
			{
				selectedValue = trigger.Event.Value;
			}
		});

		app.RunStartup();
		var world = app.GetWorld();

		// Set up delta time for flux interaction
		var deltaTime = world.GetResource<DeltaTime>();
		deltaTime.Seconds = 0.016f; // 60 FPS

		// Verify radio buttons were created
		Assert.True(world.Has<RadioButton>(radio1Id));
		Assert.True(world.Has<RadioButton>(radio2Id));
		Assert.True(world.Has<RadioButton>(radio3Id));

		// Run layout calculation
		app.Update();

		// Click on radio2
		var center2 = GetWidgetCenter(world, radio2Id);
		SimulatePointerClick(app, center2);

		// Run update to process the pointer events and trigger FluxInteraction transition
		app.Update();

		// Run one more update to process deferred commands and let Changed<FluxInteraction> systems run
		app.Update();

		// Verify radio2 was selected
		Assert.NotNull(selectedValue);
		Assert.Equal(2, selectedValue.Value);

		// Verify only radio2 is selected
		var radio1 = world.Get<RadioButton>(radio1Id);
		var radio2 = world.Get<RadioButton>(radio2Id);
		var radio3 = world.Get<RadioButton>(radio3Id);

		Assert.False(radio1.Selected);
		Assert.True(radio2.Selected);
		Assert.False(radio3.Selected);

		// Click on radio3
		selectedValue = null;
		var center3 = GetWidgetCenter(world, radio3Id);
		SimulatePointerClick(app, center3);

		// Run update to process the pointer events and trigger FluxInteraction transition
		app.Update();

		// Run one more update to process deferred commands and let Changed<FluxInteraction> systems run
		app.Update();

		// Verify radio3 was selected
		Assert.NotNull(selectedValue);
		Assert.Equal(3, selectedValue.Value);

		// Verify only radio3 is selected now
		radio1 = world.Get<RadioButton>(radio1Id);
		radio2 = world.Get<RadioButton>(radio2Id);
		radio3 = world.Get<RadioButton>(radio3Id);

		Assert.False(radio1.Selected);
		Assert.False(radio2.Selected);
		Assert.True(radio3.Selected);
	}

	[Fact]
	public void ThemedTextInput_Click_GainsFocus()
	{
		var app = new App();
		app.AddPlugin(new TinyEcsUiPlugin());
		app.AddPlugin(new TinyEcsUiWidgetsPlugin());
		app.AddResource(UiTheme.Dark());

		ulong textInputId = 0;
		bool focusGained = false;

		// Create text input
		app.AddSystem((Commands commands, Res<UiTheme> theme) =>
		{
			textInputId = ThemedWidgetBuilders.CreateTextInput(commands, theme.Value, placeholder: "Enter text...", maxLength: 50);

			// Set fixed position
			commands.Entity(textInputId).Insert(new UiNode
			{
				Width = FlexValue.Points(200f),
				Height = FlexValue.Points(36f),
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(100f),
				Top = FlexValue.Points(300f),
			});
		})
		.InStage(Stage.Startup)
		.Build();

		// Observer to track focus events
		app.AddObserver<On<TextInputFocused>>((trigger) =>
		{
			if (trigger.EntityId == textInputId)
			{
				focusGained = true;
			}
		});

		app.RunStartup();
		var world = app.GetWorld();

		// Set up delta time for flux interaction
		var deltaTime = world.GetResource<DeltaTime>();
		deltaTime.Seconds = 0.016f; // 60 FPS

		// Verify text input was created
		Assert.True(world.Has<TextInput>(textInputId));

		// Run layout calculation
		app.Update();

		// Simulate click on text input
		var center = GetWidgetCenter(world, textInputId);
		SimulatePointerClick(app, center);

		// Run one more update to process deferred commands and trigger observers
		app.Update();

		// Verify text input gained focus
		Assert.True(focusGained, "TextInput should gain focus when clicked");
	}

	[Fact]
	public void AllThemedWidgets_HaveRequiredInteractionComponents()
	{
		var app = new App();
		app.AddPlugin(new TinyEcsUiPlugin());
		app.AddPlugin(new TinyEcsUiWidgetsPlugin());
		app.AddResource(UiTheme.Dark());

		var widgetIds = new Dictionary<string, ulong>();

		// Create all widget types
		app.AddSystem((Commands commands, Res<UiTheme> theme) =>
		{
			widgetIds["button"] = ThemedWidgetBuilders.CreateButton(commands, theme.Value, "Button");
			widgetIds["slider"] = ThemedWidgetBuilders.CreateSlider(commands, theme.Value, 0f, 100f, 50f);
			widgetIds["checkbox"] = ThemedWidgetBuilders.CreateCheckbox(commands, theme.Value);
			widgetIds["toggle"] = ThemedWidgetBuilders.CreateToggle(commands, theme.Value);
			widgetIds["radio"] = ThemedWidgetBuilders.CreateRadioButton(commands, theme.Value, 1, 1);
			widgetIds["textinput"] = ThemedWidgetBuilders.CreateTextInput(commands, theme.Value);
			widgetIds["label"] = ThemedWidgetBuilders.CreateLabel(commands, theme.Value, "Label");
			widgetIds["panel"] = ThemedWidgetBuilders.CreatePanel(commands, theme.Value);
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();
		var world = app.GetWorld();

		// Verify all interactive widgets have required components
		var interactiveWidgets = new[] { "button", "slider", "checkbox", "toggle", "radio", "textinput" };

		foreach (var widgetName in interactiveWidgets)
		{
			var widgetId = widgetIds[widgetName];

			Assert.True(world.Has<Interactive>(widgetId),
				$"{widgetName} should have Interactive component");
			Assert.True(world.Has<InteractionState>(widgetId),
				$"{widgetName} should have InteractionState component");
			Assert.True(world.Has<FluxInteraction>(widgetId),
				$"{widgetName} should have FluxInteraction component");
			Assert.True(world.Has<PrevInteraction>(widgetId),
				$"{widgetName} should have PrevInteraction component");
			Assert.True(world.Has<FluxInteractionStopwatch>(widgetId),
				$"{widgetName} should have FluxInteractionStopwatch component");
		}
	}
}
