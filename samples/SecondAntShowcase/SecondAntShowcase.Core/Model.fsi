module SecondAntShowcase.Core.Model

open System
open FS.GG.UI.Controls

type ThemeMode =
    | Light
    | Dark

type PageKind =
    | Catalog
    | Template

type FormPhase =
    | Editing
    | Invalid of errors: (string * string) list
    | Submitted

type FormState =
    { Name: string
      Email: string
      Role: string
      Agree: bool
      Phase: FormPhase }

type DemoState =
    { TextValue: string
      AreaValue: string
      NumericValue: float
      SliderValue: float
      RateValue: float
      AutoCompleteValue: string
      UploadValue: string
      ButtonClicks: int
      ToggleOn: bool
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
      Tab: string
      MenuSelected: string
      StepsCurrent: int
      PaginationPage: int
      CollapseOpen: string
      ProgressValue: float
      OverlayOpen: bool
      DialogOpen: bool
      DrawerOpen: bool
      DatePickerOpen: bool
      DatePickerSelected: DateOnly option
      DatePickerFocused: ControlId option
      Form: FormState }

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
    | DatePickerOpenChanged of bool
    | DatePickerChanged of DateOnly
    | DatePickerFocusChanged of ControlId option
    | FormFieldChanged of field: string * value: string
    | FormSubmitted

type FeedbackEntry =
    { PageId: string
      Text: string }

type SecondAntShowcaseMsg =
    | NavigateTo of pageId: string
    | ToggleMode
    | PageMsg of PageMsg
    | FeedbackChanged of string
    | FeedbackSubmitted

type Page =
    { Id: string
      Title: string
      Kind: PageKind
      ControlIds: string list
      View: DemoState -> Control<SecondAntShowcaseMsg> }

type SecondAntShowcaseModel =
    { CurrentPage: string
      Mode: ThemeMode
      PageState: DemoState
      FeedbackDraft: string
      Feedback: FeedbackEntry list }

type CoverageResult =
    { Unreferenced: string list
      Duplicated: string list }

val isClean: r: CoverageResult -> bool
val validateForm: form: FormState -> (string * string) list
val updatePage: msg: PageMsg -> state: DemoState -> DemoState
val update: msg: SecondAntShowcaseMsg -> model: SecondAntShowcaseModel -> SecondAntShowcaseModel
val encodeFeedbackLine: e: FeedbackEntry -> string
val decodeFeedbackLine: line: string -> FeedbackEntry option
