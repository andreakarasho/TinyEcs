using System;
using System.Collections.Generic;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Component that defines the z-index (depth ordering) of a UI element.
/// Higher values are rendered on top and receive interactions first.
/// Equivalent to Bevy's ZIndex component.
/// </summary>
public struct ZIndex
{
	/// <summary>
	/// The z-index value. Higher values appear on top.
	/// Default is 0. Negative values are valid and appear below default elements.
	/// </summary>
	public int Value;

	public ZIndex(int value)
	{
		Value = value;
	}

	public static implicit operator ZIndex(int value) => new(value);
	public static ZIndex Default() => new(0);
}

/// <summary>
/// Global z-index that applies to an entire subtree of UI nodes.
/// Inherited by all descendants unless they have their own GlobalZIndex.
/// Similar to Bevy's GlobalZIndex.
/// </summary>
public struct GlobalZIndex
{
	/// <summary>
	/// The global z-index value. Higher values appear on top.
	/// </summary>
	public int Value;

	public GlobalZIndex(int value)
	{
		Value = value;
	}

	public static implicit operator GlobalZIndex(int value) => new(value);
}

/// <summary>
/// Entry in the UI stack representing a single UI node with its depth information.
/// </summary>
public readonly struct UiStackEntry
{
	/// <summary>Entity ID of the UI node</summary>
	public readonly ulong EntityId;

	/// <summary>Combined z-index (global + local)</summary>
	public readonly int ZIndex;

	/// <summary>Global z-index (inherited from parent tree)</summary>
	public readonly int GlobalZIndex;

	/// <summary>Local z-index (specific to this entity)</summary>
	public readonly int LocalZIndex;

	public UiStackEntry(ulong entityId, int zIndex, int globalZIndex, int localZIndex)
	{
		EntityId = entityId;
		ZIndex = zIndex;
		GlobalZIndex = globalZIndex;
		LocalZIndex = localZIndex;
	}
}

/// <summary>
/// Resource that maintains the current UI stack, containing all UI nodes ordered by their depth (back-to-front).
///
/// The first entry is the furthest node from the camera and is the first one to get rendered,
/// while the last entry is the closest to the camera and receives interactions first.
///
/// This resource is updated automatically by the UI stack system each frame.
/// Equivalent to Bevy's UiStack resource.
/// </summary>
public class UiStack
{
	/// <summary>
	/// List of UI nodes ordered from back to front (lowest to highest z-index).
	/// The last entry is the topmost element that receives pointer events first.
	/// </summary>
	public List<UiStackEntry> Entries { get; private set; }

	public UiStack()
	{
		Entries = new List<UiStackEntry>(capacity: 256);
	}

	/// <summary>
	/// Clears the stack in preparation for rebuilding.
	/// Called at the start of each frame by the stack update system.
	/// </summary>
	public void Clear()
	{
		Entries.Clear();
	}

	/// <summary>
	/// Adds a UI node to the stack.
	/// </summary>
	public void Add(UiStackEntry entry)
	{
		Entries.Add(entry);
	}

	/// <summary>
	/// Sorts the stack by z-index (back to front).
	/// Called after all entries have been added.
	/// </summary>
	public void Sort()
	{
		// Sort by combined z-index (ascending order)
		// If z-indices are equal, maintain insertion order (stable sort)
		Entries.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
	}

	/// <summary>
	/// Gets the topmost entity at the given index (from the back).
	/// Index 0 is the furthest element, last index is the topmost.
	/// </summary>
	public UiStackEntry? GetAtDepth(int index)
	{
		if (index < 0 || index >= Entries.Count)
			return null;
		return Entries[index];
	}

	/// <summary>
	/// Returns the number of UI nodes in the stack.
	/// </summary>
	public int Count => Entries.Count;
}

