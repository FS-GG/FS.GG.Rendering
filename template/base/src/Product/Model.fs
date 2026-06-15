module Product.Model

open System
//#if (profile == "governed" || profile == "headless-scene")
open FS.GG.UI.Scene

type Model =
    { Name: string
      RenderCount: int }

type Msg =
    | Rendered
    | NoOp

let initialModel =
    { Name = "Product"
      RenderCount = 0 }

let update msg model =
    match msg with
    | Rendered -> { model with RenderCount = model.RenderCount + 1 }, []
    | NoOp -> model, []

//#else
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.DesignSystem
open FS.GG.UI.Themes.Default
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

type Model =
    { Name: string
      CanSave: bool
      Page: Page
      Interactions: int
      ContentColumn: int
      ContentRow: int
      ItemCount: int
      Step: int
      TickCount: int
      NextLabel: string
      LastInput: ViewerKey option
      InputDiagnostics: InputFlowDiagnostic list
      Revenue: ChartSeries list
      GridColumns: DataGridColumn list
      GridRows: DataGridRow list
      RichIntro: RichTextBlock }

and Page =
    | Home
    | Browse
    | Detail
    | Settings
    | Summary

and InputFlowDiagnostic =
    { InputValue: string
      RawKey: string option
      Direction: string
      Page: string
      ExpectedTransition: string
      Flow: string }

type Msg =
    | NameChanged of string
    | SaveRequested
    | GridSelectionChanged of string
    | ViewerInput of ViewerKey * isDown: bool
    | ViewerKeyEventReceived of ViewerKeyEvent
    | Tick
    | Navigated of Page
    | RuntimeMsg of ControlRuntimeMsg
    | NoOp

type GeneratedLayoutValidationFailureClass =
    | MissingLayoutFacts
    | OverlappingLayoutBounds

type GeneratedLayoutValidationResult =
    { Accepted: bool
      FailureClass: GeneratedLayoutValidationFailureClass option
      Diagnostics: string list }

let revenueSeries =
    [ { Name = "Revenue"
        Points =
          [ { X = 0.0; Y = 12.0; Label = Some "Q1" }
            { X = 1.0; Y = 18.0; Label = Some "Q2" }
            { X = 2.0; Y = 15.0; Label = Some "Q3" }
            { X = 3.0; Y = 24.0; Label = Some "Q4" } ] } ]

let gridColumns =
    [ { Key = "name"; Header = "Name"; Width = 160.0; ColumnType = TextColumn }
      { Key = "amount"; Header = "Amount"; Width = 96.0; ColumnType = NumericColumn } ]

let gridColumnsAttr: Attr<Msg> =
    DataGrid.columns gridColumns

let gridRows =
    [ let row key name amount =
          { Key = key
            Cells =
              [ { RowKey = key; ColumnKey = "name"; Value = name }
                { RowKey = key; ColumnKey = "amount"; Value = amount } ] }

      row "row-1" "North" "120"
      row "row-2" "South" "98"
      row "row-3" "West" "141" ]

let richIntro =
    let baseStyle = RichText.defaultStyle Theme.light
    let accent =
        { baseStyle with
            Weight = Bold
            Foreground = Theme.light.Accent }

    { RichText.block [ RichText.run "Product-owned " accent; RichText.run "Controls guidance" baseStyle ] with
        MaxWidth = Some 360.0
        Clip = true }

let initialModel =
    { Name = "Product"
      CanSave = true
      Page = Home
      Interactions = 0
      ContentColumn = 0
      ContentRow = 0
      ItemCount = 0
      Step = 1
      TickCount = 0
      NextLabel = "Next"
      LastInput = None
      InputDiagnostics = []
      Revenue = revenueSeries
      GridColumns = gridColumns
      GridRows = gridRows
      RichIntro = richIntro }

let pageName page =
    match page with
    | Home -> "home"
    | Browse -> "browse"
    | Detail -> "detail"
    | Settings -> "settings"
    | Summary -> "summary"

