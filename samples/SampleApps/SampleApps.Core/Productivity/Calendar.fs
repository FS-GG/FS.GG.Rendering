/// Calendar scheduler — a date-grid with day navigation + validated entry creation (US3,
/// data-model.md). Arrow keys move the selection across a 28-day month grid; typing + Enter
/// adds an entry to the selected day, rejecting blank input without committing it. Pure MVU;
/// `mapKey` is model-free, so the routing happens in `update`.
module SampleApps.Core.Productivity.Calendar

open System
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open SampleApps.Core
open SampleApps.Core.Evidence

let private days = 28 // a fixed 4-week month grid (7 columns × 4 rows)
let private cols = 7

/// The model. `Entries` maps a day (1..28) to its entries; `Rejected` counts blocked adds.
type Model =
    { Selected: int
      Entries: Map<int, string list>
      Draft: string
      Errors: string list
      Rejected: int }

type Msg =
    | PrevDay
    | NextDay
    | KeyChar of char
    | KeyBackspace
    | AddEntry

let validate (text: string): Result<string, string list> =
    let trimmed = text.Trim()
    if trimmed = "" then Result.Error [ "entry text is required" ] else Result.Ok trimmed

let private dropLast (s: string): string =
    if s = "" then "" else s.Substring(0, s.Length - 1)

let init: Model =
    { Selected = 1; Entries = Map.empty; Draft = ""; Errors = []; Rejected = 0 }

let private clamp v = max 1 (min days v)

let update (msg: Msg) (model: Model): Model =
    match msg with
    | PrevDay -> { model with Selected = clamp (model.Selected - 1) }
    | NextDay -> { model with Selected = clamp (model.Selected + 1) }
    | KeyChar c -> { model with Draft = model.Draft + string c; Errors = [] }
    | KeyBackspace -> { model with Draft = dropLast model.Draft }
    | AddEntry ->
        match validate model.Draft with
        | Result.Ok text ->
            let existing = model.Entries |> Map.tryFind model.Selected |> Option.defaultValue []
            { model with
                Entries = model.Entries |> Map.add model.Selected (existing @ [ text ])
                Draft = ""
                Errors = [] }
        | Result.Error errs -> { model with Errors = errs; Rejected = model.Rejected + 1 }

let mapKey (key: ViewerKey) (pressed: bool): Msg option =
    if not pressed then
        None
    else
        match key with
        | ArrowLeft -> Some PrevDay
        | ArrowRight -> Some NextDay
        | Letter c -> Some(KeyChar c)
        | Backspace -> Some KeyBackspace
        | Enter -> Some AddEntry
        | _ -> None

/// A primary click adds the current draft to the selected day — the pointer modality claimed.
let mapPointer (interaction: PointerInteraction): Msg option =
    match interaction with
    | Click _ -> Some AddEntry
    | _ -> None

let tick (_dt: TimeSpan): Msg option = None

let view (_size: Size) (model: Model): Control<Msg> =
    let cell (day: int) =
        let marker = if model.Entries.ContainsKey day then "•" else " "
        let label =
            if day = model.Selected then sprintf "[%d]%s" day marker else sprintf " %d %s" day marker
        TextBlock.create [ TextBlock.text label ]
    let grid =
        Grid.create [ Grid.children [ for d in 1 .. days -> cell d ] ]
    let dayEntries =
        model.Entries
        |> Map.tryFind model.Selected
        |> Option.defaultValue []
        |> List.map (fun e -> Label.create [ Label.text e ])
    let entryList =
        if List.isEmpty dayEntries then [ TextBlock.create [ TextBlock.text "No entries for this day." ] ] else dayEntries
    let draftRow =
        Stack.create
            [ Stack.orientation "horizontal"
              Stack.children
                  [ TextBox.create [ TextBox.value model.Draft; TextBox.onChanged (fun _ -> AddEntry) ]
                    Button.create [ Button.text "Add entry"; Button.onClick AddEntry ] ] ]
    let errorLine =
        match model.Errors with
        | [] -> []
        | errs -> [ TextBlock.create [ TextBlock.text (sprintf "⚠ %s" (String.concat "; " errs)) ] ]
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children (
              [ Label.create [ Label.text (sprintf "Calendar — day %d" model.Selected) ]; grid; draftRow ]
              @ errorLine
              @ entryList
          ) ]

// --- scene rendering (real graphics via the public Scene primitives) ----------------

let private frameW = 460.0
let private frameH = 470.0
let private headerPx = 38.0
let private col_text = Colors.rgb 226uy 232uy 240uy
let private col_dim = Colors.rgb 148uy 163uy 184uy
let private col_panel = Colors.rgb 30uy 41uy 59uy
let private col_sel = Colors.rgb 99uy 102uy 241uy
let private col_red = Colors.rgb 239uy 68uy 68uy

