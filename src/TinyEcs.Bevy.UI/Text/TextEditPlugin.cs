// stb_textedit integration for the shared editable text fields (anything
// UiTextField spawns, and self-editable nodes). Routes the mouse, keys and
// composed chars of the ONE focused field (InputFocus) into the ActiveTextEdit
// state machine (see ActiveTextEdit.cs for the buffer, the measurement seams and
// the row table), then writes the edited value back to the field.
//
// Rendering: UiTextField gives each field two absolute overlay nodes — a
// selection rect (behind the glyphs) and a 2px caret bar — positioned every
// frame by measuring the display text up to the cursor / selection bounds with
// the bound ITextEditMeasurer. Only the focused field's overlays are shown.
//
// A SELF-EDITABLE node (one entity with Text + EditableText and no overlay
// children) shares this editor, but gets its caret / selection painted from
// geometry instead of entities: see SelfEditOverlay. With EditableText.Multiline
// it becomes a multi-line code editor: Enter and Tab are text, arrows walk rows,
// and the overlay lays out per rendered row.
//
// Keys are read RAW: UiFocusPlugin consumes the keys a focused field claims
// (Stage.First) so the host's game readers never see them, and the editor is the
// one reader that must.
//
// Fields with bespoke editors carry TextInput but NOT EditableText, so they keep
// their own readers + flex caret (TextCaret) and are untouched here.

using System.Text;
using TinyEcs.Bevy.Input;
using StbTextEdit;

namespace TinyEcs.Bevy.UI;

// RouteKeys' auto-repeat for the keys that are read as STATE edges rather than
// composed text. Typed characters already repeat because the platform re-delivers
// text input on every OS repeat tick; Enter, Tab, the arrows, Delete, Home/End,
// Page*, the Ctrl word moves and undo/redo are read off KeyboardInput, whose only
// edge is a press — one fire per physical press, however long it is held.
//
// The OS repeat flag never reaches us (the backend feeds a held-key SET once a
// frame). Hence a timer, on Res<Time>'s engine clock (never a wall clock — a
// paused clock pauses it), with the Windows defaults: one fire on the press, then
// nothing for DelayMs, then every IntervalMs. One key at a time, like a real
// keyboard's typematic.
public sealed class KeyRepeat
{
    public const float DelayMs = 400f;
    public const float IntervalMs = 33f;

    public ulong Entity;
    private KeyCode _held;
    private float _nextAt;

    public void Clear() { Entity = 0; _held = KeyCode.None; _nextAt = 0f; }

    public bool Edge(KeyboardInput kb, float nowMs, KeyCode key)
    {
        // A fresh press always fires and takes ownership — pressing a second key
        // mid-hold makes THAT one the repeating key, as the OS does.
        if (kb.IsPressedOnceRaw(key))
        {
            _held = key;
            _nextAt = nowMs + DelayMs;
            return true;
        }
        if (_held != key) return false;
        // IsPressed is "down in both of the last two snapshots", so it is true from
        // the frame after the press edge until the release.
        if (!kb.IsPressedRaw(key)) { _held = KeyCode.None; return false; }
        if (nowMs < _nextAt) return false;
        _nextAt = nowMs + IntervalMs;
        return true;
    }
}

public sealed class UiTextEditPlugin : IPlugin
{
    // Caret blink half-period (ms of engine clock). Shared by the entity-based
    // host overlays and the resource-based self-editable overlay so every field
    // in the client blinks in lockstep.
    public const float CaretBlinkMs = 530f;

    /// <param name="hold">ActiveTextEdit.CaretBlinkHold — pins the blink phase for
    /// deterministic screenshots.</param>
    public static bool CaretOn(float totalMs, bool? hold = null)
    {
        if (hold.HasValue) return hold.Value;
        return (int)(totalMs / CaretBlinkMs) % 2 == 0;
    }

    // Tab width in a multiline field. Spaces, not '\t': bitmap fonts often have
    // no tab advance, so a literal tab would render as a single blank glyph and
    // the caret column math would disagree with what is drawn.
    public const int TabSpaces = 4;

    public void Build(App app)
    {
        if (!app.HasResource<ActiveTextEdit>()) app.AddResource(new ActiveTextEdit());
        if (!app.HasResource<UiClipboard>()) app.AddResource(new UiClipboard());
        var edit = app.GetResource<ActiveTextEdit>();
        edit.ClipboardStore = app.GetResource<UiClipboard>();
        // Caret math measures with the layout's own measurer when a UI is
        // installed; a UI-less app keeps the fallback (or a test's seams).
        if (app.HasResource<UiTextMeasure>())
            edit.Bind(TextEditMeasurer.For(app.GetResource<UiTextMeasure>().Measurer));
        // The geometry SYSTEM needs the Clay layout (RunIf below); the resource is
        // registered unconditionally so RouteMouse resolves in UI-less apps.
        if (!app.HasResource<SelfEditOverlay>()) app.AddResource(new SelfEditOverlay());
        if (!app.HasResource<TextCompletion>()) app.AddResource(new TextCompletion());
        app.AddPlugin(new UiFocusPlugin());

        // Mirror MaskedText.Value into Text as mask chars before layout reads Text.
        app.AddSystem((Query<Data<Text, MaskedText>> q) => SyncMaskedText(q)).InStage(Stage.PreUpdate).Build();
        // Blink the flex caret glyph of bespoke fields that still use TextCaret.
        // EditableText fields get a measured caret + selection instead.
        app.AddSystem((Res<Time> time, Res<InputFocus> focus, Query<Data<Text, TextCaret>> carets) => CaretBlink(time, focus, carets))
            .InStage(Stage.PreUpdate).Build();

        Action<Res<InputFocus>, ResMut<ActiveTextEdit>,
            Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>, Without<MultilineText>>>,
            Query<Data<MaskedText>, Filter<With<EditableText>>>,
            Query<Data<TextFont>, With<EditableText>>,
            Query<Data<MultilineText>, With<EditableText>>,
            Query<Data<EditableText>>,
            Res<SelfEditOverlay>, Res<Time>> syncFn = SyncActiveEditor;
        Action<Res<MouseInput>, Res<Time>, ResMut<ActiveTextEdit>, Res<SelfEditOverlay>,
            Query<Data<ComputedNode>>, Query<Data<TextFieldGeom>>> mouseFn = RouteMouse;
        Action<Commands, Res<KeyboardInput>, Res<Time>, ResMut<ActiveTextEdit>, Local<KeyRepeat>,
            Res<TextCompletion>> keysFn = RouteKeys;
        Action<EventReader<CharInput>, Res<KeyboardInput>,
            ResMut<ActiveTextEdit>> charsFn = RouteChars;
        Action<Commands, ResMut<ActiveTextEdit>,
            Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>, Without<MultilineText>>>,
            Query<Data<MaskedText>, Filter<With<EditableText>>>,
            Query<Data<MultilineText>, With<EditableText>>,
            Query<Data<ReadOnlyText>>> writeBackFn = WriteBack;
        Action<Res<ActiveTextEdit>, Res<Time>,
            Query<Data<Node, TextEditCaret>>,
            Query<Data<Node, TextEditSelection>>,
            Query<Data<MultilineText>>> overlayFn = PositionOverlays;
        Action<Query<Data<Node, TextFont, MultilineText>>, Res<ActiveTextEdit>> sizeFn = SizeMultilineGlyphs;
        Action<Query<Data<Node, EditableText>,
            Filter<With<Text>, Without<MultilineText>, Without<TextFieldGeom>>>> clipFn = ClipEditorContent;

        // Declaration order is preserved within a stage (no explicit labels
        // needed). PreUpdate: sync focus, then position overlays. Update: route
        // keys, route chars, write the buffer back.
        app.AddSystem(syncFn).InStage(Stage.PreUpdate).Build();
        Action<Res<ActiveTextEdit>, Query<Data<TextFieldGeom>>, Query<Data<Node>>,
            Query<Data<ComputedNode>>> followFn = FollowCaret;
        app.AddSystem(followFn).InStage(Stage.PreUpdate).Build();
        app.AddSystem(overlayFn).InStage(Stage.PreUpdate).Build();
        app.AddSystem(mouseFn).InStage(Stage.Update).Build();
        app.AddSystem(keysFn).InStage(Stage.Update).Label(RouteKeysLabel).Build();
        app.AddSystem(charsFn).InStage(Stage.Update).Label(RouteCharsLabel).Build();
        app.AddSystem(writeBackFn).InStage(Stage.Update).Label(WriteBackLabel).Build();
        // After WriteBack so the wrapped glyph box tracks the just-edited text.
        app.AddSystem(sizeFn).InStage(Stage.Update).Build();
        app.AddSystem(clipFn).InStage(Stage.Update).Build();

        // Caret + selection geometry for SELF-EDITABLE nodes (no caret/selection
        // children). Reads the Clay layout, so only where a UI is installed.
        Action<Res<InputFocus>, Res<ActiveTextEdit>, Res<Time>, Res<UiClayContext>,
            ResMut<SelfEditOverlay>,
            Query<Data<Node>, Filter<With<EditableText>, Without<TextFieldGeom>>>,
            Query<Data<TextFont>>,
            Query<Data<TextColor>>> selfEditGeomFn = SelfEditOverlaySystem.Build;
        app.AddSystem(selfEditGeomFn).InStage(Stage.PreUpdate).RunIfResourceExists<UiClayContext>().Build();

        // A multiline self-editable field lives inside a scroll body; the field
        // can't scroll its own glyphs, so keep the caret line in the container's
        // viewport. After the geometry pass (it reads the caret band), before
        // layout consumes ScrollPosition.
        Action<Res<SelfEditOverlay>, Res<ActiveTextEdit>, Res<UiClayContext>,
            Local<SelfEditCaretScroll.FollowState>,
            Query<Data<TinyEcs.Parent>>,
            Query<Data<Node, ScrollPosition>>> caretScrollFn = SelfEditCaretScroll.Run;
        app.AddSystem(caretScrollFn).InStage(Stage.PreUpdate).RunIfResourceExists<UiClayContext>().Build();

        // Click-to-focus for an element that is its OWN editable target. A
        // UiTextField field can't use this — its hittable parts (frame, row,
        // overlays) are not the glyph, so they focus it through the per-part
        // observers that capture the glyph id. An element carrying EditableText
        // itself has no such indirection — the shape a remote UI builder makes:
        // one node with Text + EditableText, and no way to attach an observer.
        //
        // The same press also BLURS such a field when it lands anywhere else —
        // what every desktop toolkit does, and what the field's own window cannot
        // do for itself when its owner can't observe presses. Focus goes to 0;
        // a host that keeps a default keyboard sink reclaims it.
        //
        // UiTextField fields are excluded (TextFieldGeom): their hittable parts
        // are separate entities that re-focus the glyph through their own
        // observers, so a press on the field's own frame/row/overlay would blur
        // and refocus on the same press — and clicking away from one is the
        // owner's business.
        app.AddObserver<On<UiPointerDown>, ResMut<InputFocus>, ResMut<ActiveTextEdit>,
            Query<Data<EditableText>>, Query<Data<TextFieldGeom>>, Query<Data<UiKeepFocusOnClickAway>>,
            Query<Data<UiNoBlur>>, Query<Data<TinyEcs.Parent>>>(
            (trigger, focused, edit, editableQ, geomQ, stickyQ, noBlurQ, parentQ) =>
            {
                if (editableQ.Contains(trigger.EntityId))
                {
                    focused.Value.Entity = trigger.EntityId;
                    edit.Value.PendingClickEntity = trigger.EntityId;
                    edit.Value.PendingClickX = trigger.Event.Position.X;
                    edit.Value.PendingClickY = trigger.Event.Position.Y;
                    return;
                }

                // A press on a suggestion row (or inside one) completes the word in the
                // field that already holds focus; it must not blur it.
                if (PressKeepsFocus(trigger.EntityId, noBlurQ, parentQ))
                    return;

                BlurSelfEditable(focused.Value, editableQ, geomQ, stickyQ);
            });

        // ...and a press on no element at all (bare canvas) blurs it the same way.
        app.AddSystem((Res<UiPointer> pointer, Res<UiClayContext> ctx, Local<bool> prevDown, ResMut<InputFocus> focused,
                Query<Data<EditableText>> editableQ, Query<Data<TextFieldGeom>> geomQ, Query<Data<UiKeepFocusOnClickAway>> stickyQ) =>
            {
                var down = pointer.Value.Down;
                var edge = down && !prevDown.Value;
                prevDown.Value = down;
                if (edge && ctx.Value.PressedEntity == 0)
                    BlurSelfEditable(focused.Value, editableQ, geomQ, stickyQ);
            })
            .InStage(UiPlugin.UiPostLayoutStage)
            .RunIfResourceExists<UiClayContext>()
            .Build();
    }

