using System.Linq;
using System.Numerics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using Xunit;
using UiText = TinyEcs.Bevy.UI.Text;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

[Collection("ClayUi")]
public class EcsInspectorTests
{
	private struct InspPos { public int X, Y; }
	private struct InspTag;
	private struct InspNamed { public int Hp; public override string ToString() => $"hp {Hp}"; }

	[Fact]
	public void World_exposes_archetype_rows_values_and_component_types()
	{
		var world = new World();
		var e = world.Entity();
		e.Set(new InspPos { X = 3, Y = 4 });
		e.Add<InspTag>();

		var arch = world.Archetypes.Single(a => a.Count == 1 && a.All.Length == 2);
		Assert.Equal(e.ID, arch.EntityIdAt(0));
		var pos = (InspPos)arch.GetBoxed(0, 0);
		Assert.Equal(3, pos.X);
		Assert.Equal(4, pos.Y);
		var types = arch.All.Select(c => world.ComponentType(c.ID)).ToHashSet();
		Assert.Contains(typeof(InspPos), types);
		Assert.Contains(typeof(InspTag), types);
		Assert.Null(world.ComponentType(ulong.MaxValue));
	}

	[Fact]
	public void FormatValue_prefers_ToString_then_dumps_fields()
	{
		Assert.Equal("hp 7", EcsInspectorPlugin.FormatValue(new InspNamed { Hp = 7 }));
		Assert.Equal("X=1 Y=2", EcsInspectorPlugin.FormatValue(new InspPos { X = 1, Y = 2 }));
		Assert.Equal("{}", EcsInspectorPlugin.FormatValue(new InspTag()));
	}

	private static App MakeApp()
	{
		var app = new App(new World(), ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(1280, 720) });
		app.AddPlugin(new EcsInspectorPlugin());
		app.AddSystem((Commands c) =>
		{
			c.Spawn().Insert(new InspPos { X = 11, Y = 22 }).Insert<InspTag>();
			c.Spawn().Insert(new InspNamed { Hp = 5 });
		}).InStage(BevyStage.Startup).SingleThreaded().Build();
		app.AddSystem((Commands c, Res<ToggleRequest> req) =>
		{
			if (req.Value.Pending) { req.Value.Pending = false; c.EmitTrigger(new EcsInspectorToggle()); }
		}).InStage(BevyStage.First).SingleThreaded().Build();
		app.AddResource(new ToggleRequest());
		app.RunStartup();
		return app;
	}

	private sealed class ToggleRequest { public bool Pending; }

	private static void Toggle(App app)
	{
		app.GetResource<ToggleRequest>().Pending = true;
		app.Update();
		app.Update(); // first refresh of the spawned window
	}

	private static int Count<T>(App app) where T : struct
	{
		int n = 0;
		foreach (var _ in app.GetWorld().Query<Data<T>>()) n++;
		return n;
	}

	private static List<string> Texts(App app)
	{
		var list = new List<string>();
		foreach (var (_, t) in app.GetWorld().Query<Data<UiText>>()) list.Add(t.Ref.Value);
		return list;
	}

	[Fact]
	public void Toggle_opens_then_closes_the_window()
	{
		var app = MakeApp();
		Toggle(app);
		Assert.Equal(1, Count<EcsInspectorWindow>(app));
		Assert.Equal(1, Count<EcsInspectorList>(app));
		Assert.True(Count<EcsInspectorHeader>(app) >= 2);

		Toggle(app);
		Assert.Equal(0, Count<EcsInspectorWindow>(app));
		Assert.Equal(0, Count<EcsInspectorHeader>(app));
	}

	[Fact]
	public void Expanded_archetype_lists_entity_values_and_tag_columns()
	{
		var app = MakeApp();
		Toggle(app);
		Assert.DoesNotContain("X=11 Y=22", Texts(app));

		var arch = app.GetWorld().Archetypes.Single(a => a.Count > 0 &&
			a.All.Any(c => app.GetWorld().ComponentType(c.ID) == typeof(InspPos)));
		var state = app.GetResource<EcsInspectorState>();
		state.Expanded.Add(arch.Id);
		state.NextRefresh = 0;
		app.Update();

		var texts = Texts(app);
		Assert.Contains("X=11 Y=22", texts);
		Assert.Contains("InspPos", texts);
		Assert.Contains("InspTag", texts);
		Assert.Contains($"[{arch.EntityIdAt(0)}]", texts);
	}
}
