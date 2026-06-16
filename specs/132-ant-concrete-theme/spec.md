# Feature Specification: Concrete Ant Design theme with widened component coverage — Workstream D, Phase D2.1

**Feature Branch**: `132-ant-concrete-theme`

**Created**: 2026-06-16

**Status**: Draft

**Input**: User description: "next item in fs.gg concrete ant theme. lets widen the scope and try to add as many components from https://ant.design/components/overview/ as possible. also add adding the components from https://ant-design-charts.antgroup.com/en/components/overview as a follow up feature to the plan."

## Why this feature (context)

Workstream **F** (Ant Design adoption as a *design language*, not a React/DOM dependency) has fully landed its engineering pillars and its knowledge layer:

- **F1 (126)** — Ant-derived token taxonomy (seed → map → alias → component).
- **F2 (127)** — policy-driven color/contrast validation (`wcag` / `ant`).
- **F3 (128)** — `--design-system` template parameter selecting the policy.
- **F4 (129)** — central visual-state style resolver (`theme → kind → intent → states → style`) with an overridable intent-policy seam.
- **F5 (130)** — promotion of the consumer-facing token taxonomy + resolver surface to the public API.
- **F6 (131)** — per-control-family Ant interaction-pattern docs + the `fs-gg-ant-design` advisory skill.

Workstream **D1 (125)** split the monolith into `FS.GG.UI.DesignSystem`, `FS.GG.UI.Themes.Default`, and `FS.GG.UI.Controls`. Everything needed to build a *real* theme on the public surface now exists. What does **not** yet exist is a single concrete theme that visibly differs from the default — the payoff the whole F arc was building toward.

This feature is **Workstream D, Phase D2.1**: the flagship **`FS.GG.UI.Themes.AntDesign`** theme assembly. Per the user's two scope decisions (2026-06-16), it is deliberately **widened** beyond a minimal theme-over-existing-controls slice:

