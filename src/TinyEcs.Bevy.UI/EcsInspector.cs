using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.UI;

// ECS inspector — a debug window listing every non-empty archetype of the world.
// Each archetype is a clickable header (entity count + its component set);
// expanding one shows a table: one row per entity, one column per component,
// a filled square for tags and the value for data components.
//
// Emit EcsInspectorToggle to open / close it. The content is rebuilt from scratch
// every RefreshMs (or on an expand/collapse click) under a scroll container whose
// ScrollPosition survives the rebuild.
//
// Host hooks are marker components, not callbacks: EcsInspectorList (the scroll
// container — attach a scrollbar), EcsInspectorHeader (archetype rows — hover
// feedback) and EcsInspectorCell (cells whose text was cut — a tooltip with
// FullText). Observe OnAdd<T> on them to decorate.

/// <summary>Trigger: opens the inspector, or closes it when open.</summary>
public struct EcsInspectorToggle { }

/// <summary>On the inspector window root.</summary>
public struct EcsInspectorWindow
{
	public ulong List, Summary;
	public ulong Content; // child of List, despawned + respawned on every refresh
	public int ListWidth;
}

/// <summary>The scroll container holding the archetype list.</summary>
public struct EcsInspectorList { public ulong Window; }

/// <summary>A clickable archetype header row.</summary>
public struct EcsInspectorHeader { public ulong Archetype; }

/// <summary>A cell whose text was truncated (or a column name); FullText is the whole value.</summary>
public struct EcsInspectorCell { public string FullText; }

public sealed class EcsInspectorState
{
	public readonly HashSet<ulong> Expanded = new(); // Archetype.Id
	public float NextRefresh;
}

public sealed class EcsInspectorStyle
{
	public TextFont Title = new() { Size = 18 };
	public TextFont Body = new() { Size = 13 };
	public TextFont Small = new() { Size = 12 };
	public TextFont Cell = new() { Size = 12 };
	// Rough average glyph width of Cell: cells are cut to width / CharWidth chars.
	public float CharWidth = 6f;

	public ClayColor Background = new(9, 12, 17, 250);
	public ClayColor Border = new(39, 45, 54, 255);
	public ClayColor HeaderBackground = new(16, 21, 29, 255);
	public ClayColor ColumnsBackground = new(12, 16, 22, 255);
	public ClayColor Text = new(232, 237, 242, 255);
	public ClayColor TextMuted = new(152, 162, 173, 255);
	public ClayColor TextDim = new(92, 102, 115, 255);
	public ClayColor Accent = new(47, 191, 174, 255);
	public ClayColor ColumnName = new(240, 184, 76, 255);
}

/// <summary>
/// Read-only access to the world's archetype storage. There is no query for
/// "every archetype"; systems taking this param should run SingleThreaded so
/// nothing mutates the columns while they are read.
/// </summary>
public sealed class WorldArchetypes : ISystemParam
{
	private World _world = null!;
	public void Initialize(App app) => _world = app.GetWorld();
	public void Fetch(App app) { }
	public SystemParamAccess GetAccess() => new();
	public IEnumerable<Archetype> All => _world.Archetypes;
	public Type? ComponentType(ulong id) => _world.ComponentType(id);
}

public sealed class EcsInspectorPlugin : IPlugin
{
	public EcsInspectorStyle Style { get; init; } = new();

	private const float RefreshMs = 1000f;
	private const int MaxWinW = 1100, MaxWinH = 800, Margin = 20, Pad = 14;
	private const int IdW = 90, MinCellW = 60, MaxCellW = 220, RowH = 20;
	private const int MaxRows = 50; // per archetype: some hold thousands of entities
	private const int ScrollGutter = 16;

	public void Build(App app)
	{
		app.AddResource(Style);
		app.AddResource(new EcsInspectorState());
		if (!app.HasResource<UiZCounter>()) app.AddResource(new UiZCounter());
		if (!app.HasResource<Time>()) app.AddResource(new Time());

		app.AddObserver((On<EcsInspectorToggle> _, Commands commands, Res<UiZCounter> z, Res<UiSurface> surface,
			Res<EcsInspectorStyle> style, ResMut<EcsInspectorState> state, Query<Data<EcsInspectorWindow>> q) =>
		{
			var open = false;
			foreach (var (ent, _) in q) { commands.Entity(ent.Ref).Despawn(); open = true; }
			if (open) return;
			Spawn(commands, z.Value, style.Value, surface.Value.LogicalSize);
			state.Value.NextRefresh = 0;
		});

		app.AddSystem((Action<Commands, Res<Time>, Res<EcsInspectorStyle>, ResMut<EcsInspectorState>, WorldArchetypes,
				Query<Data<EcsInspectorWindow>>, Query<Data<Text>>>)Refresh)
			.InStage(Stage.Update).SingleThreaded().Build();
	}

