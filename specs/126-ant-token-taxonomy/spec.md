# Feature Specification: Ant-derived design-token taxonomy (Workstream F, Phase F1)

**Feature Branch**: `126-ant-token-taxonomy`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item for FS.GG" → resolved to Workstream **F1**: expand the design-token source and the generated token model from today's flat ~13 primitives into the Ant-derived layered taxonomy (seed → map → alias → component) plus semantic spacing, named density, a type scale, and elevation — **generated**, and **internal/additive-first** so no public contract or rendered output changes yet.

## Overview & Context

Today the framework's design tokens are a flat set of ~13 primitives (`foreground`, `background`, `accent`, `danger`, `success`, `warning`, `muted`, `fontFamily`, `fontSize`, `density`, `cornerRadius`, `contrastRequiredRatio`) for each of Light and Dark, generated from a single DTCG source of truth and consumed directly by the `Theme` values. That vocabulary is too thin to express a real, governed design language: controls read generic roles (`theme.Accent`/`theme.Danger`) directly, there is no layering between brand inputs and the friendly names render code wants, and there is nowhere to hang per-control or per-state values.

This feature adopts **Ant Design as a design language / token taxonomy** (not a React or DOM dependency) by enriching the token model into Ant's four-layer structure — **seed → map → alias → component** — and adding the supplementary semantic groups every theme needs (spacing, density, type scale, elevation). It is the **first reusable pillar** that later phases build on: the policy-driven color validator (F2/F3), the central visual-state style resolver (F4), and concrete themes/kits (Workstream D2/D3) all consume this vocabulary. Ant's published defaults (brand blue `#1677ff`, functional success/warning/error/info families, an 8-unit grid, `controlHeight 32`) are adopted as **references that inform the taxonomy's structure**, not as automatic replacements of the project's existing chosen values.

Crucially, F1 is the **enrichment-only** slice: the expanded taxonomy lands as an **internal, additive** surface that the framework's own future code (resolver, themes) and the test suites can name, with **zero public-surface-baseline change**, **zero change to any existing token value**, and **byte-identical rendered output**. Deliberate promotion of a chosen subset to public surface is explicitly deferred to a later phase (F5). This keeps the two CI hazards green throughout: the public-surface-drift gate and the design-token-drift gate.

This phase builds directly on the Workstream D1 layer split (feature 125): the **generated token module lives in the design-system layer** (`FS.GG.UI.DesignSystem`) and the **DTCG source travels with the default theme layer** (`FS.GG.UI.Themes.Default`).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A theme/resolver author has the full layered vocabulary to build against (Priority: P1)

A maintainer building the central style resolver (F4) or a concrete theme (D2) needs to express styling in terms of a layered token model — brand **seed** inputs, **map** values derived per mode, friendly **alias** names that render code consumes, and **component**-level tokens for specific control families — instead of a handful of flat roles. With this feature they can name every group, for both Light and Dark where applicable, all sourced from the single DTCG source.

**Why this priority**: This vocabulary is the substance that makes "real themes" and a real resolver possible. Without it, every later F/D phase is blocked. It is the MVP: even with nothing yet consuming the tokens, having the named, generated taxonomy is the deliverable.

**Independent Test**: From in-repo framework code (and the test suite via the same internal access the reconciler uses), reference a representative token from each layer — a seed (e.g. the brand primary), a light-mode and a dark-mode map value, a light/dark alias (e.g. a surface or text-secondary name), and a component token (e.g. a button primary background) — plus a spacing step, a named density, a type-scale entry, and an elevation level. All resolve to concrete values by typed name with no hardcoded literal at the use site.

**Acceptance Scenarios**:

1. **Given** the enriched token source, **When** a maintainer names a seed/map/alias/component token, **Then** it resolves to a concrete generated value for the requested mode (Light or Dark) without referencing an inline literal.
2. **Given** the supplementary groups, **When** a maintainer names a spacing step, a named density, a type-scale entry, or an elevation level, **Then** each resolves to its defined value (e.g. the 4/8/16/24/32 spacing scale; Comfortable/Middle/Compact density).
3. **Given** both modes, **When** a maintainer requests a map or alias token, **Then** a Light and a Dark variant both exist.

---

### User Story 2 - Nothing a consumer or user can observe changes (Priority: P1)

A consumer of any published `FS.GG.UI.*` package, and anyone running the product, must see no difference: the public API surface is unchanged, every existing token value is byte-identical, the public `Theme` record shape is unchanged, and rendered output is identical. The enrichment is invisible until a later phase deliberately consumes or promotes it.

