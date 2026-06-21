module Feature139AssemblyExtractionTests

open System
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem
open FS.GG.TestSupport

type private Msg =
    | Clicked

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private sceneRect x y w h color =
    Scene.filledRectangle { X = x; Y = y; Width = w; Height = h } color

let private assembly inFlow overlay : ControlInternals.CurrentNodeAssemblyResult =
    { InFlowScene = inFlow
      OverlayScene = overlay
      InFlowFingerprint = ControlInternals.hashScene inFlow
      OverlayFingerprint = ControlInternals.hashScene overlay
      Fingerprint = ControlInternals.hashScene (inFlow @ overlay)
      Diagnostics = []
      ChildContributions = [] }

let private raw kind attrs children : Control<Msg> =
    { Kind = kind
      Key = None
      Attributes = attrs
      Children = children
      Content = None
      Accessibility = None }

let private row key text : Control<Msg> =
    { Kind = "data-grid-row"
      Key = Some key
      Attributes = [ Attr.width 220.0; Attr.height 24.0 ]
      Children = []
      Content = Some text
      Accessibility = None }

let rec private clipNodes (s: Scene) : (Clip * Scene) list =
    s.Nodes |> List.collect clipNodesNode

and private clipNodesNode (n: SceneNode) : (Clip * Scene) list =
    match n with
    | ClipNode(c, inner) -> (c, inner) :: clipNodes inner
    | Group scenes -> scenes |> List.collect clipNodes
    | Translate(_, inner) -> clipNodes inner
    | ColorSpaceNode(_, inner) -> clipNodes inner
    | PerspectiveNode(_, inner) -> clipNodes inner
    | PictureNode p -> clipNodes p.Scene
    | CachedSubtree b -> clipNodes b.Scene
    | _ -> []

let private rectClipRects (s: Scene) : Rect list =
    clipNodes s
    |> List.choose (fun (c, _) ->
        match c with
        | RectClip r -> Some r
        | _ -> None)

let private rectClose (a: Rect) (b: Rect) =
    abs (a.X - b.X) < 0.5
    && abs (a.Y - b.Y) < 0.5
    && abs (a.Width - b.Width) < 0.5
    && abs (a.Height - b.Height) < 0.5

let rec private flattenScene (s: Scene) : SceneNode list =
    s.Nodes |> List.collect flattenNode

and private flattenNode (n: SceneNode) : SceneNode list =
    match n with
    | CachedSubtree b -> flattenScene b.Scene
    | Group scenes -> scenes |> List.collect flattenScene
    | ClipNode(c, inner) -> [ ClipNode(c, { Nodes = flattenScene inner }) ]
    | Translate(o, inner) -> [ Translate(o, { Nodes = flattenScene inner }) ]
    | ColorSpaceNode(c, inner) -> [ ColorSpaceNode(c, { Nodes = flattenScene inner }) ]
    | PerspectiveNode(t, inner) -> [ PerspectiveNode(t, { Nodes = flattenScene inner }) ]
    | PictureNode p -> [ PictureNode { p with Scene = { Nodes = flattenScene p.Scene } } ]
    | other -> [ other ]

let private visibleShape (result: ControlRenderResult<Msg>) = flattenScene result.Scene

let private featureTree () : Control<Msg> =
    Stack.create
        [ Attr.gap 0.0
          Stack.children
              [ TextBlock.create [ TextBlock.text "header" ]
                Stack.create
                    [ Attr.width 160.0
                      Attr.height 84.0
                      Attr.margin 12.0
                      Attr.padding 0.0
                      Attr.gap 0.0
                      Stack.children
                          [ row "cache-a" "cached row A"
                            raw "stack" [ Attr.width 260.0; Attr.height 36.0 ] [ TextBlock.create [ TextBlock.text "wide clipped child" ] ]
                            Overlay.create [ Overlay.child (TextBlock.create [ TextBlock.text "floating overlay" ]) ]
                            |> Control.withKey "feature139-overlay" ] ]
                row "cache-b" "cached row B" ] ]

let private eventTree () : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text "Go"; Button.onClick Clicked ] |> Control.withKey "go"
                TextBlock.create [] |> Control.withKey "missing-text" ] ]

