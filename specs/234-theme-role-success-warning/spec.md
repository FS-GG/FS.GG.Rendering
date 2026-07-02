# Feature Specification: theme-role Success/Warning (drop the `theme.Name` string-match)

**Feature Branch**: `234-theme-role-success-warning`

**Created**: 2026-07-02

**Status**: Draft

**Input**: Finding P3 / D1 of the [2026-07-02 repo review](../../docs/reports/2026-07-02-14-07-repo-code-quality-and-architecture-review.md). Resolves **FS-GG/FS.GG.Rendering#46**.

## Context (non-normative)

The pure state→style resolver `Style` (`src/DesignSystem/Style.fs`) resolves the `Success` and
`Warning` intents (variant fills and the `Valid`/`Pending` validation strokes) via two helpers,
`successColor`/`warningColor`. Those helpers used to branch on a string:

```fsharp
let isDark (theme: Theme) = theme.Name = "dark"
let successColor theme = if isDark theme then DesignTokens.Dark.success else DesignTokens.Light.success
let warningColor theme = if isDark theme then DesignTokens.Dark.warning else DesignTokens.Light.warning
```

**Two defects:**

1. **`Theme.Success` / `Theme.Warning` are never read.** Feature 125 promoted success/warning to
   first-class `Theme` role colours (`src/DesignSystem/Types.DesignSystem.fs:57-58`), but the resolver
   kept reading Default DTCG token *literals* and ignored the theme's own fields. The comment at
   `Style.fs:12` ("success/warning colours are NOT `Theme` fields") has been false since feature 125,
   and this contradicts the `Style.fsi:23-25` contract ("every colour the variant/state layers read
   originates from [the theme]").
2. **Brittle `Name` string-match.** `isDark` only recognised the literal name `"dark"`. Every theme
   whose name is anything else resolved success/warning to Default **light** tokens:
   - `AntTheme.antDark` (`Name = "AntDesign Dark"`) → its own Ant `Success`/`Warning` seed colours were
     ignored; it painted Default light green/amber.
   - `Theming.toTheme` output pins `Name = "light"` (it seeds from `Theme.light`), so a **Dark**
     live-theme also resolved light success/warning — and silently dropped mode-appropriate
     `Success`/`Warning` even though the caller asked for `Dark`.
   - Any custom theme's `Success`/`Warning` were silently discarded.

**Related:** `Theming.toTheme` also always seeded the non-colour + success/warning fields from
`Theme.light` regardless of the palette's mode (it received a `RolePalette` that did not carry the
mode), and dropped the palette's `FocusRing`.

**The fix:** read `theme.Success` / `theme.Warning` directly at each site (removing `isDark` and the
`DesignTokens` literal reads entirely), and carry the resolved `ThemeMode` on `RolePalette` so
`Theming.toTheme` seeds the framework `Theme` from the matching base — making `Success`/`Warning`,
`Name`, and the mode-scoped non-colour fields correct for a live-themed Dark palette.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A theme's own Success/Warning colours are honoured (Priority: P1)

A designer selects (or authors) a theme — AntDesign, a custom brand theme, or a live-themed Dark
palette — and expects the Success and Warning intents (success buttons, valid/pending field strokes) to
paint that theme's success/warning role colours.

**Why this priority**: This is the reported defect. The resolver silently substituting Default light
tokens is a visible mis-theming on every non-Default (and every live-themed Dark) surface, and it
violates the resolver's own "no theme-identity branching / every colour from the theme" contract.

**Independent Test**: Fully testable at the resolver seam: resolve the Success variant and the Pending
validation state under AntDesign Dark, a custom theme with a unique name, and a Dark `Theming.toTheme`
palette, and assert the resolved colour equals the theme's own `Success`/`Warning`.

**Acceptance Scenarios**:

1. **Given** `AntTheme.antDark` (name "AntDesign Dark"), **When** the Success variant resolves, **Then**
   the fill is `antDark.Success` (the Ant seed), not Default light success.
2. **Given** a custom theme with a non-"light"/"dark" name and bespoke `Success`/`Warning`, **When**
   Success/Pending resolve, **Then** they use the custom theme's `Success`/`Warning`.
