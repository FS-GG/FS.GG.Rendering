# Phase 1 Data Model — F5

F5 promotes *visibility*, so the "entities" are the promoted surface elements and the gate-delta artifacts, not new
runtime data. No new types are introduced; no existing type changes shape.

## Promoted entity 1 — `StyleResolver` (becomes public)

Module `FS.GG.UI.DesignSystem.StyleResolver`. Body unchanged from F4; only its visibility changes.

| Member | Signature (as it will appear in `StyleResolver.fsi`) | Notes |
|--------|------------------------------------------------------|-------|
| `IntentPolicy` | record `{ ApplyIntent: Theme -> string -> ResolvedStyle -> ResolvedStyle }` | The overridable seam. Function-valued field ⇒ no structural equality (tests assert by `.Name`/reference, not `=`). New public **type** row in the baseline. |
| `neutralPolicy` | `IntentPolicy` | The default, intent-agnostic policy (identity over the base). |
| `baseStyleFor` | `Theme -> string -> ResolvedStyle` | Total over `kind`: `"icon-button"` → accent outline; any other → filled accent fallback. Never throws/empty. |
| `resolve` | `IntentPolicy -> Theme -> string -> string -> StyleClass list -> VisualState -> ResolvedStyle` | Front-half path; composes `Style.resolve` (093 back-half) verbatim. |
| `resolveDefault` | `Theme -> string -> string -> StyleClass list -> VisualState -> ResolvedStyle` | `resolve neutralPolicy`; what `buttonGeom` calls. Byte-identical to today. |

**Validation rules**: totality (every `kind`/`intent`/`state` resolves, no exception) and determinism are already
proven by Feature129; F5 must not regress them. Under `neutralPolicy`, output is byte-identical to the pre-promotion
internal call.

## Promoted entity 2 — `DesignTokensExt` taxonomy (becomes public)

Module `FS.GG.UI.DesignSystem.DesignTokensExt`, generated. Nested-module shape (each becomes a public type row):

```
DesignTokensExt
├── Seed                       (colorPrimary/Success/Warning/Error/Info, colorTextBase/BgBase,
│                               fontSize, lineHeight, borderRadius, controlHeight, sizeUnit, sizeStep, motionUnit)
├── Map.{Light,Dark}           (colorPrimaryHover/Active/Bg, colorErrorBg, colorBorder, colorFillSecondary,
│                               colorBgContainer/Elevated/Layout, colorText/Secondary/Disabled)
├── Alias.{Light,Dark}         (textDefault/Secondary, surfaceCanvas/Container/Elevated, borderDefault,
│                               itemHoverBg/SelectedBg, focusRing, feedbackErrorText/WarningText)
├── Component.{Button,Input,Table,Tabs,Menu,...}  (per-component color tokens)
├── Space                      (xs/sm/md/lg/xl : float)
├── Density                    (comfortable/middle/compact : float)
├── Type.{Display,Section,Title,Body,Small}  (fontSize/lineHeight : float)
└── Elevation                  (none/low/medium/high : string)
```

**Validation rules**: every promoted token **value** is byte-identical to its pre-promotion value (regenerated from
the same `design-tokens.tokens.json`); the `Theme` record shape is unchanged; the generated `.fs` and the generated
`.fsi` agree (generator `--check` green).

## Deferred entity — `ColorPolicy` (stays internal)

`FS.GG.UI.Color.ColorPolicy` is **not** promoted (R5). No data-model change. The decision record names it as
deliberately deferred with rationale.

## Gate-delta artifacts

| Artifact | Change | Invariant |
|----------|--------|-----------|
| `tests/surface-baselines/FS.GG.UI.DesignSystem.txt` | Gains rows: `StyleResolver`, `StyleResolver+IntentPolicy`, `DesignTokensExt` and each nested sub-module type. | Every **added** row ⇔ a symbol named in the decision record; **no removed** rows; **no other** baseline file changes (SC-002, SC-007). |
| `src/DesignSystem/DesignTokensExt.fs` | `internal` removed; otherwise regenerated identical values. | Token-drift `--check` green; values byte-identical (SC-003). |
| `src/DesignSystem/DesignTokensExt.fsi` | NEW (generated). | Byte-locked to the `.fs` via `--check`. |
| `src/DesignSystem/StyleResolver.fsi` | NEW (hand-curated). | Declares exactly the five members above; nothing more. |
| `docs/product/decisions/0004-public-token-resolver-surface.md` | NEW. | Enumerates promoted + deferred surface, rationale, stability/reversibility (SC-004). |

## Relationships / dependency direction (unchanged)

`Scene ← DesignSystem ← Controls`. Promotion adds **no** dependency and reverses **no** arrow. `buttonGeom`
(Controls) keeps calling `StyleResolver.resolveDefault` — now a public binding in the package Controls already
references. The removed `InternalsVisibleTo` grants do not affect dependency direction (they were one-way test/
consumer visibility, not references).
