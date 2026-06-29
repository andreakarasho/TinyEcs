using System.Numerics;
using Clay;

namespace TinyEcs.Bevy.UI;

/// Composite parameter bundling every per-component lookup query the layout
/// walk needs. Keeps LayoutSystem.Run's signature small while keeping access
/// scheduler-aware (each inner query advertises read-access on its type).
public sealed class UiLayoutQueries : CompositeSystemParam
{
	public readonly Query<Data<Node>> Nodes;
	public readonly Query<Data<TinyEcs.Children>> Children;
	public readonly Query<Data<BackgroundColor>> Backgrounds;
	public readonly Query<Data<BorderColor>> BorderColors;
	public readonly Query<Data<BorderRadius>> BorderRadii;
	public readonly Query<Data<UiImage>> Images;
	public readonly Query<Data<Text>> Texts;
	public readonly Query<Data<TextFont>> TextFonts;
	public readonly Query<Data<TextColor>> TextColors;
	public readonly Query<Data<TextWrap>> TextWraps;
	public readonly Query<Data<ZIndex>> ZIndexes;
	public readonly Query<Data<GlobalZIndex>> GlobalZIndexes;
	public readonly Query<Data<BoxShadow>> Shadows;
	public readonly Query<Data<ComputedNode>> Computed;
	public readonly Query<Data<UiCustom>> Customs;

	public UiLayoutQueries()
	{
		Nodes           = Add(new Query<Data<Node>>());
		Children        = Add(new Query<Data<TinyEcs.Children>>());
		Backgrounds     = Add(new Query<Data<BackgroundColor>>());
		BorderColors    = Add(new Query<Data<BorderColor>>());
		BorderRadii     = Add(new Query<Data<BorderRadius>>());
		Images          = Add(new Query<Data<UiImage>>());
		Texts           = Add(new Query<Data<Text>>());
		TextFonts       = Add(new Query<Data<TextFont>>());
		TextColors      = Add(new Query<Data<TextColor>>());
		TextWraps       = Add(new Query<Data<TextWrap>>());
		ZIndexes        = Add(new Query<Data<ZIndex>>());
		GlobalZIndexes  = Add(new Query<Data<GlobalZIndex>>());
		Shadows         = Add(new Query<Data<BoxShadow>>());
		Computed        = Add(new Query<Data<ComputedNode>>());
		Customs         = Add(new Query<Data<UiCustom>>());
	}
}

// Clay element id derived from a TinyEcs entity.
//
// Keyed on the entity INDEX only (the low 32 bits of the EcsID). The index is
// unique among LIVE entities, so no two live elements ever collide on a Clay id
// in a single frame — which is exactly what Clay's duplicate-id check enforces.
//
// Generation (high 32 bits) is deliberately NOT mixed in: the Clay id is only
// 32 bits, the entity index can exceed 2^20 (the world index space is shared
// with mobiles/items), so no injective pack fits and any hash that folds in the
// generation can map two DISTINCT live entities to the same id → Clay throws
// "Duplicate element ID". Index-only is collision-free over the index values in
// use, matching the long-standing behaviour.
//
// Caveat: a recycled index (despawn → respawn) reuses its Clay id, so the new
// element inherits the dead one's retained per-id Clay state (scroll/hover).
// That is a cosmetic state-bleed, not a disappearance, and is the lesser evil
// versus duplicate-id crashes. Callers that must not bleed state should despawn
// only when no live element needs the freed index that same frame.
//
// Every Clay-id site MUST go through this so the forward id and the reverse
// ClayToEntity map agree.
internal static class UiClayId
{
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	public static ElementId Of(ulong entityId)
		=> ElementId.HashNumber((uint)entityId);
}

internal static class LayoutSystem
{