let keyName key =
    ViewerKeyboard.toKeyId key

let diagnostic flow raw direction previousPage key expected =
    { InputValue = keyName key
      RawKey = raw
      Direction = direction
      Page = pageName previousPage
      ExpectedTransition = expected
      Flow = flow }

// Neutral, non-game navigation: keys move between application pages and a content-region
// cursor (column/row) over the example controls. Pure transition over the model.
let transitionViewerInput raw direction key isDown model =
    if not isDown then
        { model with LastInput = Some key }, []
    else
        let current = model.Page

        let nextPage, interactions, contentColumn, contentRow, flow, expected =
            match current, key with
            | Home, Enter -> Browse, model.Interactions, model.ContentColumn, model.ContentRow, "home-open", "browse"
            | Home, Letter 'S' -> Settings, model.Interactions, model.ContentColumn, model.ContentRow, "settings-open", "settings"
            | Settings, Enter -> Browse, model.Interactions, model.ContentColumn, model.ContentRow, "settings-apply", "browse"
            | Settings, Escape
            | Settings, Backspace -> Home, model.Interactions, model.ContentColumn, model.ContentRow, "settings-back", "home"
            | Browse, Enter -> Detail, model.Interactions, model.ContentColumn, model.ContentRow, "open-detail", "detail"
            | Browse, ArrowLeft -> Browse, model.Interactions + 1, max 0 (model.ContentColumn - 1), model.ContentRow, "content-move", "browse"
            | Browse, ArrowRight -> Browse, model.Interactions + 1, min 9 (model.ContentColumn + 1), model.ContentRow, "content-move", "browse"
            | Browse, ArrowDown -> Browse, model.Interactions + 1, model.ContentColumn, min 19 (model.ContentRow + 1), "content-move", "browse"
            | Browse, ArrowUp -> Browse, model.Interactions + 1, model.ContentColumn, max 0 (model.ContentRow - 1), "content-move", "browse"
            | Detail, Escape
            | Detail, Backspace -> Browse, model.Interactions, model.ContentColumn, model.ContentRow, "detail-back", "browse"
            | Summary, Enter -> Home, 0, 0, 0, "restart", "home"
            | _ -> current, model.Interactions, model.ContentColumn, model.ContentRow, "ignored", pageName current

        let entry = diagnostic flow raw direction current key expected

        { model with
            Page = nextPage
            Interactions = interactions
            ContentColumn = contentColumn
            ContentRow = contentRow
            LastInput = Some key
            InputDiagnostics = entry :: model.InputDiagnostics },
        []

let dispatchViewerKey event model =
    let key, isDown = ViewerKeyboard.normalizeEvent event
    let direction = if isDown then "down" else "up"
    transitionViewerInput (Some event.RawKey) direction key isDown model

let init () : Model * AdapterCommand<Msg> =
    initialModel, []

let update msg model : Model * AdapterCommand<Msg> =
    match msg with
    | NameChanged value -> { model with Name = value }, []
    | SaveRequested -> model, [ DispatchHostCommand $"save:{model.Name}" ]
    | GridSelectionChanged _ -> model, []
    | ViewerInput(key, isDown) -> transitionViewerInput None (if isDown then "down" else "up") key isDown model
    | ViewerKeyEventReceived event -> dispatchViewerKey event model
    | Tick ->
        if model.Page = Browse then
            { model with
                TickCount = model.TickCount + 1
                ContentRow = if model.ContentRow >= 19 then 0 else model.ContentRow + 1
                ItemCount = model.ItemCount + 1 },
            []
        else
            { model with TickCount = model.TickCount + 1 }, []
    | Navigated page -> { model with Page = page }, []
    | RuntimeMsg _ -> model, []
    | NoOp -> model, []

let subscriptions _ : AdapterSubscription<Msg> list =
    ControlsElmish.subscriptions [] []

//#endif
