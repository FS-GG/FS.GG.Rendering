# Feature Specification: Replaceable Game Starter Scene

**Feature Branch**: `220-game-starter-scene`

**Created**: 2026-06-30

**Status**: Draft

**Input**: User description: "start the next Rendering owned item on the coordination board." → resolved to Coordination board item **FS-GG/FS.GG.Rendering#31** (P1 Rendering, Composition, contract `fs-gg-ui-template`): *"Game-template default is a controls demo and the governance test pins it."* Consumer-agent feedback from the TestSpec tutorial run (parent epic FS-GG/.github#74).

## Context (why this item, in plain terms)

A developer who scaffolds a **game / rendering** product from the FS.GG.UI template expects the starting point to be a small, runnable game they can replace with their own. Today the default scaffold instead ships a **controls showcase** (form, chart, DataGrid), and a generated governance test **hard-asserts** that the default launch keeps that controls showcase. So when a developer follows the tutorial's instruction to "replace the starter scene with your own game," doing it at the normal entrypoint **fails a governance test**. In the tutorial run the author had to hide the real game behind an extra launch flag (`-- pong`) just to keep tests green. Separately, the starter is more tightly coupled than the scaffold map promises, so swapping it is a large, risky edit rather than the documented few-file change.

This feature makes the game/rendering starter genuinely replaceable as the *default* path, and aligns the governance spine and the scaffold-map promise with that reality.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The default game starter is mine to replace (Priority: P1)

A developer scaffolds a game/rendering product. The default product launches a **minimal, runnable game-style scene** that is clearly meant to be replaced. The developer replaces it with their own game (e.g. Pong) **at the default entrypoint** — no hidden or alternate launch flag — and the full generated test suite (build + product tests, including the durable governance tests) passes **without editing any governance test**.

**Why this priority**: This is the core promise of a game-oriented scaffold and the exact failure the consumer hit. Without it, the template's headline use case ("build your game") fights the governance gate on day one.

**Independent Test**: Scaffold the game/rendering default, replace the starter `model`/`view` with a minimal Pong, run the generated build + tests, and confirm a green run with zero edits to governance tests and no extra launch flag.

**Acceptance Scenarios**:

1. **Given** a freshly scaffolded game/rendering product, **When** the developer runs it with no special flags, **Then** the default launch renders a minimal game-style interactive scene (not a controls showcase the developer must keep).
2. **Given** that scaffolded product, **When** the developer replaces the starter scene with their own game at the default entrypoint, **Then** the generated build and product tests pass with no edits to any governance test.
3. **Given** the developer's replacement game, **When** they run the product normally, **Then** their game is what launches — no `-- pong`-style flag is required to surface the real game.

---

### User Story 2 - Swapping the starter is a small, bounded change (Priority: P2)

When the developer replaces the starter scene, the edit is confined to a small, clearly labeled set of **developer-owned files** (the model/view seam). The **durable plumbing** (evidence commands, layout evidence, window options, program entry, governance tests) keeps compiling and passing — either untouched or with only the documented "re-point your model's fields" edits — exactly as the scaffold map promises.

**Why this priority**: The consumer found the real coupling far larger than the scaffold map advertises, forcing a parallel module instead of a clean swap. The few-file promise is what makes the scaffold trustworthy.

**Independent Test**: Perform the documented starter swap and confirm the changed-file set matches the scaffold map's "replaceable" + documented "re-point" list, with no undocumented files forced to change.

**Acceptance Scenarios**:

1. **Given** the scaffold map's replaceable/durable classification, **When** the developer swaps the starter scene, **Then** only the files the map labels replaceable (plus any it labels "re-point model fields") need to change.
2. **Given** the durable plumbing files, **When** the starter model is swapped, **Then** they continue to compile and their source-scan governance assertions stay green.
3. **Given** the scaffold map, **When** read against the actual swap, **Then** its replaceable-vs-durable description matches the real edit set (no undocumented coupling).

---

### User Story 3 - The controls showcase stays available, just not forced (Priority: P3)

The controls showcase keeps its demo value: it remains a **discoverable, explicit option** for developers who want a controls-first starting point, while no longer being the forced default for the game/rendering use case. Existing non-interactive profiles (headless-scene, governed, sample-pack) are unaffected.

**Why this priority**: The controls showcase is genuinely useful as a demo; removing it would be a regression. The fix should re-aim the default, not delete capability.

**Independent Test**: Scaffold with the explicit controls option and confirm the controls showcase still generates and passes its governance tests; scaffold the other profiles and confirm unchanged output.

**Acceptance Scenarios**:

1. **Given** a developer who wants the controls showcase, **When** they choose it explicitly, **Then** they get the controls family starter and its governance tests pass.
2. **Given** the headless-scene, governed, and sample-pack profiles, **When** scaffolded, **Then** their generated output and tests are unchanged by this feature.

---

### Edge Cases

