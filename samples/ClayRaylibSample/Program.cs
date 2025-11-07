using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Clay;
using TinyEcs.UI.Clay.Widgets;
using Clay_cs;
using System.Text;

namespace ClayRaylibSample;

/// <summary>
/// Sample demonstrating Clay UI rendering with Raylib.
/// </summary>
public static class Program
{
	private const int WINDOW_WIDTH = 1280;
	private const int WINDOW_HEIGHT = 720;

	public static unsafe void Main()
	{
		// Initialize Raylib
		Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
		Raylib.InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "Clay UI + Raylib Sample");
		Raylib.SetTargetFPS(-1);

		// Create ECS world and app
		using var world = new World();
		var app = new App(world, ThreadingMode.Single); // Use single-threaded for Raylib

		// Add Clay UI plugin
		app.AddClayUi(new ClayUiOptions
		{
			LayoutDimensions = new Clay_Dimensions(WINDOW_WIDTH, WINDOW_HEIGHT),
			ArenaSize = 1024 * 1024,  // 1MB
			EnableDebugMode = true,
			EnableCulling = false,
			MeasureTextFunction = MeasureText,
			ErrorHandler = HandleClayError
		});

		// Add rendering plugin
		app.AddPlugin(new ClayRaylibRenderPlugin());

		// Add Raylib-specific global slider drag system
		// This handles mouse movement even outside slider bounds using Raylib
		app.AddRaylibSliderDragSystem();

		// Add global observer for radio button value changes
		app.AddObserver<On<RadioValueChanged>>((world, trigger) =>
		{
			var evt = trigger.Event;
			Console.WriteLine($"Radio changed - Group: {evt.GroupKey}, Value: {evt.Value}");
		});

		// Add global observer for slider value changes
		app.AddObserver<On<SliderValueChanged>>((world, trigger) =>
		{
			var evt = trigger.Event;
			Console.WriteLine($"Slider value: {evt.Value:F2}");
		});

		// Add global observer for checkbox value changes
		app.AddObserver<On<CheckboxValueChanged>>((world, trigger) =>
		{
			var evt = trigger.Event;
			Console.WriteLine($"Checkbox checked: {evt.Checked}");
		});

		// Add global observer for button clicks
		app.AddObserver<On<ButtonClicked>>((world, trigger) =>
		{
			Console.WriteLine($"Button clicked: Entity {trigger.EntityId}");
		});

		// Add global observer for text input value changes
		app.AddObserver<On<TextInputValueChanged>>((world, trigger) =>
		{
			var evt = trigger.Event;
			Console.WriteLine($"Text input changed: {evt.Text}");
		});

		// Add global observer for text input focus
		app.AddObserver<On<TextInputFocused>>((world, trigger) =>
		{
			Console.WriteLine($"Text input focused: Entity {trigger.EntityId}");
		});

		// Add global observer for text input blur
		app.AddObserver<On<TextInputBlurred>>((world, trigger) =>
		{
			Console.WriteLine($"Text input blurred: Entity {trigger.EntityId}");
		});

		// Create UI in startup
		app.AddSystem((Commands commands) => CreateUI(commands))
			.InStage(Stage.Startup)
			.Label("app:create-ui")
			.Build();

		// Run startup
		app.RunStartup();

		// Main loop
		while (!Raylib.WindowShouldClose())
		{
			// Update ECS (which will update Clay pointer state and run systems)
			app.Update();
		}

