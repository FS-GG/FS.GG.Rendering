# Feature Specification: Central Visual-State Style Resolver (`theme → kind → intent → states → style`) — Workstream F, Phase F4

**Feature Branch**: `129-central-style-resolver`

**Created**: 2026-06-16

**Status**: Draft

**Input**: User description: "next item in fs.gg" → resolved to Workstream F, Phase F4 of the 2026-06-15 missing-features implementation plan: the central visual-state style resolver (`resolve: theme → kind → intent → states → ControlStyle`), migrate button intents to consume it, with a parity test proving resolver output ≡ current rendering for the default policy (behaviour-neutral until a theme opts in). Co-designed with the C11 visual-state style layer (093/095/096); the first three F pillars (F1 tokens / F2 color policy / F3 template parameter) are already shipped.

## Context & Motivation *(informative)*

The framework holds a **semantic intent** for a control's appearance (`ButtonIntent = Primary | Secondary | Danger | Ghost`) and a **visual-state** vocabulary (`VisualState = Normal | Disabled | Hover | Pressed | Focused | Selected | Loading | Validation`). It already has a per-state/per-class resolver (`Style.resolve theme baseStyle classes state`, shipped with the 093 visual-state style layer) that controls call. The missing piece named by the plan is the **front half** of resolution: turning a control's **kind + semantic intent** into the `baseStyle` that resolver consumes, in *one* central place, so controls stop hand-assembling styles or — worse — dropping the intent on the floor.

A Phase-0 fact (verified in the current tree) makes the scope concrete and explains why this is high-value:

> **Intent is currently dead code.** A control's intent is lowered to a `style` *attribute* string (`"primary"`/`"danger"`/…), but that attribute is **never extracted** into the `StyleClass list` the renderer reads (the renderer only reads the `styleClasses` key). So `Style.resolve` always receives `[]`, and `buttonGeom` picks its `baseStyle` from a hardcoded `primary: bool` flag (`true` for `"button"`, `false` for `"icon-button"`). **A `Danger` button renders byte-identically to a `Primary` button.** The variant→colour machinery exists (`applyCustom "danger"` → `applyVariant Danger` → `theme.Danger`) but is unreachable.

F4 closes that gap by introducing a single resolution path that takes **intent** (and kind, and states) as first-class inputs, and migrating the Button to consume it. Per the plan and an explicit scope decision (2026-06-16), F4 is **behaviour-neutral under the default theme**: wiring the resolver must leave today's rendered output **byte-identical**. The intent input becomes *capable* of changing appearance, but that divergence is **opt-in by a theme/policy** (the Ant theme in D2 / a future feature), not switched on under the default theme in F4. F4 builds the seam and proves it carries today's output unchanged; it does not "fix" the dead-code colour drop as a visible change.

This keeps F4 small, reviewable, and reversible, and it preserves the repo's hard CI invariants: **zero public-surface delta** (public token/policy/resolver promotion is deferred to F5) and unchanged test pass/skip counts.

## User Scenarios & Testing *(mandatory)*

> "Users" here are the framework's own engineers and downstream control/theme authors — the audience the resolver serves. The spec is written from their perspective.

### User Story 1 - One central path resolves a control's draw style from intent + state (Priority: P1)

A control author renders a control by asking **one** resolution path for its concrete draw style, passing the control's **kind**, its **semantic intent**, and its current **visual state(s)** plus the active theme — instead of hand-building a style or branching on ad-hoc booleans. The intent is an *input that is actually read*, not a string that is silently discarded.

**Why this priority**: This is the feature. Without a single intent-aware resolution path, controls keep duplicating style logic and the semantic intent stays dead. Everything else (parity proof, future theme divergence) depends on this path existing.

**Independent Test**: Call the resolution path with a fixed theme across the full cross-product of {kind} × {intent} × {visual state} and confirm it returns a concrete style for every combination, deterministically, with no exception — and that changing the **intent** argument is observable in the path's behaviour (the input reaches resolution rather than being dropped).

**Acceptance Scenarios**:

1. **Given** the default theme and a button kind with intent `Primary` in state `Normal`, **When** the resolution path is invoked, **Then** it returns a single concrete draw style (foreground, fill, stroke, stroke width, font) with no further branching required by the caller.
2. **Given** any control kind, any intent, and any single visual state, **When** the resolution path is invoked, **Then** it returns a concrete style and never raises — resolution is **total**.
3. **Given** two invocations differing only in the **intent** argument, **When** resolved under a theme whose mapping distinguishes those intents, **Then** the two results differ — proving intent is consumed, not discarded (contrast with today, where they are identical).
4. **Given** classes and a visual state alongside the intent, **When** resolved, **Then** the existing precedence is preserved: base (kind+intent) is overlaid by classes (in order), then by the visual state — the F4 front-half composes with the 093 resolver, it does not replace its precedence rules.

