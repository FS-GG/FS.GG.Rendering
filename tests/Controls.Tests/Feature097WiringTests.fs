module Feature097WiringTests

// R2 — the incremental evaluator wired onto the live `RetainedRender.step` path. These tests exercise
// the user-reachable retained render path (vertical slice): the extended `WorkReductionRecord`
// re-measure metric (FR-006/SC-003), the patch-derived dirty set (FR-003/SC-004), and byte-identity of
// the wired path vs a full `Control.renderTree` for every frame — localized, geometry-changing, and
// at-rest (FR-008/SC-005). Real wired path; no synthetic fixtures.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 400; Height = 300 }

let private rinit (c: Control<int>) = (RetainedRender.init theme size c).Retained

let private attr name value : Attr<int> =
    { Name = name; Category = AttrCategory.Style; Value = FloatValue value }

let private leaf key w h : Control<int> =
    { Kind = "text-block"
      Key = Some key
      Attributes = [ attr "width" w; attr "height" h ]
      Children = []
      Content = Some key
      Accessibility = None }

let private container kind key (attrs: Attr<int> list) children : Control<int> =
    { Kind = kind
      Key = Some key
      Attributes = attrs
      Children = children
      Content = None
      Accessibility = None }

/// root(stack) -> [ panel(fixed 200x100) -> [leafA, leafB] ; sibling ]. The panel is a fixed-size
/// boundary, so a change to leafA re-measures only the panel subtree and reuses root + sibling.
let private tree (leafAWidth: float) : Control<int> =
    container "stack" "root" [] [
        container "panel" "panel" [ attr "width" 200.0; attr "height" 100.0 ] [
            leaf "leafA" leafAWidth 20.0
            leaf "leafB" 50.0 20.0
        ]
        leaf "sibling" 100.0 30.0
    ]

let private sceneOf (r: ControlRenderResult<int>) = r.Scene

[<Tests>]
let tests =
    testList "Feature097 wired incremental layout" [

        test "localized geometry edit re-measures a proper subset and stays byte-identical to a full rebuild (SC-001/SC-003/FR-008)" {
            let r0 = rinit (tree 50.0)
            let next = tree 70.0 // only leafA width changed; no child op
            let s = RetainedRender.step theme size r0 next
            let baseline = s.WorkReduction.BaselineNodeCount

            Expect.isLessThan s.WorkReduction.RemeasuredNodeCount baseline "localized edit re-measures a strict subset"
            Expect.isGreaterThan s.WorkReduction.RemeasuredNodeCount 0 "but the geometry change DID re-measure (not stale)"
            // The decisive byte-identity check: the wired incremental Scene equals a full rebuild of next.
            let full = Control.renderTree theme size next
            Expect.equal (sceneOf s.Render) (sceneOf full) "wired incremental render == full rebuild (geometry change)"
        }

        test "at-rest frame (identical tree) re-measures nothing and renders byte-identical (FR-008 at-rest)" {
            let r0 = rinit (tree 50.0)
            let s = RetainedRender.step theme size r0 (tree 50.0)
            Expect.equal s.WorkReduction.RemeasuredNodeCount 0 "empty patch re-measures nothing"
            Expect.equal (sceneOf s.Render) (sceneOf (Control.renderTree theme size (tree 50.0))) "at-rest render byte-identical"
        }

        test "content-only change re-measures nothing yet stays byte-identical (SC-004 non-layout)" {
            let withContent (txt: string) =
                container "stack" "root" [] [
                    container "panel" "panel" [ attr "width" 200.0; attr "height" 100.0 ] [
                        { leaf "leafA" 50.0 20.0 with Content = Some txt }
                        leaf "leafB" 50.0 20.0
                    ]
                    leaf "sibling" 100.0 30.0
                ]
            let r0 = rinit (withContent "one")
            let next = withContent "two" // content differs; NO geometry attr, NO child op
            let s = RetainedRender.step theme size r0 next
            Expect.equal s.WorkReduction.RemeasuredNodeCount 0 "a content-only change does not dirty measure"
            Expect.equal (sceneOf s.Render) (sceneOf (Control.renderTree theme size next)) "content change repaints, byte-identical"
        }

        test "a whole-tree geometry relayout re-measures the baseline (SC-003 never under-reports)" {
            // Change the ROOT's orientation: a content-sized chain to the root => full re-measure.
            let withOrientation (o: string) =
                { container "stack" "root" [ { Name = "orientation"; Category = AttrCategory.Style; Value = TextValue o } ] [
                      leaf "a" 50.0 20.0
                      leaf "b" 50.0 20.0 ] with Content = None }
            let r0 = rinit (withOrientation "vertical")
            let s = RetainedRender.step theme size r0 (withOrientation "horizontal")
            Expect.equal s.WorkReduction.RemeasuredNodeCount s.WorkReduction.BaselineNodeCount "root geometry change re-measures the whole tree"
        }

        test "a child insert dirties its container and stays byte-identical (FR-003 child op)" {
            let r0 = rinit (tree 50.0)
            let next =
                container "stack" "root" [] [
                    container "panel" "panel" [ attr "width" 200.0; attr "height" 100.0 ] [
                        leaf "leafA" 50.0 20.0
                        leaf "leafB" 50.0 20.0
                        leaf "leafC" 50.0 20.0 // inserted
                    ]
                    leaf "sibling" 100.0 30.0
                ]
            let s = RetainedRender.step theme size r0 next
            Expect.isGreaterThan s.WorkReduction.RemeasuredNodeCount 0 "child insert dirties its container"
            Expect.equal (sceneOf s.Render) (sceneOf (Control.renderTree theme size next)) "child insert byte-identical"
        }
    ]
