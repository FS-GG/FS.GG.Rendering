/// Todo / task manager — the productivity MVP (US2, data-model.md, research R8). A pure MVU
/// core with form validation that REJECTS invalid input without committing it (FR-004/SC-007),
/// a list with inline edit that commits to the data model, and a defined empty-state. Keyboard
/// is the input the headless script drives; `mapKey` cannot see the model, so it emits
/// key-level messages that `update` resolves against the draft/edit state. `validate` is a pure,
/// directly-testable `Result`-typed function (the validate-or-reject seam).
module SampleApps.Core.Productivity.Todo

open System
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open SampleApps.Core
open SampleApps.Core.Evidence

/// A committed task.
type TodoItem = { Id: int; Title: string; Done: bool }

/// An in-progress inline edit of an existing item.
type EditState = { Id: int; Buffer: string }

/// The MVU model. `Draft` is the new-item field; `Edit` is the inline-edit buffer; `Rejected`
/// counts validation rejections (disclosed in the outcome). An empty `Items` renders the
/// empty-state.
type Model =
    { Items: TodoItem list
      Draft: string
      Errors: string list
      NextId: int
      Rejected: int
      Edit: EditState option }

/// Key-level messages (a keypress carries no model context, so the draft-vs-edit routing
/// happens in `update`).
type Msg =
    | KeyChar of char
    | KeyBackspace
    | KeyEnter
    | KeySpace
    | KeyEdit

/// The validate-or-reject seam (FR-004/R8). A title must be non-blank and ≤ 40 chars. Pure
/// and total — the suite asserts it directly.
// `Result.Ok`/`Result.Error` are qualified throughout: opening `FS.GG.UI.Controls` brings a
// `ControlDiagnosticSeverity.Error` case into scope that would otherwise shadow the F# Result
// constructors/patterns.
let validate (text: string): Result<string, string list> =
    let trimmed = text.Trim()
    if trimmed = "" then Result.Error [ "title is required" ]
    elif trimmed.Length > 40 then Result.Error [ "title must be 40 characters or fewer" ]
    else Result.Ok trimmed

let private dropLast (s: string): string =
    if s = "" then "" else s.Substring(0, s.Length - 1)

/// The empty starting model — no items, so the view shows the empty-state.
let init: Model =
    { Items = []
      Draft = ""
      Errors = []
      NextId = 1
      Rejected = 0
      Edit = None }

/// Pure reducer. `AddItem`/commit only mutate the data on `Ok`; an invalid draft is rejected
/// (error surfaced, `Rejected` incremented) and never committed (FR-004/SC-007).
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
                    Items = model.Items |> List.map (fun i -> if i.Id = e.Id then { i with Title = title } else i)
                    Edit = None
                    Errors = [] }
            | Result.Error errs -> { model with Errors = errs } // stay in edit; nothing committed
        | None ->
            match validate model.Draft with
            | Result.Ok title ->
                { model with
                    Items = model.Items @ [ { Id = model.NextId; Title = title; Done = false } ]
                    NextId = model.NextId + 1
                    Draft = ""
                    Errors = [] }
            | Result.Error errs -> { model with Errors = errs; Rejected = model.Rejected + 1 } // rejected, not committed
    | KeySpace ->
        match model.Items with
        | _ :: _ -> { model with Items = model.Items |> List.mapi (fun idx i -> if idx = 0 then { i with Done = not i.Done } else i) }
        | [] -> model
    | KeyEdit ->
        match model.Items with
        | first :: _ -> { model with Edit = Some { Id = first.Id; Buffer = first.Title } }
        | [] -> model

/// Map a key to a low-level message. Letters/Backspace edit text; Enter commits; Space toggles
/// the first item; Up begins an inline edit of the first item.
let mapKey (key: ViewerKey) (pressed: bool): Msg option =
    if not pressed then
        None
    else
        match key with
        | Letter c -> Some(KeyChar c)
        | Backspace -> Some KeyBackspace
        | Enter -> Some KeyEnter
        | Space -> Some KeySpace
        | ArrowUp -> Some KeyEdit
        | _ -> None

/// A primary click toggles the first item — the pointer modality the coverage row declares.
let mapPointer (interaction: PointerInteraction): Msg option =
    match interaction with
    | Click _ -> Some KeySpace
    | _ -> None

/// Todo has no game clock.
let tick (_dt: TimeSpan): Msg option = None

// --- view ---------------------------------------------------------------------------

/// Render the draft field + the task list (checkbox + title), or the empty-state when there
/// are no items. Coverage: `text-box` (draft), `check-box` (done), `label`/`text-block`,
/// `button`, `stack`.
let view (_size: Size) (model: Model): Control<Msg> =
    let draftField = TextBox.create [ TextBox.value model.Draft; TextBox.onChanged (fun _ -> KeyEnter) ]
    let addButton = Button.create [ Button.text "Add"; Button.onClick KeyEnter ]
    let errorLine =
        match model.Errors with
        | [] -> []
        | errs -> [ TextBlock.create [ TextBlock.text (sprintf "⚠ %s" (String.concat "; " errs)) ] ]
    let body =
        if List.isEmpty model.Items then
            [ TextBlock.create [ TextBlock.text "No tasks yet — type a title and press Enter to add one." ] ]
        else
            model.Items
            |> List.map (fun item ->
                Stack.create
                    [ Stack.orientation "horizontal"
                      Stack.children
                          [ CheckBox.create [ CheckBox.text item.Title; CheckBox.checked' item.Done; CheckBox.onChanged (fun _ -> KeySpace) ]
                            Label.create [ Label.text (if item.Done then "done" else "open") ] ] ])
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children (
              [ Label.create [ Label.text "Tasks" ]
                Stack.create [ Stack.orientation "horizontal"; Stack.children [ draftField; addButton ] ] ]
              @ errorLine
              @ body
          ) ]

