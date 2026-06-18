/// The application shell / chrome (FR-001): a top app bar (title + antLight/antDark
/// toggle), a left nav rail listing all 19 pages (family pages, then templates), a
/// scrolling content region, and a bottom status strip. `view : Size -> AntShowcaseModel
/// -> Control<AntShowcaseMsg>` is the MVU View; its tree shape is theme-independent
/// (FR-008/SC-003) — only resolved visuals differ between antLight and antDark.
module AntShowcase.Core.Shell

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open AntShowcase.Core.Model

let private region (bounds: ShellLayout.Rect) (label: string) (child: Control<AntShowcaseMsg>): Control<AntShowcaseMsg> =
    Border.create
        [ Attr.width bounds.Width
          Attr.height bounds.Height
          Attr.padding 8.0
          Border.child
              (Stack.create
                  [ Stack.children
                        [ TextBlock.create [ TextBlock.text label ]
                          child ] ]) ]

let private appBar (bounds: ShellLayout.Rect) (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let modeLabel =
        match model.Mode with
        | Light -> "Light"
        | Dark -> "Dark"
    region
        bounds
        "top-bar"
        (Stack.create
            [ Stack.orientation "horizontal"
              Stack.children
                  [ TextBlock.create [ TextBlock.text "AntShowcase" ]
                    TextBlock.create [ TextBlock.text ("Theme " + modeLabel) ] ] ])

let private navTitle (p: Page): string =
    match p.Id with
    | "display-typography" -> "Disp"
    | "cards-stats-media" -> "Cards"
    | "buttons" -> "Btn"
    | "text-numeric-input" -> "Input"
    | "selection-toggles" -> "Select"
    | "layout-containers" -> "Layout"
    | "navigation-menus" -> "Nav"
    | "overlays" -> "Overlay"
    | "feedback-status" -> "Feed"
    | "data-collections" -> "Data"
    | "charts-statistical" -> "Ch1"
    | "charts-advanced" -> "Ch2"
    | "graphs-custom" -> "Graph"
    | "tpl-workbench" -> "Work"
    | "tpl-list" -> "List"
    | "tpl-detail" -> "Detail"
    | "tpl-form" -> "Form"
    | "tpl-result" -> "Result"
    | "tpl-exception" -> "Excp"
    | _ -> ShellLayout.truncateLabel 8 p.Title

let private navItem (current: string) (p: Page): Control<AntShowcaseMsg> =
    let marker = if p.Id = current then "▸ " else "  "
    Button.create [ Button.text (marker + navTitle p); Button.onClick (NavigateTo p.Id); Attr.width 120.0 ]

let private navRail (bounds: ShellLayout.Rect) (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let header t = Label.create [ Label.text t ]
    let catalogItems = PageRegistry.catalogPages |> List.map (navItem model.CurrentPage)
    let templateItems = PageRegistry.templatePages |> List.map (navItem model.CurrentPage)
    region
        bounds
        "navigation"
        (Stack.create
            [ Stack.orientation "vertical"
              Stack.children ((header "Controls" :: catalogItems) @ (header "Templates" :: templateItems)) ])

let private content (bounds: ShellLayout.Rect) (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let page = PageRegistry.byId model.CurrentPage
    let profile = PageProfiles.byPageId page.Id
    let body = page.View model.PageState
    let framedBody =
        Panel.create
            [ Attr.width (bounds.Width - 32.0)
              Attr.height (bounds.Height - 88.0)
              Panel.children
                  [ Label.create [ Label.text page.Title ]
                    TextBlock.create [ TextBlock.text (sprintf "profile=%A · columns=%d" profile.Density profile.SectionColumns) ]
                    body ] ]
    let props = FS.GG.UI.Controls.Typed.ScrollViewer.defaults "content-scroll" (Widget.ofControl framedBody)
    region bounds "content" (Widget.toControl (FS.GG.UI.Controls.Typed.ScrollViewer.view props))

/// A feedback capture section shown on EVERY page: a draft field + submit button, plus the
/// feedback already saved for the current page. Submitting saves a page-tagged entry (pure
/// `update`); the App edge persists it so it can be acted upon later (`AntShowcase feedback`).
let private feedbackSection (bounds: ShellLayout.Rect) (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let forPage = model.Feedback |> List.filter (fun e -> e.PageId = model.CurrentPage)
    let saved =
        if List.isEmpty forPage then
            [ TextBlock.create [ TextBlock.text "No page feedback saved." ] ]
        else
            forPage |> List.map (fun e -> TextBlock.create [ TextBlock.text ("• " + e.Text) ])
    let summary = sprintf "%d page · %d total" (List.length forPage) (List.length model.Feedback)
    region
        bounds
        "feedback"
        (Stack.create
            [ Stack.orientation "horizontal"
              Stack.children
                  ([ Label.create [ Label.text "Feedback" ]
                     TextBlock.create [ TextBlock.text summary ] ]
                   @ saved) ])

let private statusStrip (bounds: ShellLayout.Rect) (model: AntShowcaseModel): Control<AntShowcaseMsg> =
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
    region bounds "status" (TextBlock.create [ TextBlock.text text ])

/// The full shell. `size` is accepted to satisfy the size-aware View seam; the layout is
/// composition-driven so it adapts without reading exact pixels here.
let view (size: Size) (model: AntShowcaseModel): Control<AntShowcaseMsg> =
    let regions = ShellLayout.calculate size
    Stack.create
        [ Stack.orientation "vertical"
          Attr.width (float size.Width)
          Attr.height (float size.Height)
          Stack.children
              [ appBar regions.TopBar model
                Stack.create
                    [ Stack.orientation "horizontal"
                      Stack.children [ navRail regions.Navigation model; content regions.Content model ] ]
                feedbackSection regions.Feedback model
                statusStrip regions.Status model ] ]