3. **Given** `Theming.toTheme (Theming.resolve Dark accent)`, **When** the Success variant resolves,
   **Then** the fill is the Dark success token (the projected theme carries dark `Success`/`Warning`).
4. **Given** the built-in `Theme.light`/`Theme.dark`, **When** Success/Warning resolve, **Then** the
   result is unchanged from before (no regression).

### Edge Cases

- **No control branches on `theme.Name`** — the Feature 132 parity guard (two themes differing only by
  `Name` must paint identically) continues to hold; removing `isDark` strengthens it.
- **`RolePalette` gains a required field** — the only constructor is `Theming.resolve`; no external
  construction site exists, so the additive record field is source-safe in-repo.
- **`toTheme` `Name` change** — the projected paint theme's `Name` now reflects the palette mode
  ("dark" for a Dark palette). Safe: the fragment-reuse key uses the static `host.Theme`, not the paint
  theme, and nothing else reads a theme's `Name` string value.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `Style.successColor` / `Style.warningColor` MUST return `theme.Success` / `theme.Warning`
  respectively — the active theme's own role colours (FR-008: colour originates from the theme).
- **FR-002**: The resolver MUST NOT branch on `theme.Name` (or any theme-identity string). `isDark` and
  the `DesignTokens.Light/Dark.success/warning` literal reads are removed from `Style.fs`.
- **FR-003**: `RolePalette` MUST carry the `ThemeMode` it was resolved against; `Theming.resolve` MUST
  populate it.
- **FR-004**: `Theming.toTheme` MUST seed the framework `Theme` from the palette's own mode base
  (`Theme.light`/`Theme.dark`), so the projected `Success`/`Warning`/`Name` and mode-scoped non-colour
  fields are mode-correct; the neutral role colours are then overwritten from the palette.
- **FR-005**: Built-in `Theme.light`/`Theme.dark` success/warning resolution MUST be unchanged
  (behaviour-preserving for the Default themes).
- **FR-006**: The `.fsi` docs MUST be corrected: `Style.fs`'s "success/warning are NOT Theme fields"
  comment and `Theming.fsi`'s "carry from `Theme.light`" note.

### Key Entities

- **Theme** (`src/DesignSystem/Types.DesignSystem.fs`): carries `Success`/`Warning` role colours (feature
  125); now actually read by the resolver. Unchanged shape.
- **RolePalette** (`src/Themes.Default/Theming.fsi`): the live-theming role palette; gains a `Mode:
  ThemeMode` field.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Under AntDesign Dark, a custom-named theme, and a Dark `Theming.toTheme` palette, the
  Success variant and Pending validation resolve to the theme's own `Success`/`Warning` — proven by
  fail-before/pass-after tests.
- **SC-002**: `Theming.toTheme (resolve Dark _)` projects a `Theme` whose `Success`/`Warning`/`Name`
  match `Theme.dark`.
- **SC-003**: Built-in Default `Theme.light`/`Theme.dark` success/warning resolution is byte-identical
  to before (no regression); the full existing suite passes with 0 new failures.
- **SC-004**: No new exported types (surface baseline unchanged); the additive `RolePalette.Mode` field
  is documented in `Theming.fsi`.

## Assumptions

- No consumer constructs a `RolePalette` directly (only `Theming.resolve` does), so adding the required
  `Mode` field is source-safe. Verified by repo-wide grep.
- Nothing reads a `Theme`'s `Name` as a behavioural switch other than the removed `Style.isDark`.
  Verified by grep (`ControlsElmish` `.Name` hits are attribute names, not theme names).
- Light and Dark DTCG tokens share identical density/cornerRadius/contrastRequiredRatio/font values, so
  mode-based seeding in `toTheme` changes only `Name` + `Success`/`Warning` in practice.
- `RolePalette.FocusRing` remains dropped by `toTheme` (the `Theme` record has no `FocusRing` field and
  the focus ring is `= Accent` today, so no behavioural bug); wiring a first-class `Theme.FocusRing` is
  a separate enhancement — out of scope (see `research.md`).
