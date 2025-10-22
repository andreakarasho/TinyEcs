using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;
using Xunit;

namespace TinyEcs.Tests;

public class UiFloatingWindowDragTest
{
	[Fact]
	public void FloatingWindow_UpdatesNodeOffsetWhenDragging()
	{
		var app = new App();
		app.AddClayUi(new ClayUiOptions
		{
			LayoutDimensions = new Clay_Dimensions(800f, 600f),
			ArenaSize = 256 * 1024,
			EnableDebugMode = false,
			UseEntityHierarchy = true,
			AutoCreatePointerState = true,
			AutoRegisterDefaultSystems = false  // Don't register default systems
		});

		// Don't add UiWidgetsPlugin - we'll manually test the logic

		// Create a floating window at position (100, 100)
		var initialPos = new Vector2(100f, 100f);
		ulong windowId = 0;

		app.AddSystem((Commands commands) =>
		{
			var window = FloatingWindowWidget.Create(
				commands,
				ClayFloatingWindowStyle.Default with
				{
					InitialSize = new Vector2(300f, 200f),
					TitleBarColor = new Clay_Color(80, 80, 200, 255)
				},
				"Test Window",
				initialPos);

			windowId = window.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();
		Console.WriteLine($"Window ID: {windowId}");

		var world = app.GetWorld();

		// Verify initial state
		{
			ref var state = ref world.Get<FloatingWindowState>(windowId);
			ref var node = ref world.Get<UiNode>(windowId);

			Console.WriteLine($"Initial - State.Position: {state.Position}");
			Console.WriteLine($"Initial - Node.floating.offset: ({node.Declaration.floating.offset.x}, {node.Declaration.floating.offset.y})");

			Assert.Equal(100f, state.Position.X);
			Assert.Equal(100f, state.Position.Y);
			Assert.Equal(100f, node.Declaration.floating.offset.x);
			Assert.Equal(100f, node.Declaration.floating.offset.y);
			Assert.False(state.IsDragging);
		}

		// Simulate starting a drag at pointer position (150, 110)
		{
			ref var state = ref world.Get<FloatingWindowState>(windowId);
			var pointerPos = new Vector2(150f, 110f);

			// Start dragging
			state.IsDragging = true;
			state.DragOffset = pointerPos - state.Position;  // (150, 110) - (100, 100) = (50, 10)

			Console.WriteLine($"After drag start - DragOffset: {state.DragOffset}");
			Assert.Equal(50f, state.DragOffset.X);
			Assert.Equal(10f, state.DragOffset.Y);
			Assert.True(state.IsDragging);
		}

		// Simulate moving pointer to (200, 150) and updating window position
		{
			ref var state = ref world.Get<FloatingWindowState>(windowId);
			ref var node = ref world.Get<UiNode>(windowId);
			var newPointerPos = new Vector2(200f, 150f);

			// Calculate new window position: pointer - dragOffset
			state.Position = newPointerPos - state.DragOffset;  // (200, 150) - (50, 10) = (150, 140)

			// Update node offset
			node.Declaration.floating.offset = new Clay_Vector2
			{
				x = state.Position.X,
				y = state.Position.Y
			};

			Console.WriteLine($"After move - State.Position: {state.Position}");
			Console.WriteLine($"After move - Node.floating.offset: ({node.Declaration.floating.offset.x}, {node.Declaration.floating.offset.y})");

			Assert.Equal(150f, state.Position.X);
			Assert.Equal(140f, state.Position.Y);
			Assert.Equal(150f, node.Declaration.floating.offset.x);
			Assert.Equal(140f, node.Declaration.floating.offset.y);
		}

		// Stop dragging
		{
			ref var state = ref world.Get<FloatingWindowState>(windowId);
			state.IsDragging = false;
			Assert.False(state.IsDragging);
		}
	}
}
