/// The six enterprise template pages (contracts/enterprise-templates.md, FR-005/FR-006).
/// Each is a composition of catalog controls ONLY (no bespoke types — SC-002), realized
/// from the committed `docs/product/ant-design/templates/*.md` recipes and populated from
/// `DemoState`. The form template owns the validation behavior (FR-006/SC-009) via the
/// pure `Model.update` transitions, reading `s.Form.Phase` to render errors / success.
module AntShowcase.Core.Templates

open FS.GG.UI.Controls
open FS.GG.UI.Controls.Display2
open FS.GG.UI.Controls.Feedback2
open AntShowcase.Core.Model
open AntShowcase.Core.DemoState

let private toControl w = Widget.toControl w

let private section (title: string) (body: Control<AntShowcaseMsg>): Control<AntShowcaseMsg> =
    Border.create
        [ Attr.width 520.0
          Attr.height 138.0
          Attr.padding 8.0
          Border.child (Stack.create [ Stack.children [ Label.create [ Label.text title ]; body ] ]) ]

let private pageShell (title: string) (primaryAction: string) (children: Control<AntShowcaseMsg> list): Control<AntShowcaseMsg> =
    Stack.create
        [ Stack.children
              [ Stack.create
                    [ Stack.orientation "horizontal"
                      Stack.children
                          [ TextBlock.create [ TextBlock.text title ]
                            Button.create [ Button.text primaryAction; Button.onClick (PageMsg ButtonClicked) ] ] ]
                Wrap.create [ Wrap.children children ] ] ]

let private rowsGrid (idPrefix: string) (label: string) (rows: string list): Control<AntShowcaseMsg> =
    let cols: DataGridColumn list =
        [ { Key = "item"; Header = label; Width = 240.0; ColumnType = TextColumn } ]
    let gridRows: DataGridRow list =
        rows
        |> List.mapi (fun i r ->
            let key = sprintf "%s-%d" idPrefix i
            { Key = key; Cells = [ { RowKey = key; ColumnKey = "item"; Value = r } ] })
    DataGrid.create cols [ DataGrid.rows gridRows ]

// ---------------------------------------------------------------------------------
// T1 — Workbench: toolbar + primary data-grid + side panel with card/statistic
// ---------------------------------------------------------------------------------
let private workbench (_s: DemoState): Control<AntShowcaseMsg> =
    let toolbar =
        Toolbar.create [ Toolbar.children [ Button.create [ Button.text "Refresh"; Button.onClick (PageMsg ButtonClicked) ]; Button.create [ Button.text "New" ] ] ]
    let side =
        Panel.create
            [ Panel.children
                  [ Card.create [ Card.title "Pipeline" ]
                    Statistic.create [ Statistic.value "3 running" ] ] ]
    pageShell
        "Operations Workbench"
        "New workflow"
        [ section "Actions" toolbar
          section "Recent builds" (rowsGrid "wb" "Recent builds" workbenchRows)
          section "Pipeline health" side ]

// ---------------------------------------------------------------------------------
// T2 — List page: filter toolbar + paginated collection + status tags
// ---------------------------------------------------------------------------------
let private listPage (s: DemoState): Control<AntShowcaseMsg> =
    let toolbar =
        Toolbar.create [ Toolbar.children [ TextBox.create [ TextBox.value "Filter…" ]; Button.create [ Button.text "Search"; Button.onClick (PageMsg ButtonClicked) ] ] ]
    let tags =
        Stack.create [ Stack.orientation "horizontal"; Stack.children (tagSamples |> List.map (fun t -> Tag.create [ Tag.text t ])) ]
    let pager =
        Navigation2.Pagination.create [ Navigation2.Pagination.total paginationTotal; Navigation2.Pagination.onChange (fun p -> PageMsg(PageChanged(int p))) ]
    pageShell
        "Orders"
        "Create order"
        [ section "Filters" toolbar
          section "Status" tags
          section "Orders" (rowsGrid "ls" "Orders" listRows)
          section "Pagination" pager ]

