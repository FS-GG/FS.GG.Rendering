module Issue460CoalescingParityTests

// Issue #460 — the conformance gate for `ControlsElmish.Coalescing`.
//
// `Perf.runScript`'s whole value is that a test written against it is a test of the HOST, not of a
// parallel mock of the host. Its signature asserted that in the words "no parallel logic". For
// coalescing that was false, and the falsehood was load-bearing: the live loop coalesces raw SAMPLES
// (before the hit-test), while `Perf` coalesces already-derived INTERACTIONS, and the two are not the
// same operation. `Perf` kept only `List.last` of a coalesced frame — so it silently ANNIHILATED a
// `HoverLeave` that was not last, which is precisely the pair `Pointer.update` emits for a move that
// changes the hit. Hover-out never fired headlessly and the suite stayed green.
//
// The asymmetry is real and cannot be refactored away (coalescing must precede the hit-test on the
// live side; a script is written in the hit-test's OUTPUT alphabet). So it is gated instead. These
// tests drive the REAL `Pointer.update` state machine — no mock, no hand-copied predicate — and fail
// if either side ever drifts from the other about what a coalesced frame may drop.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Layout
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default

// --- the observed host ------------------------------------------------------
//
// The view carries NO bindings, so `routeRetainedInteraction`'s binding resolution always misses and
// every routed interaction falls through to `MapPointer`. That makes `MapPointer` a faithful tap on
// "which interactions actually reached routing" — which is exactly the property under test.

type private Msg = Saw of PointerInteraction

let private size: Size = { Width = 320; Height = 200 }

let private view (_model: PointerInteraction list) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text "a" ] |> Control.withKey "A"
                Button.create [ Button.text "b" ] |> Control.withKey "B" ] ]

