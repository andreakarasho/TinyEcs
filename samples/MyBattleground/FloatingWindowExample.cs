using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;

namespace MyBattleground.Examples;

/// <summary>
/// Example demonstrating FloatingWindowWidget usage with various window types.
/// </summary>
public static class FloatingWindowExample
{
	public static void Run()
	{
		Console.WriteLine("=== Floating Window Widget Example ===\n");

		using var world = new World();
		var app = new App(world);

		app.AddClayUi(new ClayUiOptions
		{
			LayoutDimensions = new Clay_Dimensions(1280f, 720f),
			ArenaSize = 512 * 1024,
			EnableDebugMode = false
		});

		app.AddSystem((Commands commands) =>
		{
			// Create main application window
			var mainWindow = FloatingWindowWidget.Create(
				commands,
				ClayFloatingWindowStyle.Default,
				"Main Application",
				new Vector2(100f, 100f));

			// Add content to main window - find the content area (3rd child)
			var contentId = mainWindow.Id;

			// Add some labels inside the window
			LabelWidget.CreateHeading(commands, "Welcome!", level: 1, parent: contentId);

			SeparatorWidget.CreateHorizontal(commands, parent: contentId);

			LabelWidget.Create(commands, ClayLabelStyle.Body,
				"This is a floating window with draggable title bar.",
				parent: contentId);

			CheckboxWidget.Create(commands, ClayCheckboxStyle.Default,
				initialChecked: true,
				label: "Enable notifications",
				parent: contentId);

			SliderWidget.CreatePercent(commands, ClaySliderStyle.Default,
				initialPercent: 50f,
				parent: contentId);

			// Create a tool palette window
			var toolWindow = FloatingWindowWidget.CreateTool(
				commands,
				"Tools",
				new Vector2(600f, 100f));

			LabelWidget.Create(commands, ClayLabelStyle.Caption,
				"Tool Palette",
				parent: toolWindow.Id);

			for (int i = 0; i < 5; i++)
			{
				ButtonWidget.Create(commands,
					ClayButtonStyle.Default with { Size = new Vector2(200f, 36f) },
					$"Tool {i + 1}",
					toolWindow.Id);
			}

			// Create a dialog window
			var dialog = FloatingWindowWidget.CreateDialog(
				commands,
				"Confirmation",
				new Vector2(400f, 300f));

			LabelWidget.Create(commands, ClayLabelStyle.Body,
				"Are you sure you want to continue?",
				parent: dialog.Id);

			SeparatorWidget.CreateSpacer(commands, 20f, parent: dialog.Id);

			// Button row using a container
			var buttonRow = PanelWidget.CreateContainer(
				commands,
				new Vector2(250f, 50f),
				parent: dialog.Id,
				padding: 0,
				gap: 12,
				background: null);

			// Modify button row to use horizontal layout
			var buttonRowNode = new UiNode
			{
				Declaration = new Clay_ElementDeclaration
				{
					layout = new Clay_LayoutConfig
					{
						sizing = new Clay_Sizing(
							Clay_SizingAxis.Grow(),
							Clay_SizingAxis.Fixed(50f)),
						childGap = 12,
						layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
						childAlignment = new Clay_ChildAlignment(
							Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
							Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
					}
				}
			};
			buttonRow.Insert(buttonRowNode);

			ButtonWidget.Create(commands,
				ClayButtonStyle.Default with
				{
					Size = new Vector2(100f, 40f),
					Background = new Clay_Color(34, 197, 94, 255)
				},
				"Yes",
				buttonRow.Id);

			ButtonWidget.Create(commands,
				ClayButtonStyle.Default with
				{
					Size = new Vector2(100f, 40f),
					Background = new Clay_Color(239, 68, 68, 255)
				},
				"No",
				buttonRow.Id);

			// Create a panel window for displaying information
			var infoPanel = FloatingWindowWidget.CreatePanel(
				commands,
				"Information",
				new Vector2(850f, 150f));

			LabelWidget.Create(commands, ClayLabelStyle.Heading3,
				"System Status",
				parent: infoPanel.Id);

			LabelWidget.Create(commands, ClayLabelStyle.Body,
				"All systems operational",
				parent: infoPanel.Id);

			LabelWidget.Create(commands, ClayLabelStyle.Caption,
				"Last updated: 2025-10-22",
				parent: infoPanel.Id);

			// Add a scroll container inside a window
			var editorWindow = FloatingWindowWidget.Create(
				commands,
				ClayFloatingWindowStyle.Default with
				{
					InitialSize = new Vector2(500f, 400f),
					TitleBarColor = new Clay_Color(88, 28, 135, 255)
				},
				"Editor",
				new Vector2(200f, 400f));

			var scrollContainer = ScrollContainerWidget.CreateVertical(
				commands,
				new Vector2(460f, 300f),
				parent: editorWindow.Id);

			// Add lots of content to demonstrate scrolling
			for (int i = 0; i < 20; i++)
			{
				LabelWidget.Create(commands, ClayLabelStyle.Body,
					$"Line {i + 1}: This is some content that can be scrolled.",
					parent: scrollContainer.Id);
			}
		})
		.InStage(Stage.Startup)
		.Label("ui:spawn-windows")
		.Build();

		// Log render commands
		app.AddSystem((Res<ClayUiState> uiState) =>
		{
			var commands = uiState.Value.RenderCommands;
			Console.WriteLine($"\n[RenderCommands] Total count: {commands.Length}");
			Console.WriteLine($"Windows created successfully with {commands.Length} render elements\n");
		})
		.InStage(Stage.Update)
		.Label("ui:log-windows")
		.After("ui:clay:layout")
		.Build();

		app.RunStartup();
		app.Update(); // Run one frame to layout everything

		Console.WriteLine("Floating window example complete!");
		Console.WriteLine("\nCreated windows:");
		Console.WriteLine("  - Main Application (default style, draggable, resizable)");
		Console.WriteLine("  - Tools (tool style, compact)");
		Console.WriteLine("  - Confirmation (dialog style, no min/max)");
		Console.WriteLine("  - Information (panel style, no controls)");
		Console.WriteLine("  - Editor (custom color with scroll container)\n");
	}
}
