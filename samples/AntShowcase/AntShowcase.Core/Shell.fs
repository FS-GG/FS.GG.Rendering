/// The application shell / chrome (FR-001): a top app bar (title + antLight/antDark
/// toggle), a left nav rail listing all 19 pages (family pages, then templates), a
/// scrolling content region, and a bottom status strip. `view : Size -> AntShowcaseModel
/// -> Control<AntShowcaseMsg>` is the MVU View; its tree shape is theme-independent
/// (FR-008/SC-003) — only resolved visuals differ between antLight and antDark.
module AntShowcase.Core.Shell

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open AntShowcase.Core.Model

let private appBar (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let modeLabel =
        match model.Mode with
        | Light -> "Theme: Ant Light"
        | Dark -> "Theme: Ant Dark"
    Stack.create
        [ Stack.orientation "horizontal"
          Stack.children
              [ TextBlock.create [ TextBlock.text "Ant Design Controls Showcase" ]
                Button.create [ Button.text modeLabel; Button.onClick ToggleMode ] ] ]

let private navItem (current: string) (p: Page): Control<AntShowcaseMsg> =
    let marker = if p.Id = current then "▸ " else "  "
    Button.create [ Button.text (marker + p.Title); Button.onClick (NavigateTo p.Id) ]

let private navRail (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let header t = Label.create [ Label.text t ]
    let catalogItems = PageRegistry.catalogPages |> List.map (navItem model.CurrentPage)
    let templateItems = PageRegistry.templatePages |> List.map (navItem model.CurrentPage)
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children ((header "Controls" :: catalogItems) @ (header "Templates" :: templateItems)) ]

let private content (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let page = PageRegistry.byId model.CurrentPage
    let body = page.View model.PageState
    let props = FS.GG.UI.Controls.Typed.ScrollViewer.defaults "content-scroll" (Widget.ofControl body)
    Widget.toControl (FS.GG.UI.Controls.Typed.ScrollViewer.view props)

/// A feedback capture section shown on EVERY page: a draft field + submit button, plus the
/// feedback already saved for the current page. Submitting saves a page-tagged entry (pure
/// `update`); the App edge persists it so it can be acted upon later (`AntShowcase feedback`).
let private feedbackSection (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let forPage = model.Feedback |> List.filter (fun e -> e.PageId = model.CurrentPage)
    let saved =
        if List.isEmpty forPage then
            [ TextBlock.create [ TextBlock.text "No feedback saved for this page yet." ] ]
        else
            forPage |> List.map (fun e -> TextBlock.create [ TextBlock.text ("• " + e.Text) ])
    let summary =
        sprintf "%d saved for this page · %d total" (List.length forPage) (List.length model.Feedback)
    Panel.create
        [ Panel.children
              ([ Label.create [ Label.text "Feedback for this page" ]
                 TextBox.create [ TextBox.value model.FeedbackDraft; TextBox.onChanged (fun v -> FeedbackChanged v) ]
                 Button.create [ Button.text "Submit feedback"; Button.onClick FeedbackSubmitted ]
                 TextBlock.create [ TextBlock.text summary ] ]
               @ saved) ]

let private statusStrip (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let page = PageRegistry.byId model.CurrentPage
    let kindText =
        match page.Kind with
        | Catalog -> "catalog"
        | Template -> "template"
    let text =
        sprintf
            "%s · %s · theme=%s · %d pages · %d controls"
            page.Id
            kindText
            (AntTheme.modeName model.Mode)
            (List.length PageRegistry.all)
            (List.length page.ControlIds)
    TextBlock.create [ TextBlock.text text ]

/// The full shell. `size` is accepted to satisfy the size-aware View seam; the layout is
/// composition-driven so it adapts without reading exact pixels here.
let view (_size: Size) (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children
              [ appBar model
                Stack.create [ Stack.orientation "horizontal"; Stack.children [ navRail model; content model ] ]
                feedbackSection model
                statusStrip model ] ]
