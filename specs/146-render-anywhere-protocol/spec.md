# Feature Specification: Render-Anywhere Scene Protocol

**Feature Branch**: `146-render-anywhere-protocol`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers the next roadmap item from the referenced radical rendering architecture report: P6 Render-anywhere. The feature makes a rendered scene portable enough to be saved, restored, inspected, rendered as a reference image, and evaluated for a browser-capable rendering path.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Exchange a Portable Scene (Priority: P1)

Framework maintainers can export an existing scene into a versioned portable scene package and import it back without losing the scene's visible meaning, resource references, or compatibility diagnostics.

**Why this priority**: The portable scene package is the core contract. Reference rendering and additional backends cannot be trusted until the exchanged scene has deterministic, verifiable meaning.

**Independent Test**: Can be tested by exporting and importing a representative scene corpus, then comparing each restored scene to the original with semantic diagnostics and repeatability checks.

**Acceptance Scenarios**:

1. **Given** a representative scene using core drawing, layers, shaped text evidence, and external resources, **When** it is exported and imported, **Then** the restored scene preserves the same visible structure, resource identities, protocol version, and capability requirements.
2. **Given** the same scene content exported repeatedly under the same protocol version, **When** the exported packages are compared, **Then** they are byte-identical and produce the same scene diagnostics.
3. **Given** a package from a newer protocol version or with unsupported elements, **When** a consumer inspects it, **Then** the consumer receives a clear compatibility diagnostic before rendering is attempted.

---

### User Story 2 - Produce a Reference Rendering Oracle (Priority: P2)

Release reviewers can render a portable scene package through a trusted reference path and receive real image artifacts plus metadata that other rendering paths can be compared against.

**Why this priority**: A render-anywhere effort needs a stable visual oracle. Without a reference artifact, cross-backend claims are unverifiable.

**Independent Test**: Can be tested by rendering a fixed corpus into reference images and validating that each accepted result includes image dimensions, content checksum, protocol version, capability profile, resource manifest status, and a pass/fail verdict.

**Acceptance Scenarios**:

1. **Given** a valid portable scene package and a capable environment, **When** reference rendering runs, **Then** it produces a non-placeholder image artifact and records enough metadata to reproduce or audit the result.
2. **Given** a package with a missing resource, **When** reference rendering runs, **Then** it fails safely with an actionable diagnostic and does not accept a misleading artifact.
3. **Given** an environment that cannot render the reference image, **When** evidence is collected, **Then** the result is recorded as environment-limited rather than passed.

---

### User Story 3 - Decide Browser Rendering Feasibility (Priority: P3)

Product maintainers can evaluate a high-fidelity browser rendering path against the reference oracle and receive either an accepted feasibility result or a documented fallback decision.

**Why this priority**: The report identifies browser rendering as the next expansion target, but the project needs evidence before committing to a full backend.

**Independent Test**: Can be tested by running a feasibility corpus through the candidate browser path, comparing results against reference images, and recording unsupported capabilities or fallback rationale.

**Acceptance Scenarios**:

1. **Given** a portable package from the agreed feasibility corpus, **When** the browser rendering candidate renders it, **Then** the output is compared against the reference oracle with an explicit tolerance and verdict.
2. **Given** the browser candidate cannot support a required capability, **When** feasibility is evaluated, **Then** the report names the missing capability, affected scenes, and the chosen fallback path.
3. **Given** the feasibility run completes, **When** maintainers review the evidence, **Then** they can decide whether to continue with the candidate path without reverse-engineering raw logs.

---

### User Story 4 - Inspect Capabilities and Resources (Priority: P3)

Package consumers can inspect a portable scene package before rendering to learn which capabilities and resources are required, which are optional, and what degradation or rejection will occur on an unsupported target.

**Why this priority**: Cross-backend rendering fails badly when consumers discover missing capabilities only after pixels are corrupted or resources are silently skipped.

**Independent Test**: Can be tested by creating packages with supported, unsupported, missing, and optional resources and verifying the pre-render inspection result for each case.

**Acceptance Scenarios**:

1. **Given** a package that requires image and font resources, **When** a consumer inspects it, **Then** all required resources are listed with stable identities and availability status.
2. **Given** a package that uses an unsupported capability, **When** a consumer inspects it, **Then** the result states whether rendering is rejected or degraded and names the affected capability.

### Edge Cases

