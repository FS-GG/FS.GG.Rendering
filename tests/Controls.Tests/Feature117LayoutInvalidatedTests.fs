module Feature117LayoutInvalidatedTests

// Feature 117 (Phase 8, US2/US3, FR-006/FR-007/FR-008, SC-003/SC-006) — the layout-invalidated node
// count is the size of the PRE-pinning dirty set fed into incremental layout, distinct from the
// POST-pinning `RemeasuredNodeCount`. Reached through `[<assembly: InternalsVisibleTo("Controls.Tests")>]`
// over the real wired `RetainedRender.step`. An idle / style-only / visual-state-only frame invalidates
// and re-measures ZERO nodes and (on warm text) produces ZERO text-cache misses; a geometry frame
// reports a bounded, explainable `LayoutInvalidatedNodeCount` that is `<= RemeasuredNodeCount` (since
// fixed-size-ancestor propagation EXPANDS the pre-pinning dirty set into the re-measured boundary
// subtree — the honest direction the framework guarantees; see readiness/layout-invalidated-authority.md
// for the spec correction); and the feature-101 drift guard's attribute set is unchanged (FR-008).
// Render-only / deterministic ([[fs-gg-evidence-mode]], [[fs-gg-reconciliation]]).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }
let private rinit (c: Control<'msg>) = (RetainedRender.init theme size c).Retained

let private rowWith (key: string) (attrs: Attr<int> list) (content: string) : Control<int> =
    { Kind = "data-grid-row"; Key = Some key; Attributes = attrs; Children = []; Content = Some content; Accessibility = None }

let private style name value = { Name = name; Category = AttrCategory.Style; Value = value }

// A FIXED-size stack (so a child's re-measure boundary is the stack, not the root) of labelled rows.
// One row's `selected` style flips with model parity (style-only); one row's `width` tracks the model
// (geometry) when `geom` is set.
let private grid (n: int) (geom: bool) (model: int) : Control<int> =
    { Kind = "stack"
      Key = None
      Attributes = [ style "width" (FloatValue 240.0); style "height" (FloatValue 400.0) ]
      Children =
        [ for i in 0 .. n - 1 ->
            let baseAttrs =
                [ style "width" (FloatValue(if geom && i = 1 then 120.0 + float (model % 3) * 20.0 else 200.0))
                  style "height" (FloatValue 24.0)
                  style "selected" (BoolValue(model % 2 = 0)) ]
            rowWith (sprintf "r%d" i) baseAttrs (sprintf "label-%d" i) ]
      Content = None
      Accessibility = None }

[<Tests>]
let tests =
    testList "Feature 117 layout-invalidated count + style-only zero-work (US2/US3, FR-006/007/008)" [

        test "an idle frame invalidates and re-measures zero nodes (FR-006)" {
            let v = grid 5 false 0
            let r0 = rinit v
            let s = RetainedRender.step theme size r0 v // structurally identical → all-Keep

            Expect.equal s.WorkReduction.LayoutInvalidatedNodeCount 0 "an idle frame invalidates nothing"
            Expect.equal s.WorkReduction.RemeasuredNodeCount 0 "an idle frame re-measures nothing"
        }

        test "a style-only frame: zero invalidated, zero re-measured (FR-006/FR-007, SC-003)" {
            // `selected` flips with model parity — a Style attr, NOT in layoutAffectingAttrNames — so the
            // dirty set is empty even though every row repaints.
            let r0 = rinit (grid 5 false 0)
            let s = RetainedRender.step theme size r0 (grid 5 false 1)

            Expect.equal s.WorkReduction.LayoutInvalidatedNodeCount 0 "a style-only change invalidates no layout node"
            Expect.equal s.WorkReduction.RemeasuredNodeCount 0 "a style-only change re-measures no node"
        }

        test "a style-only / visual-state-only frame over warm text produces ZERO text-cache misses (FR-007, SC-003)" {
            // First step warms the text cache (a style flip repaints+measures every row → cold misses).
            // The second style-only flip re-measures the SAME (unchanged) text → all hits, zero misses,
            // still zero invalidated / zero re-measured.
            let r0 = rinit (grid 6 false 0)
            let warm = RetainedRender.step theme size r0 (grid 6 false 1)
            let s = RetainedRender.step theme size warm.Retained (grid 6 false 2)

            Expect.equal s.WorkReduction.LayoutInvalidatedNodeCount 0 "style-only: zero invalidated"
            Expect.equal s.WorkReduction.RemeasuredNodeCount 0 "style-only: zero re-measured"
            Expect.equal s.WorkReduction.TextMeasureCacheMisses 0 "style-only over unchanged text: ZERO text-cache misses (all served warm)"
            Expect.isTrue (s.WorkReduction.TextMeasureCacheHits > 0) "the unchanged text is served from the warm cache (hits)"
        }

        test "a geometry frame: bounded invalidated <= re-measured, both > 0 (FR-006, SC-006)" {
            let n = 5
            let r0 = rinit (grid n true 0)
            let s = RetainedRender.step theme size r0 (grid n true 1) // row 1's width changes

            let invalidated = s.WorkReduction.LayoutInvalidatedNodeCount
            let remeasured = s.WorkReduction.RemeasuredNodeCount
            let total = s.Render.NodeCount

            Expect.isTrue (invalidated >= 1) "a geometry change invalidates at least the changed node"
            Expect.isTrue (invalidated <= remeasured) (sprintf "invalidated %d <= re-measured %d (propagation expands the pre-pinning set)" invalidated remeasured)
            Expect.isTrue (remeasured >= 1) "a geometry change re-measures at least one node"
            Expect.isTrue (invalidated <= total) (sprintf "invalidated %d is bounded by the total node count %d" invalidated total)
        }

        test "the feature-101 drift-guard attribute set is unchanged — no new geometry-driving attribute (FR-008)" {
            Expect.equal
                ControlInternals.layoutAffectingAttrNames
                (Set.ofList [ "width"; "height"; "orientation" ])
                "this rung adds no new layout-affecting attribute (the drift guard stays in force)"
        }
    ]
