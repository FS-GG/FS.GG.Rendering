/// Pure MVU core (Principle IV): the showcase's state, events, and `update`. No I/O,
/// no GL — those live at the App edge. Type shapes follow data-model.md. `ThemeMode` is
/// defined here (the sample owns its own Light/Dark tag; `AntTheme.resolve` maps it to the
/// shipped antLight/antDark — R3), so this module has no theme-package dependency.
module AntShowcase.Core.Model

open FS.GG.UI.Controls

/// Light/Dark selector. Maps to AntTheme.antLight / AntTheme.antDark (R3). No accent seam
/// (unlike G1) — Ant's brand-blue is intrinsic to the theme.
type ThemeMode =
    | Light
    | Dark

/// A page is either a control-family page (its `ControlIds` join the coverage bijection)
/// or an enterprise template page (a composition, EXEMPT from the bijection — R2/R4).
type PageKind =
    | Catalog
    | Template

/// Enterprise form template phase (data-model §5a / FR-006 / SC-009).
type FormPhase =
    | Editing
    | Invalid of errors: (string * string) list
    | Submitted

/// The form template's parent-owned state.
type FormState =
    { Name: string
      Email: string
      Role: string
      Agree: bool
      Phase: FormPhase }

/// Per-control seeded interactive state, shared by every page's `view`. Populated so no
/// control renders empty (FR-004) and so interactive controls have somewhere to record a
/// visible state change (FR-014). Static demo content (option sets, collection rows,
/// chart series) lives as module literals in `DemoState.fs`; only interaction-bearing
/// values are fields here.
type DemoState =
    { // Text / numeric
      TextValue: string
      AreaValue: string
      NumericValue: float
      SliderValue: float
      RateValue: float
      AutoCompleteValue: string
      UploadValue: string
      // Buttons
      ButtonClicks: int
      ToggleOn: bool
      // Selection / toggles
      Checked: bool
      SwitchOn: bool
      RadioSelected: string
      SegmentedSelected: string
      ComboSelected: string
      ListSelected: string
      MultiSelected: string list
      TreeSelected: string
      CascaderSelected: string
      ColorSelected: string
      // Navigation
      Tab: string
      MenuSelected: string
      StepsCurrent: int
      PaginationPage: int
      CollapseOpen: string
      // Feedback / overlays
      ProgressValue: float
      OverlayOpen: bool
      DialogOpen: bool
      DrawerOpen: bool
      // Enterprise form template
      Form: FormState }

/// Control-interaction events routed to the active page (FR-014). Kept flat and pure;
/// each case maps to a single field transition in `updatePage` (interaction-contract.md).
type PageMsg =
    | ButtonClicked
    | TextChanged of string
    | AreaChanged of string
    | NumericChanged of float
    | SliderChanged of float
    | RateChanged of float
    | AutoCompleteChanged of string
    | UploadChanged of string
    | CheckChanged of bool
    | SwitchChanged of bool
    | ToggleChanged of bool
    | RadioChanged of string
    | SegmentedChanged of string
    | ComboChanged of string
    | ListSelectedMsg of string
    | MultiChanged of string list
    | TreeSelectedMsg of string
    | CascaderChanged of string
    | ColorChanged of string
    | TabChanged of string
    | MenuSelectedMsg of string
    | StepChanged of int
    | PageChanged of int
    | CollapseToggled of string
    | OverlayToggled of bool
    | DialogToggled of bool
    | DrawerToggled of bool
    | FormFieldChanged of field: string * value: string
    | FormSubmitted

/// Top-level showcase events.
type AntShowcaseMsg =
    | NavigateTo of pageId: string
    | ToggleMode
    | PageMsg of PageMsg

/// A navigable page (data-model §2). `view` builds the body from seeded state.
type Page =
    { Id: string
      Title: string
      Kind: PageKind
      ControlIds: string list
      View: DemoState -> Control<AntShowcaseMsg> }

/// The MVU model (data-model §3).
type AntShowcaseModel =
    { CurrentPage: string
      Mode: ThemeMode
      PageState: DemoState }

