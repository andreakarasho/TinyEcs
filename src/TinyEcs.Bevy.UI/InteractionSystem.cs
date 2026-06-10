using System.Numerics;
using Clay;

namespace TinyEcs.Bevy.UI;

internal static class InteractionSystem
{
	public static void PreLayout(
		Res<UiPointer> pointer,
		ResMut<UiClayContext> ctx,
		Query<Data<Interaction>> interactions)
	{
		ref readonly var p = ref pointer.Value;
		ref var c = ref ctx.Value;
		global::Clay.Clay.SetContext(c.Context);
		global::Clay.Clay.SetPointerState(p.Position, p.Down);

		// Reset interactions; PostLayout re-applies Hovered/Pressed for the topmost entity.
		foreach (var (_, interaction) in interactions)
			interaction.Ref = Interaction.None;
	}

	public static void PostLayout(
		Commands commands,
		ResMut<UiPointer> pointer,
		ResMut<UiClayContext> ctx,
		Res<Time> time,
		Query<Data<Interaction>> interactives)
	{
		ref var p = ref pointer.Value;
		var cmds = ctx.Value.LastCommands;
		var map = ctx.Value.ClayToEntity;
		var prevHover = ctx.Value.HoveredEntity;
		var clayCtx = ctx.Value.Context;

		ulong hovered = 0;
		BoundingBox hoveredBox = default;

		// Clay maintains PointerOverIds in topmost-first order. Pick the first one
		// that maps to an interactive entity and passes clip (scroll scissor) tests.
		// Refresh against this frame's layout (PreLayout's SetPointerState saw the
		// previous frame's tree).
		global::Clay.Clay.SetContext(clayCtx);
		clayCtx.RefreshPointerOverIds();
		var overIds = global::Clay.Clay.PointerOverIds;
		var pixelHit = ctx.Value.PixelHitTest;
		for (var i = 0; i < overIds.Length; i++)
		{
			var elementId = overIds[i];
			if (!map.TryGetValue(elementId.Id, out var entityId))
				continue;
			if (!interactives.Contains(entityId))
				continue;
			if (!clayCtx.PointerOver(elementId))
				continue; // outside clip bounds or rejected by CustomHitTest

			var box = clayCtx.GetElementData(elementId).BoundingBox;
			// Pixel-perfect pass-through: a host hook can reject this element
			// when the cursor lands on a transparent sprite pixel, letting the
			// hover fall through to whatever is drawn behind it.
			if (pixelHit != null && !pixelHit(entityId, p.Position, box))
				continue;

			hovered = entityId;
			hoveredBox = box;
			break;
		}

		ctx.Value.HoveredEntity = hovered;

		// Press edge: record which entity (if any) the gesture began on. Empty when
		// the user pressed on bare canvas; click logic uses this to reject mismatches.
		if (p.Down && !p.WasDown)
			ctx.Value.PressedEntity = hovered;

		if (hovered != 0)
		{
			var (_, interaction) = interactives.Get(hovered);
			interaction.Ref = p.Down ? Interaction.Pressed : Interaction.Hovered;

			// Press edge: down began this frame over this entity.
			if (p.Down && !p.WasDown)
				commands.Entity(hovered).EmitTrigger(new UiPointerDown { Position = p.Position }, propagate: true);

			// Click only when press AND release happened over the same entity.
			if (!p.Down && p.WasDown && ctx.Value.PressedEntity == hovered)
			{
				commands.Entity(hovered).EmitTrigger(new UiClick { Position = p.Position }, propagate: true);

				// Double-click synthesis: second UiClick on the same entity
				// within DoubleClickWindow seconds emits UiDoubleClick. Clears
				// the latch on emit so a triple-click reads as click + dclick,
				// not two dclicks.
				var now = time.Value.Total / 1000f;
				if (ctx.Value.LastClickEntity == hovered
					&& now - ctx.Value.LastClickTime <= ctx.Value.DoubleClickWindow)
				{
					commands.Entity(hovered).EmitTrigger(new UiDoubleClick { Position = p.Position }, propagate: true);
					ctx.Value.LastClickEntity = 0;
					ctx.Value.LastClickTime = 0f;
				}
				else
				{
					ctx.Value.LastClickEntity = hovered;
					ctx.Value.LastClickTime = now;
				}
			}

			if (prevHover != hovered)
				commands.Entity(hovered).EmitTrigger(new UiOver(), propagate: true);

			// Cursor relative position
			var rel = new Vector2(
				(p.Position.X - hoveredBox.X) / MathF.Max(1, hoveredBox.Width),
				(p.Position.Y - hoveredBox.Y) / MathF.Max(1, hoveredBox.Height));
			commands.Entity(hovered).Insert(new RelativeCursorPosition { Normalized = rel, InBounds = true });

			// Move: pointer displaced this frame while over the entity. Skip the
			// first frame an entity becomes hovered (prevHover != hovered) — that's
			// an Over, not a Move, and LastPosition may be stale.
			var moveDelta = p.Position - p.LastPosition;
			if (prevHover == hovered && moveDelta != Vector2.Zero)
				commands.Entity(hovered).EmitTrigger(new UiMove { Position = p.Position, Delta = moveDelta }, propagate: true);

			// Scroll: wheel input this frame is dispatched to the entity under the
			// pointer (the layout system separately applies it to scroll containers).
			var scroll = ctx.Value.ScrollDelta;
			if (scroll != Vector2.Zero)
				commands.Entity(hovered).EmitTrigger(new UiScroll { Position = p.Position, Delta = scroll }, propagate: true);
		}

		if (prevHover != 0 && prevHover != hovered)
			commands.Entity(prevHover).EmitTrigger(new UiOut(), propagate: true);

		// Release edge: pointer up this frame. Fire on the press-origin entity (if any),
		// then clear the press latch.
		if (!p.Down && p.WasDown)
		{
			if (ctx.Value.PressedEntity != 0)
				commands.Entity(ctx.Value.PressedEntity).EmitTrigger(new UiPointerUp { Position = p.Position }, propagate: true);
			ctx.Value.PressedEntity = 0;
		}

		// Write ComputedNode for all entities that had a render command this frame.
		for (var i = 0; i < cmds.Length; i++)
		{
			ref readonly var cmd = ref cmds[i];
			// ScissorStart: an Overflow.Scroll/Clip container that paints nothing
			// of its own still needs a ComputedNode = its clip box, so hit-tests
			// can clip overflowing children to the visible viewport (the scissor's
			// BoundingBox IS that box, keyed by the element id).
			if (cmd.CommandType != RenderCommandType.Rectangle &&
			    cmd.CommandType != RenderCommandType.Image &&
			    cmd.CommandType != RenderCommandType.Border &&
			    cmd.CommandType != RenderCommandType.Text &&
			    cmd.CommandType != RenderCommandType.Custom &&
			    cmd.CommandType != RenderCommandType.ScissorStart)
				continue;
			if (!map.TryGetValue(cmd.Id, out var entityId))
				continue;
			commands.Entity(entityId).Insert(new ComputedNode
			{
				Size = new Vector2(cmd.BoundingBox.Width, cmd.BoundingBox.Height),
				Position = new Vector2(cmd.BoundingBox.X, cmd.BoundingBox.Y),
				ClayId = cmd.Id,
				PaintOrder = i,
			});
		}

		// Latch pointer edges for next frame
		p.WasDown = p.Down;
		p.LastPosition = p.Position;
	}
}
