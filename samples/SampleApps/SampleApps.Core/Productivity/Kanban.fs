/// Kanban board — columns with pointer-driven card movement + inline edit (US3,
/// data-model.md). Cards are added by keyboard into the first column, advanced between columns
/// by a pointer click (the pointer modality this sample contributes to coverage), and inline-
/// edited in place. An empty board renders the empty-state. Pure MVU; `mapKey` is model-free,
/// so draft-vs-edit routing happens in `update`.
module SampleApps.Core.Productivity.Kanban

open System
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open SampleApps.Core
open SampleApps.Core.Evidence

let private columnNames = [ "Todo"; "Doing"; "Done" ]

type Card = { Id: int; Title: string }
type EditState = { Id: int; Buffer: string }

/// The model. `Columns` is index-aligned with `columnNames`. `Moved` counts pointer card
/// advances (disclosed in the outcome).
type Model =
    { Columns: Card list list
      Draft: string
      Errors: string list
      NextId: int
      Moved: int
      Edit: EditState option }

type Msg =
    | KeyChar of char
    | KeyBackspace
    | KeyEnter
    | KeyEdit
    | MoveForward

let validate (text: string): Result<string, string list> =
    let trimmed = text.Trim()
    if trimmed = "" then Result.Error [ "card title is required" ] else Result.Ok trimmed

let private dropLast (s: string): string =
    if s = "" then "" else s.Substring(0, s.Length - 1)

let init: Model =
    { Columns = List.replicate (List.length columnNames) []
      Draft = ""
      Errors = []
      NextId = 1
      Moved = 0
      Edit = None }

/// Replace column `i` via a function.
let private mapColumn (i: int) (f: Card list -> Card list) (cols: Card list list): Card list list =
    cols |> List.mapi (fun idx c -> if idx = i then f c else c)

/// All cards across the board (for editing the frontmost card).
let private firstCard (model: Model): Card option =
    model.Columns |> List.tryPick (function | c :: _ -> Some c | [] -> None)