		Raylib.CloseWindow();
	}

	private static void CreateUI(Commands commands)
	{
		// Create root container
		var rootNode = ClayNode.Default;
		rootNode.Layout = new Clay_LayoutConfig
		{
			sizing = new Clay_Sizing(
				Clay_SizingAxis.Grow(),
				Clay_SizingAxis.Grow()
			),
			layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
			padding = Clay_Padding.All(32),
			childGap = 16,
			childAlignment = new Clay_ChildAlignment(
				Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
				Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP
			)
		};
		rootNode.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = new Clay_Color(20, 25, 30, 255)
		};
		rootNode.Text = new ClayText()
		{
			Text = "Clay UI with Raylib",
			Config = {
				fontSize = 48,
			}
		};

		var root = commands.SpawnClayElement(rootNode);

		// Create scrollable container with scroll configuration
		var scrollContainerNode = ClayNode.Default;
		scrollContainerNode.Layout = new Clay_LayoutConfig
		{
			sizing = new Clay_Sizing(
				Clay_SizingAxis.Fixed(600),
				Clay_SizingAxis.Fixed(400)
			),
			layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
			padding = Clay_Padding.All(16),
			childGap = 8
		};
		scrollContainerNode.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = new Clay_Color(40, 45, 50, 255)
		};
		scrollContainerNode.CornerRadius = Clay_CornerRadius.All(8);
		// Enable vertical scrolling with clip (childOffset is automatically managed by Clay)
		scrollContainerNode.Clip = new Clay_ClipElementConfig
		{
			horizontal = false,
			vertical = true
		};

		// Enable scrolling with Clay's scroll container
		var scrollContainer = commands.SpawnClayElement(scrollContainerNode);
		root.AddChild(scrollContainer);

		// // Create first nested scrollable container
		// var scroll1Node = ClayNode.Default;
		// scroll1Node.Layout = new Clay_LayoutConfig
		// {
		// 	sizing = new Clay_Sizing(
		// 		Clay_SizingAxis.Grow(),
		// 		Clay_SizingAxis.Fixed(200)
		// 	),
		// 	layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
		// 	padding = Clay_Padding.All(8),
		// 	childGap = 4
		// };
		// scroll1Node.Rectangle = new Clay_RectangleRenderData
		// {
		// 	backgroundColor = new Clay_Color(60, 65, 70, 255)
		// };
		// scroll1Node.CornerRadius = Clay_CornerRadius.All(4);
		// scroll1Node.Clip = new Clay_ClipElementConfig
		// {
		// 	horizontal = false,
		// 	vertical = true
		// };
		// scroll1Node.Text = new ClayText()
		// {
		// 	Text = "Scroll Container 1",
		// };

		// var scroll1 = commands.SpawnClayElement(scroll1Node);
		// scrollContainer.AddChild(scroll1);

		// scroll1.Observe((On<ClayPointerEvent> trigger) =>
		// {
		// 	var pointerEvent = trigger.Event;
		// 	if (pointerEvent.EventType == ClayPointerEventType.Click)
		// 	{
		// 		Console.WriteLine("parent clicked 1");
		// 	}
		// })
		// .Observe((On<ClayPointerEvent> trigger) =>
		// {
		// 	var pointerEvent = trigger.Event;
		// 	if (pointerEvent.EventType == ClayPointerEventType.Click)
		// 	{
		// 		Console.WriteLine("parent clicked 2");
		// 	}
		// });

		// // Create sliders using the widget extension
		// commands.CreateSlider(scroll1, "Volume", 0.5f, 0f, 1f);
		// commands.CreateSlider(scroll1, "Brightness", 0.75f, 0f, 1f);
		// commands.CreateSlider(scroll1, "Speed", 50f, 0f, 100f, step: 5f);

		// // Create checkboxes
		// commands.CreateCheckbox(scroll1, "Enable Sound", defaultChecked: true);
		// commands.CreateCheckbox(scroll1, "Enable Music", defaultChecked: false);
		// commands.CreateCheckbox(scroll1, "Enable Vibration", defaultChecked: true);
		// commands.CreateCheckbox(scroll1, "Disabled Option", defaultChecked: false, disabled: true);

		// // Create radio buttons for difficulty selection
		// commands.CreateRadioButton(scroll1, "difficulty", "easy", "Easy", defaultSelected: true);
		// commands.CreateRadioButton(scroll1, "difficulty", "normal", "Normal");
		// commands.CreateRadioButton(scroll1, "difficulty", "hard", "Hard");
		// commands.CreateRadioButton(scroll1, "difficulty", "expert", "Expert");

		// // Create radio buttons for graphics quality
		// commands.CreateRadioButton(scroll1, "graphics", "low", "Low Quality");
		// commands.CreateRadioButton(scroll1, "graphics", "medium", "Medium Quality", defaultSelected: true);
		// commands.CreateRadioButton(scroll1, "graphics", "high", "High Quality");
		// commands.CreateRadioButton(scroll1, "graphics", "ultra", "Ultra Quality");

		// // Create 20 buttons in first scrollable area
		// var colors = new[]
		// {
		// 	new Clay_Color(70, 130, 180, 255),
		// 	new Clay_Color(180, 70, 130, 255),
		// 	new Clay_Color(130, 180, 70, 255),
		// 	new Clay_Color(180, 130, 70, 255),
		// 	new Clay_Color(70, 180, 130, 255)
		// };

		// for (int i = 0; i < 20; i++)
		// {
		// 	var color = colors[i % colors.Length];
		// 	commands.CreateButton(scroll1, $"S1 Button {i + 1}", backgroundColor: color);
		// }

		// // Create second nested scrollable container
		// var scroll2Node = ClayNode.Default with
		// {
		// 	Layout = new Clay_LayoutConfig
		// 	{
		// 		sizing = new Clay_Sizing(
		// 			Clay_SizingAxis.Grow(),
		// 			Clay_SizingAxis.Fixed(200)
		// 		),
		// 		layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
		// 		padding = Clay_Padding.All(8),
		// 		childGap = 4
		// 	},
		// 	Rectangle = new Clay_RectangleRenderData
		// 	{
		// 		backgroundColor = new Clay_Color(60, 65, 70, 255)
		// 	},
		// 	CornerRadius = Clay_CornerRadius.All(4),
		// 	Clip = new Clay_ClipElementConfig
		// 	{
		// 		horizontal = false,
		// 		vertical = true
		// 	},
		// 	Text = new ClayText()
		// 	{
		// 		Text = "Scroll Container 2",
		// 	}
		// };

		// var scroll2 = commands.SpawnClayElement(scroll2Node);
		// scrollContainer.AddChild(scroll2);

		// // Create 20 buttons in second scrollable area
		// for (int i = 0; i < 20; i++)
		// {
		// 	var color = colors[i % colors.Length];
		// 	commands.CreateButton(scroll2, $"S2 Button {i + 1}", backgroundColor: color);
		// }

		// Create third container for new widgets demonstration
		var scroll3Node = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Grow(),
					Clay_SizingAxis.Fixed(300)
				),
				layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
				padding = Clay_Padding.All(8),
				childGap = 8
			},
			Rectangle = new Clay_RectangleRenderData
			{
				backgroundColor = new Clay_Color(60, 65, 70, 255)
			},
			CornerRadius = Clay_CornerRadius.All(4),
			Clip = new Clay_ClipElementConfig
			{
				horizontal = false,
				vertical = true
			},
			Text = new ClayText()
			{
				Text = "Widget Showcase",
			}
		};

		var scroll3 = commands.SpawnClayElement(scroll3Node);
		scrollContainer.AddChild(scroll3);

		// // Create a panel to group text input widgets
		// var inputPanel = commands.CreatePanel(
		// 	scroll3,
		// 	title: "User Input",
		// 	width: 550f,
		// 	height: 0f,
		// 	backgroundColor: new Clay_Color(50, 55, 60, 255),
		// 	padding: 12f,
		// 	cornerRadius: 8
		// );

		// // Create text input fields
		// commands.CreateTextInput(
		// 	inputPanel,
		// 	placeholder: "Username...",
		// 	width: 300f,
		// 	height: 40f
		// );

		// commands.CreateTextInput(
		// 	inputPanel,
		// 	placeholder: "Email address...",
		// 	width: 300f,
		// 	height: 40f
		// );

		// commands.CreateTextInput(
		// 	inputPanel,
		// 	placeholder: "Password (disabled)",
		// 	width: 300f,
		// 	height: 40f,
		// 	disabled: true
		// );

		// // Create a panel to group progress bars
		// var progressPanel = commands.CreatePanel(
		// 	scroll3,
		// 	title: "Progress Indicators",
		// 	width: 550f,
		// 	height: 0f,
		// 	backgroundColor: new Clay_Color(50, 55, 60, 255),
		// 	padding: 12f,
		// 	cornerRadius: 8
		// );

		// // Create progress bars with different states
		// commands.CreateProgressBar(
		// 	progressPanel,
		// 	initialValue: 25f,
		// 	min: 0f,
		// 	max: 100f,
		// 	width: 300f,
		// 	height: 28f,
		// 	showLabel: true,
		// 	fillColor: new Clay_Color(76, 175, 80, 255)
		// );

		// commands.CreateProgressBar(
		// 	progressPanel,
		// 	initialValue: 60f,
		// 	min: 0f,
		// 	max: 100f,
		// 	width: 300f,
		// 	height: 28f,
		// 	showLabel: true,
		// 	fillColor: new Clay_Color(33, 150, 243, 255)
		// );

		// commands.CreateProgressBar(
		// 	progressPanel,
		// 	initialValue: 85f,
		// 	min: 0f,
		// 	max: 100f,
		// 	width: 300f,
		// 	height: 28f,
		// 	showLabel: true,
		// 	fillColor: new Clay_Color(255, 152, 0, 255)
		// );

		// commands.CreateProgressBar(
		// 	progressPanel,
		// 	initialValue: 100f,
		// 	min: 0f,
		// 	max: 100f,
		// 	width: 300f,
		// 	height: 28f,
		// 	showLabel: true,
		// 	fillColor: new Clay_Color(244, 67, 54, 255)
		// );

		// Create a panel for scrollable list demonstration
		// var scrollbarPanel = commands.CreatePanel(
		// 	scroll3,
		// 	title: "Scrollable List Example",
		// 	width: 550f,
		// 	height: 0f,
		// 	backgroundColor: new Clay_Color(50, 55, 60, 255),
		// 	padding: 12f,
		// 	cornerRadius: 8
		// );

		// // Container with scrollbar - fixed height viewport
		// var scrollableContainerNode = ClayNode.Default with
		// {
		// 	Layout = new Clay_LayoutConfig
		// 	{
		// 		sizing = new Clay_Sizing(
		// 			Clay_SizingAxis.Fixed(500),
		// 			Clay_SizingAxis.Fixed(250)
		// 		),
		// 		layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
		// 		childGap = 0
		// 	}
		// };

		// var scrollableContainer = commands.SpawnClayElement(scrollableContainerNode);
		// scrollbarPanel.AddChild(scrollableContainer);

		// Content area(will be clipped and scrolled)
		// var contentAreaNode = ClayNode.Default with
		// {
		// 	Layout = new Clay_LayoutConfig
		// 	{
		// 		sizing = new Clay_Sizing(
		// 			Clay_SizingAxis.Grow(),
		// 			Clay_SizingAxis.Grow()
		// 		),
		// 		layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
		// 		childGap = 8,
		// 		padding = Clay_Padding.All(8)
		// 	},
		// 	Rectangle = new Clay_RectangleRenderData
		// 	{
		// 		backgroundColor = new Clay_Color(35, 38, 42, 255)
		// 	},
		// 	CornerRadius = Clay_CornerRadius.All(4),
		// 	Clip = new Clay_ClipElementConfig
		// 	{
		// 		horizontal = true,  // Enable horizontal clipping
		// 		vertical = true,
		// 		childOffset = new Clay_Vector2 { x = 0f, y = 0f }
		// 	}
		// };

		// var contentArea = commands.SpawnClayElement(contentAreaNode);
		// scrollableContainer.AddChild(contentArea);

		// // Add ClayScrollContainer component to enable scrolling
		// commands.Entity(contentArea.Id).Insert(ClayScrollContainer.Default);

		// // Add list items (20 items, each 40px tall = 800px total content)
		// // Make items 800px wide to exceed viewport width and trigger horizontal scrollbar
		// for (int i = 0; i < 20; i++)
		// {
		// 	var itemNode = ClayNode.Default with
		// 	{
		// 		Layout = new Clay_LayoutConfig
		// 		{
		// 			sizing = new Clay_Sizing(
		// 				Clay_SizingAxis.Fixed(800),  // Fixed width to trigger horizontal scroll
		// 				Clay_SizingAxis.Fixed(40)
		// 			),
		// 			layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
		// 			childAlignment = new Clay_ChildAlignment(
		// 				Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
		// 				Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
		// 			),
		// 			padding = Clay_Padding.All(8)
		// 		},
		// 		Rectangle = new Clay_RectangleRenderData
		// 		{
		// 			backgroundColor = i % 2 == 0
		// 				? new Clay_Color(45, 50, 55, 255)
		// 				: new Clay_Color(40, 44, 48, 255)
		// 		},
		// 		CornerRadius = Clay_CornerRadius.All(4)
		// 	};

		// 	var item = commands.SpawnClayElement(itemNode);
		// 	contentArea.AddChild(item);

		// 	// Item text
		// 	var itemTextNode = ClayNode.Default with
		// 	{
		// 		Layout = new Clay_LayoutConfig
		// 		{
		// 			sizing = new Clay_Sizing(
		// 				Clay_SizingAxis.Grow(),
		// 				Clay_SizingAxis.Grow()
		// 			)
		// 		},
		// 		Text = new ClayText
		// 		{
		// 			Text = $"List Item #{i + 1} - This is some very long sample content that extends beyond the viewport width to demonstrate horizontal scrolling",
		// 			Config = new Clay_TextElementConfig
		// 			{
		// 				fontSize = 16,
		// 				textColor = new Clay_Color(220, 220, 220, 255)
		// 			}
		// 		}
		// 	};

		// 	var itemText = commands.SpawnClayElement(itemTextNode);
		// 	item.AddChild(itemText);
		// }

		// // Vertical scrollbar
		// // Content: 20 items × 40px = 800px, 19 gaps × 8px = 152px, padding 16px = 968px total
		// var verticalScrollbar = commands.CreateVerticalScrollbar(
		// 	scrollableContainer,
		// 	contentAreaEntityId: contentArea.Id,
		// 	contentSize: 968f,
		// 	visibleSize: 250f,
		// 	initialScroll: 0.0f
		// );

		// // Horizontal scrollbar
		// // Content: 800px item width + 16px padding = 816px total
		// // Visible: 500px container width - 12px vertical scrollbar = 488px
		// var horizontalScrollbar = commands.CreateHorizontalScrollbar(
		// 	scrollableContainer,
		// 	contentAreaEntityId: contentArea.Id,
		// 	contentSize: 816f,
		// 	visibleSize: 488f,
		// 	initialScroll: 0.0f
		// );

		// Create a nested panel example
		var nestedPanel = commands.CreatePanel(
			scroll3,
			title: "Settings",
			width: 550f,
			height: 0f,
			backgroundColor: new Clay_Color(50, 55, 60, 255),
			padding: 12f,
			cornerRadius: 8
		);

		// Add some controls inside the nested panel
		commands.CreateCheckbox(nestedPanel, "Enable Notifications", defaultChecked: true);
		commands.CreateCheckbox(nestedPanel, "Auto-save Progress", defaultChecked: true);
		commands.CreateSlider(nestedPanel, "Music Volume", 0.7f, 0f, 1f);

		// Add dropdown widget
		var qualityOptions = new[] { "Low", "Medium", "High", "Ultra" };
		commands.CreateDropdown(nestedPanel, "Quality", qualityOptions, defaultIndex: 2);

		commands.CreateButton(nestedPanel, "Save Settings", width: 150f, height: 40f);
	}

	/// <summary>
	/// Measure text dimensions using Raylib's text measurement.
	/// This function is called by Clay during layout calculation.
	/// </summary>
	private static unsafe Clay_Dimensions MeasureText(Clay_StringSlice text, Clay_TextElementConfig* config, void* userData)
	{
		// Extract text string from Clay string slice
		var textPtr = text.chars;
		var textLength = text.length;

		var textString = Encoding.UTF8.GetString((byte*)textPtr, textLength);

		// var font = Raylib.GetFontDefault();
		// var scaleFactor = config->fontSize / (float)font.BaseSize;

		// float maxTextWidth = 0.0f;
		// float lineTextWidth = 0;
		// int maxLineCharCount = 0;
		// int lineCharCount = 0;

		// for (int i = 0; i < text.length; ++i, lineCharCount++)
		// {
		// 	if (text.chars[i] == '\n')
		// 	{
		// 		maxTextWidth = Math.Max(maxTextWidth, lineTextWidth);
		// 		maxLineCharCount = Math.Max(maxLineCharCount, lineCharCount);
		// 		lineTextWidth = 0;
		// 		lineCharCount = 0;
		// 		continue;
		// 	}
		// 	int index = text.chars[i] - 32;
		// 	if (font.Glyphs[index].AdvanceX != 0) lineTextWidth += font.Glyphs[index].AdvanceX;
		// 	else lineTextWidth += (font.Recs[index].Width + font.Glyphs[index].OffsetX);
		// }

		// maxTextWidth = Math.Max(maxTextWidth, lineTextWidth);
		// maxLineCharCount = Math.Max(maxLineCharCount, lineCharCount);

		// var dim = new Clay_Dimensions();
		// dim.width = maxTextWidth * scaleFactor + (lineCharCount * config->letterSpacing);
		// dim.height = config->fontSize;


		// Measure text using Raylib
		var textSize = Raylib.MeasureTextEx(
			Raylib.GetFontDefault(),
			textString,
			config->fontSize,
			config->letterSpacing
		);

		return new Clay_Dimensions(textSize.X, textSize.Y);
	}

	/// <summary>
	/// Handle Clay errors by logging to console.
	/// This function is called when Clay encounters errors like capacity exceeded or invalid state.
	/// </summary>
	private static unsafe void HandleClayError(Clay_ErrorData errorData)
	{
		// Convert error text from Clay string slice to C# string
		var errorTextPtr = errorData.errorText.chars;
		var errorTextLength = errorData.errorText.length;
		var errorText = new string((sbyte*)errorTextPtr, 0, errorTextLength, System.Text.Encoding.UTF8);

		// Get error type name
		var errorType = errorData.errorType switch
		{
			Clay_ErrorType.CLAY_ERROR_TYPE_TEXT_MEASUREMENT_FUNCTION_NOT_PROVIDED => "TEXT_MEASUREMENT_FUNCTION_NOT_PROVIDED",
			Clay_ErrorType.CLAY_ERROR_TYPE_ARENA_CAPACITY_EXCEEDED => "ARENA_CAPACITY_EXCEEDED",
			Clay_ErrorType.CLAY_ERROR_TYPE_ELEMENTS_CAPACITY_EXCEEDED => "ELEMENTS_CAPACITY_EXCEEDED",
			Clay_ErrorType.CLAY_ERROR_TYPE_TEXT_MEASUREMENT_CAPACITY_EXCEEDED => "TEXT_MEASUREMENT_CAPACITY_EXCEEDED",
			Clay_ErrorType.CLAY_ERROR_TYPE_DUPLICATE_ID => "DUPLICATE_ID",
			Clay_ErrorType.CLAY_ERROR_TYPE_FLOATING_CONTAINER_PARENT_NOT_FOUND => "FLOATING_CONTAINER_PARENT_NOT_FOUND",
			Clay_ErrorType.CLAY_ERROR_TYPE_PERCENTAGE_OVER_1 => "PERCENTAGE_OVER_1",
			Clay_ErrorType.CLAY_ERROR_TYPE_INTERNAL_ERROR => "INTERNAL_ERROR",
			Clay_ErrorType.CLAY_ERROR_TYPE_UNBALANCED_OPEN_CLOSE => "UNBALANCED_OPEN_CLOSE",
			_ => "UNKNOWN"
		};

		// Log error to console with color
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine($"[Clay Error] {errorType}: {errorText}");
		Console.ResetColor();
	}


}