    private static void BlurSelfEditable(InputFocus focused, Query<Data<EditableText>> editableQ,
        Query<Data<TextFieldGeom>> geomQ, Query<Data<UiKeepFocusOnClickAway>> stickyQ)
    {
        var current = focused.Entity;
        if (current == 0 || !editableQ.Contains(current)
            || geomQ.Contains(current) || stickyQ.Contains(current))
            return;
        focused.Entity = 0;
    }

    /// <summary>Stage.Update labels, for hosts that feed the editor (a completion
    /// pick, a clipboard request) or stamp TextConsumed late.</summary>
    public const string RouteKeysLabel = "ui:text_route_keys";
    public const string RouteCharsLabel = "ui:text_route_chars";
    public const string WriteBackLabel = "ui:text_write_back";

    // No Changed<MaskedText> filter: a producer may mutate MaskedText.Value through
    // a random-access query Get, which does not bump the change tick. Only
    // password fields carry both Text + MaskedText, so the scan is effectively free.
    private static void SyncMaskedText(Query<Data<Text, MaskedText>> query)
    {
        foreach (var (ent, text, masked) in query)
        {
            var value = masked.Ref.Value ?? string.Empty;
            var current = text.Ref.Value ?? string.Empty;

            // The mask is always a uniform run of MaskChar, so length + char
            // identity proves equality without allocating the candidate string
            // — this scan is unconditional, so an unconditional write would hold
            // the UI relayout gate open forever.
            if (current.Length == value.Length)
            {
                var same = true;
                for (var i = 0; i < current.Length; i++)
                    if (current[i] != masked.Ref.MaskChar) { same = false; break; }
                if (same) continue;
            }

            text.Ref.Value = value.Length == 0
                ? string.Empty
                : new string(masked.Ref.MaskChar, value.Length);
            query.SetChanged<Text>(ent.Ref);
        }
    }

    // Blink the caret glyph for any TextCaret entity: "_" while its linked input
    // is focused and in the on-phase, otherwise empty so it occupies no width.
    private static void CaretBlink(Res<Time> time, Res<InputFocus> focused, Query<Data<Text, TextCaret>> carets)
    {
        var on = (int)(time.Value.Total / CaretBlinkMs) % 2 == 0;
        var focusEnt = focused.Value.Entity;
        foreach (var (ent, text, caret) in carets)
        {
            var want = (caret.Ref.Target == focusEnt && on) ? "_" : string.Empty;
            if (text.Ref.Value == want) continue;
            text.Ref.Value = want;
            carets.SetChanged<Text>(ent.Ref);
        }
    }

    // The pressed element, or any ancestor of it, carries UiNoBlur. Walks Parent
    // upward because the owner tags the suggestion ROW, while the press lands on
    // whatever child (the label, a hover tint) Clay hit-tested. Bounded so a
    // malformed parent cycle can't hang the observer.
    private static bool PressKeepsFocus(ulong entity, Query<Data<UiNoBlur>> noBlur, Query<Data<TinyEcs.Parent>> parents)
    {
        var e = entity;
        for (var depth = 0; depth < 32 && e != 0; depth++)
        {
            if (noBlur.Contains(e)) return true;
            if (!parents.Contains(e)) return false;
            var (_, parent) = parents.Get(e);
            e = parent.Ref.Id;
        }
        return false;
    }