	private static void Spawn(Commands commands, UiZCounter z, EcsInspectorStyle style, System.Numerics.Vector2 surface)
	{
		int winW = (int)MathF.Min(MaxWinW, surface.X - Margin * 2);
		int winH = (int)MathF.Min(MaxWinH, surface.Y - Margin * 2);
		int listW = winW - Pad * 2;
		var rootId = commands.Spawn()
			.Insert(new Node
			{
				Display = Display.Flex,
				PositionType = PositionType.Absolute,
				FlexDirection = FlexDirection.Column,
				Left = Val.Px(Margin), Top = Val.Px(Margin),
				Width = Val.Px(winW), Height = Val.Px(winH),
				Padding = UiRect.All(Pad),
				Gap = Val.Px(6),
				Border = UiRect.All(1),
			})
			.Insert(new BackgroundColor(style.Background))
			.Insert(new BorderColor(style.Border))
			.Insert(BorderRadius.All(10))
			.Insert(Interaction.None)
			.Insert<UiMovable>()
			.Insert(new GlobalZIndex(z.Bump()))
			.Id;

		commands.AddChild(rootId, Label(commands, "ECS INSPECTOR", style.Title, style.Text, listW));
		var summary = Label(commands, string.Empty, style.Body, style.Accent, listW);
		commands.AddChild(rootId, summary);
		commands.AddChild(rootId, Label(commands,
			"each row is an entity, each column a component - a filled cell is a tag. click an archetype to expand.",
			style.Small, style.TextMuted, listW));

		var listId = commands.Spawn()
			.Insert(new Node
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Column,
				Width = Val.Px(listW),
				Height = Val.Px(winH - Pad * 2 - 80),
				Overflow = Overflow.Scroll,
			})
			.Insert(Interaction.None)
			.Insert(new ScrollPosition())
			.Insert(new EcsInspectorList { Window = rootId })
			.Id;
		commands.AddChild(rootId, listId);

