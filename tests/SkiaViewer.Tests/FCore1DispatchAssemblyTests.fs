module FCore1DispatchAssemblyTests

// #F-CORE-1 Stage C — the ASSEMBLY gate for `runGeneratedApp`'s persistence-dispatch knot.
//
// The Issue535 seam tests drive `interpretViewerEffects` / `dispatchPersistenceBatch` in isolation and
// are the right tool for the wiring in the small. What NO test reaches is the ASSEMBLY the launch
// builds — `let rec interpretEffects … and persistenceBatchSink … and dispatchHostMsg`, tied off with
// the sticky `outcomeCloseRequested` — and that assembly is exactly where the seam shipped its worst
// bug: a `dispatchOutcome` assigned AFTER `interpretEffects initEffects`, so a product that loaded its
// save on `Init` (the single most common persistence pattern) had the outcome dropped on the floor,
// with 350 green tests attached. The staging plan (Stage C) is about to move this knot; a move that
// re-breaks it must red, and only a run through the WHOLE loop can catch it.
//
// So these drive `Viewer.runAppWithPersistence` through a REAL window — the only place the assembly
// actually runs. Gated on `runtimeCapability().PersistentWindow`: headless CI skips (the same limitation
// #365/#396/#429/#535's live legs record); a display runs it. The list is `testSequenced` because two
// overlapping `GlHost.run` calls clobber the process-wide render statics (Issue #180 / F-CORE-4). The
// window is stopped from INSIDE the product, by an outcome-driven `CloseWindow`, so the test never
// depends on an external close.

open System.Collections.Generic
open Expecto
open FS.GG.UI.Canvas
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private slot = SaveSlot "assembly-slot"
let private envelope = { Version = 1; Slot = slot; Payload = SavePayload "{score:7}" }
let private white = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

let private liveOptions =
    { Title = "F-CORE-1 dispatch assembly"
      InitialSize = { Width = 320; Height = 240 }
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FrameRateCap = None
      LogicalSize = None }

/// A sink that ANSWERS a `Load` with the envelope (the only outcome these tests map back) and echoes
/// `Save`/`Delete` so nothing the product asks for throws.
let private answeringSink (batch: PersistenceEffect list) =
    batch
    |> List.map (fun effect ->
        match effect with
        | Load s -> PersistenceOutcome.Loaded { envelope with Slot = s }
        | Save e -> PersistenceOutcome.Saved e.Slot
        | DeleteSlot s -> PersistenceOutcome.Deleted s)

let private mapLoadedTo message outcome =
    match outcome with
    | PersistenceOutcome.Loaded _ -> Some message
    | _ -> None

[<Tests>]
let tests =
    testSequenced
    <| testList
        "F-CORE-1 Stage C — persistence dispatch assembly (live window)"
        [
          // THE re-entrant round-trip, end to end: a product message emits `Persist [load]`, the sink
          // answers, the outcome re-enters `dispatchHostMsg`, and THAT product step both runs (dispatches
          // back) and asks to close — and the sticky `outcomeCloseRequested` has to carry that close all
          // the way up through the tick's dispatch to actually stop the window. If the extraction flattens
          // the recursion or loses the sticky flag, one of the two assertions reds.
          test "an outcome-driven message dispatches back through the assembled loop AND its close request stops the window" {
              if not (Viewer.runtimeCapability().PersistentWindow) then
                  skiptestf "no persistent-window capability (headless); the live assembly is not drivable here"
              else
                  let dispatched = List<string>()
                  let mutable ticks = 0

                  let host: GeneratedAppHost<int, string> =
                      { Init = fun () -> 0, []
                        Update =
                          fun msg model ->
                              match msg with
                              | "trigger-load" -> model, [ Persist [ Persistence.load slot ] ]
                              | "apply-load" ->
                                  // The outcome-driven product step: it re-entered dispatchHostMsg, and it
                                  // asks to close — the two things this test is here to prove.
                                  dispatched.Add "apply-load"
                                  model + 1, [ CloseWindow ]
                              | "force-quit" ->
                                  // Backstop: if the outcome-driven close is broken, this fires ~300 ticks
                                  // in so the run ENDS (and the assertion fails) instead of hanging forever.
                                  dispatched.Add "force-quit"
                                  model, [ CloseWindow ]
                              | _ -> model, []
                        View = fun model -> Text((0.0, 0.0), $"n={model}", white)
                        MapKey = fun _ _ -> None
                        Tick =
                          fun _ ->
                              ticks <- ticks + 1
                              if ticks = 1 then Some "trigger-load"
                              elif ticks > 300 then Some "force-quit"
                              else None
                        Diagnostics = Viewer.defaultDiagnostics }

                  match Viewer.runAppWithPersistence liveOptions answeringSink (mapLoadedTo "apply-load") host with
                  | Result.Error failure -> failtestf "the live launch failed: %A" failure.Message
                  | Result.Ok outcome ->
                      Expect.contains
                          dispatched
                          "apply-load"
                          "the Load outcome re-entered dispatchHostMsg and ran the product's Update — the whole let-rec knot fired"

                      Expect.isFalse
                          (dispatched.Contains "force-quit")
                          "the outcome-driven CloseWindow actually STOPPED the window — the backstop never had to fire"

                      Expect.equal
                          outcome.CloseReason
                          (Some AppRequestedClose)
                          "and the window closed by app request (the product), not a user or evidence close"
          }

          // THE HISTORICAL BUG, DIRECTLY. A `Persist [load]` emitted at `Init` must be answered before the
          // loop, not silently dropped. Mutual recursion makes the drop unrepresentable — there is no
          // window in which the dispatcher is not yet itself — and the extraction must preserve that.
          test "a Load requested at Init is answered, not dropped" {
              if not (Viewer.runtimeCapability().PersistentWindow) then
                  skiptestf "no persistent-window capability (headless); the live assembly is not drivable here"
              else
                  let dispatched = List<string>()
                  let mutable ticks = 0

                  let host: GeneratedAppHost<int, string> =
                      { Init = fun () -> 0, [ Persist [ Persistence.load slot ] ]
                        Update =
                          fun msg model ->
                              match msg with
                              | "apply-load" ->
                                  dispatched.Add "apply-load"
                                  model + 1, []
                              | "quit" ->
                                  dispatched.Add "quit"
                                  model, [ CloseWindow ]
                              | _ -> model, []
                        View = fun model -> Text((0.0, 0.0), $"n={model}", white)
                        MapKey = fun _ _ -> None
                        Tick =
                          fun _ ->
                              ticks <- ticks + 1
                              Some "quit"
                        Diagnostics = Viewer.defaultDiagnostics }

                  match Viewer.runAppWithPersistence liveOptions answeringSink (mapLoadedTo "apply-load") host with
                  | Result.Error failure -> failtestf "the live launch failed: %A" failure.Message
                  | Result.Ok _ ->
                      Expect.contains
                          dispatched
                          "apply-load"
                          "the Init-time Load outcome was dispatched into update — before the mutual recursion, this exact pattern was dropped"
          }
        ]
