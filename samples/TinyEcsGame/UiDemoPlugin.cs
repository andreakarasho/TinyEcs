using System;
using System.Numerics;
using Clay;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using ClayColor = Clay.Color;

namespace TinyEcsMonogameSample;

struct FruitItem
{
	public string Name;
}

sealed class FruitSelection
{
	public string Selected = string.Empty;
}

sealed class UiDemoPlugin : IPlugin
{
	private static readonly string[] Fruits =
	{
		"Apple", "Apricot", "Avocado", "Banana", "Blackberry", "Blueberry",
		"Cantaloupe", "Cherry", "Coconut", "Cranberry", "Date", "Dragonfruit",
		"Elderberry", "Fig", "Gooseberry", "Grape", "Grapefruit", "Guava",
		"Honeydew", "Jackfruit", "Kiwi", "Kumquat", "Lemon", "Lime",
		"Lychee", "Mango", "Mulberry", "Nectarine", "Olive", "Orange",
		"Papaya", "Passionfruit", "Peach", "Pear", "Persimmon", "Pineapple",
		"Plum", "Pomegranate", "Quince", "Raspberry", "Rhubarb", "Soursop",
		"Starfruit", "Strawberry", "Tamarind", "Tangerine", "Watermelon",
	};

	private static readonly ClayColor PanelBg     = ClayColor.Rgba(20, 24, 32, 230);
	private static readonly ClayColor RowBase     = ClayColor.Rgba(36, 42, 54, 255);
	private static readonly ClayColor RowHover    = ClayColor.Rgba(60, 70, 90, 255);
	private static readonly ClayColor RowPressed  = ClayColor.Rgba(80, 90, 120, 255);
	private static readonly ClayColor RowSelected = ClayColor.Rgba(60, 130, 220, 255);
	private static readonly ClayColor RowSelHover = ClayColor.Rgba(90, 150, 240, 255);

