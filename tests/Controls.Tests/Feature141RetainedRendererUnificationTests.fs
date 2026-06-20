module Feature141RetainedRendererUnificationTests

open System
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default

type private Msg =
    | Clicked

let private theme = Theme.light
let private size: Size = { Width = 720; Height = 480 }

let rec private flattenScene (scene: Scene) : SceneNode list =
    scene.Nodes |> List.collect flattenNode

and private flattenNode (node: SceneNode) : SceneNode list =
    match node with
    | CachedSubtree boundary -> flattenScene boundary.Scene
    | Group scenes -> scenes |> List.collect flattenScene
    | ClipNode(clip, inner) -> [ ClipNode(clip, { Nodes = flattenScene inner }) ]
    | Translate(offset, inner) -> [ Translate(offset, { Nodes = flattenScene inner }) ]
    | ColorSpaceNode(colorSpace, inner) -> [ ColorSpaceNode(colorSpace, { Nodes = flattenScene inner }) ]
    | PerspectiveNode(transform, inner) -> [ PerspectiveNode(transform, { Nodes = flattenScene inner }) ]
    | PictureNode picture -> [ PictureNode { picture with Scene = { Nodes = flattenScene picture.Scene } } ]
    | other -> [ other ]

let private visibleShape (result: ControlRenderResult<Msg>) = flattenScene result.Scene

let private row key text : Control<Msg> =
    { Kind = "data-grid-row"
      Key = Some key
      Attributes = [ Attr.width 220.0; Attr.height 24.0 ]
      Children = []
      Content = Some text
      Accessibility = None }

let private text key value : Control<Msg> =
    TextBlock.create [ TextBlock.text value; Attr.width 180.0; Attr.height 28.0 ]
    |> Control.withKey key

let private button key label : Control<Msg> =
    Button.create [ Button.text label; Button.onClick Clicked; Attr.width 140.0; Attr.height 40.0 ]
    |> Control.withKey key

let private featureTree () : Control<Msg> =
    Stack.create
        [ Attr.gap 0.0
          Stack.children
              [ text "header" "Feature 141"
                Stack.create
                    [ Attr.width 260.0
                      Attr.height 108.0
                      Attr.padding 0.0
                      Stack.children
                          [ row "cache-a" "cached row A"
                            button "go" "Go"
                            Overlay.create [ Overlay.child (text "floating" "Overlay") ] |> Control.withKey "overlay" ] ]
                row "cache-b" "cached row B" ] ]

let private assertEquivalent name (tree: Control<Msg>) =
    let direct = Control.renderTree theme size tree
    let cold = RetainedRender.init theme size tree
    let warm = RetainedRender.step theme size cold.Retained tree

    Expect.equal (visibleShape cold.Render) (visibleShape direct) (name + ": cold retained visible output matches direct")
    Expect.equal (visibleShape warm.Render) (visibleShape direct) (name + ": warm retained visible output matches direct")
    Expect.equal cold.Render.Bounds direct.Bounds (name + ": cold bounds match")
    Expect.equal warm.Render.Bounds direct.Bounds (name + ": warm bounds match")
    Expect.equal cold.Render.Diagnostics direct.Diagnostics (name + ": cold diagnostics match")
    Expect.equal warm.Render.Diagnostics direct.Diagnostics (name + ": warm diagnostics match")
    Expect.equal cold.Render.BoundIds direct.BoundIds (name + ": cold bound ids match")
    Expect.equal warm.Render.BoundIds direct.BoundIds (name + ": warm bound ids match")
    Expect.equal warm.Render.NodeCount direct.NodeCount (name + ": warm node count matches")

let private collectEvidence (root: RetainedNode<Msg>) =
    let rec walk node =
        node.Fragment.InvalidationEvidence @ (node.Children |> List.collect walk)

    walk root

let private repoRoot () =
    let rec walk (dir: DirectoryInfo) =
        if File.Exists(Path.Combine(dir.FullName, "FS.GG.Rendering.slnx")) then
            dir.FullName
        else
            match dir.Parent with
            | null -> Directory.GetCurrentDirectory()
            | parent -> walk parent

    walk (DirectoryInfo(AppContext.BaseDirectory))

let private readRepo path = File.ReadAllText(Path.Combine(repoRoot (), path))

