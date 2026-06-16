# Phase 1 Data Model — the token taxonomy

This feature defines no runtime entities; the "data model" is the **token taxonomy**: the groups,
their members, light/dark coverage, and the value rules. It is the authoritative checklist for what
the DTCG source must define and the generator must emit into `module internal DesignTokensExt`. Values
shown are the **deliberate, Ant-informed starting values**; existing primitives are reproduced exactly.

## Source / output shape

- **Source of truth**: `src/Themes.Default/design-tokens.tokens.json` (DTCG), extended with new
  top-level groups. The existing `light`/`dark` primitive blocks are kept **byte-identical**.
- **Generated output**: `src/DesignSystem/DesignTokensExt.fs` — `module internal DesignTokensExt`, no
  `.fsi`, `// GENERATED — do not edit` header, values traceable to source keys, deterministic ordering.
- **Existing public module**: `DesignTokens` (flat primitives) — **unchanged**.

Color values are F# `Color` (via `FS.GG.UI.Scene.Colors.rgba`); dimensions/numbers are `float`. The
generated module groups tokens by layer; mode-specific layers expose a `Light` and a `Dark` sub-group.

## Group 1 — `seed` (brand & scale inputs; mode-independent)

Stable inputs that everything else references. Ant defaults are references, not mandates.

