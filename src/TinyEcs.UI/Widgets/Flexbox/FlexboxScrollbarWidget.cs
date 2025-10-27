using System;
using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Flexbox;

/// <summary>
/// Component to track scrollbar state.
/// </summary>
public struct FlexboxScrollbarState
{
	/// <summary>Current scroll position (0.0 to 1.0).</summary>
	public float ScrollPosition;

	/// <summary>Ratio of visible content to total content (0.0 to 1.0).</summary>
	public float VisibleRatio;

	/// <summary>Whether the scrollbar handle is being dragged.</summary>
	public bool IsDragging;

	/// <summary>Offset from handle top to pointer position when drag started.</summary>
	public float DragOffset;

	/// <summary>Orientation of the scrollbar.</summary>
	public ScrollbarOrientation Orientation;
}

/// <summary>
/// Links to parts of the scrollbar for interaction updates.
/// </summary>
public struct FlexboxScrollbarLinks
{
	public ulong TrackId;
	public ulong HandleId;
}

/// <summary>
/// Scrollbar orientation.
/// </summary>
public enum ScrollbarOrientation
{
	Vertical,
	Horizontal
}

/// <summary>
/// Style configuration for scrollbar widgets.
/// </summary>
public struct FlexboxScrollbarStyle
{
	public float TrackWidth;        // Width for vertical, height for horizontal
	public float MinHandleSize;     // Minimum handle size in pixels
	public Vector4 TrackColor;
	public Vector4 HandleColor;
	public Vector4 HandleHoverColor;
	public Vector4 HandleActiveColor;
	public float HandleBorderRadius;
	public float HandlePadding;     // Padding between handle and track edges

	public static FlexboxScrollbarStyle DefaultVertical()
	{
		return new FlexboxScrollbarStyle
		{
			TrackWidth = 12f,
			MinHandleSize = 40f,
			TrackColor = new Vector4(0.2f, 0.22f, 0.25f, 1f),    // Slightly lighter than window bg
			HandleColor = new Vector4(0.5f, 0.53f, 0.56f, 0.8f),  // Medium gray, semi-transparent
			HandleHoverColor = new Vector4(0.6f, 0.63f, 0.66f, 0.9f), // Lighter on hover
			HandleActiveColor = new Vector4(0.7f, 0.73f, 0.76f, 1f), // Even lighter when dragging
			HandleBorderRadius = 6f,
			HandlePadding = 2f
		};
	}

	public static FlexboxScrollbarStyle DefaultHorizontal()
	{
		var style = DefaultVertical();
		// Same values work for both orientations
		return style;
	}
}

