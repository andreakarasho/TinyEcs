using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Widgets;

namespace TinyEcs.UI;

/// <summary>
/// Tracks Clay UI surfaces, context lifetime, and layout scheduling inside the ECS world.
/// </summary>
public unsafe sealed class ClayUiState : IDisposable
{
	private readonly List<Action<ClayUiLayoutContext>> _layoutRoots = new();
	private ClayUiOptions _options = ClayUiOptions.Default;
	private World? _world;
	private bool _useEntityHierarchy;
	private readonly HashSet<uint> _hoveredElementIds = new();
	private uint _activePointerElementId;
	private bool _hasActivePointerElement;

	private ClayArenaHandle _arenaHandle;
	private bool _hasArena;
	private Clay_Context* _context;
	private Clay_RenderCommandArray _lastRenderCommands;
	private uint _allocatedArenaSize;
	private bool _disposed;
	private static bool _measureTextRegistered;

	public ClayUiState()
	{
		_useEntityHierarchy = _options.UseEntityHierarchy;
		HasPendingLayoutPass = true;
	}

	/// <summary>
	/// Current configuration applied by the UI plugin.
	/// </summary>
	public ClayUiOptions Options => _options;

	/// <summary>
	/// Registered root layout callbacks. Each callback will be invoked during the layout pass.
	/// </summary>
	public IReadOnlyList<Action<ClayUiLayoutContext>> LayoutRoots => _layoutRoots;

	/// <summary>
	/// Indicates whether a layout pass should be executed on the next frame.
	/// </summary>
	public bool HasPendingLayoutPass { get; private set; }

	/// <summary>
	/// Most recent Clay render commands produced by the layout pass.
	/// </summary>
	public ReadOnlySpan<Clay_RenderCommand> RenderCommands
	{
		get
		{
			if (_lastRenderCommands.internalArray is null || _lastRenderCommands.length <= 0)
				return ReadOnlySpan<Clay_RenderCommand>.Empty;

			return new ReadOnlySpan<Clay_RenderCommand>(_lastRenderCommands.internalArray, _lastRenderCommands.length);
		}
	}

	internal Clay_Context* Context => _context;

	public void ApplyOptions(ClayUiOptions options)
	{
		ObjectDisposedException.ThrowIf(_disposed, typeof(ClayUiState));

		var optionsChanged = !_options.Equals(options);
		var requiresRecreate = ShouldRecreateContext(options);

		_options = options;
		SetEntityHierarchyEnabled(options.UseEntityHierarchy);

		if (requiresRecreate)
		{
			RecreateContext();
		}
		else if (_context is not null)
		{
			Clay.SetCurrentContext(_context);
			Clay.SetLayoutDimensions(options.LayoutDimensions);
			Clay.SetDebugModeEnabled(options.EnableDebugMode);
		}

		if (optionsChanged && options.ForceLayoutOnOptionsChange)
		{
			RequestLayoutPass();
		}
	}

	public void RegisterRoot(Action<ClayUiLayoutContext> build)
	{
		ObjectDisposedException.ThrowIf(_disposed, typeof(ClayUiState));
		ArgumentNullException.ThrowIfNull(build);
		_layoutRoots.Add(build);
		RequestLayoutPass();
	}

	public bool RemoveRoot(Action<ClayUiLayoutContext> build)
	{
		ObjectDisposedException.ThrowIf(_disposed, typeof(ClayUiState));
		ArgumentNullException.ThrowIfNull(build);

		var removed = _layoutRoots.Remove(build);
		if (removed)
		{
			RequestLayoutPass();
		}

		return removed;
	}

	public void ClearRoots()
	{
		ObjectDisposedException.ThrowIf(_disposed, typeof(ClayUiState));

		if (_layoutRoots.Count == 0)
			return;

		_layoutRoots.Clear();
		RequestLayoutPass();
	}

	public void RequestLayoutPass() => HasPendingLayoutPass = true;

	public void CompleteLayoutPass() => HasPendingLayoutPass = false;

	internal void AttachWorld(World world)
	{
		ArgumentNullException.ThrowIfNull(world);
		_world = world;
	}

