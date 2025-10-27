using System;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Flexbox;

namespace TinyEcsGame;

/// <summary>
/// Raylib integration plugin for Flexbox UI system.
/// Updates FlexboxPointerState from Raylib and renders Flexbox layouts.
/// </summary>
public struct RaylibFlexboxUiPlugin : IPlugin
{
	public Stage RenderingStage { get; set; }

	public void Build(App app)
	{
		var renderStage = RenderingStage;

		app.AddSystem((ResMut<FlexboxPointerState> p) => UpdatePointerState(p))
			.InStage(Stage.PreUpdate)
			.Label("ui:raylib:flexbox:update-pointer")
			.Before("ui:flexbox:pointer")  // CRITICAL: Must run before pointer processing
			.RunIfResourceExists<FlexboxPointerState>()
			.SingleThreaded()
			.Build();

		app.AddSystem((Res<FlexboxUiState> s,
					   Query<Data<FlexboxNode>> n,
					   Query<Data<FlexboxText>> t,
					   Query<Data<FlexboxScrollContainer>> sc,
					   Query<Data<FlexboxScrollContainerViewport>> vp,
					   Query<Data<Children>> children) =>
			RenderFlexboxUI(s, n, t, sc, vp, children))
			.InStage(renderStage)
			.Label("ui:raylib:flexbox:render")
			.After("render:debug")
			.RunIfResourceExists<FlexboxUiState>()
			.SingleThreaded()
			.Build();
	}

	private static void UpdatePointerState(ResMut<FlexboxPointerState> pointerState)
	{
		if (!Raylib.IsWindowReady())
			return;

		ref var state = ref pointerState.Value;

		state.Position = Raylib.GetMousePosition();
		state.PrimaryDown = Raylib.IsMouseButtonDown(MouseButton.Left);

		var scrollY = Raylib.GetMouseWheelMove();
		if (scrollY != 0f)
		{
			state.AddScroll(new Vector2(0, scrollY * 20f));
		}

		state.DeltaTime = Raylib.GetFrameTime();
	}

	private static void RenderFlexboxUI(
		Res<FlexboxUiState> uiState,
		Query<Data<FlexboxNode>> nodes,
		Query<Data<FlexboxText>> texts,
		Query<Data<FlexboxScrollContainer>> scrollers,
		Query<Data<FlexboxScrollContainerViewport>> viewports,
		Query<Data<Children>> childrenQuery)
	{
		var state = uiState.Value;

		// Render root nodes (those without parents in the Flexbox tree)
		foreach (var rootId in state.RootEntities)
		{
			if (state.TryGetNode(rootId, out var rootNode) && rootNode != null)
			{
				RenderNodeRecursive(rootId, rootNode, nodes, texts, scrollers, viewports, childrenQuery, state, Vector2.Zero, null);
			}
		}
	}

