# Feature Specification: Backward-Compatibility Shim Removal

**Feature Branch**: `184-backcompat-cleanup`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "create specs for this cleanup" — remove backward-compatibility code / legacy shims surfaced by the post code-health research, on the premise that there are no external consumers that depend on the deprecated identities.

**Change Classification**: **Mixed, per item (evidence-based).** Two candidates remove public API surface and are **Tier 1** — `ScrollViewport.MaxOffset` (US1) and `ControlEvent.Payload` (US3) are public record fields on public types, so they require `.fsi` updates, surface-baseline updates, package version bumps, and CompatibilityLedger entries. The other two are **Tier 2 (internal)** — the `Composition` legacy node layer (US2) lives in a `module internal` (`Composition.fsi:9`) and never appears on the public surface, and the untyped chart fallback (US4) is an internal `chartValues` branch. Both US2 and US4 touch a production path and so MUST be byte-stable on the retained path (FR-005), but neither changes public surface, so neither needs a bump or ledger entry. (Phase-0 research refined the original blanket Tier-1 reading; see plan.md "Change Classification" and research.md D1. FR-006/FR-007 therefore bind only the public-surface items, US1 and US3.)

## Context & Motivation

A whole-repo research pass identified code that exists **only to preserve backward compatibility with earlier internal authoring patterns** — not to serve any present consumer. The repository ships as versioned `FS.GG.UI.*` packages whose only consumers today are **in-tree** (4 sample products + the product template); no external/downstream generated product is known to depend on the deprecated identities below. Removing dead compatibility surface reduces API confusion, eliminates dual-path "do it the old way or the new way" ambiguity, and shrinks the maintained public surface.

The research also found several things **named** "legacy" that are in fact live production code and are explicitly **out of scope**: the string-keyed widget `*.create` builders, the SkiaViewer `LegacyHostMsg` message pump, and the `-v1`/`-v2` policy/scenario identity tags (which are stable identifiers, not version-gated format readers).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Remove the `MaxOffset` scroll alias (Priority: P1)

A framework maintainer reads scroll geometry from a render result. Today `ScrollViewport` exposes both `MaxOffset` and `MaxVerticalOffset`, where `MaxOffset` is a pure duplicate retained as a "vertical compatibility alias." The maintainer should have exactly one field for the vertical maximum, with no redundant alias to keep in sync.

**Why this priority**: Cleanest, most self-contained win. The alias is a literal duplicate of `MaxVerticalOffset`, and its only readers in the repository are three tests (no src/sample/template reader). Lowest risk, immediate surface reduction — the right first slice to validate the whole approach end-to-end (surface baseline + package bump + ledger).

**Independent Test**: Remove `MaxOffset` from `ScrollViewport`; retarget the three consuming tests to `MaxVerticalOffset`; confirm build + full test sweep stays at baseline parity and the affected package's surface baseline regenerates cleanly.

**Acceptance Scenarios**:

1. **Given** a render result containing a scrolled `scroll-viewer`, **When** a caller reads the vertical scroll maximum, **Then** exactly one field (`MaxVerticalOffset`) provides it and no `MaxOffset` alias is present on the public surface.
2. **Given** the affected package's surface baseline, **When** the change is built, **Then** the baseline is updated to drop `MaxOffset` and the package version is bumped with a CompatibilityLedger entry describing the removal and the one-line migration (`MaxOffset` → `MaxVerticalOffset`).
3. **Given** the in-tree samples and template, **When** they are rebuilt against the bumped package, **Then** none of them referenced `MaxOffset` and all continue to build and pass.

---

### User Story 2 - Retire the `Composition` legacy node-form layer (Priority: P2)

A maintainer composing modifier chains today has a parallel "legacy node form" entry point (`LegacyForm` with `LegacyClipping` / `LegacyTranslation` / `LegacyPerspective` / `LegacyCachedSubtree` / `LegacyText` / `LegacyOverlay`, plus `legacyLower` and `compatibilityEvidence`/`DeprecatedWithMigration`) that mirrors the modern modifier IR. This compatibility layer should be removed so there is one way to express each modifier, with the single live production use migrated onto the modern path first.

**Why this priority**: Higher value (removes a whole parallel type family + its evidence machinery) but higher effort than US1: one production call site (`Control.fs`, the overlay-lowering path) currently routes through `legacyLower`, and a dedicated test family exercises the rest. The production caller must be migrated to the modern modifier path before the layer is deleted, so it cannot be a pure deletion.