	public static void Run(
		Res<UiSurface> surface,
		Res<UiScale> scale,
		ResMut<UiClayContext> ctx,
		Res<Time> time,
		Query<Data<Node>, Without<TinyEcs.Parent>> roots,
		UiLayoutQueries q,
		Query<Data<ScrollPosition>> scrollPositions,
		Local<HashSet<ulong>> liveScrollIds,
		Local<List<ulong>> scrollPruneBuffer,
		SystemProfiler profiler)
	{
		ref var c = ref ctx.Value;
		Clay.Clay.SetContext(c.Context);
		var s = MathF.Max(0.01f, scale.Value.Value);
		Clay.Clay.SetLayoutDimensions(new Dimensions(surface.Value.LogicalSize.X * s, surface.Value.LogicalSize.Y * s));

		Clay.Clay.UpdateScrollContainers(c.EnableDragScrolling, c.ScrollDelta, time.Value.Frame);

		foreach (var (eid, sp) in scrollPositions)
		{
			var current = new Vector2(sp.Ref.OffsetX, sp.Ref.OffsetY);
			if (c.LastSyncedScroll.TryGetValue(eid.Ref, out var last) && last == current)
				continue;
			var clayId = UiClayId.Of(eid.Ref);
			Clay.Clay.Context!.SetScrollPosition(clayId, current);
		}

		Clay.Clay.BeginLayout(time.Value.Frame);

		c.ClayToEntity.Clear();
		c.ScrollClayToEntity.Clear();

		if (profiler.Enabled)
		{
			profiler.LayoutRoots = 0;
			profiler.LayoutNodes = 0;
			profiler.LayoutCulled = 0;
			profiler.LayoutBuildTicks = 0;
			profiler.LayoutConfigTicks = 0;
		}

		var walkStart = profiler.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

		foreach (var (entityId, node) in roots)
		{
			if (profiler.Enabled)
				profiler.LayoutRoots++;
			EmitNode(entityId.Ref, parentId: 0, in node.Ref, c, q, s, inheritedZ: 0, profiler);
		}

		var solveStart = profiler.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

		var cmds = Clay.Clay.EndLayout();

		if (profiler.Enabled)
		{
			var solveEnd = System.Diagnostics.Stopwatch.GetTimestamp();
			profiler.LayoutWalkTicks = solveStart - walkStart;
			profiler.LayoutSolveTicks = solveEnd - solveStart;
		}
		if (c.LastCommandsBuffer.Length < cmds.Length)
			c.LastCommandsBuffer = new RenderCommand[Math.Max(cmds.Length, c.LastCommandsBuffer.Length * 2)];
		cmds.CopyTo(c.LastCommandsBuffer);
		c.LastCommandsCount = cmds.Length;

		var live = liveScrollIds.Value;
		var prune = scrollPruneBuffer.Value;
		live.Clear();
		foreach (var (eid, sp) in scrollPositions)
		{
			live.Add(eid.Ref);
			var clayId = UiClayId.Of(eid.Ref).Id;
			var data = c.GetScrollContainerData(clayId);
			if (!data.Found)
				continue;
			sp.Ref.OffsetX = data.ScrollPosition.X;
			sp.Ref.OffsetY = data.ScrollPosition.Y;
			c.LastSyncedScroll[eid.Ref] = new Vector2(data.ScrollPosition.X, data.ScrollPosition.Y);
		}

		// Purge LastSyncedScroll entries whose entity has been despawned (or had its
		// ScrollPosition removed). Without this the dict grows unbounded over a
		// session, and entity-id recycling could resurrect stale offsets.
		if (c.LastSyncedScroll.Count > live.Count)
		{
			prune.Clear();
			foreach (var key in c.LastSyncedScroll.Keys)
				if (!live.Contains(key))
					prune.Add(key);
			foreach (var dead in prune)
				c.LastSyncedScroll.Remove(dead);
		}
	}