**Why this priority**: Behaviour- and contract-neutrality is the hard gate (mirrors the D1 discipline). Reddening the surface-drift gate or the design-token-drift gate, changing a token value, or altering a pixel would turn a safe internal enrichment into a breaking change. It is co-critical with US1.

**Independent Test**: Regenerate the public-surface baselines and confirm **zero delta** for every package; diff the existing Light/Dark primitive values and confirm byte-identity; run the full existing suite plus the gallery render-identity tests and confirm the pass/skip counts and rendered output are unchanged.

**Acceptance Scenarios**:

1. **Given** the enriched taxonomy is internal/additive, **When** the per-package public-surface baselines are regenerated, **Then** there is no change to any committed baseline.
2. **Given** the existing primitives, **When** their values are compared before and after, **Then** all are byte-identical and the public `Theme` record shape is unchanged.
3. **Given** no render path reads the new tokens, **When** the gallery and reference scenes are rendered, **Then** output is byte-identical to before (render-identity evidence passes).

---

### User Story 3 - The taxonomy stays generated and drift-checked (Priority: P2)

The enriched tokens must remain **generated from the DTCG source**, never hand-edited, with the design-token-drift gate enforcing that the committed generated artifact matches the source. A maintainer changes tokens by editing the source and regenerating; the gate fails on any divergence.

**Why this priority**: The single-source-of-truth + drift-gate discipline is what keeps the (now much larger) token set trustworthy and reviewable. It is the guard that makes future growth safe, but it sits behind having the vocabulary at all (US1) and neutrality (US2).

**Independent Test**: Regenerate from the DTCG source and confirm the generated artifact has no diff (gate green). Introduce a deliberate source change and confirm the gate flags the stale artifact until regeneration; confirm the artifact carries a "generated — do not edit" marker.

**Acceptance Scenarios**:

1. **Given** a clean tree, **When** the token module is regenerated from the DTCG source, **Then** the result is identical to the committed artifact (no diff).
2. **Given** an edited DTCG source, **When** the drift gate runs before regeneration, **Then** it reports the artifact is stale.
3. **Given** the generated artifact, **When** it is inspected, **Then** every value traces to a DTCG source entry and the file is marked generated.

---

### Edge Cases

- **Existing primitive names**: the current flat roles (`foreground`/`background`/`accent`/`danger`/`success`/`warning`/`muted`/…) must continue to exist with identical values; the new layers coexist with — never rename or replace — them.
- **Ant defaults as references, not mandates**: where Ant's published value differs from the project's existing chosen value, the existing value is preserved; Ant's value informs structure/new groups only. No existing token silently changes to an Ant number.
- **Light/Dark parity**: a map or alias token defined for one mode must be defined for both, so a theme can switch modes without missing tokens.
- **Map derivation**: map values start as explicit per-mode entries in the source (not yet algorithmically derived from seed); the structure must allow algorithms to replace explicit entries later without a vocabulary change.
- **Unconsumed tokens**: every new token is unconsumed in this phase; none may be wired into a render path here (that is F4). An accidental read that changed output would fail the render-identity check.
- **Component-token coverage**: component tokens are provided for the control families the catalog already ships and the analysis names (e.g. button, input, table, tabs, menu); a control family without a component token simply falls back to alias/role tokens as today.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The DTCG design-token source MUST be expanded to express the layered taxonomy — **seed**, **map** (Light and Dark), **alias** (Light and Dark), and **component** (per named control family) — in addition to the existing primitives.
- **FR-002**: The source MUST also define the supplementary semantic groups: a **spacing** scale (xs/sm/md/lg/xl = 4/8/16/24/32), **named density** levels (Comfortable/Middle/Compact, preserving the existing density-scaling behaviour), a **type scale** (body/small/title/section/display with line-height), and **elevation** levels (none/low/medium/high).
- **FR-003**: The token model MUST be **generated** from the DTCG source — no hand-coded values — and regeneration MUST be idempotent (same source ⇒ identical artifact).
- **FR-004**: The expansion MUST be **additive**: every token that exists today MUST keep its name and a byte-identical value. No existing value may change.
- **FR-005**: The new taxonomy MUST be **internal/additive-first**: it MUST NOT change the committed public-surface baseline of any package, and MUST NOT change the public `Theme` record shape. Public promotion of any subset is out of scope for this phase.
- **FR-006**: The enriched tokens MUST be reachable by in-repo framework code (the future resolver/themes) and by the test suites, without being part of any package's public API.
- **FR-007**: Map and alias token groups MUST provide both a Light and a Dark variant for every token they define.
- **FR-008**: No render path may read any newly added token in this phase; rendered output MUST be **byte-identical** to before (behaviour-neutral).
- **FR-009**: The generated token model MUST reside in the design-system layer (`FS.GG.UI.DesignSystem`) and the DTCG source MUST reside in the default-theme layer (`FS.GG.UI.Themes.Default`), consistent with the feature-125 layer split.
- **FR-010**: The design-token-drift gate MUST pass — the committed generated artifact MUST match the DTCG source — and the generated artifact MUST be marked "generated — do not edit" with each value traceable to a source entry.
- **FR-011**: Ant's published defaults (brand primary, functional colour families, 8-unit grid, control height) MUST be treated as **references** informing the taxonomy's structure and any genuinely new groups, NOT as automatic replacements of existing chosen primitive values.