1. **Coverage scope = theme + net-new controls.** Beyond styling the 52 existing controls, this feature **adds net-new, generic, theme-agnostic controls** to the `FS.GG.UI.Controls` library to fill the gaps in [Ant Design's component overview](https://ant.design/components/overview/) (e.g. Avatar, Tag, Alert, Collapse, Segmented, Rate, Timeline, Steps, Breadcrumb, Pagination, Card, Result, Empty, Drawer, Skeleton, …). New controls follow the **"one semantic control set, many themes"** rule — they are *not* Ant forks; they are generic primitives the Ant theme (and later Fluent/Material) styles.
2. **MVP boundary = maximal in one feature.** This feature attempts to cover **every Ant component-overview entry** that the chosen scope allows, in a single feature, rather than a core-set-now / tail-later split. The vehicle that keeps this honest is a **coverage matrix** enumerating every Ant overview component and its disposition (existing repo control / net-new control / composition of controls / intentionally not-applicable).

Charts are explicitly **out of scope** here and become a **follow-up feature appended to the implementation plan** ([Ant Design Charts overview](https://ant-design-charts.antgroup.com/en/components/overview)) — see User Story 5.

**Change Classification**: **Tier 1 (public surface change)**. This feature adds a new public package (`FS.GG.UI.Themes.AntDesign`) and new public controls to `FS.GG.UI.Controls`. New per-package surface-area baselines and regenerated existing baselines are required in lock-step (the drift gate fails on untracked baselines). No existing token *value* changes and no existing rendered output changes — the default theme stays byte-identical (the Ant theme is opt-in).

## User Scenarios & Testing *(mandatory)*

> "Users" are the framework's own engineers and downstream app/theme authors — the audience the theme and the widened control set serve. The spec is written from their perspective.

### User Story 1 - An app author opts into a visibly Ant-styled UI without forking controls (Priority: P1)

An application author who has built a screen from the framework's semantic controls selects the **AntDesign theme** instead of the default. The *same* control tree now renders with Ant's visual language — brand blue primary, Ant's control heights on the 8-unit grid, Ant's radii/spacing/typography, Ant's intent treatment (primary / default / dashed / text / link, plus danger) — while behavior, layout semantics, and the accessibility contract are unchanged. The author writes **no Ant-specific control code**; only the theme selection changes.

**Why this priority**: This is the feature's reason to exist and the visible payoff of the entire F arc. A concrete theme that diverges from the default on real controls is the MVP; without it, F1–F6 are invisible machinery. It is independently valuable even if no net-new controls (US3) are ever added.

**Independent Test**: Take a control tree built only from controls that exist today, render it under the default theme and under the AntDesign theme, and confirm: (a) the AntDesign render differs visibly from the default (color/spacing/radius/intent), (b) the behavior and accessibility contract are identical across both, and (c) no control type in the tree is Ant-specific.

**Acceptance Scenarios**:

1. **Given** a control tree of existing controls, **When** rendered under the AntDesign theme, **Then** primary buttons use Ant's brand-blue treatment and control sizing follows Ant's grid (e.g. default control height), differing observably from the default theme.
2. **Given** the same tree, **When** rendered under default vs AntDesign, **Then** the resolved accessibility contract (roles, names, states, focus order) is identical and only the visual styling differs.
3. **Given** a button carrying a semantic intent (e.g. `Danger`), **When** rendered under the AntDesign theme, **Then** the intent is *observable* (e.g. danger red) — exercising the F4 intent-policy seam that the default theme leaves neutral.
4. **Given** the AntDesign theme is not selected, **When** any existing consumer renders, **Then** output is byte-identical to today (the theme is purely additive/opt-in).

---

### User Story 2 - Maximal, honest Ant component coverage via a coverage matrix (Priority: P1)

A developer (or reviewer) wants to know exactly **which Ant components the framework can express and how**. They open a **coverage matrix** that lists every component from Ant's component overview and, for each, states its disposition: styled via an **existing** repo control, served by a **net-new** generic control added in this feature, realized as a **composition** of repo controls, or **not applicable** (e.g. React-only infrastructure components with no rendered analog). Every "covered" row names the concrete repo control(s) and token-taxonomy entries involved.

**Why this priority**: "Add as many components as possible" is only meaningful if coverage is enumerable and verifiable. The matrix is the artifact that turns "maximal" from an aspiration into a checkable contract and prevents silent gaps. It is the honesty mechanism for the whole feature.

**Independent Test**: Open the coverage matrix; confirm it has exactly one row per Ant component-overview entry, that each row's disposition is one of the defined categories, and that an automated check fails if (a) any "covered" row names a control/token that does not exist in the current surface, or (b) any Ant overview component is missing a row.

**Acceptance Scenarios**:

1. **Given** Ant's component overview, **When** the matrix is built, **Then** every overview component appears exactly once with a disposition (existing / net-new / composition / not-applicable) and a one-line rationale.
2. **Given** a "covered" row, **When** the honesty check parses it, **Then** every named repo control id and token-taxonomy entry resolves against the current public surface (no dangling or invented references).
3. **Given** a "not-applicable" row, **When** reviewed, **Then** it carries an explicit reason (e.g. "React/DOM infrastructure with no rendered surface"), never a silent omission.
4. **Given** the maximal goal, **When** the matrix is summarized, **Then** the count of components left as bare "deferred" with no disposition is **zero** — every entry is dispositioned.

---

### User Story 3 - Net-new generic controls fill the Ant-overview gaps (Priority: P1)

A control author adds the **generic primitives** the Ant overview needs but the library lacks (candidates: Avatar, Tag, Alert, Collapse/Accordion, Segmented, Rate, Timeline, Steps, Breadcrumb, Pagination, Card, Descriptions, Statistic, Result, Empty, Drawer, Popover, Popconfirm, Skeleton, Affix, Watermark, Carousel, FloatButton, Cascader, AutoComplete, Mentions, Transfer, Upload, Calendar, QRCode, Tour, Anchor). Each is added as a **theme-agnostic** control with the same semantics, attributes, visual-state vocabulary, accessibility metadata, and catalog registration as existing controls — **not** an Ant fork. The AntDesign theme then styles them; the default theme also renders them (neutrally).

**Why this priority**: This is what "widen the scope … add as many components as possible" concretely requires under the chosen scope. Without net-new controls the maximal coverage goal is unreachable, since many high-value Ant components (Tag, Alert, Card, Steps, Collapse, …) have no repo analog today.

**Independent Test**: For each net-new control, confirm it is registered in the catalog with category, attributes, visual states, and accessibility metadata; renders under **both** the default and the AntDesign theme; and passes the same catalog/semantic/accessibility/rendering test families every existing control passes.

**Acceptance Scenarios**:

1. **Given** a net-new control, **When** it is added, **Then** it appears in the typed catalog with a category, required/common attributes, the standard visual-state set, and accessibility metadata — identical in shape to existing controls.
2. **Given** a net-new control, **When** rendered under the default theme, **Then** it produces a coherent neutral rendering (it is generic, not Ant-only).
3. **Given** a net-new control, **When** rendered under the AntDesign theme, **Then** it adopts Ant's visual language for that component family.
4. **Given** a net-new control, **When** the existing control test families run, **Then** it satisfies the same catalog/semantic/interaction/accessibility/rendering contracts as every other control (no second-class controls).
5. **Given** the layering rule, **When** a net-new control is reviewed, **Then** it contains no Ant-specific branching — all Ant appearance lives in the theme/resolver, not the control.

---

### User Story 4 - "One control set, many themes" parity is proven (Priority: P2)

A maintainer wants machine-checked proof that adding the AntDesign theme did not fork behavior. A **parity test** renders an identical control tree — including the net-new controls — under the default theme and the AntDesign theme and asserts: identical behavior and accessibility contract, **divergent** resolved visuals. This is the running-code proof of the layering rule the whole D workstream commits to.

**Why this priority**: The parity proof is the guardrail that keeps the widened scope honest about *not* forking. It is P2 because US1–US3 deliver the capability; this test certifies its central invariant.

**Independent Test**: Run the parity test over a representative tree spanning every control category (display, input, selection, layout, navigation, feedback, data, overlay, plus the net-new families) and confirm it asserts contract-identity and visual-divergence and passes.

**Acceptance Scenarios**:

1. **Given** one control tree, **When** resolved under default and AntDesign themes, **Then** the behavior/accessibility contract is asserted identical and the resolved visuals are asserted to differ.
2. **Given** the parity test, **When** a future change accidentally forks behavior between themes (e.g. a control branches on theme identity), **Then** the test fails.
3. **Given** the maximal coverage goal, **When** the parity test selects its sample tree, **Then** every control category — including each net-new family — is represented (coverage is not silently narrowed to easy controls).

---

### User Story 5 - Ant Design Charts is queued as a follow-up feature in the plan (Priority: P3)

A maintainer reviewing the roadmap finds that **Ant Design Charts** ([overview](https://ant-design-charts.antgroup.com/en/components/overview)) has been recorded as an explicit **follow-up feature** in the active implementation plan — distinct from this feature, scoped to extend the framework's existing chart controls (line/bar/pie/scatter) toward the Ant Charts catalog (statistical / relational / hierarchical / geo families) under the same design-language-not-dependency rule. This feature does **not** implement charts; it only ensures the follow-up is captured so it is not lost.

**Why this priority**: The user explicitly asked for the charts work to be added "as a follow up feature to the plan," not built now. Capturing it preserves roadmap continuity at near-zero cost and keeps this feature's scope bounded. P3 because it is a planning artifact, not runtime capability.

**Independent Test**: Open the active implementation plan; confirm a new, clearly-scoped follow-up entry for Ant Design Charts exists, references the charts overview source, states it is design-language adoption (no JS/React dependency), and is sequenced after this feature.

**Acceptance Scenarios**:

1. **Given** the active implementation plan, **When** it is read after this feature, **Then** it contains a dedicated Ant Design Charts follow-up entry citing the charts overview source and scoped as a successor to D2.1.
2. **Given** that entry, **When** reviewed, **Then** it is explicit that charts are adopted as a design language (catalog + token mapping) over the framework's chart controls, not as a JS/React charting dependency.
3. **Given** this feature's scope, **When** charts are considered, **Then** no chart implementation work is included here — only the plan entry.

---

### Edge Cases

- **Ant components with no rendered analog** (e.g. `App`, `ConfigProvider`, `Util`, and other React/DOM-infrastructure entries): dispositioned **not-applicable** in the matrix with a stated reason; the AntDesign theme exposes the equivalent capability (theme/policy selection) through the framework's existing theme-provision mechanism, not a control.
- **Components that are inherently compositions** (e.g. Card, Descriptions, Result, Breadcrumb, Steps over panel/stack/icon/text): dispositioned **composition**, with the constituent controls named; the matrix must not claim a net-new primitive where a composition suffices.
- **A net-new control whose Ant component spans multiple visual states** (e.g. Collapse expand/collapse, Steps current/finished/error): the standard visual-state vocabulary must express these, or the control documents which states it uses — no Ant-only state enum leaks into the generic control.
- **Default-theme rendering of a net-new control**: must be coherent and neutral; a net-new control may not be "blank" or broken under the default theme just because it was motivated by Ant.
- **Surface-drift gate**: adding a package and new controls reddens the gate until every new/changed baseline is regenerated and committed in the same change.
- **Intent under the default theme**: the AntDesign theme turning on intent divergence (e.g. danger red) must not retroactively change default-theme output, which stays intent-neutral per F4.
- **Ant version drift**: the matrix and theme target a pinned Ant overview snapshot; the source/date is recorded so future drift is detectable rather than silent.

## Requirements *(mandatory)*

### Functional Requirements

#### The AntDesign theme assembly

- **FR-001**: The system MUST provide a new public theme package, `FS.GG.UI.Themes.AntDesign`, that supplies a concrete `Theme` instance (color, typography, spacing, radius, elevation) built on the public `FS.GG.UI.DesignSystem` token taxonomy and consumed through the public central style resolver — depending only on `DesignSystem` (no dependency on `Controls`).
- **FR-002**: The AntDesign theme MUST express Ant's reference design values — brand-blue primary, the functional success/warning/error/info families, the 8-unit spacing grid, and Ant's default control sizing — sourced from the token taxonomy, with no hardcoded literals at control use sites.
- **FR-003**: The AntDesign theme MUST supply an **intent policy** (via the F4 seam) that makes button/control intents observably distinct (primary / default / dashed / text / link and danger), proving intent divergence is reachable **without forking any control**.
- **FR-004**: Selecting the AntDesign theme MUST be **opt-in**; with no theme change, every existing consumer's rendered output stays **byte-identical** to today (the default theme is unaffected).
- **FR-005**: The AntDesign theme MUST style controls **only** through the shared resolver/token seams — it MUST NOT require or contain control forks or Ant-specific control subclasses.

#### Net-new generic controls (gap-fill for maximal coverage)

- **FR-006**: The system MUST add net-new **generic, theme-agnostic** controls to `FS.GG.UI.Controls` for the Ant overview components that have no existing repo analog and are not better served as compositions (candidate set per US3), each registered in the typed catalog with category, attributes, the standard visual-state vocabulary, and accessibility metadata in the same shape as existing controls.
- **FR-007**: Each net-new control MUST render coherently under **both** the default theme (neutral) and the AntDesign theme (Ant-styled), with **no Ant-specific branching inside the control** (all appearance differences live in the theme/resolver).
- **FR-008**: Each net-new control MUST satisfy the same catalog / semantic / interaction / accessibility / rendering test families as existing controls (no second-class controls).
- **FR-009**: Where an Ant overview component is adequately expressed by **composing** existing controls, the system MUST realize it as a documented composition rather than a redundant net-new primitive.

#### Coverage matrix and honesty

- **FR-010**: The system MUST produce a **coverage matrix** with exactly one row per Ant component-overview entry, each carrying a disposition (existing control / net-new control / composition / not-applicable) and a one-line rationale; "covered" rows MUST name the concrete repo control id(s) and at least one token-taxonomy entry.
- **FR-011**: An automated **honesty/coverage check** MUST fail if (a) any Ant overview component lacks a matrix row, (b) any "covered" row names a control id or token entry absent from the current public surface, or (c) any row lacks a disposition.
- **FR-012**: The matrix MUST record the Ant component-overview **source and snapshot date** so future drift against upstream is detectable.

#### Parity and layering proof

- **FR-013**: The system MUST provide a **parity test** that renders one identical control tree (spanning every control category, including each net-new family) under the default and AntDesign themes and asserts: identical behavior/accessibility contract, divergent resolved visuals.
- **FR-014**: The parity test MUST fail if any control branches on theme identity (i.e. if behavior — not just visuals — forks between themes).

#### Surface, CI, and provenance

- **FR-015**: The system MUST add a per-package public-surface baseline for `FS.GG.UI.Themes.AntDesign`, regenerate the `FS.GG.UI.Controls` baseline to include net-new controls, and commit all new/changed baselines in the **same change** so the surface-drift gate stays green.
- **FR-016**: The system MUST register the new theme package in the solution and the surface-baseline tooling, and update the module/layer map so the AntDesign theme moves from "planned" to "owned assembly."
- **FR-017**: The system MUST record a decision document covering the new public package, the net-new public controls, and the chosen Ant overview snapshot.
- **FR-018**: The design-token-drift gate MUST stay green — no existing token **value** changes. The AntDesign theme composes existing taxonomy entries and is expected to add **zero** new token entries; should a new entry prove unavoidable it MUST be purely additive and dispositioned in the decision record (FR-017) and coverage matrix — never a value change to an existing entry.

#### Charts follow-up (planning only)

- **FR-019**: The system MUST ensure a dedicated **Ant Design Charts** follow-up entry exists in the active implementation plan (appending it if absent — it is already recorded as Phase D2-Charts / task D2C.1), citing the charts overview source, scoping it as design-language adoption (catalog + token mapping over the framework's chart controls; no JS/React charting dependency), and sequenced after this feature.
- **FR-020**: This feature MUST NOT include any chart implementation work beyond the plan entry in FR-019.

### Key Entities

- **AntDesign theme**: a concrete `Theme` instance + intent policy in the new `FS.GG.UI.Themes.AntDesign` package, built on the public token taxonomy and resolver; opt-in; depends only on `DesignSystem`.
- **Net-new control**: a generic, theme-agnostic control added to `FS.GG.UI.Controls` to fill an Ant-overview gap; carries catalog registration, attributes, visual states, accessibility metadata; styled by themes, never theme-aware itself.
- **Coverage matrix**: the enumerated mapping of every Ant component-overview entry → disposition (existing / net-new / composition / not-applicable) + named controls/tokens + rationale; the honesty artifact and drift anchor.
- **Parity test**: the running-code proof that one control tree renders behavior-identically and visually-divergently across the default and AntDesign themes.
- **Charts follow-up entry**: the roadmap record (in the active plan) that queues Ant Design Charts adoption as a successor feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An identical control tree rendered under the default and AntDesign themes shows an **observable visual difference** (color, spacing, radius, and intent treatment) while presenting an **identical** accessibility contract — verified by the parity test.
- **SC-002**: **100%** of Ant component-overview entries appear in the coverage matrix with a disposition; the count of un-dispositioned/"bare-deferred" entries is **zero**.
- **SC-003**: **100%** of "covered" matrix rows reference only controls and token entries that exist in the current public surface (honesty check passes with zero dangling references).
- **SC-004**: Every net-new control passes the **same** catalog/semantic/interaction/accessibility/rendering test families as pre-existing controls (no test family skipped for new controls).
- **SC-005**: With the AntDesign theme **not** selected, the Default theme's **resolved-style/contract output is byte-identical** to the pre-feature baseline (opt-in guarantee), verified by the deterministic resolved-style baseline being unchanged. Any pixel-level golden-image comparison is advisory and GL-gated (honest-skipped where no GL context is available) — per research Decision 6, golden diffing is **not** the gate.
- **SC-006**: The surface-drift gate and the design-token-drift gate are **green** after the change, with committed baselines for the new package and the updated `Controls` package, and **zero** existing token-value changes.
- **SC-007**: A button's `Danger` (and the primary/default/dashed/text/link) intent is **visibly distinct** under the AntDesign theme, while remaining neutral under the default theme — demonstrating intent divergence with **zero** control forks.
- **SC-008**: The active implementation plan contains a dedicated, correctly-scoped Ant Design Charts follow-up entry sequenced after this feature, and this feature ships **no** chart implementation code.

## Assumptions

- "Concrete ant theme / next item" resolves to **Workstream D, Phase D2.1** (the flagship `FS.GG.UI.Themes.AntDesign` assembly), since F1–F6 and D1 have all landed and D2.1 is the next sequenced item that pays them off visibly.
- **Scope = theme + net-new controls** and **MVP = maximal in one feature**, per the user's explicit decisions on 2026-06-16. The coverage matrix is the mechanism that keeps "maximal" honest and bounded.
- Net-new controls are **generic library controls** (the "one semantic control set, many themes" rule), not Ant-specific types; the Ant appearance lives entirely in the theme/resolver/tokens.
- The AntDesign theme is **opt-in** and **does not** alter default-theme output; the default theme remains intent-neutral (per F4's behaviour-neutral seam).
- Ant is adopted as a **design language only** — no React, DOM, HTML, CSS, or JS dependency; Ant facts are drawn from the repo's Ant reference hub and pattern docs, not raw `ant.design` URLs at build time. The pinned Ant overview snapshot/date is recorded for drift detection.
- **Fluent and Material themes (D2.2 / D2.3)** are **out of scope** here; this feature delivers the Ant flagship and proves the machinery they will reuse.
- **Kits (D3)** — `AntDesign.Form`, `AntDesign.Table`, and the enterprise page templates — are **out of scope**; this feature delivers the theme + controls those kits will later compose.
- **Charts** are **out of scope** as implementation; only the plan follow-up entry (FR-019) is delivered here.
- Adding a public package and public controls is a **Tier 1** change requiring decision-record + baseline regeneration; the namespace/layering follows the established `FS.GG.UI.*` scheme from D1.

## Dependencies

- **F1–F6 (126–131)** — token taxonomy, color policy, template parameter, central resolver, public-surface promotion, and Ant pattern docs/skill. All required and all landed.
- **D1 (125)** — the `DesignSystem` / `Themes.Default` / `Controls` layer split that this theme builds on. Landed.
- The repo's **Ant reference hub** (`docs/product/ant-design/reference/ant-llms-sources.md`) and **per-family pattern docs** (`docs/product/ant-design/patterns/`) as the source of Ant facts; the `fs-gg-ant-design` skill for applying them.
- The **surface-drift** and **design-token-drift** CI gates and the surface-baseline tooling (`scripts/refresh-surface-baselines.fsx`).
