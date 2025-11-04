using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Plugin that manages floating panels - UI elements that float above everything else
/// with absolute positioning and high Z-index.
/// Ported from sickle_ui's floating_panel.rs.
/// </summary>
public struct FloatingPanelPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System to auto-assign Z-indices to new floating panels
		app.AddSystem((
			Commands commands,
			Query<Data<FloatingPanel>, Filter<Added<FloatingPanel>>> newPanels) =>
		{
			IndexFloatingPanels(commands, newPanels);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:floating_panel:index")
		.Build();

		// System to update panel layout (position, size, Z-index)
		app.AddSystem((
			Commands commands,
			Query<Data<FloatingPanel, UiNode>, Filter<Changed<FloatingPanel>>> changedPanels) =>
		{
			UpdatePanelLayout(commands, changedPanels);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:floating_panel:update_layout")
		.After("ui:floating_panel:index")
		.Build();
	}

	/// <summary>
	/// Auto-assigns Z-indices to newly added floating panels.
	/// Ensures new panels appear on top of existing ones.
	/// </summary>
	private static void IndexFloatingPanels(
		Commands commands,
		Query<Data<FloatingPanel>, Filter<Added<FloatingPanel>>> newPanels)
	{
		// Find the maximum Z-index among all new panels
		int maxZIndex = 0;
		foreach (var (_, panel) in newPanels)
		{
			if (panel.Ref.ZIndex.HasValue && panel.Ref.ZIndex.Value > maxZIndex)
			{
				maxZIndex = panel.Ref.ZIndex.Value;
			}
		}

		// Assign Z-indices to panels that don't have one
		int offset = 1;
		foreach (var (entityId, panel) in newPanels)
		{
			ref var p = ref panel.Ref;
			if (!p.ZIndex.HasValue)
			{
				p.ZIndex = FloatingPanelConstants.MinZIndex + maxZIndex + offset;
				offset++;
				commands.Entity(entityId.Ref).Insert(p);
			}
		}
	}

	/// <summary>
	/// Updates the layout of floating panels based on their FloatingPanel component.
	/// Applies position, size, and Z-index.
	/// </summary>
	private static void UpdatePanelLayout(
		Commands commands,
		Query<Data<FloatingPanel, UiNode>, Filter<Changed<FloatingPanel>>> changedPanels)
	{
		foreach (var (entityId, panel, node) in changedPanels)
		{
			ref readonly var p = ref panel.Ref;
			ref var n = ref node.Ref;

			// Apply absolute positioning
			n.PositionType = PositionType.Absolute;
			n.Left = FlexValue.Points(p.Position.X);
			n.Top = FlexValue.Points(p.Position.Y);

			// Apply size
			n.Width = FlexValue.Points(p.Size.X);
			n.Height = FlexValue.Points(p.Size.Y);

			// Apply Z-index based on priority
			int zIndex;
			if (p.Priority)
			{
				zIndex = FloatingPanelConstants.PriorityZIndex;
			}
			else if (p.ZIndex.HasValue)
			{
				zIndex = p.ZIndex.Value;
			}
			else
			{
				zIndex = FloatingPanelConstants.MinZIndex;
			}

			// Re-insert the modified node to trigger change detection
			commands.Entity(entityId.Ref).Insert(n);
			commands.Entity(entityId.Ref).Insert(new GlobalZIndex(zIndex));
		}
	}
}

/// <summary>
/// Constants for floating panel Z-index management.
/// </summary>
public static class FloatingPanelConstants
{
	/// <summary>Minimum Z-index for floating panels (renders above normal UI)</summary>
	public const int MinZIndex = 1000;

	/// <summary>Z-index for priority panels (e.g., dropdown menus that need to be on top)</summary>
	public const int PriorityZIndex = 10000;
}

/// <summary>
/// Component for floating panels that appear above other UI elements.
/// Floating panels use absolute positioning and high Z-index to float above the UI hierarchy.
/// </summary>
public struct FloatingPanel
{
	/// <summary>Size of the panel in pixels</summary>
	public Vector2 Size;

	/// <summary>Position of the panel in screen coordinates (top-left corner)</summary>
	public Vector2 Position;

	/// <summary>Z-index for layering. If null, will be auto-assigned.</summary>
	public int? ZIndex;

	/// <summary>
	/// If true, this panel gets the highest Z-index (priority mode).
	/// Used for dropdown menus and other UI that must appear on top.
	/// </summary>
	public bool Priority;

	public FloatingPanel()
	{
		Size = Vector2.Zero;
		Position = Vector2.Zero;
		ZIndex = null;
		Priority = false;
	}

	public FloatingPanel(Vector2 size, Vector2 position, bool priority = false)
	{
		Size = size;
		Position = position;
		ZIndex = null;
		Priority = priority;
	}
}
