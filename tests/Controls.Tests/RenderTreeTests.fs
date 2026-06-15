module ControlsRenderTreeTests

// Feature 085 US1 — Control.renderTree faithful nested-tree rendering (SC-001, FR-001/002/003).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg = Clicked

/// Count painted primitives in a Scene (recursing into Group/Clip/ColorSpace/Perspective
/// wrappers), so "nested children painted" is a structural fact, not a guess.
let rec private nodeCount (scene: Scene) : int =
    scene.Nodes
    |> List.sumBy (fun node ->
        match node with
        | SceneNode.Empty -> 0
        | SceneNode.Group children -> children |> List.sumBy nodeCount
        | SceneNode.ClipNode(_, inner)
        | SceneNode.ColorSpaceNode(_, inner)
        | SceneNode.PerspectiveNode(_, inner) -> nodeCount inner
        | SceneNode.PictureNode pic -> nodeCount pic.Scene
        | _ -> 1)

let private theme = Theme.light
let private size = { Width = 640; Height = 480 }

let private pageA: Control<Msg> =
    Stack.create
        [ Stack.children
              [ TextBlock.create [ TextBlock.text "Alpha" ]
                Button.create [ Button.text "Go"; Button.onClick Clicked ] ] ]

let private pageB: Control<Msg> =
    Stack.create
        [ Stack.children
              [ TextBlock.create [ TextBlock.text "Beta" ]
                Stack.create [ Stack.children [ TextBlock.create [ TextBlock.text "Nested" ] ] ] ] ]

[<Tests>]
let renderTreeTests =
    testList "Feature 085 Control.renderTree (US1)" [
        test "structurally different trees produce different scenes (SC-001)" {
            let a = Control.renderTree theme size pageA
            let b = Control.renderTree theme size pageB
            Expect.notEqual a.Scene b.Scene "two structurally different trees must produce different scenes"
        }

        test "nested children are laid out and painted, not just the outer container (FR-001/FR-002)" {
            let twoKids: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ TextBlock.create [ TextBlock.text "first" ]
                            TextBlock.create [ TextBlock.text "second" ] ] ]

            let rendered = Control.renderTree theme size twoKids
            // container frame + two child leaves => strictly more than one painted node.
            Expect.isGreaterThan (nodeCount rendered.Scene) 1 "nested children contribute painted nodes"

            // Changing only a NESTED child changes the scene => children really are rendered.
            let twoKidsChanged: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ TextBlock.create [ TextBlock.text "first" ]
                            TextBlock.create [ TextBlock.text "CHANGED" ] ] ]

            let renderedChanged = Control.renderTree theme size twoKidsChanged
            Expect.notEqual rendered.Scene renderedChanged.Scene "a nested-child change must change the scene"
        }

        test "renderTree lays content out to the output extent — distinct sizes differ (size-aware, FR-009 seed)" {
            let small = Control.renderTree theme { Width = 320; Height = 240 } pageA
            let large = Control.renderTree theme { Width = 1024; Height = 768 } pageA
            Expect.notEqual small.Scene large.Scene "the same tree at two extents lays out differently"
        }

        test "renderTree carries event bindings + node count for host hit-testing" {
            let rendered = Control.renderTree theme size pageA
            Expect.equal rendered.NodeCount (Control.count pageA) "NodeCount matches Control.count"
            Expect.isNonEmpty rendered.EventBindings "the bound Button contributes an EventBinding for hit-testing"
        }

        // T011 — preservation guard (FR-003): the 080 single-control PREVIEW (`Control.render`)
        // is untouched and remains distinct from the nested-tree renderer.
        test "Control.render preview is preserved and additive to renderTree (FR-003)" {
            let preview = Control.render theme pageA
            Expect.isGreaterThan (nodeCount preview.Scene) 0 "Control.render still produces a non-empty preview scene"
            Expect.equal preview.NodeCount (Control.count pageA) "Control.render NodeCount unchanged"
            let tree = Control.renderTree theme size pageA
            // The preview (flattened, fixed-y stack) and the laid-out tree are different renderers.
            Expect.notEqual preview.Scene tree.Scene "renderTree is additive — it does not replace the 080 preview"
        }
    ]
