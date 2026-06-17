# Phase 1 Data Model: Ant Design Controls Showcase (G3)

Entities are application-internal (the sample exposes no public surface). Types are plain F# records/unions
reusing the G1 shapes where they fit. `'Msg = AntShowcaseMsg` throughout.

## 1. PageKind

```
type PageKind =
    | Catalog    // a control-family page; its ControlIds participate in the coverage bijection
    | Template   // an enterprise template page; a composition, EXEMPT from the bijection (R2/R4)
```

## 2. Page

Reuses G1's page shape, extended with `Kind`.

| Field | Type | Notes |
|---|---|---|
| `Id` | `string` | stable kebab-case page id, unique across the registry (nav + evidence dir) |
| `Title` | `string` | shown in the nav rail + app bar |
| `Kind` | `PageKind` | `Catalog` or `Template` |
| `ControlIds` | `string list` | for `Catalog` pages: the catalog ids shown here (bijection domain). For `Template` pages: **`[]`** (exempt); the composed control *types* are asserted by `TemplateTests`, not counted here |
| `view` | `DemoState -> Control<AntShowcaseMsg>` | builds the page body from seeded state |

Invariant (Catalog pages): the multiset union of all `Catalog` pages' `ControlIds` equals
`Catalog.supportedControls` ids exactly — zero unreferenced, zero duplicated (R4).

## 3. AntShowcaseModel

| Field | Type | Notes |
|---|---|---|
| `CurrentPage` | `string` | active page id; defaults to the first family page |
| `Mode` | `ThemeMode` | `Light`→`antLight`, `Dark`→`antDark` (R3). No accent field (unlike G1) |
| `PageState` | `DemoState` | the seeded per-control demo state, incl. form state |

## 4. AntShowcaseMsg

```
type AntShowcaseMsg =
    | NavigateTo of pageId: string
    | ToggleMode                     // antLight <-> antDark
    | PageMsg of PageMsg             // control interactions (button click, field edit, toggle, …)
```

`PageMsg` (interaction sub-union, extended from G1 for the net-new controls and the form):
`ButtonClicked | ToggleChanged of bool | SelectionChanged of string | TextChanged of string |
FormFieldChanged of field:string * value:string | FormSubmitted | StepChanged of int |
PageChanged of int | …`. `update` is **pure**; effects always `[]` (I/O at the App edge only).

## 5. DemoState

Seeded representative content so **no control renders empty** (FR-004, R5). Literal constants only
(deterministic). Extends G1's `DemoState` with the net-new Ant controls and the form sub-state. Indicative
fields:

| Group | Example fields |
|---|---|
| Text/numeric | `TextValue`, `AreaValue`, `NumericValue`, `SliderValue`, `RateValue` |
| Buttons | `ButtonClicks`, `ToggleOn` |
| Selection | `Checked`, `RadioChoice`, `SwitchOn`, `ListSelection`, `MultiSelection`, `ComboChoice`, `CascaderPath`, `ColorValue` |
| Net-new Ant | `TimelineItems`, `StepsCurrent`/`StepsItems`, `CollapseOpen`, `SegmentedChoice`, `PaginationPage`/`Total`, `BreadcrumbTrail`, `TagSamples`, `AlertSamples`, `CardItems`, `Avatars` |
| Data | `ListItems`, `TreeNodes`, `GridRows` |
| Form (template) | `FormState : FormState` (below) |

### 5a. FormState (enterprise form template, R6 / FR-006 / SC-009)

```
type FormPhase = Editing | Invalid of errors: (string * string) list | Submitted

type FormState =
    { Name: string; Email: string; Role: string; Agree: bool; Phase: FormPhase }
```

State transitions (pure, in `update`):

| From | Msg | Guard | To |
|---|---|---|---|
| `Editing`/`Invalid` | `FormFieldChanged(f,v)` | — | `Editing` with field updated |
| any | `FormSubmitted` | validation fails (empty Name, malformed Email, no Agree) | `Invalid errors` (visible field errors, **no** success) |
| any | `FormSubmitted` | validation passes | `Submitted` (renders the `result` success control) |

`TemplateTests` assert: invalid submit ⇒ `Invalid` and the rendered tree contains `validation-message`
controls and **no** `result`-success node; valid submit ⇒ `Submitted` and a `result` node appears.

## 6. CoverageResult

Reused from G1 verbatim.

```
type CoverageResult = { Unreferenced: string list; Duplicated: string list }
```

`isClean r = List.isEmpty r.Unreferenced && List.isEmpty r.Duplicated`.

## 7. Page Evidence Record

Reused from G1's `Evidence.fs` (package-only record), Ant-themed. Per page: `PageId`, `Seed`, `Mode`
(antLight/antDark), the final state outcome (`state.txt`), the screenshot result (`frame.png` +
`ScreenshotEvidenceResult` fields), and the non-empty `NotAuthoritativeFor` disclosure (FR-012). Serialized
to `artifacts/ant-showcase/<seed>/<page-id>/{run.json,summary.md,frame.png,state.txt}`.

## 8. Catalog binding

`Catalog.supportedControls : ControlDescriptor list` (public, from `FS.GG.UI.Controls`) — the live domain of
the coverage bijection (96 controls after the R1 feed refresh). The showcase never hard-codes the count.
