using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;

namespace MyBattleground.Examples;

public static class UiClayExample
{
	public static void Run()
	{
		Console.WriteLine("=== Clay UI Example ===\n");

		using var world = new World();
		var app = new App(world);

		app.AddClayUi(new ClayUiOptions
		{
			LayoutDimensions = new Clay_Dimensions(320f, 240f),
			ArenaSize = 256 * 1024,
			EnableDebugMode = false
		});

		app.AddSystem((Commands commands) =>
		{
			var root = PanelWidget.CreateContainer(
				commands,
				new Vector2(280f, 200f),
				parent: default,
				padding: 24,
				gap: 16,
				background: new Clay_Color(34, 40, 52, 255));

			root.Insert(UiText.From("Root Panel"));

			var buttonBaseStyle = ClayButtonStyle.Default with { Size = new Vector2(180f, 48f) };
			var buttonTextConfig = buttonBaseStyle.Text;
			buttonTextConfig.fontSize = 22;
			var buttonStyle = buttonBaseStyle with { Text = buttonTextConfig };

			var button = ButtonWidget.Create(commands, buttonStyle, "Click Me", root.Id);
			button.Observe<UiPointerTrigger>(trigger =>
			{
				var evt = trigger.Event;
				Console.WriteLine($"  [Observe] primary button {evt.Type,-13} target={evt.Target} current={evt.CurrentTarget} primary={evt.IsPrimaryButton}");
			});

			var secondaryButton = ButtonWidget.Create(
				commands,
				buttonStyle with { Background = new Clay_Color(180, 135, 230, 255) },
				"Secondary",
				root.Id);
			secondaryButton.Insert(new UiNodeParent { Parent = root.Id, Index = 1 });
			secondaryButton.Observe<UiPointerTrigger>(trigger =>
			{
				var evt = trigger.Event;
				Console.WriteLine($"  [Observe] secondary button {evt.Type,-13} target={evt.Target} current={evt.CurrentTarget} primary={evt.IsPrimaryButton}");
			});
		})
		.InStage(Stage.Startup)
		.Label("ui:spawn")
		.Build();

		app.AddSystem((EventReader<UiPointerEvent> events) =>
		{
			foreach (var evt in events.Read())
			{
				var target = evt.Target;
				var current = evt.CurrentTarget;
				Console.WriteLine(
					$"  {evt.Type,-13} target={target} current={current} primary={evt.IsPrimaryButton}");
			}
		})
		.InStage(Stage.Update)
		.Label("ui:log-pointer-events")
		.After("ui:clay:layout")
		.Build();

		app.AddSystem((Res<ClayUiState> uiState) =>
		{
			var commands = uiState.Value.RenderCommands;
			Console.WriteLine($"  [RenderCommands] count={commands.Length}");
			for (var i = 0; i < commands.Length; ++i)
			{
				ref readonly var cmd = ref commands[i];
				Console.WriteLine($"    - Command #{i}: type={cmd.commandType} bounds=({cmd.boundingBox.x:F0},{cmd.boundingBox.y:F0},{cmd.boundingBox.width:F0},{cmd.boundingBox.height:F0})");
			}
		})
		.InStage(Stage.Update)
		.Label("ui:dump-render-commands")
		.After("ui:clay:layout")
		.Build();

		app.RunStartup();

		Console.WriteLine("Simulating pointer interaction...\n");

		void SimulateFrame(Vector2 position, bool primaryDown, Vector2 scrollDelta)
		{
			ref var pointer = ref world.GetResourceRef<ClayPointerState>();
			pointer.DeltaTime = 1f / 60f;
			pointer.EnableDragScrolling = true;
			pointer.Position = position;
			pointer.PrimaryDown = primaryDown;
			if (scrollDelta != Vector2.Zero)
			{
				pointer.AddScroll(scrollDelta);
			}
			app.Update();
		}

		// Hover over the primary button (roughly centered within its bounds)
		SimulateFrame(new Vector2(120f, 80f), false, Vector2.Zero);
		// Press while still inside the primary button
		SimulateFrame(new Vector2(120f, 80f), true, Vector2.Zero);
		// Drag slightly while held
		SimulateFrame(new Vector2(130f, 90f), true, new Vector2(0f, -20f));
		// Release inside the primary button
		SimulateFrame(new Vector2(120f, 80f), false, Vector2.Zero);

		Console.WriteLine("\nPointer simulation complete.\n");
	}
}