/// <summary>
/// Extension methods for adding Raylib-specific Clay UI systems.
/// </summary>
public static class RaylibClayExtensions
{
	/// <summary>
	/// Adds global slider drag tracking system that uses Raylib for mouse input.
	/// This allows sliders to continue updating even when the cursor leaves the slider bounds.
	/// </summary>
	public static App AddRaylibSliderDragSystem(this App app)
	{
		app.AddSystem((Commands commands, Query<Data<SliderState>> stateQuery, Query<Data<ClayComputedLayout>> layoutQuery) =>
		{
			// Check if any slider is being dragged
			foreach (var (entityId, statePtr) in stateQuery)
			{
				var state = statePtr.Ref;

				if (!state.IsDragging)
					continue;

				// Check if mouse button is still down
				if (!Raylib.IsMouseButtonDown(MouseButton.Left))
				{
					// Mouse released - stop dragging
					state.IsDragging = false;
					commands.Entity(entityId.Ref).Insert(state);
					continue;
				}

				// Get global mouse position from Raylib
				var mousePos = Raylib.GetMousePosition();

				// Get layout information for the slider components
				if (!layoutQuery.Contains(state.TrackEntityId) || !layoutQuery.Contains(state.ThumbEntityId))
					continue;

				var (_, trackLayoutPtr) = layoutQuery.Get(state.TrackEntityId);
				var (_, thumbLayoutPtr) = layoutQuery.Get(state.ThumbEntityId);
				var trackLayout = trackLayoutPtr.Ref;
				var thumbLayout = thumbLayoutPtr.Ref;

				// Calculate slider value using simple Lua-style calculation
				var trackWidth = trackLayout.Width;
				var trackLocalX = mousePos.X - trackLayout.X;
				var normalized = Math.Clamp(trackLocalX / trackWidth, 0f, 1f);
				var newValue = state.Min + normalized * (state.Max - state.Min);

				// Apply step if specified
				if (state.Step > 0)
				{
					newValue = MathF.Round(newValue / state.Step) * state.Step;
				}

				// Update value if it changed
				var clampedValue = Math.Clamp(newValue, state.Min, state.Max);
				if (Math.Abs(clampedValue - state.Value) > 0.0001f)
				{
					state.Value = clampedValue;
					commands.Entity(entityId.Ref).Insert(state);

					// Update visuals
					var label = state.Label;
					var value = state.Value;
					var min = state.Min;
					var max = state.Max;

					commands.Entity(state.LabelEntityId).Insert(new SliderLabelUpdate { Text = $"{label}: {value:F2}" });
					float normalizedFill = (value - min) / (max - min);
					commands.Entity(state.FillEntityId).Insert(new SliderFillUpdate { NormalizedValue = normalizedFill });
				}
			}
		})
		.InStage(Stage.First)
		.Label("raylib:slider-drag")
		.After("clay:reset-input-state")
		.SingleThreaded()
		.Build();

		return app;
	}
}