- A developer who keeps the default game starter unchanged still gets a fully green build + test run (the default is a valid product, not a broken placeholder).
- A developer who replaces the starter with a non-game UI (e.g. an invoice grid) can still follow the scaffold map's "re-point regions" guidance and pass governance — the spine is UI-family agnostic.
- The governance test's existing `//#else` "game family" branch (currently unreachable by any profile) becomes a real, exercised path — it must not assert anything the default game starter cannot satisfy.
- The contract `fs-gg-ui-template` is consumed downstream (SDD scaffold-provider, Templates governance); the change to the default starter must be sequenced so downstream consumers are not silently broken.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Scaffolding the game/rendering provider's default MUST produce a runnable starter whose normal (no-flag) launch renders a minimal game-style interactive scene that is explicitly designated as the developer's to replace — not a controls showcase the developer is expected to retain.
- **FR-002**: A developer MUST be able to replace the starter scene with their own game **at the default entrypoint** and have every generated governance test pass **without modifying any governance test** and **without** introducing an alternate launch flag to surface the real game.
- **FR-003**: The generated governance tests MUST validate the durable evidence / structure / discoverability spine in a way that is agnostic to the developer's chosen UI family; they MUST NOT require the product to retain a specific starter UI family's launch call.
- **FR-004**: Replacing the starter scene MUST be confined to the developer-owned seam (the starter `model`/`view`); the durable plumbing files MUST continue to compile and pass their scans either untouched or with only the changes the scaffold map documents as "re-point your model's fields."
- **FR-005**: The generated scaffold map / guidance MUST accurately describe which files are replaceable versus durable, consistent with the actual coupling — there MUST be no undocumented coupling that forces edits beyond the documented seam.
- **FR-006**: The controls showcase MUST remain available as an explicit, discoverable option so its demo value is preserved, without being the forced default for the game/rendering use case.
- **FR-007**: This feature MUST NOT change the generated output or tests of the headless-scene, governed, or sample-pack profiles; scope is bounded to the interactive game/app starter path.
- **FR-008**: The previously required workaround of hiding the real game behind an extra launch flag (e.g. `-- pong`) MUST no longer be necessary to keep the generated governance tests green.
- **FR-009**: The change to the `fs-gg-ui-template` contract MUST be coordinated with downstream consumers (the SDD scaffold-provider and Templates governance expectations) so the new default starter does not silently break the composition path; cross-repo impact MUST be surfaced and tracked per the coordination protocol.

### Key Entities

- **Game starter seam**: the minimal, developer-owned `model` + `view` (and a tick/subscription appropriate to a game) that the developer replaces with their own game. The unit "replace the starter scene" acts on.
- **Durable governance spine**: the model-agnostic plumbing (evidence commands, layout evidence, window options, program entry) plus `GovernanceTests.fs`, which read product source text and assert structural/evidence invariants that must survive a starter swap.
- **Product profile / family**: the scaffold choice that selects which starter is generated (game/rendering default vs. the explicit controls showcase vs. headless-scene / governed / sample-pack).
- **Scaffold map**: the durable-vs-replaceable contract document the developer reads before designing; it must match the actual edit set required to swap the starter.
- **`fs-gg-ui-template` contract**: the versioned cross-repo template surface consumed by SDD/Templates; the default-starter change is a change to this contract.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can replace the default game starter with their own game and reach a green generated build + product-test run **without editing any governance test** — verified end to end on a representative replacement (e.g. Pong).
- **SC-002**: The default game/rendering scaffold launches the developer's scene at the normal entrypoint with **0** hidden-flag workarounds required to surface the real game.
- **SC-003**: Swapping the starter scene changes only the files the scaffold map classifies as replaceable or "re-point" — **0** undocumented files are forced to change.
- **SC-004**: **100%** of generated governance tests pass for both (a) the unmodified default game starter and (b) a representative replaced game, with no governance-test edits in either case.
- **SC-005**: The scaffold map's replaceable/durable classification matches the actual swap edit set (verifiable by performing the documented swap and confirming only documented files changed).
- **SC-006**: The controls showcase and the headless-scene / governed / sample-pack profiles remain available and their generated output/tests are unchanged by this feature (**0** regressions).

## Assumptions

- The game/rendering provider's default starter should be a **minimal game skeleton** (a small `model`/`msg`/`update`/`view` plus a tick/subscription), not the controls showcase. Whether this is delivered via a new explicit "game" profile/family or by re-aiming the existing default while keeping controls as an explicit option is a design choice deferred to planning; either satisfies the requirements as long as controls stays reachable (FR-006) and other profiles are untouched (FR-007).
- "Replace the starter scene with Pong" (from the consumer tutorial) is the canonical acceptance journey for SC-001 / SC-004.
- The durable governance spine's model-agnostic intent (source-text scans, compile-order, evidence vocabulary) is retained; only the assertion that pins a specific UI family's launch call is relaxed/retargeted so the contemplated "game family" branch becomes a real, satisfiable path.
- The governance test in scope is the one shipped in this repo at `template/base/tests/Product.Tests/GovernanceTests.fs`; the scaffold map in scope is `template/base/docs/scaffold-map.md`.
- Back-compat: developers who want today's controls showcase keep an explicit, documented path to it; the change does not delete that capability.
- This touches the `fs-gg-ui-template` contract, so the resolution includes republishing the template at a coherent version and coordinating downstream (SDD scaffold-provider, Templates governance) per the cross-repo protocol; the deeper cross-repo sequencing is tracked on the Coordination board alongside sibling item #32.
