using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Example showing how to create UI elements using Bevy-style components.
/// </summary>
public static class BevyUiExample
{
	/// <summary>
	/// Creates a simple button-like UI element using Bevy-style composition.
	/// </summary>
	public static ulong CreateButton(Commands commands, string text)
	{
		// Spawn an entity with all the components that make up a button
		var buttonId = commands.Spawn()
			// UiNode defines layout properties
			.Insert(new UiNode
			{
				Width = FlexValue.Points(120f),
				Height = FlexValue.Points(40f),
				JustifyContent = Justify.Center,
				AlignItems = Align.Center,
				PaddingTop = FlexValue.Points(8f),
				PaddingRight = FlexValue.Points(16f),
				PaddingBottom = FlexValue.Points(8f),
				PaddingLeft = FlexValue.Points(16f)
			})

			// Visual appearance
			.Insert(BackgroundColor.FromRgba(70, 130, 180, 255))  // Steel blue
			.Insert(new BorderRadius(4f))

			// Make it interactive
			.Insert(new Interactive(focusable: true))

			// Get the entity ID
			.Id;

		// Create a text child
		commands.Spawn()
			.Insert(UiNode.Default())
			.Insert(new UiText(text))
			.Insert(new TextStyle(16f, new Vector4(1, 1, 1, 1)))  // White text
			.Insert(new Parent { Id = buttonId });  // Parent to the button

		return buttonId;
	}

	/// <summary>
	/// Creates a container with vertical layout (column).
	/// </summary>
	public static ulong CreateColumn(Commands commands)
	{
		return commands.Spawn()
			.Insert(new UiNode
			{
				FlexDirection = FlexDirection.Column,
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Stretch,
				Width = FlexValue.Percent(100f),
				PaddingTop = FlexValue.Points(16f),
				PaddingRight = FlexValue.Points(16f),
				PaddingBottom = FlexValue.Points(16f),
				PaddingLeft = FlexValue.Points(16f)
			})
			.Id;
	}

	/// <summary>
	/// Creates a container with horizontal layout (row).
	/// </summary>
	public static ulong CreateRow(Commands commands)
	{
		return commands.Spawn()
			.Insert(new UiNode
			{
				FlexDirection = FlexDirection.Row,
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Center,
				Width = FlexValue.Percent(100f)
			})
			.Id;
	}

	/// <summary>
	/// Creates a text label.
	/// </summary>
	public static ulong CreateLabel(Commands commands, string text, float fontSize = 16f)
	{
		return commands.Spawn()
			.Insert(UiNode.Default())
			.Insert(new UiText(text))
			.Insert(new TextStyle(fontSize, new Vector4(1, 1, 1, 1)))
			.Id;
	}

	/// <summary>
	/// Example: Create a simple UI hierarchy.
	/// </summary>
	public static void CreateExampleUi(Commands commands)
	{
		// Root container (column layout)
		var root = commands.Spawn()
			.Insert(new UiNode
			{
				FlexDirection = FlexDirection.Column,
				Width = FlexValue.Points(400f),
				Height = FlexValue.Points(300f),
				PaddingTop = FlexValue.Points(20f),
				PaddingRight = FlexValue.Points(20f),
				PaddingBottom = FlexValue.Points(20f),
				PaddingLeft = FlexValue.Points(20f)
			})
			.Insert(BackgroundColor.FromRgba(40, 40, 40, 255))  // Dark gray background
			.Id;

		// Title
		commands.Spawn()
			.Insert(new UiNode
			{
				MarginBottom = FlexValue.Points(16f)
			})
			.Insert(new UiText("My Application"))
			.Insert(new TextStyle(24f, new Vector4(1, 1, 1, 1)))
			.Insert(new Parent { Id = root });

		// Row of buttons
		var buttonRow = commands.Spawn()
			.Insert(new UiNode
			{
				FlexDirection = FlexDirection.Row,
				JustifyContent = Justify.SpaceAround,
				Width = FlexValue.Percent(100f),
				MarginBottom = FlexValue.Points(16f)
			})
			.Insert(new Parent { Id = root })
			.Id;

		// Create buttons as children of the row
		CreateButtonWithParent(commands, "Click Me", buttonRow);
		CreateButtonWithParent(commands, "Cancel", buttonRow);
		CreateButtonWithParent(commands, "Submit", buttonRow);
	}

	private static ulong CreateButtonWithParent(Commands commands, string text, ulong parentId)
	{
		var buttonId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Points(100f),
				Height = FlexValue.Points(35f),
				JustifyContent = Justify.Center,
				AlignItems = Align.Center
			})
			.Insert(BackgroundColor.FromRgba(70, 130, 180, 255))
			.Insert(new BorderRadius(4f))
			.Insert(new Interactive(focusable: true))
			.Insert(new Parent { Id = parentId })
			.Id;

		// Button text
		commands.Spawn()
			.Insert(UiNode.Default())
			.Insert(new UiText(text))
			.Insert(new TextStyle(14f, new Vector4(1, 1, 1, 1)))
			.Insert(new Parent { Id = buttonId });

		return buttonId;
	}
}
