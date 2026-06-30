# Feature Specification: Headless Image Evidence Path

**Feature Branch**: `221-headless-image-evidence`

**Created**: 2026-06-30

**Status**: Draft

**Change Classification**: **Tier 1 (contracted change)** — this feature alters the observable behavior of the public `SceneEvidence.renderPng` surface (it begins returning real PNG bytes instead of a hash stub) and adds a new injectable rasterizer seam to the `Scene` public API. Per the constitution's Change Classification, it therefore requires the full artifact chain: spec, plan, `.fsi` updates, surface-area baseline updates, test evidence, and documentation updates. **Public API impact**: new Scene seam (e.g. `setRealPngRasterizer`); changed `renderPng` output semantics; the existing `Hash`/metadata/evidence-file surfaces are preserved unchanged (FR-007). This is a Tier 1 change **within the Rendering repo only** — it does **not** change any cross-repo contract in the dependency registry (no `contract-change`).

**Input**: User description: "start the next Rendering owned item on the coordination board." → Coordination board (FS-GG Projects v2 #1) next Rendering-owned item: **FS-GG/FS.GG.Rendering#32 — no headless image-evidence path (live window + offscreen PNG both need GL)** (parent epic FS-GG/.github#74, §5.1).

## Context *(why this exists)*

A consumer agent drove the TestSpec tutorial to green but could obtain **no pixel proof of the live game** in a headless / virtual-display environment:

- The live viewer presents frames directly to the swapchain (GPU present, no GPU→CPU readback), so X11 capture tools read the window region as solid black.
- The on-demand offscreen image routine also requires GL, and the deterministic `renderPng` evidence routine returns a tiny non-image stub (the bytes of a structural hash string) when no renderer is available — it is not a decodable image.

Net effect: on a renderer-centric platform whose CI evidence should include screenshots, there is **no supported way to produce a real image of what the product draws** without a GPU and a display. This feature closes that gap.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic image evidence in a headless environment (Priority: P1)

An agent or CI job running in a bare container (no GPU, no X server, no virtual display) renders a scene description to a PNG file and gets back a real, decodable image whose pixels show what the scene draws — shapes, colors, and text. Running it again on a different machine produces the same image, so it can be committed or diffed as evidence.

**Why this priority**: This is the core gap in the issue. Without it, the platform cannot produce image evidence in the very environment (headless CI) where evidence is most needed. It is the minimum viable slice — delivered alone, it lets every downstream consumer capture pixel proof headlessly.

**Independent Test**: In a container with no GPU/display, request a PNG for a representative game scene at a fixed size; assert the output decodes as a PNG of the requested dimensions, contains non-blank pixel content, and is byte-for-byte identical to a second run.

**Acceptance Scenarios**:

1. **Given** a headless environment with no GPU and no display, **When** a scene is rendered to PNG, **Then** the result is a valid PNG of the requested width and height containing the scene's drawn content (not a stub, placeholder, or hash payload).
2. **Given** the same scene and output size, **When** the PNG is rendered twice (same machine or different machines), **Then** the two images are identical.
3. **Given** a scene rendered headlessly and the same scene rendered on a GPU-backed host, **When** both images are compared, **Then** both depict the same scene content (exact GPU/CPU pixel parity is not required).

---

### User Story 2 - Pixel proof of the live game window (Priority: P2)

An agent wants a screenshot of the live running game in an environment where the GL window cannot be captured by external tools. Following one documented, supported path, the agent obtains an image of the current live frame (or an offscreen render equivalent to it) without guesswork or decompiling.

**Why this priority**: The issue asks for *either* a headless rasterizer *or* a documented capture path for the live window. P1 delivers deterministic scene evidence; this story covers proof of the *actual live frame* and makes the supported route explicit so future agents don't rediscover it the hard way.

**Independent Test**: Follow the documented capture path against a running viewer in a virtual-display environment and confirm a non-black image of the current frame is produced, with zero undocumented steps.

**Acceptance Scenarios**:

1. **Given** a live viewer running in a virtual-display environment, **When** the documented capture path is followed, **Then** a non-black image of the current frame is produced.
2. **Given** the documentation, **When** an agent reads it, **Then** the supported capture path is described end to end (no step requires inspecting compiled binaries or trial and error).

---

### User Story 3 - Honest failure instead of silent stubs (Priority: P3)

When image evidence genuinely cannot be produced, the consumer receives a clear, typed diagnostic that names the blocked stage and classifies it as an environment limitation versus a product defect — rather than a success-shaped artifact that is actually an undersized non-image.

**Why this priority**: The original failure was hard to diagnose because a stub was returned as if it were evidence. Eliminating success-shaped failures protects the trust of every downstream evidence check, but it is only meaningful once a real path (P1) exists.

**Independent Test**: Force an unproducible request (e.g., an unsupported renderer mode) and assert a typed failure is returned with a classification and message, and that no image-shaped artifact is written.

**Acceptance Scenarios**:

1. **Given** a request that cannot produce an image, **When** evidence is rendered, **Then** a typed failure is returned with a stage, classification (unsupported-environment vs product-defect), and human-readable message.
2. **Given** any evidence request, **When** it does not fully succeed, **Then** no artifact smaller than a valid image is emitted as a "success".

---

### Edge Cases

- **Zero or negative output size** → rejected as a product defect with a clear message (existing behavior preserved).
- **Very large output size** → either succeeds within bounded memory/time or fails with a clear resource diagnostic; it must never emit a stub.
- **Scene contains content the headless rasterizer cannot reproduce faithfully** (e.g., GPU-only effects) → the image is still produced with a documented, deterministic degradation, and the degradation is disclosed rather than silently dropped.
- **Fonts/text headless** → text renders deterministically using the product's bundled font resources; a missing face is disclosed, not silently substituted in a way that breaks determinism.
- **Concurrent renders** → independent requests do not interfere and each remains deterministic.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST produce a valid, decodable PNG image from a scene description in an environment with no GPU, no OpenGL context, and no display server.
- **FR-002**: The headless image MUST contain the actual rendered visual content of the scene (geometry, color, and text), not a placeholder, stub, or text/hash payload masquerading as an image.
- **FR-003**: Rendering the same scene at the same output size MUST yield identical image output across repeated runs and across machines (deterministic), so the artifact is usable as committed/diffed CI evidence.
- **FR-004**: The headless path MUST NOT require a GPU, an OpenGL context, an X server, or a virtual display, and MUST succeed inside a bare CI container.
- **FR-005**: When image evidence cannot be produced, the system MUST return a typed failure that names the blocked stage and classifies it as unsupported-environment or product-defect, and MUST NOT emit a success-shaped artifact that is not a valid image.
- **FR-006**: The system MUST provide a documented, supported way to obtain pixel evidence of the live viewer frame (direct window capture or an offscreen render equivalent to it) for environments where the live GPU window cannot be captured by external tools.
- **FR-007**: Existing scene-evidence consumers and formats (structural hash, metadata, evidence-file writing) MUST continue to work unchanged; this feature adds a real-pixel path without breaking those surfaces.
- **FR-008**: The headless rasterizer MAY trade speed for portability (slow is acceptable), but MUST complete a single representative scene within a CI-acceptable bounded time (the concrete bound is **SC-004**: under 5 s on a standard CI runner).
- **FR-009**: The documentation that previously stated headless image evidence was unobtainable (the runtime-limitations note for §5.1) MUST be updated to describe the new supported path.

### Key Entities

- **Scene evidence request**: the scene to render, the desired output size, and the requested evidence format (image, structural hash, or metadata).
- **Image evidence artifact**: a deterministic PNG of the requested dimensions whose pixels depict the scene; the unit of "pixel proof".
- **Evidence failure**: a typed outcome carrying the blocked stage, a classification (unsupported-environment vs product-defect), and a diagnostic message.

## Success Criteria *(mandatory)*

### Measurable Outcomes

> The "representative game scene" referenced below is a single, concretely pinned fixture (fixed scene constructor + fixed output size) defined in the test scaffolding (tasks.md T006) so these criteria rest on a reproducible input rather than an ad-hoc scene.

- **SC-001**: In a headless container (no GPU, no X server), rendering a representative game scene yields a PNG that decodes to the requested dimensions with non-blank, non-stub pixel content in 100% of runs.
- **SC-002**: The same scene rendered twice produces identical image bytes, verified across at least two distinct runs/machines.
- **SC-003**: A consumer agent can obtain pixel proof of the game in a headless/virtual-display environment by following documentation alone — zero steps require decompiling binaries or guesswork.
- **SC-004**: Image evidence for a representative scene is produced within a CI-acceptable time bound (target: under 5 seconds) on a standard CI runner.
- **SC-005**: No evidence artifact smaller than a valid image is ever emitted as a "success"; the prior small-stub failure mode is eliminated and covered by a regression check.

## Assumptions

- The primary fix is a software (CPU) rasterizer that emits real pixels for the offscreen/deterministic image-evidence routine; documenting a live-GL-window capture path (User Story 2) is the complementary secondary route. This matches the issue's "even slow" framing and the platform's renderer-centric evidence needs.
- Visual-fidelity target is the scene model the live viewer renders. Exact pixel parity between the CPU headless image and the live GPU window is **not** required; a deterministic CPU rasterization of the same scene is sufficient as evidence.
- Determinism depends on the product's bundled, deterministic font resources; headless text rendering reuses those bundled faces rather than host system fonts.
- "Headless" means no GPU, no OpenGL context, and no display server (the CI/container case from the consumer report); a virtual display may exist for the live-window capture path but is not required for the deterministic offscreen path.
- This work is scoped to the Rendering repo's evidence/viewer surfaces; it does not change cross-repo contracts in the dependency registry (no `contract-change`).