    // Mouse caret placement + drag-select. A press recorded by the field's
    // observer (PendingClick) sets the caret at the clicked glyph; holding +
    // moving extends the selection. localX is in the glyph's logical text space:
    // ComputedNode.Position, the mouse, and the handler's measured widths are
    // all logical px, so the glyph origin is just the frame's x + the content
    // row's left offset.
    public static void RouteMouse(
        Res<MouseInput> mouse,
        Res<Time> time,
        ResMut<ActiveTextEdit> edit,
        Res<SelfEditOverlay> selfEdit,
        Query<Data<ComputedNode>> computedQ,
        Query<Data<TextFieldGeom>> geomQ)
    {
        var a = edit.Value;
        // Recorded whether or not a field is focused: the press that matters for a
        // seeded caret (see SeedClickFrames) lands BEFORE the field exists.
        if (mouse.Value.IsPressedOnce(MouseButton.Left))
        {
            var p = mouse.Value.Position;
            a.LastPressX = p.X;
            a.LastPressY = p.Y;
            a.LastPressTime = time.Value.Total;
            a.LastPressConsumed = false;
        }
        if (a.Entity == 0)
        {
            a.PendingClickEntity = 0;
            a.SeedClickFrames = 0;
            return;
        }

        // Text origin (top-left of the rendered glyphs) in logical px.
        // Multiline: a multiline field's frame is an absolute, contentless wrapper
        // that gets NO ComputedNode, so the frame path (below) dropped every click
        // — caret never moved, no drag-select. The glyph itself always renders, so
        // it has a ComputedNode whose Position is exactly the text's top-left, and
        // a real Y for mapping the click to the right wrapped line.
        // Single-line: keep the original frame + OffsetX path unchanged.
        float glyphLogicalX, glyphLogicalY;
        if (a.CustomMultiline)
        {
            if (!computedQ.TryGet(a.Entity, out var glyphRow)) { a.PendingClickEntity = 0; return; }
            var (_, glyphCn) = glyphRow;
            glyphLogicalX = glyphCn.Ref.Position.X;
            glyphLogicalY = glyphCn.Ref.Position.Y;
        }
        else if (geomQ.TryGet(a.Entity, out var geomRow))
        {
            var (_, geom) = geomRow;
            if (!computedQ.TryGet(geom.Ref.Frame, out var frameRow)) { a.PendingClickEntity = 0; return; }
            var (_, frameCn) = frameRow;
            glyphLogicalX = frameCn.Ref.Position.X + geom.Ref.OffsetX - geom.Ref.Scroll;
            glyphLogicalY = 0;
        }
        else if (selfEdit.Value.Active && selfEdit.Value.Entity == a.Entity)
        {
            // Self-editable node: no frame + content-row indirection to walk —
            // SelfEditOverlaySystem already resolved where Clay drew the
            // glyphs, padding and cross-axis centring included.
            var so = selfEdit.Value;
            glyphLogicalX = so.TextX;
            glyphLogicalY = so.TextY;

            // Focus arrived by resource with a fresh press on record: if that press
            // fell inside this field's box, it IS the click — the node under the
            // cursor was swapped for this one before the press could be routed.
            if (a.SeedClickFrames > 0)
            {
                a.SeedClickFrames = 0;
                if (a.PendingClickEntity != a.Entity
                    && a.LastPressX >= so.ContentLeft && a.LastPressX <= so.ContentRight
                    && a.LastPressY >= so.ContentTop && a.LastPressY <= so.ContentBottom)
                {
                    a.PendingClickEntity = a.Entity;
                    a.PendingClickX = a.LastPressX;
                    a.PendingClickY = a.LastPressY;
                }
            }
        }
        else
        {
            a.PendingClickEntity = 0;
            // Geometry not resolved yet (the overlay binds a frame after focus): keep
            // the seed alive a little, drop it for a field that never gets one.
            if (a.SeedClickFrames > 0) a.SeedClickFrames--;
            return;
        }
        if (!a.Multiline || a.CustomMultiline) a.SeedClickFrames = 0;

        // stb maps the click via LayoutRow at this y; single-line is one row at
        // y=0, multiline needs the real row (line) the cursor was dropped on.
        float LocalY(float screenY) => a.Multiline ? screenY - glyphLogicalY : 0;

        if (!mouse.Value.IsPressed(MouseButton.Left))
            a.MouseSelecting = false;

        if (a.PendingClickEntity == a.Entity)
        {
            a.PendingClickEntity = 0;
            a.LastPressConsumed = true;
            a.RowGoalX = null;
            TextEdit.Click(a, a.State, a.PendingClickX - glyphLogicalX, LocalY(a.PendingClickY));
            switch (NextClickRank(a, time.Value.Total, mouse.Value.DoubleClickDelta))
            {
                case 2:
                    // Double-click selects the word under the caret. Leave
                    // MouseSelecting false so the held second click doesn't fall
                    // into the drag branch below and collapse the word back to a
                    // caret.
                    SelectWordAt(a);
                    a.MouseSelecting = false;
                    break;
                case 3:
                    // Triple-click: single-line fields select the whole
                    // field, multiline select the line under the caret.
                    // Same MouseSelecting hygiene as the double.
                    SelectTripleAt(a);
                    a.MouseSelecting = false;
                    break;
                default:
                    a.MouseSelecting = true;
                    break;
            }
        }
        else if (a.MouseSelecting && mouse.Value.IsPressed(MouseButton.Left))
        {
            a.RowGoalX = null;
            TextEdit.Drag(a, a.State, mouse.Value.Position.X - glyphLogicalX, LocalY(mouse.Value.Position.Y));
        }
    }

    // Keep ActiveTextEdit pointed at the focused EditableText field: (re)load the
    // buffer when focus moves to a new field, resync if the field's text was
    // changed externally (e.g. ChatPlugin clears it on submit), clear when focus
    // leaves all editable fields.
    public static void SyncActiveEditor(
        Res<InputFocus> focused,
        ResMut<ActiveTextEdit> edit,
        Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>, Without<MultilineText>>> textQ,
        Query<Data<MaskedText>, Filter<With<EditableText>>> maskedQ,
        Query<Data<TextFont>, With<EditableText>> fontQ,
        Query<Data<MultilineText>, With<EditableText>> multiQ,
        Query<Data<EditableText>> flagQ,
        Res<SelfEditOverlay> selfEdit,
        Res<Time> time)
    {
        var e = focused.Value.Entity;
        var a = edit.Value;

        bool isMulti = e != 0 && multiQ.Contains(e);
        bool isMasked = e != 0 && !isMulti && maskedQ.Contains(e);
        bool isPlain = e != 0 && !isMulti && !isMasked && textQ.Contains(e);
        // Focus is on nothing, or on something that is not (or no longer) an
        // editable field — its entity may have been despawned out from under us.
        // Unbind, buffer included: see ActiveTextEdit.Unbind.
        if (!isMulti && !isMasked && !isPlain) { a.Unbind(); return; }

        // A plain-Text field asking for the multi-line editor (the code-field
        // shape). Masked fields never qualify: a password has no lines.
        bool selfMulti = false;
        if (isPlain && flagQ.TryGet(e, out var flagRow))
        {
            var (_, flag) = flagRow;
            selfMulti = flag.Ref.Multiline;
        }
        bool wantMulti = isMulti || selfMulti;
        // A self-editable field wraps at the width Clay gave its TEXT command —
        // the same width the renderer wraps at, so the row table the caret
        // uses IS the rendered one. Re-read every frame (the field can be resized
        // by its owner) and fall back to "no wrap" until the geometry pass has
        // seen the field, which is what a code field asks for anyway.
        int selfWrap = int.MaxValue;
        if (selfMulti)
        {
            var so = selfEdit.Value;
            if (so.Active && so.Entity == e && so.TextW > 0) selfWrap = (int)so.TextW;
            if (a.Entity == e && !isMulti) a.WrapWidth = selfWrap;
        }

        string current;
        int wrapWidth = a.WrapWidth;
        object? wrapStyle = null;
        if (isMulti)
        {
            var (_, m) = multiQ.Get(e);
            current = m.Ref.Value ?? string.Empty; wrapWidth = m.Ref.WrapWidth;
            wrapStyle = m.Ref.Style;
        }
        else if (isMasked) { var (_, mt) = maskedQ.Get(e); current = mt.Ref.Value ?? string.Empty; }
        else { var (_, t) = textQ.Get(e); current = t.Ref.Value ?? string.Empty; }

        bool newField = a.Entity != e;
        // `current` is already the real value for every path (masked reads
        // MaskedText.Value, not the rendered mask), so a plain buffer compare
        // detects external writes (a chat line cleared on submit).
        bool externallyChanged = !newField && !string.Equals(current, a.Buffer.ToString(), StringComparison.Ordinal);
        // The owner can flip EditableText.Multiline on a field it already has; stb's
        // single-line mode is baked at Initialize, so treat it as a rebind.
        bool modeChanged = !newField && a.Multiline != wantMulti;

        if (newField || externallyChanged || modeChanged)
        {
            a.Entity = e;
            a.Masked = isMasked;
            a.Multiline = wantMulti;
            a.CustomMultiline = isMulti;
            a.WrapWidth = isMulti ? wrapWidth : selfWrap;
            a.WrapStyle = wrapStyle;
            ushort fid = a.DefaultFontId, fsize = 12;
            if (fontQ.TryGet(e, out var fontRow)) { var (_, f) = fontRow; fid = f.Ref.FontId; fsize = f.Ref.Size; }
            a.FontId = fid;
            a.FontSize = fsize;
            a.SetBuffer(current);
            a.State.Initialize(singleLine: !wantMulti);
            a.State.RowCountPerPage = ActiveTextEdit.RowsPerPage;
            a.State.Cursor = a.Buffer.Length;
            if (newField)
            {
                // A click that moves focus here must not pair with a click on the
                // previous field as a double — restart the click streak.
                a.LastClickTime = float.NegativeInfinity;
                a.ClickStreak = 0;
                // No press was routed to this field, yet one just happened: the caller
                // swapped the node under the cursor and focused the replacement by
                // resource. RouteMouse seats the caret from that press once the box
                // is known, instead of leaving it parked at the buffer end.
                a.SeedClickFrames = a.PendingClickEntity != e && !a.LastPressConsumed
                    && time.Value.Total - a.LastPressTime <= ActiveTextEdit.SeedClickWindowMs
                    ? ActiveTextEdit.SeedClickMaxFrames : 0;
            }
        }
        // The field's font can change while it stays focused (a chat line that
        // follows a font setting live); keep caret/selection measurement on it.
        else if (fontQ.TryGet(e, out var liveFontRow))
        {
            var (_, f) = liveFontRow;
            a.FontId = f.Ref.FontId;
            a.FontSize = f.Ref.Size;
        }
    }

