# Feature Specification: standalone spec-kit product mirrors the skill union into all three agent roots

**Feature Branch**: `230-standalone-skill-root-mirror`

**Created**: 2026-07-01

**Status**: Draft

**Input**: Maintainer correction to Feature 229: "in the vendored product there are no claude skills but only agent skills; the requirement was that they mirror each other." → the standalone `spec-kit` product must carry the byte-identical skill union in **all three** agent-skill roots (`.agents/`, `.claude/`, `.codex/`), per **ADR-0011 §1**.

## Context (non-normative)

**ADR-0011 §1** requires every agent-skill root (`.claude/skills/`, `.codex/skills/`, `.agents/skills/`) to hold the **byte-identical union** of all skills produced for the product — the three runtimes (Claude Code, Codex, generic) are interchangeable. **§2** makes the `fsgg-sdd` orchestrator the mirror authority: after invoking the provider it fans the union into all three roots. That covers the **orchestrated** lanes (`--lifecycle sdd`/`none`), where the provider must write `.agents/` only so it never intrudes on the SDD-owned `.claude/`/`.codex/` trees (the `providerWroteSddTree` guard — Templates#47).

**The gap:** the **standalone** lane (`dotnet new fs-gg-ui`, default `--lifecycle spec-kit`) has **no orchestrator** to fan out. Feature 229 confined the provider to `.agents/skills/` under **every** lifecycle, so the standalone product ended up with skills only in `.agents/skills/` — `.claude/skills/` and `.codex/skills/` do not mirror it. That regresses ADR-0011 §1 for the standalone product.

**The fix:** in the **standalone `spec-kit`** lane the template itself materializes the skill union into all three roots (it is the only agent present, so it plays its own mirror authority). The **`sdd`/`none`** lanes stay `.agents/`-only (the orchestrator fans out; unchanged — required to keep Templates#47 unblocked). This is a **content/configuration** change to the template's scaffold source map plus the repo-owned gates; no `src/`/`.fsi` change.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Standalone product's agent roots mirror each other (Priority: P1)

A developer runs `dotnet new fs-gg-ui --profile game` (default `spec-kit`). The generated product carries the **same** skill set in `.agents/skills/`, `.claude/skills/`, and `.codex/skills/`, so whichever agent runtime they use (Claude Code, Codex, or a generic agent) discovers the identical skills.

**Why this priority**: This is the reported regression. Feature 229 left standalone products with skills only in `.agents/skills/`, breaking runtime interchangeability (ADR-0011 §1).

**Independent Test**: Scaffold under `spec-kit` and confirm the `fs-gg-*` skill set (dirs with `SKILL.md`) in `.claude/skills/` and `.codex/skills/` equals the set in `.agents/skills/`, byte-identical per skill.

**Acceptance Scenarios**:

1. **Given** the fixed template, **When** a `game` product is scaffolded under `spec-kit`, **Then** `.agents/skills/`, `.claude/skills/`, and `.codex/skills/` contain the identical `fs-gg-*` skill set (same dirs, byte-identical `SKILL.md`).
2. **Given** a `spec-kit` product, **When** each product UI skill (`fs-gg-{scene,symbology,skiaviewer,elmish,keyboard-input,ui-widgets,styling,layout,testing}` per profile) is inspected, **Then** it is present and byte-identical across all three roots.

---

### User Story 2 - Orchestrated lanes stay `.agents/`-only (Priority: P1)

Under `--lifecycle sdd`/`none`, the template writes UI skills into `.agents/skills/` **only** — `.claude/skills/` and `.codex/skills/` receive **zero** template-authored product skills — so an SDD-orchestrated scaffold does not trip `providerWroteSddTree` and the orchestrator (SDD#57) owns the mirror.

**Why this priority**: The three-root mirror must be added **without** re-introducing the Feature 229 / #42 fix: under orchestration the provider must not write the SDD-owned trees. `sdd`/`none` behavior is unchanged from Feature 229.

**Independent Test**: Scaffold under `sdd` and `none`; confirm `.claude/skills/` and `.codex/skills/` hold **zero** `fs-gg-*` product skills; `.agents/skills/` holds the profile's product skills.

**Acceptance Scenarios**:

1. **Given** the fixed template, **When** a product is scaffolded under `sdd` or `none`, **Then** `.claude/skills/` and `.codex/skills/` contain **zero** template-authored `fs-gg-*` product skills.
2. **Given** the fixed template, **When** products are scaffolded under `sdd` and `none`, **Then** they produce identical skill-tree output to each other.

---

### User Story 3 - Gates encode the lane-conditional mirror (Priority: P2)

The repo-owned Feature 204/219 gates + the validation script encode the invariant: each product-skill source emits to `.agents/skills/` (all lifecycles) **and** `.claude/skills/` + `.codex/skills/` (spec-kit only); the live observation proves the three-root mirror under `spec-kit` and zero product skills in `.claude/`/`.codex/` under `sdd`/`none`.

**Why this priority**: Feature 229's gates assert the opposite ("no source targets `.claude/skills/`"); they must be re-worked or they fail on the fixed template. A guard keeps a future skill from being added to only one root.

**Acceptance Scenarios**:

1. **Given** the corrected gates, **When** a product skill is added with an `.agents/`-only source (no `.claude/`/`.codex/` spec-kit twins), **Then** a gate fails naming the missing root.
2. **Given** the corrected gates, **When** the template is audited and scaffolded live, **Then** the report records the three-root mirror under `spec-kit` and `claude/codex-product-skills=0` under `sdd`/`none`, `result: pass`.

### Edge Cases

- **`.codex/skills/` is newly populated** (nothing wrote it before). Under `spec-kit` it now mirrors `.agents/skills/`; under `sdd`/`none` it stays empty.
- **Byte-identity across roots**: `.claude/`/`.codex/` product skills must be the **canonical** `template/product-skills/` versions (as `.agents/` gets), not the differing repo-root `.agents/skills/` copies — so per-skill mirror sources are required, not just a blanket copy.
- **Base workspace skill `fs-gg-project`**: it is part of the union and appears in all three roots under `spec-kit`; under `sdd`/`none` the whole base `.claude/`/`.agents/` workspace is suppressed as today.
- **`sdd`/`none` unchanged** from Feature 229: `.claude/`/`.codex/` receive zero product skills (the orchestrator fans out).

## Requirements *(mandatory)*

- **FR-001**: Under `spec-kit`, the template MUST author the byte-identical skill union into `.agents/skills/`, `.claude/skills/`, and `.codex/skills/` — the same `fs-gg-*` skill dirs with byte-identical `SKILL.md` in each root.
- **FR-002**: The `.claude/skills/` and `.codex/skills/` product skills MUST be the canonical `template/product-skills/` versions (matching `.agents/skills/`), delivered via per-skill mirror sources (a blanket copy of the repo-root `.agents/skills/` is insufficient — those copies differ).
- **FR-003**: Under `sdd` and `none`, the template MUST author **zero** `fs-gg-*` product skills into `.claude/skills/` and `.codex/skills/` (unchanged from Feature 229); the orchestrator (SDD#57) fans out. `sdd` and `none` MUST produce identical skill-tree output.
- **FR-004**: `.agents/skills/` MUST be unchanged from Feature 229 under every lifecycle (the provider tree never shrinks).
- **FR-005**: The repo-owned Feature 204/219 gates + `validate-lifecycle-template.fsx` MUST encode the lane-conditional mirror and MUST fail if a product skill emits to `.agents/` without its `spec-kit` `.claude/`+`.codex/` twins.
- **FR-006**: Delivery MUST be confirmed by an **observed scaffold** (three-root mirror under `spec-kit`; zero product skills in `.claude/`/`.codex/` under `sdd`/`none`).
- **FR-007**: Content/configuration + test-logic only — no `src/**`, no `.fsi`. A template version bump ships the correction (re-release vehicle).

## Success Criteria *(mandatory)*

- **SC-001**: A `spec-kit` scaffold's `.claude/skills/` and `.codex/skills/` `fs-gg-*` set equals `.agents/skills/`'s, byte-identical per skill (100% mirror).
- **SC-002**: An `sdd`/`none` scaffold has **0** `fs-gg-*` product skills in `.claude/skills/` and **0** in `.codex/skills/`; `.agents/skills/` holds the profile set.
- **SC-003**: `.agents/skills/` is byte-identical to the Feature 229 baseline under every lifecycle.
- **SC-004**: The corrected gates fail on an `.agents/`-only product skill and pass on the mirrored template; the live report records the three-root mirror (`spec-kit`) and `claude/codex-product-skills=0` (`sdd`/`none`), `result: pass`.

## Assumptions

- **The template is its own mirror authority only in the standalone lane.** ADR-0011 §2 assigns the mirror to `fsgg-sdd` for orchestrated composition; standalone `spec-kit` has no orchestrator, so the template materializes the union itself there. This does not conflict with §2 (different lanes) and does not affect `sdd`/`none` (still `.agents/`-only → Templates#47 stays unblocked).
- **Per-skill mirror sources are required** because the repo-root `.agents/skills/` copies differ byte-wise from the canonical `template/product-skills/` versions (verified). A blanket `.agents/skills/`→`.claude/`/`.codex/` copy would mirror stale versions; the per-skill `template/product-skills/`→root sources deliver the canonical bodies (this is the pattern the pre-229 template used for `.claude/`; this feature restores it and adds the `.codex/` parallel).
- **`.codex/skills/` newly populated under `spec-kit`.** It was never written before; the three-root requirement (ADR-0011 §1) adds it. Under `sdd`/`none` it stays empty.
- **Supersedes Feature 229's full confinement in the standalone lane.** Feature 229 removed all `.claude/skills/` writes under every lifecycle; that was correct for `sdd`/`none` but wrong for standalone `spec-kit`. This feature restores the `spec-kit` mirror (now across all three roots) while keeping `sdd`/`none` clean.
- **No src/behavior change.** Tier 2 content/config + gate rework; a template version bump ships it.
