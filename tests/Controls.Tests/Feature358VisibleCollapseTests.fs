module Feature358VisibleCollapseTests

// Feature 358 — `Attr.visible false` is honoured on the PRODUCT render path (`Control.renderTree` and
// the retained path), not just the single-control preview. The documented contract (`Attributes.fsi`:
// "when false the control is hidden from layout and paint") was implemented only in the preview walk;
// the shipped `renderTree`/`RetainedRender` path fully laid out AND fully painted a hidden control.
//
// The fix routes visibility through the existing layout->bounds->paint pipeline: `toLayout` lowers a
// hidden control (and its whole subtree) with `Visibility = Collapsed` (Yoga `Display.None`), and
// `boundsByIdOf` drops `Collapsed` bounds from the paint/hit-test map — so `paintNode` returns `[]`
// and `nodeBox` returns `None` for the hidden subtree, in BOTH render paths. `Attr.visible` is
// categorised `Layout` so the incremental classifier re-measures on a toggle (the name channel is
// gated shut by the Feature 101 drift probe; this mirrors `elevation`, a category-only layout signal).
//
// These tests drive the REAL `Control.renderTree` and `RetainedRender.init`/`step` — no mock/fake/stub.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: FS.GG.UI.Scene.Size = { Width = 320; Height = 240 }

let private render (control: Control<unit>) = Control.renderTree theme size control

/// Bounds id (`Key ?? path`) -> Rect, as `renderTree`/the retained path surface it.
let private boundOf (result: ControlRenderResult<unit>) (id: string) : Rect option =
    result.Bounds |> List.tryPick (fun (nodeId, rect) -> if nodeId = id then Some rect else None)

let private boundIds (result: ControlRenderResult<unit>) : string list =
    result.Bounds |> List.map fst

/// Every text string painted anywhere in the scene (the product path paints control labels as
/// `TextRun`; `Text`/`SizedText` are covered for completeness). Used to prove a hidden control paints
/// NOTHING — a marker string present when visible must be absent when hidden.
let private sceneTexts (result: ControlRenderResult<unit>) : string list =
    let rec go (s: Scene) = s.Nodes |> List.collect goNode

    and goNode (n: SceneNode) : string list =
        match n with
        | Text(_, t, _) -> [ t ]
        | SizedText(_, t, _, _) -> [ t ]
        | TextRun r -> [ r.Text ]
        | Group scenes -> scenes |> List.collect go
        | ClipNode(_, inner)
        | Translate(_, inner)
        | ColorSpaceNode(_, inner)
        | PerspectiveNode(_, inner) -> go inner
        | PictureNode p -> go p.Scene
        | CachedSubtree b -> go b.Scene
        | _ -> []

    go result.Scene

let private textPainted (result: ControlRenderResult<unit>) (marker: string) : bool =
    sceneTexts result |> List.exists (fun t -> t.Contains marker)

// A vertical stack (gap 0 so a reserved slot would be unambiguous) of keyed children.
let private stackOf (children: Control<unit> list) : Control<unit> =
    Stack.create [ Attr.gap 0.0; Stack.children children ]

let private leaf (key: string) (attrs: Attr<unit> list) (text: string) : Control<unit> =
    TextBlock.create (TextBlock.text text :: attrs) |> Control.withKey key