/// Outcome of the coverage check (FR-003): empty/empty ⇒ pass (data-model §6).
type CoverageResult =
    { Unreferenced: string list
      Duplicated: string list }

let isClean (r: CoverageResult): bool =
    List.isEmpty r.Unreferenced && List.isEmpty r.Duplicated

// --- form validation (pure; data-model §5a / contracts/enterprise-templates.md) -------

/// Validate the form, returning the (field, message) errors. Empty ⇒ valid.
let validateForm (form: FormState): (string * string) list =
    [ if System.String.IsNullOrWhiteSpace form.Name then
          "Name", "Name is required"
      if not (form.Email.Contains "@" && form.Email.Contains ".") then
          "Email", "Enter a valid email address"
      if not form.Agree then
          "Agree", "You must accept the terms" ]

// --- reducers -------------------------------------------------------------------------

/// Pure interaction reducer for the active page. The form transitions (FR-006/SC-009):
/// field edits move to `Editing`; submit validates → `Invalid errors` or `Submitted`.
let updatePage (msg: PageMsg) (state: DemoState): DemoState =
    match msg with
    | ButtonClicked -> { state with ButtonClicks = state.ButtonClicks + 1 }
    | TextChanged v -> { state with TextValue = v }
    | AreaChanged v -> { state with AreaValue = v }
    | NumericChanged v -> { state with NumericValue = v }
    | SliderChanged v -> { state with SliderValue = v }
    | RateChanged v -> { state with RateValue = v }
    | AutoCompleteChanged v -> { state with AutoCompleteValue = v }
    | UploadChanged v -> { state with UploadValue = v }
    | CheckChanged v -> { state with Checked = v }
    | SwitchChanged v -> { state with SwitchOn = v }
    | ToggleChanged v -> { state with ToggleOn = v }
    | RadioChanged v -> { state with RadioSelected = v }
    | SegmentedChanged v -> { state with SegmentedSelected = v }
    | ComboChanged v -> { state with ComboSelected = v }
    | ListSelectedMsg v -> { state with ListSelected = v }
    | MultiChanged v -> { state with MultiSelected = v }
    | TreeSelectedMsg v -> { state with TreeSelected = v }
    | CascaderChanged v -> { state with CascaderSelected = v }
    | ColorChanged v -> { state with ColorSelected = v }
    | TabChanged v -> { state with Tab = v }
    | MenuSelectedMsg v -> { state with MenuSelected = v }
    | StepChanged v -> { state with StepsCurrent = v }
    | PageChanged v -> { state with PaginationPage = v }
    | CollapseToggled v -> { state with CollapseOpen = v }
    | OverlayToggled v -> { state with OverlayOpen = v }
    | DialogToggled v -> { state with DialogOpen = v }
    | DrawerToggled v -> { state with DrawerOpen = v }
    | FormFieldChanged(field, value) ->
        let f = state.Form
        let f' =
            match field with
            | "Name" -> { f with Name = value }
            | "Email" -> { f with Email = value }
            | "Role" -> { f with Role = value }
            | "Agree" -> { f with Agree = (value = "true") }
            | _ -> f
        { state with Form = { f' with Phase = Editing } }
    | FormSubmitted ->
        let errors = validateForm state.Form
        let phase = if List.isEmpty errors then Submitted else Invalid errors
        { state with Form = { state.Form with Phase = phase } }

/// Pure top-level reducer (Principle IV). Mode changes alter only resolved visuals
/// downstream — never the control-tree shape (FR-008/SC-003).
let update (msg: AntShowcaseMsg) (model: AntShowcaseModel): AntShowcaseModel =
    match msg with
    | NavigateTo id -> { model with CurrentPage = id }
    | ToggleMode ->
        let flipped =
            match model.Mode with
            | Light -> Dark
            | Dark -> Light
        { model with Mode = flipped }
    | PageMsg pm -> { model with PageState = updatePage pm model.PageState }
