module Feature090DispatchTests

// Feature 090 — the interactive host dispatches authored EventBindings (LIVE-DISPATCH-1, FR-001/003),
// the responds-proof distinguishes renders from responds (RESPONDS-EVIDENCE-1, FR-006), and the
// focus-aware text seam delivers keystrokes to the focused text control (TEXT-INPUT-1, FR-008). All
// exercise the REAL adapter path (`routeInteractivePointer`/`routeFocusedText`/`captureRespondsProof`)
// with real control trees and real `Pointer.update`/`TextInput.update` — no mocks (no synthetic).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg =
    | Increment
    | Toggled of bool
    | SetName of string
    | Mapped

type private Model =
    { Count: int
      Checked: bool
      Name: string }

let private size = { Width = 320; Height = 200 }

let private update (msg: Msg) (model: Model) : Model * ViewerEffect list =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }, []
    | Toggled v -> { model with Checked = v }, []
    | SetName n -> { model with Name = n }, []
    | Mapped -> model, []

/// A view with a leaf-keyed Button (`onClick`) and a leaf-keyed CheckBox (`onChanged`), authored the
/// documented way — NO MapPointer clauses needed.
let private boundView (_: Size) (model: Model) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text (string model.Count); Button.onClick Increment ] |> Control.withKey "inc"
                CheckBox.create [ CheckBox.text "on"; CheckBox.checked' model.Checked; CheckBox.onChanged Toggled ] |> Control.withKey "chk" ] ]

let private hostOf view mapPointer : InteractiveAppHost<Model, Msg> =
    { Init = fun () -> { Count = 0; Checked = false; Name = "" }, []
      Update = update
      View = view
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = mapPointer
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private pointer phase x y : ViewerPointerInput =
    { Phase = phase
      X = x
      Y = y
      Button = Some ViewerPointerButtonKind.Primary
      DeltaX = 0.0
      DeltaY = 0.0 }

/// Centre of a control's computed bounds at `size` (the point a user clicks).
let private centreOf (host: InteractiveAppHost<Model, Msg>) (model: Model) (nodeId: ControlId) =
    let rendered = Control.renderTree host.Theme size (host.View size model)

    let available: FS.GG.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.GG.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.GG.UI.Layout.Exactly }

    let result = FS.GG.UI.Layout.Layout.evaluate available rendered.Layout
    let b = result.Bounds |> List.find (fun b -> b.NodeId = nodeId)
    b.Bounds.X + b.Bounds.Width / 2.0, b.Bounds.Y + b.Bounds.Height / 2.0

/// Drive a synthetic press+release at (x, y) through the real adapter path; return the routed msgs.
let private clickAt (host: InteractiveAppHost<Model, Msg>) (model: Model) (x: float) (y: float) =
    let state1, down = ControlsElmish.routeInteractivePointer host (Pointer.init ()) size model (pointer ViewerPointerPhaseKind.Pressed x y)
    let _state2, up = ControlsElmish.routeInteractivePointer host state1 size model (pointer ViewerPointerPhaseKind.Released x y)
    down @ up

[<Tests>]
let dispatchTests =
    testList
        "Feature 090 interactive host dispatch / proof / text seam"
        [
          // G1/G2 (US1, FR-001/FR-003) — an authored onClick/onChanged fires with ZERO MapPointer
          // clauses authored, and a competing MapPointer clause for the same control does NOT also
          // fire (no double-advance).
          test "authored onClick + onChanged fire with no MapPointer clauses; MapPointer does not double-fire (G1/G2)" {
              // A MapPointer that WOULD also map the same control — must be suppressed when a binding wins.
              let competingMapPointer =
                  fun interaction ->
                      match interaction with
                      | Click("inc", _, _, _) -> Some Mapped
                      | _ -> None

              let host = hostOf boundView competingMapPointer
              let model0 = fst (host.Init ())

              let cx, cy = centreOf host model0 "inc"
              let routed = clickAt host model0 cx cy
              Expect.contains routed Increment "the authored onClick binding fired in the live route"
              Expect.isFalse (List.contains Mapped routed) "the competing MapPointer clause did NOT also fire (authored binding wins, no double-advance)"

              // Fold the routed msgs — the model advances exactly once for the click.
              let model1 = routed |> List.fold (fun m msg -> fst (update msg m)) model0
              Expect.equal model1.Count 1 "the model advanced exactly once (Count = 1)"

              // The onChanged control routes its bound message from a click too.
              let kx, ky = centreOf host model0 "chk"
              let chkRouted = clickAt host model0 kx ky
              Expect.contains chkRouted (Toggled false) "the authored onChanged binding fired from a click"
          }

          // R1/G1 (US2→US1, FR-004) — a click inside a CONTAINER-KEYED composite (key + onClick on the
          // container, unkeyed inner node) routes the container's bound message via nearestAuthored.
          test "container-keyed composite routes its bound message from an inner-node click (R1/G1)" {
              let containerView (_: Size) (model: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ Button.create [ Button.text (string model.Count) ] ]; Attr.on "onClick" Increment ]
                  |> Control.withKey "panel"

              let host = hostOf containerView (fun _ -> None)
              let model0 = fst (host.Init ())
              // Centre of the inner (unkeyed) positional node "0.0" inside the keyed container.
              let cx, cy = centreOf host model0 "0.0"
              let routed = clickAt host model0 cx cy
              Expect.contains routed Increment "a click inside the container-keyed composite routed the container's authored binding via nearestAuthored"
          }

          // G3 (US1, FR-003) — a control with NO authored binding plus a MapPointer clause still routes
          // via MapPointer exactly as today (additive / non-regressive).
          test "no authored binding + a MapPointer clause still routes via MapPointer (G3, additive)" {
              // A view whose keyed control carries NO event binding.
              let plainView (_: Size) (_: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ Button.create [ Button.text "plain" ] |> Control.withKey "plain" ] ]

              let mapPointer =
                  fun interaction ->
                      match interaction with
                      | Click("plain", _, _, _) -> Some Increment
                      | _ -> None

              let host = hostOf plainView mapPointer
              let model0 = fst (host.Init ())
              let cx, cy = centreOf host model0 "plain"
              let routed = clickAt host model0 cx cy
              Expect.contains routed Increment "an unbound control still routes through MapPointer exactly as before"
          }

          // P1/P3 (US3, FR-006) — a responsive host (counter incremented by onClick) yields
          // before ≠ after → Responsive; an inert host (binding dropped) yields before = after → Inert.
          test "responds-proof: Responsive for a live host, Inert for an inert one (P1/P3, SC-004)" {
              let host = hostOf boundView (fun _ -> None)
              let model0 = fst (host.Init ())
              let cx, cy = centreOf host model0 "inc"

              // The `state` param threads the press; the Released sample folds into a Click (4px fold).
              let pressed, _ = ControlsElmish.routeInteractivePointer host (Pointer.init ()) size model0 (pointer ViewerPointerPhaseKind.Pressed cx cy)

              // Responsive: the click increments the counter, so the rendered button label changes.
              let proof = ControlsElmish.captureRespondsProof host pressed size model0 (pointer ViewerPointerPhaseKind.Released cx cy)
              Expect.notEqual proof.Before proof.After "a responsive host changes the rendered output after the click"
              Expect.equal proof.Verdict Responsive "verdict is Responsive (before ≠ after)"

              // Inert: a host whose binding is dropped (no onClick, no MapPointer) cannot respond.
              let inertView (_: Size) (model: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ Button.create [ Button.text (string model.Count) ] |> Control.withKey "inc" ] ]

              let inertHost = hostOf inertView (fun _ -> None)
              let inertPressed, _ = ControlsElmish.routeInteractivePointer inertHost (Pointer.init ()) size model0 (pointer ViewerPointerPhaseKind.Pressed cx cy)
              let inertProof = ControlsElmish.captureRespondsProof inertHost inertPressed size model0 (pointer ViewerPointerPhaseKind.Released cx cy)
              Expect.equal inertProof.Before inertProof.After "an inert host's output is identical before and after"
              Expect.equal inertProof.Verdict Inert "verdict is Inert — the dead-window gate fails"
          }

          // T1/T3 (US4, FR-008) — a keystroke delivered through the 092 retained focus-aware seam
          // reaches the FOCUSED control's RetainedId-keyed text state and not an unfocused one; focus
          // resolves via the retained tree (`resolveFocus`), not the replaced ControlId path.
          test "text seam: a keystroke reaches the focused text control, not an unfocused one (T1/T3)" {
              // Two keyed text controls; both author onChanged so the seam folds the focused one's binding.
              let textView (_: Size) (model: Model) : Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ TextBox.create [ TextBox.value model.Name; TextBox.onChanged SetName ] |> Control.withKey "name"
                              TextBox.create [ TextBox.value "other"; TextBox.onChanged SetName ] |> Control.withKey "other" ] ]

              let host = hostOf textView (fun _ -> None)
              let model0 = fst (host.Init ())
              let r0 = (RetainedRender.init host.Theme size (host.View size model0)).Retained

              // Focus-on-click (T3): a click over "name" resolves to its stable RetainedId via the
              // retained tree's per-node boxes.
              let cx, cy = centreOf host model0 "name"
              let focused = ControlsElmish.resolveFocus r0 cx cy
              Expect.isSome focused "a click on the text control resolves to a RetainedId (focus-on-click)"

              // Deliver 'a' to the focused control through the real seam (no hand-seeded state map).
              let r1, msgs = ControlsElmish.routeFocusedText r0 focused (InsertText "a")

              let focusedState = r1.StateByIdentity |> Map.find focused.Value
              Expect.equal focusedState.Text.Value.DraftText "a" "the character reached the FOCUSED control's RetainedId-keyed text state"
              Expect.contains msgs (SetName "a") "the focused control's onChanged binding folded the new text into a product message"

              // The other (unfocused) control acquired no text state — only the focused id advances.
              Expect.equal (Map.count r1.StateByIdentity) 1 "exactly one control (the focused one) has text state; the unfocused control is untouched"
          }

          // T1 (US4) — when nothing is focused, the seam delivers nothing (the host's MapKey path is
          // left to handle the key) and the retained structure is returned unchanged.
          test "text seam: with no focus, nothing is delivered and the structure is unchanged (T1)" {
              let textView (_: Size) (_: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ TextBox.create [ TextBox.onChanged SetName ] |> Control.withKey "name" ] ]

              let host = hostOf textView (fun _ -> None)
              let model0 = fst (host.Init ())
              let r0 = (RetainedRender.init host.Theme size (host.View size model0)).Retained
              let r1, msgs = ControlsElmish.routeFocusedText r0 None (InsertText "a")
              Expect.isEmpty msgs "no focus ⇒ no product message"
              Expect.isTrue (Map.isEmpty r1.StateByIdentity) "no focus ⇒ the retained structure is unchanged"
          } ]