| Token | Type | Value | Notes |
|---|---|---|---|
| `colorPrimary` | color | `#1677ff` | Ant brand blue (reference adopted for the seed). |
| `colorSuccess` | color | `#15803d` | Aligns to existing `light.success`. |
| `colorWarning` | color | `#b45309` | Aligns to existing `light.warning`. |
| `colorError` | color | `#b91c1c` | Aligns to existing `light.danger` (Ant's "error" role). |
| `colorInfo` | color | `#1677ff` | New functional family (info). |
| `colorTextBase` | color | `#1f2937` | Matches existing `light.foreground`. |
| `colorBgBase` | color | `#f8fafc` | Matches existing `light.background`. |
| `fontSize` | dimension | `14.0` | Matches existing `fontSize`. |
| `lineHeight` | number | `1.5` | New. |
| `borderRadius` | dimension | `4.0` | Matches existing `cornerRadius`. |
| `controlHeight` | dimension | `32.0` | Ant middle control height. |
| `sizeUnit` | dimension | `4.0` | 8-unit grid base step. |
| `sizeStep` | number | `4.0` | Grid step multiplier base. |
| `motionUnit` | number | `0.1` | Base motion duration unit (seconds). |

## Group 2 — `map.light` / `map.dark` (derived per mode; explicit entries in F1)

Per-mode values. **Every member MUST exist for both Light and Dark** (FR-007).

| Token | light | dark | Notes |
|---|---|---|---|
| `colorPrimaryHover` | `#4096ff` | `#3c89e8` | Primary interaction states. |
| `colorPrimaryActive` | `#0958d9` | `#1668dc` | |
| `colorPrimaryBg` | `#e6f4ff` | `#111a2c` | Subtle primary surface. |
| `colorErrorBg` | `#fff2f0` | `#2c1618` | |
| `colorBorder` | `#d9d9d9` | `#424242` | |
| `colorFillSecondary` | `#f5f5f5` | `#1f1f1f` | |
| `colorBgContainer` | `#ffffff` | `#1f1f1f` | |
| `colorBgElevated` | `#ffffff` | `#2a2a2a` | Dropdowns/cards. |
| `colorBgLayout` | `#f5f5f5` | `#000000` | Page canvas. |
| `colorText` | `#1f2937` | `#f1f5f9` | Matches existing foreground per mode. |
| `colorTextSecondary` | `#64748b` | `#94a3b8` | Matches existing muted per mode. |
| `colorTextDisabled` | `#bfbfbf` | `#5a5a5a` | |

## Group 3 — `alias.light` / `alias.dark` (friendly render-facing names; both modes)

Names render code (and the future resolver) consume; they reference map/seed conceptually but are
emitted as resolved values in F1.

| Token | Maps to (concept) |
|---|---|
| `text.default` | `colorText` |
| `text.secondary` | `colorTextSecondary` |
| `surface.canvas` | `colorBgLayout` |
| `surface.container` | `colorBgContainer` |
| `surface.elevated` | `colorBgElevated` |
| `border.default` | `colorBorder` |
| `item.hoverBg` | `colorFillSecondary` |
| `item.selectedBg` | `colorPrimaryBg` |
| `focus.ring` | `colorPrimary` |
| `feedback.errorText` | `colorError` |
| `feedback.warningText` | `colorWarning` |

## Group 4 — `component.<family>` (per-control-family; modes where applicable)

Seed the families the Ant analysis names; others fall back to alias/role tokens as today.

| Family | Tokens |
|---|---|
| `button` | `primaryBg`, `primaryHoverBg`, `defaultBorder`, `dangerBg` |
| `input` | `activeBorder`, `hoverBorder`, `placeholderText` |
| `table` | `headerBg`, `rowHoverBg`, `borderColor` |
| `tabs` | `itemSelectedColor`, `inkBar`, `itemColor` |
| `menu` | `itemSelectedBg`, `itemSelectedColor`, `itemHoverBg` |

## Group 5 — supplementary semantic groups

### `space` (mode-independent)
`xs = 4`, `sm = 8`, `md = 16`, `lg = 24`, `xl = 32` (the 4/8/16/24/32 scale, FR-002).

### `density` (named multipliers; mode-independent)
`Comfortable = 1.0` (**equals today's default — no behaviour change**), `Middle = 0.875`,
`Compact = 0.75`. Unconsumed in F1; `Theme.withDensity` unchanged (R7).

### `type` (type scale; mode-independent)
| Name | fontSize | lineHeight |
|---|---|---|
| `display` | `30.0` | `1.3` |
| `section` | `20.0` | `1.4` |
| `title` | `16.0` | `1.5` |
| `body` | `14.0` | `1.5` |
| `small` | `12.0` | `1.5` |

### `elevation` (shadow tiers; mode-independent)
`none`, `low` (inputs), `medium` (hover/cards/dropdowns), `high` (dialogs) — each a string shadow
descriptor (e.g. an offset/blur/color tuple rendered later); unconsumed in F1.

## Invariants (validation rules)

- **V1 — additive**: every existing primitive (`light`/`dark` × `foreground…contrastRequiredRatio`)
  keeps its name and byte-identical value; nothing is renamed or revalued (FR-004).
- **V2 — mode parity**: each `map.*` and `alias.*` member exists for both Light and Dark (FR-007).
- **V3 — internal only**: every token above is emitted into `module internal DesignTokensExt`; **none**
  appears in any `.fsi` or per-package surface baseline (FR-005).
- **V4 — generated**: every value traces to a DTCG source entry; the file is marked generated; the
  generator is idempotent (FR-003/FR-010).
- **V5 — unconsumed**: no render path reads any token in this group set during F1 (FR-008).
- **V6 — Ant-as-reference**: where an Ant default differs from an existing chosen primitive, the
  existing value wins; Ant informs new groups only (FR-011).

## Generated-module surface (internal shape sketch)

Illustrative shape the layer-coverage test names (not a public contract):

```text
module internal DesignTokensExt
  module Seed        // colorPrimary, colorSuccess, …, motionUnit
  module Map
    module Light     // colorPrimaryHover, …, colorTextDisabled
    module Dark      // colorPrimaryHover, …, colorTextDisabled
  module Alias
    module Light     // text.default → textDefault, …
    module Dark
  module Component
    module Button    // primaryBg, …
    module Input | Table | Tabs | Menu
  module Space       // xs … xl
  module Density     // comfortable, middle, compact
  module Type        // display … small (fontSize + lineHeight)
  module Elevation   // none … high
```

Dotted DTCG names (`text.default`, `item.hoverBg`) are emitted as F# identifiers (`textDefault`,
`itemHoverBg`); the mapping rule is part of the generation contract.
