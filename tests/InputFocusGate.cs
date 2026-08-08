using System.Numerics;
using TinyEcs.Bevy.Input;

namespace TinyEcs.Tests;

// The focus gate: while the host window is not active, NOTHING the OS reports
// may reach consumers — no edges, no wheel, no cursor tracking, no held-key set.
// And the frame focus comes back must not replay what happened while we were
// away (the click that raised the window, the Alt of an alt-tab).
public class InputFocusGateTest
{
    private static void Frame(MouseInput m, Vector2 pos, MouseButtons down, float wheel, bool active, float t)
    {
        m.SetSnapshot(pos, down, wheel, active);
        m.Update(t);
    }

    [Fact]
    public void Mouse_edges_are_dead_while_inactive()
    {
        var m = new MouseInput();
        Frame(m, new Vector2(10, 10), MouseButtons.None, 0f, active: false, 0f);
        Frame(m, new Vector2(10, 10), MouseButtons.Left, 0f, active: false, 16f);

        Assert.False(m.IsPressedOnce(MouseButton.Left));
        Assert.False(m.IsPressed(MouseButton.Left));

        Frame(m, new Vector2(10, 10), MouseButtons.None, 0f, active: false, 32f);
        Assert.False(m.IsReleased(MouseButton.Left));
    }

    [Fact]
    public void Wheel_is_dropped_while_inactive_and_does_not_dump_on_refocus()
    {
        var m = new MouseInput();
        Frame(m, Vector2.Zero, MouseButtons.None, 5f, active: false, 0f);
        Assert.Equal(0f, m.Wheel);

        // Refocus frame with no new scroll: the notches spun while unfocused
        // must be gone, not queued.
        Frame(m, Vector2.Zero, MouseButtons.None, 0f, active: true, 16f);
        Assert.Equal(0f, m.Wheel);

        Frame(m, Vector2.Zero, MouseButtons.None, 2f, active: true, 32f);
        Assert.Equal(2f, m.Wheel);
    }

    [Fact]
    public void Position_freezes_while_inactive()
    {
        var m = new MouseInput();
        Frame(m, new Vector2(100, 100), MouseButtons.None, 0f, active: true, 0f);
        Assert.Equal(new Vector2(100, 100), m.Position);

        // Cursor wanders across the window while another app owns focus.
        Frame(m, new Vector2(400, 300), MouseButtons.None, 0f, active: false, 16f);
        Assert.Equal(new Vector2(100, 100), m.Position);
        Assert.Equal(Vector2.Zero, m.PositionOffset);
        Assert.Equal(Vector2.Zero, m.DraggingOffset);
    }

    [Fact]
    public void Click_that_raises_the_window_is_swallowed()
    {
        var m = new MouseInput();
        // Unfocused, button up.
        Frame(m, new Vector2(50, 50), MouseButtons.None, 0f, active: false, 0f);
        // The activation click: SDL reports focus-gained and the button down on
        // the same frame.
        Frame(m, new Vector2(50, 50), MouseButtons.Left, 0f, active: true, 16f);

        Assert.False(m.IsPressedOnce(MouseButton.Left));
        // It reads as already-held, so a drag/pan gesture doesn't start either
        // until the user presses again.
        Assert.True(m.IsPressed(MouseButton.Left));

        // A fresh press after focus works normally.
        Frame(m, new Vector2(50, 50), MouseButtons.None, 0f, active: true, 32f);
        Frame(m, new Vector2(50, 50), MouseButtons.Left, 0f, active: true, 48f);
        Assert.True(m.IsPressedOnce(MouseButton.Left));
    }

    [Fact]
    public void Keys_are_dead_while_inactive_and_held_keys_do_not_fire_on_refocus()
    {
        var k = new KeyboardInput();
        Span<KeyCode> none = stackalloc KeyCode[0];
        Span<KeyCode> alt = [KeyCode.LeftAlt];

        k.SetSnapshot(none, active: true);
        k.Update(0f);

        // Alt-tab away: Alt is down, we are not focused.
        k.SetSnapshot(alt, active: false);
        k.Update(16f);
        Assert.False(k.IsPressedOnce(KeyCode.LeftAlt));
        Assert.True(k.PressedKeys.IsEmpty);

        // Alt-tab back with Alt still down.
        k.SetSnapshot(alt, active: true);
        k.Update(32f);
        Assert.False(k.IsPressedOnce(KeyCode.LeftAlt));

        // A key pressed after focus fires.
        k.SetSnapshot(none, active: true);
        k.Update(48f);
        Span<KeyCode> a = [KeyCode.A];
        k.SetSnapshot(a, active: true);
        k.Update(64f);
        Assert.True(k.IsPressedOnce(KeyCode.A));
        Assert.False(k.PressedKeys.IsEmpty);
    }
}