[<Tests>]
let tests =
    testList
        "Feature358 visible=false collapses on the product path"
        [

          test "a hidden leaf contributes NO bounds and NO scene; its visible sibling is unaffected" {
              let tree =
                  stackOf
                      [ leaf "a" [] "ALPHA"
                        leaf "b" [ Attr.visible false ] "SECRET" ]

              let result = render tree

              Expect.isSome (boundOf result "a") "the visible sibling is laid out"
              Expect.isNone (boundOf result "b") "the hidden control contributes no bounds"
              Expect.isFalse (List.contains "b" (boundIds result)) "the hidden id is absent from the Bounds list"
              Expect.isTrue (textPainted result "ALPHA") "the visible sibling paints its label"
              Expect.isFalse (textPainted result "SECRET") "the hidden control paints nothing"
          }

          test "a hidden control reserves NO layout space — a following sibling does not shift" {
              let hiddenLeading =
                  stackOf
                      [ leaf "h" [ Attr.visible false ] "SECRET"
                        leaf "a" [] "ALPHA" ]

              let removedLeading = stackOf [ leaf "a" [] "ALPHA" ]

              let visibleLeading =
                  stackOf
                      [ leaf "h" [] "HEADER"
                        leaf "a" [] "ALPHA" ]

              let yHidden = (boundOf (render hiddenLeading) "a").Value.Y
              let yRemoved = (boundOf (render removedLeading) "a").Value.Y
              let yVisible = (boundOf (render visibleLeading) "a").Value.Y

              Expect.floatClose Accuracy.high yHidden yRemoved "a hidden leading sibling reserves no space: 'a' sits where it would with the sibling removed entirely"
              Expect.isTrue (yVisible > yHidden + 0.5) "sanity: a VISIBLE leading sibling DOES push 'a' down, so the test above is load-bearing"
          }

          test "a hidden container hides its WHOLE subtree — no bounds, no scene for its descendants" {
              let tree =
                  stackOf
                      [ leaf "a" [] "ALPHA"
                        Stack.create
                            [ Attr.visible false
                              Stack.children [ leaf "deep" [] "BURIED" ] ]
                        |> Control.withKey "box" ]

              let result = render tree

              Expect.isSome (boundOf result "a") "the visible sibling is laid out"
              Expect.isNone (boundOf result "box") "the hidden container contributes no bounds"
              Expect.isNone (boundOf result "deep") "a descendant of a hidden container contributes no bounds"
              Expect.isFalse (textPainted result "BURIED") "a descendant of a hidden container paints nothing"
          }

          test "the retained path agrees with renderTree for a hidden control (init parity)" {
              let tree =
                  stackOf
                      [ leaf "a" [] "ALPHA"
                        leaf "b" [ Attr.visible false ] "SECRET" ]

              let full = render tree
              let inited = RetainedRender.init theme size tree

              Expect.equal inited.Render.Bounds full.Bounds "retained init bounds equal renderTree bounds (hidden 'b' absent from both)"
              Expect.equal (sceneTexts inited.Render) (sceneTexts full) "retained init scene text equals renderTree (neither paints 'SECRET')"
              Expect.isFalse (textPainted inited.Render "SECRET") "the retained path paints nothing for the hidden control"
          }

          test "a hidden ROOT renders no control bounds and no label, without error" {
              let root = leaf "root" [ Attr.visible false ] "ROOTTEXT"
              let result = render root

              Expect.isNone (boundOf result "root") "a hidden root contributes no bounds"
              Expect.isFalse (textPainted result "ROOTTEXT") "a hidden root paints no label"
          }

          test "toggling visible re-measures incrementally (the AttrCategory.Layout channel)" {
              let visibleTree =
                  stackOf
                      [ leaf "a" [] "ALPHA"
                        leaf "b" [ Attr.visible true ] "BETA" ]

              let hiddenTree =
                  stackOf
                      [ leaf "a" [] "ALPHA"
                        leaf "b" [ Attr.visible false ] "BETA" ]

              // visible -> hidden: a retained step must collapse 'b' (bounds + scene gone).
              let r0 = RetainedRender.init theme size visibleTree
              let toHidden = RetainedRender.step theme size r0.Retained hiddenTree
              Expect.equal toHidden.Render.Bounds (render hiddenTree).Bounds "stepping visible->hidden collapses 'b' (matches a full render of the hidden tree)"
              Expect.isFalse (textPainted toHidden.Render "BETA") "stepping to hidden stops painting 'b'"

              // hidden -> visible: the reverse step must bring 'b' back.
              let r1 = RetainedRender.init theme size hiddenTree
              let toVisible = RetainedRender.step theme size r1.Retained visibleTree
              Expect.equal toVisible.Render.Bounds (render visibleTree).Bounds "stepping hidden->visible restores 'b' (matches a full render of the visible tree)"
              Expect.isTrue (textPainted toVisible.Render "BETA") "stepping to visible paints 'b' again"
          }
        ]
