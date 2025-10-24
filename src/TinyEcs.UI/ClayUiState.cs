using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Clay_cs;

namespace TinyEcs.UI;

/// <summary>
/// Resource containing Clay UI context and state data.
/// </summary>
public unsafe sealed class ClayUiState : IDisposable
{
	public ClayUiOptions Options;
	public bool UseEntityHierarchy;
	public bool HasPendingLayoutPass;

	internal readonly HashSet<uint> HoveredElementIds = new();
	internal uint ActivePointerElementId;
	internal bool HasActivePointerElement;

	internal ClayArenaHandle ArenaHandle;
	internal bool HasArena;
	internal Clay_Context* Context;
	internal Clay_RenderCommandArray LastRenderCommands;
	internal uint AllocatedArenaSize;

	private bool _disposed;

	public ClayUiState()
	{
		Options = ClayUiOptions.Default;
		UseEntityHierarchy = Options.UseEntityHierarchy;
		HasPendingLayoutPass = true;
	}

	/// <summary>
	/// Most recent Clay render commands produced by the layout pass.
	/// </summary>
	public ReadOnlySpan<Clay_RenderCommand> RenderCommands
	{
		get
		{
			if (LastRenderCommands.internalArray is null || LastRenderCommands.length <= 0)
				return ReadOnlySpan<Clay_RenderCommand>.Empty;

			return new ReadOnlySpan<Clay_RenderCommand>(LastRenderCommands.internalArray, LastRenderCommands.length);
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		if (Context is not null)
		{
			Clay.SetCurrentContext(null);
			Context = null;
		}

		if (HasArena)
		{
			ArenaHandle.Dispose();
			ArenaHandle = default;
			HasArena = false;
			AllocatedArenaSize = 0;
		}

		LastRenderCommands = default;
		HoveredElementIds.Clear();
		HasActivePointerElement = false;
		_disposed = true;
		GC.SuppressFinalize(this);
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
