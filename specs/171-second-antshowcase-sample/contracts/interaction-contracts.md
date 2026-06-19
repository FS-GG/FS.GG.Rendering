# Contract: Interaction Contracts

## Interaction Coverage

Every interactive catalog control must map to at least one interaction contract. Contracts must cover these interaction families when present in the current catalog:

- buttons/actions
- text entry
- numeric input
- date/time selection
- sliders/rating
- toggles
- single and multi-selection
- navigation
- paging
- menus
- overlays
- feedback
- validation
- data collections
- charts
- graphs
- custom surfaces

Display-only controls are exempt from state-changing requirements only when the demonstration records a display-only reason.

## Contract Shape

Each interaction contract must define:

- `ContractId`
- `ControlIds`
- `PageId`
- `StartingState`
- `Action`
- `ExpectedStateChange`
- `VisibleEvidence`
- `ScriptStep`
- `ThemeInvariant`
- `DisplayOnlyReason`, when applicable

## Required Behaviors

Examples of acceptable visible state changes:

- button click count, command status, or feedback message changes
- entered text appears in the control or status area
- numeric, date, time, slider, or rating value changes
- checkbox, switch, radio, list, combo, cascader, color, or segmented selection changes
- active tab, menu item, step, breadcrumb/anchor state, or pagination page changes
- overlay, drawer, dialog, popover, popconfirm, tooltip, or tour opens or closes
- form validation errors appear for invalid data and success appears only for valid data
- data grid/list/tree filtering, selection, expansion, or paging changes
- chart/graph selection, highlighted datum, active series, or status changes
- custom surface records pointer/keyboard interaction visibly

## MVU Requirements

The pure Core update function must own interaction state:

```text
Model + Msg -> Model
```

Rules:

- App callbacks dispatch messages; they do not mutate hidden state.
- Theme switching dispatches a visual-mode message and preserves control values.
- Scripted interactions reuse the same message/update path as interactive mode where practical.
- Deterministic evidence uses seeded input and stable ordered script steps.

## Test Requirements

Required sample tests:

- every interactive control has at least one contract
- every display-only control has a display-only reason
- each contract changes the expected field or state
- theme switch preserves active interaction state
- form invalid submission shows errors and not success
- form valid submission shows success
- list template filtering/selection/paging changes visible state
- exception template records or displays the selected recovery path
- scripted runs with the same seed produce the same outcomes

## Failure Contract

Interaction evidence fails when:

- a control has no contract and no display-only reason
- the expected state does not change
- the change is not visible to a reviewer
- behavior differs between Ant light and Ant dark except for visual treatment
- a script step references an unknown page, control, theme, or action
