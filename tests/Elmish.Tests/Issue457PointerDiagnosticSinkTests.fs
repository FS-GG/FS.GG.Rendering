module Issue457PointerDiagnosticSinkTests

// Issue #457 — every pointer diagnostic was filtered out by `AdapterCmd.productMessages`.
//
// `interpretPointerEffect` lowers a pointer diagnostic into `ReportAdapterDiagnostic` correctly, but
// every host routing site then piped the command through `AdapterCmd.productMessages`, which keeps
// only `DispatchProductMessage`. The diagnostic was constructed, typed, given a code and a message —
// and dropped on the floor by the one function every routing path went through. The escape hatch the
// silent-no-op family reaches for ("just emit a diagnostic") was itself a silent no-op here.
//
// The consequence, reproduced below: a headless click at a `ControlId` that does not exist was
// COMPLETELY silent — byte-identical to no input at all. A typo'd id produced a test that drives
// nothing, and if its assertion was a negative one ("the screen did not change") it PASSED.
//
// These tests drive the REAL routing paths (`Perf.runScript` — the scripted route the field report
// used — and `routeInteractivePointer`, the live oracle), and assert the diagnostic now REACHES AN
// OBSERVER via `host.Diagnostics.Sink`. The false-positive guards matter as much as the positives: a
// control that legitimately has no binding must NOT be reported, or the channel cries wolf.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg = Bump

let private size: Size = { Width = 320; Height = 200 }

/// Collect everything the adapter reports to the host's diagnostics sink.
let private sinking () =
    let captured = ResizeArray<ViewerDiagnosticEvent>()
    let options = { Viewer.defaultDiagnostics with Sink = Some captured.Add }
    captured, options

/// A Bump-counter host whose keyed button fires `Bump` on click. `MapPointer` declines everything, so
/// an interaction no authored binding consumes dispatches NOTHING — the silence under test.
let private hostWith (options: ViewerDiagnosticsOptions) : InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun Bump model -> model + 1, []
      View =
        fun _ _ ->
            Stack.create [ Stack.children [ Button.create [ Button.text "go"; Button.onClick Bump ] |> Control.withKey "btn" ] ]
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = options }

/// The same tree, but the button carries NO binding: a click on it legitimately resolves to no
/// message. This is the false-positive guard — an unbound control is not a defect.
let private unboundHostWith (options: ViewerDiagnosticsOptions) : InteractiveAppHost<int, Msg> =
    { hostWith options with
        View = fun _ _ -> Stack.create [ Stack.children [ Button.create [ Button.text "go" ] |> Control.withKey "btn" ] ] }

let private pointer phase x y : ViewerPointerInput =
    { Phase = phase
      X = x
      Y = y
      Button = Some ViewerPointerButtonKind.Primary
      DeltaX = 0.0
      DeltaY = 0.0 }

/// Centre of a control's computed bounds at `size` (the point a user clicks).
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

let private unresolved (captured: ResizeArray<ViewerDiagnosticEvent>) =
    captured |> Seq.filter (fun e -> e.Message.Contains "UnresolvedControlId") |> List.ofSeq

