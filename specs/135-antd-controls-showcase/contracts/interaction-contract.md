# Contract: Pointer & Keyboard Interaction (FR-014 / US5 / SC-009 form)

Defines how the showcase's interactive controls respond to seeded pointer/keyboard input, so US5 has a
concrete behavior to implement against and `InteractionTests` (T035) has an authoritative oracle. Display-only
controls are explicitly exempt. All transitions are pure `update` reductions (no I/O in `update`); the App
edge maps raw `ViewerKey`/pointer events to `AntShowcaseMsg` via `Host.mapKey`/`mapPointer`.

## Input mapping (Host edge)

| Raw input | Maps to | Notes |
|---|---|---|
| `Enter` / `Space` (key down) on focused command | `PageMsg ButtonClicked` | activation of the focused button-like control |
| Pointer press on a control with an `onClick` | `PageMsg ButtonClicked` (or control-specific) | hit-test resolves the target control |
| Key down for navigation (page rail selection) | `NavigateTo pageId` | rail entries are activatable |
| Text entry on a focused text/numeric field | `PageMsg (TextChanged …)` / `FormFieldChanged` | per field identity |

Only **pressed** transitions are mapped (key-up is ignored), keeping seeded scripts minimal and deterministic.

## Per-family behavior contract

| Control family | Interactive? | Input → visible state change |
|---|---|---|
| Buttons (button, icon-button, split-button) | yes | activation increments/sets a visible counter or fires the bound `PageMsg` |
| Toggles (toggle-button, switch, check-box) | yes | activation flips the bound boolean; visual reflects the new state |
| Selection (radio-group, segmented, list-box, multi-select, combo-box, cascader) | yes | selection sets the bound choice; the selected item is marked |
| Text/numeric (text-box, text-area, numeric-input, slider, rate, date/time pickers, auto-complete) | yes | input updates the bound value; the field re-renders the value |
| Navigation (tabs, menu, breadcrumb, steps, pagination, anchor) | yes | activation changes the active index/page/step |
| Disclosure (collapse, drawer, dialog, overlay, popover, tooltip, popconfirm, tour) | yes | activation toggles open/closed/visible |
| Display/typography (text-block, rich-text, label, icon, separator, badge, tag, avatar, image, statistic, descriptions, qr-code, watermark, carousel, timeline, calendar, card) | **no (exempt)** | renders only; not expected to respond to input |
| Feedback status (progress-bar, spinner, validation-message, empty, skeleton, result, toast, alert) | **no (exempt)** | renders only (driven by model state, not direct input) |
| Charts / graphs / custom | **no (exempt)** | render seeded data only |

"Exempt" controls satisfy the contract by rendering correctly; `InteractionTests` asserts they do **not**
change on input and raises no error.

## Form interaction (template form page — ties to SC-009)

Drives the `FormState` transitions in data-model.md §5a:

| Sequence | Expected |
|---|---|
| edit fields (`FormFieldChanged`) | `Editing`; bound values update |
| submit with invalid input (`FormSubmitted`) | `Invalid errors`; `validation-message` controls appear; **no** success `result` |
| correct input, submit again | `Submitted`; a success `result` control appears |

The form page's seeded `Scripts.fs` entry (T032) replays an **invalid-then-valid** sequence so headless
evidence captures both states deterministically.

## Test oracle (T035)

`InteractionTests` drives seeded `Key`/`Pointer` `FrameInput` through `update` for one representative control
of each interactive family and asserts the tabulated visible state change; for one display-only control it
asserts the model/tree is unchanged. No wall-clock, no RNG.
