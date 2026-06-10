using System.Numerics;
using Clay;

namespace TinyEcs.Bevy.UI;

public sealed class UiScale { public float Value = 1f; }

/// <summary>The ITextMeasurer the layout pass was initialized with — exposed
/// so widgets (e.g. TextField caret math) measure with the same metrics Clay
/// laid the glyphs out with.</summary>
public sealed class UiTextMeasure { public ITextMeasurer Measurer = null!; }

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
	// Previous-frame cursor position, latched by InteractionSystem. Powers UiMove
	// delta. Not host-fed.
	public Vector2 LastPosition;
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
	// Reusable buffer for the most recent frame's render commands. LayoutSystem
	// copies Clay's span into this and bumps Count; consumers read via the
	// LastCommands span getter. No per-frame allocation.
	internal RenderCommand[] LastCommandsBuffer = Array.Empty<RenderCommand>();
	internal int LastCommandsCount;
	public ReadOnlySpan<RenderCommand> LastCommands => LastCommandsBuffer.AsSpan(0, LastCommandsCount);
	internal readonly Dictionary<uint, ulong> ClayToEntity = new();
	// Clay ElementId -> entity for elements declared with Overflow.Scroll.
	// Renderers consult this each frame to draw scrollbar overlays.
	public readonly Dictionary<uint, ulong> ScrollClayToEntity = new();
	// entity -> last ScrollPosition value we wrote to its component last frame.
	// Used to detect user mutations and push them back to Clay.
	internal readonly Dictionary<ulong, Vector2> LastSyncedScroll = new();
	// Topmost interactive entity under the pointer this frame (0 = none). Public
	// so host event routers can derive DOM-style enter/leave at subtree
	// boundaries instead of per-frame hovered-target flips.
	public ulong HoveredEntity;
	// Entity the pointer-down began on this gesture. Reset on release. Used to gate
	// UiClick so a click only fires when press and release land on the same entity.
	internal ulong PressedEntity;
	// Last UiClick target + the Time.Total second it landed on. Powers
	// UiDoubleClick synthesis: a second UiClick on the same entity within
	// DoubleClickWindow seconds emits UiDoubleClick and clears the latch.
	internal ulong LastClickEntity;
	internal float LastClickTime;
	// Window for UiDoubleClick synthesis (seconds). 0.35s matches the
	// platform double-click default used by most legacy clients.
	public float DoubleClickWindow = 0.35f;
	// Optional pixel-perfect hit-test hook. When set, InteractionSystem calls it
	// for each pointer-over candidate (entity, cursor pos, element box); returning
	// false treats the cursor as missing that element — e.g. a transparent sprite
	// pixel — and the hover falls through to the element behind. Keeps Bevy.UI
	// asset-agnostic while letting the host reject clicks on transparent pixels.
	public Func<ulong, Vector2, BoundingBox, bool>? PixelHitTest;
	public Vector2 ScrollDelta;
	public bool EnableDragScrolling;

	public ScrollContainerData GetScrollContainerData(uint clayId)
		=> Context.GetScrollContainerData(new ElementId { Id = clayId });

	/// Last-frame layout bounding box for an entity, by entity id. Returns false
	/// when the entity emitted no element last frame. Works for layout-only nodes
	/// (e.g. an empty scroll/clip container) that paint nothing and therefore have
	/// no render command / ComputedNode — the host can still hit-test them.
	public bool TryGetElementBoundingBox(ulong entityId, out BoundingBox box)
	{
		var data = Context.GetElementData(UiClayId.Of(entityId));
		box = data.BoundingBox;
		return data.Found;
	}

	/// Programmatically scroll a container by entity id. `offset` follows Bevy's
	/// convention: positive Y = scrolled down (content shifts up).
	public void SetScrollPosition(ulong entityId, Vector2 offset)
	{
		var clayId = UiClayId.Of(entityId);
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
