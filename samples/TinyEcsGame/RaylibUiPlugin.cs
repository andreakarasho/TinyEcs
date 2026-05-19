using System.Numerics;
using Clay;
using Raylib_cs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;
using RColor = Raylib_cs.Color;

namespace TinyEcsMonogameSample;

sealed class RaylibTextMeasurer : ITextMeasurer
{
	public Dimensions MeasureText(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing)
	{
		var s = new string(text);
		var size = Raylib.MeasureTextEx(Raylib.GetFontDefault(), s, fontSize, letterSpacing);
		return new Dimensions(size.X, size.Y);
	}
}

sealed class RaylibUiPlugin : IPlugin
{
	public Vector2 LogicalSize;

	public void Build(App app)
	{
		app.AddPlugin(new UiPlugin
		{
			TextMeasurer = new RaylibTextMeasurer(),
			LogicalSize = LogicalSize,
		});

		// Feed pointer + delta-time + wheel into UI state before layout runs.
		app.AddSystem((ResMut<UiPointer> pointer, ResMut<UiClayContext> ctx, ResMut<UiSurface> surf) =>
		{
			pointer.Value.Position = Raylib.GetMousePosition();
			pointer.Value.Down = Raylib.IsMouseButtonDown(MouseButton.Left);
			ctx.Value.DeltaTime = Raylib.GetFrameTime();
			// Clay multiplies the delta by 30 internally inside UpdateScrollContainers.
			// One raylib wheel tick (~1.0) thus becomes ~30 pixels of scroll — about one row.
			var wheel = Raylib.GetMouseWheelMoveV();
			ctx.Value.ScrollDelta = wheel;
			ctx.Value.EnableDragScrolling = false;
			surf.Value.LogicalSize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
		})
		.InStage(Stage.PreUpdate)
		.SingleThreaded()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();

		// Draw the UI inside the main BeginDrawing/EndDrawing block.
		app.AddSystem((Res<UiRenderCommands> cmds) =>
		{
			var span = cmds.Value.Span;
			for (var i = 0; i < span.Length; i++)
			{
				ref readonly var cmd = ref span[i];
				DrawCommand(in cmd);
			}
		})
		.InStage(Stage.Last)
		.Label("ui:draw")
		.After("render:debug")
		.Before("render:end")
		.SingleThreaded()
		.RunIf(_ => Raylib.IsWindowReady())
		.Build();
	}

	private static void DrawCommand(in RenderCommand cmd)
	{
		var bb = cmd.BoundingBox;
		var rect = new Rectangle(bb.X, bb.Y, bb.Width, bb.Height);

		switch (cmd.CommandType)
		{
			case RenderCommandType.Rectangle:
			{
				ref readonly var d = ref cmd.Rectangle;
				var color = ToRaylib(d.BackgroundColor);
				if (d.CornerRadius.HasRadius)
				{
					var radius = MathF.Max(d.CornerRadius.TopLeft, d.CornerRadius.TopRight);
					var roundness = MathF.Min(bb.Width, bb.Height) > 0
						? (2f * radius) / MathF.Min(bb.Width, bb.Height)
						: 0f;
					Raylib.DrawRectangleRounded(rect, MathF.Min(roundness, 1f), 8, color);
				}
				else
				{
					Raylib.DrawRectangleRec(rect, color);
				}
				break;
			}
			case RenderCommandType.Border:
			{
				ref readonly var d = ref cmd.Border;
				var color = ToRaylib(d.Color);
				if (d.Width.Top > 0)    Raylib.DrawRectangle((int)bb.X, (int)bb.Y, (int)bb.Width, d.Width.Top, color);
				if (d.Width.Bottom > 0) Raylib.DrawRectangle((int)bb.X, (int)(bb.Y + bb.Height - d.Width.Bottom), (int)bb.Width, d.Width.Bottom, color);
				if (d.Width.Left > 0)   Raylib.DrawRectangle((int)bb.X, (int)bb.Y, d.Width.Left, (int)bb.Height, color);
				if (d.Width.Right > 0)  Raylib.DrawRectangle((int)(bb.X + bb.Width - d.Width.Right), (int)bb.Y, d.Width.Right, (int)bb.Height, color);
				break;
			}
			case RenderCommandType.Text:
			{
				ref readonly var d = ref cmd.Text;
				Raylib.DrawTextEx(
					Raylib.GetFontDefault(),
					d.Text ?? string.Empty,
					new Vector2(bb.X, bb.Y),
					d.FontSize,
					d.LetterSpacing,
					ToRaylib(d.TextColor));
				break;
			}
			case RenderCommandType.ScissorStart:
				Raylib.BeginScissorMode((int)bb.X, (int)bb.Y, (int)bb.Width, (int)bb.Height);
				break;
			case RenderCommandType.ScissorEnd:
				Raylib.EndScissorMode();
				break;
		}
	}

	private static RColor ToRaylib(ClayColor c)
		=> new((byte)c.R, (byte)c.G, (byte)c.B, (byte)c.A);
}