		commands.Entity(rootId).Insert(new EcsInspectorWindow { List = listId, Summary = summary, ListWidth = listW });
	}

	private static void Refresh(
		Commands commands,
		Res<Time> time,
		Res<EcsInspectorStyle> style,
		ResMut<EcsInspectorState> state,
		WorldArchetypes world,
		Query<Data<EcsInspectorWindow>> windowsQ,
		Query<Data<Text>> textQ)
	{
		foreach (var (_, win) in windowsQ)
		{
			if (time.Value.Total < state.Value.NextRefresh) return;
			state.Value.NextRefresh = time.Value.Total + RefreshMs;

			var archetypes = new List<Archetype>();
			int entities = 0;
			foreach (var a in world.All)
			{
				if (a.Count == 0) continue;
				archetypes.Add(a);
				entities += a.Count;
			}
			archetypes.Sort((x, y) => y.Count.CompareTo(x.Count));

			if (textQ.TryGet(win.Ref.Summary, out var row))
			{
				var (_, text) = row;
				var summary = $"{archetypes.Count} archetypes   {entities} entities";
				if (text.Ref.Value != summary)
				{
					text.Ref.Value = summary;
					textQ.SetChanged<Text>(win.Ref.Summary);
				}
			}

			int width = win.Ref.ListWidth - ScrollGutter;
			if (win.Ref.Content != 0) commands.Entity(win.Ref.Content).Despawn();
			var contentId = commands.Spawn()
				.Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Val.Px(width), Gap = Val.Px(2) })
				.Id;
			win.Ref.Content = contentId;
			commands.AddChild(win.Ref.List, contentId);

			foreach (var a in archetypes)
				BuildArchetype(commands, world, style.Value, contentId, a, width, state.Value.Expanded.Contains(a.Id));
			return;
		}
	}

	private static void BuildArchetype(Commands commands, WorldArchetypes world, EcsInspectorStyle style,
		ulong parent, Archetype a, int width, bool expanded)
	{
		var names = new StringBuilder();
		foreach (var c in a.All)
		{
			if (names.Length > 0) names.Append("  ");
			names.Append(ComponentName(world, c.ID));
		}

		var archId = a.Id;
		var header = commands.Spawn()
			.Insert(new Node { Width = Val.Px(width), Padding = UiRect.All(4) })
			.Insert(new BackgroundColor(style.HeaderBackground))
			.Insert(new Text($"{(expanded ? "-" : "+")} {a.Count,6}   {names}"))
			.Insert(style.Body)
			.Insert(new TextColor(expanded ? style.Accent : style.Text))
			.Insert(new TextWrap(TextWrapKind.Words))
			.Insert(Interaction.None)
			.Insert<UiNoWindowDrag>()
			.Insert(new EcsInspectorHeader { Archetype = archId })
			.Observe((On<UiClick> _, ResMut<EcsInspectorState> st) =>
			{
				if (!st.Value.Expanded.Remove(archId)) st.Value.Expanded.Add(archId);
				st.Value.NextRefresh = 0;
			});
		commands.AddChild(parent, header.Id);
		if (!expanded) return;

		// Fixed cell width: wide archetypes run past the window edge.
		int cellW = Math.Clamp((width - IdW) / Math.Max(1, a.All.Length), MinCellW, MaxCellW);

		var head = Row(commands, parent, style.ColumnsBackground);
		Cell(commands, style, head, "entity", IdW, style.TextMuted);
		foreach (var c in a.All)
			Cell(commands, style, head, ComponentName(world, c.ID), cellW, style.ColumnName, alwaysTagFull: true);

		int rows = Math.Min(a.Count, MaxRows);
		for (int r = 0; r < rows; r++)
		{
			var line = Row(commands, parent, new ClayColor(0, 0, 0, 0));
			Cell(commands, style, line, $"[{a.EntityIdAt(r)}]", IdW, style.Text);
			int dataCol = 0;
			foreach (var c in a.All)
			{
				if (c.Size <= 0)
					TagCell(commands, style, line, cellW);
				else
					Cell(commands, style, line, FormatValue(a.GetBoxed(dataCol++, r)), cellW, style.Text);
			}
		}
		if (a.Count > rows)
			commands.AddChild(parent, Label(commands, $"   ... {a.Count - rows} more", style.Small, style.TextDim, width));
	}

	private static ulong Row(Commands commands, ulong parent, ClayColor bg)
	{
		var row = commands.Spawn()
			.Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Row, Height = Val.Px(RowH), AlignItems = AlignItems.Center })
			.Insert(new BackgroundColor(bg))
			.Id;
		commands.AddChild(parent, row);
		return row;
	}

	// Text is cut to fit by character count rather than an Overflow.Clip per cell:
	// one scissor per cell is a lot of render commands for a table.
	private static void Cell(Commands commands, EcsInspectorStyle style, ulong row, string text, int width, ClayColor color, bool alwaysTagFull = false)
	{
		int max = Math.Max(3, (int)((width - 6) / style.CharWidth));
		var shown = text.Length <= max ? text : string.Concat(text.AsSpan(0, max - 2), "..");
		var cell = commands.Spawn()
			.Insert(new Node { Width = Val.Px(width), Height = Val.Px(RowH), Padding = new UiRect(Val.Px(4), Val.Px(0), Val.Px(2), Val.Px(0)) })
			.Insert(new Text(shown))
			.Insert(style.Cell)
			.Insert(new TextColor(color))
			.Insert(new TextWrap(TextWrapKind.None));
		if (alwaysTagFull || shown.Length != text.Length)
			cell.Insert(new EcsInspectorCell { FullText = text });
		commands.AddChild(row, cell.Id);
	}

	private static void TagCell(Commands commands, EcsInspectorStyle style, ulong row, int width)
	{
		var cell = commands.Spawn()
			.Insert(new Node { Width = Val.Px(width), Height = Val.Px(RowH), Padding = new UiRect(Val.Px(4), Val.Px(0), Val.Px(3), Val.Px(0)) })
			.Id;
		commands.AddChild(row, cell);
		var dot = commands.Spawn()
			.Insert(new Node { Width = Val.Px(14), Height = Val.Px(14) })
			.Insert(new BackgroundColor(style.Accent))
			.Insert(BorderRadius.All(3))
			.Id;
		commands.AddChild(cell, dot);
	}

	private static ulong Label(Commands commands, string text, TextFont font, ClayColor color, int width)
		=> commands.Spawn()
			.Insert(new Node { Width = Val.Px(width) })
			.Insert(new Text(text))
			.Insert(font)
			.Insert(new TextColor(color))
			.Insert(new TextWrap(TextWrapKind.Words))
			.Id;

	private static string ComponentName(WorldArchetypes world, ulong id)
	{
		var type = world.ComponentType(id);
		return type == null ? $"#{id}" : TypeName(type);
	}

	internal static string TypeName(Type t)
	{
		if (!t.IsGenericType) return t.Name;
		var name = t.Name;
		var tick = name.IndexOf('`');
		if (tick >= 0) name = name[..tick];
		var sb = new StringBuilder(name).Append('<');
		var args = t.GetGenericArguments();
		for (int i = 0; i < args.Length; i++)
		{
			if (i > 0) sb.Append(',');
			sb.Append(TypeName(args[i]));
		}
		return sb.Append('>').ToString();
	}

	/// <summary>
	/// A value's own ToString, or its fields as "name=value" when the type keeps
	/// the default (type-name) ToString. Reflection: an AOT build may show fewer
	/// fields where the trimmer removed their metadata.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Debug view; trimmed fields just don't show.")]
	public static string FormatValue(object value)
	{
		var type = value.GetType();
		var s = value.ToString() ?? string.Empty;
		if (s != type.ToString()) return s;

		var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (fields.Length == 0) return "{}";
		var sb = new StringBuilder();
		foreach (var f in fields)
		{
			if (sb.Length > 0) sb.Append(' ');
			// Auto-property backing fields are named "<Name>k__BackingField".
			var name = f.Name.StartsWith('<') ? f.Name[1..f.Name.IndexOf('>')] : f.Name;
			sb.Append(name).Append('=').Append(f.GetValue(value)?.ToString() ?? "null");
			if (sb.Length > 256) break;
		}
		return sb.ToString();
	}
}
