module AppRootBehaviorTests

open System
open Expecto
open AppRoot.Program
open AppRoot.Model
open FS.GG.UI.Scene

[<Tests>]
let windowOptionOverlayTests =
    testList "window option overlay" [
        test "safe default and every single option overlay preserve unspecified fields" {
            let baseline: AppRoot.WindowOptions.WindowBehaviorSettings = AppRoot.WindowOptions.parseWindowBehavior []
            Expect.equal baseline.Startup "fullscreen" "no flags use the safe exclusive-fullscreen default"
            let cases: (string list * (AppRoot.WindowOptions.WindowBehaviorSettings -> bool)) list =
                [ [ "--window-resize"; "fixed-size" ], fun v -> v.Resize = "fixed-size"
                  [ "--window-maximize"; "not-maximizable" ], fun v -> v.Maximize = "not-maximizable"
                  [ "--window-startup"; "normal" ], fun v -> v.Startup = "normal"
                  [ "--window-position"; "10,20" ], fun v -> v.Position = "10,20"
                  [ "--window-backend"; "opengl" ], fun v -> v.Backend = "opengl" ]
            for args, changed in cases do
                let overlay: AppRoot.WindowOptions.WindowBehaviorSettings = AppRoot.WindowOptions.parseWindowBehavior args
                Expect.isTrue (changed overlay) "the supplied flag changes its own field"
                if args.Head <> "--window-resize" then Expect.equal overlay.Resize baseline.Resize "unrelated resize remains default"
                if args.Head <> "--window-maximize" then Expect.equal overlay.Maximize baseline.Maximize "unrelated maximize remains default"
                if args.Head <> "--window-startup" then Expect.equal overlay.Startup baseline.Startup "omitted startup remains safe default"
                if args.Head <> "--window-position" then Expect.equal overlay.Position baseline.Position "unrelated position remains default"
                if args.Head <> "--window-backend" then Expect.equal overlay.Backend baseline.Backend "unrelated backend remains default"
        }
    ]

// Feature 060 (FR-005): replaceable scaffold-BEHAVIOR tests. These call the scaffold
// product's `view`/`update`/host/scene-text directly, so when you replace the scaffold
// model with your own you rewrite THIS file. `GovernanceTests.fs` (compiled first) keeps
// its model-agnostic source/structure/evidence scans green across that swap.

let rec collectSceneNodes node =
    seq {
        yield node
        // Recurse into EVERY container case. This match is deliberately exhaustive — no `_`
        // wildcard — so a new `SceneNode` case added to FS.GG.UI.Scene surfaces here as an
        // incomplete-match warning (FS0025) naming the unhandled case, instead of being
        // silently swallowed by a catch-all and dropping every node beneath it (#899). The
        // library's own walker (`Scene.describe`) is exhaustive for the same reason; mirror it.
        match node with
        | Group scenes ->
            for scene in scenes do
                for child in scene.Nodes do
                    yield! collectSceneNodes child
        | ClipNode(_, scene)
        | ColorSpaceNode(_, scene)
        | PerspectiveNode(_, scene)
        | Translate(_, scene) ->
            for child in scene.Nodes do
                yield! collectSceneNodes child
        | PictureNode picture ->
            for child in picture.Scene.Nodes do
                yield! collectSceneNodes child
        | CachedSubtree boundary ->
            for child in boundary.Scene.Nodes do
                yield! collectSceneNodes child
        // Leaf nodes carry no child scenes. Enumerated explicitly (not a `_` wildcard) so the
        // exhaustiveness guard above stays armed — a new case forces a decision here.
        | Empty
        | Rectangle _
        | PaintedRectangle _
        | Circle _
        | FilledEllipse _
        | Ellipse _
        | Line _
        | Path _
        | Points _
        | Vertices _
        | Arc _
        | Text _
        | TextRun _
        | Image _
        | RegionNode _
        | Chart _
        | SizedText _
        | GlyphRun _ -> ()
    }

let sceneText node =
    collectSceneNodes node
    |> Seq.choose (function
        | Text(_, value, _) -> Some value
        | TextRun run -> Some run.Text
        | SizedText(_, value, _, _) -> Some value
        | GlyphRun run -> Some run.Data.Text
        | _ -> None)
    |> String.concat " "