    public static void RouteKeys(
        Commands commands,
        Res<KeyboardInput> kb,
        Res<Time> time,
        ResMut<ActiveTextEdit> edit,
        Local<KeyRepeat> repeat,
        Res<TextCompletion> completion)
    {
        var a = edit.Value;
        var rep = repeat.Value;
        if (a.Entity == 0) { rep.Clear(); return; }
        // A field that takes focus mid-hold must not inherit the previous field's
        // armed repeat: the key that moved focus (a Tab out of a form, an Enter
        // that opened a window) is still physically down, and its next repeat tick
        // would fire into the field that just arrived.
        if (rep.Entity != a.Entity) { rep.Clear(); rep.Entity = a.Entity; }

        // The press edge, plus OS-cadence auto-repeat while the key stays held.
        // Typed characters repeat for free (the platform re-delivers text input per OS repeat,
        // and backends re-synthesize '\b' for a held Backspace); every key handled HERE
        // is read as a state edge instead, so without this it fires exactly once
        // per press and a held Enter / arrow / Delete does nothing.
        bool Edge(KeyCode k) => rep.Edge(kb.Value, time.Value.Total, k);

        bool shift = Modifier(kb.Value, KeyCode.LeftShift, KeyCode.RightShift);
        bool ctrl = !AltGr(kb.Value) && Modifier(kb.Value, KeyCode.LeftControl, KeyCode.RightControl);

        if (ctrl)
        {
            // Latch the chord's letter for RouteChars (see the leak note there).
            foreach (var chord in s_chordKeys)
                if (kb.Value.IsPressedOnceRaw(chord) || kb.Value.IsPressedRaw(chord)) { a.ChordKey = chord; break; }

            if (kb.Value.IsPressedOnceRaw(KeyCode.A)) { a.RowGoalX = null; TextEdit.Key(a, a.State, TextEditKey.TextStart); TextEdit.Key(a, a.State, TextEditKey.TextEnd, shift: true); }
            if (kb.Value.IsPressedOnceRaw(KeyCode.C)) DoCopy(a);
            if (kb.Value.IsPressedOnceRaw(KeyCode.X)) DoCut(a);
            if (kb.Value.IsPressedOnceRaw(KeyCode.V)) DoPaste(a);
            if (Edge(KeyCode.Z)) TextEdit.Key(a, a.State, shift ? TextEditKey.Redo : TextEditKey.Undo);
            // Ctrl+Y is the OTHER redo every Windows editor ships; Ctrl+Shift+Z is
            // the one this editor already had. Both, so neither muscle memory loses.
            if (Edge(KeyCode.Y)) TextEdit.Key(a, a.State, TextEditKey.Redo);
            if (Edge(KeyCode.Left)) { a.RowGoalX = null; TextEdit.Key(a, a.State, TextEditKey.WordLeft, shift); }
            if (Edge(KeyCode.Right)) { a.RowGoalX = null; TextEdit.Key(a, a.State, TextEditKey.WordRight, shift); }
            // Ctrl+Home/End = document start/end (plain Home/End are per line).
            if (Edge(KeyCode.Home)) { a.RowGoalX = null; TextEdit.Key(a, a.State, TextEditKey.TextStart, shift); }
            if (Edge(KeyCode.End)) { a.RowGoalX = null; TextEdit.Key(a, a.State, TextEditKey.TextEnd, shift); }
            // Ctrl+Backspace / Ctrl+Delete: delete to the word boundary. No selection ->
            // extend one from the caret first; an existing selection is deleted as-is
            // (what every editor does). RouteChars drains the '\b' char while Ctrl is
            // down, so this cannot double-delete.
            if (Edge(KeyCode.Back))
            {
                a.RowGoalX = null;
                if (a.State.SelectStart == a.State.SelectEnd) SelectWordBackAt(a);
                TextEdit.Key(a, a.State, TextEditKey.Backspace);
            }
            if (Edge(KeyCode.Delete))
            {
                a.RowGoalX = null;
                if (a.State.SelectStart == a.State.SelectEnd) TextEdit.Key(a, a.State, TextEditKey.WordRight, shift: true);
                TextEdit.Key(a, a.State, TextEditKey.Delete);
            }
            return;
        }

        if (Edge(KeyCode.Left)) { a.RowGoalX = null; TextEdit.Key(a, a.State, TextEditKey.Left, shift); }
        if (Edge(KeyCode.Right)) { a.RowGoalX = null; TextEdit.Key(a, a.State, TextEditKey.Right, shift); }
        if (Edge(KeyCode.Home)) { a.RowGoalX = null; TextEdit.Key(a, a.State, TextEditKey.LineStart, shift); }
        if (Edge(KeyCode.End)) { a.RowGoalX = null; TextEdit.Key(a, a.State, TextEditKey.LineEnd, shift); }
        if (Edge(KeyCode.Delete)) TextEdit.Key(a, a.State, TextEditKey.Delete);
        // Multiline fields: arrows move between rows, Enter inserts a newline.
        // Enter newline comes from the key (not the char path): backends also
        // synthesize Enter as a TextInput '\r' (char 13), but RouteChars drops
        // '\r' so it doesn't double up here.
        if (a.Multiline)
        {
            // While a completion offer is live, Up/Down walk the SUGGESTIONS (wrapping)
            // instead of the rows, and Tab finishes whichever one is highlighted. The
            // list is short (its owner caps it), so Page* still move the caret.
            var items = CompletionLive(a, completion.Value);
            SyncCompletionSelection(a, items, completion.Value.Prefix);
            if (Edge(KeyCode.Up))
            {
                if (items != null) StepCompletion(a, items, -1);
                else MoveRows(a, -1, shift);
            }
            if (Edge(KeyCode.Down))
            {
                if (items != null) StepCompletion(a, items, 1);
                else MoveRows(a, 1, shift);
            }
            if (Edge(KeyCode.PageUp)) MoveRows(a, -ActiveTextEdit.RowsPerPage, shift);
            if (Edge(KeyCode.PageDown)) MoveRows(a, ActiveTextEdit.RowsPerPage, shift);
            // Shift+Enter is the same newline: a multiline field has no "submit",
            // so there is no second meaning to give it.
            if (Edge(KeyCode.Enter)) TextEdit.InputChar(a, a.State, '\n');
            // Tab indents instead of moving focus — UiFocusPlugin consumed Tab for
            // the multiline field, so Tab navigation yields. Backends that deliver
            // Tab as char 9 (and Delete/Home/End as 127/2/3) are dropped by
            // ClassifyChar, so this can't double.
            if (Edge(KeyCode.Tab))
            {
                if (shift) Outdent(a);
                else if (!TryComplete(a, completion.Value, a.CompletionSelected)) Indent(a);
            }
        }
        else if (kb.Value.IsPressedOnceRaw(KeyCode.Enter))
            commands.Entity(a.Entity).EmitTrigger(new TextEditSubmit { Value = a.Buffer.ToString() }, propagate: true);
        // Backspace is handled in RouteChars (backends deliver it as the '\b'
        // text-input char); handling KeyCode.Back here too would double-delete.
    }