---

### User Story 2 - Migrating the Button is behaviour-neutral under the default theme (Priority: P1)

A maintainer migrates the Button control to obtain its `baseStyle` from the central resolution path (replacing the hardcoded `primary: bool` dispatch). After the migration, **every control the framework renders today looks exactly the same** under the default theme — pixel-for-pixel, byte-for-byte — and the whole existing test corpus stays green with the same pass/skip counts.

**Why this priority**: Co-equal P1. The plan's acceptance for F4 is "resolver migration is behaviour-neutral under the default policy (parity test green)." A migration that changes any default-theme output is a regression, not F4. The byte-identity gate is what makes this change safe to land and reversible.

**Independent Test**: Render the representative control set (buttons in each intent and state, plus the catalog controls that already call `Style.resolve`) twice — once via the pre-migration path and once via the resolver-migrated path — under the default theme, and assert the two scene/style outputs are byte-identical. Run the full existing suite and confirm unchanged pass/skip counts.

**Acceptance Scenarios**:

1. **Given** the default theme, **When** a `"button"` (filled) and an `"icon-button"` (outline) are rendered through the migrated path in every visual state, **Then** their resolved styles and emitted scene are byte-identical to the pre-migration output.
2. **Given** the default theme, **When** a button is rendered with each of `Primary`/`Secondary`/`Danger`/`Ghost`, **Then** the output matches today's output for that path — i.e. the default theme's intent mapping reproduces the current appearance (intents do **not** visibly diverge under the default theme in F4).
3. **Given** the full existing test corpus, **When** it is run after migration, **Then** pass/skip counts are unchanged and no test is removed, skipped, or weakened to accommodate the change.
4. **Given** at-rest, animation, layout, identity, and cache behaviour, **When** measured after migration, **Then** they are unaffected (the resolver touches style assembly only, not the render-loop seams owned by 097/099/103/116/117/120/121).

---

### User Story 3 - Intent is a theme-overridable seam (capability, not yet exercised) (Priority: P2)

A theme/policy author can make the **same** intent resolve to a **different** draw style — e.g. a real `Danger` red — by supplying a theme/policy that maps intents differently, **without forking the control** (no `DangerButton`, no per-theme control variant). In F4 the default theme keeps neutrality; this story proves the *seam admits* divergence so the Ant theme (D2) and later policies can opt in.

**Why this priority**: P2 — it is the strategic payoff (it is why F4 exists ahead of D2), but it is a **capability** demonstration, not a default behaviour change. F4 ships the seam and proves divergence is reachable through a non-default mapping; it does not turn divergence on by default.

**Independent Test**: Supply a non-default mapping (a test theme/policy) that distinguishes `Danger` from `Primary`, resolve the same button under it, and confirm the resolved style differs from the `Primary` result — through the resolution path alone, with **no edit to any control's render code**.

**Acceptance Scenarios**:

1. **Given** a test mapping where `Danger` ≠ `Primary`, **When** a button is resolved with intent `Danger`, **Then** its style differs from the same button resolved with intent `Primary`, and neither required a new control type.
2. **Given** the same control set, **When** rendered once under the default theme and once under a divergent test mapping, **Then** only the divergent run changes — the default run remains byte-identical to today (User Story 2 still holds).
3. **Given** the framework's "one semantic control set, many looks" rule, **When** F4 lands, **Then** the count of control types is unchanged — intent selects a *style*, never a *control*.

---

### Edge Cases

