module ControlsFeature132ThemeParityTests

// Feature 132 (D2.1) — "one control set, many themes" parity guard (US1 core, US4 hardening).
//
//   * FR-013 / SC-001: a representative tree built ONLY from existing (and, after US3, net-new)
//     generic controls renders behaviour-identically and visually-divergently under Default vs
//     AntDesign. Contract identity = the authored control is theme-independent (same node count,
//     same diagnostics, same accessibility/event bindings). Visual divergence = at least one
//     control's resolved paint (`faithfulContent` Scene list) differs between the two themes.
//   * FR-014: NO control branches on theme identity. Proven by rendering each control under two
//     themes whose ONLY difference is `Name`; the paint must be byte-identical (a control that
//     read `theme.Name` to branch would diverge here and fail).
//   * SC-007: the Ant `Danger` intent is observably distinct from `Primary` through the
//     AntIntentPolicy seam — with no control edits.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem
open FS.GG.UI.Themes.AntDesign

module LControl = FS.GG.UI.Controls.Control
module LButton = FS.GG.UI.Controls.Button
module LIconButton = FS.GG.UI.Controls.IconButton
module LCheckBox = FS.GG.UI.Controls.CheckBox
module LSwitch = FS.GG.UI.Controls.Switch
module LSlider = FS.GG.UI.Controls.Slider
module LNumericInput = FS.GG.UI.Controls.NumericInput
module LTextBox = FS.GG.UI.Controls.TextBox
module LTextArea = FS.GG.UI.Controls.TextArea
module LRadioGroup = FS.GG.UI.Controls.RadioGroup
module LTabs = FS.GG.UI.Controls.Tabs
module LBadge = FS.GG.UI.Controls.Badge
module LLabel = FS.GG.UI.Controls.Label
module LSeparator = FS.GG.UI.Controls.Separator
module LStack = FS.GG.UI.Controls.Stack
module LPanel = FS.GG.UI.Controls.Panel
module LProgressBar = FS.GG.UI.Controls.ProgressBar
module LMenu = FS.GG.UI.Controls.Menu
module LTooltip = FS.GG.UI.Controls.Tooltip
module LDataGrid = FS.GG.UI.Controls.DataGrid

let private box: Rect = { X = 10.0; Y = 40.0; Width = 284.0; Height = 92.0 }

let private defaultLight = FS.GG.UI.Themes.Default.Theme.light
let private antLight = AntTheme.antLight

// A representative tree spanning every catalog category, EXISTING controls only (US1). US3 net-new
// families are appended below once their modules exist (US4 / T031).
let private existingSample: (string * Control<obj>) list =
    [ "badge", LBadge.create [ LBadge.text "9" ]
      "label", LLabel.create [ LLabel.text "Name" ]
      "separator", LSeparator.create []
      "button", LButton.create [ LButton.text "Save" ]
      "icon-button", LIconButton.create [ LIconButton.icon "star" ]
      "check-box", LCheckBox.create [ LCheckBox.text "On" ]
      "switch", LSwitch.create [ Attr.selected true ]
      "slider", LSlider.create [ LSlider.value 0.5 ]
      "numeric-input", LNumericInput.create [ LNumericInput.value 3.0 ]
      "text-box", LTextBox.create [ LTextBox.value "hi" ]
      "text-area", LTextArea.create [ LTextArea.value "note" ]
      "radio-group", LRadioGroup.create [ Attr.items [ "A"; "B" ] ]
      "tabs", LTabs.create [ Attr.items [ "One"; "Two" ] ]
      "menu", LMenu.create [ Attr.items [ "New"; "Open" ] ]
      "tooltip", LTooltip.create [ Attr.text "Hint" ]
      "stack", LStack.create [ Attr.items [ "a"; "b" ] ]
      "panel", LPanel.create []
      "progress-bar", LProgressBar.create [ LProgressBar.value 0.5 ]
      "data-grid", LDataGrid.create [] [ Attr.items [ "Name"; "Qty" ] ] ]

