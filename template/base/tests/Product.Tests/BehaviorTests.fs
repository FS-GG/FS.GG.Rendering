module AppRootBehaviorTests

open System
open Expecto
open AppRoot.Program
open AppRoot.Model
open FS.GG.UI.Scene

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
            let scene: FS.GG.UI.Scene.Scene = { Nodes = [ AppRoot.Program.view initialModel ] }
            let text = scene.Nodes |> List.map sceneText |> String.concat " "
            let updated, effects = AppRoot.Program.update Rendered initialModel

            Expect.isNonEmpty scene.Nodes "AppRoot.Program.view returns a scene"
            Expect.stringContains text "Governed headless scene" "headless view renders scene text"
            Expect.equal updated.RenderCount 1 "headless update is callable"
            Expect.isEmpty effects "headless update has no host effects"
        }

        test "generated headless layout evidence is readable" {
            let report = AppRoot.Program.layoutEvidenceForSize { Width = 640; Height = 480 } initialModel

            Expect.equal report.ProofLevel ReadableLayout "headless layout report proves readable layout"
            Expect.isSome report.HudRegion "headless layout report has a named summary region"
            Expect.isSome report.GameplayRegion "headless layout report has a named content region"
            Expect.isNonEmpty report.TextBounds "headless layout report has text bounds"
            Expect.isNonEmpty report.GameplayBounds "headless layout report has scene content bounds"
            Expect.equal report.OverlapStatus NoLayoutOverlap "headless layout report has no overlaps"
        }

        //#if (profile == "governed")
        test "generated governed profile validates layout through Testing helpers" {
            let report = AppRoot.Program.layoutEvidenceForSize { Width = 640; Height = 480 } initialModel
            let result =
                FS.GG.UI.Testing.GeneratedLayoutValidation.validate
                    { Report = report
                      RequireReadableLayout = true }

            Expect.isTrue result.Accepted "governed profile can validate generated layout evidence"
            Expect.equal result.FailureClass None "accepted governed layout has no failure class"
        }
        //#endif
    ]
//#else
//#if (profile == "game")
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open AppRoot.Geometry // Vec2 (Vx/Vy) — the collision-safe positions the starter uses (feature 250)

// One fixed sim step's worth of elapsed time — enough for `update (Tick oneStep)` to drain a step.
let private oneStep = 1.0 / 60.0

