# Feature Specification: Fix the generated build.fsx governance-engine resolution

**Feature Branch**: `202-fix-build-fsx-engine`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "fix the build.fsx"

## Context (why this exists)

Every product scaffolded from the `fs-gg-ui` template ships a `build.fsx` whose `Verify` target runs
two governance gates — **EvidenceGraph** and **EvidenceAudit** — in-process by loading a published
governance engine (`FS.GG.UI.Build`) and invoking its generated-evidence façade by reflection. Today
that path is broken end-to-end:

1. **Stale rebrand path** — the script probes the NuGet cache folder `fs.skia.ui.build` (the
   pre-rebrand identity) instead of `fs.gg.ui.build`, so it could never find the engine even if it
   were present.
2. **No engine anywhere** — `FS.GG.UI.Build` has no producer project in any known repository, is on
   no configured feed, and is in no cache. The runtime restore therefore fails and `Verify` aborts on
   the evidence gates for every profile that includes them.

The consequence: `dotnet fsi build.fsx target Verify` cannot complete in any generated product. This
feature makes that gate **work** — the evidence/audit gates resolve a real engine and run green —
while keeping the rest of the template's governance wiring intact.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A scaffolded product's Verify gate runs the governance evidence gates green (Priority: P1) 🎯 MVP

A developer scaffolds a product from the template, restores it, and runs the project's full
verification gate. The EvidenceGraph and EvidenceAudit governance checks actually execute and pass;
the gate exits successfully.

**Why this priority**: This is the whole point of the feature. Without it, the template's headline
"governed product" promise is unmet — the generated `Verify` gate fails on first use.

**Independent Test**: In a freshly generated product (a profile that includes the gate), run the
verification gate after restore; confirm the evidence and audit steps run (produce their evidence
output, not a skipped/log-only stub) and the gate exits 0.

**Acceptance Scenarios**:

1. **Given** a freshly scaffolded product whose version pin resolves on a configured feed, **When**
   the developer runs the full verification gate, **Then** EvidenceGraph and EvidenceAudit each
   execute against the resolved engine and the gate completes successfully.
2. **Given** the same product, **When** the verification gate completes, **Then** the evidence and
   audit artifacts the gates produce exist and the run is not a completion-only log stub.
3. **Given** a profile that does not include the evidence gates, **When** the developer runs its
   verification gate, **Then** it completes successfully without requiring the engine.

---

### User Story 2 - The engine resolves correctly and stays in lock-step with the single version pin (Priority: P2)

A developer (or maintainer) upgrades the product by editing the single FS.GG.UI version value and
restoring. The governance engine moves with the libraries — there is no second version literal and no
superseded/pre-rebrand identifier anywhere in the generated build script.

**Why this priority**: The template's core invariant is "one version edit upgrades everything." A
correct engine binding must honour it, and the stale rebrand identifier must be gone so resolution
can ever succeed.

**Independent Test**: Inspect a generated product's build script and version configuration; confirm
the engine is resolved from the single version source, that no `fs.skia.ui`/pre-rebrand engine
identifier or cache path remains, and that changing the single version value moves both libraries and
the gate engine.

**Acceptance Scenarios**:

1. **Given** a generated product, **When** its build script resolves the engine, **Then** it uses the
   single version-of-truth value and a current (post-rebrand) engine identity.
2. **Given** a generated product, **When** the single version value is changed and the product is
   restored, **Then** the engine resolved by the gate matches that new value.
3. **Given** a generated product, **When** its build script is searched for engine identifiers,
   **Then** no superseded/pre-rebrand engine package name or cache path is present.

---

### User Story 3 - Honest, diagnosable failure when the engine is genuinely unavailable (Priority: P3)

When the engine cannot be resolved (e.g., the pinned version is not yet on any configured feed, or an
offline environment), the gate fails with a clear message naming the engine and where it was sought —
never a silent pass, and never mislabelled as a defect in the developer's own product.

**Why this priority**: A working gate must also fail honestly; this protects the evidence-integrity
guarantee and keeps the failure self-explanatory.

**Independent Test**: Point a generated product at a version/feed where the engine is absent; run the
verification gate; confirm it fails with a message naming the engine identity and the feed/location,
and that it does not report success.

**Acceptance Scenarios**:

1. **Given** a generated product whose engine version is not on any configured feed, **When** the
   verification gate runs, **Then** it fails with a message that names the engine and the feed/path it
   was sought on.
2. **Given** that same failure, **When** the developer reads the message, **Then** it is clear the
   missing engine is a framework/feed condition, not a defect in their generated product.

---

### Edge Cases

- Engine available only on a local development feed (in-repo work) vs. a public feed (published
  consumer) — both paths must resolve.
