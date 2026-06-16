# Phase 1 Data Model: Concrete Ant Design theme (D2.1)

Entities are expressed against the **existing public `DesignSystem` types** (no new shapes where one exists). New named entities are the coverage matrix row and the net-new control catalog entry.

## 1. AntDesign Theme value(s)

Reuses `FS.GG.UI.DesignSystem.Theme` (flat record) — **no new type**.

| Field | Source | Notes |
|---|---|---|
| `Name` | `"AntDesign"` / `"AntDesign Dark"` | identifies the theme; read by parity test only for labelling, never branched on |
| `Foreground` / `Background` / `Muted` | Ant `DesignTokensExt` alias entries (light/dark) | surface + text roles |
| `Accent` | Ant brand-blue seed/map entry | Ant primary `#1677ff` family (from generated tokens, not a literal) |
| `Danger` / `Success` / `Warning` | Ant functional families | drives intent + feedback controls |
| `FontFamily` / `FontSize` | Ant `DesignTokensExt.Type` | Ant type scale |
| `Density` | Ant named density | Ant `controlHeight 32` / 8-unit grid expressed via density+space |
| `CornerRadius` | Ant alias radius | Ant default radius |
| `ContrastRequiredRatio` | per active color policy (F2) | `ant` policy thresholds when selected |

**Values**: `antLight: Theme`, `antDark: Theme` (mirrors `Theme.light`/`Theme.dark`). Validation: every field sourced from a `DesignTokensExt` entry; honesty check confirms referenced token entries exist.

## 2. AntIntentPolicy

Reuses `FS.GG.UI.DesignSystem.StyleResolver.IntentPolicy` — **no new type**.

```
AntIntentPolicy : IntentPolicy
  ApplyIntent : Theme -> intent:string -> ResolvedStyle -> ResolvedStyle
```

| Intent string | Resolved effect (over the structural base) |
|---|---|
| `"primary"` | brand-blue fill, on-primary foreground |
| `"default"` | neutral surface fill + accent/neutral outline |
| `"dashed"` | as default, dashed stroke |
| `"text"` | no fill/stroke, accent-on-hover foreground |
| `"link"` | no fill/stroke, accent foreground |
| `"danger"` | `theme.Danger` applied to fill/stroke/foreground per kind |
| `""` / unknown | identity (structural base unchanged) — total, never raises |

State transitions: none (pure function). Composes *before* the 093 back-half `Style.resolve` class+state overlay, preserving precedence (base → classes → visual state) per FR — the front half only adjusts the base.

## 3. Net-new control (catalog entry)

Each net-new control is a `Catalog` row + a `.fsi`/`.fs` module, identical in shape to existing controls.

| Attribute | Constraint |
|---|---|
| `id` | kebab-case, unique in `catalog.yml` (e.g. `tag`, `alert`, `avatar`, `collapse`, `segmented`, `rate`, `timeline`, `steps`, `breadcrumb`, `pagination`, `card`, `result`, `empty`, `drawer`, `skeleton`, …) |
| `category` | one of the existing categories (display / input / selection / navigation / layout / feedback / data / overlay / custom) |
| `module` / `typedModule` | F# module name in `Controls` |
| `requiredAttributes` / `commonAttributes` | standard set (`enabled, visible, width, height, padding, style, theme, accessibility`) + control-specific |
| `visualStates` | the standard 8 (`normal, disabled, hover, pressed, focused, selected, validation, loading`) — Ant-specific states (e.g. Steps finished/error, Collapse expanded) expressed via these or documented |
| `accessibility` | role + nameSource + stateMetadata + focus/keyboard, same schema as existing rows |
| `events` | e.g. `onClick`, `onChange`, `onClose` where interactive |
| `supportStatus` | `supported` |

Invariants: renders coherently under **both** themes; **no** branch on theme identity; passes all five test families. Stateful controls expose `Model`/`Msg`/`Effect` (DataGrid pattern) when workflow-bearing; otherwise parent-owned state via attributes + events.

## 4. Coverage matrix row

The matrix is a doc table (`docs/product/ant-design/coverage/ant-component-coverage.md`); each row:

| Column | Meaning | Validated by honesty check |
|---|---|---|
| `antComponent` | exact Ant overview name | must be present for every overview entry (no gaps) |
| `antCategory` | Ant's grouping (General/Layout/Navigation/Data Entry/Data Display/Feedback/Other) | informational |
| `disposition` | `existing` \| `net-new` \| `composition` \| `not-applicable` | must be one of the four; never blank |
| `repoControls` | control id(s) involved (for existing/net-new/composition) | each id must exist in `Catalog` |
| `tokenEntries` | ≥1 `DesignTokensExt` entry the styling uses (for covered rows) | each must exist in the `DesignSystem` public surface |
| `rationale` | one line; required for `composition` and `not-applicable` | non-empty |

Matrix header records: Ant source (the repo Ant reference hub) and the hub's snapshot retrieval date (`2026-06-16`) — the hub is the single owner of that date; there is no upstream Ant version label to record.

## 5. Provenance / decision record

A `docs/product/decisions/` entry recording: the new public package, the list of net-new public controls (surface delta), the chosen Ant snapshot, and the "no token-value change / opt-in / no fork" guarantees. Referenced from `module-map.md` (AntDesign theme row: planned → owned assembly).
