use bevy::prelude::*;

use crate::{ui_builder::*, ui_style::SetImageExt};

#[derive(Component, Debug, Default, Reflect)]
#[reflect(Component)]
pub struct Thumbnail;

#[derive(Component, Clone, Debug, Default, Reflect)]
#[reflect(Component)]
pub struct ThumbnailConfig {
    pub name: String,
    pub alt_code: Option<KeyCode>,
    pub leading_icon: Option<String>,
}
//Label config added?

impl Thumbnail {
    fn bundle() -> impl Bundle {
        ImageBundle {
            style: Style {
                width: Val::Px(16.),
                height: Val::Px(16.),
                ..default()
            },
            ..default()
        }
    }
}

pub trait UiIconExt<'w, 's> {
    fn icon<'a>(&'a mut self, path: impl Into<String>) -> UiBuilder<'w, 's, 'a, Entity>;
}

impl<'w, 's> UiIconExt<'w, 's> for UiBuilder<'w, 's, '_, Entity> {
    fn icon<'a>(&'a mut self, path: impl Into<String>) -> UiBuilder<'w, 's, 'a, Entity> {
        let mut icon = self.spawn((Icon::bundle(), Icon));

        icon.style().image(path);

        icon
    }
}