/// The application shell / chrome (FR-001): a top app bar (title + theme toggle +
/// accent selector), a left nav rail of the 10 pages, a scrolling content region, and
/// a bottom status strip. `view : Size -> GalleryModel -> Control<GalleryMsg>` is the
/// MVU View. Its tree shape is theme-independent (FR-006/SC-003).
module ControlsGallery.Core.Shell

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default.Theming
open ControlsGallery.Core.Model
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private appBar (model: GalleryModel): Control<GalleryMsg> =
    let themeLabel =
        match model.Mode with
        | Light -> "Theme: Light"
        | Dark -> "Theme: Dark"
    Stack.create
        [ Stack.orientation "horizontal"
          Stack.children
              [ TextBlock.create [ TextBlock.text "Controls Gallery — Indigo & Teal on Slate" ]
                Button.create [ Button.text themeLabel; Button.onClick ToggleTheme ]
                Button.create [ Button.text "Accent: Indigo"; Button.onClick (SelectAccent GalleryTheme.indigo) ]
                Button.create [ Button.text "Accent: Teal"; Button.onClick (SelectAccent GalleryTheme.teal) ] ] ]

let private navRail (_model: GalleryModel): Control<GalleryMsg> =
    let item (p: GalleryPage) =
        Button.create [ Button.text (sprintf "%d. %s" p.Index p.Title); Button.onClick (SelectPage p.Id) ]
    Stack.create [ Stack.orientation "vertical"; Stack.children (Pages.all |> List.map item) ]

let private content (model: GalleryModel): Control<GalleryMsg> =
    let page = Pages.byId model.CurrentPage
    let body = page.Build model.PageState
    let props = FS.GG.UI.Controls.Typed.ScrollViewer.defaults "content-scroll" (Widget.ofControl body)
    Widget.toControl (FS.GG.UI.Controls.Typed.ScrollViewer.view props)

let private statusStrip (model: GalleryModel): Control<GalleryMsg> =
    let page = Pages.byId model.CurrentPage
    let modeText =
        match model.Mode with
        | Light -> "light"
        | Dark -> "dark"
    let text =
        sprintf
            "Page %d/%d · %s · theme=%s · accent=%s · %d controls"
            page.Index
            (List.length Pages.all)
            page.Id
            modeText
            (GalleryTheme.accentId model.Accent)
            (List.length page.ControlIds)
    TextBlock.create [ TextBlock.text text ]

/// The full shell. `size` is accepted to satisfy the size-aware View seam; the layout
/// is composition-driven so it adapts without reading exact pixels here.
let view (_size: Size) (model: GalleryModel): Control<GalleryMsg> =
    Stack.create
        [ Stack.orientation "vertical"
          Stack.children
              [ appBar model
                Stack.create [ Stack.orientation "horizontal"; Stack.children [ navRail model; content model ] ]
                statusStrip model ] ]
