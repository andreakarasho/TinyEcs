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
		// Compare first: an unconditional store dirties the whole Interaction
		// column every frame for nothing.
		foreach (var (_, interaction) in interactions)
			if (interaction.Ref != Interaction.None)
				interaction.Ref = Interaction.None;
	}

	public static void PostLayout(
		Commands commands,
		ResMut<UiPointer> pointer,
		ResMut<UiClayContext> ctx,
		Res<Time> time,
		Query<Data<Interaction>> interactives,
		Query<Data<RelativeCursorPosition>> relCursor,
		Query<Data<InteractionDisabled>> disabled,
		Query<Data<Hovered>> hoveredMarks,
		Query<Data<Pressed>> pressedMarks,
		UiGesturePick pick)
	{
		ref var p = ref pointer.Value;
		var prevHover = ctx.Value.HoveredEntity;

		// ONE hit-test model: the shared pick (topmost painted element by
		// PaintOrder, pixel hook, hidden / overflow-clip pruning) — the same
		// answer host gestures get. A painted element blocks whatever is behind
		// it whether or not it is interactive; the Interaction target is the
		// nearest Interaction ancestor-or-self of that hit. Served from
		// UiPickCache, refreshed right after layout at this pointer position.
		var hit = pick.Topmost(p.Position);
		ulong hovered = UiPick.AncestorWith(hit.Entity, interactives, pick.Parents);
		if (hovered != 0 && disabled.Contains(hovered))
			hovered = 0;

		ctx.Value.HoveredEntity = hovered;

		if (prevHover != hovered)
		{
			if (prevHover != 0 && hoveredMarks.Contains(prevHover))
				commands.Entity(prevHover).Remove<Hovered>();
			if (hovered != 0)
				commands.Entity(hovered).Insert(new Hovered());
		}

		// Press edge: record which entity (if any) the gesture began on. Empty when
		// the user pressed on bare canvas; click logic uses this to reject mismatches.
		if (p.Down && !p.WasDown)
		{
			ctx.Value.PressedEntity = hovered;
			if (hovered != 0)
				commands.Entity(hovered).Insert(new Pressed());
		}

		// Right-button twin: same latch for the UiRightClick gate. The press
		// POSITION is latched here too — the trigger carries it (the menu must open
		// where the button came down, not where the cursor is at release).
		if (p.RightDown && !p.RightWasDown)
		{
			ctx.Value.RightPressedEntity = hovered;
			ctx.Value.RightPressedPosition = p.Position;
		}

		if (hovered != 0)
		{
			var (_, interaction) = interactives.Get(hovered);
			var want = p.Down ? Interaction.Pressed : Interaction.Hovered;
			if (interaction.Ref != want)
				interaction.Ref = want;

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

			// Right-click: the same press+release match as the left UiClick. This is the
			// mod context-menu bridge's signal — the host's own right-click gestures
			// (window close, worldmap menu) run over the raw mouse and never saw this.
			if (!p.RightDown && p.RightWasDown && ctx.Value.RightPressedEntity == hovered)
				commands.Entity(hovered).EmitTrigger(new UiRightClick { Position = ctx.Value.RightPressedPosition }, propagate: true);

			if (prevHover != hovered)
				commands.Entity(hovered).EmitTrigger(new UiOver(), propagate: true);

			// Cursor relative position. Written IN PLACE when the component is
			// already there — an unconditional Insert re-queues a deferred
			// command (and an OnInsert observer fan-out) every frame the pointer
			// rests on an element.
			ctx.Value.TryGetElementBoundingBox(hovered, out var hoveredBox);
			var rel = new Vector2(
				(p.Position.X - hoveredBox.X) / MathF.Max(1, hoveredBox.Width),
				(p.Position.Y - hoveredBox.Y) / MathF.Max(1, hoveredBox.Height));
			if (relCursor.TryGet(hovered, out var relData))
			{
				var (_, relPtr) = relData;
				if (relPtr.Ref.Normalized != rel || !relPtr.Ref.InBounds)
				{
					relPtr.Ref.Normalized = rel;
					relPtr.Ref.InBounds = true;
				}
			}
			else
			{
				commands.Entity(hovered).Insert(new RelativeCursorPosition { Normalized = rel, InBounds = true });
			}

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
			var pressed = ctx.Value.PressedEntity;
			if (pressed != 0)
			{
				commands.Entity(pressed).EmitTrigger(new UiPointerUp { Position = p.Position }, propagate: true);
				if (pressedMarks.Contains(pressed))
					commands.Entity(pressed).Remove<Pressed>();
			}
			ctx.Value.PressedEntity = 0;
		}
		if (!p.RightDown && p.RightWasDown)
			ctx.Value.RightPressedEntity = 0;

		// Latch pointer edges for next frame
		p.WasDown = p.Down;
		p.RightWasDown = p.RightDown;
		p.LastPosition = p.Position;
	}

	internal sealed class ComputedScratch
	{
		// entity -> PaintOrder written this relayout.
		public readonly Dictionary<ulong, int> Order = new();
		// UiContainsByBounds elements that emitted no render command, and the
		// first command index found inside their subtree (-1 = none).
		public readonly Dictionary<ulong, int> Bare = new();
		public readonly List<ulong> BareKeys = new();
	}

	// ComputedNode writeback: runs in UiLayoutStage right after the solve, so a
	// ComputedNode inserted for a newly rendered entity is applied by the
	// stage's command flush BEFORE the pick (UiPickCache refresh + PostLayout)
	// reads it — a fresh element is hittable on the frame it first lays out.
	//
	// PaintOrder = 2 * render-command index. The odd slots are for
	// UiContainsByBounds elements that paint nothing (no command of their own):
	// they still get a ComputedNode (their layout box) ranked just below the
	// first thing painted inside them, so their whole box is a pick surface at
	// the depth they would paint at.
	public static void WriteComputedNodes(
		Commands commands,
		ResMut<UiClayContext> ctx,
		Query<Data<ComputedNode>> computed,
		Query<Data<UiContainsByBounds>> bounds,
		Query<Data<TinyEcs.Parent>> parents,
		Local<ComputedScratch> scratch)
	{
		var cmds = ctx.Value.LastCommands;
		var map = ctx.Value.ClayToEntity;

		// Only when the tree was actually re-solved: on a gated-out frame the
		// commands (and therefore every box) are byte-identical to the ones
		// already written.
		if (ctx.Value.ComputedGeneration == ctx.Value.LayoutGeneration)
			return;
		ctx.Value.ComputedGeneration = ctx.Value.LayoutGeneration;

		var order = scratch.Value.Order;
		order.Clear();

		// One element can emit several commands (Rectangle + Border, or
		// Rectangle + ScissorStart). Walk BACKWARDS and keep the first hit
		// per entity: that is the LAST command it emitted, writing each entity
		// once — two writes per frame would make the neq-compare below flap
		// forever and pin the relayout gate open.
		// A Border is skipped on the first pass: it is emitted AFTER the
		// element's children (it frames them), so ranking a bordered panel by it
		// put the panel above its own flow content in every topmost-first hit
		// test. Border-only elements get theirs on the second pass.
		for (var pass = 0; pass < 2; pass++)
		for (var i = cmds.Length - 1; i >= 0; i--)
		{
			ref readonly var cmd = ref cmds[i];
			if ((cmd.CommandType == RenderCommandType.Border) != (pass == 1))
				continue;
			// ScissorStart: an Overflow.Scroll/Clip container that paints nothing
			// of its own still needs a ComputedNode = its box, so hit-tests can
			// clip overflowing children to the visible viewport.
			if (cmd.CommandType != RenderCommandType.Rectangle &&
			    cmd.CommandType != RenderCommandType.Image &&
			    cmd.CommandType != RenderCommandType.Border &&
			    cmd.CommandType != RenderCommandType.Text &&
			    cmd.CommandType != RenderCommandType.Custom &&
			    cmd.CommandType != RenderCommandType.ScissorStart)
				continue;
			var box = cmd.BoundingBox;
			if (!map.TryGetValue(cmd.Id, out var entityId))
			{
				// A text run carries its own child id: credit it to the element,
				// with the element's box (the run is only the glyph extent).
				if (cmd.CommandType != RenderCommandType.Text
				    || !ctx.Value.TextClayToEntity.TryGetValue(cmd.Id, out entityId)
				    || !ctx.Value.TryGetElementBoundingBox(entityId, out box))
					continue;
			}
			if (!order.TryAdd(entityId, i * 2))
				continue;
			// The scissor rect is the CLIP, not the element: for a ClipToParent
			// float it is the parent's whole viewport, which would turn the
			// float into a page-sized hit box. Ancestor clips are the hit
			// test's job (it walks the parent chain).
			if (cmd.CommandType == RenderCommandType.ScissorStart
			    && ctx.Value.TryGetElementBoundingBox(entityId, out var own))
				box = own;

			Write(commands, ctx, computed, entityId, box, cmd.Id, i * 2);
		}

		// Bounds-hit elements that painted nothing.
		var bare = scratch.Value.Bare;
		var bareKeys = scratch.Value.BareKeys;
		bare.Clear();
		bareKeys.Clear();
		foreach (var (ent, _) in bounds)
			if (!order.ContainsKey(ent.Ref))
			{
				bare[ent.Ref] = -1;
				bareKeys.Add(ent.Ref);
			}
		if (bareKeys.Count == 0)
			return;

		// First command inside each bare element's subtree: walk every
		// command's owner up its parent chain (forward order, so the first
		// index recorded per ancestor is the smallest).
		for (var i = 0; i < cmds.Length; i++)
		{
			if (!map.TryGetValue(cmds[i].Id, out var cur)
			    && !ctx.Value.TextClayToEntity.TryGetValue(cmds[i].Id, out cur))
				continue;
			for (var depth = 0; depth < 32 && parents.TryGet(cur, out var parentRow); depth++)
			{
				var (_, p) = parentRow;
				cur = (ulong)p.Ref.Id;
				if (bare.TryGetValue(cur, out var first) && first < 0)
					bare[cur] = i;
			}
		}

		foreach (var entityId in bareKeys)
		{
			if (!ctx.Value.TryGetElementBoundingBox(entityId, out var box))
				continue;
			var first = bare[entityId];
			int paintOrder;
			if (first >= 0)
				paintOrder = first * 2 - 1;
			else
			{
				// Nothing painted inside: rank just above the nearest painted
				// ancestor (below that ancestor's later-painted children — an
				// approximation that only matters where it overlaps a sibling).
				paintOrder = -1;
				var cur = entityId;
				for (var depth = 0; depth < 32 && parents.TryGet(cur, out var parentRow); depth++)
				{
					var (_, p) = parentRow;
					cur = (ulong)p.Ref.Id;
					if (order.TryGetValue(cur, out var ancestorOrder))
					{
						paintOrder = ancestorOrder + 1;
						break;
					}
				}
			}
			Write(commands, ctx, computed, entityId, box, UiClayId.Of(entityId).Id, paintOrder);
		}
	}

	private static void Write(
		Commands commands,
		ResMut<UiClayContext> ctx,
		Query<Data<ComputedNode>> computed,
		ulong entityId,
		BoundingBox box,
		uint clayId,
		int paintOrder)
	{
		var size = new Vector2(box.Width, box.Height);
		var pos = new Vector2(box.X, box.Y);
		if (computed.TryGet(entityId, out var computedData))
		{
			var (_, cn) = computedData;
			if (cn.Ref.Size != size || cn.Ref.Position != pos
				|| cn.Ref.ClayId != clayId || cn.Ref.PaintOrder != paintOrder)
			{
				var resized = cn.Ref.Size != size;
				cn.Ref.Size = size;
				cn.Ref.Position = pos;
				cn.Ref.ClayId = clayId;
				cn.Ref.PaintOrder = paintOrder;
				computed.SetChanged<ComputedNode>(entityId);
				// ComputedNode is layout OUTPUT, but BuildDecl reads the
				// PARENT's ComputedNode for Right/Bottom anchoring — the
				// solve must re-run (this root) until that feedback settles.
				// Position / paint order shifts feed nothing back.
				if (resized)
					ctx.Value.DirtyEntities.Add(entityId);
			}
		}
		else
		{
			commands.Entity(entityId).Insert(new ComputedNode
			{
				Size = size,
				Position = pos,
				ClayId = clayId,
				PaintOrder = paintOrder,
			});
			// Same feedback loop: an anchored child laid out before its
			// parent had a ComputedNode needs one more pass.
			ctx.Value.DirtyEntities.Add(entityId);
		}
	}

	internal struct HoverIntentState
	{
		public ulong Entity;
		public float StartMs;
		public bool Fired;
	}

	// Hover intent: UiHoverStart after the pointer rests over a
	// UiHoverIntent-tagged entity for its delay, UiHoverEnd when it leaves.
	// Runs after PostLayout (declaration order) so HoveredEntity is fresh.
	public static void HoverIntent(
		Commands commands,
		Res<UiPointer> pointer,
		Res<UiClayContext> ctx,
		Res<Time> time,
		Local<HoverIntentState> state,
		Query<Data<UiHoverIntent>> intents)
	{
		var hovered = ctx.Value.HoveredEntity;
		if (hovered != 0 && !intents.Contains(hovered))
			hovered = 0;

		var now = time.Value.Total;

		if (hovered != state.Value.Entity)
		{
			if (state.Value.Fired && state.Value.Entity != 0 && intents.Contains(state.Value.Entity))
				commands.Entity(state.Value.Entity).EmitTrigger(new UiHoverEnd(), propagate: true);
			state.Value = new HoverIntentState { Entity = hovered, StartMs = now };
			return;
		}

		if (hovered == 0 || state.Value.Fired)
			return;

		var (_, intent) = intents.Get(hovered);
		var delay = intent.Ref.DelayMs > 0 ? intent.Ref.DelayMs : 250f;
		if (now - state.Value.StartMs >= delay)
		{
			commands.Entity(hovered).EmitTrigger(new UiHoverStart { Position = pointer.Value.Position }, propagate: true);
			state.Value.Fired = true;
		}
	}
}
