module PointerInteractionTests

// Feature 075 — pointer/mouse interaction front door. Deterministic, scripted
// PointerMsg sequences are driven through the pure `Pointer.update`/`replay`
// reducer over a fabricated `LayoutResult`; these are genuine inputs (no mocks),
// so every assertion below is real evidence (Principle V — no [S] tasks here).

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Layout
open FS.GG.UI.Controls

// --- fixtures ---------------------------------------------------------------

let private policy: PixelSnapPolicy = { ScaleFactor = 1.0; Mode = Round }

let private bounds x y w h : LayoutBounds = { X = x; Y = y; Width = w; Height = h }

let private layoutOf (items: (string * LayoutBounds * LayoutVisibility) list) : LayoutResult =
    { Bounds = items |> List.map (fun (id, b, vis) -> { NodeId = id; Bounds = b; Visibility = vis })
      Diagnostics = []
      Invalidated = []
      Revision = 0L }

/// Two side-by-side 100x40 buttons: A at [0,100), B at [100,200).
let private twoButtons =
    layoutOf [ "A", bounds 0.0 0.0 100.0 40.0, Visible; "B", bounds 100.0 0.0 100.0 40.0, Visible ]

let private inA = 50.0, 20.0
let private inB = 150.0, 20.0
let private empty = 300.0, 300.0

let private run layout msgs = Pointer.replay policy layout msgs (Pointer.init ())

let private step layout msg state = Pointer.update policy layout msg state

// --- US1: hover -------------------------------------------------------------

[<Tests>]
let hoverTests =
    testList
        "Pointer US1 hover (SC-001/FR-003)"
        [ test "moving from A to B emits leave A then enter B in order" {
              let _, effects =
                  run twoButtons [ PointerMsg.Move inA; PointerMsg.Move inB ]

              Expect.equal
                  effects
                  [ HoverEnter("A", 50.0, 20.0); HoverLeave "A"; HoverEnter("B", 150.0, 20.0) ]
                  "leave-A precedes enter-B"
          }

          test "moving within the same control emits no redundant transition" {
              let _, effects = run twoButtons [ PointerMsg.Move(50.0, 20.0); PointerMsg.Move(60.0, 25.0) ]
              Expect.equal effects [ HoverEnter("A", 50.0, 20.0) ] "no second transition inside A"
          }

          test "moving to empty space emits leave only" {
              let _, effects = run twoButtons [ PointerMsg.Move inA; PointerMsg.Move empty ]
              Expect.equal effects [ HoverEnter("A", 50.0, 20.0); HoverLeave "A" ] "leave with no enter on empty space"
          }

          test "leaving the window emits hover-leave and clears the hover" {
              let state, effects = run twoButtons [ PointerMsg.Move inA; PointerMsg.WindowExited ]
              Expect.equal effects [ HoverEnter("A", 50.0, 20.0); HoverLeave "A" ] "window exit leaves A"
              Expect.equal state.Hover None "no hover after window exit"
          }

          test "overlapping controls: the front-most (last in paint order) wins" {
              let overlap =
                  layoutOf
                      [ "back", bounds 0.0 0.0 100.0 40.0, Visible
                        "front", bounds 50.0 0.0 100.0 40.0, Visible ]

              let _, effects = run overlap [ PointerMsg.Move(70.0, 20.0) ]
              Expect.equal effects [ HoverEnter("front", 70.0, 20.0) ] "front-most control is hovered"
          }

          test "hidden controls are never a hover target" {
              let withHidden =
                  layoutOf
                      [ "A", bounds 0.0 0.0 100.0 40.0, Visible
                        "ghost", bounds 0.0 0.0 100.0 40.0, Hidden ]

              let _, effects = run withHidden [ PointerMsg.Move(50.0, 20.0) ]
              Expect.equal effects [ HoverEnter("A", 50.0, 20.0) ] "hidden ghost is skipped, A is hovered"
          }

          test "Pointer.origin tags pointer-originated interactions (FR-011)" {
              Expect.equal Pointer.origin Pointer "the single v1 pointer origin tag"
          } ]

// --- US2: click + focus -----------------------------------------------------

let private hasClick btn effects =
    effects
    |> List.exists (function
        | Click(_, b, _, _) -> b = btn
        | _ -> false)