### Key Entities *(include if feature involves data)*

- **Seed tokens**: the stable brand/scale inputs (primary, functional success/warning/error/info, text-base, bg-base, base font size, line height, border radius, control height, size unit/step, motion unit).
- **Map tokens (Light/Dark)**: per-mode derived values (primary hover/active/bg, error bg, border, secondary fill, container/elevated/layout backgrounds, text/secondary/disabled) — explicit per-mode entries in this phase.
- **Alias tokens (Light/Dark)**: friendly, render-facing names (text default/secondary, surface canvas/container/elevated, border default, item hover/selected bg, focus ring, feedback error/warning text).
- **Component tokens**: per-control-family values (e.g. button primary bg, input active border, table header/row-hover bg, tabs selected colour, menu selected bg) for the families the catalog ships.
- **Spacing scale / Named density / Type scale / Elevation set**: the supplementary semantic groups (FR-002).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A maintainer can name a token from **every** layer (seed, map, alias, component) and **every** supplementary group (spacing, density, type, elevation), for both Light and Dark where the layer is mode-specific, with no inline literal at the use site.
- **SC-002**: **Zero** change to every committed public-surface baseline and **100%** of today's existing token values byte-identical; the public `Theme` record shape unchanged.
- **SC-003**: The full existing test suite passes with the **same** pass/skip counts as before this feature, and gallery/reference render output is **byte-identical** (behaviour-neutral).
- **SC-004**: Regenerating the token model from the DTCG source produces **no diff** (design-token-drift gate green); a deliberate source edit is reflected only by regeneration, never by hand.
- **SC-005**: The taxonomy covers all four Ant layers plus the four supplementary groups, with map/alias defined for both modes and component tokens present for at least the control families named in the analysis (button, input, table, tabs, menu).
- **SC-006**: `dotnet build` is green with **0 new warnings/errors**.

## Assumptions

- **Internal placement**: "internal/additive-first" means the enriched taxonomy is exposed as an assembly-internal generated surface (reachable by in-repo framework code and tests via the existing internals-visibility mechanism), not as new public `.fsi` surface. The exact internal shape is a planning decision; the spec only requires zero public-surface delta.
- **Explicit map values, not algorithms**: per the analysis, map/alias values land as explicit per-mode DTCG entries in this phase; algorithmic derivation from seed (e.g. tint/shade ladders) is a later enhancement and out of scope here.
- **Values are the project's choices**: existing Light/Dark primitive values are preserved exactly; new groups use deliberate values informed by — but not mechanically copied from — Ant's defaults. Where this spec needs a concrete number it states it (e.g. the 4/8/16/24/32 spacing scale).
- **Layer placement follows D1**: generated module in `FS.GG.UI.DesignSystem`, DTCG source in `FS.GG.UI.Themes.Default` (feature 125). Package names appear as identity/placement per established project convention, not as implementation prescriptions.
- **No consumer yet**: the central resolver (F4), the policy validator (F2/F3), concrete themes (D2), and public-surface promotion (F5) are explicitly **out of scope**; F1 delivers only the generated, internal, additive vocabulary.
- **Drift tooling reused**: the existing DTCG-source-to-module generation tooling and the design-token-drift gate are extended to cover the larger taxonomy rather than replaced.

## Out of Scope

- The `ColorPolicy` abstraction and `wcag`/`ant` policies (F2), and the `--design-system` template parameter (F3).
- The central `resolve` visual-state style resolver and any control migration to consume it (F4).
- Promoting any token to public surface, and any decision record for that promotion (F5).
- Concrete Ant/Fluent/Material themes and kits (Workstream D2/D3).
- Any change to the public `Theme` record shape beyond the already-landed `Success`/`Warning` roles.
