module ControlsFeature132NewControlContractTests

// Feature 132 (D2.1) — the net-new control contract suite (US3, FR-008/SC-004, contract R6).
// Parameterized over every net-new control id, exercising the SAME five families existing controls
// pass — Catalog, Semantic, Interaction, Accessibility, Rendering — plus a DUAL-THEME render
// (Default neutral + AntDesign Ant-styled), confirming each net-new control is a first-class,
// theme-agnostic citizen of the library.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

module DefaultTheme = FS.GG.UI.Themes.Default.Theme
module Ant = FS.GG.UI.Themes.AntDesign.AntTheme

type Msg =
    | Closed
    | Clicked
    | Confirmed
    | Cancelled
    | Changed of string

// Every net-new control id with a minimally-attributed instance. The kind string IS the catalog id.
let private netNewControls : (string * Control<Msg>) list =
    [ "tag", Display2.Tag.create [ Display2.Tag.text "beta" ]
      "avatar", Display2.Avatar.create [ Display2.Avatar.text "AB" ]
      "card", Display2.Card.create [ Display2.Card.title "Card" ]
      "descriptions", Display2.Descriptions.create [ Attr.items [ "Name"; "Ant" ] ]
      "statistic", Display2.Statistic.create [ Display2.Statistic.value "42" ]
      "timeline", Display2.Timeline.create [ Attr.items [ "a"; "b" ] ]
      "empty", Display2.Empty.create [ Display2.Empty.text "No data" ]
      "skeleton", Display2.Skeleton.create []
      "qr-code", Display2.QrCode.create [ Display2.QrCode.value "abc" ]
      "watermark", Display2.Watermark.create [ Display2.Watermark.text "FS.GG" ]
      "alert", Feedback2.Alert.create [ Feedback2.Alert.text "Heads up" ]
      "result", Feedback2.Result.create [ Feedback2.Result.title "Done" ]
      "drawer", Feedback2.Drawer.create [ Feedback2.Drawer.title "Side" ]
      "popover", Feedback2.Popover.create [ Feedback2.Popover.text "Hint" ]
      "popconfirm", Feedback2.Popconfirm.create [ Feedback2.Popconfirm.text "Sure?" ]
      "tour", Feedback2.Tour.create [ Feedback2.Tour.text "Step 1" ]
      "float-button", Feedback2.FloatButton.create [ Feedback2.FloatButton.text "+" ]
      "breadcrumb", Navigation2.Breadcrumb.create [ Attr.items [ "Home"; "Lib" ] ]
      "steps", Navigation2.Steps.create [ Attr.items [ "One"; "Two" ] ]
      "pagination", Navigation2.Pagination.create [ Navigation2.Pagination.total 5 ]
      "segmented", Navigation2.Segmented.create [ Attr.items [ "Day"; "Week" ] ]
      "anchor", Navigation2.Anchor.create [ Attr.items [ "Intro"; "API" ] ]
      "affix", Navigation2.Affix.create [ Navigation2.Affix.text "Pinned" ]
      "collapse", Interactive2.Collapse.create [ Attr.items [ "Sec 1"; "Sec 2" ] ]
      "rate", Interactive2.Rate.create [ Interactive2.Rate.value 3.0 ]
      "carousel", Interactive2.Carousel.create [ Attr.items [ "1"; "2" ] ]
      "calendar", Interactive2.Calendar.create []
      "cascader", DataEntry2.Cascader.create [ Attr.items [ "Region"; "City" ] ]
      "auto-complete", DataEntry2.AutoComplete.create [ DataEntry2.AutoComplete.value "qu" ]
      "upload", DataEntry2.Upload.create [ DataEntry2.Upload.text "Upload" ] ]

let private rowsById = Catalog.supportedControls |> List.map (fun r -> r.Id, r) |> Map.ofList

let private standardStates =
    set [ "normal"; "disabled"; "hover"; "pressed"; "focused"; "selected"; "validation"; "loading" ]

// Feature 184 (US3): the string payload now rides the typed `Nav` as `EditedText` (read back via
// `navText` by the onChange adapters); call sites keep passing a `string option`.
let private mkEvent kind (payload: string option) : ControlEvent =
    { Kind = kind; ControlId = None; Origin = ControlEventOrigin.Pointer; Nav = payload |> Option.map EditedText }