	public void Build(App app)
	{
		app.AddPlugin(new ScrollbarPlugin());
		app.AddPlugin(new CheckboxPlugin());
		app.AddPlugin(new SliderPlugin());
		app.AddResource(new FruitSelection());

		app.AddSystem((Commands commands) =>
		{
			// Fullscreen root, centers panel.
			var root = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Row,
					JustifyContent = JustifyContent.Center,
					AlignItems = AlignItems.Center,
					Width = Val.Percent(1f),
					Height = Val.Percent(1f),
				});

			// Outer panel: title + (list + scrollbar row) + status + checkbox + slider.
			var panel = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					AlignItems = AlignItems.Start,
					Width = Val.Px(280),
					Height = Val.Px(480),
					Padding = UiRect.All(12),
					Gap = Val.Px(8),
				})
				.Insert(new BackgroundColor(PanelBg))
				.Insert(new BorderRadius { TopLeft = 8, TopRight = 8, BottomLeft = 8, BottomRight = 8 });

			var title = commands.Spawn()
				.Insert(new Node { Width = Val.Auto, Height = Val.Auto })
				.Insert(new Text("Pick your favorite fruit"))
				.Insert(new TextFont { Size = 18 })
				.Insert(new TextColor(ClayColor.White));

			// Row that holds the scroll list and its scrollbar side by side.
			var listRow = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Row,
					AlignItems = AlignItems.Start,
					Width = Val.Px(256),
					Height = Val.Px(280),
					Gap = Val.Px(4),
				});

			// Scroll list — Overflow.Scroll makes it a Clay scroll container.
			var list = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					AlignItems = AlignItems.Start,
					Width = Val.Px(232),
					Height = Val.Px(280),
					Padding = UiRect.All(4),
					Gap = Val.Px(2),
					Overflow = Overflow.Scroll,
				})
				.Insert(new BackgroundColor(ClayColor.Rgba(12, 14, 20, 255)))
				.Insert(new BorderRadius { TopLeft = 4, TopRight = 4, BottomLeft = 4, BottomRight = 4 })
				.Insert(new ScrollPosition());

			// Scrollbar track + thumb. ScrollbarPlugin sizes/positions the thumb each frame.
			// Track carries Interaction + FocusPolicy so clicking the rail jumps scroll.
			var bar = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					Width = Val.Px(12),
					Height = Val.Px(280),
				})
				.Insert(new BackgroundColor(ClayColor.Rgba(0, 0, 0, 80)))
				.Insert(new BorderRadius { TopLeft = 4, TopRight = 4, BottomLeft = 4, BottomRight = 4 })
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new Scrollbar
				{
					Target = list.Id,
					Orientation = ScrollbarOrientation.Vertical,
					MinThumbLength = 24,
				});

			var thumb = commands.Spawn()
				.Insert(new Node())
				.Insert(new ScrollbarThumb())
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new ScrollbarDragState())
				.Insert(new BackgroundColor(ClayColor.Rgba(180, 200, 230, 220)))
				.Insert(new BorderRadius { TopLeft = 4, TopRight = 4, BottomLeft = 4, BottomRight = 4 });

			commands.AddChild(root, panel);
			commands.AddChild(panel, title);
			commands.AddChild(panel, listRow);
			commands.AddChild(listRow, list);
			commands.AddChild(listRow, bar);
			commands.AddChild(bar, thumb);

			foreach (var fruit in Fruits)
			{
				var row = commands.Spawn()
					.Insert(new Node
					{
						Display = Display.Flex,
						FlexDirection = FlexDirection.Row,
						AlignItems = AlignItems.Center,
						JustifyContent = JustifyContent.Start,
						Width = Val.Px(220),
						Height = Val.Px(26),
						Padding = UiRect.Symmetric(10, 2),
					})
					.Insert(new Interaction())
					.Insert(new FocusPolicy { Block = true })
					.Insert(new FruitItem { Name = fruit })
					.Insert(new BackgroundColor(RowBase))
					.Insert(new BorderRadius { TopLeft = 3, TopRight = 3, BottomLeft = 3, BottomRight = 3 });

				var rowText = commands.Spawn()
					.Insert(new Node())
					.Insert(new Text(fruit))
					.Insert(new TextFont { Size = 16 })
					.Insert(new TextColor(ClayColor.White));

				commands.AddChild(list, row);
				commands.AddChild(row, rowText);
			}

			// Status line: "Selected: <name>".
			var status = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					Width = Val.Auto,
					Height = Val.Auto,
					Padding = UiRect.Vertical(4),
				})
				.Insert(new Text("Selected: (none)"))
				.Insert(new TextFont { Size = 14 })
				.Insert(new TextColor(ClayColor.Rgba(180, 200, 230, 255)))
				.Insert(new StatusLabel());

			commands.AddChild(panel, status);

			// Checkbox row.
			var checkboxRow = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Row,
					AlignItems = AlignItems.Center,
					Gap = Val.Px(8),
					Width = Val.Px(256),
					Height = Val.Px(28),
				});

			var checkbox = commands.Spawn()
				.Insert(new Node { Display = Display.Flex, Width = Val.Px(20), Height = Val.Px(20) })
				.Insert(new Checkbox { Checked = false })
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new BackgroundColor(ClayColor.Rgba(40, 48, 64, 255)))
				.Insert(new BorderRadius { TopLeft = 3, TopRight = 3, BottomLeft = 3, BottomRight = 3 });

			var checkboxLabel = commands.Spawn()
				.Insert(new Node())
				.Insert(new Text("I really like fruit"))
				.Insert(new TextFont { Size = 14 })
				.Insert(new TextColor(ClayColor.White));

			commands.AddChild(panel, checkboxRow);
			commands.AddChild(checkboxRow, checkbox);
			commands.AddChild(checkboxRow, checkboxLabel);

			// Slider row.
			var sliderRow = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Row,
					AlignItems = AlignItems.Center,
					Gap = Val.Px(8),
					Width = Val.Px(256),
					Height = Val.Px(28),
				});

			var sliderTrack = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					Width = Val.Px(180),
					Height = Val.Px(12),
				})
				.Insert(new Slider
				{
					Min = 0, Max = 100, Value = 50,
					ThumbLength = 18,
					Orientation = ScrollbarOrientation.Horizontal,
				})
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new BackgroundColor(ClayColor.Rgba(40, 48, 64, 255)))
				.Insert(new BorderRadius { TopLeft = 6, TopRight = 6, BottomLeft = 6, BottomRight = 6 });

			var sliderThumb = commands.Spawn()
				.Insert(new Node())
				.Insert(new SliderThumb())
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new SliderDragState())
				.Insert(new BackgroundColor(ClayColor.Rgba(180, 200, 230, 240)))
				.Insert(new BorderRadius { TopLeft = 6, TopRight = 6, BottomLeft = 6, BottomRight = 6 });

			var sliderValueLabel = commands.Spawn()
				.Insert(new Node { Width = Val.Px(50), Height = Val.Auto })
				.Insert(new Text("50"))
				.Insert(new TextFont { Size = 14 })
				.Insert(new TextColor(ClayColor.White))
				.Insert(new SliderValueLabel());

			commands.AddChild(panel, sliderRow);
			commands.AddChild(sliderRow, sliderTrack);
			commands.AddChild(sliderTrack, sliderThumb);
			commands.AddChild(sliderRow, sliderValueLabel);

			// Bubble-propagation demo. The button is a child of `buttonRow` which
			// is a child of `panel`. Clicking the button fires UiClick with
			// propagate=true; the button's entity observer runs first, then the
			// trigger walks up Parent links and fires the row's observer and the
			// panel's observer with the SAME trigger.EntityId (the button).
			var buttonRow = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Row,
					AlignItems = AlignItems.Center,
					Gap = Val.Px(8),
					Width = Val.Px(256),
					Height = Val.Px(32),
				});

			var button = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					Width = Val.Px(120),
					Height = Val.Px(28),
					Padding = UiRect.Symmetric(8, 4),
					JustifyContent = JustifyContent.Center,
					AlignItems = AlignItems.Center,
				})
				.Insert(new Button())
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new BackgroundColor(ClayColor.Rgba(60, 130, 220, 255)))
				.Insert(new BorderRadius { TopLeft = 4, TopRight = 4, BottomLeft = 4, BottomRight = 4 });

			var buttonText = commands.Spawn()
				.Insert(new Node())
				.Insert(new Text("Send"))
				.Insert(new TextFont { Size = 14 })
				.Insert(new TextColor(ClayColor.White));

			commands.AddChild(panel, buttonRow);
			commands.AddChild(buttonRow, button);
			commands.AddChild(button, buttonText);

			// Entity-targeted observers (one per ancestor) — fired in bubble order.
			button.Observe<On<UiClick>>((trigger) =>
				Console.WriteLine($"  [button]  click on entity {trigger.EntityId}"));
			buttonRow.Observe<On<UiClick>>((trigger) =>
				Console.WriteLine($"  [row]     bubbled click from entity {trigger.EntityId}"));
			panel.Observe<On<UiClick>>((trigger) =>
				Console.WriteLine($"  [panel]   bubbled click from entity {trigger.EntityId}"));

			// ZIndex demo: two overlapping absolute squares pinned to the top-right
			// of the panel. Clicking either square swaps their ZIndex so they
			// reorder visually. ZIndex/GlobalZIndex are only honoured on
			// PositionType.Absolute elements.
			var stackHolder = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Top = Val.Px(8),
					Right = Val.Px(8),
					Width = Val.Px(70),
					Height = Val.Px(50),
				});

			var squareA = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(0),
					Top = Val.Px(0),
					Width = Val.Px(40),
					Height = Val.Px(40),
				})
				.Insert(new BackgroundColor(ClayColor.Rgba(220, 80, 80, 230)))
				.Insert(new BorderRadius { TopLeft = 4, TopRight = 4, BottomLeft = 4, BottomRight = 4 })
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new ZIndex(1))
				.Insert(new ZSwapTag { Id = 1 });

			var squareB = commands.Spawn()
				.Insert(new Node
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(20),
					Top = Val.Px(10),
					Width = Val.Px(40),
					Height = Val.Px(40),
				})
				.Insert(new BackgroundColor(ClayColor.Rgba(80, 160, 220, 230)))
				.Insert(new BorderRadius { TopLeft = 4, TopRight = 4, BottomLeft = 4, BottomRight = 4 })
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new ZIndex(2))
				.Insert(new ZSwapTag { Id = 2 });

			commands.AddChild(panel, stackHolder);
			commands.AddChild(stackHolder, squareA);
			commands.AddChild(stackHolder, squareB);
		})
		.InStage(Stage.Startup)
		.After("raylib:create-window")
		.SingleThreaded()
		.Build();

		// Click → update selection. Entity-targeted trigger fires on the row entity.
		app.AddObserver<On<UiClick>, Query<Data<FruitItem>>, ResMut<FruitSelection>>((trigger, rows, sel) =>
		{
			if (!rows.Contains(trigger.EntityId))
				return;
			var (_, item) = rows.Get(trigger.EntityId);
			sel.Value.Selected = item.Ref.Name;
		});

		// Click a ZSwap square → bump it past the current top, but only if it
		// isn't already on top (avoid runaway ZIndex on repeated clicks).
		app.AddObserver<On<UiClick>, Query<Data<ZSwapTag, ZIndex>>>((trigger, squares) =>
		{
			if (!squares.Contains(trigger.EntityId))
				return;

			int maxZ = int.MinValue;
			foreach (var (_e, _tag, z) in squares)
				if (z.Ref.Value > maxZ) maxZ = z.Ref.Value;

			var (_, _tag2, myZ) = squares.Get(trigger.EntityId);
			if (myZ.Ref.Value >= maxZ)
				return;
			myZ.Ref.Value = maxZ + 1;
			Console.WriteLine($"  [zswap]   entity {trigger.EntityId} now z={myZ.Ref.Value}");
		});

		// Tint rows by selection × interaction state.
		app.AddSystem((Res<FruitSelection> sel, Query<Data<FruitItem, Interaction, BackgroundColor>> rows) =>
		{
			var selected = sel.Value.Selected;
			foreach (var (_, item, interaction, bg) in rows)
			{
				var isSelected = item.Ref.Name == selected;
				bg.Ref.Value = (isSelected, interaction.Ref) switch
				{
					(true,  Interaction.Pressed) => RowPressed,
					(true,  _)                   => isSelected ? RowSelHover : RowSelected,
					(false, Interaction.Pressed) => RowPressed,
					(false, Interaction.Hovered) => RowHover,
					_                            => RowBase,
				};
				// Selected wins unless pressed; correct above mapping.
				if (isSelected && interaction.Ref != Interaction.Pressed)
					bg.Ref.Value = interaction.Ref == Interaction.Hovered ? RowSelHover : RowSelected;
			}
		})
		.InStage(Stage.Update)
		.SingleThreaded()
		.Build();

		// Update status label text.
		app.AddSystem((Res<FruitSelection> sel, Query<Data<Text>, Filter<With<StatusLabel>>> labels) =>
		{
			foreach (var (_, txt) in labels)
			{
				var sel2 = sel.Value.Selected;
				txt.Ref.Value = string.IsNullOrEmpty(sel2)
					? "Selected: (none)"
					: $"Selected: {sel2}";
			}
		})
		.InStage(Stage.Update)
		.SingleThreaded()
		.Build();

		// Recolor checkbox by Checked state.
		app.AddSystem((Query<Data<Checkbox, BackgroundColor>> boxes) =>
		{
			foreach (var (_, cb, bg) in boxes)
			{
				bg.Ref.Value = cb.Ref.Checked
					? ClayColor.Rgba(80, 180, 120, 255)
					: ClayColor.Rgba(40, 48, 64, 255);
			}
		})
		.InStage(Stage.Update)
		.SingleThreaded()
		.Build();

		// Update slider value label.
		app.AddSystem((Query<Data<Slider>> sliders, Query<Data<Text>, Filter<With<SliderValueLabel>>> labels) =>
		{
			float value = 0;
			foreach (var (_, s) in sliders) value = s.Ref.Value;
			foreach (var (_, t) in labels) t.Ref.Value = ((int)value).ToString();
		})
		.InStage(Stage.Update)
		.SingleThreaded()
		.Build();
	}
}

// Marker component for the status text element. Has a single byte field to
// avoid the empty-struct archetype hazard noted in CLAUDE.md.
struct StatusLabel
{
	public byte _;
}

struct SliderValueLabel
{
	public byte _;
}

struct ZSwapTag
{
	public byte Id;
}