let private generatedTree (rng: Random) (index: int) : Control<Msg> =
    let leaf i =
        if rng.Next(0, 3) = 0 then
            button (sprintf "b-%d-%d" index i) (sprintf "Run %d/%d" index i)
        else
            text (sprintf "t-%d-%d" index i) (sprintf "Text %d/%d" index i)

    let childCount = rng.Next(1, 5)
    let children =
        [ for i in 0 .. childCount - 1 ->
              if rng.Next(0, 4) = 0 then
                  Stack.create
                      [ Attr.width (float (160 + rng.Next(0, 90)))
                        Attr.height (float (60 + rng.Next(0, 60)))
                        Stack.children [ leaf (i * 10); leaf (i * 10 + 1) ] ]
                  |> Control.withKey (sprintf "nested-%d-%d" index i)
              else
                  leaf i ]

    Stack.create
        [ Attr.width (float (320 + rng.Next(0, 120)))
          Attr.height (float (180 + rng.Next(0, 160)))
          Attr.gap (float (rng.Next(0, 6)))
          Stack.children children ]
    |> Control.withKey (sprintf "root-%d" index)

[<Tests>]
let parityTests =
    testList "Feature141 direct/cold/warm retained parity" [
        test "focused nested controls, cache boundaries, overlays, diagnostics, fingerprints, and glyph proof stay equivalent" {
            let tree = featureTree ()
            assertEquivalent "feature fixture" tree

            let init = RetainedRender.init theme size tree
            let warm = RetainedRender.step theme size init.Retained tree

            Expect.equal
                init.Retained.Root.Fragment.Assembly.Fingerprint
                warm.Retained.Root.Fragment.Assembly.Fingerprint
                "retained root stores a stable owner-produced composable assembly fingerprint"

            Expect.equal
                init.Retained.Root.Fragment.Assembly.InFlowFingerprint
                warm.Retained.Root.Fragment.Assembly.InFlowFingerprint
                "retained root stores a stable owner-produced in-flow fingerprint"

            Expect.equal warm.Retained.Root.Fragment.InvalidationEvidence.Head.Decision Reused "warm idle frame records root reuse"
            Expect.equal warm.Retained.Root.Fragment.InvalidationEvidence.Head.Reason StableInputs "warm idle frame records stable inputs"
        }

        test "empty, no-reuse, bounds, diagnostics, events, overlays, cached subtrees, and public result fields remain compatible" {
            assertEquivalent "empty" (Stack.create [ Stack.children [] ])
            assertEquivalent "no-reuse screen" (Stack.create [ Stack.children [ text "plain" "Plain"; button "action" "Action" ] ])
            assertEquivalent "feature fixture" (featureTree ())
        }
    ]

[<Tests>]
let invalidationEvidenceTests =
    testList "Feature141 retained invalidation evidence" [
        test "visual, layout, identity, child-order, insertion, and removal changes are recorded without stale output" {
            let initial =
                Stack.create
                    [ Stack.children [ text "a" "A"; text "b" "B"; button "go" "Go" ] ]

            let init = RetainedRender.init theme size initial

            let changedVisual =
                Stack.create
                    [ Stack.children [ text "a" "A changed"; text "b" "B"; button "go" "Go" ] ]

            let visual = RetainedRender.step theme size init.Retained changedVisual
            Expect.contains (collectEvidence visual.Retained.Root |> List.map _.Reason) VisualInput "visual input changes are recorded"
            Expect.equal (visibleShape visual.Render) (visibleShape (Control.renderTree theme size changedVisual)) "visual change output matches direct"

            let changedLayout =
                Stack.create
                    [ Stack.children [ text "a" "A"; TextBlock.create [ TextBlock.text "B"; Attr.width 260.0; Attr.height 28.0 ] |> Control.withKey "b"; button "go" "Go" ] ]

            let layout = RetainedRender.step theme size init.Retained changedLayout
            Expect.contains (collectEvidence layout.Retained.Root |> List.map _.Reason) LayoutInput "layout input changes are recorded"
            Expect.equal (visibleShape layout.Render) (visibleShape (Control.renderTree theme size changedLayout)) "layout change output matches direct"

            let changedIdentity =
                Stack.create
                    [ Stack.children [ text "a" "A"; text "b" "B"; button "go" "Go" ] ]
                |> Control.withKey "new-root"

            let identity = RetainedRender.step theme size init.Retained changedIdentity
            Expect.contains (collectEvidence identity.Retained.Root |> List.map _.Reason) ExplicitIdentity "explicit identity changes discard/rebuild"

            let reordered =
                Stack.create
                    [ Stack.children [ button "go" "Go"; text "a" "A"; text "b" "B" ] ]

            let reorder = RetainedRender.step theme size init.Retained reordered
            Expect.contains (collectEvidence reorder.Retained.Root |> List.map _.Reason) ChildOrdering "child ordering changes are recorded"
            Expect.equal (visibleShape reorder.Render) (visibleShape (Control.renderTree theme size reordered)) "reordered output matches direct"

            let inserted =
                Stack.create
                    [ Stack.children [ text "a" "A"; text "inserted" "Inserted"; text "b" "B"; button "go" "Go" ] ]

            let insertion = RetainedRender.step theme size init.Retained inserted
            Expect.contains (collectEvidence insertion.Retained.Root |> List.map _.Reason) ChildInsertion "child insertion changes are recorded"

            let removed =
                Stack.create
                    [ Stack.children [ text "a" "A"; button "go" "Go" ] ]

            let removal = RetainedRender.step theme size init.Retained removed
            Expect.contains (collectEvidence removal.Retained.Root |> List.map _.Reason) ChildRemoval "child removal changes are recorded"
            Expect.equal (visibleShape removal.Render) (visibleShape (Control.renderTree theme size removed)) "removed-child output matches direct"
        }

        test "Feature 140 composition evidence is the retained reuse classification source" {
            let chain =
                ([ { Effect = Composition.Offset(4.0, 8.0); Source = Composition.AuthoredModifier }
                   { Effect = Composition.LocalZOrder 2; Source = Composition.AuthoredModifier } ]
                 : Composition.ModifierEntry list)
                |> Composition.normalize

            let evidence = Composition.retainedReuseEvidence chain
            Expect.equal evidence.NormalizedModifierFingerprint (Composition.fingerprint chain.NormalizedEffects) "reuse evidence shares the normalized modifier fingerprint"
            Expect.isTrue evidence.AffectsPaint "offset affects paint invalidation"
            Expect.isTrue evidence.AffectsOrder "local z-order affects order invalidation"
            Expect.isNonEmpty evidence.Reasons "classification reasons are retained for diagnostics"
        }
    ]

