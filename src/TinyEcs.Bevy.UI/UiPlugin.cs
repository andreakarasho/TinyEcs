using System.Numerics;
using Clay;

namespace TinyEcs.Bevy.UI;

public sealed class UiPlugin : IPlugin
{
	public ITextMeasurer? TextMeasurer;
	public Vector2 LogicalSize = new(1280, 720);
	public int MaxElements = 8192;

	public static readonly Stage UiPreLayoutStage  = Stage.Custom("UiPreLayout");
	public static readonly Stage UiLayoutStage     = Stage.Custom("UiLayout");
	public static readonly Stage UiPostLayoutStage = Stage.Custom("UiPostLayout");
	public static readonly Stage UiRenderStage     = Stage.Custom("UiRender");

	public void Build(App app)
	{
		var measurer = TextMeasurer ?? new SimpleTextMeasurer();

		Clay.Clay.Initialize(new Dimensions(LogicalSize.X, LogicalSize.Y), measurer, MaxElements);
		var clayCtx = Clay.Clay.Context!;

		app.AddPlugin(new TimePlugin());

		app.AddResource(new UiScale());
		app.AddResource(new UiTextMeasure { Measurer = measurer });
		app.AddResource(new UiSurface { LogicalSize = LogicalSize, PhysicalSize = LogicalSize });
		app.AddResource(new UiPointer());
		app.AddResource(new UiRenderCommands());
		app.AddResource(new UiClayContext { Context = clayCtx });
		app.AddResource(new UiTextureRegistry());
		app.AddResource(new UiFontRegistry());

		app.AddStage(UiPreLayoutStage).After(Stage.PreUpdate).Before(UiLayoutStage);
		app.AddStage(UiLayoutStage).After(Stage.Update);
		app.AddStage(UiPostLayoutStage).After(UiLayoutStage);
		app.AddStage(UiRenderStage).After(UiPostLayoutStage).Before(Stage.Last);

		app.AddSystem((Res<UiPointer> p, ResMut<UiClayContext> c, Query<Data<Interaction>> q) =>
			InteractionSystem.PreLayout(p, c, q))
			.InStage(UiPreLayoutStage).SingleThreaded().Build();

		app.AddSystem((Res<UiSurface> s, Res<UiScale> scale, ResMut<UiClayContext> c,
			Res<Time> time,
			Res<UiPointer> pointer,
			Query<Data<Node>, Without<TinyEcs.Parent>> roots,
			UiLayoutQueries q,
			UiLayoutChanged changed,
			Query<Data<ScrollPosition>> scrolls,
			Local<HashSet<ulong>> liveIds,
			Local<List<ulong>> pruneBuf,
			ResMut<SystemProfiler> prof) =>
			LayoutSystem.Run(s, scale, c, time, pointer, roots, q, changed, scrolls, liveIds, pruneBuf, prof.Value))
			.InStage(UiLayoutStage).SingleThreaded().Build();

		app.AddSystem((Commands cmd, ResMut<UiPointer> p, ResMut<UiClayContext> c,
			Res<Time> time,
			Query<Data<Interaction>> q,
			Query<Data<UiContainsByBounds>> boundsOnly,
			Query<Data<ComputedNode>> computed,
			Query<Data<RelativeCursorPosition>> relCursor,
			Local<HashSet<ulong>> computedSeen) =>
			InteractionSystem.PostLayout(cmd, p, c, time, q, boundsOnly, computed, relCursor, computedSeen))
			.InStage(UiPostLayoutStage).SingleThreaded().Build();

		app.AddSystem((Commands cmd, Res<UiPointer> p, Res<UiClayContext> c, Res<Time> time,
			Local<InteractionSystem.HoverIntentState> state,
			Query<Data<UiHoverIntent>> intents) =>
			InteractionSystem.HoverIntent(cmd, p, c, time, state, intents))
			.InStage(UiPostLayoutStage).SingleThreaded().Build();

		app.AddSystem((Res<UiClayContext> c, ResMut<UiRenderCommands> o) =>
			RenderSystem.Publish(c, o))
			.InStage(UiRenderStage).SingleThreaded().Build();

		// Structural edits the Changed<T> gate cannot see. A tick only exists
		// while the component does, so REMOVING one (or despawning its entity)
		// leaves nothing to mark — yet the element, its rect, its border must
		// disappear from the tree. Same for (re)parenting: it moves an existing
		// Node between subtrees without touching any layout value.
		ForceRelayoutOn<OnRemove<Node>>(app);
		ForceRelayoutOn<OnInsert<TinyEcs.Parent>>(app);
		ForceRelayoutOn<OnRemove<TinyEcs.Parent>>(app);
		ForceRelayoutOn<OnRemove<BackgroundColor>>(app);
		ForceRelayoutOn<OnRemove<BorderColor>>(app);
		ForceRelayoutOn<OnRemove<BorderRadius>>(app);
		ForceRelayoutOn<OnRemove<UiImage>>(app);
		ForceRelayoutOn<OnRemove<Text>>(app);
		ForceRelayoutOn<OnRemove<TextFont>>(app);
		ForceRelayoutOn<OnRemove<TextColor>>(app);
		ForceRelayoutOn<OnRemove<TextWrap>>(app);
		ForceRelayoutOn<OnRemove<ZIndex>>(app);
		ForceRelayoutOn<OnRemove<GlobalZIndex>>(app);
		ForceRelayoutOn<OnRemove<BoxShadow>>(app);
		ForceRelayoutOn<OnRemove<UiCustom>>(app);
		ForceRelayoutOn<OnRemove<ScrollPosition>>(app);
	}

	private static void ForceRelayoutOn<TTrigger>(App app) where TTrigger : ITrigger
		=> app.AddObserver<TTrigger, ResMut<UiClayContext>>((_, ctx) => ctx.Value.MarkLayoutDirty());
}