let update (msg: Msg) (model: Model): Model =
    match msg with
    | KeyChar c ->
        match model.Edit with
        | Some e -> { model with Edit = Some { e with Buffer = e.Buffer + string c } }
        | None -> { model with Draft = model.Draft + string c; Errors = [] }
    | KeyBackspace ->
        match model.Edit with
        | Some e -> { model with Edit = Some { e with Buffer = dropLast e.Buffer } }
        | None -> { model with Draft = dropLast model.Draft }
    | KeyEnter ->
        match model.Edit with
        | Some e ->
            match validate e.Buffer with
            | Result.Ok title ->
                { model with
                    Columns = model.Columns |> List.map (List.map (fun c -> if c.Id = e.Id then { c with Title = title } else c))
                    Edit = None
                    Errors = [] }
            | Result.Error errs -> { model with Errors = errs }
        | None ->
            match validate model.Draft with
            | Result.Ok title ->
                { model with
                    Columns = mapColumn 0 (fun c -> c @ [ { Id = model.NextId; Title = title } ]) model.Columns
                    NextId = model.NextId + 1
                    Draft = ""
                    Errors = [] }
            | Result.Error errs -> { model with Errors = errs }
    | KeyEdit ->
        match firstCard model with
        | Some card -> { model with Edit = Some { Id = card.Id; Buffer = card.Title } }
        | None -> model
    | MoveForward ->
        // advance the most-advanced movable card: Doing->Done first, else Todo->Doing.
        match model.Columns with
        | [ todo; doing; done' ] ->
            match doing with
            | card :: rest -> { model with Columns = [ todo; rest; done' @ [ card ] ]; Moved = model.Moved + 1 }
            | [] ->
                match todo with
                | card :: rest -> { model with Columns = [ rest; doing @ [ card ]; done' ]; Moved = model.Moved + 1 }
                | [] -> model
        | _ -> model

let mapKey (key: ViewerKey) (pressed: bool): Msg option =
    if not pressed then
        None
    else
        match key with
        | Letter c -> Some(KeyChar c)
        | Backspace -> Some KeyBackspace
        | Enter -> Some KeyEnter
        | ArrowUp -> Some KeyEdit
        | _ -> None

/// A primary click advances the frontmost card — the pointer modality the coverage row claims.
let mapPointer (interaction: PointerInteraction): Msg option =
    match interaction with
    | Click _ -> Some MoveForward
    | _ -> None

let tick (_dt: TimeSpan): Msg option = None

let view (_size: Size) (model: Model): Control<Msg> =
    let totalCards = model.Columns |> List.sumBy List.length
    let columnView (name: string) (cards: Card list) =
        let cardViews =
            if List.isEmpty cards then
                [ TextBlock.create [ TextBlock.text "(empty)" ] ]
            else
                cards |> List.map (fun c -> Border.create [ Border.child (TextBlock.create [ TextBlock.text c.Title ]) ])
        Stack.create
            [ Stack.orientation "vertical"
              Stack.children (Label.create [ Label.text name ] :: cardViews) ]
    let board =
        if totalCards = 0 then
            [ TextBlock.create [ TextBlock.text "No cards yet — type a title and press Enter to add one to Todo." ] ]
        else
            [ Stack.create
                  [ Stack.orientation "horizontal"
                    Stack.children (List.map2 columnView columnNames model.Columns) ] ]
    let draftRow =
        Stack.create
            [ Stack.orientation "horizontal"
              Stack.children
                  [ TextBox.create [ TextBox.value model.Draft; TextBox.onChanged (fun _ -> KeyEnter) ]
                    Button.create [ Button.text "Add to Todo"; Button.onClick KeyEnter ] ] ]
    Stack.create [ Stack.orientation "vertical"; Stack.children (Label.create [ Label.text "Kanban" ] :: draftRow :: board) ]

// --- scene rendering (real graphics via the public Scene primitives) ----------------

let private frameW = 600.0
let private frameH = 420.0
let private headerPx = 38.0
let private col_text = Colors.rgb 226uy 232uy 240uy
let private col_dim = Colors.rgb 148uy 163uy 184uy
let private col_panel = Colors.rgb 30uy 41uy 59uy

/// Paint the three columns side by side, each card a rounded panel with its title, plus the
/// draft line. Real `Scene` graphics.
let renderScene (_size: Size) (model: Model): Scene =
    let total = model.Columns |> List.sumBy List.length
    let gap = 10.0
    let colW = (frameW - gap * 4.0) / 3.0
    let header =
        [ Scene.rectangle (0.0, 0.0, frameW, frameH) (Colors.rgb 17uy 24uy 39uy)
          Scene.sizedText (12.0, 25.0) (sprintf "KANBAN   %d cards   %d done   %d moved" total (match model.Columns with | [ _; _; d ] -> List.length d | _ -> 0) model.Moved) 16.0 col_text ]
    let columns =
        model.Columns
        |> List.mapi (fun ci cards ->
            let x = gap + float ci * (colW + gap)
            let colBg = Scene.rectangle (x, headerPx, colW, frameH - headerPx - 50.0) (Colors.rgb 23uy 31uy 47uy)
            let colTitle = Scene.sizedText (x + 8.0, headerPx + 22.0) (List.item ci columnNames) 14.0 col_dim
            let cardViews =
                if List.isEmpty cards then
                    [ Scene.sizedText (x + 8.0, headerPx + 50.0) "(empty)" 12.0 col_dim ]
                else
                    cards
                    |> List.mapi (fun i card ->
                        let cy = headerPx + 36.0 + float i * 40.0
                        [ Scene.rectangle (x + 6.0, cy, colW - 12.0, 32.0) col_panel
                          Scene.sizedText (x + 14.0, cy + 21.0) card.Title 13.0 col_text ])
                    |> List.concat
            colBg :: colTitle :: cardViews)
        |> List.concat
    let draftLabel = match model.Edit with | Some e -> sprintf "editing: %s" e.Buffer | None -> (if model.Draft = "" then "type a card title, ENTER adds to Todo (CLICK advances)" else model.Draft)
    let draft =
        [ Scene.rectangle (gap, frameH - 40.0, frameW - gap * 2.0, 30.0) col_panel
          Scene.sizedText (gap + 8.0, frameH - 19.0) draftLabel 13.0 (if model.Edit.IsNone && model.Draft = "" then col_dim else col_text) ]
    Scene.group (header @ columns @ draft)

// --- evidence wiring ----------------------------------------------------------------

let deriveOutcome (model: Model): ExpectedOutcome =
    let total = model.Columns |> List.sumBy List.length
    let doneCount = match model.Columns with | [ _; _; d ] -> List.length d | _ -> 0
    { Kind = "productivity"
      Values =
        [ "cards", string total
          "done", string doneCount
          "moved", string model.Moved ] }

let private noMods: KeyModifiers = { Ctrl = false; Alt = false; Shift = false; Meta = false }
let private press (k: ViewerKey): FrameInput<Msg> = FrameInput.Key(k, noMods)
let private clickAt: FrameInput<Msg> = FrameInput.Pointer(Click("board", PointerButton.Primary, 0.0, 0.0))
let private typeText (s: string): FrameInput<Msg> list = [ for c in s -> press (Letter c) ]

/// A seeded script: add two cards, advance one fully to Done via two clicks, then inline-edit
/// the remaining card.
let script: FrameInput<Msg> list =
    typeText "alpha" @ [ press Enter ]
    @ typeText "beta" @ [ press Enter ]
    @ [ clickAt; clickAt ]                 // advance the frontmost card Todo->Doing->Done
    @ [ press ArrowUp ] @ typeText "Z" @ [ press Enter ] // inline-edit the remaining card
    @ [ FrameInput.Idle ]

/// Pinned literal (seed-independent — Kanban carries no PRNG).
let expected: ExpectedOutcome =
    { Kind = "productivity"
      Values = [ "cards", "2"; "done", "1"; "moved", "2" ] }

let private hostFor (_seedValue: int) (mode) (accent) =
    Harness.host (fun () -> init) update view mapKey mapPointer tick (SampleTheme.resolve mode accent)

let recordAt (seedValue: int): SampleEvidenceRecord =
    Harness.recordFor "kanban" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue

let entry: Harness.SampleEntry =
    { Id = "kanban"
      Family = "productivity"
      Title = "Kanban board"
      Controls = [ "stack"; "border"; "text-box"; "button"; "label"; "text-block" ]
      Inputs = [ "pointer"; "keyboard" ]
      RunEvidence = fun seedValue outDir -> Harness.evidenceForScene renderScene "kanban" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue outDir
      Interactive = fun _ -> Harness.runInteractiveScene "Kanban — Sample Apps" { Width = int frameW; Height = int frameH } (fun () -> init) update renderScene mapKey tick
      Outcome = expected }
