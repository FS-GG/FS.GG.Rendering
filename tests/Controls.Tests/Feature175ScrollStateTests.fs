module Feature175ScrollStateTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Scene

// Feature 175 (US1, FR-001/FR-002): the pure `ScrollState` transition + derived thumb geometry.
// These exercise the new surface that did not exist before the feature (failing-first: the type and
// module are new; no prior test could be green). T008 = clamp; T009 = thumb height/position + no-thumb.

let private extent content viewport =
    ScrollState.empty |> ScrollState.withExtent content viewport

[<Tests>]
let tests =
    testList "Feature175ScrollState" [
        // ---- T008: applyScrollDelta clamps at both bounds, no overscroll --------------------------
        test "applyScrollDelta clamps to [0, max(0, content - viewport)] at the top bound" {
            let s = extent 1000.0 400.0 // maxOffset = 600
            let scrolledUp = s |> ScrollState.applyScrollDelta -50.0
            Expect.equal scrolledUp.Offset 0.0 "scrolling up from 0 stays clamped at 0 (no negative overscroll)"
        }

        test "applyScrollDelta clamps at the bottom bound with no overscroll" {
            let s = extent 1000.0 400.0 // maxOffset = 600
            let scrolledFar = s |> ScrollState.applyScrollDelta 100000.0
            Expect.equal scrolledFar.Offset 600.0 "a huge delta clamps to maxOffset, never beyond"
        }

        test "applyScrollDelta accumulates within bounds and re-clamps each step" {
            let s = extent 1000.0 400.0
            let a = s |> ScrollState.applyScrollDelta 250.0
            Expect.equal a.Offset 250.0 "in-range delta applies exactly"
            let b = a |> ScrollState.applyScrollDelta 250.0
            Expect.equal b.Offset 500.0 "second in-range delta accumulates"
            let c = b |> ScrollState.applyScrollDelta 250.0
            Expect.equal c.Offset 600.0 "the step that would overshoot clamps to maxOffset"
            let d = c |> ScrollState.applyScrollDelta -1000.0
            Expect.equal d.Offset 0.0 "a large negative returns cleanly to the top"
        }

        test "non-overflowing content has maxOffset 0 so any delta stays at 0" {
            let s = extent 300.0 400.0 // content fits
            Expect.equal (ScrollState.maxOffset s) 0.0 "no scroll range when content fits"
            let scrolled = s |> ScrollState.applyScrollDelta 500.0
            Expect.equal scrolled.Offset 0.0 "cannot scroll a fitting region"
        }

        // ---- T009: thumb height/position + no-thumb when content fits (incl. dead-zone) -----------
        test "thumb height derives from the viewport/content ratio" {
            let s = extent 1000.0 400.0 // ratio 0.4
            Expect.floatClose Accuracy.high (ScrollState.thumbHeight s) (400.0 * 400.0 / 1000.0) "thumb height = viewport * viewport/content"
        }

        test "thumb position tracks the offset monotonically across the track" {
            let s = extent 1000.0 400.0
            let track = 400.0
            let atTop = ScrollState.thumbPosition track { s with Offset = 0.0 }
            let atMid = ScrollState.thumbPosition track { s with Offset = 300.0 }
            let atBottom = ScrollState.thumbPosition track { s with Offset = ScrollState.maxOffset s }
            Expect.equal atTop 0.0 "thumb at track top when offset is 0"
            Expect.isTrue (atMid > atTop && atBottom > atMid) "thumb position increases with offset"
            let travel = track - ScrollState.thumbHeight s
            Expect.floatClose Accuracy.high atBottom travel "thumb bottoms out at (track - thumbHeight)"
        }

        test "no draggable thumb when content fits the viewport exactly" {
            let s = extent 400.0 400.0
            Expect.isFalse (ScrollState.scrollable s) "exact fit is not scrollable"
            Expect.equal (ScrollState.thumbHeight s) 0.0 "no thumb is presented on an exact fit"
            Expect.equal (ScrollState.thumbPosition 400.0 s) 0.0 "no thumb position on an exact fit"
        }

        test "one-pixel overflow is treated as non-scrollable (dead-zone, avoids flicker)" {
            let s = extent 401.0 400.0 // 1px overflow
            Expect.isFalse (ScrollState.scrollable s) "a one-pixel overflow stays within the dead-zone"
            Expect.equal (ScrollState.thumbHeight s) 0.0 "no flickering thumb for a one-pixel overflow"
        }

        test "withExtent re-clamps a stale offset when the content shrinks" {
            let scrolled = extent 1000.0 400.0 |> ScrollState.applyScrollDelta 600.0 // offset 600
            let shrunk = scrolled |> ScrollState.withExtent 500.0 400.0 // new maxOffset = 100
            Expect.equal shrunk.Offset 100.0 "shrinking content re-clamps the offset to the new maxOffset"
        }
    ]