	private static void RenderNodeRecursive(
		ulong entityId,
		global::Flexbox.Node node,
		Query<Data<FlexboxNode>> nodes,
		Query<Data<FlexboxText>> texts,
		Query<Data<FlexboxScrollContainer>> scrollers,
		Query<Data<FlexboxScrollContainerViewport>> viewports,
		Query<Data<Children>> childrenQuery,
		FlexboxUiState state,
		Vector2 parentPosition,
		Rectangle? clip)
	{
		var layout = node.layout;

		// Flexbox positions are relative to parent
		// Accumulate parent position to get absolute screen position
		var drawPos = parentPosition + new Vector2(layout.left, layout.top);
		var drawSize = new Vector2(layout.width, layout.height);

		// Compute child clip and scroll offset
		var childClip = clip;
		var childParentPosition = drawPos; // Children positioned relative to this element

		// Check if this entity is a scroll viewport (has viewport component)
		if (viewports.Contains(entityId))
		{
			// This is a scroll viewport - apply clipping at viewport height
			var vp = viewports.Get(entityId);
			vp.Deconstruct(out var vpPtr);
			var viewportHeight = vpPtr.Ref.Height;

			var contentRect = new Rectangle(
				drawPos.X,
				drawPos.Y,
				layout.width,
				viewportHeight);

			childClip = childClip.HasValue ? Intersect(childClip.Value, contentRect) : contentRect;
		}

		// Check if this entity is a scroll content area (has scroll container component)
		if (scrollers.Contains(entityId))
		{
			// This is the scrollable content area - apply scroll offset to children
			var sc = scrollers.Get(entityId);
			sc.Deconstruct(out var sPtr);
			var scrollOffset = sPtr.Ref.Offset;
			childParentPosition += new Vector2(-scrollOffset.X, -scrollOffset.Y);
		}

		// Apply current clip for this entity draw
		if (clip.HasValue)
		{
			var r = clip.Value;
			Raylib.BeginScissorMode((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
		}

		if (nodes.Contains(entityId))
		{
			var n = nodes.Get(entityId);
			n.Deconstruct(out var nodePtr);
			ref var fxNode = ref nodePtr.Ref;

			if (fxNode.BackgroundColor.W > 0f)
				DrawRectangle(drawPos, drawSize, fxNode.BackgroundColor, fxNode.BorderRadius);
			if (fxNode.BorderColor.W > 0f)
				DrawBorder(drawPos, drawSize, fxNode.BorderColor, fxNode.BorderRadius);

			if (texts.Contains(entityId))
			{
				var t = texts.Get(entityId);
				t.Deconstruct(out var tPtr);
				ref var td = ref tPtr.Ref;
				// Text is drawn at this element's position
				// (Flexbox already accounts for padding in child positioning)
				DrawText(drawPos, drawSize, ref td);
			}
		}

		if (clip.HasValue)
			Raylib.EndScissorMode();

		// Draw children - pass accumulated position
		for (int i = 0; i < node.ChildrenCount; i++)
		{
			var childNode = node.GetChild(i);
			if (childNode != null && childNode.Context is ValueTuple<ulong, uint> context)
			{
				var (childEntityId, _) = context;
				RenderNodeRecursive(childEntityId, childNode, nodes, texts, scrollers, viewports, childrenQuery, state, childParentPosition, childClip);
			}
		}
	}

	private static Rectangle Intersect(Rectangle a, Rectangle b)
	{
		float x = MathF.Max(a.X, b.X);
		float y = MathF.Max(a.Y, b.Y);
		float r = MathF.Min(a.X + a.Width, b.X + b.Width);
		float btm = MathF.Min(a.Y + a.Height, b.Y + b.Height);
		return new Rectangle(x, y, MathF.Max(0, r - x), MathF.Max(0, btm - y));
	}

	private static void DrawRectangle(Vector2 position, Vector2 size, Vector4 color, float borderRadius)
	{
		var rect = new Rectangle(position.X, position.Y, size.X, size.Y);
		var raylibColor = new Color((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255), (byte)(color.W * 255));

		if (borderRadius > 0f)
		{
			Raylib.DrawRectangleRounded(rect, borderRadius / Math.Max(size.X, size.Y), 8, raylibColor);
		}
		else
		{
			Raylib.DrawRectangleRec(rect, raylibColor);
		}
	}

	private static void DrawBorder(Vector2 position, Vector2 size, Vector4 color, float borderRadius)
	{
		var rect = new Rectangle(position.X, position.Y, size.X, size.Y);
		var raylibColor = new Color((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255), (byte)(color.W * 255));

		// Use non-rounded border for simplicity (API differences across Raylib versions)
		Raylib.DrawRectangleLinesEx(rect, 2f, raylibColor);
	}

	private static void DrawText(Vector2 contentPosition, Vector2 contentSize, ref FlexboxText textData)
	{
		if (string.IsNullOrEmpty(textData.Text))
			return;

		var raylibColor = new Color((byte)(textData.Color.X * 255), (byte)(textData.Color.Y * 255), (byte)(textData.Color.Z * 255), (byte)(textData.Color.W * 255));
		Raylib.DrawText(textData.Text, (int)contentPosition.X, (int)contentPosition.Y, (int)textData.FontSize, raylibColor);
	}
}
