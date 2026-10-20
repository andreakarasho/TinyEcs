// Route the mouse wheel to the scrollable container under the cursor and consume
// it, so a world camera (or any later wheel consumer honouring
// MouseInput.WheelConsumed) doesn't also act on a notch spent over UI.
//
// Scroll containers that paint nothing get no ComputedNode, so they are found
// through Clay's own layout box (TryGetElementBoundingBox) and scrolled by
// mutating their ScrollPosition component (the supported sync path); Clay's
// native ScrollDelta is zeroed when UI takes the wheel.
//
// A host that captures the wheel for something else (recording a binding)
// consumes it before this runs; a host gesture that must see the notch first
// orders itself .Before(Label).

using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI;

/// <summary>A painted element that is NOT UI for wheel purposes: the wheel reaches
/// whatever lies behind it (a full-screen world viewport drawn as a UI node).</summary>
public struct UiWheelPassThrough;

public sealed class UiWheelRoutingPlugin : IPlugin
{
	/// <summary>System label: a host wheel gesture that must see the notch before UI
	/// consumes it orders itself <c>.Before(UiWheelRoutingPlugin.Label)</c>.</summary>
	public const string Label = "ui:route_wheel";

	/// <summary>Content pixels scrolled per wheel notch.</summary>
	public float PixelsPerNotch = 90f;

	public void Build(App app)
	{
		if (!app.HasResource<MouseInput>()) app.AddResource(new MouseInput());
		var step = PixelsPerNotch;
		// Stage.First, after the host's input feed (declaration order): world
		// consumers in Update read WheelConsumed.
		app.AddSystem((
			ResMut<MouseInput> mouse,
			ResMut<UiClayContext> ctx,
			Query<Data<ScrollPosition>> scrollPosQ,
			UiGesturePick pick,
			Query<Data<GlobalZIndex>> zQ,
			Query<Data<UiWheelPassThrough>> passQ) =>
			Route(mouse, ctx, scrollPosQ, pick, zQ, passQ, step))
			.InStage(Stage.First).Label(Label).Build();
	}

	private static void Route(
		ResMut<MouseInput> mouse,
		ResMut<UiClayContext> ctx,
		Query<Data<ScrollPosition>> scrollPosQ,
		UiGesturePick pick,
		Query<Data<GlobalZIndex>> zQ,
		Query<Data<UiWheelPassThrough>> passQ,
		float step)
	{
		var wheel = mouse.Value.Wheel;
		if (Math.Abs(wheel) < 0.001f) return;

		var pos = mouse.Value.Position;

		// Topmost scrollable container under the cursor, by its window's z (only
		// the root carries one). Highest z wins so the front window scrolls, not an
		// overlapping one behind it. Uses last frame's layout.
		ulong topScroll = 0;
		float maxY = 0f;
		int topZ = int.MinValue;
		float topArea = float.MaxValue;
		foreach (var (ent, _) in scrollPosQ)
		{
			var data = ctx.Value.GetScrollContainerData(UiClayId.Of(ent.Ref).Id);
			if (!data.Found || data.MaxScrollY <= 0f) continue;
			if (!ctx.Value.TryGetElementBoundingBox(ent.Ref, out var bb)) continue;
			if (pos.X < bb.X || pos.X > bb.X + bb.Width) continue;
			if (pos.Y < bb.Y || pos.Y > bb.Y + bb.Height) continue;
			var z = UiHierarchy.ResolveWindowZ(ent.Ref, pick.Parents, zQ);
			if (z < topZ) continue;
			// One window can stack overlapping scrollables (a page viewport + a
			// dropdown floating over it) at the same root z. The smaller box is the
			// overlay / innermost container: it wins, not query iteration order.
			var area = bb.Width * bb.Height;
			if (z == topZ && area >= topArea) continue;
			topZ = z;
			topArea = area;
			topScroll = ent.Ref;
			maxY = data.MaxScrollY;
		}

		// Is the cursor over ANY UI? Pixel-perfect over every painted element (not
		// UiContainsByBounds boxes, so a transparent window interior lets the wheel
		// through). A UiWheelPassThrough hit is the world behind the UI, not UI.
		// The owning window's z arbitrates scroll fall-through below; a
		// non-window hit blocks the wheel too but carries no window z.
		int topWindowZ = int.MinValue;
		var uiHit = UiPick.Topmost(pos, pick.Rendered, pick.Parents, pixelHit: ctx.Value.PixelHitTest);
		bool overUi = uiHit.Found && !passQ.Contains(uiHit.Entity);
		if (overUi)
		{
			var root = pick.MovableRoot(uiHit.Entity);
			if (root != 0)
			{
				var (_, _, z) = pick.Movables.Get(root);
				topWindowZ = z.Ref.Value;
			}
		}

		if (!overUi && topScroll == 0) return;

		ctx.Value.ScrollDelta = System.Numerics.Vector2.Zero;
		mouse.Value.ConsumeWheel();

		if (topScroll == 0) return;

		// A higher window covers the cursor: it captures the wheel (consumed above)
		// instead of letting it scroll the window beneath.
		if (topZ < topWindowZ) return;

		var (_, sp) = scrollPosQ.Get(topScroll);
		// wheel > 0 (up) -> OffsetY decreases.
		var want = Math.Clamp(sp.Ref.OffsetY - wheel * step, 0f, maxY);
		if (sp.Ref.OffsetY != want)
		{
			sp.Ref.OffsetY = want;
			scrollPosQ.SetChanged<ScrollPosition>(topScroll);
		}
	}
}