// Regression guard for #899: the scene walker must recurse through EVERY container case.
// `Translate` and `CachedSubtree` were added to `SceneNode` after this helper was first
// written, and a `| _ -> ()` catch-all silently dropped everything beneath them — a product
// that wraps its world layers in a `Translate` camera-slide got a walker that under-reported
// with no error. This test is profile-independent (it exercises the shared helpers directly,
// not the product `view`), so it guards the fix across every scaffold profile.
[<Tests>]
let sceneWalkerTests =
    let black = { Red = 0uy; Green = 0uy; Blue = 0uy; Alpha = 255uy }

    testList "scene-walker (#899)" [
        test "collectSceneNodes recurses through Translate and CachedSubtree; sceneText reads their text" {
            let translated =
                Translate((5.0, 5.0), { Nodes = [ Text((0.0, 0.0), "hello", black); SizedText((0.0, 0.0), "sized", 12.0, black) ] })
            let cached =
                CachedSubtree { CacheId = 1UL; Fingerprint = 2UL; Scene = { Nodes = [ Text((1.0, 1.0), "cached", black) ] } }
            let root = Group [ { Nodes = [ translated; cached ] } ]

            let nodes = collectSceneNodes root |> Seq.toList
            // Group + Translate + (Text, SizedText) + CachedSubtree + Text = 6. Pre-fix, the two
            // container children were dropped and this was 3.
            Expect.equal (List.length nodes) 6 "the walker yields every node beneath Translate and CachedSubtree"

            let text = sceneText root
            Expect.stringContains text "hello" "Text beneath a Translate is reached"
            Expect.stringContains text "sized" "SizedText is recognised as text-bearing"
            Expect.stringContains text "cached" "Text beneath a CachedSubtree is reached"
        }
    ]

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
open FS.GG.UI.Controls
open AppRoot.Geometry // Vec2 (Vx/Vy) — the collision-safe positions the starter uses (feature 250)

// One fixed sim step's worth of elapsed time — enough for `update (Tick oneStep)` to drain a step.
let private oneStep = 1.0 / 60.0