// GAME family (feature 220): replaceable scaffold-behaviour tests. These drive the Pong
// skeleton's update/view/tick/host directly, so when you swap in your own game you rewrite THIS
// file. GovernanceTests.fs stays model-agnostic and keeps passing across the swap (SC-004).
[<Tests>]
let behaviorTests =
    testList "product-behavior" [
        test "generated product test suite is wired" {
            Expect.equal 1 1 "product tests run"
        }

        test "game default view renders the playfield, ball, paddles and score as a scene" {
            let scene = AppRoot.Program.view AppRoot.Program.initialModel
            let nodes = collectSceneNodes scene |> Seq.toList
            let text = sceneText scene

            Expect.isTrue (List.length nodes >= 5) "game view draws the playfield, ball, two paddles and a score HUD"
            Expect.stringContains text "0 : 0" "the unmodified default renders the served 0:0 score"
        }

        test "game tick advances the ball and the tick count (the default is a live, moving product)" {
            let before = AppRoot.Program.initialModel
            let after, effects = AppRoot.Program.update (Tick oneStep) before

            Expect.notEqual after.Ball before.Ball "a tick integrates the ball position"
            Expect.equal after.TickCount (before.TickCount + 1) "a tick advances the tick count"
            Expect.isEmpty effects "a pure game tick emits no host command"
        }

        test "game keyboard input moves the paddles and records the last input" {
            let model0 = AppRoot.Program.initialModel
            let leftUp, _ = AppRoot.Program.update (ViewerInput(Letter 'W', true)) model0
            let rightDown, _ = AppRoot.Program.update (ViewerInput(ArrowDown, true)) model0

            Expect.isLessThan leftUp.LeftPaddleY model0.LeftPaddleY "W moves the left paddle up"
            Expect.isGreaterThan rightDown.RightPaddleY model0.RightPaddleY "Down moves the right paddle down"
            Expect.equal rightDown.LastInput (Some ArrowDown) "the last input key is recorded"
        }

        test "game MovePaddle clamps paddles inside the playfield" {
            let model0 = AppRoot.Program.initialModel

            let raised =
                List.replicate 100 (MovePaddle(LeftSide, PaddleUp))
                |> List.fold (fun m msg -> fst (AppRoot.Program.update msg m)) model0

            Expect.isTrue (raised.LeftPaddleY >= 0.0) "the paddle never leaves the top of the playfield"
        }

        test "game scores and re-serves when the ball passes an undefended edge" {
            let model0 = AppRoot.Program.initialModel

            let missed =
                { model0 with
                    Ball = { Pos = vec2 18.0 10.0; Velocity = vec2 -8.0 model0.Ball.Velocity.Vy }
                    LeftPaddleY = 300.0 }

            let scored, _ = AppRoot.Program.update (Tick oneStep) missed

            Expect.equal scored.RightScore 1 "the right side scores when the ball passes the left edge"
            Expect.equal scored.Ball.Pos.Vx (model0.Playfield.Vx / 2.0) "the ball re-serves to the centre"
        }

        test "generated game host exposes viewer input and tick mapping and advances the game" {
            let host = AppRoot.Program.generatedHost
            let model0 = fst (host.Init())

            Expect.isSome (host.MapKey ArrowUp true) "generatedHost maps viewer keys to messages"
            Expect.isSome (host.Tick (TimeSpan.FromMilliseconds 16.0)) "generatedHost ticks at >=16ms"

            let updated, effects = host.Update (Tick oneStep) model0
            Expect.notEqual updated model0 "generatedHost.Update advances the game"
            Expect.isNonEmpty effects "generatedHost returns a render effect to SkiaViewer"
        }

        test "generated game host boundary keeps app commands separate from viewer effects" {
            let model0 = AppRoot.Program.initialModel
            let hosted, appCommands, viewerEffects = AppRoot.Program.interpretAtHostBoundary (Tick oneStep) model0

            Expect.notEqual hosted model0 "host boundary applies the pure update result"
            Expect.isEmpty appCommands "the game tick produces no app command"
            Expect.exists viewerEffects (function RenderScene _ -> true | _ -> false) "host boundary emits a render effect separately"
        }

        test "game layout evidence re-points HUD onto the score strip and gameplay onto the playfield" {
            let report = AppRoot.Program.layoutEvidenceForSize { Width = 640; Height = 480 } AppRoot.Program.initialModel

            Expect.equal report.ProofLevel ReadableLayout "game layout report proves readable layout"
            Expect.isSome report.HudRegion "score region is named"
            Expect.isSome report.GameplayRegion "playfield region is named"
            Expect.isNonEmpty report.TextBounds "score text bounds are present"
            Expect.isNonEmpty report.GameplayBounds "active ball bounds are present"
            Expect.equal report.OverlapStatus NoLayoutOverlap "score and playfield bounds do not overlap"
        }

        test "game active item (the ball) stays inside the playfield region" {
            let size: FS.GG.UI.Scene.Size = { Width = 640; Height = 480 }
            let model0 = AppRoot.Program.initialModel
            let ticked = List.replicate 30 (Tick oneStep) |> List.fold (fun m msg -> fst (AppRoot.Program.update msg m)) model0

            let region = AppRoot.Program.gameplayRegionForSize size
            let bounds = AppRoot.Program.activeGameplayBoundsForSize size ticked

            Expect.isTrue (AppRoot.Program.boundsInside region.Bounds bounds.Bounds) "the ball stays inside the playfield region"
            Expect.isTrue (AppRoot.Program.movementUsesGameplayRegion size ticked) "movement policy is region based"
            Expect.isTrue (AppRoot.Program.spawnUsesGameplayRegion size model0) "spawn policy is region based"
            Expect.isTrue (AppRoot.Program.collisionUsesGameplayRegion size ticked) "collision policy is region based"
        }

        test "game layout validation accepts a readable report and rejects a factless one" {
            let good = AppRoot.Program.layoutEvidenceForSize { Width = 640; Height = 480 } AppRoot.Program.initialModel
            let goodResult = AppRoot.Program.validateGeneratedLayout good
            let broken = { good with HudRegion = None; GameplayBounds = [] }
            let brokenResult = AppRoot.Program.validateGeneratedLayout broken

            Expect.isTrue goodResult.Accepted "a readable game layout validates"
            Expect.isFalse brokenResult.Accepted "a layout missing facts is rejected"
            Expect.equal brokenResult.FailureClass (Some MissingLayoutFacts) "missing facts are classified"
        }

        test "generated default game dispatches input, advances over time, and keeps evidence flags opt-in" {
            let model0 = AppRoot.Program.initialModel
            let moved, _ = AppRoot.Program.update (ViewerInput(ArrowUp, true)) model0
            Expect.notEqual moved model0 "keyboard input changes game state"

            match AppRoot.Program.tick (TimeSpan.FromMilliseconds 500.0) with
            | Some tickMsg ->
                let afterTick, _ = AppRoot.Program.update tickMsg moved
                Expect.notEqual afterTick moved "time-based tick advances game state"
            | None -> failtest "generated tick must advance game state over time"

            let source = System.IO.File.ReadAllText(System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "Product", "Program.fs"))
            let defaultBranch = source.Substring(source.LastIndexOf("| None ->", StringComparison.Ordinal))
            Expect.stringContains defaultBranch "Viewer.runApp viewerOptions generatedHost" "game-family normal launch uses the keyboard-only persistent host"
            Expect.isFalse (defaultBranch.Contains("--launch-evidence")) "launch evidence flag stays out of normal launch branch"
            Expect.isFalse (defaultBranch.Contains("--bounded-smoke")) "bounded smoke flag stays out of normal launch branch"
            Expect.isFalse (defaultBranch.Contains("self-closed-for-evidence=true")) "normal launch does not report evidence self-close"
        }

        // #136 (epic #134): the --window-diagnostics probe must agree with the real launch. Its
        // verdict is derived from the SAME gate Viewer.runApp consults (Viewer.runtimeCapability()),
        // so the reported live-window capability equals that gate and it never fabricates an observed
        // window failure — the self-report/reality mismatch the reporter hit.
        test "window diagnostics verdict matches the real runtime gate (#136 / epic #134)" {
            let dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "window-diagnostics-" + System.Guid.NewGuid().ToString("N"))
            let path = System.IO.Path.Combine(dir, "window-diagnostics.txt")
            let exitCode = AppRoot.EvidenceCommands.windowDiagnostics path
            let output = System.IO.File.ReadAllText path
            System.IO.Directory.Delete(dir, true)

            let capability = Viewer.runtimeCapability ()
            let supportedText = if capability.PersistentWindow then "true" else "false"

            Expect.equal exitCode 0 "window diagnostics command succeeds"
            Expect.stringContains output ("persistent-window-supported=" + supportedText) "probe reports the real runtime-capability gate verbatim, not a fabricated verdict"
            Expect.stringContains output "diagnostic-class=window-visibility" "probe still enumerates the window-visibility class"

            Expect.isFalse (output.Contains "visible=observed:false") "probe never fabricates an observed window-invisibility"
            Expect.isFalse (output.Contains "status=failed") "probe never reports a failed status it did not observe"
            Expect.isFalse (output.Contains "taskbar-only" && output.Contains "status=ok") "taskbar-only is never reported ok"

            if capability.PersistentWindow then
                Expect.isFalse (output.Contains "status=unsupported") "a window-capable host is never told a live window is impossible"
            else
                Expect.stringContains output "status=unsupported" "an unsupported host is reported unsupported (matching the real launch), not failed"
        }

        // #139: the keyboard-only host boundary must be SURFACED where a game author first wires input
        // (a comment at the input-mapping site) and must stay ACCURATE to the emitted host contract, so
        // it cannot silently rot if the seams change. Source/contract scan — no host launch needed.
        test "keyboard-only host boundary is surfaced at the input-wiring site and accurate to the host contract (#139)" {
            let readAppRootFile parts =
                System.IO.File.ReadAllText(System.IO.Path.Combine(Array.append [| __SOURCE_DIRECTORY__; ".."; ".." |] parts))

            let typeBlock (source: string) (name: string) =
                let start = source.IndexOf("type " + name, System.StringComparison.Ordinal)
                let next = source.IndexOf("type ", start + 5, System.StringComparison.Ordinal)
                let stop = if next < 0 then source.Length else next
                source.Substring(start, stop - start)

            // A1 — the boundary is present at the game input-wiring site (Model.fs, by paddleForKey).
            let modelSource = readAppRootFile [| "src"; "Product"; "Model.fs" |]
            Expect.stringContains modelSource "HOST INPUT BOUNDARY" "Model.fs surfaces the keyboard-only host boundary at the input-wiring site"
            Expect.stringContains (modelSource.ToLowerInvariant()) "keyboard-only" "the boundary states the default host is keyboard-only"
            Expect.stringContains modelSource "runInteractiveApp" "the boundary names the pointer-aware interactive host path as the way to mouse-aim"
            Expect.stringContains modelSource "MapPointer" "the boundary names the pointer seam an author would need"

            // A2 — accurate: the emitted ViewerKey has NO mouse/pointer case (keyboard keys only).
            let keyboardFsi = readAppRootFile [| "docs"; "api-surface"; "KeyboardInput"; "KeyboardInput.fsi" |]
            let viewerKeyBlock = typeBlock keyboardFsi "ViewerKey"
            Expect.stringContains viewerKeyBlock "ArrowLeft" "ViewerKey enumerates keyboard keys"
            Expect.isFalse (viewerKeyBlock.Contains "Mouse") "ViewerKey has no mouse case — the note's core claim"
            Expect.isFalse (viewerKeyBlock.Contains "Pointer") "ViewerKey has no pointer case — the note's core claim"

            // A3 — accurate: the default host (GeneratedAppHost) exposes MapKey but NOT MapPointer;
            // the pointer-aware InteractiveAppHost is where MapPointer lives.
            let viewerFsi = readAppRootFile [| "docs"; "api-surface"; "SkiaViewer"; "SkiaViewer.fsi" |]
            let generatedHostBlock = typeBlock viewerFsi "GeneratedAppHost"
            Expect.stringContains generatedHostBlock "MapKey:" "the default host exposes a keyboard MapKey seam"
            Expect.isFalse (generatedHostBlock.Contains "MapPointer") "the default host has NO pointer seam (keyboard-only) — the boundary is real"
            let interactiveHostBlock = typeBlock viewerFsi "InteractiveViewerHost"
            Expect.stringContains interactiveHostBlock "MapPointer:" "the pointer-aware interactive host is where the mouse seam actually lives"
            // the note's author-facing signpost names must exist in the shipped contract (accuracy of FR-002)
            Expect.stringContains viewerFsi "InteractiveAppHost" "the note's named pointer-aware host type is real in the shipped surface"
            Expect.stringContains viewerFsi "runInteractiveApp" "the note's named interactive-host entry point is real in the shipped surface"
        }
    ]