	public ClayUiLayoutContext CreateContext() => new(this);

	public void RunLayoutPass(
		Query<Data<UiNode>, Filter<Without<Parent>>> rootNodes,
		Query<Data<UiNode>> allNodes,
		Query<Data<UiText>> uiTexts,
		Query<Data<Children>> childLists,
		Query<Data<FloatingWindowState>> floatingWindows,
		ResMut<UiWindowOrder> windowOrder,
		Local<List<ulong>> windows)
	{
		ObjectDisposedException.ThrowIf(_disposed, typeof(ClayUiState));

		var shouldRun = _options.AutoRunLayout || HasPendingLayoutPass;
		if (!shouldRun)
			return;

		EnsureContext();

		Clay.SetCurrentContext(_context);
		Clay.SetDebugModeEnabled(_options.EnableDebugMode);
		Clay.SetLayoutDimensions(_options.LayoutDimensions);

		// Restore scroll positions from previous frame BEFORE BeginLayout clears them
		RestoreScrollPositions(allNodes);

		Clay.BeginLayout();

		var context = CreateContext();
		if (_useEntityHierarchy)
		{
			ClayUiEntityLayout.Build(context, rootNodes, allNodes, uiTexts, childLists, floatingWindows, windowOrder, windows);
		}

		foreach (var build in _layoutRoots)
		{
			build(context);
		}

		_lastRenderCommands = ClayInterop.Clay_EndLayout();
		// Clay.ClayStrings.Clear();
		if (!_options.AutoRunLayout)
			CompleteLayoutPass();
		else
			HasPendingLayoutPass = false;
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		DisposeContext();
		_hoveredElementIds.Clear();
		_hasActivePointerElement = false;
		_disposed = true;
		GC.SuppressFinalize(this);
	}

	private void EnsureContext()
	{
		if (_context is not null)
			return;

		var desiredArenaSize = ResolveArenaSize(_options.ArenaSize);
		_arenaHandle = Clay.CreateArena(desiredArenaSize);
		_hasArena = true;
		_allocatedArenaSize = desiredArenaSize;

		_context = Clay.Initialize(_arenaHandle, _options.LayoutDimensions, IntPtr.Zero);
		EnsureMeasureTextFunction();
		Clay.SetCurrentContext(_context);
		Clay.SetDebugModeEnabled(_options.EnableDebugMode);
	}

	private bool ShouldRecreateContext(ClayUiOptions options)
	{
		if (_context is null)
			return false;

		var desired = ResolveArenaSize(options.ArenaSize);
		return desired != _allocatedArenaSize;
	}

	private static uint ResolveArenaSize(uint requestedSize)
	{
		var min = Clay.MinMemorySize();
		if (requestedSize == 0)
			return min;

		return requestedSize < min ? min : requestedSize;
	}

	private void RecreateContext()
	{
		DisposeContext();
		EnsureContext();
		RequestLayoutPass();
	}

	private void DisposeContext()
	{
		if (_context is not null)
		{
			Clay.SetCurrentContext(null);
			_context = null;
		}

		if (_hasArena)
		{
			_arenaHandle.Dispose();
			_arenaHandle = default;
			_hasArena = false;
			_allocatedArenaSize = 0;
		}

		_lastRenderCommands = default;
	}

	// Element registry methods removed - now using ECS queries directly in ClayUiSystems
	// No need to maintain a separate dictionary when we can query UiNode components by their Clay ID

	internal int GetHoveredElementCount() => _hoveredElementIds.Count;

	internal void CopyHoveredElementIds(Span<uint> destination)
	{
		var index = 0;
		foreach (var key in _hoveredElementIds)
		{
			if (index >= destination.Length)
				break;
			destination[index++] = key;
		}
	}

	internal void BeginHoverUpdate()
	{
		_hoveredElementIds.Clear();
	}

	internal void AddHoveredElement(uint key)
	{
		if (key != 0)
			_hoveredElementIds.Add(key);
	}

	internal bool IsElementHovered(uint key) => _hoveredElementIds.Contains(key);

