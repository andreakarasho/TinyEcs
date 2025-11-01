using TinyEcs.Bevy;
using Flexbox;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Resource that manages the Flexbox layout state.
/// Stores the root container node.
/// </summary>
public class FlexboxUiState
{
	/// <summary>
	/// The root Flexbox container that holds all UI nodes.
	/// </summary>
	public Node Root { get; }

	public FlexboxUiState()
	{
		Root = Flex.CreateDefaultNode();
		Root.nodeStyle.Display = Display.Flex;
		Root.nodeStyle.FlexDirection = FlexDirection.Column;
		Root.nodeStyle.Dimensions[(int)Dimension.Width] = new Value(100f, Unit.Percent);
		Root.nodeStyle.Dimensions[(int)Dimension.Height] = new Value(100f, Unit.Percent);
	}

	/// <summary>
	/// Calculate layout for the entire UI tree.
	/// Call this once per frame after all style updates.
	/// </summary>
	public void CalculateLayout(float availableWidth, float availableHeight)
	{
		Flex.CalculateLayout(Root, availableWidth, availableHeight, Direction.LTR);
		// Root.CalculateLayout(availableWidth, availableHeight, Direction.LTR);
	}
}

/// <summary>
/// Flexbox value that can be points, percent, or auto.
/// </summary>
public struct FlexValue
{
	public float Value;
	public Unit Unit;

	public FlexValue(float value, Unit unit)
	{
		Value = value;
		Unit = unit;
	}

	public static FlexValue Points(float value) => new FlexValue(value, Unit.Point);
	public static FlexValue Percent(float value) => new FlexValue(value, Unit.Percent);
	public static FlexValue Auto() => new FlexValue(float.NaN, Unit.Auto);
	public static FlexValue Undefined() => new FlexValue(float.NaN, Unit.Undefined);

	public bool IsUndefined => Unit == Unit.Undefined;
	public bool IsAuto => Unit == Unit.Auto;
	public bool IsDefined => Unit != Unit.Undefined && Unit != Unit.Auto;

	public static implicit operator FlexValue(float value) => Points(value);
}

/// <summary>
/// Flexbox flex-basis value (auto, content, or length).
/// </summary>
public struct FlexBasis
{
	public FlexValue Value;

	public FlexBasis(FlexValue value)
	{
		Value = value;
	}

	public static FlexBasis Auto() => new FlexBasis(FlexValue.Auto());
	public static FlexBasis Points(float value) => new FlexBasis(FlexValue.Points(value));
	public static FlexBasis Percent(float value) => new FlexBasis(FlexValue.Percent(value));

	public static implicit operator FlexBasis(float value) => Points(value);
}

/// <summary>
/// Plugin that integrates Flexbox layout with TinyEcs.Bevy UI components.
/// Converts ECS component data (Style, Parent) into Flexbox nodes and manages the layout tree.
/// </summary>
public struct FlexboxUiPlugin : IPlugin
{
	/// <summary>
	/// Default available width for layout calculation (can be overridden).
	/// </summary>
	public float AvailableWidth { get; set; }

	/// <summary>
	/// Default available height for layout calculation (can be overridden).
	/// </summary>
	public float AvailableHeight { get; set; }

	public FlexboxUiPlugin()
	{
		AvailableWidth = 1920f;
		AvailableHeight = 1080f;
	}