// ---------------------------------------------------------------------------------
// T3 — Detail page: descriptions + related card/panel + tabs + activity timeline
// ---------------------------------------------------------------------------------
let private detail (s: DemoState): Control<AntShowcaseMsg> =
    let tabs = Tabs.create [ Tabs.items tabItems; Tabs.selected s.Tab; Tabs.onChanged (fun v -> PageMsg(TabChanged v)) ]
    let related =
        Panel.create [ Panel.children [ Card.create [ Card.title "Related" ]; Timeline.create [ Attr.items timelineItems ] ] ]
    pageShell
        "Account Detail"
        "Edit account"
        [ section "Facts" (Descriptions.create [ Attr.items detailFacts ])
          section "Sections" tabs
          section "Related activity" related ]

// ---------------------------------------------------------------------------------
// T4 — Form page: sectioned fields + validation-message + submit -> result
// ---------------------------------------------------------------------------------
let private form (s: DemoState): Control<AntShowcaseMsg> =
    let f = s.Form
    let roleProps =
        { FS.GG.UI.Controls.Typed.ComboBox.defaults "tpl-form-role" with
            Items = comboItems
            OnChanged = Some(fun v -> PageMsg(FormFieldChanged("Role", v))) }
    let roleModel, _ = FS.GG.UI.Controls.Typed.ComboBox.init roleProps
    let fields =
        [ section "Name" (TextBox.create [ TextBox.value f.Name; TextBox.onChanged (fun v -> PageMsg(FormFieldChanged("Name", v))) ])
          section "Email" (TextBox.create [ TextBox.value f.Email; TextBox.onChanged (fun v -> PageMsg(FormFieldChanged("Email", v))) ])
          section "Bio" (TextArea.create [ TextArea.value "" ])
          section "Role" (toControl (FS.GG.UI.Controls.Typed.ComboBox.view roleProps roleModel))
          section "Agree to terms" (Switch.create [ Switch.checked' f.Agree; Switch.onChanged (fun v -> PageMsg(FormFieldChanged("Agree", (if v then "true" else "false")))) ]) ]
    let errorNodes =
        match f.Phase with
        | Invalid errors -> errors |> List.map (fun (_, m) -> ValidationMessage.create [ ValidationMessage.text m ])
        | _ -> []
    let outcome =
        match f.Phase with
        | Submitted -> [ Result.create [ Result.title "Profile saved" ] ]
        | _ -> []
    let submit = Button.create [ Button.text "Submit"; Button.onClick (PageMsg FormSubmitted) ]
    pageShell "Profile Form" "Save profile" (fields @ errorNodes @ [ section "Submit" submit ] @ outcome)

// ---------------------------------------------------------------------------------
// T5 — Result page: success result + follow-up buttons + a statistic
// ---------------------------------------------------------------------------------
let private result (_s: DemoState): Control<AntShowcaseMsg> =
    pageShell
        "Order Result"
        "View order"
        [ section "Outcome" (Result.create [ Result.title "Order placed successfully" ])
          section "Reference" (Statistic.create [ Statistic.value "Order #1042" ])
          section "Next steps" (Stack.create [ Stack.orientation "horizontal"; Stack.children [ Button.create [ Button.text "View order"; Button.onClick (PageMsg ButtonClicked) ]; Button.create [ Button.text "Continue" ] ] ]) ]

// ---------------------------------------------------------------------------------
// T6 — Exception page: 403/404/500 result + recovery button + status icon
// ---------------------------------------------------------------------------------
let private exception' (_s: DemoState): Control<AntShowcaseMsg> =
    pageShell
        "Exception Recovery"
        "Back home"
        [ section "Status" (Icon.create [ Icon.name "warning" ])
          section "Message" (Result.create [ Result.title "404 — Page not found" ])
          section "Recovery" (Button.create [ Button.text "Back home"; Button.onClick (PageMsg ButtonClicked) ]) ]

/// The six template pages, tagged `Template` (ControlIds = [], exempt from the bijection).
let all: Page list =
    [ { Id = "tpl-workbench"; Title = "Workbench"; Kind = Template; ControlIds = []; View = workbench }
      { Id = "tpl-list"; Title = "List Page"; Kind = Template; ControlIds = []; View = listPage }
      { Id = "tpl-detail"; Title = "Detail Page"; Kind = Template; ControlIds = []; View = detail }
      { Id = "tpl-form"; Title = "Form Page"; Kind = Template; ControlIds = []; View = form }
      { Id = "tpl-result"; Title = "Result Page"; Kind = Template; ControlIds = []; View = result }
      { Id = "tpl-exception"; Title = "Exception (403/404/500)"; Kind = Template; ControlIds = []; View = exception' } ]
