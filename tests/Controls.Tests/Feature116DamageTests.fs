module Feature116DamageTests

// Feature 116 (US1, FR-001/FR-002/FR-003/FR-004, SC-001) — the per-frame DAMAGE set surfaced as the
// internal `WorkReductionRecord.RepaintedNodeCount` / `DirtyRectCount` / `DirtyArea`, reached through
// `[<assembly: InternalsVisibleTo("Controls.Tests")>]` over the REAL wired `RetainedRender.step` path.
// A localized visual change reports a small region (the changed control's box, not the whole frame); a
// theme switch (all paint invalidated) reports frame-spanning damage (every node repainted); an idle
// frame reports `0/0/0`; the integer counts are deterministic across runs. Render-only / deterministic
// — no live Vulkan window ([[fs-skia-evidence-mode]]).
//
// DirtyArea definition (research §a, pinned in readiness/damage-metrics-authority.md): the summed
// integer w*h over the DISTINCT repainted boxes. A localized change covers only the changed box
// (< FrameArea); a theme switch repaints every node (RepaintedNodeCount = TotalNodeCount) so its area
// is frame-spanning (≫ the localized area).

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }
let private frameArea = 640 * 480

let private rinit (t: Theme) (s: Size) (c: Control<'msg>) : RetainedRender<'msg> =
    (RetainedRender.init t s c).Retained

let private leaf (key: string) (content: string) : Control<int> =
    { Kind = "text-block"
      Key = Some key
      Attributes =
        [ { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 120.0 }
          { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 24.0 } ]
      Children = []
      Content = Some content
      Accessibility = None }

let private stack (children: Control<int> list) : Control<int> =
    { Kind = "stack"
      Key = None
      Attributes = []
      Children = children
      Content = None
      Accessibility = None }

let private tree (labels: string list) : Control<int> =
    stack [ for l in labels -> leaf l (l.ToUpper()) ]

[<Tests>]
let tests =
    testList "Feature 116 damage set (US1, FR-001/002/003/004, SC-001)" [

        test "idle frame: an unchanged tree repaints nothing → 0/0/0 (FR-003)" {
            let c = tree [ "a"; "b"; "c" ]
            let r0 = rinit theme size c
            let s = RetainedRender.step theme size r0 c

            Expect.equal s.WorkReduction.RepaintedNodeCount 0 "idle repaints no node"
            Expect.equal s.WorkReduction.DirtyRectCount 0 "idle has no dirty rectangle"
            Expect.equal s.WorkReduction.DirtyArea 0 "idle has zero dirty area"
        }

        test "localized change: one leaf's content change reports a small region, not frame-spanning (FR-001/FR-002)" {
            let before = tree [ "a"; "b"; "c" ]
            let after = stack [ leaf "a" "A"; leaf "b" "CHANGED"; leaf "c" "C" ]
            let r0 = rinit theme size before
            let s = RetainedRender.step theme size r0 after

            let total = Control.count after

            // exactly the changed leaf repaints (fixed size → no sibling shift), so RepaintedNodeCount = 1.
            Expect.equal s.WorkReduction.RepaintedNodeCount 1 "only the changed leaf repaints"
            Expect.isTrue (s.WorkReduction.RepaintedNodeCount <= 4) "localized: <= 4 (the regression tripwire)"
            Expect.isTrue (s.WorkReduction.RepaintedNodeCount < total) "localized: fewer than every node"
            Expect.equal s.WorkReduction.DirtyRectCount 1 "one distinct damaged box"
            Expect.equal s.WorkReduction.DirtyArea (120 * 24) "damaged area = exactly the changed leaf's box (120*24)"
            Expect.isTrue (s.WorkReduction.DirtyArea < frameArea) "localized damage is far below the frame area"
        }

        test "theme switch: all paint invalidated → every node repainted, frame-spanning (FR-002)" {
            let c = tree [ "a"; "b"; "c" ]
            let r0 = rinit theme size c
            // step the SAME tree under a different theme: the theme is part of the reuse key, so every
            // node repaints under the new theme.
            let s = RetainedRender.step Theme.dark size r0 c

            let total = Control.count c
            Expect.equal s.WorkReduction.RepaintedNodeCount total "theme switch repaints every node (TotalNodeCount)"
            Expect.isTrue (s.WorkReduction.DirtyArea > (120 * 24)) "theme-switch damage is frame-spanning (≫ a single leaf)"
        }

        test "localized vs theme: damage is proportional to the change (SC-001)" {
            let before = tree [ "a"; "b"; "c"; "d" ]
            let localizedAfter = stack [ leaf "a" "A"; leaf "b" "B2"; leaf "c" "C"; leaf "d" "D" ]
            let r0 = rinit theme size before

            let localized = RetainedRender.step theme size r0 localizedAfter
            let themeSpanning = RetainedRender.step Theme.dark size r0 before

            Expect.isTrue
                (localized.WorkReduction.RepaintedNodeCount < themeSpanning.WorkReduction.RepaintedNodeCount)
                "a localized change repaints far fewer nodes than a whole-frame invalidation"
            Expect.isTrue
                (localized.WorkReduction.DirtyArea < themeSpanning.WorkReduction.DirtyArea)
                "a localized change damages far less area than a whole-frame invalidation"
        }

        test "deterministic: the integer damage counts re-run byte-identically (FR-004)" {
            let before = tree [ "a"; "b"; "c" ]
            let after = stack [ leaf "a" "A"; leaf "b" "Z"; leaf "c" "C" ]

            let run () =
                let r0 = rinit theme size before
                let s = RetainedRender.step theme size r0 after
                s.WorkReduction.RepaintedNodeCount, s.WorkReduction.DirtyRectCount, s.WorkReduction.DirtyArea

            Expect.equal (run ()) (run ()) "identical (prev, next) → identical damage integers"
        }
    ]