	internal void SetActivePointerTarget(uint key)
	{
		if (key == 0)
		{
			_hasActivePointerElement = false;
			_activePointerElementId = 0;
			return;
		}

		_hasActivePointerElement = true;
		_activePointerElementId = key;
	}

	internal bool TryConsumeActivePointerTarget(out uint key)
	{
		if (_hasActivePointerElement)
		{
			key = _activePointerElementId;
			_hasActivePointerElement = false;
			_activePointerElementId = 0;
			return true;
		}

		key = 0;
		return false;
	}

	internal bool TryPeekActivePointerTarget(out uint key)
	{
		if (_hasActivePointerElement)
		{
			key = _activePointerElementId;
			return true;
		}

		key = 0;
		return false;
	}

	private static unsafe void EnsureMeasureTextFunction()
	{
		if (_measureTextRegistered)
			return;

		delegate* unmanaged[Cdecl]<Clay_StringSlice, Clay_TextElementConfig*, void*, Clay_Dimensions> measurePtr = &MeasureTextCallback;
		Clay.SetMeasureTextFunction((nint)measurePtr);
		_measureTextRegistered = true;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe Clay_Dimensions MeasureTextCallback(Clay_StringSlice slice, Clay_TextElementConfig* config, void* userData)
	{
		var bytes = new ReadOnlySpan<byte>((byte*)slice.chars, slice.length);
		var text = bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);

		ushort fontSize = 16;
		ushort lineHeight = 0;
		ushort letterSpacing = 0;

		if (config is not null)
		{
			if (config->fontSize > 0)
				fontSize = config->fontSize;
			if (config->lineHeight > 0)
				lineHeight = config->lineHeight;
			letterSpacing = config->letterSpacing;
		}

		var effectiveLineHeight = lineHeight > 0 ? lineHeight : fontSize;
		var glyphAdvance = (fontSize * 0.6f) + letterSpacing;
		var width = text.Length == 0 ? 0f : MathF.Max(0f, glyphAdvance * text.Length);

		return new Clay_Dimensions(width, effectiveLineHeight);
	}

	internal void SetEntityHierarchyEnabled(bool enabled)
	{
		if (_useEntityHierarchy == enabled)
			return;
		_useEntityHierarchy = enabled;
		RequestLayoutPass();
	}

	internal bool IsEntityHierarchyEnabled => _useEntityHierarchy;

	private void RestoreScrollPositions(Query<Data<UiNode>> allNodes)
	{
		// Query all nodes with clipping enabled and update their childOffset directly
		// from the previous frame's scroll position (before BeginLayout clears it)
		foreach (var (entityId, nodePtr) in allNodes)
		{
			ref var node = ref nodePtr.Ref;
			if (node.Declaration.clip.vertical || node.Declaration.clip.horizontal)
			{
				if (node.Declaration.id.id != 0)
				{
					var scrollData = Clay.GetScrollContainerData(node.Declaration.id);
					if (scrollData.found && scrollData.scrollPosition != null)
					{
						// Update the node's childOffset directly with the previous frame's scroll position
						node.Declaration.clip.childOffset = *scrollData.scrollPosition;
					}
				}
			}
		}
	}
}

public readonly record struct ClayUiOptions
{
	public ClayUiOptions()
	{
	}

	public Clay_Dimensions LayoutDimensions { get; init; } = new Clay_Dimensions(1920f, 1080f);
	public uint ArenaSize { get; init; }
	public bool AutoRegisterDefaultSystems { get; init; } = true;
	public bool AutoRunLayout { get; init; } = true;
	public bool ForceLayoutOnOptionsChange { get; init; } = true;
	public bool EnableDebugMode { get; init; }
	public bool UseEntityHierarchy { get; init; } = true;
	public bool AutoCreatePointerState { get; init; } = true;

	public static ClayUiOptions Default => new();
}

public readonly record struct ClayUiLayoutContext(ClayUiState State)
{
	public ClayUiOptions Options => State.Options;

	public ReadOnlySpan<Clay_RenderCommand> RenderCommands => State.RenderCommands;

	public unsafe Clay_Context* ClayContext => State.Context;
}