    // Finish the word in front of the caret with the item at <paramref name="index"/>,
    // plus a space so the next token starts clean. Returns false — the caller falls
    // through (Tab indents) — for every offer that no longer describes what is on
    // screen. <paramref name="index"/> is the highlighted suggestion: Tab passes
    // a.CompletionSelected (what Up/Down moved to), a mouse pick passes the clicked row.
    //
    // The prefix is re-checked against the BUFFER rather than trusted: the owner
    // publishes the offer off a caret snapshot it read one or more frames ago, and
    // between then and this keypress the player may have typed, deleted, clicked
    // elsewhere or closed the field. Without the check a stale offer would eat
    // whatever happens to sit before the caret now.
    //
    // ponytail: this APPENDS the item's remainder instead of replacing the typed
    // prefix, so the whole completion is one TextEdit.Paste — i.e. ONE undo record,
    // and a single Ctrl+Z puts the half-typed word back. Replacing would need a
    // delete + an insert (stb's combined MakeUndoReplace is private), which costs
    // two Ctrl+Z for one Tab. The visible price: a prefix typed in another case
    // keeps that case (`HEA` + `dmsg` -> `HEAdmsg`), which the scripting parser
    // folds anyway.
    public static bool TryComplete(ActiveTextEdit a, TextCompletion c, int index)
    {
        if (c == null || c.Entity != a.Entity || c.Items == null || c.Items.Length == 0)
            return false;
        // A selection means Tab is a re-indent gesture, not a completion — and
        // Paste would eat the selection before inserting.
        if (a.State.SelectStart != a.State.SelectEnd)
            return false;
        index = Math.Clamp(index, 0, c.Items.Length - 1);
        var prefix = c.Prefix;
        var item = c.Items[index];
        if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(item) || item.Length <= prefix.Length)
            return false;
        if (!item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var cursor = a.State.Cursor;
        var at = cursor - prefix.Length;
        if (at < 0 || cursor > a.Buffer.Length)
            return false;
        for (var i = 0; i < prefix.Length; i++)
            if (char.ToLowerInvariant(a.Buffer[at + i]) != char.ToLowerInvariant(prefix[i]))
                return false;

        a.RowGoalX = null;
        TextEdit.Paste(a, a.State, (item.Substring(prefix.Length) + " ").AsSpan());
        return true;
    }

    // The live offer's items, or null when there is nothing to complete with: the offer
    // must name the focused field and carry at least one item. This is the single "is a
    // popup open for THIS field" test — Up/Down gate on it, and it is what a null means
    // "fall through to a normal row move / indent".
    private static string[]? CompletionLive(ActiveTextEdit a, TextCompletion c)
        => (c == null || c.Entity != a.Entity || c.Items == null || c.Items.Length == 0) ? null : c.Items;

    // Keep the highlighted index valid for the CURRENT offer. A new prefix starts a new
    // word, so the highlight goes back to the top (the "Tab-takes-the-first" default)
    // rather than pointing at an item that no longer matches. A shrinking list clamps
    // instead of wrapping, so an index that was valid a moment ago never overspans.
    private static void SyncCompletionSelection(ActiveTextEdit a, string[]? items, string prefix)
    {
        if (items != null)
        {
            if (prefix != a.CompletionPrefix)
            {
                a.CompletionPrefix = prefix;
                a.CompletionSelected = 0;
            }
            if (a.CompletionSelected >= items.Length) a.CompletionSelected = items.Length - 1;
            else if (a.CompletionSelected < 0) a.CompletionSelected = 0;
        }
        else if (a.CompletionPrefix.Length > 0)
        {
            a.CompletionPrefix = string.Empty;
            a.CompletionSelected = 0;
        }
    }

    // Move the highlight by one row, wrapping — the arrow-key navigation over the list.
    private static void StepCompletion(ActiveTextEdit a, string[] items, int delta)
    {
        var n = items.Length;
        a.CompletionSelected = (a.CompletionSelected + delta + n) % n;
    }

    /// <summary>A one-shot "insert item <paramref name="index"/>" from a click on a
    /// suggestion row, applied once per distinct <paramref name="seq"/>. The row
    /// carries UiNoBlur, so the field is still focused and the buffer live — the
    /// same re-validation as Tab keeps a stale pick from eating the caret.</summary>
    public static void ApplyCompletionPick(ActiveTextEdit a, TextCompletion offer, ulong entity, int index, int seq)
    {
        if (a.Entity == 0 || entity != a.Entity || seq == a.CompletionPickSeq)
            return;
        a.CompletionPickSeq = seq;
        _ = TryComplete(a, offer, index);
    }

    // The three clipboard operations as ONE code path, shared by the Ctrl+C/X/V key
    // handling and ApplyClipboardAction, so a context menu and the keyboard can
    // never disagree about what copy / cut / paste do.
    public static void DoCopy(ActiveTextEdit a)
    {
        var s = Selected(a);
        RecordCopy(a, s);
        if (s.Length > 0) a.Clipboard = s;
    }

    public static void DoCut(ActiveTextEdit a)
    {
        var s = Selected(a);
        RecordCopy(a, s);
        if (s.Length > 0)
        {
            a.Clipboard = s;
            TextEdit.Cut(a, a.State);
        }
    }

    public static void DoPaste(ActiveTextEdit a)
    {
        var paste = NormalizePaste(a.Clipboard, a.Multiline);
        if (paste.Length > 0) TextEdit.Paste(a, a.State, paste.AsSpan());
    }

    public enum ClipboardAction { Copy = 0, Cut = 1, Paste = 2 }

    /// <summary>A one-shot copy / cut / paste request for the field
    /// <paramref name="entity"/> (a context menu), applied once per distinct
    /// <paramref name="seq"/>; rejected once focus has moved on — a menu left open
    /// across a blur must not paste into whatever took focus.</summary>
    public static void ApplyClipboardAction(ActiveTextEdit a, ulong entity, ClipboardAction action, int seq)
    {
        if (a.Entity == 0 || entity != a.Entity || seq == a.ClipboardActionSeq)
            return;
        a.ClipboardActionSeq = seq;
        switch (action)
        {
            case ClipboardAction.Copy: DoCopy(a); break;
            case ClipboardAction.Cut: DoCut(a); break;
            case ClipboardAction.Paste: DoPaste(a); break;
        }
    }

    // Tab: indent to the NEXT tab stop, as one undo step.
    //
    // Two things a fixed four-space insert got wrong, both of which show up the
    // moment a code editor is actually used:
    //  - It ignored the column. Tab from column 2 landed on column 6, not 4, so
    //    nothing a player tabbed ever lined up with anything else — the whole
    //    point of pressing Tab.
    //  - Four separate TextEdit.InputChar calls are four separate undo records, so
    //    Ctrl+Z ate the indent one space at a time (four undos per Tab).
    // TextEdit.Paste fixes both: one MakeUndoInsert for the whole run, and it
    // deletes the selection first (Tab over a selection replaces it, as here).
    private static void Indent(ActiveTextEdit a)
    {
        // The caret ends up at the selection's START once Paste deletes it, so the
        // stop is measured from there, not from wherever the drag happened to end.
        int at = a.State.SelectStart == a.State.SelectEnd
            ? a.State.Cursor
            : Math.Min(a.State.SelectStart, a.State.SelectEnd);
        int column = at - LineStart(a, at);
        int count = TabSpaces - column % TabSpaces;
        TextEdit.Paste(a, a.State, s_tabRun.AsSpan(0, count));
    }

    // Shift+Tab: outdent the caret's line by up to one tab stop of leading spaces.
    // Deleted through the selection so stb records it as a single undo step, then
    // the caret is put back where it was minus what vanished in front of it.
    private static void Outdent(ActiveTextEdit a)
    {
        int lineStart = LineStart(a, a.State.Cursor);
        int count = 0;
        while (count < TabSpaces && lineStart + count < a.Buffer.Length
               && a.Buffer[lineStart + count] == ' ')
            ++count;
        if (count == 0) return;

        int cursor = a.State.Cursor;
        a.State.SelectStart = lineStart;
        a.State.SelectEnd = lineStart + count;
        a.State.Cursor = lineStart + count;
        TextEdit.Key(a, a.State, TextEditKey.Delete);
        int moved = Math.Max(lineStart, cursor - count);
        a.State.Cursor = a.State.SelectStart = a.State.SelectEnd = moved;
    }

    // Index just past the '\n' that opens the BUFFER line holding `index` (0 on the
    // first line). Buffer lines, not wrapped rows: an indent belongs to the line the
    // text was typed on, and a wrapped continuation row has no indent of its own.
    private static int LineStart(ActiveTextEdit a, int index)
    {
        int i = Math.Clamp(index, 0, a.Buffer.Length);
        while (i > 0 && a.Buffer[i - 1] != '\n') --i;
        return i;
    }

    private static readonly char[] s_tabRun = new string(' ', TabSpaces).ToCharArray();

