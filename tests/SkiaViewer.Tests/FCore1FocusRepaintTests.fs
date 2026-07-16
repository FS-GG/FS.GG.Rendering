module FCore1FocusRepaintTests

// #F-CORE-1 Stage D — the live-loop PLACEMENT gate for danger zone #5 (`runtimeStateRepaint` placement).
//
// `Feature175RepaintSignalTests` locks the POLICY headlessly: `runtimeStateRepaint` re-derives iff the
// input produced no product message. What that cannot reach is whether the interactive loop's
// `handleKey` actually ROUTES a no-message key through that policy on THIS frame — the "focus one click
// behind / dead-hover / dead-scroll" class the placement exists to kill. Only the live loop exercises
// the placement, so this drives `runInteractiveViewerScript` through a REAL window: a key that maps to
// NO product message but mutates host runtime state (focus) must re-derive the scene on that key,
// observable as a "runtime-state-repaint" trace emitted from the live loop.
//
// `testSequenced`: the trace-capture buffer is process-global (F175 S3), and two overlapping
// `GlHost.run` calls clobber the render statics (Issue #180). Gated on `PersistentWindow`: headless CI
// skips (as #365/#396/#429/#535's live legs do); a display runs it. The script drives two key events
// then exhausts, which is what closes the window — no external close needed.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer

let private white = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

let private options: ViewerOptions =
    { Title = "F-CORE-1 focus repaint"
      InitialSize = { Width = 320; Height = 240 }
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FrameRateCap = None
      LogicalSize = None }

[<Tests>]
let tests =
    testSequenced
    <| testList
        "F-CORE-1 Stage D — input-handler repaint placement (live window)"
        [
          test "the interactive loop re-derives on a no-message key — runtime state is visible on THIS frame, not one key behind" {
              if not (Viewer.runtimeCapability().PersistentWindow) then
                  skiptestf "no persistent-window capability (headless); the live loop is not drivable here"
              else
                  // `focus` stands in for host-internal runtime state (focus traversal / hover / scroll):
                  // a key mutates it WITHOUT producing a product message, so the model never changes and
                  // only the `runtimeStateRepaint` re-derive can put the change on screen this frame.
                  let mutable focus = 0

                  let host: InteractiveViewerHost<int, int> =
                      { Init = fun () -> 0, []
                        Update = fun _ model -> model, []
                        View = fun _ _ -> Text((0.0, 0.0), $"focus={focus}", white)
                        MapKey =
                          fun _ isDown ->
                              if isDown then
                                  focus <- focus + 1

                              [] // NO product message — the whole point of this case
                        MapPointer = fun _ _ _ -> []
                        Tick = fun _ -> None
                        Diagnostics = Viewer.defaultDiagnostics }

                  let script =
                      [ ViewerScriptInput.Key(ViewerKey.ArrowRight, true)
                        ViewerScriptInput.Key(ViewerKey.ArrowRight, false)
                        ViewerScriptInput.WaitFrame ]

                  Viewer.traceStartCapture ()

                  match Viewer.runInteractiveViewerScript options script host with
                  | Result.Error failure -> failtestf "the live scripted launch failed: %A" failure.Message
                  | Result.Ok _ ->
                      let events = Viewer.traceDrainCapture ()

                      Expect.isTrue (focus > 0) "the scripted key actually reached MapKey through the live loop"

                      Expect.exists
                          events
                          (fun (event, _) -> event = "runtime-state-repaint")
                          "the no-message key re-derived the scene from host.View on THIS frame — the placement is intact, not one key behind"
          }
        ]