let private eventSample =
    { Kind = "click"
      ControlId = Some "go"
      Origin = ControlEventOrigin.Pointer
      Payload = None
      Nav = None }

let private bindingShape (result: ControlRenderResult<Msg>) =
    result.EventBindings
    |> List.map (fun binding -> binding.ControlId, binding.EventKind, binding.Dispatch eventSample)

let private assertRetainedMatches (name: string) (tree: Control<Msg>) =
    let full = Control.renderTree theme size tree
    let inited = RetainedRender.init theme size tree
    let warm = RetainedRender.step theme size inited.Retained tree

    Expect.equal (visibleShape inited.Render) (visibleShape full) (name + ": retained init visible scene equals immediate renderTree")
    Expect.equal (visibleShape warm.Render) (visibleShape full) (name + ": warm retained visible scene equals immediate renderTree")
    Expect.equal inited.Render.Bounds full.Bounds (name + ": retained init bounds match")
    Expect.equal warm.Render.Bounds full.Bounds (name + ": warm retained bounds match")
    Expect.equal inited.Render.Diagnostics full.Diagnostics (name + ": retained init diagnostics match")
    Expect.equal warm.Render.Diagnostics full.Diagnostics (name + ": warm retained diagnostics match")
    Expect.equal (bindingShape inited.Render) (bindingShape full) (name + ": retained init event bindings match")
    Expect.equal (bindingShape warm.Render) (bindingShape full) (name + ": warm retained event bindings match")
    Expect.equal inited.Render.BoundIds full.BoundIds (name + ": retained init bound ids match")
    Expect.equal warm.Render.BoundIds full.BoundIds (name + ": warm retained bound ids match")
    Expect.equal warm.Render.NodeCount full.NodeCount (name + ": retained node count matches")

let private repoRoot () = RepositoryRoot.value

let private readRepo path = File.ReadAllText(Path.Combine(repoRoot (), path))

[<Tests>]
let directAssemblyContract =
    testList "Feature139 direct current-node assembly contract" [
        test "leaf and box-less nodes compose flat" {
            let box = { X = 10.0; Y = 20.0; Width = 100.0; Height = 50.0 }
            let own = [ sceneRect 10.0 20.0 100.0 50.0 Colors.black ]
            let child = [ sceneRect 0.0 0.0 20.0 20.0 Colors.white ]
            let plain = raw "stack" [] []

            let leaf = ControlInternals.assembleCurrentNode plain (Some box) own []
            Expect.equal leaf.InFlowScene own "a leaf keeps only its own scene in flow"
            Expect.isEmpty leaf.OverlayScene "a non-overlay leaf has no overlay contribution"

            let boxless = ControlInternals.assembleCurrentNode plain None own [ assembly child [] ]
            Expect.equal boxless.InFlowScene (own @ child) "a box-less node composes own and child scenes flat"
            Expect.isEmpty (rectClipRects (Scene.group boxless.InFlowScene)) "box-less composition introduces no clip"
        }

        test "children are clipped to a node box when a box and child scene are present" {
            let box = { X = 10.0; Y = 20.0; Width = 100.0; Height = 50.0 }
            let own = [ sceneRect 10.0 20.0 100.0 50.0 Colors.black ]
            let child = [ sceneRect 0.0 0.0 400.0 400.0 Colors.white ]
            let plain = raw "stack" [] []

            let result = ControlInternals.assembleCurrentNode plain (Some box) own [ assembly child [] ]
            let clips = rectClipRects (Scene.group result.InFlowScene)
            Expect.equal clips.Length 1 "one container clip wraps the assembled child in-flow scene"
            Expect.isTrue (rectClose (List.head clips) box) "the clip rect is the current node box"
        }

        test "overlay nodes promote composed own and child in-flow paint to the overlay group" {
            let box = { X = 5.0; Y = 6.0; Width = 70.0; Height = 30.0 }
            let own = [ sceneRect 5.0 6.0 70.0 30.0 Colors.black ]
            let child = [ sceneRect 6.0 7.0 20.0 10.0 Colors.white ]
            let deeperOverlay = [ sceneRect 7.0 8.0 12.0 6.0 (Colors.rgb 185uy 28uy 28uy) ]
            let overlay = raw "overlay" [] []

            let result = ControlInternals.assembleCurrentNode overlay (Some box) own [ assembly child deeperOverlay ]
            Expect.isEmpty result.InFlowScene "an overlay node contributes no in-flow scene"
            Expect.equal result.OverlayScene (ControlInternals.composeContainerScene (Some box) own child @ deeperOverlay) "the overlay contribution preserves composed subtree before deeper overlays"
        }
    ]