let private host: InteractiveAppHost<PointerInteraction list, Msg> =
    { Init = fun () -> [], []
      Update = fun (Saw i) model -> model @ [ i ], []
      View = fun _ model -> view model
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun interaction -> Some(Saw interaction)
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

/// Feed an interaction script to `Perf` and return the interactions that actually reached routing.
let private observed (script: PointerInteraction list) : PointerInteraction list =
    let model, _ =
        ControlsElmish.Perf.runScriptToModel host size (script |> List.map FrameInput.Pointer)

    model

// --- the real pointer state machine -----------------------------------------

let private policy: PixelSnapPolicy = { ScaleFactor = 1.0; Mode = Round }

/// Two side-by-side 100x40 buttons: A at [0,100), B at [100,200).
let private layout: LayoutResult =
    { Bounds =
        [ { NodeId = "A"
            Bounds = { X = 0.0; Y = 0.0; Width = 100.0; Height = 40.0 }
            Visibility = Visible }
          { NodeId = "B"
            Bounds = { X = 100.0; Y = 0.0; Width = 100.0; Height = 40.0 }
            Visibility = Visible } ]
      Diagnostics = []
      Invalidated = []
      Revision = 0L }

let private sample phase x y button : PointerSample =
    { Phase = phase
      X = x
      Y = y
      Button = button
      DeltaX = 0.0
      DeltaY = 0.0 }

let private moved x y = sample PointerPhase.Moved x y None
let private pressed x y = sample PointerPhase.Pressed x y (Some PointerButton.Primary)
let private released x y = sample PointerPhase.Released x y (Some PointerButton.Primary)
let private exited = sample PointerPhase.Exited 0.0 0.0 None

/// The honest interaction trace: what the REAL state machine derives from this raw sample stream.
/// This is the alphabet a `Perf` script is written in, and what the live host routes.
let private traceOf (samples: PointerSample list) : PointerInteraction list =
    samples
    |> List.fold
        (fun (state, acc) s ->
            match Pointer.toMsg s with
            | None -> state, acc
            | Some msg ->
                let state', interactions, _ = Pointer.update policy layout msg state
                state', acc @ interactions)
        (Pointer.init (), [])
    |> snd

// A `HoverLeave` / `DragBegin` / press / release / scroll is a STATE TRANSITION: the live host
// re-derives it from whichever sample survives coalescing, so it is always delivered. Only a
// superseded POSITION may be dropped. This is `Coalescing.isSupersedablePosition`, restated here
// independently so the test is not merely asserting the implementation against itself.
let private isPosition (interaction: PointerInteraction) =
    match interaction with
    | HoverEnter _
    | DragMove _ -> true
    | _ -> false

// --- the corpus -------------------------------------------------------------

let private corpus: (string * PointerSample list) list =
    [ "hover A, then across to B (the canonical hit-changing move)",
      [ moved 10.0 10.0; moved 30.0 20.0; moved 150.0 20.0; moved 160.0 25.0 ]

      "hover A, then leave the window entirely", [ moved 10.0 10.0; exited ]

      "hover A, then out to empty space (leave with no enter)",
      [ moved 10.0 10.0; moved 300.0 300.0 ]

      "press in A, drag past threshold, release in B",
      [ moved 10.0 10.0
        pressed 10.0 10.0
        moved 40.0 20.0
        moved 90.0 20.0
        moved 150.0 20.0
        released 150.0 20.0 ]

      "a long hover burst inside one control (nothing to lose)",
      [ moved 10.0 10.0; moved 12.0 11.0; moved 14.0 12.0; moved 16.0 13.0; moved 18.0 14.0 ]

      "across A -> B -> A, twice (leave/enter pairs back to back)",
      [ moved 10.0 10.0; moved 150.0 20.0; moved 20.0 10.0; moved 160.0 20.0 ] ]

[<Tests>]
let tests =
    testList
        "issue-460 Coalescing.Parity"
        [
          // --- the regression that shipped -----------------------------------
          testCase "the leave+enter pair a hit-changing move emits survives Perf coalescing"
          <| fun () ->
              // `Pointer.update` emits [HoverLeave prior; HoverEnter next] TOGETHER, from ONE sample.
              // The live host routes both. `Perf` used to route only the enter.
              let trace = traceOf [ moved 10.0 10.0; moved 150.0 20.0 ]

              Expect.contains trace (HoverLeave "A") "PRECONDITION: the real state machine emits the leave"

              let seen = observed trace

              Expect.contains seen (HoverLeave "A") "Perf must route the HoverLeave — the live host always delivers it"

          // --- the general property, over the real state machine ---------------
          yield!
              corpus
              |> List.map (fun (name, samples) ->
                  testCase $"no state transition is lost: {name}"
                  <| fun () ->
                      let trace = traceOf samples
                      let seen = observed trace

                      let transitions = trace |> List.filter (isPosition >> not)
                      let seenTransitions = seen |> List.filter (isPosition >> not)

                      // Every non-positional interaction the real state machine derived must reach
                      // routing, in order. Positions may collapse; state transitions may not.
                      Expect.equal
                          seenTransitions
                          transitions
                          $"Perf dropped a state transition the live host delivers.\n  trace: %A{trace}\n  saw:   %A{seen}")

          // --- coalescing must still actually coalesce -------------------------
          testCase "a hover burst inside one control still collapses to a single processed move"
          <| fun () ->
              // The fix must not un-coalesce moves: that would be the perf regression the byte-stable
              // goldens exist to catch, and it would make this whole surface pointless.
              let trace =
                  traceOf [ moved 10.0 10.0; moved 12.0 11.0; moved 14.0 12.0; moved 16.0 13.0 ]

              let frames =
                  ControlsElmish.Perf.runScript host size (trace |> List.map FrameInput.Pointer)

              Expect.equal (List.length frames) 1 "the burst is ONE frame"
              Expect.equal frames.Head.PointerMovesProcessed 1 "exactly one processed move"

              let enters = observed trace |> List.filter isPosition
              Expect.equal (List.length enters) 1 "only the LAST position is routed"

          // --- the rule itself -------------------------------------------------
          testCase "only Moved is a coalescible sample"
          <| fun () ->
              let coalescible =
                  [ PointerPhase.Moved
                    PointerPhase.Pressed
                    PointerPhase.Released
                    PointerPhase.Wheel
                    PointerPhase.Exited ]
                  |> List.filter ControlsElmish.Coalescing.isCoalescibleSample

              Expect.equal coalescible [ PointerPhase.Moved ] "a discrete sample must never be dropped"

          testCase "every supersedable interaction is groupable, and HoverLeave is groupable but not supersedable"
          <| fun () ->
              let every =
                  [ HoverEnter("A", 1.0, 1.0)
                    HoverLeave "A"
                    PressedDown("A", PointerButton.Primary, 1.0, 1.0)
                    ReleasedUp("A", PointerButton.Primary, 1.0, 1.0)
                    Click("A", PointerButton.Primary, 1.0, 1.0)
                    DragBegin("A", PointerButton.Primary, 1.0, 1.0)
                    DragMove("A", PointerButton.Primary, 1.0, 1.0)
                    DragEnd("A", PointerButton.Primary, 1.0, 1.0)
                    DragCancelled(Some "A")
                    Scroll("A", 0.0, 1.0, 1.0, 1.0)
                    FocusMovedByPointer "A" ]

              // A frame can only drop what it groups, so the drop set must be a SUBSET of the group
              // set. If someone widens `isSupersedablePosition` past `isMoveInteraction`, that is an
              // interaction dropped from a frame it was never coalesced into.
              for i in every do
                  if ControlsElmish.Coalescing.isSupersedablePosition i then
                      Expect.isTrue
                          (ControlsElmish.Coalescing.isMoveInteraction i)
                          $"%A{i} is supersedable but not groupable — it would be dropped from a frame it is not in"

              Expect.isTrue
                  (ControlsElmish.Coalescing.isMoveInteraction (HoverLeave "A"))
                  "HoverLeave belongs to the move frame (it is emitted by a move)"

              Expect.isFalse
                  (ControlsElmish.Coalescing.isSupersedablePosition (HoverLeave "A"))
                  "HoverLeave is a state transition, NOT a superseded position — issue #460"

          testCase "routedInCoalescedFrame keeps every transition in order and the last position"
          <| fun () ->
              let frame =
                  [ HoverLeave "A"
                    HoverEnter("B", 1.0, 1.0)
                    HoverLeave "B"
                    HoverEnter("A", 2.0, 2.0)
                    HoverEnter("A", 3.0, 3.0) ]

              Expect.equal
                  (ControlsElmish.Coalescing.routedInCoalescedFrame frame)
                  [ HoverLeave "A"; HoverLeave "B"; HoverEnter("A", 3.0, 3.0) ]
                  "both leaves survive in order; only the last of the superseded enters does" ]
