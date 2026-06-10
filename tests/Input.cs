using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;
using Xunit;

namespace TinyEcs.Tests
{
	public class InputTest
	{
		private static MouseInput Mouse() => new();

		private static void Frame(MouseInput m, ref float t, Vector2 pos, MouseButtons down, float wheel = 0f, bool active = true)
		{
			m.SetSnapshot(pos, down, wheel, active);
			m.Update(t);
			t += 16f;
		}

		[Fact]
		public void Mouse_PressEdges()
		{
			var m = Mouse();
			var t = 0f;

			Frame(m, ref t, Vector2.Zero, MouseButtons.None);
			Frame(m, ref t, Vector2.Zero, MouseButtons.Left);

			Assert.True(m.IsPressedOnce(MouseButton.Left));
			Assert.False(m.IsPressed(MouseButton.Left));
			Assert.False(m.IsReleased(MouseButton.Left));

			Frame(m, ref t, Vector2.Zero, MouseButtons.Left);
			Assert.False(m.IsPressedOnce(MouseButton.Left));
			Assert.True(m.IsPressed(MouseButton.Left));

			Frame(m, ref t, Vector2.Zero, MouseButtons.None);
			Assert.True(m.IsReleased(MouseButton.Left));
			Assert.False(m.IsPressed(MouseButton.Left));
		}

		[Fact]
		public void Mouse_SnapshotPersistsWithoutRefeed()
		{
			var m = Mouse();
			m.SetSnapshot(new Vector2(5, 5), MouseButtons.Left);
			m.Update(0f);
			// no SetSnapshot this frame — held state must carry over
			m.Update(16f);

			Assert.True(m.IsPressed(MouseButton.Left));
		}

		[Fact]
		public void Mouse_ConsumeSuppressesReadsForFrame()
		{
			var m = Mouse();
			var t = 0f;

			Frame(m, ref t, Vector2.Zero, MouseButtons.None);
			Frame(m, ref t, Vector2.Zero, MouseButtons.Right);

			Assert.True(m.IsPressedOnce(MouseButton.Right));
			m.Consume(MouseButton.Right);
			Assert.False(m.IsPressedOnce(MouseButton.Right));
			Assert.True(m.IsConsumed(MouseButton.Right));

			// consume flag clears on the next frame
			Frame(m, ref t, Vector2.Zero, MouseButtons.Right);
			Assert.False(m.IsConsumed(MouseButton.Right));
			Assert.True(m.IsPressed(MouseButton.Right));
		}

		[Fact]
		public void Mouse_WheelConsume()
		{
			var m = Mouse();
			var t = 0f;

			Frame(m, ref t, Vector2.Zero, MouseButtons.None, wheel: 2f);
			Assert.Equal(2f, m.Wheel);

			m.ConsumeWheel();
			Assert.True(m.WheelConsumed);
			Assert.Equal(0f, m.Wheel);

			// wheel delta does not repeat without a new feed
			m.Update(t);
			Assert.Equal(0f, m.Wheel);
			Assert.False(m.WheelConsumed);
		}

		[Fact]
		public void Mouse_DoubleClickWithinWindow()
		{
			var m = Mouse();
			var t = 0f;

			Frame(m, ref t, Vector2.Zero, MouseButtons.None);
			Frame(m, ref t, Vector2.Zero, MouseButtons.Left);
			Frame(m, ref t, Vector2.Zero, MouseButtons.None);
			Frame(m, ref t, Vector2.Zero, MouseButtons.Left);

			Assert.True(m.IsPressedDouble(MouseButton.Left));

			// latch clears the frame after it reports
			Frame(m, ref t, Vector2.Zero, MouseButtons.Left);
			Assert.False(m.IsPressedDouble(MouseButton.Left));
		}

