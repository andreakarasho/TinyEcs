using System.Numerics;
using Clay_cs;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Clay;

/// <summary>
/// Plugin responsible for Clay layout calculation in retained mode.
/// Runs in PreUpdate stage to calculate layout before rendering.
/// </summary>
public struct ClayLayoutPlugin : IPlugin
{
	public void Build(App app)
	{
		// System ordering:
		// 1. Track root entities (entities without parents)
		// 2. Mark dirty when ClayNode changes
		// 3. Build Clay layout hierarchy
		// 4. Calculate layout (if dirty)
		// 5. Read computed layout back to components

		app.AddSystem((Res<ClayUiOptions> options) =>
		{
			Clay_cs.Clay.SetLayoutDimensions(options.Value.LayoutDimensions);
		})
		.InStage(Stage.First)
		.SingleThreaded()
		.Build();

		app.AddSystem((
			ResMut<ClayUiState> state,
			Query<Data<ClayNode>, Filter<Without<Parent>>> rootQuery,
			Query<Data<ClayNode>, Filter<With<Parent>>> childQuery
		) => TrackRootEntities(state, rootQuery, childQuery))
			.InStage(Stage.PreUpdate)
			.Label("clay:track-roots")
			.Build();

		app.AddSystem((
			ResMut<ClayUiState> state,
			Query<Data<ClayNode>, Filter<Changed<ClayNode>>> changedNodes
		) => MarkDirtyOnNodeChange(state, changedNodes))
			.InStage(Stage.PreUpdate)
			.Label("clay:mark-dirty")
			.After("clay:track-roots")
			.Build();

		app.AddSystem((
			ResMut<ClayUiState> state,
			Query<Data<Parent>, Filter<With<ClayNode>, Changed<Parent>>> changedParents
		) => MarkDirtyOnHierarchyChange(state, changedParents))
			.InStage(Stage.PreUpdate)
			.Label("clay:mark-dirty-hierarchy")
			.After("clay:mark-dirty")
			.Build();

		app.AddSystem((
			ResMut<ClayUiState> state,
			ResMut<ClayPointerState> pointer,
			Query<Data<ClayNode, ClayElementId>> nodeQuery,
			Query<Data<Children>> childrenQuery,
			Query<Data<ClayText>, Filter<With<ClayNode>>> textQuery
		) => CalculateLayout(state, pointer, nodeQuery, childrenQuery, textQuery))
			.InStage(Stage.PreUpdate)
			.Label("clay:calculate-layout")
			.After("clay:mark-dirty-hierarchy")
			.Build();

		app.AddSystem((
			Res<ClayUiState> state,
			Commands commands,
			Query<Data<ClayElementId>> elementQuery
		) => ReadComputedLayout(state, commands, elementQuery))
			.InStage(Stage.PreUpdate)
			.Label("clay:read-layout")
			.After("clay:calculate-layout")
			.Build();
	}

	/// <summary>
	/// Track entities that are roots (no parent) for layout calculation.
	/// </summary>
	private static void TrackRootEntities(
		ResMut<ClayUiState> state,
		Query<Data<ClayNode>, Filter<Without<Parent>>> rootQuery,
		Query<Data<ClayNode>, Filter<With<Parent>>> childQuery)
	{
		state.Value.RootEntities.Clear();

		// Add all entities without Parent to root list
		foreach (var (entityId, _) in rootQuery)
		{
			state.Value.RootEntities.Add(entityId.Ref);
		}
	}

	/// <summary>
	/// Mark layout as dirty when any ClayNode component changes.
	/// </summary>
	private static void MarkDirtyOnNodeChange(
		ResMut<ClayUiState> state,
		Query<Data<ClayNode>, Filter<Changed<ClayNode>>> changedNodes)
	{
		// Check if any nodes changed
		bool hasChanges = false;
		foreach (var _ in changedNodes)
		{
			hasChanges = true;
			break;
		}

		if (hasChanges)
		{
			state.Value.LayoutDirty = true;
		}
	}

	/// <summary>
	/// Mark layout as dirty when hierarchy changes (parent/child added/removed).
	/// </summary>
	private static void MarkDirtyOnHierarchyChange(
		ResMut<ClayUiState> state,
		Query<Data<Parent>, Filter<With<ClayNode>, Changed<Parent>>> changedParents)
	{
		// Check if any parent relationships changed
		bool hasChanges = false;
		foreach (var _ in changedParents)
		{
			hasChanges = true;
			break;
		}

		if (hasChanges)
		{
			state.Value.LayoutDirty = true;
		}
	}