	public readonly void Build(App app)
	{
		// Register the FlexboxUiState resource
		app.AddResource(new FlexboxUiState());

		// Register the TextMeasureContext resource (renderer will populate it)
		var textMeasureContext = new TextMeasureContext();
		app.AddResource(textMeasureContext);

		// Set up the GetTextData callback once at plugin build time
		// The callback captures World in its closure to allow the measure function to query entities
		var world = app.GetWorld();
		textMeasureContext.GetTextData = (entityId) =>
		{
			var entity = world.Entity(entityId);
			if (!entity.Has<UiText>())
				return (string.Empty, TextStyle.Default());

			var text = entity.Get<UiText>().Value;
			var style = entity.Has<TextStyle>()
				? entity.Get<TextStyle>()
				: TextStyle.Default();

			return (text, style);
		};

		// Copy to local variables to avoid struct 'this' capture
		var width = AvailableWidth;
		var height = AvailableHeight;

		// Phase 2: Node Lifecycle - Use observers for OnInsert/OnRemove
		app.AddObserver<OnAdd<UiNode>, ResMut<FlexboxUiState>, Commands>(OnUiNodeInserted);
		app.AddObserver<OnRemove<FlexboxNodeRef>, ResMut<FlexboxUiState>>(OnUiNodeRemoved);

		// Attach measure function when UiText is added
		app.AddObserver<OnAdd<UiText>, Query<Data<FlexboxNodeRef>>, Res<TextMeasureContext>>(OnTextAdded);

		// Update measure function when UiText or TextStyle changes
		app.AddObserver<OnInsert<UiText>, Query<Data<FlexboxNodeRef>>>(OnTextChanged);
		app.AddObserver<OnInsert<TextStyle>, Query<Data<FlexboxNodeRef>>>(OnTextStyleChanged);

		// Phase 3: Property Synchronization
		app.AddSystem((Query<Data<UiNode, FlexboxNodeRef>, Filter<Changed<UiNode>>> changedNodes) =>
		{
			SyncUiNodeToFlexbox(changedNodes);
		})
			.InStage(Stage.PreUpdate)
			.Label("flexbox:sync_node")
			.Build();

		app.AddObserver<OnAdd<Parent>, Query<Data<FlexboxNodeRef>>, ResMut<FlexboxUiState>>(SyncHierarchyToFlexbox2);

		// Calculate layout after syncing nodes and hierarchy, before reading ComputedLayout
		app.AddSystem((ResMut<FlexboxUiState> state) =>
		{
			state.Value.CalculateLayout(width, height);
		})
			.InStage(Stage.PostUpdate)
			.Label("flexbox:calc_layout")
			.Build();

		app.AddSystem((
			Query<Data<FlexboxNodeRef>> nodeRefs,
			Query<Data<Parent>> parents,
			Query<Data<Scrollable>> scrollables,
			Query<Data<UiNode>> uiNodes,
			Commands commands) =>
		{
			ReadComputedLayout(nodeRefs, parents, scrollables, uiNodes, commands);
		})
			.InStage(Stage.PostUpdate)
			.Label("flexbox:read_layout")
			.After("flexbox:calc_layout")
			.Build();

		// Add Phase 1 interaction plugins (from sickle_ui port)
		// These must be added AFTER flexbox systems are registered (they reference flexbox labels)
		// Order matters: FluxInteraction must be added before InteractionState (InteractionState references FluxInteraction labels)
		app.AddPlugin(new FluxInteractionPlugin());
		app.AddPlugin(new InteractionStatePlugin());
		app.AddPlugin(new DragPlugin());
		app.AddPlugin(new ScrollPlugin());

		// Add Phase 2 animation plugin (from sickle_ui port)
		app.AddPlugin(new AnimatedInteractionPlugin());
	}

	/// <summary>
	/// Observer: Called when UiNode component is added to an entity.
	/// Creates a new Flexbox node and attaches it to the root.
	/// </summary>
	private static void OnUiNodeInserted(
		OnAdd<UiNode> trigger,
		ResMut<FlexboxUiState> state,
		Commands commands)
	{
		// Create a new Flexbox node for this UI entity
		var node = Flex.CreateDefaultNode();

		// Default styling - can be overridden by Style component
		node.nodeStyle.Display = Display.Flex;
		node.nodeStyle.FlexDirection = FlexDirection.Column;

		// Add as child of root (will be reparented by hierarchy sync if needed)
		state.Value.Root.AddChild(node);

		// Store reference to the Flexbox node on the entity
		commands.Entity(trigger.EntityId).Insert(new FlexboxNodeRef
		{
			Node = node,
			ElementId = (uint)trigger.EntityId // Use entity ID as element ID for tracking
		});
	}

	/// <summary>
	/// Observer: Called when UiNode component is removed from an entity.
	/// Destroys the associated Flexbox node.
	/// </summary>
	private static void OnUiNodeRemoved(
		OnRemove<FlexboxNodeRef> trigger,
		ResMut<FlexboxUiState> state)
	{
		if (trigger.Component.Node != null)
		{
			// Remove from parent if attached
			if (trigger.Component.Node.Parent != null)
			{
				trigger.Component.Node.Parent.RemoveChild(trigger.Component.Node);
			}
		}
	}