// ---- T010 (FR-009): offset-aware hit-testing — paint AND hit-test both read the shifted bounds ----
// The host stamps a `scrollOffset` attr on the scroll-viewer; the layout bounds of its descendants
// shift up by that offset, so a screen point resolves to the content that scrolled under it.

[<Tests>]
let offsetAwareHitTestTests =
    let row key =
        Button.create [ Button.text key; Attr.width 120.0; Attr.height 40.0 ] |> Control.withKey key

    // A scroll-viewer taller than its viewport, optionally carrying a stamped scroll offset.
    let scrollViewer (offset: float) =
        let rows = [ for i in 1..8 -> row (sprintf "r%d" i) ]
        let content = Stack.create [ Stack.orientation "vertical"; Stack.children rows ]
        Control.create
            "scroll-viewer"
            [ yield Attr.children [ content ]
              if offset > 0.0 then yield Attr.create AttrKeys.ScrollOffset Layout (FloatValue offset) ]
        |> Control.withKey "sv"

    let hitAt offset x y =
        Control.hitTest (Feature150ScrollFixtures.render (scrollViewer offset)) x y

    testList "Feature175OffsetAwareHitTest" [
        test "scrolling by delta makes screen point y resolve to the content at y+delta" {
            let delta = 40.0
            let x = 30.0
            // For several in-viewport rows, the scrolled hit equals the unscrolled hit one delta lower.
            for y in [ 30.0; 50.0; 70.0 ] do
                let scrolled = hitAt delta x y
                let unscrolledLower = hitAt 0.0 x (y + delta)
                Expect.equal scrolled unscrolledLower (sprintf "at y=%g, scrolled hit == unscrolled hit at y+delta" y)
        }

        test "a positive offset changes which control is under a fixed screen point" {
            let x = 30.0
            let y = 70.0
            let atRest = hitAt 0.0 x y
            let scrolled = hitAt 120.0 x y
            Expect.notEqual scrolled atRest "after scrolling, a later row sits under the same screen point"
        }
    ]

// ---- T016 (FR-001): keyboard scroll-key → delta mapper (the enumerated keys) ----
[<Tests>]
let scrollKeyTests =
    let vp = 300.0
    testList "Feature175ScrollKeys" [
        test "arrow keys apply a line step in each direction" {
            Expect.equal (Pointer.scrollKeyDelta "ArrowDown" false vp) (Some 40.0) "ArrowDown = +line step"
            Expect.equal (Pointer.scrollKeyDelta "ArrowUp" false vp) (Some -40.0) "ArrowUp = -line step"
        }
        test "page keys apply a viewport-height step" {
            Expect.equal (Pointer.scrollKeyDelta "PageDown" false vp) (Some vp) "PageDown = +viewport"
            Expect.equal (Pointer.scrollKeyDelta "PageUp" false vp) (Some -vp) "PageUp = -viewport"
        }
        test "Home/End emit a large signed delta (clamped to top/bottom downstream)" {
            let home = Pointer.scrollKeyDelta "Home" false vp |> Option.get
            let endd = Pointer.scrollKeyDelta "End" false vp |> Option.get
            Expect.isLessThan home -1000.0 "Home is a large negative (lands at top after clamp)"
            Expect.isGreaterThan endd 1000.0 "End is a large positive (lands at bottom after clamp)"
            // verify the clamp lands exactly at the bounds via applyScrollDelta
            let s = ScrollState.empty |> ScrollState.withExtent 1000.0 vp
            Expect.equal (ScrollState.applyScrollDelta home s).Offset 0.0 "Home clamps to top"
            Expect.equal (ScrollState.applyScrollDelta endd s).Offset (ScrollState.maxOffset s) "End clamps to bottom"
        }
        test "Space pages down, Shift+Space pages up" {
            Expect.equal (Pointer.scrollKeyDelta "Space" false vp) (Some vp) "Space = page down"
            Expect.equal (Pointer.scrollKeyDelta "Space" true vp) (Some -vp) "Shift+Space = page up"
        }
        test "a non-scroll key yields no delta" {
            Expect.equal (Pointer.scrollKeyDelta "Enter" false vp) None "Enter does not scroll"
            Expect.equal (Pointer.scrollKeyDelta "a" false vp) None "a letter does not scroll"
        }
    ]