// --- scene rendering (real graphics via the public Scene primitives) ----------------

let private frameW = 460.0
let private frameH = 460.0
let private headerPx = 38.0
let private col_text = Colors.rgb 226uy 232uy 240uy
let private col_dim = Colors.rgb 148uy 163uy 184uy
let private col_panel = Colors.rgb 30uy 41uy 59uy
let private col_green = Colors.rgb 34uy 197uy 94uy
let private col_red = Colors.rgb 239uy 68uy 68uy

/// Paint the task manager: a draft field, a hint line, validation errors, and the task list
/// (a checkbox square + title per row), or the empty-state. Real `Scene` graphics.
let renderScene (_size: Size) (model: Model): Scene =
    let doneCount = model.Items |> List.filter (fun i -> i.Done) |> List.length
    let header =
        [ Scene.rectangle (0.0, 0.0, frameW, frameH) (Colors.rgb 17uy 24uy 39uy)
          Scene.sizedText (12.0, 25.0) (sprintf "TODO   %d tasks   %d done" (List.length model.Items) doneCount) 16.0 col_text ]
    let draftLabel = match model.Edit with | Some e -> sprintf "editing: %s" e.Buffer | None -> (if model.Draft = "" then "type a title and press ENTER" else model.Draft)
    let draftIsPlaceholder = model.Edit.IsNone && model.Draft = ""
    let draft =
        [ Scene.rectangle (12.0, headerPx + 6.0, frameW - 24.0, 30.0) col_panel
          Scene.sizedText (20.0, headerPx + 27.0) draftLabel 14.0 (if draftIsPlaceholder then col_dim else col_text) ]
    let hint = [ Scene.sizedText (12.0, headerPx + 58.0) "ENTER=add   UP=edit first   SPACE=toggle first" 11.0 col_dim ]
    let errs =
        match model.Errors with
        | [] -> []
        | e -> [ Scene.sizedText (12.0, headerPx + 78.0) ("! " + String.concat "; " e) 12.0 col_red ]
    let listTop = headerPx + 98.0
    let body =
        if List.isEmpty model.Items then
            [ Scene.sizedText (12.0, listTop + 8.0) "No tasks yet - type a title and press ENTER." 14.0 col_dim ]
        else
            model.Items
            |> List.mapi (fun i item ->
                let y = listTop + float i * 30.0
                [ Scene.rectangle (16.0, y, 18.0, 18.0) (if item.Done then col_green else Colors.rgb 51uy 65uy 85uy)
                  Scene.sizedText (44.0, y + 15.0) item.Title 14.0 (if item.Done then col_dim else col_text) ])
            |> List.concat
    Scene.group (header @ draft @ hint @ errs @ body)

// --- evidence wiring ----------------------------------------------------------------

/// The achieved acceptance outcome: committed vs rejected vs completed counts.
let deriveOutcome (model: Model): ExpectedOutcome =
    let completed = model.Items |> List.filter (fun i -> i.Done) |> List.length
    { Kind = "productivity"
      Values =
        [ "committed", string (List.length model.Items)
          "rejected", string model.Rejected
          "completed", string completed ] }

let private noMods: KeyModifiers = { Ctrl = false; Alt = false; Shift = false; Meta = false }
let private press (k: ViewerKey): FrameInput<Msg> = FrameInput.Key(k, noMods)
let private typeText (s: string): FrameInput<Msg> list = [ for c in s -> press (Letter c) ]

/// A seeded script: add a valid item, attempt an invalid (empty) add → rejected, add another
/// valid item, toggle the first complete, then inline-edit the first item and commit.
let script: FrameInput<Msg> list =
    typeText "milk"
    @ [ press Enter ]                       // commit "milk"
    @ [ press Enter ]                       // empty draft -> rejected
    @ typeText "walk"
    @ [ press Enter ]                       // commit "walk"
    @ [ press Space ]                       // toggle first item complete
    @ [ press ArrowUp ]                     // begin inline edit of the first item
    @ typeText "X"
    @ [ press Enter ]                       // commit the inline edit
    @ [ FrameInput.Idle ]

/// The authored acceptance outcome (pinned literal; seed-independent — Todo carries no PRNG).
let expected: ExpectedOutcome =
    { Kind = "productivity"
      Values = [ "committed", "2"; "rejected", "1"; "completed", "1" ] }

let private hostFor (_seedValue: int) (mode) (accent) =
    Harness.host (fun () -> init) update view mapKey mapPointer tick (SampleTheme.resolve mode accent)

/// A pure record for the suites (no disk, no GL).
let recordAt (seedValue: int): SampleEvidenceRecord =
    Harness.recordFor "todo" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue

let entry: Harness.SampleEntry =
    { Id = "todo"
      Family = "productivity"
      Title = "Todo / task manager"
      Controls = [ "stack"; "text-box"; "check-box"; "label"; "button"; "text-block" ]
      Inputs = [ "keyboard"; "pointer" ]
      RunEvidence = fun seedValue outDir -> Harness.evidenceForScene renderScene "todo" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue outDir
      Interactive = fun _ -> Harness.runInteractiveScene "Todo — Sample Apps" { Width = int frameW; Height = int frameH } (fun () -> init) update renderScene mapKey tick
      Outcome = expected }
