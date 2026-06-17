# Implementation Plan: Shared Assembly Extraction (Feature 139)

**Branch**: `139-shared-assembly-extraction` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/139-shared-assembly-extraction/spec.md`

## Summary

Feature 139 is P1 from the active radical rendering architecture report: reduce the current renderer's
highest-risk duplication by making today's scene composition semantics flow through one internal assembly
rule set. The visible behavior must not change. Immediate rendering, retained initialization, retained
updates, and retained cache/replay emit walks should agree because they use the same current-semantics node
assembly rule, not because several hand-written paths happen to stay synchronized.

The technical approach is deliberately conservative. Keep the current public scene shape, overlay behavior,
container clipping behavior, cache boundaries, diagnostics, and metrics. Add an internal current-node assembly
boundary that accepts a node's own paint, evaluated box, and already-assembled child results, then returns
the same in-flow and overlay contributions that today's full and retained paths build independently. Wire
`Control.renderTree`, retained first-frame build, retained fresh/carry/update rebuilds, and the retained
cache/replay emit walk through that boundary. Do not introduce modifier algebra, portals, public IR changes,
or the later retained-renderer unification in this feature.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`.

**Primary Dependencies**: Existing in-repo packages only: `FS.GG.UI.Controls`, `FS.GG.UI.Scene`,
`FS.GG.UI.Layout`, `FS.GG.UI.DesignSystem`, and `FS.GG.UI.Themes.Default` in tests. No new runtime
dependency is planned.

**Storage**: N/A. The feature changes transient render assembly only; no persisted state or external data
store is introduced.

**Testing**: Expecto/FsCheck through existing test projects. Focused validation is in
`tests/Controls.Tests/Controls.Tests.fsproj`; broader compatibility uses the existing Controls, Layout,
SkiaViewer, Rendering.Harness, and package-surface gates.

**Target Platform**: Linux/dev and CI. The planned proofs are deterministic and headless; no live GL/window
context is required for focused Controls parity.

**Project Type**: F# UI framework/library with a retained rendering runtime and deterministic headless
verification.

**Performance Goals**: No user-facing performance target in this refactor. The required performance
constraint is non-regression in work-reduction metrics and no extra full-tree rendering pass, verified through
focused Feature 139 assertions plus existing work-reduction oracle suites. The measurable architecture outcome
is one authoritative current-semantics assembly boundary used by all existing assembly paths.

**Constraints**:
- Tier 1 signature-impacting internal refactor: `.fsi` implementation contracts may change, but public
  authoring, scene, package, and wire contracts must not drift.
- Visibility remains `.fsi`-owned. Any new internal module or internal contract must have a curated `.fsi`;
  public surface baselines must report no intentional changes.
- Existing full-versus-retained, cache-on-versus-cache-off, and incremental-layout parity oracles must remain
  valid.
- Existing golden or pixel evidence must require no intentional rebaseline.
- Keep R2+ semantics out of scope: no modifier algebra, portal model, public IR changes, intrinsic layout
  protocol, HarfBuzz shaping, compositor changes, or portable scene protocol.
- Keep implementation plain: records/tuples/functions over current scene lists are preferred over new class
  hierarchies, dynamic dispatch, reflection, or broad dependency moves.

**Scale/Scope**: One internal vertical slice across `src/Controls/Control.fsi`, `src/Controls/Control.fs`,
`src/Controls/RetainedRender.fsi`, `src/Controls/RetainedRender.fs`, `src/Controls/Controls.fsproj` only if
an additional internal file is justified, and focused tests under `tests/Controls.Tests`.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (signature-impacting internal refactor)**. The feature refactors internal
renderer assembly ownership and intentionally updates curated `.fsi` implementation contracts for the shared
assembly seam. It has no intended public authoring, scene, package, wire, or observable behavior change. It
requires the full artifact chain, `.fsi`-first contract edits, failing semantic/parity tests before routing,
surface-area checks, compatibility evidence, and documentation/evidence updates. Public package surface
baselines must report zero intentional drift.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec -> FSI -> Semantic Tests -> Implementation | PASS | The spec defines scope, compatibility, and verification first. Implementation tasks must update internal `.fsi` shape before `.fs`, add failing parity tests, then refactor bodies. |
| II. Visibility lives in `.fsi` | PASS | Existing public surface remains unchanged. Internal exposure for tests or RetainedRender stays in `Control.fsi`/`RetainedRender.fsi` or a new paired internal `.fsi` if a file split is used. |
| III. Idiomatic simplicity | PASS | The design uses a small pure assembly function over current data shapes. No custom operators, reflection, SRTP, type providers, or new computation expressions are planned. |
| IV. Elmish/MVU boundary | PASS | This feature introduces no new stateful workflow or I/O. Existing Elmish host metrics continue to consume retained render output unchanged. |
| V. Test evidence mandatory | PASS | Required tests cover immediate/retained parity, clipping, offsets, overlays, cache boundaries, empty content, warm retained reuse, and cache-disabled parity. |
| VI. Observability and safe failure | PASS | No operational path is added. Existing diagnostics and failure behavior must remain semantically unchanged for equivalent inputs. |

**Gate result**: PASS. No unresolved clarifications and no constitution violations.

**Post-design re-check**: PASS. Phase 0/1 artifacts add no dependency, storage, public API, or semantic
change. The design narrows implementation to a current-semantics internal boundary and documents later-phase
exclusions.

## Project Structure

### Documentation (this feature)

```text
specs/139-shared-assembly-extraction/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── assembly-compatibility.md
├── checklists/
│   └── requirements.md
└── tasks.md                 # Created by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
src/Controls/
├── Control.fsi / Control.fs               # internal current-node assembly boundary and renderTree caller
├── RetainedRender.fsi / RetainedRender.fs # retained build/carry/update/emit callers
└── Controls.fsproj                        # compile-order update only if a new internal file is justified

tests/Controls.Tests/
├── Feature139AssemblyExtractionTests.fs   # focused current-assembly and parity coverage
├── Feature137ClippingTests.fs             # existing clipping/overlay regression coverage remains green
├── Feature091RetainedRenderTests.fs       # retained render full-build parity remains green
├── Feature092RetainedRenderTests.fs       # chained retained render parity remains green
└── Audit_PictureCache.fs                  # cache-enabled/cache-disabled scene parity remains green

tests/Layout.Tests/
└── Audit_IncrementalLayout.fs             # unchanged, run as compatibility guard

tests/Package.Tests/
└── SurfaceAreaTests.fs                    # confirms no public surface drift
```

**Structure Decision**: Single F# solution. The first implementation should prefer a `ControlInternals`
current-node assembly helper because `Control.renderTree` is defined in `Control.fs` before `RetainedRender`
and most current paint helpers already live there. A new `src/Controls/Assemble.fsi/fs` file is acceptable
only if it preserves compile order without moving large unrelated paint logic or widening public surface.
Either shape must leave one authoritative current-semantics assembly rule used by immediate and retained
paths.

## Phase 0: Research Summary

See [research.md](./research.md). Decisions are resolved and no clarification markers remain.

## Phase 1: Design Summary

See [data-model.md](./data-model.md), [contracts/assembly-compatibility.md](./contracts/assembly-compatibility.md),
and [quickstart.md](./quickstart.md). There is no new external interface; the contract artifact documents
the internal compatibility behavior that implementation and tests must preserve.

## Complexity Tracking

No constitution violations require justification.