[<Tests>]
let parityAndCompatibility =
    testList "Feature139 immediate-retained parity and compatibility" [
        test "nested clipping, offsets, cache boundaries, overlays, and warm reuse stay byte-identical" {
            let tree = featureTree ()
            let full = Control.renderTree theme size tree
            let inited = RetainedRender.init theme size tree
            let warm = RetainedRender.step theme size inited.Retained tree

            Expect.equal (visibleShape inited.Render) (visibleShape full) "retained init matches immediate renderTree"
            Expect.equal (visibleShape warm.Render) (visibleShape full) "warm retained step matches immediate renderTree through transparent cache wrappers"
            Expect.isNonEmpty (rectClipRects full.Scene) "the fixture exercises container clipping"
            Expect.equal warm.WorkReduction.RecomputedNodeCount 0 "an idle warm step does not repaint an unchanged tree"
            Expect.equal warm.WorkReduction.RemeasuredNodeCount 0 "an idle warm step does not remeasure an unchanged tree"
            Expect.isLessThan warm.WorkReduction.RecomputedNodeCount warm.WorkReduction.BaselineNodeCount "warm reuse avoids a full-tree paint pass"
            Expect.isTrue (warm.WorkReduction.PictureCacheHits > 0) "the fixture exercises retained picture cache boundaries"
        }

        test "empty, overlay-free, overlay, clipped, bounds, diagnostics, events, and bound ids remain compatible" {
            assertRetainedMatches "empty" (Stack.create [ Stack.children [] ])
            assertRetainedMatches "overlay-free" (Stack.create [ Stack.children [ TextBlock.create [ TextBlock.text "plain" ]; Button.create [ Button.text "Go"; Button.onClick Clicked ] ] ])
            assertRetainedMatches "overlay" (featureTree ())
            assertRetainedMatches "diagnostics-events-boundids" (eventTree ())
        }
    ]

[<Tests>]
let ownershipAndArchitecture =
    testList "Feature139 assembly ownership evidence" [
        test "retained rendering does not reimplement container clipping plus overlay splitting" {
            let retained = readRepo "src/Controls/RetainedRender.fs"
            Expect.isFalse (retained.Contains("composeContainerScene")) "RetainedRender must not call composeContainerScene directly"
            Expect.isFalse (retained.Contains("isOverlayNode")) "RetainedRender must not branch on overlay nodes directly"
            Expect.isFalse (retained.Contains("composeRetainedScenes")) "the old retained-local assembly owner must stay removed"
            Expect.isTrue (retained.Contains("assembleRetainedNode")) "retained build sites route through the local adapter to assembleCurrentNode"
            Expect.isTrue (retained.Contains("ControlInternals.assembleCurrentNode")) "retained emit/build code calls the shared assembly seam"
        }

        test "the shared owner and scope fence are documented for later phases" {
            let control = readRepo "src/Controls/Control.fs"
            let quickstart = readRepo "specs/139-shared-assembly-extraction/quickstart.md"
            let forbidden =
                [ "modifier algebra"
                  "portals"
                  "public IR changes"
                  "intrinsic layout"
                  "text shaping"
                  "compositor"
                  "portable protocol" ]

            Expect.isTrue (control.Contains("let assembleCurrentNode")) "ControlInternals owns the current-node assembly seam"
            Expect.isTrue (control.Contains("R1a")) "the seam comments identify the R1a scope"

            for term in forbidden do
                Expect.isTrue (quickstart.Contains term) (sprintf "quickstart records the later-phase exclusion: %s" term)
        }
    ]