    // Vertical caret movement by whole rows, column preserved.
    //
    // Not TextEdit.Key(Up/Down): stb's vertical walk has a hole at the very END
    // of the buffer — its find-char-pos shortcut reports the row as the LAST
    // row's start and the goal x as 0 (upstream stb_textedit.h does the same),
    // so the first Up right after typing moves to column 0 of the line you are
    // ON instead of the line above. In a code editor that is the most common
    // Up there is. The row table makes the correct move small, and it is the
    // same table LayoutRow feeds stb for clicks, so the two cannot disagree.
    //
    // The wrapped MultilineText fields keep stb's walk: they are read-mostly and
    // their row table is the host wrap's.
    public static void MoveRows(ActiveTextEdit a, int delta, bool shift)
    {
        if (a.CustomMultiline)
        {
            var key = delta switch
            {
                -1 => TextEditKey.Up,
                1 => TextEditKey.Down,
                < -1 => TextEditKey.PageUp,
                _ => TextEditKey.PageDown,
            };
            TextEdit.Key(a, a.State, key, shift);
            return;
        }

        var rows = a.Rows;
        if (rows.Count == 0) return;
        var (row, _) = a.RowAt(a.State.Cursor);
        // Latch the goal x on the first of a run of vertical moves so walking
        // through short lines and back out keeps the original column.
        a.RowGoalX ??= a.CaretXY(a.State.Cursor).X;
        var target = Math.Clamp(row + delta, 0, rows.Count - 1);
        var cursor = a.IndexForX(target, a.RowGoalX.Value);
        if (shift)
        {
            if (a.State.SelectStart == a.State.SelectEnd) a.State.SelectStart = a.State.Cursor;
            a.State.SelectEnd = cursor;
        }
        else
        {
            a.State.SelectStart = a.State.SelectEnd = cursor;
        }
        a.State.Cursor = cursor;
    }

    // Is a modifier down THIS frame? Must accept the press edge as well as the
    // held state: IsPressed is "in the last two snapshots", so a chord whose
    // modifier and main key first appear in the SAME polled frame (a quick
    // Ctrl+C, or any tap while the frame time is long) would read ctrl=false and
    // the shortcut would silently vanish.
    private static bool Modifier(KeyboardInput kb, KeyCode left, KeyCode right)
        => Down(kb, left) || Down(kb, right);

    private static bool Down(KeyboardInput kb, KeyCode key)
        => kb.IsPressedRaw(key) || kb.IsPressedOnceRaw(key);

    // AltGr composes real characters ('@', '€', 'à' on an italian/german layout)
    // and reports as Ctrl + RIGHT Alt, so while it is down Ctrl is NOT a shortcut
    // modifier — otherwise AltGr+a fires select-all instead of typing the char.
    //
    // RIGHT Alt only. Treating either Alt as AltGr makes a LEFT Alt that reads
    // down silently kill every Ctrl chord (Ctrl+A/C/X/V/Z stop working with no
    // feedback at all), and a left Alt can read down without the user holding it:
    // a backend that never clears its held-key cache on focus loss keeps an Alt
    // whose key-up went to the window switcher latched. KeyboardInput masks keys
    // stranded that way, and this is the second guard: left Alt is never AltGr.
    private static bool AltGr(KeyboardInput kb) => Down(kb, KeyCode.RightAlt);

    // Keys RouteKeys claims as Ctrl chords. Latched in ActiveTextEdit.ChordKey so
    // RouteChars can drop the chord's composed text for as long as the letter is
    // physically down.
    private static readonly KeyCode[] s_chordKeys =
        { KeyCode.A, KeyCode.C, KeyCode.X, KeyCode.V, KeyCode.Y, KeyCode.Z, KeyCode.Left, KeyCode.Right };

    // Is the key down this frame, or was it down last frame? One frame of grace
    // (IsReleased) so the auto-repeat char that arrives in the same poll as the
    // key's release is still covered.
    private static bool DownOrJustReleased(KeyboardInput kb, KeyCode key)
        => kb.IsPressedRaw(key) || kb.IsPressedOnceRaw(key) || kb.IsReleasedRaw(key);

    public static void RouteChars(
        EventReader<CharInput> reader,
        Res<KeyboardInput> kb,
        ResMut<ActiveTextEdit> edit)
    {
        // A handler claimed this frame's key press (a hotkey bound to a letter,
        // KeyboardInput.ConsumeText) — swallow the composed text so the bound key
        // doesn't also type into the focused field.
        if (kb.Value.TextConsumed)
            return;

        var a = edit.Value;
        // Before the focus check: the latch must expire even while no field holds
        // focus, or a chord that ran as focus was leaving stays armed forever and
        // eats the first char the next field receives.
        if (a.ChordKey != KeyCode.None && !DownOrJustReleased(kb.Value, a.ChordKey))
            a.ChordKey = KeyCode.None;
        if (a.Entity == 0) return;

        // A Ctrl chord must never TYPE into the field, only act on it. Two
        // distinct leaks, both seen as junk replacing a Ctrl+A selection:
        //  1. text delivered while Ctrl is HELD (most platforms drop the control char for
        //     Ctrl+letter, but not every platform/layout does, and Ctrl+Space is
        //     a plain 0x20). AltGr (Ctrl + right Alt) is excluded — it composes
        //     real characters.
        //  2. the chord's letter held past the Ctrl RELEASE: a backend that lifts
        //     its text-input suppression the instant Ctrl goes up delivers the
        //     key's auto-repeat as a literal char — the 'v' of a Ctrl+V typed
        //     into the field it was supposed to paste into.
        bool ctrl = !AltGr(kb.Value) && Modifier(kb.Value, KeyCode.LeftControl, KeyCode.RightControl);
        if (ctrl || a.ChordKey != KeyCode.None)
            return;

        foreach (var ev in reader.Read())
        {
            var ch = ev.Value;
            switch (ClassifyChar(ch, a.Multiline))
            {
                case TextCharAction.Backspace: TextEdit.Key(a, a.State, TextEditKey.Backspace); break;
                case TextCharAction.Newline: TextEdit.InputChar(a, a.State, '\n'); break;
                case TextCharAction.Insert: TextEdit.InputChar(a, a.State, ch); break;
                // Drop: '\r', a newline in a single-line field, and other control chars.
            }
        }
    }

    // Clipboard text → what may be inserted. TextEdit.Paste inserts the span
    // verbatim, and the OS clipboard hands back CRLF on Windows — including for
    // text this very editor copied — so a raw '\r' would land in the buffer as a
    // glyph and ride into the field's Text. Single-line fields then flatten: a
    // newline is never legal there — the clipboard can hold anything, a login box
    // cannot.
    public static string NormalizePaste(string text, bool multiline)
    {
        var s = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        return multiline ? s : s.Replace('\n', ' ');
    }

    public enum TextCharAction { Drop, Insert, Backspace, Newline }

    // Classify a TextInput char into an edit action. The control-char rules are
    // parity-sensitive (and a past defect source):
    //   '\b' -> backspace; '\r' -> dropped (Enter already inserts the newline via
    //   KeyCode.Enter, and pasted "\r\n" must not double up); '\n' -> newline only in
    //   multiline fields; other control chars (< space, 127 = Home/End/Tab/Delete
    //   synthesized as text input by some backends) are dropped; everything else inserts.
    public static TextCharAction ClassifyChar(char ch, bool multiline)
    {
        if (ch == '\b') return TextCharAction.Backspace;
        if (ch == '\r') return TextCharAction.Drop;
        if (ch == '\n') return multiline ? TextCharAction.Newline : TextCharAction.Drop;
        if (ch < ' ' || ch == (char)127) return TextCharAction.Drop;
        return TextCharAction.Insert;
    }

    // Push the edited buffer back into the field's Text (plain), MaskedText
    // (SyncMaskedText then mirrors the mask chars into Text for the renderer) or
    // MultilineText.
    public static void WriteBack(
        Commands commands,
        ResMut<ActiveTextEdit> edit,
        Query<Data<Text>, Filter<With<EditableText>, Without<MaskedText>, Without<MultilineText>>> textQ,
        Query<Data<MaskedText>, Filter<With<EditableText>>> maskedQ,
        Query<Data<MultilineText>, With<EditableText>> multiQ,
        Query<Data<ReadOnlyText>> readOnlyQ)
    {
        var a = edit.Value;
        if (a.Entity == 0) return;

        var value = a.Buffer.ToString();
        // Read-only: undo whatever the keys did to the buffer, keep caret + selection.
        if (readOnlyQ.Contains(a.Entity) && textQ.TryGet(a.Entity, out var roRow))
        {
            var (_, rt) = roRow;
            var text = rt.Ref.Value ?? string.Empty;
            if (!string.Equals(text, value, StringComparison.Ordinal))
            {
                a.SetBuffer(text);
                a.State.Cursor = Math.Min(a.State.Cursor, text.Length);
                a.State.SelectStart = Math.Min(a.State.SelectStart, text.Length);
                a.State.SelectEnd = Math.Min(a.State.SelectEnd, text.Length);
            }
            return;
        }
        if (a.CustomMultiline)
        {
            if (multiQ.TryGet(a.Entity, out var multiRow))
            {
                var (_, m) = multiRow;
                if (!string.Equals(m.Ref.Value, value, StringComparison.Ordinal))
                {
                    m.Ref.Value = value;
                    Changed(commands, a.Entity, value);
                }
            }
        }
        else if (a.Masked)
        {
            if (maskedQ.TryGet(a.Entity, out var maskedRow))
            {
                var (_, mt) = maskedRow;
                if (!string.Equals(mt.Ref.Value, value, StringComparison.Ordinal))
                {
                    mt.Ref.Value = value;
                    Changed(commands, a.Entity, value);
                }
            }
        }
        else if (textQ.TryGet(a.Entity, out var textRow))
        {
            var (_, t) = textRow;
            if (!string.Equals(t.Ref.Value, value, StringComparison.Ordinal))
            {
                t.Ref.Value = value;
                textQ.SetChanged<Text>(a.Entity);
                Changed(commands, a.Entity, value);
            }
        }
    }

