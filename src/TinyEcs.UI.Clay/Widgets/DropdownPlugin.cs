using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds dropdown widget systems to the application.
/// </summary>
public struct DropdownPlugin : IPlugin
{
	public void Build(App app)
	{
		// Observer to handle dropdown toggle requests
		app.AddObserver<On<DropdownToggleRequested>, Commands, Query<Data<DropdownState>>>((trigger, commands, dropdowns) =>
		{
			var dropdownEntityId = trigger.EntityId;

			if (!dropdowns.Contains(dropdownEntityId))
				return;

			var (_, statePtr) = dropdowns.Get(dropdownEntityId);
			var state = statePtr.Ref;

			// Toggle the open state
			state.IsOpen = !state.IsOpen;

			if (state.IsOpen)
			{
				// Spawn the menu entity
				var menuId = DropdownWidget.SpawnDropdownMenu(commands, dropdownEntityId, state);
				state.MenuEntityId = menuId;
			}
			else
			{
				// Despawn the menu entity if it exists
				if (state.MenuEntityId != 0)
				{
					commands.Entity(state.MenuEntityId).Despawn();
					state.MenuEntityId = 0;
				}
			}

			commands.Entity(dropdownEntityId).Insert(state);
		});

		// Observer to handle dropdown value changes
		app.AddObserver<On<DropdownValueChanged>, Commands, Query<Data<DropdownState>>>((trigger, commands, dropdowns) =>
		{
			var dropdownEntityId = trigger.EntityId;
			var valueChanged = trigger.Event;

			Console.WriteLine($"[Dropdown] Value changed - Index: {valueChanged.SelectedIndex}, Value: {valueChanged.SelectedValue}");

			if (!dropdowns.Contains(dropdownEntityId))
				return;

			var (_, statePtr) = dropdowns.Get(dropdownEntityId);
			var state = statePtr.Ref;

			Console.WriteLine($"[Dropdown] Previous state - SelectedIndex: {state.SelectedIndex}");

			// Update selected index
			state.SelectedIndex = valueChanged.SelectedIndex;

			Console.WriteLine($"[Dropdown] New state - SelectedIndex: {state.SelectedIndex}");

			// Update button text
			var buttonText = $"{state.Label}: {valueChanged.SelectedValue}";
			commands.Entity(state.ButtonTextEntityId).Insert(new DropdownButtonUpdate
			{
				Text = buttonText
			});

			// Despawn the menu entity
			if (state.MenuEntityId != 0)
			{
				commands.Entity(state.MenuEntityId).Despawn();
				state.MenuEntityId = 0;
			}

			// Close the dropdown
			state.IsOpen = false;
			commands.Entity(dropdownEntityId).Insert(state);

			Console.WriteLine($"[Dropdown] State updated and inserted back to entity {dropdownEntityId}");
		});

		// System to update dropdown button text
		app.AddSystem((Commands commands, Query<Data<DropdownButtonUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				// Update text
				if (node.Text != null)
				{
					var text = node.Text.Value;
					text.Text = update.Text;
					node.Text = text;
					commands.Entity(entityId.Ref).Insert(node);
				}
			}
		})
		.InStage(Stage.First)
		.Label("dropdown:update-button-text")
		.Build();

		// System to detect clicks outside dropdown to close it
		app.AddSystem((Commands commands, Res<ClayPointerState> pointer, Query<Data<DropdownState, ClayComputedLayout>> dropdowns, Query<Data<ClayComputedLayout>> layouts) =>
		{
			// Only process on click
			if (!pointer.Value.PrimaryPressed)
				return;

			var mouseX = pointer.Value.Position.X;
			var mouseY = pointer.Value.Position.Y;

			foreach (var (entityId, statePtr, containerLayoutPtr) in dropdowns)
			{
				var state = statePtr.Ref;

				// Only check if dropdown is open
				if (!state.IsOpen)
					continue;

				var containerLayout = containerLayoutPtr.Ref;

				// Get button and menu layouts
				if (!layouts.Contains(state.ButtonEntityId) || state.MenuEntityId == 0 || !layouts.Contains(state.MenuEntityId))
					continue;

				var (_, buttonLayoutPtr) = layouts.Get(state.ButtonEntityId);
				var (_, menuLayoutPtr) = layouts.Get(state.MenuEntityId);
				var buttonLayout = buttonLayoutPtr.Ref;
				var menuLayout = menuLayoutPtr.Ref;

				// Check if click is outside both button and menu
				bool clickInButton = mouseX >= buttonLayout.X && mouseX <= buttonLayout.X + buttonLayout.Width &&
									 mouseY >= buttonLayout.Y && mouseY <= buttonLayout.Y + buttonLayout.Height;

				bool clickInMenu = mouseX >= menuLayout.X && mouseX <= menuLayout.X + menuLayout.Width &&
								   mouseY >= menuLayout.Y && mouseY <= menuLayout.Y + menuLayout.Height;

				// If click is outside, close the dropdown
				if (!clickInButton && !clickInMenu)
				{
					// Despawn the menu entity
					if (state.MenuEntityId != 0)
					{
						commands.Entity(state.MenuEntityId).Despawn();
						state.MenuEntityId = 0;
					}

					state.IsOpen = false;
					commands.Entity(entityId.Ref).Insert(state);
				}
			}
		})
		.InStage(Stage.Update)
		.Label("dropdown:handle-click-outside")
		.Build();
	}
}