- **Unknown / custom intent or kind**: resolution is total — an unrecognised intent or a `Custom`/free-form control kind falls back to a defined default style, never throwing and never silently producing an empty/transparent style that disappears.
- **Combined / conflicting visual states** (e.g. `Disabled` together with `Hover`, or author-set `Validation` over a derived `Hover`): precedence is deterministic and inherited from the existing visual-state layer (093 base<class<state ordering; 096 derived-state precedence and author-intent-out-ranks-derived) — F4 does not introduce a second, conflicting precedence.
- **`Validation` / `Loading` states on a button**: resolve to a concrete style (no exception); under the default theme the result preserves today's output.
- **`icon-button` (the current `primary = false` outline path)**: preserved exactly — the migration must not collapse the filled/outline distinction.
- **A control that does not (yet) carry an intent**: resolves under a defined default intent; behaviour-neutral.
- **A theme missing a colour an intent would use** (e.g. relying on `theme.Success`/`theme.Warning`, added in feature 125): the default theme already carries these; resolution must not require fields the public `Theme` record lacks (no `Theme` shape change in F4).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The framework MUST provide a **single resolution path** that maps `theme + control-kind + semantic-intent + visual-state(s)` to a **concrete draw style** (the existing `ResolvedStyle`-shaped fields: foreground, fill, stroke, stroke width, font family, font size, font weight). It composes with — and reuses, not replaces — the existing per-class/per-state resolver (`Style.resolve`) for the class+state overlay.
- **FR-002**: The **semantic intent MUST be a consumed input** to that path. The Button MUST be migrated to obtain its `baseStyle` from the resolution path keyed by its intent, replacing the hardcoded `primary: bool` dispatch. The current dead-code drop (intent lowered to a `style` attribute that is never read) MUST be eliminated as a code path — the intent reaches resolution.
- **FR-003**: Under the **default theme**, the migration MUST be **behaviour-neutral**: the resolved style and emitted scene for every control the framework renders today MUST be **byte-identical** to the pre-migration output, across all intents and all visual states. (The default theme's intent mapping reproduces today's appearance; `Danger` is permitted to look like `Primary` under the default theme — see Assumptions.)
- **FR-004**: Resolution MUST be **total and deterministic**: every `(kind, intent, state)` combination — including unrecognised/`Custom` kinds and unknown intents — yields a concrete style with no exception and no nondeterminism (same inputs → same style).
- **FR-005**: The intent input MUST be **theme/policy-overridable**: a theme or policy can map the same intent to a different style **without forking the control** and **without editing any control's render code**. F4 MUST prove this seam by demonstrating divergence through a non-default mapping (User Story 3); it MUST NOT enable that divergence under the default theme.
- **FR-006**: The existing **visual-state and class precedence MUST be preserved** (base from kind+intent < classes in attach order < visual state; the 096 derived-state precedence and author-intent-out-ranks-derived rules unchanged). F4 adds the kind+intent → base step ahead of that pipeline; it does not alter the pipeline.
- **FR-007**: F4 MUST add **no new public package API surface** — **zero** per-package surface-baseline delta and zero design-token-drift-baseline delta. Any deliberate promotion of a resolver/intent/style surface to public is **deferred to F5**. The new resolution logic lands internal/additive, reachable by tests through the established internal-visibility grant.
- **FR-008**: F4 MUST NOT fork controls per intent or per theme (no `DangerButton`, no theme-specific control variant). Intent selects a *style*; there remains **one semantic control set**. The count of control types is unchanged.
- **FR-009**: F4 MUST add **no new project/package dependency**. The resolver lives within the existing dependency layering (DesignSystem depends on Scene only; Controls references DesignSystem only; Controls never references a theme). It MUST NOT introduce a JSON parser or any web/React/DOM/icon-font dependency into a product/test assembly.
- **FR-010**: The **behaviour-neutrality MUST be proven by an automated parity check** that compares resolver-migrated output against the pre-migration output for the default theme across a representative control/intent/state set, and is part of the always-runnable suite (no manual visual diffing as the sole evidence).
- **FR-011**: F4 MUST NOT alter render-loop seams owned by other features — animation clock (099/103/121), layout/incremental layout (097), memoization (113), virtualization (114), picture/text caches (116/117), or fingerprint/replay (120). At-rest, settled, and cached behaviour stay byte-identical; the change is confined to style assembly.
- **FR-012**: F4 MUST NOT change the public `Theme` record shape. It MAY consume `Theme` fields that already exist (including `Success`/`Warning` added in feature 125); broader `Theme` expansion remains deferred (no consumer routes it in F4).

### Key Entities *(include if feature involves data)*

