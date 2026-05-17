using System.Numerics;
using Clay;
using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.UI;

internal static class LayoutSystem
{
	public static void Run(
		WorldParam worldParam,
		Res<UiSurface> surface,
		ResMut<UiClayContext> ctx,
		Query<Data<Node>, Filter<With<UiRoot>>> roots,
		Query<Data<ScrollPosition>> scrollPositions)
	{
		var world = worldParam.World;
		ref var c = ref ctx.Value;
		Clay.Clay.SetContext(c.Context);
		Clay.Clay.SetLayoutDimensions(new Dimensions(surface.Value.LogicalSize.X, surface.Value.LogicalSize.Y));

		// Drive scroll containers (mouse wheel, drag) BEFORE BeginLayout — Clay applies
		// the delta to its container state and then layout reads the updated positions.
		Clay.Clay.UpdateScrollContainers(c.EnableDragScrolling, c.ScrollDelta, c.DeltaTime);

		// Push user-mutated ScrollPosition components to Clay. A value differing from
		// what we wrote last frame means the user changed it; everything else is a
		// no-op echo we should leave alone so wheel input keeps working.
		foreach (var (eid, sp) in scrollPositions)
		{
			var current = new Vector2(sp.Ref.OffsetX, sp.Ref.OffsetY);
			if (c.LastSyncedScroll.TryGetValue(eid.Ref, out var last) && last == current)
				continue;
			var clayId = ElementId.HashNumber((uint)eid.Ref);
			Clay.Clay.Context!.SetScrollPosition(clayId, current);
		}

		Clay.Clay.BeginLayout(c.DeltaTime);

		c.ClayToEntity.Clear();
		c.ScrollClayToEntity.Clear();

		foreach (var (entityId, node) in roots)
		{
			EmitNode(world, entityId.Ref, in node.Ref, c);
		}

		var cmds = Clay.Clay.EndLayout();
		c.LastCommands = cmds.ToArray();

		// Sync Clay scroll state back into ScrollPosition components and record the
		// value in LastSyncedScroll so the next frame's push step can distinguish
		// our echo from a user mutation.
		foreach (var (eid, sp) in scrollPositions)
		{
			var clayId = ElementId.HashNumber((uint)eid.Ref).Id;
			var data = c.GetScrollContainerData(clayId);
			if (!data.Found)
				continue;
			sp.Ref.OffsetX = data.ScrollPosition.X;
			sp.Ref.OffsetY = data.ScrollPosition.Y;
			c.LastSyncedScroll[eid.Ref] = new Vector2(data.ScrollPosition.X, data.ScrollPosition.Y);
		}
	}

	private static void EmitNode(
		World world,
		ulong entityId,
		in Node node,
		UiClayContext c)
	{
		if (node.Display == Display.None)
			return;

		var view = world.Entity(entityId);
		var decl = BuildDecl(entityId, in node, view);
		var ctx = global::Clay.Clay.Context!;
		ctx.OpenElement();
		ctx.ConfigureOpenElement(decl);
		c.ClayToEntity[decl.Id.Id] = entityId;
		if (node.Overflow == Overflow.Scroll)
			c.ScrollClayToEntity[decl.Id.Id] = entityId;

		if (view.Has<Text>())
		{
			ref var txt = ref view.Get<Text>();
			var tcfg = TextConfig.Default;
			if (view.Has<TextFont>())
			{
				ref var f = ref view.Get<TextFont>();
				tcfg.FontId = f.FontId;
				if (f.Size > 0) tcfg.FontSize = f.Size;
			}
			if (view.Has<TextColor>())
				tcfg.TextColor = view.Get<TextColor>().Value;
			ctx.AddText((txt.Value ?? string.Empty).AsSpan(), tcfg);
		}

		if (view.Has<TinyEcs.Children>())
		{
			ref var children = ref view.Get<TinyEcs.Children>();
			foreach (var childId in children)
			{
				var childView = world.Entity(childId);
				if (!childView.Has<Node>())
					continue;
				EmitNode(world, childId, in childView.Get<Node>(), c);
			}
		}

		ctx.CloseElement();
	}

	private static ElementDeclaration BuildDecl(ulong entityId, in Node node, EntityView view)
	{
		var decl = new ElementDeclaration
		{
			Id = ElementId.HashNumber((uint)entityId),
			Layout = new LayoutConfig
			{
				Sizing = new Sizing(
					ClayMap.ToSizing(node.Width,  node.MinWidth,  node.MaxWidth),
					ClayMap.ToSizing(node.Height, node.MinHeight, node.MaxHeight)),
				Padding = ClayMap.ToPadding(node.Padding),
				ChildGap = node.Gap.Type == ValType.Px ? (ushort)MathF.Max(0, node.Gap.Value) : (ushort)0,
				ChildAlignment = new ChildAlignment(
					ClayMap.MapJustify(node.JustifyContent),
					ClayMap.MapAlign(node.AlignItems)),
				Direction = ClayMap.MapDirection(node.FlexDirection),
				ClipContent = node.Overflow == Overflow.Clip || node.Overflow == Overflow.Scroll,
			},
		};

		if (node.Overflow == Overflow.Scroll)
			decl.Scroll = ScrollConfig.VerticalScroll;

		if (view.Has<BackgroundColor>())
			decl.BackgroundColor = view.Get<BackgroundColor>().Value;

		if (view.Has<BorderRadius>())
			decl.CornerRadius = ClayMap.ToCornerRadius(view.Get<BorderRadius>());

		if (view.Has<BorderColor>())
		{
			decl.Border.Color = view.Get<BorderColor>().Value;
			decl.Border.Width = ClayMap.ToBorderWidth(node.Border);
		}

		if (view.Has<BoxShadow>())
		{
			ref var s = ref view.Get<BoxShadow>();
			decl.Shadow.Color = s.Color;
			decl.Shadow.OffsetX = s.OffsetX;
			decl.Shadow.OffsetY = s.OffsetY;
			decl.Shadow.BlurRadius = s.BlurRadius;
			decl.Shadow.SpreadRadius = s.SpreadRadius;
		}

		if (view.Has<UiImage>())
		{
			ref var img = ref view.Get<UiImage>();
			decl.Image.ImageData = img.ImageData;
			decl.Image.SourceDimensions = new Dimensions(img.SourceSize.X, img.SourceSize.Y);
			decl.Image.Tint = img.Tint;
		}

		if (node.PositionType == PositionType.Absolute)
		{
			decl.Floating.AttachTo = FloatingAttachTo.Parent;
			decl.Floating.Offset = new System.Numerics.Vector2(
				node.Left.Type == ValType.Px ? node.Left.Value : 0f,
				node.Top.Type  == ValType.Px ? node.Top.Value  : 0f);
			decl.Floating.ZIndex = view.Has<GlobalZIndex>() ? (short)view.Get<GlobalZIndex>().Value
				: view.Has<ZIndex>() ? (short)view.Get<ZIndex>().Value
				: (short)0;
		}

		return decl;
	}
}