    private static void Changed(Commands commands, ulong entity, string value)
        => commands.Entity(entity).EmitTrigger(new TextEditChanged { Value = value }, propagate: true);

    // A single-line field whose text outgrows its clipping frame: slide the text
    // row left just enough to keep the caret inside (the overlays ride the row);
    // every other field sits at its start. Only for frames that clip: sliding
    // under an unclipped frame would draw the head of the text left of the box.
    private const float FollowMargin = 2f;

    public static void FollowCaret(
        Res<ActiveTextEdit> edit,
        Query<Data<TextFieldGeom>> geoms,
        Query<Data<Node>> nodes,
        Query<Data<ComputedNode>> computed)
    {
        var a = edit.Value;
        foreach (var (ent, geom) in geoms)
        {
            ref var g = ref geom.Ref;
            if (g.Row == 0 || !nodes.TryGet(g.Row, out var rowRow))
                continue;
            var (_, rowNode) = rowRow;

            float want = 0f;
            if (ent.Ref == a.Entity && !a.CustomMultiline && rowNode.Ref.Overflow == Overflow.Visible
                && nodes.TryGet(g.Frame, out var frameRow) && computed.TryGet(g.Frame, out var frameCnRow))
            {
                var (_, frameNode) = frameRow;
                var (_, frameCn) = frameCnRow;
                // Symmetric inset: the text keeps the same gap on the right as on the left.
                float visible = frameCn.Ref.Size.X - 2f * g.RowLeft;
                if (frameNode.Ref.Overflow != Overflow.Visible && visible > 0f)
                {
                    float caretX = a.CaretXY(a.State.Cursor).X;
                    float textW = a.CaretXY(a.Buffer.Length).X;
                    want = g.Scroll;
                    if (caretX - want > visible - FollowMargin)
                        want = caretX - visible + FollowMargin;
                    if (caretX - want < 0f)
                        want = caretX;
                    want = Math.Clamp(want, 0f, MathF.Max(0f, textW + FollowMargin - visible));
                }
            }
            if (want == g.Scroll)
                continue;
            g.Scroll = want;
            rowNode.Ref.Left = Val.Px(g.RowLeft - want);
            nodes.SetChanged<Node>(g.Row);
        }
    }

    public static void PositionOverlays(
        Res<ActiveTextEdit> edit,
        Res<Time> time,
        Query<Data<Node, TextEditCaret>> carets,
        Query<Data<Node, TextEditSelection>> selections,
        Query<Data<MultilineText>> multiGlyphs)
    {
        var a = edit.Value;
        bool on = CaretOn(time.Value.Total, a.CaretBlinkHold);
        int s0 = Math.Min(a.State.SelectStart, a.State.SelectEnd);
        int s1 = Math.Max(a.State.SelectStart, a.State.SelectEnd);

        foreach (var (ent, node, caret) in carets)
        {
            // Multiline: the host draws the caret in-content (so it scrolls/clips
            // with the text); the overlay node serves single-line.
            if (caret.Ref.Target != a.Entity || a.Entity == 0 || a.CustomMultiline)
            {
                if (node.Ref.Display != Display.None) { node.Ref.Display = Display.None; carets.SetChanged<Node>(ent.Ref); }
                continue;
            }
            var (cx, cy) = a.CaretXY(a.State.Cursor);
            float caretX = cx + caret.Ref.OffsetX;
            if (caret.Ref.MaxX > 0 && caretX > caret.Ref.MaxX)
            {
                if (node.Ref.Display != Display.None) { node.Ref.Display = Display.None; carets.SetChanged<Node>(ent.Ref); }
                continue;
            }
            // Size the bar to the glyph ink (cap-top to descender), not the full
            // font cell — the cell's top leading made the caret overshoot above the
            // text. Sit it on the ink at the line's natural top (cy + ink top),
            // matching the glyphs (drawn at their natural origin) and the multiline
            // path below. cy = 0 for single-line.
            var (caretInkTop, caretH, _) = a.CaretMetrics();
            float caretY = cy + caretInkTop + caret.Ref.OffsetY;
            if (caret.Ref.MaxY > 0)
            {
                if (caretY >= caret.Ref.MaxY)
                {
                    if (node.Ref.Display != Display.None) { node.Ref.Display = Display.None; carets.SetChanged<Node>(ent.Ref); }
                    continue;
                }
                caretH = MathF.Min(caretH, caret.Ref.MaxY - caretY);
            }
            var caretDisplay = on ? Display.Flex : Display.None;
            if (node.Ref.Display != caretDisplay
                || node.Ref.Left.Type != ValType.Px || node.Ref.Left.Value != caretX
                || node.Ref.Top.Type != ValType.Px || node.Ref.Top.Value != caretY
                || node.Ref.Height.Type != ValType.Px || node.Ref.Height.Value != caretH)
            {
                node.Ref.Display = caretDisplay;
                node.Ref.Left = Val.Px(caretX);
                node.Ref.Top = Val.Px(caretY);
                node.Ref.Height = Val.Px(caretH);
                carets.SetChanged<Node>(ent.Ref);
            }
        }

        foreach (var (ent, node, sel) in selections)
        {
            // Multiline: the host draws the selection in-content (per-row rects,
            // range stamped below) — the single overlay node only serves
            // single-line / masked fields. Hide it for multiline.
            if (sel.Ref.Target != a.Entity || a.Entity == 0 || s0 == s1 || a.CustomMultiline)
            {
                if (node.Ref.Display != Display.None) { node.Ref.Display = Display.None; selections.SetChanged<Node>(ent.Ref); }
                continue;
            }
            var (x0, y0) = a.CaretXY(s0);
            var (x1, _) = a.CaretXY(s1);
            float selLeft = x0 + sel.Ref.OffsetX;
            float selRight = x1 + sel.Ref.OffsetX;
            if (sel.Ref.MaxX > 0)
            {
                selRight = MathF.Min(selRight, sel.Ref.MaxX);
                if (selLeft >= selRight)
                {
                    if (node.Ref.Display != Display.None) { node.Ref.Display = Display.None; selections.SetChanged<Node>(ent.Ref); }
                    continue;
                }
            }
            // Sit the highlight on the same ink band as the glyphs/caret, at the
            // line's natural top (single-line: y0 = 0, box top = row top).
            var (selInkTop, selInkH, _) = a.CaretMetrics();
            float selTop = y0 + selInkTop + sel.Ref.OffsetY;
            float selH = selInkH;
            if (sel.Ref.MaxY > 0)
            {
                if (selTop >= sel.Ref.MaxY)
                {
                    if (node.Ref.Display != Display.None) { node.Ref.Display = Display.None; selections.SetChanged<Node>(ent.Ref); }
                    continue;
                }
                selH = MathF.Min(selInkH, sel.Ref.MaxY - selTop);
            }
            float selW = MathF.Max(1, selRight - selLeft);
            if (node.Ref.Display != Display.Flex
                || node.Ref.Height.Type != ValType.Px || node.Ref.Height.Value != selH
                || node.Ref.Left.Type != ValType.Px || node.Ref.Left.Value != selLeft
                || node.Ref.Top.Type != ValType.Px || node.Ref.Top.Value != selTop
                || node.Ref.Width.Type != ValType.Px || node.Ref.Width.Value != selW)
            {
                node.Ref.Display = Display.Flex;
                node.Ref.Height = Val.Px(selH);
                node.Ref.Left = Val.Px(selLeft);
                node.Ref.Top = Val.Px(selTop);
                node.Ref.Width = Val.Px(selW);
                selections.SetChanged<Node>(ent.Ref);
            }
        }

        // Multiline: stamp the selection range + caret onto the focused field's
        // MultilineText so the host paints them in-content (scroll/clip safe);
        // clear on every other multiline field. Plain fields, no layout mark.
        foreach (var (ent, multi) in multiGlyphs)
        {
            ref var m = ref multi.Ref;
            bool focused = ent.Ref == a.Entity && a.CustomMultiline;
            bool selActive = focused && s0 != s1;
            m.SelStart = selActive ? s0 : -1;
            m.SelEnd = selActive ? s1 : -1;
            if (focused)
            {
                var (cx, cy) = a.CaretXY(a.State.Cursor);
                // Size the caret to the font's glyph ink (cap-top to descender),
                // not the full line cell — the line cell has top leading that made
                // the bar overshoot above the text.
                var (caretTop, caretH, _) = a.CaretMetrics();
                m.CaretOn = on;
                m.CaretX = cx;
                m.CaretY = cy + caretTop;
                m.CaretH = caretH;
            }
            else
            {
                m.CaretOn = false;
            }
        }
    }