[<Tests>]
let tests =
    testList "Issue #457 — a pointer diagnostic reaches an observer instead of being filtered out" [

        // THE REGRESSION TEST. Before the fix this script was byte-identical to no input at all: no
        // message, no model change, no diagnostic, nothing. This is the assertion that "a test that
        // clicks a nonexistent id can be made to fail" — the issue's acceptance criterion.
        test "a scripted click at an id NO control carries is reported, and names the id" {
            let captured, options = sinking ()
            let host = hostWith options
            let cx, cy = centreOf host 0 "btn"

            let frames =
                ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(Click("this-button-does-not-exist", PointerButton.Primary, cx, cy)) ]

            let reported = unresolved captured
            Expect.hasLength reported 1 "the click named an id no control carries — exactly one diagnostic"

            Expect.stringContains
                reported.[0].Message
                "this-button-does-not-exist"
                "the diagnostic NAMES the offending id (the whole point — a code with no id is unactionable)"

            Expect.equal reported.[0].Level ViewerDiagnosticLevel.Warning "a named id that resolves to nothing is a caller defect, not routine"
            Expect.equal reported.[0].Category ViewerDiagnosticCategory.Input "pointer routing is an Input-category diagnostic"
            Expect.equal frames.[0].ProductModelChanged false "the click still dispatched nothing — the SILENCE is what is fixed, not the routing"
        }

        // The field report's sharpest case: a case-typo of a REAL id. Indistinguishable from a working
        // click without this, because `ControlId` is a bare `type ControlId = string` — no compile-time
        // protection whatsoever.
        test "a case-typo of a real id is reported, not silently ignored" {
            let captured, options = sinking ()
            let host = hostWith options
            let cx, cy = centreOf host 0 "btn"

            let frames = ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(Click("BTN", PointerButton.Primary, cx, cy)) ]

            let reported = unresolved captured
            Expect.hasLength reported 1 "'BTN' is not 'btn' — a mis-cased id names no control"
            Expect.stringContains reported.[0].Message "BTN" "the diagnostic names the id as written, so the typo is visible"
            Expect.equal frames.[0].ProductModelChanged false "the mis-cased click drove nothing"
        }

        // FALSE-POSITIVE GUARD. A control with no authored binding is a legitimate, supported shape
        // (`MapPointer` is the fallback for it). Reporting it would make the channel useless noise.
        test "a click on a REAL control with no binding is NOT reported as unresolved" {
            let captured, options = sinking ()
            let host = unboundHostWith options
            let cx, cy = centreOf host 0 "btn"

            let frames = ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(Click("btn", PointerButton.Primary, cx, cy)) ]

            Expect.isEmpty (unresolved captured) "'btn' EXISTS — having no binding is not a defect, and must not be reported as one"
            Expect.equal frames.[0].ProductModelChanged false "an unbound click still dispatches nothing (unchanged)"
        }

        // NO-REGRESSION GUARD: the fix must not disturb the path that works.
        test "a click on a bound control still dispatches, and reports nothing" {
            let captured, options = sinking ()
            let host = hostWith options
            let cx, cy = centreOf host 0 "btn"

            let frames = ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(Click("btn", PointerButton.Primary, cx, cy)) ]

            Expect.isEmpty (unresolved captured) "a click that resolves is not a diagnostic"
            Expect.isTrue frames.[0].ProductModelChanged "the authored binding still fired (routing is unchanged)"
        }

        // THE FALSE-POSITIVE GUARD THAT MATTERS IN PRODUCTION. The tests above hand-write the routed
        // interaction; this one lets the REAL geometric hit-test produce it. A user clicking the
        // container/background — not the button — resolves to a laid-out node with no authored binding.
        // That node must be found in `Bounds`, or every background click in a live app with diagnostics
        // enabled would emit a spurious Warning and bury the real defect this channel exists to report.
        test "a real click that lands on an unbound CONTAINER is NOT reported as unresolved" {
            let captured, options = sinking ()
            let host = hostWith options

            // Bottom-right of the window: inside the laid-out tree, well clear of the button.
            let x, y = float size.Width - 4.0, float size.Height - 4.0

            let state1, _ =
                ControlsElmish.routeInteractivePointer host (Pointer.init ()) size 0 (pointer ViewerPointerPhaseKind.Pressed x y)

            ControlsElmish.routeInteractivePointer host state1 size 0 (pointer ViewerPointerPhaseKind.Released x y)
            |> ignore

            Expect.isEmpty
                (unresolved captured)
                "a real hit-tested click on an unbound container names a control that EXISTS — reporting it would make the channel noise"
        }

        // A hover legitimately names a control that is not in the frame — scripts spell "the pointer is
        // over nothing" exactly that way. Only ACTIVATING interactions are checked, or the channel
        // would cry wolf on every such script.
        test "a hover at an absent id is NOT reported — only activating interactions are checked" {
            let captured, options = sinking ()
            let host = hostWith options

            ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(HoverEnter("outside", -10.0, -10.0)) ]
            |> ignore

            Expect.isEmpty (unresolved captured) "a hover is not an activation — naming an absent id there is not a defect"
        }

        // The OTHER half of the issue: a GENUINE geometric `HitTestMiss`, raised by `Pointer.update` on a
        // real press that lands on no control, was ALSO eaten by `productMessages`. It now reaches the
        // sink — at Info, because clicking empty space is a routine thing a user does, not a defect.
        test "a genuine geometric hit-test miss reaches the sink instead of being filtered out" {
            let captured, options = sinking ()
            let host = hostWith options

            ControlsElmish.routeInteractivePointer host (Pointer.init ()) size 0 (pointer ViewerPointerPhaseKind.Pressed -50.0 -50.0)
            |> ignore

            let misses = captured |> Seq.filter (fun e -> e.Message.Contains "HitTestMiss") |> List.ofSeq
            Expect.isNonEmpty misses "the press resolved to no control — `Pointer.update` raised HitTestMiss and the host must not swallow it"
            Expect.equal misses.[0].Level ViewerDiagnosticLevel.Info "a press on empty space is routine, not a defect"
            Expect.equal misses.[0].Category ViewerDiagnosticCategory.Input "pointer routing is an Input-category diagnostic"
        }

        // A host that observes nothing must behave exactly as it did before the fix.
        test "a host with no diagnostics sink is inert" {
            let host = hostWith { Viewer.defaultDiagnostics with Sink = None }
            let cx, cy = centreOf host 0 "btn"

            let bogus =
                ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(Click("nope", PointerButton.Primary, cx, cy)) ]

            let real =
                ControlsElmish.Perf.runScript host size [ FrameInput.Pointer(Click("btn", PointerButton.Primary, cx, cy)) ]

            Expect.equal bogus.[0].ProductModelChanged false "no sink, no observer — and routing is untouched"
            Expect.isTrue real.[0].ProductModelChanged "the bound click still fires with diagnostics disabled"
        }

        // The missing companion itself: `productMessages` DISCARDS what `diagnostics` keeps. This law is
        // the whole defect in miniature.
        test "AdapterCmd.diagnostics extracts exactly what productMessages discards" {
            let d = ControlsElmish.diagnostic "pointer" "HitTestMiss" "missed"
            let command: AdapterCommand<Msg> = [ DispatchProductMessage Bump; ReportAdapterDiagnostic d ]

            Expect.equal (AdapterCmd.productMessages command) [ Bump ] "productMessages keeps only the product messages"
            Expect.equal (AdapterCmd.diagnostics command) [ d ] "diagnostics keeps the reports productMessages drops"
            Expect.isEmpty (AdapterCmd.diagnostics (AdapterCmd.ofMessage Bump)) "law: diagnostics (ofMessage m) = []"
        }
    ]