	/// <summary>
	/// Calculate Clay layout for all elements.
	/// Uses retained mode: only recalculates when layout is dirty.
	/// </summary>
	private static unsafe void CalculateLayout(
		ResMut<ClayUiState> state,
		ResMut<ClayPointerState> pointer,
		Query<Data<ClayNode, ClayElementId>> nodeQuery,
		Query<Data<Children>> childrenQuery,
		Query<Data<ClayText>, Filter<With<ClayNode>>> textQuery)
	{
		if (!state.Value.LayoutDirty)
		{
			// Layout hasn't changed, skip calculation
			// return;
		}

		// Update Clay pointer state
		Clay_cs.Clay.SetPointerState(pointer.Value.Position, pointer.Value.PrimaryDown);

		// Update Clay scroll containers
		var scrollDelta = pointer.Value.GetAccumulatedScroll();
		Clay_cs.Clay.UpdateScrollContainers(
			pointer.Value.EnableDragScrolling,
			scrollDelta,
			pointer.Value.DeltaTime);

		// Begin Clay layout
		Clay_cs.Clay.BeginLayout();        // Build layout hierarchy starting from root entities
		foreach (var rootId in state.Value.RootEntities)
		{
			BuildLayoutRecursive(rootId, nodeQuery, childrenQuery, textQuery, state.Value);
		}

		// End layout and get render commands
		var renderCommandArray = ClayInterop.Clay_EndLayout();

		// Store pointer and length for render commands
		// The render command array is stored in Clay's internal arena and is valid until next BeginLayout
		state.Value.RenderCommandsPtr = renderCommandArray.internalArray;
		state.Value.RenderCommandsLength = renderCommandArray.length;

		// Layout is now clean
		state.Value.LayoutDirty = false;
	}

	/// <summary>
	/// Recursively build Clay layout hierarchy.
	/// </summary>
	private static unsafe void BuildLayoutRecursive(
		ulong entityId,
		Query<Data<ClayNode, ClayElementId>> nodeQuery,
		Query<Data<Children>> childrenQuery,
		Query<Data<ClayText>, Filter<With<ClayNode>>> textQuery,
		ClayUiState state)
	{
		if (!nodeQuery.Contains(entityId))
		{
			return; // Entity doesn't have ClayNode
		}

		var (node, clayId) = nodeQuery.Get(entityId);

		// Open element
		Clay_cs.Clay.OpenElement(new Clay_ElementId() { id = clayId.Ref.Id });

		// Configure element
		var decl = new Clay_ElementDeclaration
		{
			layout = node.Ref.Layout
		};

		// Add visual configurations
		if (node.Ref.Rectangle.HasValue)
		{
			decl.backgroundColor = node.Ref.Rectangle.Value.backgroundColor;
		}

		if (node.Ref.Border.HasValue)
		{
			decl.border = node.Ref.Border.Value;
		}

		if (node.Ref.CornerRadius.HasValue)
		{
			decl.cornerRadius = node.Ref.CornerRadius.Value;
		}

		if (node.Ref.Floating.HasValue)
		{
			decl.floating = node.Ref.Floating.Value;
		}

		if (node.Ref.Clip.HasValue)
		{
			decl.clip = node.Ref.Clip.Value;
			if (decl.clip.vertical || decl.clip.horizontal)
			{
				// Reset scroll offset to zero; Clay manages this internally
				decl.clip.childOffset = Clay_cs.Clay.GetScrollOffset();
			}
		}

		if (node.Ref.Custom.HasValue)
		{
			decl.custom = node.Ref.Custom.Value;
		}

		Clay_cs.Clay.ConfigureOpenElement(decl);

		if (textQuery.Contains(entityId))
		{
			var (_, text) = textQuery.Get(entityId);
			var clayString = Clay_cs.Clay.ClayStrings.Get(text.Ref.Text);
			var textConfig = node.Ref.Text ?? new Clay_TextElementConfig
			{
				fontSize = 16,
				textColor = new Clay_Color(255, 255, 255, 255)
			};
			Clay_cs.Clay.OpenTextElement(clayString, textConfig);
		}

		if (childrenQuery.Contains(entityId))
		{
			var (_, children) = childrenQuery.Get(entityId);

			foreach (var childId in children.Ref)
			{
				BuildLayoutRecursive(childId, nodeQuery, childrenQuery, textQuery, state);
			}
		}

		// Close element
		Clay_cs.Clay.CloseElement();
	}

	/// <summary>
	/// Read computed layout from Clay and write to ClayComputedLayout components.
	/// </summary>
	private static unsafe void ReadComputedLayout(
		Res<ClayUiState> state,
		Commands commands,
		Query<Data<ClayElementId>> elementQuery)
	{
		foreach (var (entityId, clayId) in elementQuery)
		{
			// Get element data from Clay
			var elementData = Clay_cs.Clay.GetElementData(new Clay_cs.Clay_ElementId { id = clayId.Ref.Id });

			// Write computed layout
			var computed = new ClayComputedLayout
			{
				X = elementData.boundingBox.x,
				Y = elementData.boundingBox.y,
				Width = elementData.boundingBox.width,
				Height = elementData.boundingBox.height
			};

			commands.Entity(entityId.Ref).Insert(computed);
		}
	}
}