[<Tests>]
let clickTests =
    testList
        "Pointer US2 click + focus (SC-002/FR-004/FR-005)"
        [ test "press then release over the same control dispatches one click" {
              let _, effects =
                  run twoButtons [ PointerMsg.Down(PointerButton.Primary, 50.0, 20.0); PointerMsg.Up(PointerButton.Primary, 50.0, 20.0) ]

              let clicks = effects |> List.filter (function Click _ -> true | _ -> false)
              Expect.equal clicks [ Click("A", PointerButton.Primary, 50.0, 20.0) ] "exactly one click on A"
          }

          test "press records the control and moves focus to it (FR-004)" {
              let state, interactions, runtimeMsgs =
                  step twoButtons (PointerMsg.Down(PointerButton.Primary, 50.0, 20.0)) (Pointer.init ())

              Expect.equal
                  interactions
                  [ PressedDown("A", PointerButton.Primary, 50.0, 20.0); FocusMovedByPointer "A" ]
                  "press emits pressed-down then focus-moved"

              Expect.equal runtimeMsgs [ PressControl "A"; FocusControl(Some "A") ] "runtime press + focus messages"
              Expect.isTrue (state.Presses.ContainsKey PointerButton.Primary) "press candidate recorded"
          }

          test "release off the pressed control dispatches no click and clears the press" {
              let state, effects =
                  run twoButtons [ PointerMsg.Down(PointerButton.Primary, 50.0, 20.0); PointerMsg.Up(PointerButton.Primary, 150.0, 20.0) ]

              Expect.isFalse (hasClick PointerButton.Primary effects) "no click when released over a different control"
              Expect.isTrue state.Presses.IsEmpty "press state cleared after release"
          }

          test "press over empty space emits a hit-test-miss diagnostic" {
              let _, effects = run twoButtons [ PointerMsg.Down(PointerButton.Primary, 300.0, 300.0) ]

              match effects with
              | [ Diagnostic d ] -> Expect.equal d.Code HitTestMiss "press miss is a hit-test-miss diagnostic"
              | other -> failtestf "expected a single HitTestMiss diagnostic, got %A" other
          } ]

// --- US3: drag + cancel -----------------------------------------------------

[<Tests>]
let dragTests =
    testList
        "Pointer US3 drag + cancel (SC-003/SC-004/FR-006/FR-007)"
        [ test "press, move past threshold, move, release yields one begin, ordered moves, one end" {
              let _, effects =
                  run
                      twoButtons
                      [ PointerMsg.Down(PointerButton.Primary, 50.0, 20.0)
                        PointerMsg.Move(50.0, 30.0)
                        PointerMsg.Move(50.0, 38.0)
                        PointerMsg.Up(PointerButton.Primary, 50.0, 38.0) ]

              Expect.equal
                  effects
                  [ PressedDown("A", PointerButton.Primary, 50.0, 20.0)
                    FocusMovedByPointer "A"
                    DragBegin("A", PointerButton.Primary, 50.0, 20.0)
                    DragMove("A", PointerButton.Primary, 50.0, 38.0)
                    DragEnd("A", PointerButton.Primary, 50.0, 38.0) ]
                  "single begin, ordered move, single end — no click"

              Expect.isFalse (hasClick PointerButton.Primary effects) "a committed drag never also clicks"
          }

          test "sub-threshold press/release is a click, not a drag" {
              let _, effects =
                  run
                      twoButtons
                      [ PointerMsg.Down(PointerButton.Primary, 50.0, 20.0)
                        PointerMsg.Move(51.0, 21.0)
                        PointerMsg.Up(PointerButton.Primary, 51.0, 21.0) ]

              Expect.isTrue (hasClick PointerButton.Primary effects) "below-threshold movement keeps it a click"

              Expect.isFalse
                  (effects |> List.exists (function DragBegin _ -> true | _ -> false))
                  "no drag begins below the threshold"
          }

          test "window exit mid-drag cancels with empty presses and no active drag" {
              let state, effects =
                  run
                      twoButtons
                      [ PointerMsg.Down(PointerButton.Primary, 50.0, 20.0)
                        PointerMsg.Move(50.0, 36.0)
                        PointerMsg.WindowExited ]

              Expect.isTrue
                  (effects |> List.exists (function DragCancelled(Some "A") -> true | _ -> false))
                  "drag is cancelled for A"

              Expect.isTrue state.Presses.IsEmpty "no dangling presses after cancel (SC-004)"
          }

          test "focus loss mid-press cancels deterministically and issues the runtime FocusLost" {
              let pressed, _, _ =
                  step twoButtons (PointerMsg.Down(PointerButton.Primary, 50.0, 20.0)) (Pointer.init ())

              let state, interactions, runtimeMsgs = step twoButtons PointerMsg.FocusLost pressed

              Expect.equal interactions [ DragCancelled(Some "A") ] "press in flight is cancelled"
              Expect.equal runtimeMsgs [ ControlRuntimeMsg.FocusLost ] "focus loss propagates to the runtime"
              Expect.isTrue state.Presses.IsEmpty "presses cleared on focus loss"
          } ]

