# Feature Specification: Refresh fs-gg-ui Template to Current Scene API

**Feature Branch**: `201-refresh-template-scene-api`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "Refresh the fs-gg-ui template so template/base/src/Product/*.fs matches the current Scene API (and re-pin FsSkiaUiVersion) — a FS.GG.Rendering maintenance task."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generated product compiles and runs against the current engine (Priority: P1)

A developer scaffolds a new product from the `fs-gg-ui` template. The seed product code (`template/base/src/Product/*.fs`) and its package pin (`FsSkiaUiVersion`) reference the FS.GG.UI Scene API as it exists today, so the freshly generated product restores, builds, and produces its scene/evidence output without the developer having to first fix compile errors caused by API drift.

**Why this priority**: The template's entire purpose is to give downstream products a working starting point. If the seed code targets a stale Scene API or a stale package version, every new product is broken on first build — the template fails at its one job. This is the core of the maintenance task.

**Independent Test**: Scaffold a product from the template at each supported profile, restore against the current local package feed, and build. The build succeeds with no Scene-API-related compile errors and the product emits its expected scene/evidence.

**Acceptance Scenarios**:

1. **Given** the template at the `headless-scene`/`governed` profile, **When** a product is generated and built against the re-pinned `FsSkiaUiVersion`, **Then** `Product/*.fs` compiles with no errors or warnings caused by Scene API mismatch and the scene renders to evidence.
2. **Given** the template at the `app`/`sample-pack` profile, **When** a product is generated and built, **Then** the viewer/controls/scene code compiles against the current API and the product launches its scene.
3. **Given** the re-pinned `FsSkiaUiVersion`, **When** `dotnet restore` runs against the local feed, **Then** every `FS.GG.UI.*` package resolves to a single, consistent, currently-published version with no missing-package or version-conflict errors.

---

### User Story 2 - Bundled API-surface docs and seed code agree with the engine (Priority: P2)

A developer relies on the documentation the template ships (`template/base/docs/api-surface/`) and on the seed `Product/*.fs` as a worked example of the Scene API. These reference materials reflect the current public Scene surface, so the example a developer copies from is the API they actually have.

**Why this priority**: Stale bundled signatures and example code mislead developers into writing code against constructors/shapes that no longer match the engine, reintroducing drift in every downstream product. Correct once, here, rather than in every generated product.

**Independent Test**: Compare the template's bundled Scene API-surface signature against the current public Scene surface and confirm they agree; confirm the constructs used in `Product/*.fs` exist in the current surface.

**Acceptance Scenarios**:

1. **Given** the current public Scene API, **When** the bundled `api-surface/Scene` signature is compared against it, **Then** the bundled copy matches the current public surface (no removed/renamed types or members presented as current).
2. **Given** the seed `Product/*.fs`, **When** each Scene type/constructor it uses is checked against the current surface, **Then** every referenced construct exists with the shape the code assumes.

---

### User Story 3 - The refresh is verifiable and the template's own checks stay green (Priority: P3)

A maintainer can confirm the refresh is complete and correct by running the template's existing validation (governance tests, generated-product build/evidence checks) and seeing them pass against the re-pinned version, with no remaining references to the previous pinned version.

**Why this priority**: Without a green verification pass the refresh is unproven; this guards against a partial bump (e.g. one package or doc left on the old version) silently shipping.

**Independent Test**: Run the template's governance/generated-product checks after the refresh; all pass and no artifact still references the superseded `FsSkiaUiVersion`.

**Acceptance Scenarios**:

1. **Given** the refreshed template, **When** the governance tests run, **Then** they pass, including the single-source-version invariant (`FsSkiaUiVersion` remains the only FS.GG.UI version literal).
2. **Given** the refreshed template, **When** the repository is searched for the previously pinned version literal, **Then** no stale occurrence remains in template package pins, docs, or seed code.

---

### Edge Cases

