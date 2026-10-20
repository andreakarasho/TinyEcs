// Keep the caret line of a focused MULTI-LINE self-editable field inside its
// scroll viewport.
//
// A code editor is typically a tall Text node inside an Overflow.Scroll container
// (a scroll body). Clay owns the glyph draw, so the field itself cannot scroll
// its text — the ancestor container does. Typing past the bottom of the viewport
// therefore moves the caret out of the visible band and the operator types
// blind; the same on Up/PageUp/Ctrl+Home.
//
// SelfEditOverlay already publishes the caret band in layout space (unclipped:
// where the caret WANTS to be), so this only has to find the nearest scrolling
// ancestor and nudge its ScrollPosition until the band is inside. Writing
// ScrollPosition in place is enough — LayoutSystem diffs it against
// LastSyncedScroll and pushes the change into Clay before the next layout, and
// Clay clamps the far end to the content size (writing the clamped value back
// into the component).
//
// It follows the caret only on the frames the caret MOVED. Adjusting every frame
// made every OTHER writer of that ScrollPosition a no-op: the wheel router
// (Stage.First) scrolled the container and this pass, one stage later, pulled it
// straight back to the caret row — a focused editor could not be scrolled with
// the wheel at all.

using Clay;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace TinyEcs.Bevy.UI;

public static class SelfEditCaretScroll
{
    // Extra px kept between the caret band and the viewport edge it was pushed
    // away from, so the caret line never sits half-clipped against the border.
    private const float Margin = 2f;

    // Last frame's caret identity for the follow gate.
    public struct FollowState
    {
        public ulong Entity;
        public int Cursor;
        public int Length;
        // The selection bounds count as caret movement: an arrow key pressed
        // while text is selected COLLAPSES the selection and leaves the cursor
        // index where it was (stb's rule), and that keystroke must still bring
        // the caret row back into a freely scrolled viewport.
        public int SelStart, SelEnd;
        // A caret move can need more than one frame to settle: the caret band is
        // derived from the LAST laid-out frame, so a nudge computed while the
        // container is still moving lands short. Stays armed until the band is
        // inside — or until the offset it asks for is already clamped out, which
        // is the caret-on-the-last-row case that used to fight the wheel forever.
        public bool Pending;
    }

    public static void Run(
        Res<SelfEditOverlay> overlay,
        Res<ActiveTextEdit> edit,
        Res<UiClayContext> ctx,
        Local<FollowState> follow,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<Node, ScrollPosition>> scrollers)
    {
        ref var st = ref follow.Value;
        var o = overlay.Value;
        // Cleared, not kept: the next bind of a field must scroll its caret into
        // view once (focus gain), which the entity compare below only sees if the
        // remembered identity died with the focus.
        if (!o.Active || !o.Multiline || o.CaretH <= 0) { st = default; return; }

        var a = edit.Value;
        // The press that focused this field is turned into a caret index later
        // this frame (UiTextEditPlugin.RouteMouse, Stage.Update) — right now the
        // cursor is still at the buffer end where the bind parked it. Following
        // that would jump the container to the bottom for a frame and back.
        if (a.PendingClickEntity == o.Entity) return;
        // The SEED twin of the gate above: a press on the coloured view spawned
        // this field (the owner opens it focused, cursor parked at the buffer end)
        // and will be turned into a caret index as soon as the geometry binds.
        // The press's screen position is only valid against the geometry it was
        // made over — if this pass drags the container to the buffer end first,
        // the seed maps the press through the moved layout, the caret lands
        // elsewhere, and the view the player pointed at has slid away from under
        // the cursor. Hold the container still for the window; it expires in a
        // few frames (RouteMouse drops it when the press is consumed or the
        // mouse is up), after which the follow chases the parked cursor as usual.
        if (a.SeedClickFrames > 0) { st.Pending = false; return; }

        if (st.Entity != o.Entity || st.Cursor != a.State.Cursor || st.Length != a.Buffer.Length
            || st.SelStart != a.State.SelectStart || st.SelEnd != a.State.SelectEnd)
            st.Pending = true;
        st.Entity = o.Entity;
        st.Cursor = a.State.Cursor;
        st.Length = a.Buffer.Length;
        st.SelStart = a.State.SelectStart;
        st.SelEnd = a.State.SelectEnd;
        if (!st.Pending) return;

        // Nearest scrolling ancestor. The field is a child of its owner's own
        // padding/row nodes, so the container is never the parent itself.
        var walk = o.Entity;
        for (var depth = 0; depth < 64; depth++)
        {
            if (!parents.TryGet(walk, out var parentRow)) break;
            var (_, parent) = parentRow;
            walk = parent.Ref.Id;
            if (walk == 0) break;
            if (!scrollers.TryGet(walk, out var scrollRow)) continue;
            var (_, node, scroll) = scrollRow;
            if (node.Ref.Overflow != Overflow.Scroll) continue;
            if (!ctx.Value.TryGetElementBoundingBox(walk, out var box)) break;

            var top = box.Y + Px(node.Ref.Padding.Top);
            var bottom = box.Y + box.Height - Px(node.Ref.Padding.Bottom);
            if (bottom - top < o.CaretH) break;

            // Clay's ScrollPosition.Y is a POSITIVE scrolled-down amount (child
            // offset -= it), clamped to [0, content - viewport] by the layout.
            var offset = scroll.Ref.OffsetY;
            if (o.CaretY < top)
                offset -= top - o.CaretY + Margin;
            else if (o.CaretY + o.CaretH > bottom)
                offset += o.CaretY + o.CaretH - bottom + Margin;
            else
                break;

            // Clamp here rather than letting the layout do it: an offset the
            // container cannot reach (caret on the last row, Margin past the
            // content end) would otherwise be re-requested every frame and keep
            // the follow armed, which is exactly what ate the wheel.
            var data = ctx.Value.GetScrollContainerData(SelfEditGeometry.NodeClayId(walk));
            var max = data.Found ? data.MaxScrollY : float.MaxValue;
            var target = Math.Clamp(offset, 0f, max);
            if (MathF.Abs(target - scroll.Ref.OffsetY) < 0.5f) break;

            scroll.Ref.OffsetY = target;
            scrollers.SetChanged<ScrollPosition>(walk);
            return;
        }

        st.Pending = false;
    }

    private static float Px(in Val v) => v.Type == ValType.Px ? v.Value : 0f;
}