// --- US4: per-button / secondary --------------------------------------------

[<Tests>]
let secondaryTests =
    testList
        "Pointer US4 per-button discrimination (SC-008/FR-013)"
        [ test "secondary press/release yields a secondary click and no primary click" {
              let _, effects =
                  run twoButtons [ PointerMsg.Down(PointerButton.Secondary, 50.0, 20.0); PointerMsg.Up(PointerButton.Secondary, 50.0, 20.0) ]

              Expect.isTrue (hasClick PointerButton.Secondary effects) "secondary click is reported"
              Expect.isFalse (hasClick PointerButton.Primary effects) "no primary click for a secondary press"
          }

          test "middle press/release yields a distinct middle click" {
              let _, effects =
                  run twoButtons [ PointerMsg.Down(PointerButton.Middle, 50.0, 20.0); PointerMsg.Up(PointerButton.Middle, 50.0, 20.0) ]

              Expect.isTrue (hasClick PointerButton.Middle effects) "middle click is reported"
              Expect.isFalse (hasClick PointerButton.Primary effects) "middle is not misattributed to primary"
          }

          test "secondary press does not steal focus (only primary does)" {
              let _, interactions, runtimeMsgs =
                  step twoButtons (PointerMsg.Down(PointerButton.Secondary, 50.0, 20.0)) (Pointer.init ())

              Expect.equal interactions [ PressedDown("A", PointerButton.Secondary, 50.0, 20.0) ] "no focus-move interaction"
              Expect.equal runtimeMsgs [ PressControl "A" ] "secondary press records but does not focus"
          }

          test "overlapping primary + secondary presses resolve independently" {
              let state0 = Pointer.init ()
              let s1, _, _ = step twoButtons (PointerMsg.Down(PointerButton.Primary, 50.0, 20.0)) state0
              let s2, _, _ = step twoButtons (PointerMsg.Down(PointerButton.Secondary, 60.0, 20.0)) s1
              let s3, primaryEffects, _ = step twoButtons (PointerMsg.Up(PointerButton.Primary, 50.0, 20.0)) s2
              let _, secondaryEffects, _ = step twoButtons (PointerMsg.Up(PointerButton.Secondary, 60.0, 20.0)) s3

              Expect.isTrue (hasClick PointerButton.Primary primaryEffects) "primary release resolves to a primary click"
              Expect.isFalse (hasClick PointerButton.Secondary primaryEffects) "primary release does not emit a secondary click"
              Expect.isTrue (s3.Presses.ContainsKey PointerButton.Secondary) "secondary press survives the primary release"
              Expect.isTrue (hasClick PointerButton.Secondary secondaryEffects) "secondary release resolves to a secondary click"
          } ]

// --- US5: wheel / scroll ----------------------------------------------------

[<Tests>]
let wheelTests =
    testList
        "Pointer US5 wheel/scroll (SC-009/FR-014)"
        [ test "wheel over a control emits a scroll with the signed delta" {
              let _, effects = run twoButtons [ PointerMsg.WheelMsg(0.0, -3.0, 50.0, 20.0) ]
              Expect.equal effects [ Scroll("A", 0.0, -3.0, 50.0, 20.0) ] "scroll addressed to A with the signed delta"
          }

          test "wheel over empty space emits no scroll interaction" {
              let _, effects = run twoButtons [ PointerMsg.WheelMsg(0.0, -3.0, 300.0, 300.0) ]
              Expect.equal effects [] "no scroll over empty space"
          } ]

