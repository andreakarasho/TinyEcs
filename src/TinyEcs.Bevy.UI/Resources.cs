using System.Numerics;
using Clay;

namespace TinyEcs.Bevy.UI;

public sealed class UiScale { public float Value = 1f; }

public sealed class UiSurface
{
	public Vector2 LogicalSize = new(1280, 720);
	public Vector2 PhysicalSize = new(1280, 720);
}

public sealed class UiPointer
{
	public Vector2 Position;
	public bool Down;
	public bool WasDown;
}

public sealed class UiRenderCommands
{
	public RenderCommand[] Buffer = Array.Empty<RenderCommand>();
	public int Count;

	public ReadOnlySpan<RenderCommand> Span => Buffer.AsSpan(0, Count);
}

public sealed class UiClayContext
{
	internal ClayContext Context = null!;
	internal RenderCommand[] LastCommands = Array.Empty<RenderCommand>();
	internal readonly Dictionary<uint, ulong> ClayToEntity = new();
	// Clay ElementId -> entity for elements declared with Overflow.Scroll.
	// Renderers consult this each frame to draw scrollbar overlays.
	public readonly Dictionary<uint, ulong> ScrollClayToEntity = new();
	// entity -> last ScrollPosition value we wrote to its component last frame.
	// Used to detect user mutations and push them back to Clay.
	internal readonly Dictionary<ulong, Vector2> LastSyncedScroll = new();
	internal ulong HoveredEntity;
	// Entity the pointer-down began on this gesture. Reset on release. Used to gate
	// UiClick so a click only fires when press and release land on the same entity.
	internal ulong PressedEntity;
	public float DeltaTime;
	public Vector2 ScrollDelta;
	public bool EnableDragScrolling;

	public ScrollContainerData GetScrollContainerData(uint clayId)
		=> Context.GetScrollContainerData(new ElementId { Id = clayId });

	/// Programmatically scroll a container by entity id. `offset` follows Bevy's
	/// convention: positive Y = scrolled down (content shifts up).
	public void SetScrollPosition(ulong entityId, Vector2 offset)
	{
		var clayId = ElementId.HashNumber((uint)entityId);
		Context.SetScrollPosition(clayId, offset);
	}
}

public sealed class UiTextureRegistry
{
	private readonly Dictionary<uint, object> _byId = new();
	private uint _next = 1;

	public uint Register(object texture)
	{
		var id = _next++;
		_byId[id] = texture;
		return id;
	}

	public bool TryGet(uint id, out object texture)
	{
		if (_byId.TryGetValue(id, out var t)) { texture = t; return true; }
		texture = null!;
		return false;
	}
}

/// Exposes the underlying TinyEcs World inside a system. The Bevy app schedules
/// systems by ISystemParam list; this thin wrapper lets us reach the World
/// without bypassing the scheduler.
public sealed class WorldParam : ISystemParam
{
	private TinyEcs.World _world = null!;

	public TinyEcs.World World => _world;

	public void Initialize(App app) => _world = app.GetWorld();
	public void Fetch(App app) => _world = app.GetWorld();
	public SystemParamAccess GetAccess() => new();
}

public sealed class UiFontRegistry
{
	private readonly Dictionary<ushort, object> _byId = new();

	public void Register(ushort id, object font) => _byId[id] = font;
	public bool TryGet(ushort id, out object font)
	{
		if (_byId.TryGetValue(id, out var f)) { font = f; return true; }
		font = null!;
		return false;
	}
}