/// <summary>
/// Plugin for rendering Clay UI with Raylib.
/// </summary>
public struct ClayRaylibRenderPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddSystem((Res<ClayUiState> state) =>
		{
			state.Value.LayoutDimensions.width = Raylib.GetRenderWidth();
			state.Value.LayoutDimensions.height = Raylib.GetRenderHeight();
		})
		.InStage(Stage.First)
		.SingleThreaded()
		.Build();

		// System to update Clay pointer state from Raylib input
		app.AddSystem((ResMut<ClayPointerState> pointer) => UpdatePointerInput(pointer))
			.InStage(Stage.First)
			.Label("raylib:update-pointer-input")
			.SingleThreaded()
			.Build();

		// System to update Clay text input state from Raylib keyboard input
		app.AddSystem((ResMut<ClayTextInputState> textInput) => UpdateTextInput(textInput))
			.InStage(Stage.First)
			.Label("raylib:update-text-input")
			.SingleThreaded()
			.Build();

		// System to begin Raylib drawing
		app.AddSystem((World _) =>
		{
			Raylib.BeginDrawing();
			Raylib.ClearBackground(Raylib_cs.Color.Black);
		})
		.InStage(Stage.PostUpdate)
		.Label("raylib:begin-draw")
		.After("clay:interaction")
		.SingleThreaded()
		.Build();

		// System to render Clay UI
		app.AddSystem((Res<ClayUiState> state, Local<Stack<Rectangle>> scissorStack) => RenderClayUI(state, scissorStack))
			.InStage(Stage.PostUpdate)
			.Label("raylib:render-clay")
			.After("raylib:begin-draw")
			.SingleThreaded()
			.Build();

		// System to end Raylib drawing
		app.AddSystem((World _) =>
		{
			// Draw FPS
			Raylib.DrawFPS(10, 10);
			Raylib.EndDrawing();
		})
		.InStage(Stage.PostUpdate)
		.Label("raylib:end-draw")
		.After("raylib:render-clay")
		.SingleThreaded()
		.Build();
	}

	private static void UpdatePointerInput(ResMut<ClayPointerState> pointer)
	{
		var mousePos = Raylib.GetMousePosition();
		pointer.Value.Position = new Vector2(mousePos.X, mousePos.Y);

		var wasDown = pointer.Value.PrimaryDown;
		var isDown = Raylib.IsMouseButtonDown(MouseButton.Left);

		pointer.Value.PrimaryDown = isDown;
		pointer.Value.PrimaryPressed = isDown && !wasDown;
		pointer.Value.PrimaryReleased = !isDown && wasDown;

		var mouseWheel = Raylib.GetMouseWheelMove();
		if (mouseWheel != 0)
		{
			// Check if Shift is held - if so, scroll horizontally
			bool shiftHeld = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);

			if (shiftHeld)
			{
				// Shift+Wheel = horizontal scroll (X component)
				pointer.Value.AddScroll(new Vector2(mouseWheel * 20f, 0f));
			}
			else
			{
				// Normal wheel = vertical scroll (Y component)
				pointer.Value.AddScroll(new Vector2(0f, mouseWheel * 20f));
			}
		}

		pointer.Value.DeltaTime = Raylib.GetFrameTime();
	}

	private static void UpdateTextInput(ResMut<ClayTextInputState> textInput)
	{
		// Inject typed characters
		int key = Raylib.GetCharPressed();
		while (key > 0)
		{
			textInput.Value.AddChar((char)key);
			key = Raylib.GetCharPressed();
		}

		// Handle special keys
		if (Raylib.IsKeyPressed(KeyboardKey.Backspace) || Raylib.IsKeyPressedRepeat(KeyboardKey.Backspace))
			textInput.Value.BackspacePressed = true;

		if (Raylib.IsKeyPressed(KeyboardKey.Delete))
			textInput.Value.DeletePressed = true;

		if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter))
			textInput.Value.EnterPressed = true;

		if (Raylib.IsKeyPressed(KeyboardKey.Escape))
			textInput.Value.EscapePressed = true;

		if (Raylib.IsKeyPressed(KeyboardKey.Left))
			textInput.Value.LeftPressed = true;

		if (Raylib.IsKeyPressed(KeyboardKey.Right))
			textInput.Value.RightPressed = true;

		if (Raylib.IsKeyPressed(KeyboardKey.Home))
			textInput.Value.HomePressed = true;

		if (Raylib.IsKeyPressed(KeyboardKey.End))
			textInput.Value.EndPressed = true;

		// Handle keyboard shortcuts (Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X)
		bool ctrlDown = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);

		if (ctrlDown && Raylib.IsKeyPressed(KeyboardKey.A))
			textInput.Value.SelectAllPressed = true;

		if (ctrlDown && Raylib.IsKeyPressed(KeyboardKey.C))
			textInput.Value.CopyPressed = true;

		if (ctrlDown && Raylib.IsKeyPressed(KeyboardKey.V))
			textInput.Value.PastePressed = true;

		if (ctrlDown && Raylib.IsKeyPressed(KeyboardKey.X))
			textInput.Value.CutPressed = true;
	}

	private static unsafe void RenderClayUI(Res<ClayUiState> state, Local<Stack<Rectangle>> scissorStack)
	{
		var commands = state.Value.RenderCommands;

		// Clear scissor stack at the start of each frame
		scissorStack.Value!.Clear();

		foreach (ref readonly var cmd in commands)
		{
			// Manual culling: Check if command is off-screen
			// IMPORTANT: Only cull visual commands (RECTANGLE, BORDER, TEXT)!
			// Always process SCISSOR_START/END to maintain Begin/End pairing and stack integrity.
			bool isOffScreen = cmd.boundingBox.x + cmd.boundingBox.width < 0 ||
							   cmd.boundingBox.y + cmd.boundingBox.height < 0;

			switch (cmd.commandType)
			{
				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
					// Cull off-screen visual commands for performance
					if (isOffScreen) break;
					RenderRectangle(cmd);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER:
					// Cull off-screen visual commands for performance
					if (isOffScreen) break;
					RenderBorder(cmd);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
					// Cull off-screen visual commands for performance
					if (isOffScreen) break;
					RenderText(cmd);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
					// Start scissor test (clipping)
					var scissorBox = cmd.boundingBox;

					// Create scissor rectangle from Clay's bounding box
					var newScissor = new Rectangle(
						scissorBox.x,
						scissorBox.y,
						scissorBox.width,
						scissorBox.height
					);

					// If there's already a scissor region active, intersect with it
					if (scissorStack.Value.Count > 0)
					{
						var currentScissor = scissorStack.Value.Peek();
						newScissor = IntersectRectangles(currentScissor, newScissor);
					}

					// Clamp scissor to screen bounds and ensure positive dimensions
					var screenWidth = Raylib.GetScreenWidth();
					var screenHeight = Raylib.GetScreenHeight();
					newScissor.X = Math.Clamp(newScissor.X, 0, screenWidth);
					newScissor.Y = Math.Clamp(newScissor.Y, 0, screenHeight);
					newScissor.Width = Math.Max(0, Math.Min(newScissor.Width, screenWidth - newScissor.X));
					newScissor.Height = Math.Max(0, Math.Min(newScissor.Height, screenHeight - newScissor.Y));

					// Push the new scissor region onto the stack
					scissorStack.Value.Push(newScissor);

					// Always apply scissor mode (even for zero dimensions to maintain Begin/End pairing)
					Raylib.BeginScissorMode(
						(int)newScissor.X,
						(int)newScissor.Y,
						(int)newScissor.Width,
						(int)newScissor.Height
					);
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:

					// End current scissor mode
					Raylib.EndScissorMode();

					// Pop the current scissor region
					if (scissorStack.Value.Count > 0)
					{
						var popped = scissorStack.Value.Pop();
					}

					// If there's a parent scissor region, restore it
					if (scissorStack.Value.Count > 0)
					{
						var parentScissor = scissorStack.Value.Peek();
						Raylib.BeginScissorMode(
							(int)parentScissor.X,
							(int)parentScissor.Y,
							(int)parentScissor.Width,
							(int)parentScissor.Height
						);
					}
					break;

				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
				case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_CUSTOM:
					// Not implemented in this sample
					break;
			}
		}
	}

	/// <summary>
	/// Calculate the intersection of two rectangles for nested scissor regions.
	/// </summary>
	private static Rectangle IntersectRectangles(Rectangle a, Rectangle b)
	{
		var x1 = Math.Max(a.X, b.X);
		var y1 = Math.Max(a.Y, b.Y);
		var x2 = Math.Min(a.X + a.Width, b.X + b.Width);
		var y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

		// If rectangles don't intersect, return empty rectangle
		if (x2 <= x1 || y2 <= y1)
		{
			return new Rectangle(0, 0, 0, 0);
		}

		return new Rectangle(x1, y1, x2 - x1, y2 - y1);
	}

	private static unsafe void RenderRectangle(Clay_RenderCommand cmd)
	{
		var bounds = cmd.boundingBox;
		var rect = cmd.renderData.rectangle;
		var clayColor = rect.backgroundColor;
		var color = new Raylib_cs.Color(
			(byte)Math.Clamp(clayColor.r, 0, 255),
			(byte)Math.Clamp(clayColor.g, 0, 255),
			(byte)Math.Clamp(clayColor.b, 0, 255),
			(byte)Math.Clamp(clayColor.a, 0, 255)
		);

		// Check if we have corner radius
		if (cmd.renderData.rectangle.cornerRadius.topLeft > 0)
		{
			var radius = cmd.renderData.rectangle.cornerRadius.topLeft;
			Raylib.DrawRectangleRounded(
				new Rectangle(bounds.x, bounds.y, bounds.width, bounds.height),
				radius / Math.Min(bounds.width, bounds.height),
				8,  // segments
				color
			);
		}
		else
		{
			Raylib.DrawRectangle(
				(int)bounds.x,
				(int)bounds.y,
				(int)bounds.width,
				(int)bounds.height,
				color
			);
		}
	}

	private static unsafe void RenderBorder(Clay_RenderCommand cmd)
	{
		var bounds = cmd.boundingBox;
		var border = cmd.renderData.border;
		var clayColor = border.color;
		var color = new Raylib_cs.Color(
			(byte)Math.Clamp(clayColor.r, 0, 255),
			(byte)Math.Clamp(clayColor.g, 0, 255),
			(byte)Math.Clamp(clayColor.b, 0, 255),
			(byte)Math.Clamp(clayColor.a, 0, 255)
		);

		// Check if we have corner radius - use rounded lines if so
		if (border.cornerRadius.topLeft > 0)
		{
			var radius = border.cornerRadius.topLeft;
			// Use the maximum border width for the line thickness
			var thickness = Math.Max(Math.Max(border.width.left, border.width.right),
									  Math.Max(border.width.top, border.width.bottom));

			// DrawRectangleRoundedLinesEx: Rectangle, roundness, segments, line thickness, color
			Raylib.DrawRectangleRoundedLinesEx(
				new Rectangle(bounds.x, bounds.y, bounds.width, bounds.height),
				radius / Math.Min(bounds.width, bounds.height),
				8,  // segments
				thickness,
				color
			);
		}
		else
		{
			// Draw border rectangles for each side (non-rounded)
			if (border.width.left > 0)
			{
				Raylib.DrawRectangle(
					(int)bounds.x,
					(int)bounds.y,
					(int)border.width.left,
					(int)bounds.height,
					color
				);
			}

			if (border.width.right > 0)
			{
				Raylib.DrawRectangle(
					(int)(bounds.x + bounds.width - border.width.right),
					(int)bounds.y,
					(int)border.width.right,
					(int)bounds.height,
					color
				);
			}

			if (border.width.top > 0)
			{
				Raylib.DrawRectangle(
					(int)bounds.x,
					(int)bounds.y,
					(int)bounds.width,
					(int)border.width.top,
					color
				);
			}

			if (border.width.bottom > 0)
			{
				Raylib.DrawRectangle(
					(int)bounds.x,
					(int)(bounds.y + bounds.height - border.width.bottom),
					(int)bounds.width,
					(int)border.width.bottom,
					color
				);
			}
		}
	}

	private static unsafe void RenderText(Clay_RenderCommand cmd)
	{
		var bounds = cmd.boundingBox;
		var textData = cmd.renderData.text;
		var clayColor = textData.textColor;
		var color = new Raylib_cs.Color(
			(byte)Math.Clamp(clayColor.r, 0, 255),
			(byte)Math.Clamp(clayColor.g, 0, 255),
			(byte)Math.Clamp(clayColor.b, 0, 255),
			(byte)Math.Clamp(clayColor.a, 0, 255)
		);

		// Extract text string from Clay string slice
		var textPtr = textData.stringContents.chars;
		var textLength = textData.stringContents.length;
		var text = Encoding.UTF8.GetString((byte*)textPtr, textLength);

		// Clay provides the bounding box already positioned correctly based on alignment
		// Just draw the text at the provided position
		Raylib.DrawTextEx(
			Raylib.GetFontDefault(),
			text,
			new Vector2(bounds.x, bounds.y),
			textData.fontSize,
			textData.letterSpacing,
			color
		);
	}
}