**Independent Test**: Migrate the one production caller off `legacyLower`; delete the `LegacyForm`/`legacyLower`/`compatibilityEvidence` surface and the Feature-140 legacy-compatibility tests; confirm the lowered modifier output for the overlay path is byte-identical to before and the full sweep stays at baseline parity.

**Acceptance Scenarios**:

1. **Given** the modern modifier IR, **When** the overlay-lowering production path runs, **Then** it produces a byte-identical modifier chain without calling any `legacy*` helper.
2. **Given** the `Composition` public surface, **When** the change is built, **Then** `LegacyForm`, `LegacyCompatibilityStatus`, `legacyLower`, and `compatibilityEvidence` no longer appear in the surface baseline, the package is bumped, and a CompatibilityLedger entry records the removal.
3. **Given** the Feature-140 legacy-compatibility tests, **When** the layer is removed, **Then** tests asserting the *removed* compatibility behavior are deleted (not weakened), and any still-relevant overlay behavior is covered by a test on the modern path.

---

### User Story 3 - Retire the `ControlEvent.Payload` string-compat field (Priority: P3)

A control event today carries both a typed `Nav` payload and a stringly-typed `Payload : string option` that is "retained for backward compatibility" and dual-set alongside `Nav`. The string field should be removed so control events have a single, typed representation, with all readers migrated to the typed accessor first.

**Why this priority**: Largest blast radius of the candidates. Although labeled backward-compat, `Payload` has roughly seven **live** production-`src` readers across the framework (interactive/navigation/data-entry attribute helpers, widget lowering, data-grid, control change/select handlers), plus dual-set writers in `Controls.Elmish`/`OverlayState` and several test readers. It is load-bearing today; removal is a real migration to the typed payload, not a deletion. Sequenced last so the lower-risk wins land first.

**Independent Test**: Migrate every `Payload` reader to the typed payload (or a typed accessor that replaces the string lookup); remove the field; confirm event-handling behavior is unchanged via existing event/widget tests at baseline parity.

**Acceptance Scenarios**:

1. **Given** a navigation/selection event, **When** a handler reads the moved item or value, **Then** it obtains it from the typed payload and no code reads a string `Payload` field.
2. **Given** the `ControlEvent` public surface, **When** the change is built, **Then** `Payload` is removed from the surface baseline, affected packages are bumped, and a CompatibilityLedger entry documents the typed replacement and migration.
3. **Given** the in-tree samples and template, **When** they are rebuilt, **Then** any sample/template handler that read `Payload` is migrated and continues to pass.

---

### User Story 4 - Remove the untyped flat-chart authoring fallback (Priority: P4)

Chart value reading today accepts both the typed series/point shapes the typed front door stores **and** a flat `float list`/`float array` fallback "retained for legacy untyped authoring." If no current authoring relies on the flat shape, the fallback should be removed so chart values have a single typed source.

**Why this priority**: Lowest-confidence candidate. It is observable behavior, not just surface, and confirming no authoring (including samples and the template) relies on the flat shape is a prerequisite. Sequenced last and gated on that confirmation; if any in-tree consumer authors flat lists, this story is dropped rather than forced.

**Independent Test**: Confirm no in-tree consumer (src, samples, template) authors flat float lists for charts; remove the fallback branch; confirm chart rendering for the typed path is byte-identical and the full sweep stays at baseline parity.

**Acceptance Scenarios**:

1. **Given** a chart authored through the typed front door, **When** its values are read, **Then** the output is byte-identical to before the fallback removal.
2. **Given** the repository (src + samples + template), **When** scanned for flat-list chart authoring, **Then** zero such call sites exist, justifying removal.
3. **Given** any discovered flat-list authoring, **When** found, **Then** this story is descoped and the finding recorded, rather than breaking that consumer.

---

### Edge Cases