	/// <summary>
	/// Context data stored in Node.Context for text measurement.
	/// Contains the entity ID and the measure context reference.
	/// </summary>
	private class TextNodeContext
	{
		public ulong EntityId;
		public TextMeasureContext MeasureContext;

		public TextNodeContext(ulong entityId, TextMeasureContext measureContext)
		{
			EntityId = entityId;
			MeasureContext = measureContext;
		}
	}

	/// <summary>
	/// Observer: Called when UiText component is added to an entity.
	/// Attaches a measure function to the Flexbox node for automatic text sizing.
	/// </summary>
	private static void OnTextAdded(
		OnAdd<UiText> trigger,
		Query<Data<FlexboxNodeRef>> nodeRefs,
		Res<TextMeasureContext> measureContext)
	{
		var entityId = trigger.EntityId;
		if (!nodeRefs.Contains(entityId))
			return;

		var (_, nodeRef) = nodeRefs.Get(entityId);
		var node = nodeRef.Ref.Node;
		if (node == null)
			return;

		// Store entity ID and measure context in node context for lookup in measure function
		node.Context = new TextNodeContext(entityId, measureContext.Value);

		// Attach measure function to the Flexbox node
		node.SetMeasureFunc(MeasureText);
		node.MarkAsDirty();
		node.ResetLayout();
	}

	/// <summary>
	/// Observer: Called when UiText component is updated.
	/// Marks the Flexbox node as dirty to trigger remeasurement.
	/// </summary>
	private static void OnTextChanged(
		OnInsert<UiText> trigger,
		Query<Data<FlexboxNodeRef>> nodeRefs)
	{
		var entityId = trigger.EntityId;
		if (!nodeRefs.Contains(entityId))
			return;

		var (_, nodeRef) = nodeRefs.Get(entityId);
		var node = nodeRef.Ref.Node;
		if (node == null)
			return;

		// Mark node as dirty to trigger remeasurement
		node.MarkAsDirty();
		node.ResetLayout();
	}

	/// <summary>
	/// Observer: Called when TextStyle component is updated.
	/// Marks the Flexbox node as dirty to trigger remeasurement.
	/// </summary>
	private static void OnTextStyleChanged(
		OnInsert<TextStyle> trigger,
		Query<Data<FlexboxNodeRef>> nodeRefs)
	{
		var entityId = trigger.EntityId;
		if (!nodeRefs.Contains(entityId))
			return;

		var (_, nodeRef) = nodeRefs.Get(entityId);
		var node = nodeRef.Ref.Node;
		if (node == null)
			return;

		// Mark node as dirty to trigger remeasurement
		node.MarkAsDirty();
		node.ResetLayout();
	}

	/// <summary>
	/// Measure function called by Flexbox to determine text intrinsic dimensions.
	/// Uses the entity ID stored in node.Context to query for UiText and TextStyle components.
	/// </summary>
	private static Size MeasureText(
		Node node,
		float width,
		MeasureMode widthMode,
		float height,
		MeasureMode heightMode)
	{
		// Get the TextNodeContext from node.Context
		if (node.Context is not TextNodeContext context)
			return new Size(0, 0);

		var entityId = context.EntityId;
		var measureContext = context.MeasureContext;

		// Verify callbacks are set
		if (measureContext.GetTextData == null || measureContext.MeasureText == null)
			return new Size(0, 0);

		// Get text and style from the entity using the callback
		var (text, style) = measureContext.GetTextData(entityId);

		if (string.IsNullOrEmpty(text))
			return new Size(0, 0);

		// Call the renderer's measurement function
		var (measuredWidth, measuredHeight) = measureContext.MeasureText(text, style);

		return new Size(measuredWidth, measuredHeight);
	}

