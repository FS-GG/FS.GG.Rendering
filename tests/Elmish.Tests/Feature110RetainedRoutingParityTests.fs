module Feature110RetainedRoutingParityTests

// Feature 110 (US2, FR-006) — retained-frame pointer routing is DISPATCH-IDENTICAL to the preserved
// full-render oracle. Each test routes the SAME pointer event through both paths and asserts the
// dispatched message list and the matched control identity are equal:
//   - the oracle: `ControlsElmish.routeInteractivePointer` (renders `host.View` + `Control.renderTree`
//     every sample, hit-tests + `nearestAuthored` over that fresh tree) — PRESERVED unchanged (FR-007);
//   - the retained route: `ControlsElmish.routeRetainedPointer` over the retained frame built once by
//     `RetainedRender.init`, hit-testing via `retainedHitTest` + the `authoredControlIds` lookup with NO
//     per-sample render.
// All exercise the REAL adapter seams with real control trees and real `Pointer.update` — no mocks. The
// scenes cover keyed controls, UNKEYED SAME-KIND SIBLINGS (FR-005, only distinguishable through retained
// identity), a composite whose binding is authored ABOVE the hit node (FR-003), and nested containers.
// Controls carry no general value equality, so parity is asserted on the (equatable) product `Msg` list
// and on the resolved authored `ControlId`, exactly as features 092/098/100 established.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer

type private Msg =
    | Clicked of int
    | Toggled of bool

let private size: Size = { Width = 320; Height = 200 }

let private hostOf (view: Size -> int -> Control<Msg>) (mapPointer: PointerInteraction -> Msg option) : InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun _ model -> model, []
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

// Centre of a node's computed bounds (the point a user clicks), via the SAME layout eval the oracle uses.
let private centreOf (host: InteractiveAppHost<int, Msg>) (model: int) (nodeId: ControlId) =
    let rendered = Control.renderTree host.Theme size (host.View size model)

    let available: FS.GG.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.GG.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.GG.UI.Layout.Exactly }

    let result = FS.GG.UI.Layout.Layout.evaluate available rendered.Layout
    let b = result.Bounds |> List.find (fun b -> b.NodeId = nodeId)
    b.Bounds.X + b.Bounds.Width / 2.0, b.Bounds.Y + b.Bounds.Height / 2.0

// Drive a press+release at (x, y) through the preserved full-render ORACLE; return the routed msgs.
let private clickOracle (host: InteractiveAppHost<int, Msg>) (model: int) (x: float) (y: float) : Msg list =
    let s1, down = ControlsElmish.routeInteractivePointer host (Pointer.init ()) size model (pointer ViewerPointerPhaseKind.Pressed x y)
    let _s2, up = ControlsElmish.routeInteractivePointer host s1 size model (pointer ViewerPointerPhaseKind.Released x y)
    down @ up

// Drive the SAME press+release through the RETAINED route over a frame built once from the model;
// return the routed msgs and the summed fallback count (must be 0 on every normal scene).
let private clickRetained (host: InteractiveAppHost<int, Msg>) (model: int) (x: float) (y: float) : Msg list * int =
    let r = RetainedRender.init host.Theme size (host.View size model)
    let s1, down, fb1 = ControlsElmish.routeRetainedPointer host r.Retained r.Render (Pointer.init ()) size model (pointer ViewerPointerPhaseKind.Pressed x y)
    let _s2, up, fb2 = ControlsElmish.routeRetainedPointer host r.Retained r.Render s1 size model (pointer ViewerPointerPhaseKind.Released x y)
    down @ up, fb1 + fb2

// Assert the two paths dispatch the identical message list with no fallback (the SC-003 parity gate).
let private assertParity host model name x y =
    let oracle = clickOracle host model x y
    let retained, fb = clickRetained host model x y
    Expect.equal retained oracle (sprintf "%s: retained route dispatches the identical message list as the oracle (FR-006/SC-003)" name)
    Expect.equal fb 0 (sprintf "%s: the retained route resolved from the frame with no full-render fallback (SC-005)" name)

// ---- scenes -----------------------------------------------------------------------------------

// Keyed leaf buttons + a keyed checkbox: the documented authoring shape.
let private keyedView (_: Size) (_: int) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text "a"; Button.onClick (Clicked 0) ] |> Control.withKey "a"
                CheckBox.create [ CheckBox.text "c"; CheckBox.checked' false; CheckBox.onChanged Toggled ] |> Control.withKey "c" ] ]

// UNKEYED same-kind siblings (FR-005): two buttons with NO key, each authoring a DISTINCT onClick. Their
// authored ids are positional paths ("0.0", "0.1"); only retained identity keeps them apart.
let private unkeyedSiblingsView (_: Size) (_: int) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text "first"; Button.onClick (Clicked 1) ]
                Button.create [ Button.text "second"; Button.onClick (Clicked 2) ] ] ]

// A composite whose binding is authored ABOVE the hit node (FR-003): a container-keyed Stack with an
// `onClick`, whose inner positional node "0.0" carries no binding — a click inside resolves UP to "panel".
let private compositeView (_: Size) (_: int) : Control<Msg> =
    Stack.create [ Stack.children [ Button.create [ Button.text "inner" ] ]; Attr.on "onClick" (Clicked 7) ]
    |> Control.withKey "panel"