// A raw viewer key-DOWN event, built exactly as the host receives one from the window. The #911
// scene-host driver (`runKeyScriptToModel` / `auditKeyWiring` / `reachableMessages`) normalizes the
// RawKey itself ("w" -> Letter 'W', "ArrowUp" -> ArrowUp), so a test hands it the raw string the
// live runtime would, never a pre-normalized ViewerKey.
let private downKey raw : ViewerKeyEvent =
    { RawKey = raw; Direction = ViewerKeyDirection.KeyDown }

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

        // Host-boot (#991/#1000) + the audio-init seam (#458). The game's turnkey DEFAULT launch is
        // now the pointer-host `interactiveHost` booting the generic game shell, so this asserts what
        // the LAUNCH host does at `Init` — model-agnostically (the shell's default-launch BEHAVIOUR),
        // not a brittle model substring:
        //   (1) it boots the shell's MAIN MENU (`Init -> MainMenu`), not straight into the play model;
        //   (2) it still routes the LOADED initial play state through the audio seam (#458).
        //
        // (2) is only catchable at the SINK, not the model: a restored setting the mixer was never
        // told about is indistinguishable, from inside the model, from one applied correctly. So it
        // asks what the host actually HANDED OUT — the ViewerEffect list `Init` returns, which is what
        // `ControlsElmish.runInteractiveAppWithAudio` feeds to the audio sink. The failure leg is real:
        // drop the `AudioCues.forTransition Started` route from the shell host's `Init` and this reds.
        test "generated game host boots the shell main menu and cues the initial state through the same seam (#991/#1000/#458)" {
            let host = AppRoot.Program.interactiveHost
            let model0, initEffects = host.Init()

            // (1) host-boot: the turnkey default launches into the shell's main menu.
            Expect.equal
                model0.Shell.Screen
                AppRoot.GameShell.MainMenu
                "the interactive launch host boots into the shell main menu (Init -> MainMenu), not the raw play model"

            // (2) #458: the loaded initial play state still reaches the audio sink at Init.
            let expected =
                AppRoot.AudioCues.forTransition Started AppRoot.Program.initialModel AppRoot.Program.initialModel

            Expect.isNonEmpty
                expected
                "the scaffold ships `Started` wired to a cue — an empty one would make this test vacuous (#458)"

            let cued =
                initEffects
                |> List.collect (fun effect ->
                    match effect with
                    | PlayAudio cues -> cues
                    | _ -> [])

            Expect.equal
                cued
                expected
                "interactiveHost.Init routes the initial play model through AudioCues.forTransition Started — state that is LOADED rather than transitioned into must still reach the audio sink (#458)"
        }

        // Issue #1012: acceptance at the ACTUAL generated launch host. This is deliberately not a
        // GameShell.update test: native pointer/key values enter InteractiveAppHost, retained hit
        // bounds dispatch authored bindings, and held input advances only on fixed ticks.
        test "interactive shell host proves default geometry, retained menu/rebind clicks, and key down -> ticks -> up (#1012)" {
            let host = AppRoot.Program.interactiveHost
            let size = AppRoot.Program.viewerOptions.InitialSize

            Expect.equal
                size
                AppRoot.EvidenceCommands.shellConfig.InitialDisplay.Resolution
                "the default native surface and authored shell resolution are one coordinate space"

            let defaultWindow =
                AppRoot.GameShell.windowBehavior AppRoot.EvidenceCommands.shellConfig.InitialDisplay

            Expect.equal defaultWindow.StartupState ViewerWindowStartupState.Normal "default launch explicitly uses the shell's Windowed behavior"

            Expect.equal
                AppRoot.Program.viewerOptions.LogicalSize
                (Some AppRoot.EvidenceCommands.shellConfig.InitialDisplay.Resolution)
                "the generated game seeds SkiaViewer with the shell's logical canvas"

            let pointer phase x y : ViewerPointerInput =
                { Phase = phase
                  X = x
                  Y = y
                  Button = Some ViewerPointerButtonKind.Primary
                  DeltaX = 0.0
                  DeltaY = 0.0 }

            let foldMessages model messages =
                messages |> List.fold (fun current msg -> fst (host.Update msg current)) model

            let rendered model =
                FS.GG.UI.Controls.Control.renderTree host.Theme size (host.View size model)

            let centre controlId model =
                let frame = rendered model
                Expect.isTrue (Set.contains controlId frame.BoundIds) $"{controlId} is an authored bound control, not a silent test target"

                let available: FS.GG.UI.Layout.AvailableSpace =
                    { Width = float size.Width
                      WidthMode = FS.GG.UI.Layout.Exactly
                      Height = float size.Height
                      HeightMode = FS.GG.UI.Layout.Exactly }

                let layout = FS.GG.UI.Layout.Layout.evaluate available frame.Layout
                let bounds = (layout.Bounds |> List.find (fun b -> b.NodeId = controlId)).Bounds
                bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height / 2.0

            let clickWithProof controlId model =
                let x, y = centre controlId model
                let state, down =
                    FS.GG.UI.Controls.Elmish.ControlsElmish.routeInteractivePointer
                        host
                        (Pointer.init ())
                        size
                        model
                        (pointer ViewerPointerPhaseKind.Pressed x y)
                let afterDown = foldMessages model down
                let proof =
                    FS.GG.UI.Controls.Elmish.ControlsElmish.captureRespondsProof
                        host
                        state
                        size
                        afterDown
                        (pointer ViewerPointerPhaseKind.Released x y)
                let _, up =
                    FS.GG.UI.Controls.Elmish.ControlsElmish.routeInteractivePointer
                        host state size afterDown (pointer ViewerPointerPhaseKind.Released x y)
                proof, foldMessages afterDown up

            let model0 = fst (host.Init())
            let startProof, playing = clickWithProof "start" model0
            Expect.equal startProof.Verdict FS.GG.UI.Controls.Elmish.Responsive "retained Start click visibly enters play"
            Expect.equal playing.Shell.Screen AppRoot.GameShell.Playing "the native retained pointer route dispatches Start"

            let raw key isDown = host.MapKey key isDown |> Option.get
            let tickMessage () = host.Tick (TimeSpan.FromMilliseconds 16.0) |> Option.get
            let keyDown = fst (host.Update (raw (Letter 'W') true) playing)
            let afterTick1 = fst (host.Update (tickMessage ()) keyDown)
            let afterTick2 = fst (host.Update (tickMessage ()) afterTick1)
            let keyUp = fst (host.Update (raw (Letter 'W') false) afterTick2)
            let afterReleaseTick = fst (host.Update (tickMessage ()) keyUp)

            Expect.isTrue (afterTick1.Play.LeftPaddleY < keyDown.Play.LeftPaddleY) "held W advances on the first fixed tick"
            Expect.isTrue (afterTick2.Play.LeftPaddleY < afterTick1.Play.LeftPaddleY) "held W remains active across a second fixed tick"
            Expect.equal afterReleaseTick.Play.LeftPaddleY keyUp.Play.LeftPaddleY "key-up clears the held control before the next tick"

            let configProof, settings = clickWithProof "config" model0
            Expect.equal configProof.Verdict FS.GG.UI.Controls.Elmish.Responsive "retained Config click visibly opens settings"

            let rebindId = "rebind-left-up"

            let rebindProof, armed = clickWithProof rebindId settings
            Expect.equal rebindProof.Verdict FS.GG.UI.Controls.Elmish.Responsive "retained rebind-row click visibly enters capture"
            Expect.isSome armed.Shell.Rebinding "the clicked row arms the next raw key"
            let capturedCommand = armed.Shell.Rebinding.Value

            let captured = fst (host.Update (raw (Letter 'Q') true) armed)
            Expect.equal captured.Shell.Rebinding None "the next native raw key completes capture"
            Expect.equal
                (Keymap.resolve captured.Shell.Keymap (ViewerKeyboard.toKeyId (Letter 'Q')))
                (Some capturedCommand)
                "the real host's raw-key seam installs the captured key"
            Expect.equal
                (Keymap.resolve captured.Shell.Keymap (ViewerKeyboard.toKeyId (Letter 'W')))
                None
                "the real retained-host capture replaces the command binding instead of adding a second key"

            // Issue #1014: change the selected logical canvas, prove the host-boundary effect, then
            // click the same semantic Config control from its corresponding physical point on a
            // deliberately differently-shaped surface. SkiaViewer's exact runtime inverse is gated
            // in its package suite; this generated-product assertion proves the emitted host wiring
            // and retained control compose with it.
            let resolution1080: FS.GG.UI.Scene.Size = { Width = 1920; Height = 1080 }
            let shell1080, shellEffects =
                AppRoot.GameShell.update (AppRoot.GameShell.SetResolution resolution1080) model0.Shell

            let viewerEffects =
                shellEffects |> List.collect AppRoot.EvidenceCommands.viewerEffectsForShellEffect

            Expect.exists
                viewerEffects
                (function ApplyLogicalCanvas size -> size = resolution1080 | _ -> false)
                "DisplayChanged dynamically installs the selected logical canvas"

            for mode in
                [ AppRoot.GameShell.Windowed
                  AppRoot.GameShell.Borderless
                  AppRoot.GameShell.Fullscreen ] do
                let _, modeEffects =
                    AppRoot.GameShell.update (AppRoot.GameShell.SetDisplayMode mode) shell1080

                let modeViewerEffects =
                    modeEffects
                    |> List.collect AppRoot.EvidenceCommands.viewerEffectsForShellEffect

                Expect.exists
                    modeViewerEffects
                    (function ApplyWindowOptions _ -> true | _ -> false)
                    $"{mode} emits its presentation request"

                Expect.exists
                    modeViewerEffects
                    (function ApplyLogicalCanvas size -> size = resolution1080 | _ -> false)
                    $"{mode} keeps the selected logical coordinate policy"

            let model1080 = { model0 with Shell = shell1080 }
            let surface: FS.GG.UI.Scene.Size = { Width = 1600; Height = 1000 }
            let frame1080 = FS.GG.UI.Controls.Control.renderTree host.Theme resolution1080 (host.View resolution1080 model1080)
            let available1080: FS.GG.UI.Layout.AvailableSpace =
                { Width = float resolution1080.Width
                  WidthMode = FS.GG.UI.Layout.Exactly
                  Height = float resolution1080.Height
                  HeightMode = FS.GG.UI.Layout.Exactly }
            let layout1080 = FS.GG.UI.Layout.Layout.evaluate available1080 frame1080.Layout
            let configBounds = (layout1080.Bounds |> List.find (fun b -> b.NodeId = "config")).Bounds
            let logicalX = configBounds.X + configBounds.Width / 2.0
            let logicalY = configBounds.Y + configBounds.Height / 2.0
            let fit = LogicalCanvas.fit resolution1080 surface
            let physicalX = logicalX * fit.Scale + fit.OffsetX
            let physicalY = logicalY * fit.Scale + fit.OffsetY
            let routedX, routedY = LogicalCanvas.toLogicalPoint resolution1080 surface physicalX physicalY
            let state1080, down1080 =
                FS.GG.UI.Controls.Elmish.ControlsElmish.routeInteractivePointer
                    host (Pointer.init ()) resolution1080 model1080
                    (pointer ViewerPointerPhaseKind.Pressed routedX routedY)
            let afterDown1080 = foldMessages model1080 down1080
            let _, up1080 =
                FS.GG.UI.Controls.Elmish.ControlsElmish.routeInteractivePointer
                    host state1080 resolution1080 afterDown1080
                    (pointer ViewerPointerPhaseKind.Released routedX routedY)
            let configured1080 = foldMessages afterDown1080 up1080
            Expect.equal
                configured1080.Shell.Screen
                AppRoot.GameShell.Settings
                "the corresponding physical point still activates Config after 1280x720 -> 1920x1080"
        }

        // Issue #1022: this is intentionally capability-gated rather than replaced by another effect-
        // emission assertion. On a desktop host it drives the generated shell's DisplayChanged path
        // through ControlsElmish, the shared ViewerEffect interpreter, and the real native-window
        // mutation boundary; on a headless host it records the irreducible native tier as skipped.
        test "DisplayChanged reaches the live native window-mode boundary (#1022)" {
            if not (Viewer.runtimeCapability().PersistentWindow) then
                skiptest "SKIPPED(tier=T2 native-window/GL): no persistent desktop window is available"

            let observed = ResizeArray<ViewerDiagnosticEvent>()
            let host =
                { AppRoot.Program.interactiveHost with
                    Tick =
                        fun _ ->
                            Some(
                                AppRoot.EvidenceCommands.ShellDispatch(
                                    AppRoot.GameShell.SetDisplayMode AppRoot.GameShell.Fullscreen))
                    Diagnostics =
                        { Viewer.defaultDiagnostics with
                            Categories = Set.add ViewerDiagnosticCategory.Window Viewer.defaultDiagnostics.Categories
                            Sink = Some observed.Add } }

            let result =
                FS.GG.UI.Controls.Elmish.ControlsElmish.Live.runScriptWithWindowBehavior
                    AppRoot.Program.viewerOptions
                    (AppRoot.GameShell.windowBehavior AppRoot.EvidenceCommands.shellConfig.InitialDisplay)
                    host
                    [ FS.GG.UI.Controls.Elmish.FrameInput.Idle
                      FS.GG.UI.Controls.Elmish.FrameInput.Idle ]

            match result with
            | Result.Error failure -> failtestf "live generated host failed: %s" failure.Message
            | Result.Ok _ -> ()

            Expect.exists observed
                (fun diagnostic ->
                    diagnostic.Category = ViewerDiagnosticCategory.Window
                    && diagnostic.Message.Contains "Runtime window behavior applied: mode=fullscreen")
                "the generated DisplayChanged request is observed after real native mutation, not only at emission"
        }

        // Issue #912: PLAYED THROUGH THE HOST — the one altitude the host tests above never reach.
        //
        // They prove the halves in isolation: `MapKey ArrowUp true` returns SOME message, and
        // `Update` (handed a message) advances the model. Neither COMPOSES them, and the gap between
        // the halves is exactly where a product ships green-but-unplayable — a key that maps to a
        // message `update` quietly ignores, or a `mapKey` that folds every key into a key-state
        // snapshot no gameplay ever reads. The model-level test below (`update (ViewerInput …)`)
        // INJECTS the message; it never asks whether pressing the key PRODUCES it.
        //
        // `runKeyScriptToModel` asks the whole question. It folds a raw key SCRIPT through the host's
        // own `dispatchKey` (`normalizeEvent -> MapKey -> Update`) — the SAME fold the live runtime
        // performs — from `Init`'s model to the final one, no window and no GL. Rougue1 shipped "v1
        // complete" with 108 green tests while unplayable precisely because the suite injected
        // messages straight into `update` and nothing ever exercised key -> host -> update. This is
        // the shape of test that would have caught it — rewrite its script/assertions for your game.
        test "generated game host PLAYS THROUGH: a pressed key reaches gameplay, not just a message" {
            let host = AppRoot.Program.generatedHost
            let restedLeftPaddle = (fst (host.Init())).LeftPaddleY

            // 'W' drives the LEFT paddle UP (Model.paddleForKey -> movePaddle). Folded key -> mapKey
            // -> update through the real driver, two presses move it. If `mapKey` stopped wrapping the
            // key, or `update` stopped routing `ViewerInput` to `movePaddle`, the paddle would not
            // move and this reddens — where injecting `ViewerInput(Letter 'W', true)` stays green.
            let played, _ = GeneratedAppHost.runKeyScriptToModel host [ downKey "w"; downKey "w" ]

            Expect.isTrue
                (played.LeftPaddleY < restedLeftPaddle)
                "two 'w' presses, folded key -> mapKey -> update, move the left paddle up: the key reaches gameplay"
            Expect.equal played.LastInput (Some(Letter 'W')) "the host records the last key it actually routed"
        }

        // Issue #911/#912: HANDLED-BUT-UNWIRED guard — the scene-host analogue of the click path's
        // `BoundIds` ("a key that is bound is a key that dispatches"). `reachableMessages` returns
        // every message the host's runtime SOURCES: `mapKey` over the probed keys, plus `Tick` when a
        // sample is given. A `Msg` case your `update` handles but that appears in NO source is
        // dispatched by nothing — the Rougue1 defect. List the intents a player must be able to reach
        // and assert each is sourced; it reddens the moment you add a handled intent (a Fire, a Jump)
        // and forget to wire an input — or a tick — to it. (This starter's `mapKey` wraps EVERY key as
        // `ViewerInput`, so `auditKeyWiring.Dead` is structurally empty here and the PLAYED-THROUGH
        // test above is the load-bearing guard; a game whose `mapKey` returns intent messages directly
        // gets a non-vacuous `Dead` too — see the fs-gg-elmish skill.)
        test "generated game host SOURCES the intents a player must reach (handled-but-unwired guard)" {
            let host = AppRoot.Program.generatedHost
            let probe = [ downKey "w"; downKey "s"; downKey "ArrowUp"; downKey "ArrowDown" ]
            let reachable = GeneratedAppHost.reachableMessages host probe (Some(TimeSpan.FromMilliseconds 16.0))

            Expect.isTrue
                (reachable |> List.exists (function ViewerInput _ -> true | _ -> false))
                "the keyboard is a live source of ViewerInput — the seam the paddle router reads, not a dead key-state snapshot"
            Expect.isTrue
                (reachable |> List.exists (function Tick _ -> true | _ -> false))
                "the clock is a live source of Tick — the fixed-step sim advances from a real source, not only from injected ticks"
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
            // #991/#1000 + Governance#297: the game family now boots the generic game shell on the
            // pointer-aware `interactiveHost`. Asserted by the default-launch BEHAVIOUR — the audio sink
            // and the persistent host reach the launcher — not the launcher-overload substring, so this
            // survives both a model swap and a sanctioned launcher upgrade (the model-agnostic contract
            // the durable GovernanceTests spine owns; reused here rather than re-pinned).
            AppRootGovernanceTests.assertDefaultLaunchHonoursEffects defaultBranch
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

        // #901/#1030: the --view-image probe renders the FULL product view at an explicit logical
        // size through the window-free CPU readback. Two non-default sizes prove the command does
        // not merely relabel a fixed PNG, and invalid dimensions must fail instead of falling back.
        test "view-image probe renders requested logical sizes and rejects invalid dimensions (#901/#1030)" {
            let pngSignature = [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

            let readBigEndianInt32 (bytes: byte array) offset =
                (int bytes[offset] <<< 24)
                ||| (int bytes[offset + 1] <<< 16)
                ||| (int bytes[offset + 2] <<< 8)
                ||| int bytes[offset + 3]

            for width, height in [ 320, 200; 901, 507 ] do
                let dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "view-image-" + System.Guid.NewGuid().ToString("N"))
                let path = System.IO.Path.Combine(dir, "view-image.png")
                let exitCode = AppRoot.EvidenceCommands.viewImageAtSize path width height
                let metadata = System.IO.File.ReadAllText(path + ".metadata.txt")
                let pngExists = System.IO.File.Exists path
                let pngBytes = if pngExists then System.IO.File.ReadAllBytes path else [||]
                System.IO.Directory.Delete(dir, true)

                Expect.equal exitCode 0 "valid view-image dimensions produce pixels or an honest unsupported result"
                Expect.stringContains metadata "command=--view-image" "records the evidence command"
                Expect.stringContains metadata $"requested-size={width}x{height}" "records the requested logical size"
                Expect.stringContains metadata $"output-size={width}x{height}" "records the render surface size"

                if metadata.Contains "status=ok" then
                    Expect.isTrue pngExists "a supported host writes the PNG file"
                    Expect.equal (pngBytes |> Array.truncate 8) pngSignature "the readback is a decodable PNG"
                    Expect.equal (readBigEndianInt32 pngBytes 16) width "the PNG IHDR width matches the request"
                    Expect.equal (readBigEndianInt32 pngBytes 20) height "the PNG IHDR height matches the request"
                    Expect.stringContains metadata $"actual-size={width}x{height}" "metadata records the PNG-header size"
                    Expect.stringContains metadata "dimensions-match=True" "metadata proves requested and actual dimensions agree"
                    Expect.stringContains metadata "renders-full-view=true" "the probe renders the full product view"
                else
                    Expect.stringContains metadata "status=unsupported" "an unsupported host reports unsupported (never fabricated pixels)"

            let invalidPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "view-image-invalid-" + System.Guid.NewGuid().ToString("N") + ".png")
            let invalid = AppRoot.EvidenceCommands.tryRunEvidenceCommand [ "--view-image"; invalidPath; "0"; "720" ]
            Expect.equal invalid (Some 1) "non-positive explicit dimensions return a nonzero command result"
            Expect.isFalse (System.IO.File.Exists invalidPath) "invalid dimensions never fall back to a default-size image"

            let oversizedPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "view-image-oversized-" + System.Guid.NewGuid().ToString("N") + ".png")
            let oversized =
                AppRoot.EvidenceCommands.tryRunEvidenceCommand
                    [ "--view-image"; oversizedPath; "2000000000"; "2000000000" ]
            Expect.equal oversized (Some 1) "oversized dimensions fail before attempting a CPU raster allocation"
            Expect.isFalse (System.IO.File.Exists oversizedPath) "an oversized request writes no image"
        }

        // #139 (revised for #991/#1000): the input boundary must be SURFACED where a game author first
        // wires input (a comment at the input-mapping site) and must stay ACCURATE to the emitted host
        // contract, so it cannot silently rot if the seams change. The game family now boots the
        // pointer-host shell by default, so the note states that the PLAY-INPUT mapping is keyboard-only
        // while the turnkey launch is the pointer-aware interactive host. Source/contract scan — no
        // host launch needed.
        test "play-input boundary is surfaced at the input-wiring site and accurate to the host contract (#139/#1000)" {
            let readAppRootFile parts =
                System.IO.File.ReadAllText(System.IO.Path.Combine(Array.append [| __SOURCE_DIRECTORY__; ".."; ".." |] parts))

            let typeBlock (source: string) (name: string) =
                let start = source.IndexOf("type " + name, System.StringComparison.Ordinal)
                let next = source.IndexOf("type ", start + 5, System.StringComparison.Ordinal)
                let stop = if next < 0 then source.Length else next
                source.Substring(start, stop - start)

            // A1 — the boundary is present at the game input-wiring site (Model.fs, by paddleForKey).
            let modelSource = readAppRootFile [| "src"; "Product"; "Model.fs" |]
            Expect.stringContains modelSource "HOST INPUT BOUNDARY" "Model.fs surfaces the input-wiring boundary at the input-wiring site"
            Expect.stringContains (modelSource.ToLowerInvariant()) "keyboard-only" "the boundary states the play-input mapping (paddleForKey) is keyboard-only"
            Expect.stringContains modelSource "runInteractiveApp" "the boundary names the pointer-aware interactive host the turnkey default now launches on"
            Expect.stringContains modelSource "MapPointer" "the boundary names the pointer seam the interactive host adds"

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
        // through the framework tree-render path (`Control.renderTree`), not hand-drawn
        // placeholder geometry. The rendered scene therefore carries the example control text.
        test "default view renders real controls through the framework render path" {
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

        // Issue #436 (and #458, one profile over). Assert at the SINK, not at the model.
        //
        // This is the only shape of test that catches this class. A test that asserts on the MODEL
        // cannot: a restored setting the mixer was never told about is indistinguishable, from inside
        // the model, from one that was applied correctly. So this asks what the host actually HANDED
        // OUT — the ViewerEffect list `Init` returns, which is what the `*WithAudio` launch entry
        // point feeds to the audio sink.
        //
        // The host under test is the one this profile LAUNCHES with: `app` launches
        // `ControlsElmish.runInteractiveAppWithAudio ... interactiveHost`, sample-pack launches
        // `Viewer.runAppWithAudio ... generatedHost`. Testing the other one would prove nothing about
        // what the product actually does at startup.
        //
        // Its failure leg is real: revert this profile's `Init` to `fun () -> initialModel, []` (what
        // the app profile's was before #436) and this goes red. That matters — a fix whose failure leg
        // is untested is how this class survives its own fix.
        test "generated host cues the INITIAL state through the same seam as every other state" {
            //#if (profile == "app")
            let host = AppRoot.Program.interactiveHost
            //#else
            let host = AppRoot.Program.generatedHost
            //#endif
            let model0, initEffects = host.Init()

            let expected = AppRoot.AudioCues.forTransition Started model0 model0

            Expect.isNonEmpty
                expected
                "the scaffold ships `Started` wired to a cue — an empty one would make this test vacuous (#458)"

            let cued =
                initEffects
                |> List.collect (fun effect ->
                    match effect with
                    | PlayAudio cues -> cues
                    | _ -> [])

            Expect.equal
                cued
                expected
                "Init routes the initial model through AudioCues.forTransition Started — state that is LOADED rather than transitioned into must still reach the audio sink (#458/#436)"
        }

        // The other half of the seam: a real transition's cues reach the sink too. An Init-only wiring
        // would play the startup cue and then fall silent for every interaction after it.
        test "generated host cues a real transition through the audio seam" {
            //#if (profile == "app")
            let host = AppRoot.Program.interactiveHost
            //#else
            let host = AppRoot.Program.generatedHost
            //#endif
            let _, effects = host.Update SaveRequested initialModel

            let expected =
                AppRoot.AudioCues.forTransition SaveRequested initialModel (fst (AppRoot.Program.update SaveRequested initialModel))

            Expect.isNonEmpty expected "the Controls starter cues `SaveRequested` — an empty cue would make this test vacuous"

            let cued =
                effects
                |> List.collect (fun effect ->
                    match effect with
                    | PlayAudio cues -> cues
                    | _ -> [])

            Expect.equal cued expected "Update lifts the transition's cues onto ViewerEffect.PlayAudio for the audio sink"
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

            // #436: `generatedHost` is the host the game/sample-pack families LAUNCH with, so on
            // sample-pack its Update now appends the frame's `PlayAudio` batch to the boundary's
            // effects — it returns the boundary's render effects PLUS the cues, and `SaveRequested`
            // is a cued transition. (The `app` family launches with `interactiveHost`, which carries
            // the seam instead; `generatedHost` there serves only the evidence commands, which
            // discard audio.) So compare the RENDER half — the part SkiaViewer draws, and the part
            // this test is actually about — rather than the raw list length, which would now differ
            // by exactly the cue batch and turn a correctly-wired seam into a red test.
            // Compare what `RenderScene` CARRIES, not the effects as values: `ViewerEffect` has no
            // structural equality. `ViewerDiagnosticsOptions.Sink` is a function, and it reaches the DU
            // through `StartBoundedRun of ViewerRunRequest`, so `Expect.equal` over a `ViewerEffect list`
            // does not compile (FS0001) — and the generated product is only ever compiled by the
            // release-only job, so it failed nowhere until it failed the release (#679). `SceneNode` has
            // equality, and it is the payload this assertion was always about.
            let renderedScenes effects =
                effects |> List.choose (function RenderScene scene -> Some scene | _ -> None)

            Expect.equal
                (renderedScenes hostViewerEffects)
                (renderedScenes viewerEffects)
                "generated host returns the boundary's render effects to SkiaViewer, unchanged"
            Expect.isNonEmpty (renderedScenes hostViewerEffects) "generated host returns render effects to SkiaViewer"

            // ...and nothing ELSE. Comparing only the render half would stop noticing a host that
            // emitted a stray effect — a duplicated `PlayAudio` (every sound played twice, if a future
            // edit lifted cues in `interpretAtHostBoundary` AND in `Update`), or an evidence effect
            // that has no business on the live path. The old length-equality caught that class; keep
            // the guard rather than trading it away for the cue batch.
            let nonRender effects =
                effects |> List.filter (function RenderScene _ -> false | _ -> true)

            Expect.isEmpty (nonRender viewerEffects) "the host boundary itself emits only render effects"
            Expect.isTrue
                (nonRender hostViewerEffects |> List.forall (function PlayAudio _ -> true | _ -> false))
                "the generated host adds only audio cue batches to the boundary's effects — nothing else"
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
            // #436: and every family's default launch carries the audio sink. `sample-pack` used to
            // land on the sinkless `Viewer.runApp viewerOptions generatedHost` while referencing all
            // four FS.GG.Audio packages; it now shares the game family's expression, which is the one
            // it always should have had — the two families already shared `generatedHost`.
            //#if (profile == "app")
            Expect.stringContains defaultBranch "ControlsElmish.runInteractiveAppWithAudio viewerOptions audioSink interactiveHost" "controls-family normal launch uses the pointer-aware persistent host, with the #429/#436 audio sink"
            //#else
            Expect.stringContains defaultBranch "Viewer.runAppWithAudio viewerOptions audioSink generatedHost" "game/sample-pack normal launch uses the keyboard-only persistent host (with the #245 audio sink)"
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

        // #901/#1030: the --view-image probe renders the FULL product view at an explicit logical
        // size through the window-free CPU readback. Two non-default sizes prove the command does
        // not merely relabel a fixed PNG, and invalid dimensions must fail instead of falling back.
        test "view-image probe renders requested logical sizes and rejects invalid dimensions (#901/#1030)" {
            let pngSignature = [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

            let readBigEndianInt32 (bytes: byte array) offset =
                (int bytes[offset] <<< 24)
                ||| (int bytes[offset + 1] <<< 16)
                ||| (int bytes[offset + 2] <<< 8)
                ||| int bytes[offset + 3]

            for width, height in [ 320, 200; 901, 507 ] do
                let dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "view-image-" + System.Guid.NewGuid().ToString("N"))
                let path = System.IO.Path.Combine(dir, "view-image.png")
                let exitCode = AppRoot.EvidenceCommands.viewImageAtSize path width height
                let metadata = System.IO.File.ReadAllText(path + ".metadata.txt")
                let pngExists = System.IO.File.Exists path
                let pngBytes = if pngExists then System.IO.File.ReadAllBytes path else [||]
                System.IO.Directory.Delete(dir, true)

                Expect.equal exitCode 0 "valid view-image dimensions produce pixels or an honest unsupported result"
                Expect.stringContains metadata "command=--view-image" "records the evidence command"
                Expect.stringContains metadata $"requested-size={width}x{height}" "records the requested logical size"
                Expect.stringContains metadata $"output-size={width}x{height}" "records the render surface size"

                if metadata.Contains "status=ok" then
                    Expect.isTrue pngExists "a supported host writes the PNG file"
                    Expect.equal (pngBytes |> Array.truncate 8) pngSignature "the readback is a decodable PNG"
                    Expect.equal (readBigEndianInt32 pngBytes 16) width "the PNG IHDR width matches the request"
                    Expect.equal (readBigEndianInt32 pngBytes 20) height "the PNG IHDR height matches the request"
                    Expect.stringContains metadata $"actual-size={width}x{height}" "metadata records the PNG-header size"
                    Expect.stringContains metadata "dimensions-match=True" "metadata proves requested and actual dimensions agree"
                    Expect.stringContains metadata "renders-full-view=true" "the probe renders the full product view"
                else
                    Expect.stringContains metadata "status=unsupported" "an unsupported host reports unsupported (never fabricated pixels)"

            let invalidPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "view-image-invalid-" + System.Guid.NewGuid().ToString("N") + ".png")
            let invalid = AppRoot.EvidenceCommands.tryRunEvidenceCommand [ "--view-image"; invalidPath; "0"; "720" ]
            Expect.equal invalid (Some 1) "non-positive explicit dimensions return a nonzero command result"
            Expect.isFalse (System.IO.File.Exists invalidPath) "invalid dimensions never fall back to a default-size image"

            let oversizedPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "view-image-oversized-" + System.Guid.NewGuid().ToString("N") + ".png")
            let oversized =
                AppRoot.EvidenceCommands.tryRunEvidenceCommand
                    [ "--view-image"; oversizedPath; "2000000000"; "2000000000" ]
            Expect.equal oversized (Some 1) "oversized dimensions fail before attempting a CPU raster allocation"
            Expect.isFalse (System.IO.File.Exists oversizedPath) "an oversized request writes no image"
        }
    ]
//#endif
//#endif
