# Sickle UI

A widget library built on top of `bevy_ui`.

## Example

```
cargo run --example simple_editor
```

**THIS IS CURRENTLY IN HEAVY DEVELOPMENT**

Do not depend on this project. It is here for reference for the moment. If you 
still want to try it locally from within your project, clone the repository 
next to yours. This is needed for the asset source to find the local assets.

Main missing features:
- Centralized focus management
- Centralized theming support
- Text / Text area input widgets
- Documentation

What it can already do:
- Resizable layout
  - Rows / columns
  - Scroll views
  - Docking zones
  - Tab containers
  - Floating panels
  - Sized zones
  - Foldables
- Input
  - Slider
  - Dropdown
  - Checkbox
  - Radio groups
- Menu
  - Menu item (with leading/trailing icons and support for keyboard shortcuts)
  - Toggle menu item
  - Submenu
  - Context menu (component-based)
- Static
  - Icon
  - Label
- Utility
  - Command-based styling
  - Temporal tracking of interactions
  - Animated interactions
  - Context based extensions
  - Drag / drop interactions
  - Scroll interactions

