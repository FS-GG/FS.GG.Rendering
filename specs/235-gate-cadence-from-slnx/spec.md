# Feature Specification: derive the gate test loop from the slnx (close the CI-cadence gap)

**Feature Branch**: `235-gate-cadence-from-slnx`

**Created**: 2026-07-02

**Status**: Shipped

**Input**: Finding P4 / B1 of the [2026-07-02 repo review](../../docs/reports/2026-07-02-14-07-repo-code-quality-and-architecture-review.md). Resolves **FS-GG/FS.GG.Rendering#47**.

## Context (non-normative)

`.github/workflows/gate.yml` ran its default local (deterministic, capability `none`) tier off a
hardcoded literal — `for p in Scene Layout KeyboardInput Elmish Controls Diagnostics Testing Lib`.
Six test projects present in `FS.GG.Rendering.slnx` compiled under `dotnet build` but were named in
**no** CI cadence (gate deterministic, gate GL, release, or capability), so their assertions never
executed:

- `Build.Tests`, `Canvas.Tests`, `Symbology.Tests`, `Symbology.Render.Tests`,
  `SymbologyBoard.Tests`, `Rendering.Harness.Tests`.

All six are capability `none` (deterministic / offscreen-CPU): none needs GL, X11, or a display
(`Symbology.Render.Tests` rasterises through the raster `SKSurface.Create(info)` path in
`src/SkiaViewer/ReferenceRendering.fs`, not `GRContext`; the harness's GL/x11 tokens are evidence
string literals). Only `SkiaViewer.Tests` and `Smoke.Tests` genuinely require GL, and they already
run in the gate's GL step under degrade-and-disclose.

The auditable cadence map (`docs/ci/cadence-map.md` §2) — and its upstream source
`docs/validation/validation-set.md` — had drifted in **both** directions: both still listed the
retired `Color.Tests` / `Input.Tests`, and both omitted `Diagnostics.Tests` (which the gate does run)
plus all six orphans. The map's own "exactly one cadence per member" invariant (FR-009) was broken.

Wiring `Rendering.Harness.Tests` into the gate also surfaced a pre-existing latent failure that had
been invisible precisely because the project never ran: `Feature168 SkillInventoryTests` "repository
parity has no unresolved findings" was **red** (`WarningStatus`) — the `fs-gg-samples` template skill
(`template/fragments/samples/skill/SKILL.md`) satisfied only 3 of the 4 reference groups of the
`package-pin-drift` guidance rule (it never said "local feed"). This is exactly the class of hidden
regression the review predicted.

## Clarifications

None required — the fix is fully specified by the review finding.

## Requirements

- **FR-001** — The gate's deterministic local tier MUST be **derived from `FS.GG.Rendering.slnx`**
  (its `tests/*.Tests` members), not a hardcoded project list, so a newly added test project is in a
  cadence by construction.
- **FR-002** — GL-capability test projects (`SkiaViewer.Tests`, `Smoke.Tests`) MUST be excluded from
  the deterministic tier and continue to run in the gate GL step (degrade-and-disclose). Their names
  MUST come from a single source of truth in `gate.yml` (`GL_TEST_PROJECTS`) shared by both steps.
- **FR-003** — All six orphan projects MUST execute at the gate (they all fall into the derived
  deterministic tier) and MUST pass headless.
- **FR-004** — A **meta-guard** test MUST assert that the gate's test coverage equals the slnx test
  set: `GL_TEST_PROJECTS ⊆ slnx test projects`, the deterministic tier is slnx-derived (not a
  hardcoded name list), and their union is exactly the slnx test set — so the "runs in no cadence"
  class is closed permanently.
- **FR-005** — `docs/ci/cadence-map.md` and `docs/validation/validation-set.md` MUST be refreshed to
  the true membership (drop `Color.Tests`/`Input.Tests`; add `Diagnostics.Tests` and the six
  orphans), and the meta-guard MUST assert both docs stay coherent with the slnx (no retired names;
  every slnx test project present).
- **FR-006** — The `fs-gg-samples` template skill MUST satisfy the `package-pin-drift` guidance rule
  (reference the local feed and the refresh script), and the skill manifest
  (`template/skill-manifest/skill-manifest.json`) MUST be regenerated so its `fs-gg-samples` digest
  matches — restoring `Feature168` repository parity to `Passed` with zero findings.

## Success Criteria

- **SC-001** — The gate deterministic tier runs all 14 non-GL slnx test projects (was 8);
  `Rendering.Harness.Tests` and the other five orphans execute and pass.
- **SC-002** — The new meta-guard (`tests/Build.Tests/CadenceCoverageTests.fs`) is green and fails if
  any future slnx test project is not covered by a cadence, if `GL_TEST_PROJECTS` names a phantom, or
  if either cadence doc drifts.
- **SC-003** — `Feature168 SkillInventoryTests` "repository parity has no unresolved findings" is
  green (`OverallStatus = Passed`, no findings).
- **SC-004** — Full solution builds clean; every gate-lane test project passes headless.

## Out of scope

- Reclassifying any project as GL/x11 (all six orphans are deterministic).
- Wiring `Package.Tests` / template `Product.Tests` into the gate — they are release-only by design
  and excluded from the slnx.
- Rewriting the frozen `specs/005-ci-cadence-wiring` contracts (historical spec artifacts).
