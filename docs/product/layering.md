# Design and control layering

> Migration Stage R2 deliverable, adapted from the source `docs/FS.GG/design-and-controls.md`.
> It defines the UI layer boundaries the rendering product commits to. It MUST stay consistent
> with the constitution's Engineering-Constraints layering clause; where they overlap they
> agree, and if they ever diverge the constitution wins.

## Ownership

The rendering repository owns design and controls as separate layers inside the product:
controls own **semantics and behavior**, while the design-system and theme layers own
**visual decisions**. Specifically it owns semantic controls (`Button`, `TextBox`,
`ComboBox`, `DataGrid`, `Dialog`), design-system primitives (tokens, theme records, density,
typography, radii, shadows, color roles, motion, icons, visual-state rules), concrete themes
(Ant Design, Fluent, Material-inspired), and optional design-specific kits where a design
language defines real product patterns beyond styling. Governance does not own design
decisions.

## The four layers

### 1. Semantic controls

- **Owns**: input behavior, focus behavior, accessibility role, state machine, value model,
  command semantics, keyboard behavior. One behavior/accessibility contract per control.
- **Does NOT own**: colors, spacing, typography, radii, shadows, icon choice, or any visual
  treatment (those belong to Themes); opinionated multi-control compositions (those belong to
  Kits).
- **Examples**: `Button`, `TextBox`, `ComboBox`, `DataGrid`, `Dialog`.

### 2. Design-system primitives

- **Owns**: the shared token model and slot names used across themes — component token slots,
  density, typography scale, radii, color roles, motion, visual-state rule definitions.
- **Does NOT own**: behavior or accessibility (Controls); concrete values for a specific look
  (Themes); composition/workflow (Kits).
- **Examples**: `tokens`, `theme records`, `density`, `color roles`, `visual-state rules`.

### 3. Themes

- **Owns**: concrete values and style mappings for a design language — color palettes,
  typography, spacing, density, radii, shadows, hover/pressed/disabled/focused/validation
  visuals, default icons.
- **Does NOT own**: control behavior or accessibility (Controls); the slot/token vocabulary
  itself (Design system); product workflows (Kits).
- **Examples**: `Themes.AntDesign`, `Themes.Fluent`, `Themes.Material`.

### 4. Design-specific kits

- **Owns**: opinionated compositions that encode a design language's product patterns — form
  layout and validation flow, table filtering/sorting/empty-state conventions, layout
  primitives implying child structure, result/statistic/description/page-header components,
  opinionated loading/error/empty/success states.
- **Does NOT own**: single-control behavior (Controls) or pure visual treatment (Themes); a
  kit is justified only when a design language adds composition or workflow behavior beyond
  styling.
- **Examples**: `AntDesign.Form`, `AntDesign.Table`, `AntDesign.Result`, `AntDesign.Descriptions`.

## One semantic control set, many themes

The default is **one** semantic control set styled by **many** themes:

```text
Controls.Button + Themes.AntDesign
Controls.Button + Themes.Fluent
Controls.Button + Themes.Material
```

`Button` keeps one behavior and accessibility contract; the theme changes component tokens,
colors, spacing, typography, radius, border, shadow, icons, and visual-state styling. Creating
`AntButton`, `FluentButton`, and `MaterialButton` as **separate behavior surfaces is rejected
by default.**

**Why**: forking controls per theme multiplies the things that must stay in sync for every
design language — tests, public API and docs, accessibility behavior, keyboard behavior, focus
rules, and generated examples. One behavior contract with swappable themes keeps all of those
single-sourced. A new control *type* is justified only by genuinely new behavior, never by a
new look.

## Decision rule — which layer?

Use the smallest layer that preserves the contract:

| Change type | Layer |
|---|---|
| Visual tokens, color, spacing, typography, radius, shadow, density, icon choice, visual states | Theme |
| Shared style slots / token names needed across themes | Design system |
| Input/focus/accessibility behavior, state machine, value model, command semantics | Control |
| Opinionated composition, data workflow, validation layout, table behavior, expected child structure | Kit / pattern |

### Worked examples

- A new `DatePicker` with keyboard/focus behavior → **Control**.
- A `spacing.lg` token used across themes → **Design system**.
- A Fluent-styled skin for the existing `Button` → **Theme**.
- `AntDesign.Form` with validation-flow layout → **Kit / pattern**.
- A proposed separate `FluentButton` behavior type → **Rejected** by the one-control-set rule;
  implement it as a Fluent theme over the existing `Button`.
