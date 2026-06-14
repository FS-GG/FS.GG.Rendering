module ProductBehaviorTests

open System
open Expecto
open Product.Program
open Product.Model
open FS.Skia.UI.Scene

// Feature 060 (FR-005): replaceable scaffold-BEHAVIOR tests. These call the scaffold
// product's `view`/`update`/host/scene-text directly, so when you replace the scaffold
// model with your own you rewrite THIS file. `GovernanceTests.fs` (compiled first) keeps
// its model-agnostic source/structure/evidence scans green across that swap.

let rec collectSceneNodes node =
    seq {
        yield node
        match node with
        | Group scenes ->
            for scene in scenes do
                for child in scene.Nodes do
                    yield! collectSceneNodes child
        | ClipNode(_, scene)
        | ColorSpaceNode(_, scene)
        | PerspectiveNode(_, scene) ->
            for child in scene.Nodes do
                yield! collectSceneNodes child
        | PictureNode picture ->
            for child in picture.Scene.Nodes do
                yield! collectSceneNodes child
        | _ -> ()
    }

let sceneText node =
    collectSceneNodes node
    |> Seq.choose (function Text(_, value, _) -> Some value | TextRun run -> Some run.Text | _ -> None)
    |> String.concat " "

//#if (profile == "governed" || profile == "headless-scene")
[<Tests>]
let behaviorTests =
    testList "product-behavior" [
        test "generated headless product exposes scene contract" {
            let scene: FS.Skia.UI.Scene.Scene = { Nodes = [ Product.Program.view initialModel ] }
            let text = scene.Nodes |> List.map sceneText |> String.concat " "
            let updated, effects = Product.Program.update Rendered initialModel

            Expect.isNonEmpty scene.Nodes "Product.Program.view returns a scene"
            Expect.stringContains text "Governed headless scene" "headless view renders scene text"
            Expect.equal updated.RenderCount 1 "headless update is callable"
            Expect.isEmpty effects "headless update has no host effects"
        }

        test "generated headless layout evidence is readable" {
            let report = Product.Program.layoutEvidenceForSize { Width = 640; Height = 480 } initialModel

            Expect.equal report.ProofLevel ReadableLayout "headless layout report proves readable layout"
            Expect.isSome report.HudRegion "headless layout report has a named summary region"
            Expect.isSome report.GameplayRegion "headless layout report has a named content region"
            Expect.isNonEmpty report.TextBounds "headless layout report has text bounds"
            Expect.isNonEmpty report.GameplayBounds "headless layout report has scene content bounds"
            Expect.equal report.OverlapStatus NoLayoutOverlap "headless layout report has no overlaps"
        }

        //#if (profile == "governed")
        test "generated governed profile validates layout through Testing helpers" {
            let report = Product.Program.layoutEvidenceForSize { Width = 640; Height = 480 } initialModel
            let result =
                FS.Skia.UI.Testing.GeneratedLayoutValidation.validate
                    { Report = report
                      RequireReadableLayout = true }

            Expect.isTrue result.Accepted "governed profile can validate generated layout evidence"
            Expect.equal result.FailureClass None "accepted governed layout has no failure class"
        }
        //#endif
    ]
//#else
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.SkiaViewer