//#else
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer

[<Tests>]
let behaviorTests =
    testList "product-behavior" [
        test "generated product test suite is wired" {
            Expect.equal 1 1 "product tests run"
        }

        test "generated public contract exposes qualified app-owned names" {
            let scene: FS.GG.UI.Scene.Scene = { Nodes = [ AppRoot.Program.view initialModel ] }
            let host = AppRoot.Program.generatedHost
            let updated, _ = AppRoot.Program.update NoOp initialModel

            Expect.isNonEmpty scene.Nodes "AppRoot.Program.view returns a scene"
            Expect.equal updated initialModel "AppRoot.Program.update is callable as the app reducer"
            Expect.isSome (host.MapKey Enter true) "AppRoot.Program.generatedHost exposes viewer input mapping"
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
            let host = AppRoot.Program.interactiveHost
            let size: FS.GG.UI.Scene.Size = { Width = 640; Height = 480 }
            let model0 = fst (host.Init())
            let rendered = Control.renderTree host.Theme size (host.View size model0)

            // Resolve the "save" control's evaluated box via the layout engine (the same path
            // runInteractiveApp hit-tests), then click its centre.
            let available: FS.GG.UI.Layout.AvailableSpace =
                { Width = float size.Width
                  WidthMode = FS.GG.UI.Layout.Exactly
                  Height = float size.Height
                  HeightMode = FS.GG.UI.Layout.Exactly }

            let layoutResult = FS.GG.UI.Layout.Layout.evaluate available rendered.Layout
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

            let summary, _ = AppRoot.Program.update (Navigated Summary) backToBrowse

            let restarted, _ =
                dispatchViewerKey { RawKey = "Enter"; Direction = ViewerKeyDirection.KeyDown } summary

            Expect.equal settings.Page Settings "settings page opens through viewer key"
            Expect.equal browse.Page Browse "settings apply enters browse page"
            Expect.equal detail.Page Detail "enter opens the detail page"
            Expect.equal backToBrowse.Page Browse "escape returns from detail to browse"
            Expect.equal restarted.Page Home "summary page restarts through viewer Enter"
        }

        test "pure generated app transitions expose model message and effect behavior" {
            let started, startEffects = AppRoot.Program.update (ViewerInput(Enter, true)) initialModel
            let interacted, interactionEffects = AppRoot.Program.update (ViewerInput(ArrowLeft, true)) started

            Expect.equal started.Page Browse "pure update opens the browse page"
            Expect.isEmpty startEffects "input transition has no host command"
            Expect.equal interacted.Interactions 1 "content-region interaction is counted"
            Expect.isEmpty interactionEffects "content interaction has no host command"
        }

        test "generated host boundary keeps app commands separate from viewer effects" {
            let unchanged, appCommands = AppRoot.Program.update SaveRequested initialModel
            let hosted, observedAppCommands, viewerEffects = AppRoot.Program.interpretAtHostBoundary SaveRequested initialModel
            let hostUpdated, hostViewerEffects = AppRoot.Program.generatedHost.Update SaveRequested initialModel

            Expect.equal unchanged initialModel "save command does not mutate the app model"
            Expect.equal hosted initialModel "host boundary preserves pure update result"
            Expect.equal hostUpdated initialModel "generated host uses the same pure update result"
            Expect.exists appCommands (function DispatchHostCommand "save:Product" -> true | _ -> false) "pure update emits an app command"
            Expect.equal observedAppCommands appCommands "host boundary exposes app commands before interpretation"
            Expect.exists (observedAppCommands |> List.map AppRoot.Program.appCommandName) ((=) "app-command:dispatch-host-command:save:Product") "app command category is named separately"
            Expect.exists viewerEffects (function RenderScene _ -> true | _ -> false) "host boundary emits viewer render effect separately"
            Expect.equal hostViewerEffects.Length viewerEffects.Length "generated host returns the same number of viewer effects to SkiaViewer"
            Expect.exists hostViewerEffects (function RenderScene _ -> true | _ -> false) "generated host returns render effects to SkiaViewer"
        }

        test "generated layout evidence separates summary and content regions at default and constrained sizes" {
            let defaultReport = AppRoot.Program.layoutEvidenceForSize { Width = 1280; Height = 720 } initialModel
            let constrainedReport = AppRoot.Program.layoutEvidenceForSize { Width = 640; Height = 480 } initialModel

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
            let summaryOverlap = AppRoot.Program.layoutEvidenceForSize { Width = 480; Height = 480 } initialModel
            let contentOverlap =
                AppRoot.Program.layoutEvidenceForSize
                    { Width = 640; Height = 480 }
                    { initialModel with ContentRow = -6 }

            let summaryResult = AppRoot.Program.validateGeneratedLayout summaryOverlap
            let contentResult = AppRoot.Program.validateGeneratedLayout contentOverlap

            Expect.isFalse summaryResult.Accepted "summary/summary overlap fails validation"
            Expect.equal summaryResult.FailureClass (Some OverlappingLayoutBounds) "summary overlap is classified"
            Expect.isFalse contentResult.Accepted "summary/content overlap fails validation"
            Expect.equal contentResult.FailureClass (Some OverlappingLayoutBounds) "summary/content overlap is classified"
        }

        test "generated content policies use the content region for the active item and bounds" {
            let started, _ = AppRoot.Program.update (ViewerInput(Enter, true)) initialModel
            let moved, _ = AppRoot.Program.update (ViewerInput(ArrowRight, true)) started
            let ticked, _ = AppRoot.Program.update Tick moved

            let region = AppRoot.Program.gameplayRegionForSize { Width = 640; Height = 480 }
            let bounds = AppRoot.Program.activeGameplayBoundsForSize { Width = 640; Height = 480 } ticked

            Expect.isTrue (AppRoot.Program.boundsInside region.Bounds bounds.Bounds) "active item remains inside the content region"
            Expect.isTrue (AppRoot.Program.movementUsesGameplayRegion { Width = 640; Height = 480 } ticked) "movement policy is region based"
            Expect.isTrue (AppRoot.Program.spawnUsesGameplayRegion { Width = 640; Height = 480 } initialModel) "spawn policy is region based"
            Expect.isTrue (AppRoot.Program.collisionUsesGameplayRegion { Width = 640; Height = 480 } ticked) "collision policy is region based"
        }

        test "generated default app dispatches input, advances over time, and keeps evidence flags opt-in" {
            let started, _ = dispatchViewerKey { RawKey = "Enter"; Direction = ViewerKeyDirection.KeyDown } initialModel
            let moved, _ = dispatchViewerKey { RawKey = "ArrowRight"; Direction = ViewerKeyDirection.KeyDown } started

            Expect.notEqual moved initialModel "keyboard input changes application state"
            Expect.isGreaterThan moved.Interactions started.Interactions "right input is reflected in content state"

            match tick (TimeSpan.FromMilliseconds 500.0) with
            | Some tickMsg ->
                let afterTick, _ = AppRoot.Program.update tickMsg moved
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

        // #136 (epic #134): the --window-diagnostics probe must agree with the real launch. Its
        // verdict is derived from the SAME gate ControlsElmish.runInteractiveApp/Viewer.runApp consult
        // (Viewer.runtimeCapability()), so the reported live-window capability equals that gate and it
        // never fabricates an observed window failure — the self-report/reality mismatch the reporter hit.
        test "window diagnostics verdict matches the real runtime gate (#136 / epic #134)" {
            let dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "window-diagnostics-" + System.Guid.NewGuid().ToString("N"))
            let path = System.IO.Path.Combine(dir, "window-diagnostics.txt")
            let exitCode = AppRoot.EvidenceCommands.windowDiagnostics path
            let output = System.IO.File.ReadAllText path
            System.IO.Directory.Delete(dir, true)

            let capability = Viewer.runtimeCapability ()
            let supportedText = if capability.PersistentWindow then "true" else "false"

            Expect.equal exitCode 0 "window diagnostics command succeeds"
            Expect.stringContains output ("persistent-window-supported=" + supportedText) "probe reports the real runtime-capability gate verbatim, not a fabricated verdict"
            Expect.stringContains output "diagnostic-class=window-visibility" "probe still enumerates the window-visibility class"

            Expect.isFalse (output.Contains "visible=observed:false") "probe never fabricates an observed window-invisibility"
            Expect.isFalse (output.Contains "status=failed") "probe never reports a failed status it did not observe"
            Expect.isFalse (output.Contains "taskbar-only" && output.Contains "status=ok") "taskbar-only is never reported ok"

            if capability.PersistentWindow then
                Expect.isFalse (output.Contains "status=unsupported") "a window-capable host is never told a live window is impossible"
            else
                Expect.stringContains output "status=unsupported" "an unsupported host is reported unsupported (matching the real launch), not failed"
        }
    ]
//#endif
//#endif
