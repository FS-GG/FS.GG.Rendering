module SecondAntShowcase.Core.InteractionContracts

open SecondAntShowcase.Core.Model

type InteractionContract =
    { ContractId: string
      ControlIds: string list
      PageId: string
      StartingState: string
      Action: string
      ExpectedStateChange: string
      VisibleEvidence: string
      ScriptStep: SecondAntShowcaseMsg option
      ThemeInvariant: bool
      DisplayOnlyReason: string option }

type InteractionCoverage =
    { MissingContractOrReason: string list
      ContractedControls: string list
      DisplayOnlyControls: string list }

let private pageFor controlId =
    PageRegistry.catalogPages
    |> List.tryFind (fun page -> page.ControlIds |> List.contains controlId)
    |> Option.map _.Id
    |> Option.defaultValue "unknown"

let private contract id controls action expected evidence script =
    { ContractId = id
      ControlIds = controls
      PageId = pageFor (List.head controls)
      StartingState = "seeded demo state"
      Action = action
      ExpectedStateChange = expected
      VisibleEvidence = evidence
      ScriptStep = Some(PageMsg script)
      ThemeInvariant = true
      DisplayOnlyReason = None }

let all: InteractionContract list =
    [ contract "button-click" [ "button"; "icon-button"; "split-button"; "float-button" ] "activate command" "ButtonClicks increments" "status area and command counter change" ButtonClicked
      contract "toggle-switch" [ "toggle-button"; "switch" ] "toggle boolean value" "ToggleOn/SwitchOn changes" "checked visual state changes" (ToggleChanged false)
      contract "text-entry" [ "text-box"; "text-area"; "auto-complete" ] "enter text" "text fields update" "entered text is rendered" (TextChanged "review text")
      contract "numeric-entry" [ "numeric-input" ] "set numeric value" "NumericValue changes" "numeric value display changes" (NumericChanged 64.0)
      contract "date-time" [ "date-picker"; "time-picker" ] "choose a date/time value" "DatePickerSelected changes" "selected temporal value is visible" (DatePickerChanged(System.DateOnly(2026, 6, 19)))
      contract "slider-rating" [ "slider"; "rate"; "progress-bar" ] "adjust value" "SliderValue/RateValue/ProgressValue changes" "position or stars visibly change" (SliderChanged 0.75)
      contract "selection-single" [ "radio-group"; "combo-box"; "list-box"; "segmented"; "cascader"; "color-picker" ] "select another option" "selected option changes" "selected label or swatch changes" (ComboChanged "Product")
      contract "selection-multi" [ "check-box"; "multi-select-list"; "tree-view" ] "change selected values" "selection state changes" "checked/selected rows change" (MultiChanged [ "Bold"; "Italic" ])
      contract "navigation" [ "tabs"; "menu"; "breadcrumb"; "steps"; "pagination"; "anchor"; "affix" ] "navigate within page" "active navigation state changes" "active item or page indicator changes" (TabChanged "Activity")
      contract "disclosure" [ "collapse"; "drawer"; "popover"; "popconfirm"; "tooltip"; "dialog"; "overlay"; "tour" ] "open or close surface" "expanded/open state changes" "overlay, drawer, or panel visibility changes" (OverlayToggled true)
      contract "upload" [ "upload" ] "select upload artifact" "UploadValue changes" "selected file name changes" (UploadChanged "contract.pdf")
      contract "data-collection" [ "list-view"; "data-grid" ] "select or page collection" "PaginationPage changes" "visible row/page state changes" (PageChanged 2)
      contract "form-validation" [ "validation-message" ] "submit invalid form" "Form.Phase becomes Invalid" "validation message appears" FormSubmitted
      contract "graph-custom" [ "graph-view"; "custom-control" ] "script pointer/keyboard action" "interaction event is recorded" "custom/graph status changes" ButtonClicked ]

