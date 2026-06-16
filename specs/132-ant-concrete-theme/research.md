# Phase 0 Research: Concrete Ant Design theme (D2.1)

All Technical-Context unknowns are resolved below. No `NEEDS CLARIFICATION` remains (the two scope-defining decisions were settled with the user on 2026-06-16: coverage scope = theme + net-new controls; MVP = maximal in one feature).

## Decision 1 — How the AntDesign theme is constructed

**Decision**: The `Themes.AntDesign` package mirrors `Themes.Default`: it produces concrete `FS.GG.UI.DesignSystem.Theme` value(s) (`antLight`, `antDark`) whose flat fields (`Foreground`/`Background`/`Accent`/`Danger`/`Success`/`Warning`/`Muted`/`FontFamily`/`FontSize`/`Density`/`CornerRadius`/`ContrastRequiredRatio`) are populated from the **Ant-derived entries already present in the layered `DesignTokensExt`** (seed→map→alias→component, Space/Type/Density/Elevation) generated in `DesignSystem`. No literals at use sites; no new token *values*.

**Rationale**: F1/126 generated the Ant-derived taxonomy precisely so a theme could compose it; F5/130 promoted it public. The flat `Theme` record is the contract every control already reads, so populating it from Ant token entries yields Ant visuals with zero control edits. Keeps the design-token-drift gate green (no value changes).

**Alternatives considered**: (a) Expand the `Theme` record shape to carry the full taxonomy — rejected: the plan and F5 explicitly defer broad `Theme`-shape change (consumer break); the resolver + flat record already suffice. (b) Hardcode Ant hex values in the theme — rejected: violates "no literals at use sites" and bypasses the generated taxonomy/drift gate.

## Decision 2 — How Ant intent divergence is delivered without forking controls

**Decision**: Supply an `AntIntentPolicy : StyleResolver.IntentPolicy` whose `ApplyIntent: Theme -> string -> ResolvedStyle -> ResolvedStyle` maps Ant's button/control intents — `primary` (brand-blue fill), `default` (outlined), `dashed`, `text`, `link`, and `danger` (→ `theme.Danger`) — onto the structural base from `StyleResolver.baseStyleFor`. The Default theme keeps `StyleResolver.neutralPolicy` (identity), staying intent-neutral and byte-identical.

**Rationale**: F4/129 built the `IntentPolicy` seam specifically to make intent divergence reachable "without forking any control," proven only from a test until F5 made it public. D2.1 is where a real policy ships. This is the mechanism behind FR-003/SC-007.

**Alternatives considered**: Per-theme control subclasses (`AntButton`) — rejected hard by the constitution layer rule and FR-005/FR-014.

## Decision 3 — Where net-new controls live and what shape they take

**Decision**: Net-new controls live **in `FS.GG.UI.Controls`** (generic, theme-agnostic), grouped into a few cohesive modules (compile-order-friendly) rather than one file per control. Default shape = **pure render + attributes + events**, exactly like existing `Badge`/`CheckBox`/`Slider` where the parent (app Elmish model) owns state. Only genuinely workflow-bearing components adopt the existing `DataGrid`/`Collections` `Model`/`Msg`/`Effect` pattern.

**Rationale**: The layer rule forbids theme-specific controls; the bulk of Ant's overview is presentational and fits the stateless render+attr pattern already pervasive in the catalog. Reusing `DataGrid`'s MVU pattern for the few stateful ones satisfies Principle IV without inventing new machinery.

**Alternatives considered**: Putting new controls in the theme package — rejected: would make them theme-coupled and unreachable by Fluent/Material later. One file per control — rejected: ~30 files balloon the fsproj and compile graph; cohesive grouping is simpler (Principle III).

## Decision 4 — Disposition of every Ant overview component (scope of "maximal")

**Decision**: Classify each Ant overview component into one of four dispositions, recorded in the coverage matrix:

- **existing** — already a repo control (e.g. Button→`button`, Input→`text-box`, Select→`combo-box`, Table→`data-grid`, Tabs→`tabs`, Tooltip→`tooltip`, Modal→`dialog`, Slider→`slider`, Switch→`switch`, Checkbox→`check-box`, Radio→`radio-group`, Tree→`tree-view`, List→`list-view`, Progress→`progress-bar`, Spin→`spinner`, DatePicker→`date-picker`, TimePicker→`time-picker`, ColorPicker→`color-picker`, Menu→`menu`, Dropdown→`context-menu`, Divider→`separator`, Flex/Space→`stack`/`wrap`, Grid→`grid`, Layout→`dock`/`panel`/`border`, Image→`image`, Typography→`text-block`/`label`/`rich-text`, Icon→`icon`, Message/Notification→`toast`).
- **net-new** — generic control added here (candidates: Avatar, Tag, Alert, Collapse, Segmented, Rate, Timeline, Steps, Breadcrumb, Pagination, Card, Descriptions, Statistic, Result, Empty, Drawer, Popover, Popconfirm, Skeleton, Affix, Watermark, Carousel, FloatButton, Anchor, AutoComplete, Mentions, Cascader, Transfer, Upload, Calendar, QRCode, Tour).
- **composition** — assembled from existing/net-new controls (e.g. Card = panel+stack+text; Descriptions = grid+label; Result = stack+icon+text+button; Breadcrumb = stack+text+separator). The matrix names constituents; a composition is preferred over a redundant primitive.
- **not-applicable** — React/DOM infrastructure with no rendered surface (`App`, `ConfigProvider`, `Util`), or capabilities the framework already exposes non-as-a-control (theme/policy selection). Each carries an explicit reason.

**Rationale**: "Maximal" is only meaningful and reviewable as an enumerated, dispositioned set (US2). The four categories also bound which items become code vs docs vs no-op, preventing silent gaps (SC-002).

**Alternatives considered**: A free-text "we covered a lot" note — rejected: not verifiable, no honesty check possible.

**Note on net-new vs composition boundary**: the final split between `net-new` and `composition` for borderline items (Card, Steps, Breadcrumb, Result, Descriptions, Segmented) is finalized during P-B when the matrix is authored; the rule is "add a primitive only when composition can't express the interaction/visual-state cleanly."

## Decision 5 — Honesty/coverage check mechanism

**Decision**: An Expecto test (`Feature132CoverageMatrixTests.fs`) parses the matrix doc and fails if: (a) any Ant overview component (from the pinned snapshot list) lacks a row; (b) any row lacks a disposition; (c) any `existing`/`net-new`/`composition` row names a control id absent from `Catalog` or a token entry absent from the `DesignSystem` public surface. Mirrors the F6/131 honesty-check approach (parse-docs-then-assert-against-live-surface).

**Rationale**: Matches an established repo pattern; turns FR-010/FR-011/SC-002/SC-003 into a CI-enforced contract; detects drift when controls/tokens are renamed.

**Alternatives considered**: Manual review only — rejected: drifts silently. Generating the matrix from code — rejected: the disposition + rationale are editorial judgements that belong in the doc, with the check guarding references.

## Decision 6 — Parity test design

**Decision**: `Feature132ThemeParityTests.fs` builds one `Control<'msg>` tree spanning every catalog category **including each net-new family**, resolves it under `Themes.Default` and `Themes.AntDesign`, and asserts: (1) identical behaviour/accessibility contract (roles, names, states, focus order, event bindings); (2) at least one resolved visual property differs (color/spacing/radius/intent); (3) no control reads theme identity to branch behaviour. Follows `Feature093ParityTests.fs` / `Feature105ParityTests.fs`.

**Rationale**: This is the running-code proof of the layer rule (FR-013/FR-014/SC-001) and the regression guard against accidental forks.

**Alternatives considered**: Golden-image diffing — rejected as the *primary* mechanism: brittle and GL-gated; resolved-style/contract assertions are deterministic and headless. (Golden images may be added later as advisory evidence under the harness, not as the gate.)

## Decision 7 — Ant snapshot provenance

**Decision**: Pin the Ant component-overview list to the repo's Ant reference hub snapshot (the hub is the single owner of the Ant retrieval date — `2026-06-16`; there is no upstream Ant version label to record) and record the source + that date in the matrix header so future upstream drift is detectable, not silent (FR-012). Ant facts come from `docs/product/ant-design/` (hub + pattern docs), never live `ant.design` fetches at build time.

**Rationale**: Consistent with the design-language-not-dependency rule and F6's provenance discipline.