- **Control kind**: the identifier of *what* is being styled (e.g. button, icon-button, and the other catalog kinds). Already a string discriminator in the codebase; F4 keys resolution on it without requiring a new typed enum on the public surface.
- **Semantic intent**: the *role/emphasis* a control should communicate (`Primary | Secondary | Danger | Ghost`, with room for `Success | Warning | Link | Text` as the catalog grows). The first-class input F4 makes live.
- **Visual state(s)**: interaction/validation status (`Normal | Disabled | Hover | Pressed | Focused | Selected | Loading | Validation`). Consumed by the existing per-state resolver; F4 keeps that consumption intact.
- **Resolved draw style**: the concrete, ready-to-paint result (the existing `ResolvedStyle` field set). The single output of the resolution path.
- **Theme/policy mapping**: the function from (kind, intent) → base draw style under a given theme. The default theme's mapping is byte-identical to today; an alternative mapping is what a future theme supplies to make intents diverge.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: **Default-theme parity is exact** — 100% of the controls the framework renders today produce byte-identical resolved styles and scene output before vs. after the migration, across every intent and every visual state in the parity set. (Behaviour-neutral.)
- **SC-002**: **Intent is consumed** — for 100% of button intents, the intent argument reaches resolution (no value is dropped on the floor); demonstrated by a non-default mapping under which ≥1 intent resolves to a style strictly different from `Primary`, where today it does not.
- **SC-003**: **Resolution is total** — an exhaustive enumeration of {kind} × {intent} × {visual state} returns a concrete style for every combination with zero exceptions and zero nondeterministic results across repeated runs.
- **SC-004**: **Zero public-surface delta** — the per-package surface baselines and the design-token-drift baseline are unchanged after F4 (the drift gate passes with no regenerated public rows attributable to this feature).
- **SC-005**: **Test corpus integrity** — the full existing suite's pass/skip counts are unchanged; no test is removed, skipped, or weakened to accommodate F4; the new parity check runs in the always-runnable tier.
- **SC-006**: **One control set preserved** — the number of control types is unchanged (no control forked per intent/theme).
- **SC-007**: **Divergence is reachable without touching controls** — a theme/policy can remap ≥1 intent to a visibly different style purely through the resolution seam, with **zero** edits to any control's render code, proving the seam D2/Ant will consume.
- **SC-008**: **Render-loop neutrality** — animation, layout, memoization, virtualization, caches, and fingerprint/replay metrics are identical before vs. after F4 for the same inputs (no seam outside style assembly is touched).

## Assumptions

- **Strict behaviour-neutral scope (decided 2026-06-16)**: F4 preserves today's default-theme output byte-for-byte. The currently-ignored intent (a `Danger` button rendering like `Primary` under the default theme) is **preserved**, not "fixed", in F4. Making intents visibly diverge under the default theme — or under the Ant theme — is **out of scope** and belongs to D2/a later feature that opts in through the seam this feature builds. This matches the master plan's "behaviour-neutral until a theme opts in" and the F2/F3 default-neutral precedent.
- **Reuse the existing resolver, don't replace it**: the 093 `Style.resolve theme baseStyle classes state` (class+state overlay) is reused verbatim for the back half of resolution; F4 supplies the front half (kind+intent → `baseStyle`). The existing variant/state colour machinery (`applyVariant`, `applyCustom`) is reused, not duplicated.
- **Internal-additive, public promotion deferred to F5**: like F1 (126) and F2 (127), F4 lands its new logic in an internal module reached by tests via `InternalsVisibleTo`, with no `.fsi`/public-surface change. F5 owns any deliberate public promotion plus the decision record.
- **Button-first migration**: the Button (filled + icon/outline) is the migration target for F4 (the plan names "button intents first"). Other controls already calling `Style.resolve` keep working unchanged; broad control-by-control migration to the new front-half path beyond the button is **not required** by F4 (it may be staged later), provided default-theme parity holds for everything.
- **Co-design with C11 (093/095/096)** is satisfied by composing on the already-shipped visual-state layer; F4 does not re-open 093/095/096.
- **Parity oracle**: the pre-migration rendering is the parity oracle (byte-compare of resolved styles / emitted scene under the default theme), consistent with the repo's existing byte-identity proofs (e.g. 097/103 settled-path identity).
- **Environment/build**: F# on `net10.0`, single solution `FS.GG.Rendering.slnx`, `TreatWarningsAsErrors=true` (code must be warning-clean), headless deterministic tier (no GL required for the parity/totality checks). Surface-drift gate (`scripts/refresh-surface-baselines.fsx` + committed `tests/surface-baselines/*.txt`) and the design-token-drift gate both stay green and are regenerated only if — and they should not be — a public row changes.

## Dependencies

- **Upstream (shipped)**: feature 093 visual-state style layer (`Style.resolve`, `ResolvedStyle`, `VisualState`, `StyleClass`, `StyleVariant`); 096 runtime visual-state bridge (derived-state precedence); 125 DesignSystem/Themes split (the assembly the resolver lives in; `Theme.Success`/`Warning`); 126 token taxonomy (F1) and 127 ColorPolicy (F2) available for later governance but not required to be wired by F4.
- **Downstream (enabled by F4)**: D2 concrete themes (Ant/Fluent/Material) — the first real consumers that opt intent-divergence in through this seam; F5 public-surface promotion + decision record; G3 Ant-styled showcase.
- **Out of scope**: visible intent divergence under any theme; public-surface promotion (F5); `Theme` shape expansion; control migrations beyond the button; wiring ColorPolicy as a runtime gate on resolver output.