	/// <summary>
	/// Helper: Converts FlexValue to Flexbox.Value
	/// Creates a new Value instance if target is null, otherwise updates the existing instance.
	/// This avoids unnecessary allocations.
	/// </summary>
	private static void ToFlexboxValue(ref Value target, FlexValue value)
	{
		if (target == null)
		{
			// Create new instance
			if (value.IsAuto)
				target = new Value(float.NaN, Unit.Auto);
			else if (value.IsUndefined)
				target = Value.UndefinedValue;
			else
				target = new Value(value.Value, value.Unit);
		}
		else
		{
			// Reuse existing instance
			if (value.IsAuto)
			{
				target.value = float.NaN;
				target.unit = Unit.Auto;
			}
			else if (value.IsUndefined)
			{
				target.value = float.NaN;
				target.unit = Unit.Undefined;
			}
			else
			{
				target.value = value.Value;
				target.unit = value.Unit;
			}
		}
	}

	/// <summary>
	/// System: Syncs UiNode component changes to Flexbox node properties.
	/// Runs in PreUpdate stage.
	/// </summary>
	private static void SyncUiNodeToFlexbox(
		Query<Data<UiNode, FlexboxNodeRef>, Filter<Changed<UiNode>>> changedNodes)
	{
		foreach (var (uiNode, nodeRef) in changedNodes)
		{
			ref var nodeData = ref uiNode.Ref;
			ref var nn = ref nodeRef.Ref;
			ref var node = ref nn.Node;

			if (node == null)
				continue;

			var ns = node.nodeStyle;

			// Sync all UiNode properties to Flexbox node
			ns.Display = nodeData.Display;
			ns.PositionType = nodeData.PositionType;
			ns.FlexDirection = nodeData.FlexDirection;
			ns.FlexWrap = nodeData.FlexWrap;
			ns.Overflow = nodeData.Overflow;
			ns.AlignItems = nodeData.AlignItems;
			ns.AlignSelf = nodeData.AlignSelf;
			ns.AlignContent = nodeData.AlignContent;
			ns.JustifyContent = nodeData.JustifyContent;
			ns.FlexGrow = nodeData.FlexGrow;
			ns.FlexShrink = nodeData.FlexShrink;
			ToFlexboxValue(ref ns.FlexBasis, nodeData.FlexBasis.Value);

			// Dimensions
			ToFlexboxValue(ref ns.Dimensions[(int)Dimension.Width], nodeData.Width);
			ToFlexboxValue(ref ns.Dimensions[(int)Dimension.Height], nodeData.Height);
			ToFlexboxValue(ref ns.MinDimensions[(int)Dimension.Width], nodeData.MinWidth);
			ToFlexboxValue(ref ns.MinDimensions[(int)Dimension.Height], nodeData.MinHeight);
			ToFlexboxValue(ref ns.MaxDimensions[(int)Dimension.Width], nodeData.MaxWidth);
			ToFlexboxValue(ref ns.MaxDimensions[(int)Dimension.Height], nodeData.MaxHeight);

			// Position
			ToFlexboxValue(ref ns.Position[(int)Edge.Left], nodeData.Left);
			ToFlexboxValue(ref ns.Position[(int)Edge.Right], nodeData.Right);
			ToFlexboxValue(ref ns.Position[(int)Edge.Top], nodeData.Top);
			ToFlexboxValue(ref ns.Position[(int)Edge.Bottom], nodeData.Bottom);

			// Margin
			ToFlexboxValue(ref ns.Margin[(int)Edge.Left], nodeData.MarginLeft);
			ToFlexboxValue(ref ns.Margin[(int)Edge.Right], nodeData.MarginRight);
			ToFlexboxValue(ref ns.Margin[(int)Edge.Top], nodeData.MarginTop);
			ToFlexboxValue(ref ns.Margin[(int)Edge.Bottom], nodeData.MarginBottom);

			// Padding
			ToFlexboxValue(ref ns.Padding[(int)Edge.Left], nodeData.PaddingLeft);
			ToFlexboxValue(ref ns.Padding[(int)Edge.Right], nodeData.PaddingRight);
			ToFlexboxValue(ref ns.Padding[(int)Edge.Top], nodeData.PaddingTop);
			ToFlexboxValue(ref ns.Padding[(int)Edge.Bottom], nodeData.PaddingBottom);

			// Border
			ToFlexboxValue(ref ns.Border[(int)Edge.Left], nodeData.BorderLeft);
			ToFlexboxValue(ref ns.Border[(int)Edge.Right], nodeData.BorderRight);
			ToFlexboxValue(ref ns.Border[(int)Edge.Top], nodeData.BorderTop);
			ToFlexboxValue(ref ns.Border[(int)Edge.Bottom], nodeData.BorderBottom);

			// Mark node as dirty to trigger relayout
			node.MarkAsDirty();
			node.ResetLayout();
		}
	}

