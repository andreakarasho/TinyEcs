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
/// Systems for managing Clay UI layout passes and context lifecycle.
/// </summary>
public static unsafe class ClayLayoutSystems
{
	private static bool _measureTextRegistered;

	public static void RunLayoutPass(
		ResMut<ClayUiState> stateParam,
		Query<Data<UiNode>, Filter<Without<Parent>>> rootNodes,
		Query<Data<UiNode>> allNodes,
		Query<Data<UiText>> uiTexts,
		Query<Data<Children>> childLists,
		Query<Data<FloatingWindowState>> floatingWindows,
		ResMut<UiWindowOrder> windowOrder,
		Local<HashSet<ulong>> windows)
	{
		ref var state = ref stateParam.Value;

		EnsureContext(ref state);

		Clay.SetCurrentContext(state.Context);
		Clay.SetDebugModeEnabled(state.Options.EnableDebugMode);
		Clay.SetLayoutDimensions(state.Options.LayoutDimensions);

		Clay.BeginLayout();

		if (state.UseEntityHierarchy)
		{
			ClayUiEntityLayout.Build(ref state, rootNodes, allNodes, uiTexts, childLists, floatingWindows, windowOrder, windows);
		}

		state.LastRenderCommands = ClayInterop.Clay_EndLayout();
	}

	public static void ApplyOptions(ResMut<ClayUiState> stateParam, ClayUiOptions options)
	{
		ref var state = ref stateParam.Value;

		var requiresRecreate = ShouldRecreateContext(ref state, options);

		state.Options = options;
		state.UseEntityHierarchy = options.UseEntityHierarchy;

		if (requiresRecreate)
		{
			RecreateContext(ref state);
		}
		else if (state.Context is not null)
		{
			Clay.SetCurrentContext(state.Context);
			Clay.SetLayoutDimensions(options.LayoutDimensions);
			Clay.SetDebugModeEnabled(options.EnableDebugMode);
		}
	}

	private static void EnsureContext(ref ClayUiState state)
	{
		if (state.Context is not null)
			return;

		var desiredArenaSize = ResolveArenaSize(state.Options.ArenaSize);
		state.ArenaHandle = Clay.CreateArena(desiredArenaSize);
		state.HasArena = true;
		state.AllocatedArenaSize = desiredArenaSize;

		state.Context = Clay.Initialize(state.ArenaHandle, state.Options.LayoutDimensions, IntPtr.Zero);
		EnsureMeasureTextFunction();
		Clay.SetCurrentContext(state.Context);
		Clay.SetDebugModeEnabled(state.Options.EnableDebugMode);
	}

	private static bool ShouldRecreateContext(ref ClayUiState state, ClayUiOptions options)
	{
		if (state.Context is null)
			return false;

		var desired = ResolveArenaSize(options.ArenaSize);
		return desired != state.AllocatedArenaSize;
	}

	private static uint ResolveArenaSize(uint requestedSize)
	{
		var min = Clay.MinMemorySize();
		if (requestedSize == 0)
			return min;

		return requestedSize < min ? min : requestedSize;
	}

	private static void RecreateContext(ref ClayUiState state)
	{
		DisposeContext(ref state);
		EnsureContext(ref state);
	}

	private static void DisposeContext(ref ClayUiState state)
	{
		if (state.Context is not null)
		{
			Clay.SetCurrentContext(null);
			state.Context = null;
		}

		if (state.HasArena)
		{
			state.ArenaHandle.Dispose();
			state.ArenaHandle = default;
			state.HasArena = false;
			state.AllocatedArenaSize = 0;
		}

		state.LastRenderCommands = default;
	}

	private static void EnsureMeasureTextFunction()
	{
		if (_measureTextRegistered)
			return;

		delegate* unmanaged[Cdecl]<Clay_StringSlice, Clay_TextElementConfig*, void*, Clay_Dimensions> measurePtr = &MeasureTextCallback;
		Clay.SetMeasureTextFunction((nint)measurePtr);
		_measureTextRegistered = true;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static Clay_Dimensions MeasureTextCallback(Clay_StringSlice slice, Clay_TextElementConfig* config, void* userData)
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
}