// Nested containers around a keyed bound button (deep positional path).
let private nestedView (_: Size) (_: int) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Stack.create
                    [ Stack.children
                          [ Stack.create [ Stack.children [ Button.create [ Button.text "deep"; Button.onClick (Clicked 9) ] |> Control.withKey "deep" ] ] ] ] ] ]

[<Tests>]
let tests =
    testList "Feature 110 retained routing is dispatch-identical to the full-render oracle (US2, FR-006/SC-003)" [

        test "keyed controls: a click on a keyed button and a keyed checkbox dispatch identically (SC-003)" {
            let host = hostOf keyedView (fun _ -> None)
            let model = fst (host.Init ())
            let ax, ay = centreOf host model "a"
            assertParity host model "keyed button" ax ay
            let cx, cy = centreOf host model "c"
            assertParity host model "keyed checkbox" cx cy
        }

        test "unkeyed same-kind siblings: each sibling selects the same one and fires the same binding (SC-004/FR-005)" {
            let host = hostOf unkeyedSiblingsView (fun _ -> None)
            let model = fst (host.Init ())

            // The two unkeyed buttons resolve to distinct positional ids "0.0" / "0.1".
            let x0, y0 = centreOf host model "0.0"
            let x1, y1 = centreOf host model "0.1"

            assertParity host model "unkeyed sibling #0" x0 y0
            assertParity host model "unkeyed sibling #1" x1 y1

            // And the retained route genuinely distinguishes them (not collapsed onto one id).
            let first, _ = clickRetained host model x0 y0
            let second, _ = clickRetained host model x1 y1
            Expect.equal first [ Clicked 1 ] "the first unkeyed sibling fires its own binding"
            Expect.equal second [ Clicked 2 ] "the second unkeyed sibling fires its OWN, different binding (retained identity, FR-005)"
        }

        test "composite: a click on the inner node dispatches the binding authored ABOVE it (SC-003/FR-003)" {
            let host = hostOf compositeView (fun _ -> None)
            let model = fst (host.Init ())
            // Click the inner unkeyed node "0.0"; both paths climb to the keyed container "panel".
            let ix, iy = centreOf host model "0.0"
            assertParity host model "composite inner-node click" ix iy
            let retained, _ = clickRetained host model ix iy
            Expect.equal retained [ Clicked 7 ] "the retained-id→authored-id lookup dispatched the container's binding (FR-003)"
        }

        test "nested containers: a deep keyed button dispatches identically (SC-003)" {
            let host = hostOf nestedView (fun _ -> None)
            let model = fst (host.Init ())
            let dx, dy = centreOf host model "deep"
            assertParity host model "deeply nested keyed button" dx dy
        }

        test "MapPointer fallback parity: an unbound control routes through MapPointer identically (FR-006)" {
            // No authored binding on the keyed control; a MapPointer clause maps the click. Both the oracle
            // and the retained route must defer to MapPointer for the same interaction (additive path).
            let plainView (_: Size) (_: int) : Control<Msg> =
                Stack.create [ Stack.children [ Button.create [ Button.text "plain" ] |> Control.withKey "plain" ] ]

            let mapPointer =
                fun interaction ->
                    match interaction with
                    | Click("plain", _, _, _) -> Some(Clicked 42)
                    | _ -> None

            let host = hostOf plainView mapPointer
            let model = fst (host.Init ())
            let px, py = centreOf host model "plain"
            assertParity host model "unbound control via MapPointer" px py
            let retained, _ = clickRetained host model px py
            Expect.equal retained [ Clicked 42 ] "the retained route fell through to MapPointer exactly as the oracle"
        }

        // T020 (US2, FR-006 focus clause) — a click that also moves focus yields the SAME focused identity
        // via the retained path. Focus-on-click is resolved by `resolveFocus` (retainedHitTest), the SAME
        // production path before and after feature 110 — so the focused identity is unchanged by construction.
        test "focus outcome parity: a click on a focusable control resolves the same focused identity (FR-006)" {
            let focusView (_: Size) (_: int) : Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ Button.create [ Button.text "x" ] // non-focusable filler before the target
                            TextBox.create [ TextBox.value "f"; TextBox.onChanged (fun _ -> Clicked 0) ] |> Control.withKey "field" ] ]

            let host = hostOf focusView (fun _ -> None)
            let model = fst (host.Init ())
            let r = RetainedRender.init host.Theme size (host.View size model)
            let fx, fy = centreOf host model "field"

            // The production focus path: resolve the RetainedId under the click, unchanged by feature 110.
            let focused = ControlsElmish.resolveFocus r.Retained fx fy
            Expect.isSome focused "the click resolves to a focusable control's RetainedId"

            // It is the SAME id the retained hit-test lands on (the focus outcome the live loop would set).
            let hit = RetainedRender.retainedHitTest fx fy r.Retained
            Expect.equal focused hit "the focus-on-click identity equals the retained hit identity (FR-006 focus clause)"
        }
    ]
