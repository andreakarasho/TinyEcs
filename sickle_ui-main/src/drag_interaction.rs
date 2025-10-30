use bevy::{
    prelude::*,
    window::{CursorGrabMode, PrimaryWindow},
};
use bevy_reflect::Reflect;

use crate::{FluxInteraction, FluxInteractionUpdate};

pub struct DragInteractionPlugin;

impl Plugin for DragInteractionPlugin {
    fn build(&self, app: &mut App) {
        app.configure_sets(Update, DraggableUpdate.after(FluxInteractionUpdate))
            .add_systems(
                Update,
                (
                    update_drag_progress,
                    update_drag_state,
                    update_cursor_confinement_from_drag,
                )
                    .chain()
                    .in_set(DraggableUpdate),
            );
    }
}

#[derive(SystemSet, Clone, Eq, Debug, Hash, PartialEq)]
pub struct DraggableUpdate;

#[derive(Component, Clone, Copy, Default, Debug, Reflect)]
#[reflect(Component)]
pub struct Draggable {
    pub state: DragState,
    pub origin: Option<Vec2>,
    pub position: Option<Vec2>,
    pub diff: Option<Vec2>,
    pub source: DragSource,
}

impl Draggable {
    fn clear(&mut self) {
        self.origin = None;
        self.position = None;
        self.diff = Vec2::default().into();
    }
}

#[derive(Clone, Copy, Default, Debug, PartialEq, Eq, Reflect)]
#[reflect]
pub enum DragState {
    #[default]
    Inactive,
    MaybeDragged,
    DragStart,
    Dragging,
    DragEnd,
    DragCanceled,
}

#[derive(Clone, Copy, Default, Debug, PartialEq, Eq, Reflect)]
#[reflect]
pub enum DragSource {
    #[default]
    Mouse,
    Touch(u64),
}

fn update_cursor_confinement_from_drag(
    q_draggable: Query<&Draggable, Changed<Draggable>>,
    mut q_window: Query<&mut Window, With<PrimaryWindow>>,
) {
    let Ok(mut window) = q_window.get_single_mut() else {
        return;
    };

    if let Some(_) = q_draggable
        .iter()
        .find(|&draggable| draggable.state == DragState::DragStart)
    {
        window.cursor.grab_mode = CursorGrabMode::Confined;
    } else if let Some(_) = q_draggable.iter().find(|&draggable| {
        draggable.state == DragState::DragEnd || draggable.state == DragState::DragCanceled
    }) {
        window.cursor.grab_mode = CursorGrabMode::None;
    }
}

// TODO: Consider using MouseMotion and TouchInput events directly
// TODO: Remove dependency on PrimaryWindow
fn update_drag_progress(
    mut q_draggable: Query<(&mut Draggable, &FluxInteraction)>,
    q_window: Query<&Window, With<PrimaryWindow>>,
    r_touches: Res<Touches>,
    r_keys: Res<ButtonInput<KeyCode>>,
) {
    let Ok(window) = q_window.get_single() else {
        return;
    };

    for (mut draggable, flux_interaction) in &mut q_draggable {
        if draggable.state == DragState::DragEnd {
            draggable.state = DragState::Inactive;
            draggable.clear();
        } else if draggable.state == DragState::DragCanceled {
            draggable.state = DragState::Inactive;
        } else if *flux_interaction == FluxInteraction::Pressed
            && (draggable.state == DragState::MaybeDragged
                || draggable.state == DragState::DragStart
                || draggable.state == DragState::Dragging)
        {
            if (draggable.state == DragState::DragStart || draggable.state == DragState::Dragging)
                && r_keys.just_pressed(KeyCode::Escape)
            {
                draggable.state = DragState::DragCanceled;
                draggable.clear();
                continue;
            }

            // Drag start is only a single frame, triggered after initial movement
            if draggable.state == DragState::DragStart {
                draggable.state = DragState::Dragging;
            }

            let position: Option<Vec2> = match draggable.source {
                DragSource::Mouse => window.cursor_position(),
                DragSource::Touch(id) => match r_touches.get_pressed(id) {
                    Some(touch) => touch.position().into(),
                    None => None,
                },
            };

            if let (Some(current_position), Some(new_position)) = (draggable.position, position) {
                let diff = new_position - current_position;

                // No tolerance threshold, just move
                if diff.length_squared() > 0. {
                    if draggable.state == DragState::MaybeDragged {
                        draggable.state = DragState::DragStart;
                    }

                    draggable.position = new_position.into();
                    draggable.diff = (new_position - current_position).into();
                }
            }
        }
    }
}

fn update_drag_state(
    mut q_draggable: Query<(&mut Draggable, &FluxInteraction), Changed<FluxInteraction>>,
    q_window: Query<&Window, With<PrimaryWindow>>,
    r_touches: Res<Touches>,
) {
    for (mut draggable, flux_interaction) in &mut q_draggable {
        if *flux_interaction == FluxInteraction::Pressed
            && draggable.state != DragState::MaybeDragged
        {
            let mut drag_source = DragSource::Mouse;
            let mut position = q_window.single().cursor_position();
            if position.is_none() {
                position = r_touches.first_pressed_position();
                drag_source = DragSource::Touch(r_touches.iter().next().unwrap().id());
            }

            draggable.state = DragState::MaybeDragged;
            draggable.source = drag_source;
            draggable.origin = position;
            draggable.position = position;
            draggable.diff = Vec2::default().into();
        } else if *flux_interaction == FluxInteraction::Released
            || *flux_interaction == FluxInteraction::PressCanceled
        {
            if draggable.state == DragState::DragStart || draggable.state == DragState::Dragging {
                draggable.state = DragState::DragEnd;
            } else if draggable.state == DragState::MaybeDragged {
                draggable.state = DragState::Inactive;
                draggable.clear();
            }
        }
    }
}