	private static void EmitNode(ulong entityId, ulong parentId, in Node node, UiClayContext c, UiLayoutQueries q, float scale, int inheritedZ, SystemProfiler profiler)
	{
		if (node.Display == Display.None)
		{
			if (profiler.Enabled)
				profiler.LayoutCulled++;
			return;
		}

		if (profiler.Enabled)
			profiler.LayoutNodes++;

		// Resolve the z once here so it can be both applied to this element and
		// threaded down to children. An element with no z of its own inherits
		// its ancestor's — so a window only needs to carry a single z on its
		// root and the whole subtree rides that layer (Clay sorts every
		// absolute element as an independent float root by z, equal z keeping
		// DFS order). Backward-compatible: an element that sets its own z is
		// unchanged.
		var tBuild = profiler.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
		int resolvedZ = ResolveZ(entityId, q, inheritedZ);

		var decl = BuildDecl(entityId, parentId, in node, q, scale, resolvedZ);
		if (profiler.Enabled)
			profiler.LayoutBuildTicks += System.Diagnostics.Stopwatch.GetTimestamp() - tBuild;

		var ctx = Clay.Clay.Context!;
		ctx.OpenElement();

		var tConfig = profiler.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
		ctx.ConfigureOpenElement(decl);
		if (profiler.Enabled)
			profiler.LayoutConfigTicks += System.Diagnostics.Stopwatch.GetTimestamp() - tConfig;
		c.ClayToEntity[decl.Id.Id] = entityId;
		if (node.Overflow == Overflow.Scroll)
			c.ScrollClayToEntity[decl.Id.Id] = entityId;

		if (q.Texts.TryGet(entityId, out var textData))
		{
			var (_, textPtr) = textData;
			var tcfg = TextConfig.Default;
			if (q.TextFonts.TryGet(entityId, out var fontData))
			{
				var (_, fontPtr) = fontData;
				tcfg.FontId = fontPtr.Ref.FontId;
				if (fontPtr.Ref.Size > 0)
					tcfg.FontSize = (ushort)MathF.Max(1, fontPtr.Ref.Size * scale);
			}
			if (q.TextColors.TryGet(entityId, out var colorData))
			{
				var (_, colorPtr) = colorData;
				tcfg.TextColor = colorPtr.Ref.Value;
			}
			if (q.TextWraps.TryGet(entityId, out var wrapData))
			{
				var (_, wrapPtr) = wrapData;
				tcfg.WrapMode = (TextWrapMode)wrapPtr.Ref.Kind;
			}
			// Pass the string directly (not .AsSpan()): Clay's string overload
			// stores the reference instead of copying the span into a fresh
			// string every frame (immediate-mode runs this per text node/frame).
			ctx.AddText(textPtr.Ref.Value ?? string.Empty, tcfg);
		}

		if (q.Children.TryGet(entityId, out var childrenData))
		{
			var (_, childrenPtr) = childrenData;
			ref var children = ref childrenPtr.Ref;
			foreach (var childId in children)
			{
				if (!q.Nodes.TryGet(childId, out var childNodeData))
					continue;
				var (_, childNodePtr) = childNodeData;
				EmitNode(childId, entityId, in childNodePtr.Ref, c, q, scale, resolvedZ, profiler);
			}
		}

		ctx.CloseElement();
	}

	// Own z wins; absent z inherits the ancestor's (threaded down the walk).
	private static int ResolveZ(ulong entityId, UiLayoutQueries q, int inheritedZ)
	{
		if (q.GlobalZIndexes.TryGet(entityId, out var gz))
		{
			var (_, p) = gz;
			return p.Ref.Value;
		}
		if (q.ZIndexes.TryGet(entityId, out var z))
		{
			var (_, p) = z;
			return p.Ref.Value;
		}
		return inheritedZ;
	}