    // Size each multiline field's wrapped glyph box to its content so it grows as
    // lines are added (caret Top maps within it) and whatever sits below it stays
    // correct. Runs for all multiline fields, not just the focused one.
    private static readonly List<TextRow> s_sizeRows = new();

    public static void SizeMultilineGlyphs(Query<Data<Node, TextFont, MultilineText>> q, Res<ActiveTextEdit> edit)
    {
        var measurer = edit.Value.Measurer;
        foreach (var (ent, node, font, multi) in q)
        {
            s_sizeRows.Clear();
            measurer.WrapRows(multi.Ref.Value ?? string.Empty, font.Ref.FontId, font.Ref.Size, multi.Ref.WrapWidth, multi.Ref.Style, s_sizeRows);
            float total = 0;
            foreach (var ln in s_sizeRows)
                total += ln.Height > 0 ? ln.Height : font.Ref.Size;
            float wantH = Math.Max(total, font.Ref.Size);
            float wantW = multi.Ref.WrapWidth;
            if (node.Ref.Height.Type != ValType.Px || node.Ref.Height.Value != wantH
                || node.Ref.Width.Type != ValType.Px || node.Ref.Width.Value != wantW)
            {
                node.Ref.Height = Val.Px(wantH);
                node.Ref.Width = Val.Px(wantW);
                q.SetChanged<Node>(ent.Ref);
            }
        }
    }

    // A self-editable MULTILINE field (the code-field shape) clips its own box.
    //
    // Clay sizes a TEXT command to the text's own measured extent and the
    // renderer paints the whole run at that box, so a code field
    // whose buffer is taller than its node — or whose lines are longer than it, which
    // TextWrap.None makes the normal case, and TextWrap.None is mandatory for the
    // caret maths — paints its overflow straight over whatever is drawn after it: the
    // rows past the pane land on the window underneath and a long line runs out the side
    // of the window. Nothing else stops it. Clay only emits a scissor for a node that
    // asks for one, and a field's ancestors are ordinary flow boxes; the editors
    // that look right are the ones that happen to sit in an Overflow.Scroll body.
    //
    // Only when the owner left Overflow at its default: a field that asked for Scroll
    // keeps it (that is a container, and Clay clips those already).
    public static void ClipEditorContent(
        Query<Data<Node, EditableText>,
            Filter<With<Text>, Without<MultilineText>, Without<TextFieldGeom>>> q)
    {
        foreach (var (ent, node, flag) in q)
        {
            if (!flag.Ref.Multiline || node.Ref.Overflow != Overflow.Visible) continue;
            node.Ref.Overflow = Overflow.Clip;
            q.SetChanged<Node>(ent.Ref);
        }
    }

    // Extend the selection back from the caret to the word boundary
    // (Ctrl+Backspace, no prior selection). One group per press, VS Code style:
    // a run of spaces/tabs before the caret selects the whole run (the next press
    // takes the previous word); a NEWLINE never groups — the walk stops at the
    // nearest '\n' and each press deletes exactly one, so blank lines merge one
    // at a time instead of vanishing at once; a word char selects the word back
    // to its start but not the space before it. The library's MoveWordLeft walk
    // eats whitespace (all of it) AND the whole previous word in a single press,
    // which is why this lives here instead.
    // Orientation mirrors the library's WordLeft|Shift result (SelectStart = old
    // cursor, SelectEnd = the boundary), so the following Backspace sees exactly
    // what the key path used to see.
    public static void SelectWordBackAt(ActiveTextEdit a)
    {
        var buf = a.Buffer.ToString();
        int len = buf.Length;
        int c = Math.Clamp(a.State.Cursor, 0, len);
        int start = c;
        if (c > 0)
        {
            --start;
            if (char.IsWhiteSpace(buf[start]))
            {
                while (start > 0 && char.IsWhiteSpace(buf[start - 1]) && buf[start - 1] != '\n') --start;
            }
            else
            {
                while (start > 0 && !char.IsWhiteSpace(buf[start - 1])) --start;
            }
        }
        a.State.SelectStart = c;
        a.State.SelectEnd = start;
        a.State.Cursor = start;
    }

    // Expand the selection to the whitespace-delimited word containing the
    // caret (double-click select-word). Classic text-box intent: a
    // click that lands on whitespace (or an empty buffer) selects nothing. Reads
    // the real Buffer chars, so it works for masked/multiline fields too
    // (newline counts as whitespace, so a word never spans lines).
    public static void SelectWordAt(ActiveTextEdit a)
    {
        var buf = a.Buffer;
        int len = buf.Length;
        int c = Math.Clamp(a.State.Cursor, 0, len);

        // Anchor on the char under the caret; if the caret landed just past a
        // word (on the following whitespace / buffer end) but a word char
        // precedes it, anchor on that preceding char instead.
        int anchor = c;
        if (anchor >= len || char.IsWhiteSpace(buf[anchor]))
        {
            if (c > 0 && !char.IsWhiteSpace(buf[c - 1])) anchor = c - 1;
            else return;
        }

        int start = anchor;
        while (start > 0 && !char.IsWhiteSpace(buf[start - 1])) --start;
        int end = anchor + 1;
        while (end < len && !char.IsWhiteSpace(buf[end])) ++end;

        a.State.SelectStart = start;
        a.State.SelectEnd = end;
        a.State.Cursor = end;
    }

    // Rank this press single (1) / double (2) / triple (3) off the consecutive-
    // press streak: a press lands within the double-click window of the
    // previous one and continues the streak, otherwise it starts a fresh one.
    // A 4th press inside the window restarts at 1 so it behaves as a fresh
    // single (which clears the select-all via Click above) instead of chaining
    // into yet another triple.
    public static int NextClickRank(ActiveTextEdit a, float nowMs, float doubleClickMs = 300f)
    {
        bool isFollowUp = nowMs - a.LastClickTime <= doubleClickMs;
        a.LastClickTime = nowMs;
        a.ClickStreak = isFollowUp ? a.ClickStreak + 1 : 1;
        int rank = a.ClickStreak;
        if (rank > 3) { a.ClickStreak = 1; rank = 1; }
        return rank;
    }

    // Triple-click selection. Single-line: the whole field (that IS the whole
    // word/message). Multiline: the \n-delimited line containing the caret,
    // standard-editor behavior (Notepad / VS Code) — the trailing newline is NOT
    // included, so a following Delete removes the line's text but keeps the
    // empty line. Wrapped paragraphs count as one logical line (the \n in the
    // buffer, not the visual wrap).
    public static void SelectTripleAt(ActiveTextEdit a)
    {
        var buf = a.Buffer.ToString();
        int len = buf.Length;
        int c = Math.Clamp(a.State.Cursor, 0, len);
        if (!a.Multiline)
        {
            a.State.SelectStart = 0;
            a.State.SelectEnd = len;
            a.State.Cursor = len;
            return;
        }

        int start = c > 0 ? buf.LastIndexOf('\n', c - 1) + 1 : 0;
        int end = buf.IndexOf('\n', c);
        if (end < 0) end = len;

        a.State.SelectStart = start;
        a.State.SelectEnd = end;
        a.State.Cursor = end;
    }

    // Stamp what a Ctrl+C / Ctrl+X gesture actually took, empty selections
    // included — that is the case worth reporting. See ActiveTextEdit.CopyAttempts.
    public static void RecordCopy(ActiveTextEdit a, string copied)
    {
        a.CopyAttempts++;
        a.CopyChars = copied.Length;
        var lines = copied.Length == 0 ? 0 : 1;
        foreach (var ch in copied)
            if (ch == '\n') ++lines;
        a.CopyLines = lines;
    }

    public static string Selected(ActiveTextEdit a)
    {
        int s0 = Math.Min(a.State.SelectStart, a.State.SelectEnd);
        int s1 = Math.Max(a.State.SelectStart, a.State.SelectEnd);
        if (s0 == s1) return string.Empty;
        return a.Buffer.ToString(s0, s1 - s0);
    }
}