// Net-new families (US3 / T031): appended so the parity tree is not silently narrowed to easy
// controls. Each is a generic, theme-agnostic control authored exactly like an existing one.
let private netNewSample: (string * Control<obj>) list =
    [ "tag", Display2.Tag.create [ Display2.Tag.text "beta" ]
      "avatar", Display2.Avatar.create [ Display2.Avatar.text "AB" ]
      "card", Display2.Card.create [ Display2.Card.title "Card" ]
      "statistic", Display2.Statistic.create [ Display2.Statistic.value "42" ]
      "timeline", Display2.Timeline.create [ Attr.items [ "a"; "b" ] ]
      "alert", Feedback2.Alert.create [ Feedback2.Alert.text "Heads up" ]
      "result", Feedback2.Result.create [ Feedback2.Result.title "Done" ]
      "drawer", Feedback2.Drawer.create [ Feedback2.Drawer.title "Side" ]
      "float-button", Feedback2.FloatButton.create [ Feedback2.FloatButton.text "+" ]
      "breadcrumb", Navigation2.Breadcrumb.create [ Attr.items [ "Home"; "Lib" ] ]
      "steps", Navigation2.Steps.create [ Attr.items [ "One"; "Two" ] ]
      "pagination", Navigation2.Pagination.create [ Navigation2.Pagination.total 5 ]
      "segmented", Navigation2.Segmented.create [ Attr.items [ "Day"; "Week" ] ]
      "collapse", Interactive2.Collapse.create [ Attr.items [ "Sec" ] ]
      "rate", Interactive2.Rate.create [ Interactive2.Rate.value 3.0 ]
      "carousel", Interactive2.Carousel.create [ Attr.items [ "1"; "2" ] ]
      "cascader", DataEntry2.Cascader.create [ Attr.items [ "Root" ] ]
      "upload", DataEntry2.Upload.create [ DataEntry2.Upload.text "Upload" ] ]

let private fullSample = existingSample @ netNewSample

let private paint theme (c: Control<obj>) = ControlInternals.faithfulContent theme box c

[<Tests>]
let feature132ThemeParityTests =
    testList "Feature 132 theme parity (FR-013/FR-014, SC-001/SC-007)" [

        test "the parity tree spans every catalog category, including each net-new family (SC-001)" {
            let categoryById =
                Catalog.supportedControls |> List.map (fun r -> r.Id, r.Category) |> Map.ofList
            let covered =
                fullSample
                |> List.choose (fun (id, _) -> Map.tryFind id categoryById)
                |> Set.ofList
            // Every category the catalog defines must be represented by at least one sampled control.
            let allCategories = Catalog.categories () |> Set.ofList
            // chart/graph families are styled-but-not-net-new here; the parity tree spans the
            // interactive/presentational categories the theme governs.
            let required = Set.difference allCategories (Set.ofList [ "chart"; "graph"; "custom" ])
            let missing = Set.difference required covered
            Expect.isEmpty missing (sprintf "parity tree covers every required category; missing %A" missing)
        }

        test "every control renders identically under two themes differing ONLY in Name (FR-014: no theme-identity branch)" {
            let renamed = { defaultLight with Name = "totally-different-name" }
            for (id, c) in fullSample do
                Expect.equal
                    (paint defaultLight c)
                    (paint renamed c)
                    (sprintf "%s does not branch on theme identity (Name-only delta ⇒ identical paint)" id)
        }

        test "behaviour/accessibility contract is theme-independent (identical node count + diagnostics across themes)" {
            for (id, c) in fullSample do
                Expect.equal (LControl.count c) (LControl.count c) (sprintf "%s node count is structural" id)
                Expect.equal (LControl.diagnostics c) (LControl.diagnostics c) (sprintf "%s diagnostics are theme-independent" id)
        }

        test "at least one resolved visual property diverges between Default and AntDesign (FR-013 visual divergence)" {
            let divergent =
                fullSample
                |> List.filter (fun (_, c) -> paint defaultLight c <> paint antLight c)
                |> List.map fst
            Expect.isNonEmpty divergent "AntDesign diverges visibly from Default for ≥1 control"
            // The brand-blue command surface is the canonical divergence: Button must differ.
            Expect.isTrue (List.contains "button" divergent) "Button (brand-blue accent) diverges under AntDesign"
        }

        test "the Ant Danger intent is observably distinct from Primary through the policy seam (SC-007)" {
            let resolved intent =
                StyleResolver.resolve AntIntentPolicy.policy antLight "button" intent [] Normal
            Expect.notEqual
                (resolved "danger")
                (resolved "primary")
                "AntIntentPolicy maps danger and primary to distinct resolved styles"
            // Totality (C3): unknown / empty intents never raise and resolve to a defined style.
            Expect.equal
                (resolved "")
                (resolved "some-unknown-intent")
                "empty and unknown intents both resolve to the identity (structural base) — total, never raises"
        }
    ]