- Pinned engine version not yet published → clear, named failure (US3), not a silent pass.
- Profiles that do not include the evidence gates must keep passing `Verify` without the engine.
- Offline / restore-blocked environments → honest diagnostic, no fabricated success.
- The engine's transitive dependencies must also resolve so the in-process invocation succeeds.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A generated product's verification gate MUST execute the EvidenceGraph and EvidenceAudit
  governance gates against a real, resolved engine and exit successfully, for every profile that
  includes those gates.
- **FR-002**: The generated build script MUST resolve the governance engine using the single FS.GG.UI
  version source of truth and the current (post-rebrand) engine identity — no superseded/pre-rebrand
  package name or cache path may remain.
- **FR-003**: The governance engine that provides the gates MUST be obtainable from a configured feed
  (published or producible) so a freshly scaffolded product can restore and run it without manual,
  out-of-band setup.
- **FR-004**: The engine version MUST stay in lock-step with the single FS.GG.UI version value — one
  edit plus restore moves the libraries and the gate engine together; no second version literal is
  introduced.
- **FR-005**: When the engine cannot be resolved, the gate MUST fail with a clear diagnostic naming
  the engine identity and the feed/location searched; it MUST NOT report success and MUST NOT present
  the condition as a defect in the developer's generated product.
- **FR-006**: The gates MUST continue to run in-process (no shelled Python or audit scripts
  reintroduced); the only retained external process invoked by the build remains `dotnet test`.
- **FR-007**: The generated product's existing governance tests that assert the build wiring
  (in-process engine invocation, single-version resolution, no engine reference version literal, clean
  text logs, no decommissioned scripts) MUST remain green.
- **FR-008**: The remaining verification steps (Dev, GeneratedGuidanceCheck, TemplateDrift, Test) and
  all four template profiles MUST continue to generate, restore, build, and pass as they do today.

### Key Entities *(include if feature involves data)*

- **Generated build script**: the per-product `build.fsx` that orchestrates the verification gate and
  binds the governance engine at runtime.
- **Governance engine**: the published component that provides the EvidenceGraph/EvidenceAudit
  generated-evidence entrypoint the build invokes.
- **Single version pin**: the one FS.GG.UI version value that governs both the libraries and the gate
  engine.
- **Verification gate**: the composite `Verify` target (Dev + GeneratedGuidanceCheck + TemplateDrift +
  EvidenceGraph + EvidenceAudit + Test).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a freshly scaffolded product, the verification gate completes successfully with the
  evidence and audit gates actually executed (not skipped/log-only) for 100% of profiles that include
  them.
- **SC-002**: A developer can run the verification gate end-to-end after restore with zero extra
  manual engine-setup steps.
- **SC-003**: Exactly one FS.GG.UI/engine version value governs both libraries and the gate engine; a
  single-value upgrade + restore is sufficient (no second literal).
- **SC-004**: Zero superseded/pre-rebrand engine **package names** (`FS.Skia.UI`) or **cache paths**
  (`fs.skia.ui.build`) remain in the generated build script. (Scope note: this targets the engine's
  package identity and NuGet cache folder only; the single-pin MSBuild property `FsSkiaUiVersion` is a
  deliberately retained internal property name — FR-007 requires it to stay present — and is explicitly
  out of scope here.)
- **SC-005**: 100% of engine-unavailable runs fail with a message that names the engine and the
  feed/location, diagnosable without reading the build script source.

## Assumptions

- "Fix the build.fsx" means **make `Verify` fully pass** against a real, resolvable engine (operator
  decision for this feature), not merely correct the stale path or degrade gracefully.
- The work centres on the generated template's build wiring (`template/base/build.fsx` and how the
  engine is sourced/pinned). The governance engine's internal rules are out of scope except for the
  entrypoint/contract the build calls and the requirement that the engine be obtainable.
- Validation follows the established template method: generate each profile, restore against a
  configured feed, and run the gate — the generated product, not `template/base` in place, is the unit
  of verification.
- A coherent local feed (and the single-version pin practice) from feature 201 is the working
  baseline; this feature does not re-litigate the version-pin model.

## Dependencies

- **A governance engine exposing the generated EvidenceGraph/EvidenceAudit entrypoint must exist or be
  produced/published.** No `FS.GG.UI.Build` package currently has a producer in any known repository.
  Candidate resolutions (to be decided in `/speckit-plan` + `/speckit-clarify`, as an implementation
  choice the spec deliberately leaves open):
  - Re-point the build to the standalone **FS.GG.Governance** engine (which already publishes, e.g.,
    `FS.GG.Governance.Cli` to the local feed and carries an Evidence/Kernel surface), adapting the
    build's invocation contract to it; or
  - (Re)establish a producer for the `FS.GG.UI.Build` engine package and publish it to the feed.
- Whichever engine is chosen, it (and its transitive dependency closure) must be restorable from a
  configured feed at the pinned version.