/// Paint the 7×4 month grid (selected day highlighted, a dot when a day has entries), the
/// selected day's entry list, and the draft line. Real `Scene` graphics.
let renderScene (_size: Size) (model: Model): Scene =
    let committed = model.Entries |> Map.toList |> List.sumBy (fun (_, es) -> List.length es)
    let cw, ch, gap = 60.0, 44.0, 2.0
    let gridX, gridY = 12.0, headerPx + 8.0
    let header =
        [ Scene.rectangle (0.0, 0.0, frameW, frameH) (Colors.rgb 17uy 24uy 39uy)
          Scene.sizedText (12.0, 25.0) (sprintf "CALENDAR   day %d   %d entries   %d rejected" model.Selected committed model.Rejected) 15.0 col_text ]
    let cells =
        [ for day in 1 .. days do
              let col = (day - 1) % cols
              let row = (day - 1) / cols
              let x = gridX + float col * (cw + gap)
              let y = gridY + float row * (ch + gap)
              let hasEntries = model.Entries.ContainsKey day
              yield Scene.rectangle (x, y, cw, ch) (if day = model.Selected then col_sel else col_panel)
              yield Scene.sizedText (x + 8.0, y + 22.0) (string day) 14.0 col_text
              if hasEntries then
                  yield Scene.rectangle (x + cw - 14.0, y + 8.0, 6.0, 6.0) (Colors.rgb 34uy 197uy 94uy) ]
    let listTop = gridY + 4.0 * (ch + gap) + 14.0
    let entries =
        model.Entries |> Map.tryFind model.Selected |> Option.defaultValue []
    let entryViews =
        if List.isEmpty entries then
            [ Scene.sizedText (12.0, listTop + 8.0) (sprintf "No entries for day %d." model.Selected) 13.0 col_dim ]
        else
            entries
            |> List.mapi (fun i e -> Scene.sizedText (12.0, listTop + 8.0 + float i * 22.0) (sprintf "- %s" e) 13.0 col_text)
    let errs =
        match model.Errors with
        | [] -> []
        | e -> [ Scene.sizedText (12.0, frameH - 52.0) ("! " + String.concat "; " e) 12.0 col_red ]
    let draftLabel = if model.Draft = "" then "LEFT/RIGHT pick a day, type an entry, ENTER adds" else model.Draft
    let draft =
        [ Scene.rectangle (12.0, frameH - 40.0, frameW - 24.0, 30.0) col_panel
          Scene.sizedText (20.0, frameH - 19.0) draftLabel 13.0 (if model.Draft = "" then col_dim else col_text) ]
    Scene.group (header @ cells @ entryViews @ errs @ draft)

// --- evidence wiring ----------------------------------------------------------------

let deriveOutcome (model: Model): ExpectedOutcome =
    let committed = model.Entries |> Map.toList |> List.sumBy (fun (_, es) -> List.length es)
    { Kind = "productivity"
      Values =
        [ "committed", string committed
          "rejected", string model.Rejected
          "selectedDay", string model.Selected ] }

let private noMods: KeyModifiers = { Ctrl = false; Alt = false; Shift = false; Meta = false }
let private press (k: ViewerKey): FrameInput<Msg> = FrameInput.Key(k, noMods)
let private typeText (s: string): FrameInput<Msg> list = [ for c in s -> press (Letter c) ]

/// A seeded script: move two days forward, add a valid entry, attempt a blank add (rejected),
/// add another valid entry.
let script: FrameInput<Msg> list =
    [ press ArrowRight; press ArrowRight ]       // select day 3
    @ typeText "meet" @ [ press Enter ]          // commit
    @ [ press Enter ]                            // blank -> rejected
    @ typeText "call" @ [ press Enter ]          // commit
    @ [ FrameInput.Idle ]

/// Pinned literal (seed-independent — Calendar carries no PRNG).
let expected: ExpectedOutcome =
    { Kind = "productivity"
      Values = [ "committed", "2"; "rejected", "1"; "selectedDay", "3" ] }

let private hostFor (_seedValue: int) (mode) (accent) =
    Harness.host (fun () -> init) update view mapKey mapPointer tick (SampleTheme.resolve mode accent)

let recordAt (seedValue: int): SampleEvidenceRecord =
    Harness.recordFor "calendar" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue

let entry: Harness.SampleEntry =
    { Id = "calendar"
      Family = "productivity"
      Title = "Calendar scheduler"
      Controls = [ "stack"; "grid"; "text-box"; "button"; "label"; "text-block" ]
      Inputs = [ "keyboard"; "pointer" ]
      RunEvidence = fun seedValue outDir -> Harness.evidenceForScene renderScene "calendar" (hostFor seedValue FS.GG.UI.Themes.Default.Theming.Light SampleTheme.indigo) script deriveOutcome seedValue outDir
      Interactive = fun _ -> Harness.runInteractiveScene "Calendar — Sample Apps" { Width = int frameW; Height = int frameH } (fun () -> init) update renderScene mapKey tick
      Outcome = expected }