	/// <summary>
	/// Observer: Syncs Parent component changes to Flexbox node hierarchy.
	/// </summary>
	private static void SyncHierarchyToFlexbox2(
		OnAdd<Parent> trigger,
		Query<Data<FlexboxNodeRef>> allNodeRefs,
		ResMut<FlexboxUiState> state
	)
	{
		var childEntityId = trigger.EntityId;
		var parentEntityId = trigger.Component.Id;

		if (!allNodeRefs.Contains(childEntityId))
			return;

		var (_, childNodeRef) = allNodeRefs.Get(childEntityId);
		var childNode = childNodeRef.Ref.Node;
		if (childNode == null)
			return;

		// Remove from current parent first
		if (childNode.Parent != null)
		{
			childNode.Parent.RemoveChild(childNode);
		}

		// Find parent's Flexbox node
		Node? parentNode = null;
		if (allNodeRefs.Contains(parentEntityId))
		{
			var (_, parentNodeRef) = allNodeRefs.Get(parentEntityId);
			parentNode = parentNodeRef.Ref.Node;
		}

		// Add to new parent (or root if parent not found)
		if (parentNode != null)
		{
			parentNode.AddChild(childNode);
		}
		else
		{
			// No parent entity or parent doesn't have FlexboxNodeRef
			// Add to root as fallback
			state.Value.Root.AddChild(childNode);
		}
	}

	/// <summary>
	/// System: Reads computed layout from Flexbox nodes back to ComputedLayout components.
	/// Runs in PostUpdate stage after layout calculation.
	/// Uses LayoutGet* methods which return absolute screen coordinates (accounting for parent offsets).
	/// </summary>
	private static void ReadComputedLayout(
		Query<Data<FlexboxNodeRef>> nodeRefs,
		Query<Data<Parent>> parents,
		Query<Data<Scrollable>> scrollables,
		Query<Data<UiNode>> uiNodes,
		Commands commands)
	{
		foreach (var (entityId, nodeRef) in nodeRefs)
		{
			ref var nn = ref nodeRef.Ref;
			ref var node = ref nn.Node;
			if (node == null)
				continue;

			// Read absolute layout coordinates using Flexbox LayoutGet* methods
			// LayoutGetX/Y recursively walk parent chain for absolute coordinates
			// LayoutGetLeft/Top return relative coordinates (don't use those)
			var x = node.LayoutGetX();
			var y = node.LayoutGetY();

			// Apply scroll transform from ancestor scrollables: sum of their ScrollOffset, plus
			// the nearest scrollable's ContentOrigin to normalize initial view.
			var scrollTransform = GetScrollTransformWithNearestOrigin(entityId.Ref, parents, scrollables);
			x -= scrollTransform.X;
			y -= scrollTransform.Y;

			commands.Entity(entityId.Ref).Insert(new ComputedLayout
			{
				X = x,
				Y = y,
				Width = node.LayoutGetWidth(),
				Height = node.LayoutGetHeight()
			});
		}
	}

	/// <summary>
	/// Walks up the parent chain and accumulates scroll offsets from scrollable containers.
	/// </summary>
	private static System.Numerics.Vector2 GetScrollTransformWithNearestOrigin(
			ulong entityId,
			Query<Data<Parent>> parents,
			Query<Data<Scrollable>> scrollables)
	{
		var total = System.Numerics.Vector2.Zero;
		var currentId = entityId;

		// Walk up parent chain
		while (parents.Contains(currentId))
		{
			var (_, parent) = parents.Get(currentId);
			var parentId = parent.Ref.Id;

			// Check if parent is scrollable
			if (scrollables.Contains(parentId))
			{
				var (_, scrollable) = scrollables.Get(parentId);
				var s = scrollable.Ref;
				total += s.ScrollOffset;
			}

			currentId = parentId;

			// Prevent infinite loops
			if (currentId == entityId)
				break;
		}

		return total;
	}
}