[<Tests>]
let feature132NewControlContractTests =
    testList "Feature 132 net-new control contract (FR-008/SC-004, R6)" [

        // --- Catalog family -------------------------------------------------------------------
        testList "Catalog" [
            for (id, _) in netNewControls do
                test (sprintf "%s has a complete, supported catalog row" id) {
                    match Map.tryFind id rowsById with
                    | None -> failtestf "%s is missing from Catalog.supportedControls" id
                    | Some row ->
                        Expect.isFalse (System.String.IsNullOrWhiteSpace row.Purpose) (sprintf "%s has a purpose" id)
                        Expect.equal (Set.ofList row.VisualStates) standardStates (sprintf "%s declares the standard 8 visual states" id)
                        Expect.equal row.SupportStatus "supported" (sprintf "%s is supported" id)
                        Expect.equal row.Owner "controls" (sprintf "%s is Controls-owned" id)
                        Expect.contains (Catalog.categories ()) row.Category (sprintf "%s category is a known catalog category" id)
                }
        ]

        // --- Semantic family ------------------------------------------------------------------
        testList "Semantic" [
            for (id, ctrl) in netNewControls do
                test (sprintf "%s authors with the expected kind and is diagnostic-clean" id) {
                    Expect.equal ctrl.Kind id (sprintf "%s control carries kind '%s'" id id)
                    Expect.isGreaterThan (Control.count ctrl) 0 (sprintf "%s has at least one node" id)
                    let errors = Control.diagnostics ctrl |> List.filter (fun d -> d.Severity = Error)
                    Expect.isEmpty errors (sprintf "%s authors with no Error diagnostics" id)
                }
        ]

        // --- Accessibility family -------------------------------------------------------------
        testList "Accessibility" [
            for (id, _) in netNewControls do
                test (sprintf "%s advertises accessibility role + state metadata" id) {
                    let row = rowsById.[id]
                    Expect.isFalse (System.String.IsNullOrWhiteSpace row.Accessibility.Role) (sprintf "%s has an accessibility role" id)
                    Expect.isNonEmpty row.Accessibility.StateMetadata (sprintf "%s reports state metadata" id)
                    Expect.isFalse (System.String.IsNullOrWhiteSpace row.Accessibility.KeyboardOperation) (sprintf "%s documents keyboard operation" id)
                }
        ]

        // --- Rendering family (DUAL THEME, SC-004) --------------------------------------------
        testList "Rendering (dual theme)" [
            for (id, ctrl) in netNewControls do
                test (sprintf "%s renders coherently under BOTH Default and AntDesign" id) {
                    for (tname, theme) in [ "Default", DefaultTheme.light; "AntDesign", Ant.antLight ] do
                        let rendered = Control.render theme ctrl
                        Expect.isEmpty rendered.Diagnostics (sprintf "%s renders with no diagnostics under %s" id tname)
                        Expect.isGreaterThan rendered.NodeCount 0 (sprintf "%s has a non-empty render tree under %s" id tname)
                        let evidence = Scene.renderReadbackEvidence { Width = 320; Height = 160 } rendered.Scene
                        Expect.isNonEmpty evidence.DeterministicHash (sprintf "%s produces deterministic render evidence under %s" id tname)
                }
        ]

        // --- Interaction family ---------------------------------------------------------------
        testList "Interaction" [
            test "tag onClose dispatches on a close event" {
                let c = Display2.Tag.create [ Display2.Tag.text "x"; Display2.Tag.onClose Closed ]
                Expect.equal (Control.dispatch (mkEvent "close" None) c) [ Closed ] "onClose fires Closed"
            }
            test "float-button onClick dispatches on a click event" {
                let c = Feedback2.FloatButton.create [ Feedback2.FloatButton.onClick Clicked ]
                Expect.equal (Control.dispatch (mkEvent "click" None) c) [ Clicked ] "onClick fires Clicked"
            }
            test "popconfirm onConfirm / onCancel dispatch independently" {
                let c =
                    Feedback2.Popconfirm.create
                        [ Feedback2.Popconfirm.onConfirm Confirmed; Feedback2.Popconfirm.onCancel Cancelled ]
                Expect.equal (Control.dispatch (mkEvent "confirm" None) c) [ Confirmed ] "onConfirm fires Confirmed"
                Expect.equal (Control.dispatch (mkEvent "cancel" None) c) [ Cancelled ] "onCancel fires Cancelled"
            }
            test "segmented onChange carries the selected payload" {
                let c = Navigation2.Segmented.create [ Attr.items [ "Day"; "Week" ]; Navigation2.Segmented.onChange Changed ]
                Expect.equal (Control.dispatch (mkEvent "change" (Some "Week")) c) [ Changed "Week" ] "onChange carries the payload"
            }
            test "rate onChange carries the new rating payload" {
                let c = Interactive2.Rate.create [ Interactive2.Rate.value 2.0; Interactive2.Rate.onChange Changed ]
                Expect.equal (Control.dispatch (mkEvent "change" (Some "4")) c) [ Changed "4" ] "onChange carries the rating"
            }
            test "upload onChange carries the file reference payload" {
                let c = DataEntry2.Upload.create [ DataEntry2.Upload.text "Upload"; DataEntry2.Upload.onChange Changed ]
                Expect.equal (Control.dispatch (mkEvent "change" (Some "f.png")) c) [ Changed "f.png" ] "onChange carries the file ref"
            }
        ]
    ]
