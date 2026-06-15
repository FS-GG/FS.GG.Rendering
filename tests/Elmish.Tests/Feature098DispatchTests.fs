module Feature098DispatchTests

// Feature 098 (R3) — binding-aware ancestor recovery, exercised through the REAL live-adapter routing
// seam `ControlsElmish.routeInteractivePointer` (the same seam `runInteractiveApp` wires), with real
// control trees and real `Pointer.update` — no mocks. US1 (T009): an UNKEYED authored Button.onClick —
// and a nested unkeyed bound control — now dispatch where the un-widened (key-only) `nearestAuthored`
// returned None (an artifact an un-fixed build cannot produce). US2 (T012): keyed-leaf and
// container-keyed recovery stay byte-identical to 090, and a MapPointer-only consumer is unchanged.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer

type private Msg =
    | Inc
    | Outer
    | Mapped
    | MappedInner

type private Model = { Count: int }

let private size = { Width = 320; Height = 200 }

let private update (msg: Msg) (model: Model) : Model * ViewerEffect list =
    match msg with
    | Inc -> { model with Count = model.Count + 1 }, []
    | _ -> model, []

let private hostOf view mapPointer : InteractiveAppHost<Model, Msg> =
    { Init = fun () -> { Count = 0 }, []
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

/// Centre of a control's computed bounds at `size` (the point a user clicks) — found by the layout
/// NodeId, which is the unified `Key ?? path` id.
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
        "Feature 098 binding-aware recovery via the live routing seam"
        [
          // US1 / AS1 (SC-001) — a single UNKEYED Button.onClick dispatches in the live seam, and a
          // competing MapPointer for the same control does NOT also fire (binding wins, no double-advance).
          test "US1 AS1: an unkeyed Button.onClick dispatches; MapPointer is not consulted" {
              let unkeyedView (_: Size) (_: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ Button.create [ Button.text "go"; Button.onClick Inc ] ] ]

              // A MapPointer that WOULD also map the same unkeyed control's path id — must be suppressed.
              let competing =
                  fun interaction ->
                      match interaction with
                      | Click("0.0", _, _, _) -> Some Mapped
                      | _ -> None

              let host = hostOf unkeyedView competing
              let model0 = fst (host.Init ())
              let cx, cy = centreOf host model0 "0.0"
              let routed = clickAt host model0 cx cy
              Expect.contains routed Inc "the unkeyed onClick binding fired in the live route (dead button is now alive)"
              Expect.isFalse (List.contains Mapped routed) "the competing MapPointer clause did NOT also fire (binding wins)"
          }

          // US1 / AS2 (SC-001) — a nested unkeyed BOUND control inside an UNBOUND, unkeyed container:
          // a Click on the inner control recovers the inner bound node ("0.0.0") and dispatches it.
          test "US1 AS2: a nested unkeyed bound control inside an unbound container dispatches" {
              let nestedView (_: Size) (_: Model) : Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ Stack.create [ Stack.children [ Button.create [ Button.text "deep"; Button.onClick Inc ] ] ] ] ]

              let host = hostOf nestedView (fun _ -> None)
              let model0 = fst (host.Init ())
              let cx, cy = centreOf host model0 "0.0.0"
              let routed = clickAt host model0 cx cy
              Expect.contains routed Inc "a click on the deeply-nested unkeyed bound control recovered it and dispatched"
          }

          // US1 / AS3 (SC-005) — an unkeyed UNBOUND leaf with no bound/keyed ancestor recovers None and
          // falls back to MapPointer exactly as 090 (no spurious binding dispatch).
          test "US1 AS3: an unbound unkeyed leaf recovers None and falls back to MapPointer" {
              let plainView (_: Size) (_: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ Button.create [ Button.text "plain" ] ] ]

              // With a MapPointer, the raw interaction routes through it.
              let mapPointer =
                  fun interaction ->
                      match interaction with
                      | Click("0.0", _, _, _) -> Some Mapped
                      | _ -> None

              let host = hostOf plainView mapPointer
              let model0 = fst (host.Init ())
              let cx, cy = centreOf host model0 "0.0"
              let routed = clickAt host model0 cx cy
              Expect.contains routed Mapped "an unbound leaf falls back to MapPointer with the raw interaction"

              // Without a MapPointer, nothing dispatches — no spurious binding was invented.
              let inertHost = hostOf plainView (fun _ -> None)
              let inertRouted = clickAt inertHost model0 cx cy
              Expect.isEmpty inertRouted "no binding, no MapPointer ⇒ no spurious dispatch (recovery stayed None)"
          }

          // US2 / AS1 (SC-002) — a directly-keyed leaf with a binding resolves to its Key (a fixed point)
          // and dispatches, byte-identical to 090.
          test "US2 AS1: a keyed leaf resolves to its Key and dispatches (unchanged from 090)" {
              let keyedView (_: Size) (_: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ Button.create [ Button.text "go"; Button.onClick Inc ] |> Control.withKey "go" ] ]

              let host = hostOf keyedView (fun _ -> None)
              let model0 = fst (host.Init ())
              let cx, cy = centreOf host model0 "go"
              let routed = clickAt host model0 cx cy
              Expect.contains routed Inc "the keyed leaf's binding fired (fixed point, unchanged)"
          }

          // US2 / AS2 (SC-002) — a container-keyed composite: a Click on an inner UNKEYED, UNBOUND
          // positional node climbs to the keyed container and dispatches the container's binding.
          test "US2 AS2: a container-keyed composite dispatches from an inner unbound-node click" {
              let containerView (_: Size) (_: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ Button.create [ Button.text "label" ] ]; Attr.on "onClick" Outer ]
                  |> Control.withKey "panel"

              let host = hostOf containerView (fun _ -> None)
              let model0 = fst (host.Init ())
              // Centre of the inner unkeyed, unbound positional node "0.0" inside the keyed container.
              let cx, cy = centreOf host model0 "0.0"
              let routed = clickAt host model0 cx cy
              Expect.contains routed Outer "the inner-node click climbed to the keyed container and dispatched its binding"
          }

          // US2 / AS3 (SC-002) — a control with BOTH a Key and a binding: the binding is found by the
          // unified id (the Key), with no double-dispatch.
          test "US2 AS3: a keyed+bound control dispatches exactly once (no double-dispatch)" {
              let view (_: Size) (_: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ Button.create [ Button.text "go"; Button.onClick Inc ] |> Control.withKey "go" ] ]

              let host = hostOf view (fun _ -> None)
              let model0 = fst (host.Init ())
              let cx, cy = centreOf host model0 "go"
              let routed = clickAt host model0 cx cy
              Expect.equal (routed |> List.filter ((=) Inc) |> List.length) 1 "exactly one dispatch for a keyed+bound control (no double-dispatch)"
          }

          // US2 (SC-002, FR-005) — a MapPointer-only consumer (no authored bindings anywhere) is
          // bit-for-bit unchanged: the raw interaction still routes through MapPointer.
          test "US2: a MapPointer-only consumer is bit-for-bit unchanged" {
              let plainView (_: Size) (_: Model) : Control<Msg> =
                  Stack.create [ Stack.children [ Button.create [ Button.text "plain" ] ] ]

              let mapPointer =
                  fun interaction ->
                      match interaction with
                      | Click("0.0", _, _, _) -> Some MappedInner
                      | _ -> None

              let host = hostOf plainView mapPointer
              let model0 = fst (host.Init ())
              let cx, cy = centreOf host model0 "0.0"
              let routed = clickAt host model0 cx cy
              Expect.equal routed [ MappedInner ] "a MapPointer-only consumer routes exactly its mapped message (unchanged)"
          } ]