- **A Scene construct used by the seed code was removed or renamed** in the current API: the seed `Product/*.fs` and any bundled example must be rewritten to the current equivalent, not left referencing the removed construct.
- **A profile-specific code path** (`app`, `sample-pack`, `governed`, `headless-scene`) drifts independently: each profile's compiled output must be validated, not only the default.
- **Version literal appears outside package pins** (e.g. `UPGRADING.md` examples, README, doc snippets): re-pinning must cover every place the version literal is presented as the current pin, while leaving illustrative/historical references that are intentionally not the pin untouched.
- **The current published version equals the existing pin**: the task still verifies seed-code/API agreement; the version bump may be a no-op while the code refresh is not.
- **Multi-line/profile-guarded source** (`//#if` regions): refreshing one branch of a guarded region must not break the other branch.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The seed product source under `template/base/src/Product/*.fs` MUST compile against the current public FS.GG.UI Scene API for every supported template profile, with no errors or warnings attributable to Scene API drift.
- **FR-002**: Every Scene type, constructor, and member referenced by the seed product code MUST exist in the current public Scene surface with the shape the code assumes; references to removed or renamed constructs MUST be updated to their current equivalents.
- **FR-003**: `FsSkiaUiVersion` in the template's `Directory.Packages.props` MUST be re-pinned to the current published FS.GG.UI version available from the repository's local package feed.
- **FR-004**: `FsSkiaUiVersion` MUST remain the single source of FS.GG.UI version truth — all `FS.GG.UI.*` package pins continue to reference `$(FsSkiaUiVersion)` and no second FS.GG.UI version literal is introduced.
- **FR-005**: The bundled Scene API-surface reference shipped by the template (`template/base/docs/api-surface/Scene`) MUST be brought into agreement with the current public Scene surface.
- **FR-006**: No template artifact (package pins, seed code, docs, or version-statement examples that represent the current pin) MAY continue to reference the superseded `FsSkiaUiVersion` value after the refresh.
- **FR-007**: A product generated from the refreshed template MUST restore against the local feed and produce its expected scene/evidence output for each supported profile.
- **FR-008**: The template's existing validation (governance tests and generated-product build/evidence checks) MUST pass against the refreshed template.
- **FR-009**: The refresh MUST preserve existing template behaviour and structure beyond what the API/version change requires — no new product features, no profile changes, no unrelated refactors.

### Key Entities *(include if data involved)*

- **fs-gg-ui template**: The product-scaffolding template under `template/`; `template/base` holds the seed product, package pins, bundled docs, and validation.
- **Seed product source**: `template/base/src/Product/*.fs` (`Model.fs`, `View.fs`, `LayoutEvidence.fs`, `EvidenceCommands.fs`, `Program.fs`, profile-guarded `WindowOptions.fs`) — the worked example exercising the Scene API.
- **`FsSkiaUiVersion` pin**: The single version property in `template/base/Directory.Packages.props` that every `FS.GG.UI.*` package reference resolves to.
- **Bundled API surface**: `template/base/docs/api-surface/` signature snapshots shipped to downstream developers as the API reference.
- **Public Scene API**: The current FS.GG.UI Scene public surface (`src/Scene/*.fsi`) the template must target.
- **Template profiles**: `headless-scene`, `governed`, `app`, `sample-pack` — independently-guarded build configurations that must each be validated.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A product generated from the refreshed template builds successfully for 100% of supported profiles with zero Scene-API-related compile errors or warnings.
- **SC-002**: 100% of Scene constructs referenced by the seed product code exist in the current public Scene surface.
- **SC-003**: Exactly one FS.GG.UI version literal (`FsSkiaUiVersion`) exists in the template, and it equals the current published feed version; zero occurrences of the superseded version literal remain in template pins, seed code, or current-pin documentation.
- **SC-004**: The bundled Scene API-surface reference matches the current public Scene surface (zero presented-as-current constructs that are absent from the live surface).
- **SC-005**: 100% of the template's existing governance and generated-product validation checks pass against the refreshed template.
- **SC-006**: A generated product emits its expected scene/evidence output for every supported profile (no runtime failure attributable to the refresh).

## Assumptions

- "Current Scene API" means the public Scene surface defined by `src/Scene/*.fsi` in this repository at refresh time (`Types.fsi`, `Scene.fsi`, `Evidence.fsi`, `Inspection.fsi`, `TextShaping.fsi`, `SceneCodec.fsi`, `Animation.fsi`).
- "Re-pin `FsSkiaUiVersion`" means setting the pin to the latest FS.GG.UI version produced by the repository's normal local-feed packing/merge process, not to an arbitrary or hand-picked value.
- The refresh is scoped to API/version conformance and its direct fallout; it is explicitly not an opportunity to add product features, change supported profiles, or perform unrelated refactors (FR-009).
- Profile guards (`//#if`) in the seed source remain the mechanism for profile variation; refreshing applies within each guarded branch.
- Version literals that are intentionally illustrative or historical (e.g. an example value in upgrade documentation that is not the live pin) are out of scope unless they are presented as the current pin.
- The template's existing governance tests and generated-product checks are the authoritative definition of "the template still works" and are sufficient to verify the refresh.