/// <summary>
/// Plugin that maintains the UI stack resource.
/// Adds a system that rebuilds the stack each frame based on UI node z-indices.
/// </summary>
public struct UiStackPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Register the UI stack resource
		app.AddResource(new UiStack());

		// System to rebuild the UI stack each frame
		app.AddSystem((
			Query<Data<ComputedLayout>> allUiNodes,
			Query<Data<ZIndex>> zIndexNodes,
			Query<Data<GlobalZIndex>> globalZIndexNodes,
			ResMut<UiStack> uiStack) =>
		{
			UpdateUiStack(allUiNodes, zIndexNodes, globalZIndexNodes, uiStack);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:stack:update")
		.After("flexbox:read_layout") // Run after layout is read into ComputedLayout
		.Build();

		// Build render commands from UI stack (in correct z-order)
		app.AddResource(new UiRenderCommands());
		app.AddSystem((
			Res<UiStack> uiStack,
			ResMut<UiRenderCommands> renderCommands,
			Query<Data<ComputedLayout>> layouts,
			Query<Data<BackgroundColor>> backgrounds,
			Query<Data<BorderColor>> borderColors,
			Query<Data<UiNode>> uiNodes,
			Query<Data<UiText>> texts,
			Query<Data<Scrollable>> scrollables,
			Query<Data<Parent>> parents,
			Local<Stack<(ulong entityId, bool hasClip)>> clipStack) =>
		{
			BuildRenderCommands(uiStack, renderCommands, layouts, backgrounds, borderColors, uiNodes, texts, scrollables, parents, clipStack);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:build-render-commands")
		.After("ui:stack:update")
		.Build();
	}

	/// <summary>
	/// Rebuilds the UI stack by iterating all UI nodes and sorting them by z-index.
	/// </summary>
	private static void UpdateUiStack(
		Query<Data<ComputedLayout>> allUiNodes,
		Query<Data<ZIndex>> zIndexNodes,
		Query<Data<GlobalZIndex>> globalZIndexNodes,
		ResMut<UiStack> uiStack)
	{
		ref var stack = ref uiStack.Value;
		stack.Clear();

		// Iterate all UI nodes and add them to the stack
		foreach (var (entityId, layout) in allUiNodes)
		{
			ref readonly var entity = ref entityId.Ref;

			// Get local z-index (default to 0 if not present)
			var localZIndex = 0;
			if (zIndexNodes.Contains(entity))
			{
				var (_, zIndex) = zIndexNodes.Get(entity);
				localZIndex = zIndex.Ref.Value;
			}

			// Get global z-index (default to 0 if not present)
			var globalZIndex = 0;
			if (globalZIndexNodes.Contains(entity))
			{
				var (_, gzIndex) = globalZIndexNodes.Get(entity);
				globalZIndex = gzIndex.Ref.Value;
			}

			// Combined z-index is global + local
			var combinedZIndex = globalZIndex + localZIndex;

			stack.Add(new UiStackEntry(
				entityId: entity,
				zIndex: combinedZIndex,
				globalZIndex: globalZIndex,
				localZIndex: localZIndex));
		}

		// Sort the stack by z-index (back to front)
		stack.Sort();
	}

	/// <summary>
	/// Builds rendering commands by walking the UI stack in order (back to front).
	/// For each entity, emits commands for background, border, text, and clipping.
	/// Handles scrollable containers by emitting BeginClip/EndClip commands.
	/// </summary>
	private static void BuildRenderCommands(
		Res<UiStack> uiStack,
		ResMut<UiRenderCommands> renderCommands,
		Query<Data<ComputedLayout>> layouts,
		Query<Data<BackgroundColor>> backgrounds,
		Query<Data<BorderColor>> borderColors,
		Query<Data<UiNode>> uiNodes,
		Query<Data<UiText>> texts,
		Query<Data<Scrollable>> scrollables,
		Query<Data<Parent>> parents,
		Local<Stack<(ulong entityId, bool hasClip)>> clipStack)
	{
		ref var commands = ref renderCommands.Value;
		commands.Clear();

		// Walk the UI stack in order (back to front)
		foreach (var entry in uiStack.Value.Entries)
		{
			var entityId = entry.EntityId;

			// Check if we need to end clip regions for entities no longer in scope
			// (when we reach an entity that's not a child of the current clip)
			while (clipStack.Value!.Count > 0)
			{
				var (clipEntityId, hasClip) = clipStack.Value!.Peek();

				// Check if current entity is a descendant of the clip entity
				bool isDescendant = IsDescendantOf(entityId, clipEntityId, parents);

				if (!isDescendant)
				{
					// Pop the clip
					clipStack.Value!.Pop();
					if (hasClip)
					{
						commands.Add(RenderCommand.EndClip());
					}
				}
				else
				{
					break; // Still inside this clip region
				}
			}

			// Get layout (required for all rendering)
			if (!layouts.Contains(entityId))
				continue;

			var (_, layout) = layouts.Get(entityId);
			ref var l = ref layout.Ref;

			// Check if this is a scrollable container - emit BeginClip
			bool isScrollable = scrollables.Contains(entityId);
			if (isScrollable)
			{
				// Emit clip command before rendering this entity's content
				commands.Add(RenderCommand.BeginClip(l.X, l.Y, l.Width, l.Height));
				clipStack.Value!.Push((entityId, true));
			}
			else
			{
				// Not scrollable, but track in stack to know when children end
				clipStack.Value!.Push((entityId, false));
			}

			// Emit background command if entity has BackgroundColor
			if (backgrounds.Contains(entityId))
			{
				var (_, bg) = backgrounds.Get(entityId);
				commands.Add(RenderCommand.DrawBackground(
					entityId, l.X, l.Y, l.Width, l.Height, bg.Ref.Color));
			}

			// Emit border command if entity has BorderColor
			if (borderColors.Contains(entityId) && uiNodes.Contains(entityId))
			{
				var (_, borderColor) = borderColors.Get(entityId);
				var (_, uiNode) = uiNodes.Get(entityId);

				// Get border radius if present
				var borderRadius = 0f;
				// BorderRadius is stored in UiNode component, need to check if it exists
				// For now, use 0f as default

				commands.Add(RenderCommand.DrawBorder(
					entityId, l.X, l.Y, l.Width, l.Height, borderColor.Ref.Color, borderRadius));
			}

			// Emit text command if entity has UiText
			if (texts.Contains(entityId))
			{
				var (_, text) = texts.Get(entityId);
				// Default text color and size (will be customizable later)
				var textColor = new System.Numerics.Vector4(1f, 1f, 1f, 1f); // White
				var fontSize = 20f;

				commands.Add(RenderCommand.DrawText(
					entityId, l.X, l.Y, l.Width, l.Height, text.Ref.Value, fontSize, textColor));
			}
		}

		// End any remaining clip regions
		while (clipStack.Value!.Count > 0)
		{
			var (_, hasClip) = clipStack.Value!.Pop();
			if (hasClip)
			{
				commands.Add(RenderCommand.EndClip());
			}
		}
	}

	/// <summary>
	/// Checks if an entity is a descendant of another entity in the hierarchy.
	/// </summary>
	private static bool IsDescendantOf(ulong entityId, ulong ancestorId, Query<Data<Parent>> parents)
	{
		if (entityId == ancestorId)
			return true;

		// Walk up the parent chain
		var currentId = entityId;
		while (parents.Contains(currentId))
		{
			var (_, parent) = parents.Get(currentId);
			currentId = parent.Ref.Id;

			if (currentId == ancestorId)
				return true;

			// Prevent infinite loops
			if (currentId == entityId)
				break;
		}

		return false;
	}
}
