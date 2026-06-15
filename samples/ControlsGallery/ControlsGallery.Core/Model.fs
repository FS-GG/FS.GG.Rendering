/// Pure MVU core (Principle IV): the gallery's state, events, and `update`. No I/O,
/// no GL — those live at the App edge. Type shapes follow data-model.md.
module ControlsGallery.Core.Model

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Theming

/// Per-control seeded interactive state, shared by every page's `Build`. Populated so
/// no control renders empty (FR-004) and so interactive controls have somewhere to
/// record a visible state change (FR-012).
type DemoState =
    { ButtonClicks: int
      TextValue: string
      AreaValue: string
      NumericValue: float
      SliderValue: float
      Checked: bool
      SwitchOn: bool
      ToggleOn: bool
      RadioSelected: string
      ComboSelected: string
      ListSelected: string
      MultiSelected: string list
      TreeSelected: string
      Tab: string
      MenuSelected: string
      ProgressValue: float
      ColorSelected: string
      OverlayOpen: bool
      DialogOpen: bool }

/// Control-interaction events routed to the active page (FR-012). Kept flat and pure;
/// every case maps to a single field transition in `update`.
type PageMsg =
    | ButtonClicked
    | TextChanged of string
    | AreaChanged of string
    | NumericChanged of float
    | SliderChanged of float
    | CheckChanged of bool
    | SwitchChanged of bool
    | ToggleChanged of bool
    | RadioChanged of string
    | ComboChanged of string
    | ListSelectedMsg of string
    | MultiChanged of string list
    | TreeSelectedMsg of string
    | TabChanged of string
    | MenuSelectedMsg of string
    | ColorChanged of string
    | OverlayToggled of bool
    | DialogToggled of bool

/// Top-level gallery events.
type GalleryMsg =
    | SelectPage of string
    | ToggleTheme
    | SelectAccent of Color
    | PageMsg of PageMsg

/// One of the exactly-10 navigable pages (data-model.md "GalleryPage").
type GalleryPage =
    { Id: string
      Index: int
      Title: string
      Family: string
      ControlIds: string list
      Build: DemoState -> Control<GalleryMsg> }

/// Outcome of the coverage check (FR-003): empty/empty ⇒ pass.
type CoverageResult =
    { Unreferenced: string list
      Duplicated: string list }

/// The MVU model.
type GalleryModel =
    { CurrentPage: string
      Mode: ThemeMode
      Accent: Color
      PageState: DemoState }

/// Pure interaction reducer for the active page.
let updatePage (msg: PageMsg) (state: DemoState): DemoState =
    match msg with
    | ButtonClicked -> { state with ButtonClicks = state.ButtonClicks + 1 }
    | TextChanged v -> { state with TextValue = v }
    | AreaChanged v -> { state with AreaValue = v }
    | NumericChanged v -> { state with NumericValue = v }
    | SliderChanged v -> { state with SliderValue = v }
    | CheckChanged v -> { state with Checked = v }
    | SwitchChanged v -> { state with SwitchOn = v }
    | ToggleChanged v -> { state with ToggleOn = v }
    | RadioChanged v -> { state with RadioSelected = v }
    | ComboChanged v -> { state with ComboSelected = v }
    | ListSelectedMsg v -> { state with ListSelected = v }
    | MultiChanged v -> { state with MultiSelected = v }
    | TreeSelectedMsg v -> { state with TreeSelected = v }
    | TabChanged v -> { state with Tab = v }
    | MenuSelectedMsg v -> { state with MenuSelected = v }
    | ColorChanged v -> { state with ColorSelected = v }
    | OverlayToggled v -> { state with OverlayOpen = v }
    | DialogToggled v -> { state with DialogOpen = v }

/// Pure top-level reducer (Principle IV). Theme/accent changes alter only resolved
/// visuals downstream — never the control-tree shape (FR-006/SC-003).
let update (msg: GalleryMsg) (model: GalleryModel): GalleryModel =
    match msg with
    | SelectPage id -> { model with CurrentPage = id }
    | ToggleTheme ->
        let flipped =
            match model.Mode with
            | Light -> Dark
            | Dark -> Light
        { model with Mode = flipped }
    | SelectAccent c -> { model with Accent = c }
    | PageMsg pm -> { model with PageState = updatePage pm model.PageState }
