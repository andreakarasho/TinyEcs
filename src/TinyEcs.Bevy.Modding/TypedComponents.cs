// Typed (no-JSON) component path for the tinyecs:modding modding API. Maps the WIT
// `component-value` variant (native records) <-> host TinyEcs.Bevy.UI components,
// with no serialization. The string/JSON registry (ModComponentRegistry) still
// backs the `spawn`/`get`/`set` methods; this backs `spawn-typed`/`insert-typed`/
// `set-typed`. Core Bevy.UI set only — extend the switch + the WIT variant
// together when sweeping the rest.

using TinyEcs;
using TinyEcs.Bevy.UI;
using WitApp = Wit.Tinyecs.Modding.App;
using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.Modding;

internal static class ModTypedComponents
{
    // ── scalar/record converters (WIT record <-> host struct) ────────────────
    private static Val ToVal(in WitApp.Val v) => new((ValType)v.Kind, v.Value);
    private static UiRect ToRect(in WitApp.UiRect r) => new(ToVal(r.Left), ToVal(r.Right), ToVal(r.Top), ToVal(r.Bottom));
    private static ClayColor ToColor(in WitApp.Color c) => new(c.R, c.G, c.B, c.A);

    private static Node ToNode(in WitApp.NodeRec n) => new()
    {
        Display = (Display)n.Display,
        PositionType = (PositionType)n.PositionType,
        Overflow = (Overflow)n.Overflow,
        FlexDirection = (FlexDirection)n.FlexDirection,
        JustifyContent = (JustifyContent)n.JustifyContent,
        AlignItems = (AlignItems)n.AlignItems,
        Width = ToVal(n.Width), Height = ToVal(n.Height),
        MinWidth = ToVal(n.MinWidth), MinHeight = ToVal(n.MinHeight),
        MaxWidth = ToVal(n.MaxWidth), MaxHeight = ToVal(n.MaxHeight),
        Left = ToVal(n.Left), Top = ToVal(n.Top), Right = ToVal(n.Right), Bottom = ToVal(n.Bottom),
        Padding = ToRect(n.Padding), Border = ToRect(n.Border),
        Gap = ToVal(n.Gap), AspectRatio = n.AspectRatio,
    };

    // Apply a typed component-value to an entity (spawn-typed / insert-typed /
    // set-typed). The variant case IS the component type.
    public static void Apply(World w, ulong e, WitApp.ComponentValue cv)
    {
        switch (cv.Discriminant)
        {
            case WitApp.ComponentValue.Case.Node: w.Set(e, ToNode(cv.NodePayload)); break;
            case WitApp.ComponentValue.Case.Text: w.Set(e, new Text(cv.TextPayload.Value)); break;
            case WitApp.ComponentValue.Case.TextFont: w.Set(e, new TextFont { FontId = cv.TextFontPayload.FontId, Size = cv.TextFontPayload.Size }); break;
            case WitApp.ComponentValue.Case.TextColor: w.Set(e, new TextColor(ToColor(cv.TextColorPayload))); break;
            case WitApp.ComponentValue.Case.BgColor: w.Set(e, new BackgroundColor(ToColor(cv.BgColorPayload))); break;
            case WitApp.ComponentValue.Case.Interaction: w.Set(e, (Interaction)cv.InteractionPayload); break;
            case WitApp.ComponentValue.Case.GlobalZ: w.Set(e, new GlobalZIndex(cv.GlobalZPayload.Value)); break;
            case WitApp.ComponentValue.Case.UiName: w.Set(e, new UiName { Value = cv.UiNamePayload.Value }); break;
            case WitApp.ComponentValue.Case.Movable: w.Set(e, default(UiMovable)); break;
            case WitApp.ComponentValue.Case.NoWindowDrag: w.Set(e, default(UiNoWindowDrag)); break;
            case WitApp.ComponentValue.Case.ContainsByBounds: w.Set(e, default(UiContainsByBounds)); break;
        }
    }
}
