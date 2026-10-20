// Drag-to-resize for any window root: tag the root and get the gesture instead
// of hand-rolling rect math off the raw mouse.
//
// The grab band is a square at the window's bottom-right corner (Grip px, 10 by
// default). Latches on press-once + a UiGestureGate claim, so window-drag doesn't
// fire at the same time. Only the ROOT's Node.Width/Height change — children
// follow through flex (Percent / Grow), so tag a window whose content is laid
// out relatively.

using System.Numerics;
using TinyEcs.Bevy.Input;

namespace TinyEcs.Bevy.UI;

public struct UiResizable
{
	public float MinW, MinH;
	public float MaxW, MaxH; // 0 = unbounded (DefaultMax)
	public float Grip;       // 0 = default corner size
	public float GripInset;  // grab square pulled in from the corner by this much
	public bool WholePixels; // truncate the new size to whole px
}

public sealed class UiResizablePlugin : IPlugin
{
	private const float DefaultGrip = 10f;
	private const float DefaultMin = 40f;
	private const float DefaultMax = 4000f;

	public void Build(App app)
	{
		if (!app.HasResource<MouseInput>()) app.AddResource(new MouseInput());
		var resizeFn = ResizeDrag;
		app.AddSystem(resizeFn).InStage(Stage.PreUpdate).Build();
	}

	private struct Anchor
	{
		public bool Active, Claimed;
		public ulong Window;
		public Vector2 Mouse0;
		public float OriginW, OriginH;
	}

	private static void ResizeDrag(
		Res<MouseInput> mouse,
		ResMut<UiGestureGate> gate,
		Local<Anchor> anchor,
		UiGesturePick pick,
		Query<Data<Node, UiResizable>> windowsQ)
	{
		bool held = mouse.Value.IsPressed(MouseButton.Left) || mouse.Value.IsPressedOnce(MouseButton.Left);
		if (!held)
		{
			if (anchor.Value.Claimed)
				gate.Value.Release(UiGestureMode.Window);
			anchor.Value = default;
			return;
		}

		if (mouse.Value.IsPressedOnce(MouseButton.Left) && gate.Value.IsFree)
		{
			var pos = mouse.Value.Position;
			foreach (var (ent, node, rs) in windowsQ)
			{
				if (node.Ref.Display == Display.None) continue;
				float grip = rs.Ref.Grip > 0f ? rs.Ref.Grip : DefaultGrip;
				float gx = node.Ref.Left.Value + node.Ref.Width.Value - grip - rs.Ref.GripInset;
				float gy = node.Ref.Top.Value + node.Ref.Height.Value - grip - rs.Ref.GripInset;
				if (pos.X < gx || pos.Y < gy) continue;
				if (pos.X >= gx + grip || pos.Y >= gy + grip) continue;
				if (!pick.IsTopmostWindow(pos, ent.Ref)) continue;
				gate.Value.TryClaim(UiGestureMode.Window);
				anchor.Value = new Anchor
				{
					Active = true,
					Claimed = true,
					Window = ent.Ref,
					Mouse0 = pos,
					OriginW = node.Ref.Width.Value,
					OriginH = node.Ref.Height.Value,
				};
				break;
			}
		}

		if (!anchor.Value.Active || !windowsQ.TryGet(anchor.Value.Window, out var row)) return;
		var (_, root, res) = row;

		var delta = mouse.Value.Position - anchor.Value.Mouse0;
		float minW = res.Ref.MinW > 0f ? res.Ref.MinW : DefaultMin;
		float minH = res.Ref.MinH > 0f ? res.Ref.MinH : DefaultMin;
		float maxW = res.Ref.MaxW > 0f ? res.Ref.MaxW : DefaultMax;
		float maxH = res.Ref.MaxH > 0f ? res.Ref.MaxH : DefaultMax;
		float w = Math.Clamp(anchor.Value.OriginW + delta.X, minW, maxW);
		float h = Math.Clamp(anchor.Value.OriginH + delta.Y, minH, maxH);
		if (res.Ref.WholePixels)
		{
			w = (int)w;
			h = (int)h;
		}

		if (root.Ref.Width.Value == w && root.Ref.Height.Value == h) return;
		root.Ref.Width = Val.Px(w);
		root.Ref.Height = Val.Px(h);
		windowsQ.SetChanged<Node>(anchor.Value.Window);
	}
}
