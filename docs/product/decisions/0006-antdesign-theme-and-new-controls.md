# 0006. Ant Design theme package and net-new generic controls (D2.1)

**Status**: accepted
**Date**: 2026-06-16

## Decision

Ship the flagship **`FS.GG.UI.Themes.AntDesign`** theme package and widen the standard control set so
the framework covers Ant Design's component overview maximally, in one feature. This is a **Tier 1**
change: a new public package plus new public controls in `FS.GG.UI.Controls`, with `.fsi` files,
per-package surface baselines, and this decision record landed in lock-step.

Four sub-decisions:

1. **A theme package, not a control fork.** `Themes.AntDesign` contains exactly two public modules —
   `AntTheme` (the concrete `Theme` values `antLight`/`antDark` + `resolve`) and `AntIntentPolicy`
   (a `StyleResolver.IntentPolicy` mapping `primary`/`default`/`dashed`/`text`/`link`/`danger` to
   distinct resolved styles). It depends **only** on `FS.GG.UI.DesignSystem` — never on `Controls`.
   Ant's appearance is realized entirely through the shared resolver/token seams; no control reads
   theme identity (the parity test `Feature132ThemeParityTests` enforces this).

2. **Net-new controls are generic and theme-agnostic.** The Ant overview has many components with no
   repo analog (Tag, Alert, Card, Steps, Collapse, …). These are added to `FS.GG.UI.Controls` as
   generic primitives (kind-string controls authored exactly like `Badge`), grouped into five
   cohesive modules: `Display2`, `Feedback2`, `Navigation2`, `Interactive2`, `DataEntry2`. They are
   styled by **both** themes and theme-aware in neither. Their state is parent-owned via attributes +
   events — no internal mutable state (Constitution IV). The genuinely heavyweight Ant data-entry
   components (Transfer, Mentions) are dispositioned `composition` in the coverage matrix rather than
   given the `DataGrid` `Model`/`Msg`/`Effect` machinery, because MVU is too heavy for their value.

3. **"Maximal" is kept honest by a machine-checked coverage matrix.** Every Ant overview component
   (pinned 70-entry snapshot, retrieved 2026-06-16) gets exactly one row in
   `docs/product/ant-design/coverage/ant-component-coverage.md` with a disposition of `existing` /
   `net-new` / `composition` / `not-applicable`. The honesty check
   `Feature132CoverageMatrixTests` fails on any missing row, dangling control/token reference, blank
   disposition, or missing rationale. Disposition totals: **31 existing, 30 net-new, 6 composition,
   3 not-applicable**.

4. **No token-value change; opt-in; behaviour-neutral.** AntDesign is selected by a consumer; the
   Default theme's resolved-style/contract output stays byte-identical (the `StyleResolver`
   neutral path is untouched). Every Ant `Theme` field and intent colour derives from a generated
   `DesignTokensExt`/`DesignTokens` entry — no inline literals — so the design-token-drift gate
   stays green (the new package adds entries to no token store; it composes existing ones).

## Public surface delta

**New package `FS.GG.UI.Themes.AntDesign`:**

- `AntTheme` — `antLight: Theme`, `antDark: Theme`, `resolve: Theme option -> Theme`
- `AntIntentPolicy` — `policy: StyleResolver.IntentPolicy`

**New public controls in `FS.GG.UI.Controls` (30 net-new ids):**

- `Display2` — `tag`, `avatar`, `card`, `descriptions`, `statistic`, `timeline`, `empty`,
  `skeleton`, `qr-code`, `watermark`
- `Feedback2` — `alert`, `result`, `drawer`, `popover`, `popconfirm`, `tour`, `float-button`
- `Navigation2` — `breadcrumb`, `steps`, `pagination`, `segmented`, `anchor`, `affix`
- `Interactive2` — `collapse`, `rate`, `carousel`, `calendar`
- `DataEntry2` — `cascader`, `auto-complete`, `upload`

Each net-new control has a catalog row (`catalog.yml` + the GENERATED rows in `Catalog.fs`), a
curated `.fsi`, and is rendered through the kind-string dispatch in `Control.fs`'s `faithfulContent`.
Both per-package surface baselines (`FS.GG.UI.Themes.AntDesign.txt` new,
`FS.GG.UI.Controls.txt` grown) are regenerated and committed in this change.

## Ant snapshot

The Ant component overview and tokens trace to the central reference hub
[`../ant-design/reference/ant-llms-sources.md`](../ant-design/reference/ant-llms-sources.md), whose
**snapshot retrieval date `2026-06-16`** is the single owner of provenance. Ant publishes no upstream
version label in its LLM docs, so none is restated here.

## Rationale

A theme + new-generic-controls split (rather than a theme-only or composition-only coverage, both
offered and declined) is the only route to maximal Ant-overview coverage without per-theme control
forks. The coverage matrix + honesty check make the breadth auditable; the parity test proves the
layering invariant ("one control set, many themes; no control branches on theme identity"). Charts
are deferred to the already-recorded plan follow-up (Phase D2-Charts / task D2C.1) — no chart code
ships here.

## Consequences

- Consumers can opt into a visibly Ant-styled UI by selecting `AntTheme.antLight`/`antDark` and the
  `AntIntentPolicy`, with no control changes.
- The standard control set grows from 52 to 82 supported controls.
- Future concrete themes (Fluent, Material) follow this package shape; future controls that need
  workflow state follow the `DataGrid` MVU pattern or are dispositioned `composition`.