	private static ElementDeclaration BuildDecl(ulong entityId, ulong parentId, in Node node, UiLayoutQueries q, float scale, int resolvedZ)
	{
		var decl = new ElementDeclaration
		{
			Id = UiClayId.Of(entityId),
			Layout = new LayoutConfig
			{
				Sizing = new Sizing(
					ClayMap.ToSizing(node.Width,  node.MinWidth,  node.MaxWidth, scale),
					ClayMap.ToSizing(node.Height, node.MinHeight, node.MaxHeight, scale)),
				Padding = ClayMap.ToPadding(node.Padding, scale),
				ChildGap = node.Gap.Type == ValType.Px ? (ushort)MathF.Max(0, node.Gap.Value * scale) : (ushort)0,
				// Clay's ChildAlignment is direction-agnostic (X, Y). Flexbox semantics
				// flip the axes by FlexDirection: row → main=X/cross=Y, column →
				// main=Y/cross=X. Map JustifyContent to the main axis and AlignItems
				// to the cross axis accordingly.
				ChildAlignment = node.FlexDirection == FlexDirection.Column
					? new ChildAlignment(
						ClayMap.MapAlignItemsX(node.AlignItems),
						ClayMap.MapJustifyY(node.JustifyContent))
					: new ChildAlignment(
						ClayMap.MapJustify(node.JustifyContent),
						ClayMap.MapAlign(node.AlignItems)),
				Direction = ClayMap.MapDirection(node.FlexDirection),
				ClipContent = node.Overflow == Overflow.Clip || node.Overflow == Overflow.Scroll,
			},
		};

		if (node.Overflow == Overflow.Scroll)
			decl.Scroll = ScrollConfig.VerticalScroll;

		if (q.Backgrounds.TryGet(entityId, out var bg))
		{
			var (_, p) = bg;
			decl.BackgroundColor = p.Ref.Value;
		}

		if (q.BorderRadii.TryGet(entityId, out var br))
		{
			var (_, p) = br;
			decl.CornerRadius = ClayMap.ToCornerRadius(p.Ref, scale);
		}

		if (q.BorderColors.TryGet(entityId, out var bc))
		{
			var (_, p) = bc;
			decl.Border = new BorderConfig
			{
				Color = p.Ref.Value,
				Width = ClayMap.ToBorderWidth(node.Border, scale),
			};
		}

		if (q.Shadows.TryGet(entityId, out var sh))
		{
			var (_, p) = sh;
			decl.Shadow = new ShadowConfig
			{
				Color = p.Ref.Color,
				OffsetX = p.Ref.OffsetX * scale,
				OffsetY = p.Ref.OffsetY * scale,
				BlurRadius = p.Ref.BlurRadius * scale,
				SpreadRadius = p.Ref.SpreadRadius * scale,
			};
		}

		if (q.Images.TryGet(entityId, out var img))
		{
			var (_, p) = img;
			decl.Image = new ImageConfig
			{
				ImageData = p.Ref.ImageData,
				// SourceDimensions is the texture's native pixel size — unaffected by scale.
				SourceDimensions = new Dimensions(p.Ref.SourceSize.X, p.Ref.SourceSize.Y),
				Tint = p.Ref.Tint,
			};
		}

		if (q.Customs.TryGet(entityId, out var cu))
		{
			var (_, p) = cu;
			decl.Custom = CustomConfig.Create(p.Ref.Data);
		}

		if (node.PositionType == PositionType.Absolute)
		{
			// Clay's Floating only honours `Offset` (relative to parent top-left),
			// so anchor to the opposite edge by translating Right/Bottom into a
			// left/top offset using the parent's size. Left wins over Right when
			// both are set; same for Top vs Bottom.
			bool useRight  = node.Right.Type  == ValType.Px && node.Left.Type != ValType.Px;
			bool useBottom = node.Bottom.Type == ValType.Px && node.Top.Type  != ValType.Px;

			// Parent's ComputedNode.Size is already in scaled (Clay-output) pixels,
			// so we compare child dimensions in the same space.
			float parentW = 0f, parentH = 0f;
			if ((useRight || useBottom) && parentId != 0 && q.Computed.TryGet(parentId, out var pcData))
			{
				var (_, pc) = pcData;
				parentW = pc.Ref.Size.X;
				parentH = pc.Ref.Size.Y;
			}

			float childW = node.Width.Type  == ValType.Px ? node.Width.Value  * scale : 0f;
			float childH = node.Height.Type == ValType.Px ? node.Height.Value * scale : 0f;

			float ox = useRight
				? MathF.Max(0f, parentW - childW - node.Right.Value * scale)
				: (node.Left.Type == ValType.Px ? node.Left.Value * scale : 0f);
			float oy = useBottom
				? MathF.Max(0f, parentH - childH - node.Bottom.Value * scale)
				: (node.Top.Type  == ValType.Px ? node.Top.Value  * scale : 0f);

			// Z layering on absolute elements maps directly to Clay's Floating.ZIndex
			// (signed 16-bit). resolvedZ is the element's own z, or its ancestor's
			// when it carries none — so children ride their window's root layer.
			// Non-absolute elements ignore z since Clay only z-sorts floats.
			decl.Floating = new FloatingConfig
			{
				AttachTo = FloatingAttachTo.Parent,
				Offset = new Vector2(ox, oy),
				ZIndex = (short)Math.Clamp(resolvedZ, short.MinValue, short.MaxValue),
			};
		}

		return decl;
	}
}
