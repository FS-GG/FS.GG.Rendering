module Feature191CanvasInputTests

// Feature 191 (US2, T022/T023, C6/FR-006/FR-007) — the embedded `canvas` forwards RAW input to the
// bound model through the real adapter routing seams: `routeInteractivePointer` delivers the raw
// PointerSample (in canvas-local coordinates) to `onPointer` for an in-box pointer and nothing for an
// out-of-box one; `routeFocusedKey` delivers ViewerKey + KeyModifiers to a focused canvas's `onKey`
// and nothing to an unfocused one; the canvas participates in `Focus.order`. No mocks, no live GL.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg =
    | Pointed of PointerSample
    | Keyed of ViewerKey * KeyModifiers
    | Other

type private Model = { Last: int }

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 240 }

let private update (msg: Msg) (model: Model) : Model * ViewerEffect list =
    match msg with
    | _ -> { model with Last = model.Last + 1 }, []

let private demoScene = { Nodes = [ Rectangle((0.0, 0.0, 20.0, 20.0), Colors.black) ] }

// A keyed canvas bound to onPointer + onKey, beside a keyed button (chrome / focus sibling).
let private canvasTree: Control<Msg> =
    Stack.create
        [ Stack.children
              [ Canvas.create
                    [ Attr.width 120.0
                      Attr.height 80.0
                      Canvas.scene demoScene
                      Canvas.onPointer Pointed
                      Canvas.onKey (fun k m -> Keyed(k, m)) ]
                |> Control.withKey "cv"
                Button.create [ Button.text "x"; Button.onClick Other ] |> Control.withKey "btn" ] ]

let private view (_: Size) (_: Model) : Control<Msg> = canvasTree

let private hostOf () : InteractiveAppHost<Model, Msg> =
    { Init = fun () -> { Last = 0 }, []
      Update = update
      View = view
      Theme = theme
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private pointer phase x y : ViewerPointerInput =
    { Phase = phase; X = x; Y = y; Button = Some ViewerPointerButtonKind.Primary; DeltaX = 0.0; DeltaY = 0.0 }

let private canvasBox () : Rect =
    let rendered = Control.renderTree theme size canvasTree
    rendered.Bounds |> List.find (fun (id, _) -> id = "cv") |> snd

let private pointedSamples (msgs: Msg list) = msgs |> List.choose (function Pointed s -> Some s | _ -> None)

// --- key routing scaffolding (mirrors Feature094) ---
let private rinit (c: Control<Msg>) : RetainedRender<Msg> = (RetainedRender.init theme size c).Retained

let rec private findByKey (key: ControlId) (n: RetainedNode<Msg>) : RetainedNode<Msg> option =
    if n.Control.Key = Some key then Some n else n.Children |> List.tryPick (findByKey key)

let private idOfKey (key: ControlId) (r: RetainedRender<Msg>) : RetainedId option =
    findByKey key r.Root |> Option.map (fun n -> n.Identity)

let private order (r: RetainedRender<Msg>) : TabOrder = Focus.order r.Root.Control

[<Tests>]
let tests =
    testList "Feature 191 canvas raw input forwarding (US2)" [

        // T022 — raw pointer forwarding in canvas-local space; out-of-box dispatches nothing.
        test "an in-box pointer forwards the raw sample (canvas-local) to onPointer; out-of-box does not" {
            let host = hostOf ()
            let model = { Last = 0 }
            let box = canvasBox ()
            let cx, cy = box.X + box.Width / 2.0, box.Y + box.Height / 2.0

            // In-box press → exactly one Pointed, with coordinates resolved to canvas-local space.
            let _, pressMsgs = ControlsElmish.routeInteractivePointer host (Pointer.init ()) size model (pointer ViewerPointerPhaseKind.Pressed cx cy)
            match pointedSamples pressMsgs with
            | [ s ] ->
                Expect.equal s.Phase PointerPhase.Pressed "the raw phase is forwarded"
                Expect.floatClose Accuracy.high s.X (cx - box.X) "x is in canvas-local space"
                Expect.floatClose Accuracy.high s.Y (cy - box.Y) "y is in canvas-local space"
            | other -> failtestf "expected exactly one forwarded pointer sample, got %A" other

            // In-box move and wheel also reach the canvas (independent of the click gesture fold).
            let _, moveMsgs = ControlsElmish.routeInteractivePointer host (Pointer.init ()) size model (pointer ViewerPointerPhaseKind.Moved cx cy)
            Expect.isNonEmpty (pointedSamples moveMsgs) "an in-box move forwards a sample"

            // Out-of-box press → no canvas sample (FR-006).
            let ox = box.X + box.Width + 40.0
            let _, outMsgs = ControlsElmish.routeInteractivePointer host (Pointer.init ()) size model (pointer ViewerPointerPhaseKind.Pressed ox cy)
            Expect.isEmpty (pointedSamples outMsgs) "an out-of-box pointer forwards nothing to the canvas"
        }

        // T023 — raw key forwarding to a focused canvas; unfocused gets nothing; canvas is a focus stop.
        test "a focused canvas forwards ViewerKey + KeyModifiers to onKey; an unfocused canvas does not" {
            let r = rinit canvasTree
            let cv = idOfKey "cv" r
            let btn = idOfKey "btn" r

            let _, _, focusedMsgs = ControlsElmish.routeFocusedKey r cv (order r) (ViewerKey.Letter 'a') true
            match focusedMsgs with
            | [ Keyed(ViewerKey.Letter 'a', mods) ] -> Expect.isTrue mods.Shift "Shift is carried in the modifiers"
            | other -> failtestf "expected one forwarded key with modifiers, got %A" other

            // A different focused control does not deliver to the canvas's onKey.
            let _, _, otherMsgs = ControlsElmish.routeFocusedKey r btn (order r) (ViewerKey.Letter 'a') false
            Expect.isEmpty (otherMsgs |> List.choose (function Keyed _ as m -> Some m | _ -> None)) "an unfocused canvas receives no key"

            // The canvas participates in Focus.order (it is a focus stop).
            let stops = (order r).Stops |> List.map (fun s -> s.Control)
            Expect.isTrue (stops |> List.contains "cv") "the canvas is a focus stop in the tab order"
        }
    ]
