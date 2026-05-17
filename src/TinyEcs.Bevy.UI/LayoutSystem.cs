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
	public readonly Query<Data<ZIndex>> ZIndexes;
	public readonly Query<Data<GlobalZIndex>> GlobalZIndexes;
	public readonly Query<Data<BoxShadow>> Shadows;

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
		ZIndexes        = Add(new Query<Data<ZIndex>>());
		GlobalZIndexes  = Add(new Query<Data<GlobalZIndex>>());
		Shadows         = Add(new Query<Data<BoxShadow>>());
	}
}

internal static class LayoutSystem
{
	public static void Run(
		Res<UiSurface> surface,
		ResMut<UiClayContext> ctx,
		Query<Data<Node>, Filter<With<UiRoot>>> roots,
		UiLayoutQueries q,
		Query<Data<ScrollPosition>> scrollPositions)
	{
		ref var c = ref ctx.Value;
		Clay.Clay.SetContext(c.Context);
		Clay.Clay.SetLayoutDimensions(new Dimensions(surface.Value.LogicalSize.X, surface.Value.LogicalSize.Y));

		Clay.Clay.UpdateScrollContainers(c.EnableDragScrolling, c.ScrollDelta, c.DeltaTime);

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
			EmitNode(entityId.Ref, in node.Ref, c, q);

		var cmds = Clay.Clay.EndLayout();
		if (c.LastCommandsBuffer.Length < cmds.Length)
			c.LastCommandsBuffer = new RenderCommand[Math.Max(cmds.Length, c.LastCommandsBuffer.Length * 2)];
		cmds.CopyTo(c.LastCommandsBuffer);
		c.LastCommandsCount = cmds.Length;

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

	private static void EmitNode(ulong entityId, in Node node, UiClayContext c, UiLayoutQueries q)
	{
		if (node.Display == Display.None)
			return;

		var decl = BuildDecl(entityId, in node, q);
		var ctx = Clay.Clay.Context!;
		ctx.OpenElement();
		ctx.ConfigureOpenElement(decl);
		c.ClayToEntity[decl.Id.Id] = entityId;
		if (node.Overflow == Overflow.Scroll)
			c.ScrollClayToEntity[decl.Id.Id] = entityId;

		if (q.Texts.Contains(entityId))
		{
			var (_, textPtr) = q.Texts.Get(entityId);
			var tcfg = TextConfig.Default;
			if (q.TextFonts.Contains(entityId))
			{
				var (_, fontPtr) = q.TextFonts.Get(entityId);
				tcfg.FontId = fontPtr.Ref.FontId;
				if (fontPtr.Ref.Size > 0) tcfg.FontSize = fontPtr.Ref.Size;
			}
			if (q.TextColors.Contains(entityId))
			{
				var (_, colorPtr) = q.TextColors.Get(entityId);
				tcfg.TextColor = colorPtr.Ref.Value;
			}
			ctx.AddText((textPtr.Ref.Value ?? string.Empty).AsSpan(), tcfg);
		}

		if (q.Children.Contains(entityId))
		{
			var (_, childrenPtr) = q.Children.Get(entityId);
			ref var children = ref childrenPtr.Ref;
			foreach (var childId in children)
			{
				if (!q.Nodes.Contains(childId))
					continue;
				var (_, childNodePtr) = q.Nodes.Get(childId);
				EmitNode(childId, in childNodePtr.Ref, c, q);
			}
		}

		ctx.CloseElement();
	}

	private static ElementDeclaration BuildDecl(ulong entityId, in Node node, UiLayoutQueries q)
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

		if (q.Backgrounds.Contains(entityId))
		{
			var (_, p) = q.Backgrounds.Get(entityId);
			decl.BackgroundColor = p.Ref.Value;
		}

		if (q.BorderRadii.Contains(entityId))
		{
			var (_, p) = q.BorderRadii.Get(entityId);
			decl.CornerRadius = ClayMap.ToCornerRadius(p.Ref);
		}

		if (q.BorderColors.Contains(entityId))
		{
			var (_, p) = q.BorderColors.Get(entityId);
			decl.Border.Color = p.Ref.Value;
			decl.Border.Width = ClayMap.ToBorderWidth(node.Border);
		}

		if (q.Shadows.Contains(entityId))
		{
			var (_, p) = q.Shadows.Get(entityId);
			decl.Shadow.Color = p.Ref.Color;
			decl.Shadow.OffsetX = p.Ref.OffsetX;
			decl.Shadow.OffsetY = p.Ref.OffsetY;
			decl.Shadow.BlurRadius = p.Ref.BlurRadius;
			decl.Shadow.SpreadRadius = p.Ref.SpreadRadius;
		}

		if (q.Images.Contains(entityId))
		{
			var (_, p) = q.Images.Get(entityId);
			decl.Image.ImageData = p.Ref.ImageData;
			decl.Image.SourceDimensions = new Dimensions(p.Ref.SourceSize.X, p.Ref.SourceSize.Y);
			decl.Image.Tint = p.Ref.Tint;
		}

		if (node.PositionType == PositionType.Absolute)
		{
			decl.Floating.AttachTo = FloatingAttachTo.Parent;
			decl.Floating.Offset = new Vector2(
				node.Left.Type == ValType.Px ? node.Left.Value : 0f,
				node.Top.Type  == ValType.Px ? node.Top.Value  : 0f);

			short z = 0;
			if (q.GlobalZIndexes.Contains(entityId))
			{
				var (_, p) = q.GlobalZIndexes.Get(entityId);
				z = (short)p.Ref.Value;
			}
			else if (q.ZIndexes.Contains(entityId))
			{
				var (_, p) = q.ZIndexes.Get(entityId);
				z = (short)p.Ref.Value;
			}
			decl.Floating.ZIndex = z;
		}

		return decl;
	}
}
