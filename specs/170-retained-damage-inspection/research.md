# Research: Retained Render Damage Inspection

## Decision: Add retained/damage records instead of changing `VisualInspectionArtifact`

**Rationale**: `VisualInspectionArtifact` is a public F# record. Adding fields would break source compatibility for existing record literals in tests or consumers. Retained inspection should therefore be represented by additive records that can wrap or link to the existing final-screen artifact while preserving Feature 165 consumers.

**Alternatives considered**:

- Add optional fields directly to `VisualInspectionArtifact`: rejected because F# record field additions are source-breaking.
- Put retained/damage facts only in Testing: rejected because Controls must emit evidence and samples/generated products need dependency-light machine contracts.
- Create a new package: rejected because the existing `Scene` inspection vocabulary already owns dependency-light visual evidence and no new package boundary is needed.

## Decision: Emit from the real retained render transition path

**Rationale**: The feature is about explaining retained behavior after `RetainedRender.step`, not a separate model. Controls already has internal retained identities, invalidation evidence, shifted/recomputed counters, dirty rectangles, picture/replay counters, and existing union-area helpers. Emitting inspection from that path proves the same transition the runtime uses.

**Alternatives considered**:

- Recompute a separate diff from two final `Control.renderTree` outputs: rejected because it would miss retained decisions, reused fragments, and unsupported retained facts.
- Inspect screenshots or pixels: rejected because damage locality is semantic evidence and must run headless.
- Expose internal retained records directly as public API: rejected because it would freeze implementation details instead of an inspection contract.

## Decision: Report dirty regions with true union area and visible percentage

**Rationale**: Existing retained-render tests already protect union-area semantics for overlapping rectangles. Damage inspection must preserve that rule so overlapping dirty regions are counted once and dirty area never exceeds the visible frame. The summary should include union area, visible dirty area, dirty percentage, and the contributing rectangles/regions.

**Alternatives considered**:

- Sum all dirty rectangles: rejected because the retrospective explicitly identifies double-counting overlap as misleading.
- Store only dirty rectangle count: rejected because count cannot distinguish one full-surface rectangle from several small localized rectangles.
- Store only percentage: rejected because reviewers still need affected region and node context.

## Decision: Validate locality against expected regions, with full-surface as an automatic blocker for localized interactions

**Rationale**: A single global broad-damage threshold is too crude for different screen sizes and controls. Each localized test or sample assertion should declare expected affected regions/scopes and an optional maximum dirty percentage. Validation then flags dirty regions outside that scope, excessive union area, and any full-surface dirty region for a localized transition unless an intentional exception is present.

**Alternatives considered**:

- Hardcode one broad threshold for every scenario: rejected because a small control and a large panel have different legitimate dirty budgets.
- Accept any partial damage below 100%: rejected because a 70% repaint can still be too broad for a button hover.
- Require pixel comparison: rejected because the feature is structured readiness evidence, not image analysis.

## Decision: Keep shifted and repainted node evidence separate

**Rationale**: The retained renderer already distinguishes shifted work from changed paint work in `WorkReductionRecord`. Reviewers need to know whether a localized change caused layout movement, paint work, or both. A node may be shifted without repainting, repainted without moving, both, added, removed, reused, unaffected, or unsupported.

**Alternatives considered**:

- Count shifted nodes as repainted nodes: rejected because it hides layout movement as paint cost.
- Report only aggregate counts: rejected because reviewers cannot identify which visual region caused a finding.
- Report every retained internal field: rejected because the inspection contract should be stable and reviewer-oriented.

## Decision: Model first-frame, empty damage, unsupported damage, and not-inspected damage explicitly

**Rationale**: The first retained frame has no prior frame; a stable transition can have empty damage; a subsystem can be unavailable while layout inspection exists; and some declared scopes can be intentionally not inspected. These states must be machine-readable so summaries do not treat missing evidence as accepted evidence.

**Alternatives considered**:

- Omit damage for first frames or unsupported scopes: rejected because absent data is ambiguous.
- Treat empty damage and unsupported damage the same: rejected because empty damage is valid evidence and unsupported damage is not.
- Collapse not-inspected into environment-limited: rejected because not-inspected can be a planning or coverage gap, not a host limitation.

## Decision: Use stable public inspection ids, not raw internal retained ids, as the artifact identity

**Rationale**: Internal `RetainedId` values are useful for retained matching but should not become a permanent public contract. Artifacts should identify nodes with the existing inspection id strategy, using authored keys when available and deterministic structural ids otherwise, and may include an opaque retained identity token for correlation when needed.

**Alternatives considered**:

- Expose `RetainedId` publicly: rejected because it freezes internal retained implementation details.
- Require authored keys for every node: rejected because existing controls and generated products do not always have keys.
- Use run-order ids only: rejected because stable repeated-run findings would churn.

## Decision: Migrate the AntShowcase `charts-statistical` full-shell assertion first

**Rationale**: `VisualShell.theme and current page affordances render in full shell` already exercises a real page through the shell at preferred size in both canonical themes. Migrating this assertion to consume structured retained inspection evidence proves sample value without changing screenshot target matrices: current readiness tests expect 38 preferred targets and 12 minimum targets.

**Alternatives considered**:

- Migrate every visual-shell assertion: rejected as broader than the representative sample-adoption requirement.
- Migrate only a pure shell-layout unit test: rejected because the spec asks for a real page, theme, and size.
- Change screenshot readiness targets: rejected because screenshot count changes are out of scope unless deliberately documented.

## Decision: Add a `retained-inspection` validation lane

**Rationale**: The repository already documents `scripts/run-validation-lanes.fsx` as the maintained validation entry point. Adding a feature-focused lane gives contributors a canonical command that runs retained inspection, damage locality, harness registration, and AntShowcase sample-adoption checks without depending on a stale wrapper name.

**Alternatives considered**:

- Recreate a legacy wrapper command: rejected because the retrospective calls out stale wrapper drift.
- Document only several direct `dotnet test` commands: rejected because the spec requires a canonical entry point.
- Add the lane to required default validation immediately: rejected for planning; the feature can make it required only if maintainers accept the cadence cost in `docs/validation/validation-set.md`.