/// <summary>
/// Creates scrollbar widgets for the Flexbox UI system.
/// Scrollbars have a track and a draggable handle.
/// </summary>
public static class FlexboxScrollbarWidget
{
	/// <summary>
	/// Creates a vertical scrollbar.
	/// </summary>
	/// <param name="commands">Entity commands for spawning.</param>
	/// <param name="height">Total height of the scrollbar track.</param>
	/// <param name="visibleRatio">Ratio of visible content (0.0-1.0). Determines handle size.</param>
	/// <param name="style">Style configuration.</param>
	/// <param name="parent">Optional parent entity ID.</param>
	/// <returns>Entity commands for the scrollbar container.</returns>
	public static FlexboxScrollbarHandle CreateVertical(
		Commands commands,
		float height,
		float visibleRatio = 1.0f,
		FlexboxScrollbarStyle? style = null,
		ulong parent = 0)
	{
		var scrollbarStyle = style ?? FlexboxScrollbarStyle.DefaultVertical();
		visibleRatio = Math.Clamp(visibleRatio, 0.01f, 1.0f);

		// Create scrollbar container (track)
		var trackId = commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Column,
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Center,
				PositionType = PositionType.Relative,
				Width = FlexValue.Points(scrollbarStyle.TrackWidth),
				Height = FlexValue.Points(height),
				BackgroundColor = scrollbarStyle.TrackColor,
				PaddingTop = scrollbarStyle.HandlePadding,
				PaddingBottom = scrollbarStyle.HandlePadding
			})
			.Insert(new FlexboxInteractive())
			.Id;

		if (parent != 0)
			commands.Entity(trackId).Insert(new FlexboxNodeParent(parent));

		// Calculate handle height
		var availableHeight = height - (scrollbarStyle.HandlePadding * 2f);
		var handleHeight = Math.Max(scrollbarStyle.MinHandleSize, availableHeight * visibleRatio);

		// Create handle with absolute positioning
		var handleId = commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				PositionType = PositionType.Absolute,
				Top = FlexValue.Points(scrollbarStyle.HandlePadding), // Start at top
				Left = FlexValue.Points(scrollbarStyle.HandlePadding),
				Width = FlexValue.Points(scrollbarStyle.TrackWidth - scrollbarStyle.HandlePadding * 2f),
				Height = FlexValue.Points(handleHeight),
				BackgroundColor = scrollbarStyle.HandleColor,
				BorderRadius = scrollbarStyle.HandleBorderRadius
			})
			.Insert(new FlexboxInteractive())
			.Insert(new FlexboxNodeParent(trackId))
			.Id;

		// Create scrollbar state component
		var scrollbarState = new FlexboxScrollbarState
		{
			ScrollPosition = 0f,
			VisibleRatio = visibleRatio,
			IsDragging = false,
			DragOffset = 0f,
			Orientation = ScrollbarOrientation.Vertical
		};

		commands.Entity(trackId).Insert(scrollbarState);
		commands.Entity(trackId).Insert(new FlexboxScrollbarLinks
		{
			TrackId = trackId,
			HandleId = handleId
		});
		commands.Entity(trackId).Insert(scrollbarStyle);

		return new FlexboxScrollbarHandle
		{
			ScrollbarId = trackId,
			TrackId = trackId,
			HandleId = handleId
		};
	}

	/// <summary>
	/// Creates a horizontal scrollbar.
	/// </summary>
	/// <param name="commands">Entity commands for spawning.</param>
	/// <param name="width">Total width of the scrollbar track.</param>
	/// <param name="visibleRatio">Ratio of visible content (0.0-1.0). Determines handle size.</param>
	/// <param name="style">Style configuration.</param>
	/// <param name="parent">Optional parent entity ID.</param>
	/// <returns>Entity commands for the scrollbar container.</returns>
	public static FlexboxScrollbarHandle CreateHorizontal(
		Commands commands,
		float width,
		float visibleRatio = 1.0f,
		FlexboxScrollbarStyle? style = null,
		ulong parent = 0)
	{
		var scrollbarStyle = style ?? FlexboxScrollbarStyle.DefaultHorizontal();
		visibleRatio = Math.Clamp(visibleRatio, 0.01f, 1.0f);

		// Create scrollbar container (track)
		var trackId = commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Row,
				JustifyContent = Justify.FlexStart,
				AlignItems = Align.Center,
				PositionType = PositionType.Relative,
				Width = FlexValue.Points(width),
				Height = FlexValue.Points(scrollbarStyle.TrackWidth),
				BackgroundColor = scrollbarStyle.TrackColor,
				PaddingLeft = scrollbarStyle.HandlePadding,
				PaddingRight = scrollbarStyle.HandlePadding
			})
			.Insert(new FlexboxInteractive())
			.Id;

		if (parent != 0)
			commands.Entity(trackId).Insert(new FlexboxNodeParent(parent));

		// Calculate handle width
		var availableWidth = width - (scrollbarStyle.HandlePadding * 2f);
		var handleWidth = Math.Max(scrollbarStyle.MinHandleSize, availableWidth * visibleRatio);

		// Create handle with absolute positioning
		var handleId = commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(scrollbarStyle.HandlePadding), // Start at left
				Top = FlexValue.Points(scrollbarStyle.HandlePadding),
				Width = FlexValue.Points(handleWidth),
				Height = FlexValue.Points(scrollbarStyle.TrackWidth - scrollbarStyle.HandlePadding * 2f),
				BackgroundColor = scrollbarStyle.HandleColor,
				BorderRadius = scrollbarStyle.HandleBorderRadius
			})
			.Insert(new FlexboxInteractive())
			.Insert(new FlexboxNodeParent(trackId))
			.Id;

		// Create scrollbar state component
		var scrollbarState = new FlexboxScrollbarState
		{
			ScrollPosition = 0f,
			VisibleRatio = visibleRatio,
			IsDragging = false,
			DragOffset = 0f,
			Orientation = ScrollbarOrientation.Horizontal
		};

		commands.Entity(trackId).Insert(scrollbarState);
		commands.Entity(trackId).Insert(new FlexboxScrollbarLinks
		{
			TrackId = trackId,
			HandleId = handleId
		});
		commands.Entity(trackId).Insert(scrollbarStyle);

		return new FlexboxScrollbarHandle
		{
			ScrollbarId = trackId,
			TrackId = trackId,
			HandleId = handleId
		};
	}
}

/// <summary>
/// Return value from FlexboxScrollbarWidget.Create methods.
/// Provides IDs for the scrollbar and its parts.
/// </summary>
public struct FlexboxScrollbarHandle
{
	public ulong ScrollbarId;  // Container entity with state
	public ulong TrackId;      // Track entity
	public ulong HandleId;     // Draggable handle entity
}