[<Tests>]
let randomizedEquivalenceTests =
    testList "Feature141 RetainedRandomizedEquivalence" [
        test "200 deterministic generated trees compare direct, cold retained, and warm retained output" {
            let rng = Random 141

            for i in 1..200 do
                let tree = generatedTree rng i
                assertEquivalent (sprintf "generated case %03d" i) tree
        }
    ]

[<Tests>]
let architectureGuardTests =
    testList "Feature141 one-owner architecture guards" [
        test "RetainedRender stores owner-produced assembly results and does not contain retained-local composition rule sets" {
            let retainedFsi = readRepo "src/Controls/RetainedRender.fsi"
            let retainedFs = readRepo "src/Controls/RetainedRender.fs"

            Expect.isTrue (retainedFsi.Contains("Assembly: ControlInternals.CurrentNodeAssemblyResult")) "RenderFragment stores owner-produced assembly"
            Expect.isTrue (retainedFsi.Contains("RetainedInvalidationEvidence")) "RenderFragment stores invalidation evidence"
            Expect.isFalse (retainedFs.Contains("composeRetainedScenes")) "old retained-local composition helper stays removed"
            Expect.isFalse (retainedFs.Contains("composeContainerScene")) "retained renderer does not call the container composition helper directly"
            Expect.isFalse (retainedFs.Contains("isOverlayNode")) "retained renderer does not branch on overlay semantics directly"
            Expect.isTrue (retainedFs.Contains("ControlInternals.assembleCurrentNode")) "fresh/replay assembly routes through the shared owner"
        }

        test "ControlInternals has exactly one assembly owner and Feature 141 stays inside scope" {
            let control = readRepo "src/Controls/Control.fs"
            let retainedFsi = readRepo "src/Controls/RetainedRender.fsi"
            let outOfScope =
                [ "portable scene serialization"
                  "overlay interaction state"
                  "damage-scissored presentation"
                  "intrinsic layout protocol"
                  "full text shaping" ]

            let ownerCount =
                System.Text.RegularExpressions.Regex.Matches(control, "\\blet assembleCurrentNode\\b").Count

            Expect.equal ownerCount 1 "there is exactly one current-node assembly owner"
            Expect.isTrue (control.Contains("type CurrentNodeAssemblyResult")) "assembly result is owned by ControlInternals"
            Expect.isFalse (retainedFsi.Contains("val .*retained renderer")) "no new public retained renderer API is introduced"

            let tasks = readRepo "specs/141-retained-renderer-unification/tasks.md"
            for term in outOfScope do
                Expect.isTrue (tasks.Contains term) (sprintf "out-of-scope work remains documented: %s" term)
        }
    ]