- A consumer receives a package from a newer major protocol version.
- A consumer receives a package with unknown but length-skippable data.
- A required resource is unavailable, corrupted, duplicated, or mismatched.
- Two semantically identical scenes are authored with different source ordering that should remain semantically distinct.
- A scene contains shaped text evidence but no text shaping provider is available on the rendering target.
- A scene uses capabilities outside the target's supported profile.
- A browser feasibility run produces close but not exact visual output.
- The reference rendering environment is unavailable or cannot allocate the requested surface size.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST define a portable scene package contract containing protocol version, scene content, capability profile, deterministic ordering, and resource manifest.
- **FR-002**: System MUST import portable scene packages and report whether they are accepted, rejected, or accepted with explicit degradation.
- **FR-003**: System MUST preserve visible scene meaning across export and import for the agreed representative corpus.
- **FR-004**: System MUST produce byte-identical portable packages for equivalent scene content under the same protocol version.
- **FR-005**: System MUST represent external resources through stable resource identities and manifest entries rather than relying on local machine paths.
- **FR-006**: System MUST report missing, unsupported, corrupted, or mismatched resources before accepting a rendering result.
- **FR-007**: System MUST preserve shaped text evidence needed for consistent measurement and drawing across portable scene exchange.
- **FR-008**: System MUST provide a trusted reference rendering path that produces real image artifacts from portable scene packages in capable environments.
- **FR-009**: System MUST record reference rendering evidence with image dimensions, checksum or equivalent identity, protocol version, capability profile, resource status, and verdict.
- **FR-010**: System MUST compare non-reference rendering attempts against the reference oracle with an explicit tolerance and a clear pass/fail result.
- **FR-011**: System MUST evaluate a browser-capable rendering path against the reference oracle and record either an accepted feasibility result or a documented fallback decision.
- **FR-012**: System MUST make unsupported protocol versions, unknown required capabilities, and unsupported target profiles fail safely with actionable diagnostics.
- **FR-013**: System MUST include compatibility and migration guidance for any public contract or observable rendering behavior changed by this feature.
- **FR-014**: System MUST classify this feature as Tier 1 whenever the portable package contract, public surface, dependencies, or observable rendering behavior changes.

### Key Entities

- **Portable Scene Package**: A versioned, deterministic representation of scene content, resource requirements, and capability requirements that can be exchanged outside the original process.
- **Protocol Version**: The compatibility identity for a portable scene package, including rules for accepted, rejected, or degraded consumption.
- **Capability Profile**: The set of scene features a producer requires and a target declares it can render or degrade safely.
- **Resource Manifest**: The resource table for external assets such as images or fonts, including stable identities, required or optional status, and availability diagnostics.
- **Reference Rendering Evidence**: The accepted visual artifact and audit metadata produced by the trusted reference path for a portable scene package.
- **Backend Feasibility Report**: The evidence record comparing a candidate rendering path to the reference oracle and documenting support gaps or fallback decisions.
- **Compatibility Ledger**: The project-facing record of public contract impact, migration guidance, and intentional limitations for the feature.

### Scope and Classification

- This feature is the first P6 Render-anywhere slice from the radical rendering roadmap.
- In scope: portable scene exchange, deterministic round-trip evidence, reference image evidence, resource and capability inspection, browser rendering feasibility, and compatibility guidance.
- The representative corpus MUST include, at minimum, scenes covering core drawing, layers/portals, shaped text evidence, image resources, font resources, missing/corrupted resource negatives, unsupported capability negatives, unsupported version negatives, and at least three browser-feasibility showcase scenes. The exact scene IDs are recorded before story tests begin.
- Out of scope: full production browser hosting, generalized compositor optimization, new layout semantics, and editing or text caret behavior.
- Expected classification: Tier 1, because the feature is expected to introduce a durable scene exchange contract and may affect public surface or observable rendering behavior.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the agreed representative scene corpus round-trips without semantic mismatches.
- **SC-002**: 50 repeated exports of the same representative scene produce byte-identical portable packages.
- **SC-003**: 100% of accepted reference rendering results include a real image artifact, dimensions, output identity, protocol version, capability profile, resource status, and verdict.
- **SC-004**: 100% of missing-resource, unsupported-capability, and unsupported-version negative cases produce actionable diagnostics and no accepted misleading artifact.
- **SC-005**: At least three representative showcase scenes are evaluated through the browser feasibility path against the reference oracle.
- **SC-006**: The browser feasibility report ends with one of two explicit outcomes: accepted candidate path or documented fallback path.
- **SC-007**: Every public contract or observable rendering change introduced by the feature has compatibility notes and migration guidance before implementation readiness is claimed.
- **SC-008**: Release reviewers can determine from the evidence package within 10 minutes whether the feature met its round-trip, reference-rendering, and browser-feasibility goals.

## Assumptions

- P0 through P5 of the referenced radical rendering roadmap are available as the starting point, including stable modifier/layer foundations, retained renderer unification, shaped text evidence, and overlay visual proof.
- The first P6 slice focuses on the exchange contract, reference rendering, and feasibility evidence; it does not need to deliver a production browser backend.
- The representative corpus may reuse existing showcase, harness, and deterministic evidence scenes when they cover the required capabilities and the readiness corpus record names the exact scenes.
- Environment-limited results are acceptable only when clearly labeled and excluded from accepted rendering evidence.
- The planning phase will decide the concrete storage format, target projects, dependency choices, and public contract shape under the repository constitution.