[<Tests>]
let behaviorTests =
    testList "product-behavior" [
        test "generated product test suite is wired" {
            Expect.equal 1 1 "product tests run"
        }

        test "generated public contract exposes qualified app-owned names" {
            let scene: FS.Skia.UI.Scene.Scene = { Nodes = [ Product.Program.view initialModel ] }
            let host = Product.Program.generatedHost
            let updated, _ = Product.Program.update NoOp initialModel

            Expect.isNonEmpty scene.Nodes "Product.Program.view returns a scene"
            Expect.equal updated initialModel "Product.Program.update is callable as the app reducer"
            Expect.isSome (host.MapKey Enter true) "Product.Program.generatedHost exposes viewer input mapping"
        }

        test "product-owned controls example is wired" {
            let view = controlsExampleView initialModel
            Expect.isGreaterThan (Control.count view) 7 "product example owns form, rich text, chart, graph, and DataGrid controls"
        }

        test "product-owned form chart and DataGrid controls are constructible" {
            let textBox =
                TextBox.create [
                    TextBox.value initialModel.Name
                    TextBox.onChanged NameChanged
                ]

            let lineChart = LineChart.create [ LineChart.series initialModel.Revenue ]
            let dataGrid = DataGrid.create initialModel.GridColumns [ DataGrid.rows initialModel.GridRows ]

            Expect.isGreaterThan (Control.count textBox) 0 "TextBox product example is constructible"
            Expect.isGreaterThan (Control.count lineChart) 0 "LineChart product example is constructible"
            Expect.isGreaterThan (Control.count dataGrid) 0 "DataGrid product example is constructible"
        }

        test "generated product adapter program is product-owned" {
            let model, initCommands = adapterProgram.Init()
            let updated, saveCommands = adapterProgram.Update SaveRequested model
            let view = adapterProgram.View updated
            let subscriptions = adapterProgram.Subscriptions updated

            Expect.isEmpty initCommands "adapter init starts without host commands"
            Expect.isNonEmpty saveCommands "save emits product-owned adapter command"
            Expect.isEmpty subscriptions "default generated product has no subscriptions"
            Expect.isGreaterThan (Control.count view) 7 "adapter view returns Controls"
        }

        // FR-003 / SC-002: the unmodified default `view` renders the REAL example controls
        // through the production tree-render path (`Control.renderTree`), not hand-drawn
        // placeholder geometry. The rendered scene therefore carries the example control text.
        test "default view renders real controls through the production render path" {
            let rendered = view initialModel
            let nodes = collectSceneNodes rendered |> Seq.toList
            let text = sceneText rendered

            Expect.isGreaterThan (List.length nodes) (Control.count (controlsExampleView initialModel)) "renderTree paints nested controls (more nodes than the control count)"
            Expect.stringContains text "Product controls" "the rendered scene shows the example TextBlock's real text"
            Expect.stringContains text "Save" "the rendered scene shows the example Button's real label"
        }

        // SC-002 corollary: a NESTED-control change is reflected in the rendered scene, proving
        // the real control tree (not a fixed placeholder) drives the view.
        test "default view reflects the control tree (nested change changes the scene)" {
            let before = view initialModel
            let after = view { initialModel with Name = "Renamed" }
            Expect.notEqual before after "the TextBox value flows through renderTree into the scene"
        }

        //#if (profile == "app")
        // SC-003 (FR-004): a synthetic pointer press+release at a live control's bounds, routed
        // through the EXACT step runInteractiveApp wires (ControlsElmish.routeInteractivePointer),
        // dispatches that control's bound message — proving the pointer host is interactive.
        test "pointer click on the Save control routes its bound message (SC-003)" {
            let host = Product.Program.interactiveHost
            let size: FS.Skia.UI.Scene.Size = { Width = 640; Height = 480 }
            let model0 = fst (host.Init())
            let rendered = Control.renderTree host.Theme size (host.View size model0)

            // Resolve the "save" control's evaluated box via the layout engine (the same path
            // runInteractiveApp hit-tests), then click its centre.
            let available: FS.Skia.UI.Layout.AvailableSpace =
                { Width = float size.Width
                  WidthMode = FS.Skia.UI.Layout.Exactly
                  Height = float size.Height
                  HeightMode = FS.Skia.UI.Layout.Exactly }

            let layoutResult = FS.Skia.UI.Layout.Layout.evaluate available rendered.Layout
            let saveBox = (layoutResult.Bounds |> List.find (fun b -> b.NodeId = "save")).Bounds
            let cx = saveBox.X + saveBox.Width / 2.0
            let cy = saveBox.Y + saveBox.Height / 2.0

            let pointer phase x y : ViewerPointerInput =
                { Phase = phase; X = x; Y = y; Button = Some ViewerPointerButtonKind.Primary; DeltaX = 0.0; DeltaY = 0.0 }

            let state1, downMsgs =
                ControlsElmish.routeInteractivePointer host (Pointer.init ()) size model0 (pointer ViewerPointerPhaseKind.Pressed cx cy)

            let _state2, upMsgs =
                ControlsElmish.routeInteractivePointer host state1 size model0 (pointer ViewerPointerPhaseKind.Released cx cy)

            let routed = downMsgs @ upMsgs
            Expect.contains routed SaveRequested "press+release on the Save control dispatches its bound SaveRequested message"

            let _, effects =
                routed |> List.fold (fun (m, fx) msg -> let m', fx' = host.Update msg m in m', fx @ fx') (model0, [])

            Expect.isNonEmpty effects "the routed control message produces a host effect"
        }
        //#endif

        test "generated graphical app navigates pages through viewer key events" {
            let browse, _ =
                dispatchViewerKey { RawKey = "Enter"; Direction = ViewerKeyDirection.KeyDown } initialModel

            Expect.equal browse.Page Browse "Home opens Browse from viewer Enter"
            Expect.equal browse.LastInput (Some Enter) "normalized input is stored"
            Expect.exists browse.InputDiagnostics (fun item -> item.Flow = "home-open" && item.RawKey = Some "Enter") "diagnostic names the viewer input flow"
        }

        test "generated app settings, detail-back, and restart flows use viewer keys" {
            let settings, _ =
                dispatchViewerKey { RawKey = "S"; Direction = ViewerKeyDirection.KeyDown } initialModel

            let browse, _ =
                dispatchViewerKey { RawKey = "Return"; Direction = ViewerKeyDirection.KeyDown } settings

            let detail, _ =
                dispatchViewerKey { RawKey = "Enter"; Direction = ViewerKeyDirection.KeyDown } browse

            let backToBrowse, _ =
                dispatchViewerKey { RawKey = "Esc"; Direction = ViewerKeyDirection.KeyDown } detail

            let summary, _ = Product.Program.update (Navigated Summary) backToBrowse

            let restarted, _ =
                dispatchViewerKey { RawKey = "Enter"; Direction = ViewerKeyDirection.KeyDown } summary

            Expect.equal settings.Page Settings "settings page opens through viewer key"
            Expect.equal browse.Page Browse "settings apply enters browse page"
            Expect.equal detail.Page Detail "enter opens the detail page"
            Expect.equal backToBrowse.Page Browse "escape returns from detail to browse"
            Expect.equal restarted.Page Home "summary page restarts through viewer Enter"
        }

        test "pure generated app transitions expose model message and effect behavior" {
            let started, startEffects = Product.Program.update (ViewerInput(Enter, true)) initialModel
            let interacted, interactionEffects = Product.Program.update (ViewerInput(ArrowLeft, true)) started

            Expect.equal started.Page Browse "pure update opens the browse page"
            Expect.isEmpty startEffects "input transition has no host command"
            Expect.equal interacted.Interactions 1 "content-region interaction is counted"
            Expect.isEmpty interactionEffects "content interaction has no host command"
        }

        test "generated host boundary keeps app commands separate from viewer effects" {
            let unchanged, appCommands = Product.Program.update SaveRequested initialModel
            let hosted, observedAppCommands, viewerEffects = Product.Program.interpretAtHostBoundary SaveRequested initialModel
            let hostUpdated, hostViewerEffects = Product.Program.generatedHost.Update SaveRequested initialModel

            Expect.equal unchanged initialModel "save command does not mutate the app model"
            Expect.equal hosted initialModel "host boundary preserves pure update result"
            Expect.equal hostUpdated initialModel "generated host uses the same pure update result"
            Expect.exists appCommands (function DispatchHostCommand "save:Product" -> true | _ -> false) "pure update emits an app command"
            Expect.equal observedAppCommands appCommands "host boundary exposes app commands before interpretation"
            Expect.exists (observedAppCommands |> List.map Product.Program.appCommandName) ((=) "app-command:dispatch-host-command:save:Product") "app command category is named separately"
            Expect.exists viewerEffects (function RenderScene _ -> true | _ -> false) "host boundary emits viewer render effect separately"
            Expect.equal hostViewerEffects.Length viewerEffects.Length "generated host returns the same number of viewer effects to SkiaViewer"
            Expect.exists hostViewerEffects (function RenderScene _ -> true | _ -> false) "generated host returns render effects to SkiaViewer"
        }

        test "generated layout evidence separates summary and content regions at default and constrained sizes" {
            let defaultReport = Product.Program.layoutEvidenceForSize { Width = 1280; Height = 720 } initialModel
            let constrainedReport = Product.Program.layoutEvidenceForSize { Width = 640; Height = 480 } initialModel

            [ defaultReport; constrainedReport ]
            |> List.iter (fun report ->
                Expect.equal report.ProofLevel ReadableLayout "generated report proves readable layout"
                Expect.isSome report.HudRegion "summary region is named"
                Expect.isSome report.GameplayRegion "content region is named"
                Expect.isNonEmpty report.TextBounds "summary text bounds are present"
                Expect.isNonEmpty report.GameplayBounds "active content bounds are present"
                Expect.equal report.OverlapStatus NoLayoutOverlap "summary and content bounds do not overlap"
                Expect.equal report.MeasurementMode ApproximateTextBounds "generated layout evidence reports the measurement mode"
                Expect.isEmpty report.UnsupportedReasons "readable generated layout does not use unsupported-host classification")
        }

        test "generated layout validation fails broken summary and content layouts" {
            let summaryOverlap = Product.Program.layoutEvidenceForSize { Width = 480; Height = 480 } initialModel
            let contentOverlap =
                Product.Program.layoutEvidenceForSize
                    { Width = 640; Height = 480 }
                    { initialModel with ContentRow = -6 }

            let summaryResult = Product.Program.validateGeneratedLayout summaryOverlap
            let contentResult = Product.Program.validateGeneratedLayout contentOverlap

            Expect.isFalse summaryResult.Accepted "summary/summary overlap fails validation"
            Expect.equal summaryResult.FailureClass (Some OverlappingLayoutBounds) "summary overlap is classified"
            Expect.isFalse contentResult.Accepted "summary/content overlap fails validation"
            Expect.equal contentResult.FailureClass (Some OverlappingLayoutBounds) "summary/content overlap is classified"
        }

        test "generated content policies use the content region for the active item and bounds" {
            let started, _ = Product.Program.update (ViewerInput(Enter, true)) initialModel
            let moved, _ = Product.Program.update (ViewerInput(ArrowRight, true)) started
            let ticked, _ = Product.Program.update Tick moved

            let region = Product.Program.gameplayRegionForSize { Width = 640; Height = 480 }
            let bounds = Product.Program.activeGameplayBoundsForSize { Width = 640; Height = 480 } ticked

            Expect.isTrue (Product.Program.boundsInside region.Bounds bounds.Bounds) "active item remains inside the content region"
            Expect.isTrue (Product.Program.movementUsesGameplayRegion { Width = 640; Height = 480 } ticked) "movement policy is region based"
            Expect.isTrue (Product.Program.spawnUsesGameplayRegion { Width = 640; Height = 480 } initialModel) "spawn policy is region based"
            Expect.isTrue (Product.Program.collisionUsesGameplayRegion { Width = 640; Height = 480 } ticked) "collision policy is region based"
        }

        test "generated default app dispatches input, advances over time, and keeps evidence flags opt-in" {
            let started, _ = dispatchViewerKey { RawKey = "Enter"; Direction = ViewerKeyDirection.KeyDown } initialModel
            let moved, _ = dispatchViewerKey { RawKey = "ArrowRight"; Direction = ViewerKeyDirection.KeyDown } started

            Expect.notEqual moved initialModel "keyboard input changes application state"
            Expect.isGreaterThan moved.Interactions started.Interactions "right input is reflected in content state"

            match tick (TimeSpan.FromMilliseconds 500.0) with
            | Some tickMsg ->
                let afterTick, _ = Product.Program.update tickMsg moved
                Expect.notEqual afterTick moved "time-based tick advances application state"
            | None -> failtest "generated tick must advance application state over time"

            let source = System.IO.File.ReadAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "Product", "Program.fs"))
            let defaultBranch = source.Substring(source.LastIndexOf("| None ->", StringComparison.Ordinal))
            // FR-005 (086): per-family persistent interactive host in the default launch.
            //#if (profile == "app")
            Expect.stringContains defaultBranch "ControlsElmish.runInteractiveApp viewerOptions interactiveHost" "controls-family normal launch uses the pointer-aware persistent host"
            //#else
            Expect.stringContains defaultBranch "Viewer.runApp viewerOptions generatedHost" "game-family normal launch uses the keyboard-only persistent host"
            //#endif
            Expect.isFalse (defaultBranch.Contains("--launch-evidence")) "launch evidence flag stays out of normal launch branch"
            Expect.isFalse (defaultBranch.Contains("--bounded-smoke")) "bounded smoke flag stays out of normal launch branch"
            Expect.isFalse (defaultBranch.Contains("self-closed-for-evidence=true")) "normal launch does not report evidence self-close"
        }
    ]
//#endif