- **An in-tree consumer turns out to depend on a "dead" identity.** The premise ("no consumer") is verified per-item against `src/` + the 4 samples + the template before removal; if a real dependency is found, that item is migrated first or descoped — never broken silently.
- **Removal changes lowered/rendered output.** Each removal that touches a production path (US2 overlay, US4 chart) must demonstrate byte-identical output for the retained path; any drift blocks the removal (narrow/retain, per the code-health Feature-182 FR-009 precedent).
- **Surface freeze conflict.** Removals intentionally change the public surface; baselines and the public-surface union must be regenerated and the change recorded as Tier 1 with package bumps, rather than asserting an unchanged surface.
- **A removed identity is referenced only by its own tests.** Those tests are deleted (the behavior no longer exists), never weakened to keep a green build.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST remove the `ScrollViewport.MaxOffset` alias and expose the vertical scroll maximum through exactly one field (`MaxVerticalOffset`).
- **FR-002**: The system MUST remove the `Composition` legacy node-form layer (`LegacyForm`, `LegacyCompatibilityStatus`, `legacyLower`, `compatibilityEvidence`) after migrating its single live production caller to the modern modifier IR.
- **FR-003**: The system MUST remove the `ControlEvent.Payload` string-compatibility field after migrating all readers to the typed payload representation.
- **FR-004**: The system MUST remove the untyped flat-chart authoring fallback **only if** no in-tree consumer (src, samples, template) authors flat float-list chart data; otherwise this requirement is descoped with the finding recorded.
- **FR-005**: For every removal that touches a production code path, the retained path's lowered/rendered output MUST be byte-identical to the pre-change output.
- **FR-006**: Each removal MUST update the affected public-module surface baseline(s) and the public-surface union; the change MUST NOT assert an unchanged surface.
- **FR-007**: Each package whose public surface changes MUST receive a version bump and a CompatibilityLedger entry documenting the removed identity and its migration.
- **FR-008**: Tests that assert *removed* compatibility behavior MUST be deleted; retained behavior MUST keep equivalent coverage on the modern path. No assertion may be weakened to green the build.
- **FR-009**: The premise of "no dependent consumer" MUST be verified per item against `src/`, the in-tree samples, and the product template before that item is removed.
- **FR-010**: Items named "legacy"/"compat" that are live production code with active consumers — the widget `*.create` builders, the SkiaViewer `LegacyHostMsg` pump, and the `-v1`/`-v2` identity tags — MUST NOT be removed by this feature.
- **FR-011**: After all in-scope removals, the full build + test sweep MUST remain at baseline red/green parity (the same known pre-existing reds, no new reds, no flipped greens).

### Key Entities

- **Deprecated identity**: A public or behavioral element retained solely for backward compatibility (`MaxOffset`, the `Composition` legacy layer, `ControlEvent.Payload`, the flat-chart fallback). Attributes: owning package, public-surface status, current consumers, modern replacement, migration note.
- **Consumer**: A code site that depends on a deprecated identity — classified as production-`src`, test-only, in-tree sample, or product template. Determines whether an item is a free deletion, a migrate-then-delete, or descoped.
- **CompatibilityLedger entry**: The per-package record documenting a public-surface change (removed identity + migration guidance) accompanying a version bump.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All four deprecated identities are either removed or explicitly descoped with a recorded reason; zero remain in an ambiguous "kept but unused" state.
- **SC-002**: The public surface is strictly smaller — the affected surface baselines drop the removed identities and add none.
- **SC-003**: For every production-path removal, the retained path's output is byte-identical (verified by golden/round-trip comparison).
- **SC-004**: The full build + test sweep is at baseline red/green parity after the feature, with no new reds and no flipped greens.
- **SC-005**: Every public-surface change is accompanied by a package version bump and a CompatibilityLedger entry; no surface change ships without one.
- **SC-006**: Zero in-scope removal breaks any in-tree sample or the product template (each rebuilds and passes against the bumped packages).
- **SC-007**: No test is weakened; tests for removed behavior are deleted and retained behavior keeps equivalent coverage.

## Assumptions

- **No external/downstream consumer** depends on the deprecated identities. In-tree consumers (4 samples + template) DO consume the packages and are verified per item (FR-009); the premise is taken to mean no *unknown external* product relies on these specific deprecated elements.
- The repository's surface-stability governance (`.fsi` baselines, CompatibilityLedger, per-feature package bumps) **stays in force**; this feature works *within* it (recording deliberate removals) and does not relax or remove the governance itself.
- "Byte-identical retained output" is the safety bar for production-path removals, consistent with the code-health phase precedent (Feature 182 FR-009): if a removal cannot be shown byte-stable, it is narrowed or descoped.
- Each removal is independently shippable and independently verifiable; the four stories may land as separate commits in priority order (P1 → P4).
- Items merely *named* "legacy" but actively used (widget `*.create`, `LegacyHostMsg`, `-v1`/`-v2` tags) are deliberately excluded; this feature is about dead compatibility surface, not renaming live code.