let displayOnlyReasons: Map<string, string> =
    [ "text-block", "static typography sample"
      "rich-text", "static rich text formatting sample"
      "label", "static field label sample"
      "icon", "static iconography sample"
      "separator", "static layout divider"
      "badge", "static status count sample"
      "tag", "static classification token"
      "avatar", "static identity sample"
      "image", "static media sample"
      "card", "static container sample"
      "descriptions", "static facts display"
      "statistic", "static metric display"
      "qr-code", "static encoded value"
      "watermark", "static background affordance"
      "calendar", "static month display in this sample"
      "carousel", "seeded slide set without auto-play evidence"
      "timeline", "static chronological display sample"
      "toolbar", "composes command controls covered by button contracts"
      "context-menu", "displayed as menu pattern without host context gesture in headless evidence"
      "toast", "display-only notification state in this sample"
      "spinner", "static loading sample"
      "empty", "static empty-state sample"
      "skeleton", "static loading placeholder sample"
      "alert", "static feedback sample"
      "result", "static outcome sample"
      "line-chart", "static seeded chart data"
      "bar-chart", "static seeded chart data"
      "pie-chart", "static seeded chart data"
      "scatter-plot", "static seeded chart data"
      "area-chart", "static seeded chart data"
      "column-chart", "static seeded chart data"
      "histogram", "static seeded chart data"
      "box-plot", "static seeded chart data"
      "heatmap", "static seeded chart data"
      "radar-chart", "static seeded chart data"
      "rose-chart", "static seeded chart data"
      "waterfall-chart", "static seeded chart data"
      "funnel-chart", "static seeded chart data"
      "gauge-chart", "static seeded chart data"
      "treemap", "static seeded chart data"
      "sunburst", "static seeded chart data"
      "sankey-diagram", "static seeded graph data"
      "chord-diagram", "static seeded graph data"
      "stack", "layout primitive, behavior comes from composed children"
      "grid", "layout primitive, behavior comes from composed children"
      "dock", "layout primitive, behavior comes from composed children"
      "wrap", "layout primitive, behavior comes from composed children"
      "border", "layout primitive, behavior comes from composed children"
      "panel", "layout primitive, behavior comes from composed children"
      "scroll-viewer", "host scrolling behavior, not a pure Core state transition"
      "split-view", "static layout split sample" ]
    |> Map.ofList

let forControl controlId =
    all |> List.filter (fun contract -> contract.ControlIds |> List.contains controlId)

let coverage () =
    let contracted = all |> List.collect _.ControlIds |> List.distinct |> List.sort
    let displayOnly = displayOnlyReasons |> Map.toList |> List.map fst |> List.distinct |> List.sort
    let known = Set.ofList (contracted @ displayOnly)
    let missing =
        CoverageMap.catalogIds ()
        |> List.filter (fun id -> not (known.Contains id))
        |> List.sort
    { MissingContractOrReason = missing
      ContractedControls = contracted
      DisplayOnlyControls = displayOnly }

let isClean coverage = List.isEmpty coverage.MissingContractOrReason

let summaryMarkdown () =
    let c = coverage ()
    [ "# Interaction Review"
      ""
      sprintf "- contracted controls: %d" (List.length c.ContractedControls)
      sprintf "- display-only controls with reasons: %d" (List.length c.DisplayOnlyControls)
      sprintf "- missing contract or reason: %d" (List.length c.MissingContractOrReason)
      ""
      "## Contracts"
      ""
      for contract in all do
          sprintf "- `%s` on `%s`: %s -> %s" contract.ContractId contract.PageId contract.Action contract.VisibleEvidence
      ""
      "## Display-Only Reasons"
      ""
      for KeyValue(controlId, reason) in displayOnlyReasons do
          sprintf "- `%s`: %s" controlId reason
      if not (List.isEmpty c.MissingContractOrReason) then
          ""
          "## Missing"
          ""
          for id in c.MissingContractOrReason do
              sprintf "- `%s`" id ]
    |> String.concat System.Environment.NewLine