		[Fact]
		public void Mouse_DoubleClickExpires()
		{
			var m = Mouse();
			var t = 0f;

			Frame(m, ref t, Vector2.Zero, MouseButtons.None);
			Frame(m, ref t, Vector2.Zero, MouseButtons.Left);
			Frame(m, ref t, Vector2.Zero, MouseButtons.None);

			t += 1000f; // beyond DoubleClickDelta
			Frame(m, ref t, Vector2.Zero, MouseButtons.Left);

			Assert.False(m.IsPressedDouble(MouseButton.Left));
		}

		[Fact]
		public void Mouse_InactiveGatesEdges()
		{
			var m = Mouse();
			var t = 0f;

			Frame(m, ref t, Vector2.Zero, MouseButtons.None, active: false);
			Frame(m, ref t, Vector2.Zero, MouseButtons.Left, active: false);

			Assert.False(m.IsPressedOnce(MouseButton.Left));

			// state still advanced: refocusing while held reports IsPressed, not a stale edge
			Frame(m, ref t, Vector2.Zero, MouseButtons.Left, active: true);
			Assert.False(m.IsPressedOnce(MouseButton.Left));
			Assert.True(m.IsPressed(MouseButton.Left));
		}

		[Fact]
		public void Mouse_DraggingOffsetAnchorsOnPress()
		{
			var m = Mouse();
			var t = 0f;

			Frame(m, ref t, new Vector2(10, 10), MouseButtons.None);
			Frame(m, ref t, new Vector2(10, 10), MouseButtons.Left);
			Assert.Equal(Vector2.Zero, m.DraggingOffset);

			Frame(m, ref t, new Vector2(25, 18), MouseButtons.Left);
			Assert.Equal(new Vector2(15, 8), m.DraggingOffset);

			Frame(m, ref t, new Vector2(25, 18), MouseButtons.None);
			// release resets the anchor to origin (legacy ClassicUO semantics)
			Assert.Equal(new Vector2(25, 18), m.DraggingOffset);
		}

		[Fact]
		public void Mouse_PositionOffset()
		{
			var m = Mouse();
			var t = 0f;

			Frame(m, ref t, new Vector2(3, 4), MouseButtons.None);
			Frame(m, ref t, new Vector2(10, 6), MouseButtons.None);

			Assert.Equal(new Vector2(10, 6), m.Position);
			Assert.Equal(new Vector2(7, 2), m.PositionOffset);
		}

		[Fact]
		public void Keyboard_PressEdges()
		{
			var k = new KeyboardInput();

			k.SetSnapshot([]);
			k.Update(0f);
			k.SetSnapshot([KeyCode.Enter]);
			k.Update(16f);

			Assert.True(k.IsPressedOnce(KeyCode.Enter));
			Assert.False(k.IsPressed(KeyCode.Enter));

			k.SetSnapshot([KeyCode.Enter]);
			k.Update(32f);
			Assert.True(k.IsPressed(KeyCode.Enter));
			Assert.False(k.IsPressedOnce(KeyCode.Enter));

			k.SetSnapshot([]);
			k.Update(48f);
			Assert.True(k.IsReleased(KeyCode.Enter));
		}

		[Fact]
		public void Keyboard_PressedKeysAndInactiveGate()
		{
			var k = new KeyboardInput();

			k.SetSnapshot([KeyCode.A, KeyCode.LeftShift]);
			k.Update(0f);

			Assert.Equal(2, k.PressedKeys.Length);
			Assert.Contains(KeyCode.A, k.PressedKeys.ToArray());
			Assert.Contains(KeyCode.LeftShift, k.PressedKeys.ToArray());

			k.SetSnapshot([KeyCode.A, KeyCode.LeftShift], active: false);
			k.Update(16f);
			Assert.False(k.IsPressed(KeyCode.A));
		}

		[Fact]
		public void Plugin_RegistersResources()
		{
			var app = new App();
			app.AddPlugin(new InputPlugin());

			Assert.True(app.HasResource<MouseInput>());
			Assert.True(app.HasResource<KeyboardInput>());
		}
	}
}