// --- Integration: determinism / replay (T027, SC-005) -----------------------

[<Tests>]
let determinismTests =
    testList
        "Pointer determinism / replay (SC-005/FR-009)"
        [ test "replaying the same sequence twice yields identical effects and state" {
              let script =
                  [ PointerMsg.Move inA
                    PointerMsg.Down(PointerButton.Primary, 50.0, 20.0)
                    PointerMsg.Move(50.0, 36.0)
                    PointerMsg.Up(PointerButton.Primary, 50.0, 36.0)
                    PointerMsg.Move inB
                    PointerMsg.WheelMsg(0.0, -2.0, 150.0, 20.0) ]

              let s1, e1 = run twoButtons script
              let s2, e2 = run twoButtons script
              Expect.equal e1 e2 "identical effects on re-run"
              Expect.equal s1 s2 "identical final state on re-run"
          } ]

// --- FsCheck properties -----------------------------------------------------

module private Gen075 =
    let point: Gen<float * float> =
        gen {
            let! x = Gen.choose (0, 220)
            let! y = Gen.choose (0, 60)
            return float x, float y
        }

    let moveBurst: Gen<(float * float) list> = Gen.listOf point

    // Sub-threshold deltas around the press origin in A (distance < 4px).
    let nearMoves: Gen<(float * float) list> =
        let delta =
            gen {
                let! dx = Gen.choose (-2, 2)
                let! dy = Gen.choose (-2, 2)
                return 50.0 + float dx, 20.0 + float dy
            }

        Gen.listOf delta

let private hoverOnly =
    List.choose (function
        | HoverEnter(c, _, _) -> Some(Choice1Of2 c)
        | HoverLeave c -> Some(Choice2Of2 c)
        | _ -> None)

[<Tests>]
let properties =
    testList
        "Pointer properties (FsCheck)"
        [ test "no duplicate or skipped hover transitions under random move bursts (FR-003)" {
              let valid (points: (float * float) list) =
                  let _, effects = run twoButtons (points |> List.map PointerMsg.Move)
                  // Fold the hover transitions: a leave requires the named control
                  // to be the current hover; an enter requires no current hover.
                  // The final hover must equal the hit of the last move.
                  let mutable current: string option = None
                  let mutable ok = true

                  for t in hoverOnly effects do
                      match t with
                      | Choice2Of2 c -> // HoverLeave c
                          if current <> Some c then ok <- false
                          current <- None
                      | Choice1Of2 c -> // HoverEnter c
                          if current <> None then ok <- false
                          current <- Some c

                  let lastHit =
                      match List.tryLast points with
                      | Some(x, y) -> Layout.hitTestComputed policy twoButtons x y
                      | None -> None

                  ok && current = lastHit

              let config = Config.QuickThrowOnFailure.WithMaxTest 500
              Check.One(config, Prop.forAll (Arb.fromGen Gen075.moveBurst) valid)
          }

          test "press/release pair is never dropped or reordered under interleaved moves (FR-008)" {
              let preserved (moves: (float * float) list) =
                  let msgs =
                      (PointerMsg.Down(PointerButton.Primary, 50.0, 20.0)
                       :: (moves |> List.map PointerMsg.Move))
                      @ [ PointerMsg.Up(PointerButton.Primary, 50.0, 20.0) ]

                  let _, effects = run twoButtons msgs
                  let pressedIdx = effects |> List.tryFindIndex (function PressedDown _ -> true | _ -> false)
                  let clickIdx = effects |> List.tryFindIndex (function Click _ -> true | _ -> false)
                  let pressedCount = effects |> List.filter (function PressedDown _ -> true | _ -> false) |> List.length
                  let clickCount = effects |> List.filter (function Click _ -> true | _ -> false) |> List.length
                  let noDrag = effects |> List.forall (function DragBegin _ | DragMove _ | DragEnd _ -> false | _ -> true)

                  match pressedIdx, clickIdx with
                  | Some p, Some c -> pressedCount = 1 && clickCount = 1 && p < c && noDrag
                  | _ -> false

              let config = Config.QuickThrowOnFailure.WithMaxTest 500
              Check.One(config, Prop.forAll (Arb.fromGen Gen075.nearMoves) preserved)
          } ]
