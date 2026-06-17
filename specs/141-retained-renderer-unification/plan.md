# Implementation Plan: Retained Renderer Unification (Feature 141)

**Branch**: `141-retained-renderer-unification` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/141-retained-renderer-unification/spec.md`

## Summary

Feature 141 starts P3/R1b from the active radical rendering architecture report: finish retained
renderer unification after Feature 139's shared current-node assembly seam and Feature 140's internal
composition foundation. The core outcome is one authoritative assembly producer for current scene
semantics, with retained rendering acting only as a reconciliation, reuse, invalidation, diagnostics,
and cache layer over that producer.

The implementation should preserve public authoring and Scene contracts. The planned work is
internal to `src/Controls`: evolve the existing `ControlInternals.assembleCurrentNode` boundary into
the only place that assembles control-owned scene semantics; make direct rendering, cold retained
rendering, warm retained rendering, and retained emit/replay all consume the same assembly result; and
remove retained-only composition branches that can fabricate independently assembled output. Retained
state may keep identities, previous assembly results, cache metadata, layout state, and reuse evidence,
but it must not own scene composition rules.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`, nullable enabled, warnings-as-errors
including the `.fsi` visibility rule (`FS0078`).

**Primary Dependencies**: Existing in-repo packages: `FS.GG.UI.Controls`, `FS.GG.UI.Scene`,
`FS.GG.UI.Layout`, `FS.GG.UI.DesignSystem`, `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.SkiaViewer`, and
`FS.GG.UI.Testing`. Runtime pins remain unchanged: FSharp.Core `10.1.301`, SkiaSharp
`4.147.0-preview.3.1`, Yoga.Net `3.2.3`, Silk.NET `2.23.0`, and Fable.Elmish `5.0.2`.
No new runtime dependency is planned.

**Storage**: N/A. The feature changes transient retained render state, in-memory caches, diagnostics,
fingerprints, tests, and readiness evidence. No persisted data store or serialization format is added.

**Testing**: Expecto plus existing FsCheck-based property coverage through current test projects. Focused
work belongs primarily in `tests/Controls.Tests`, with compatibility evidence from `tests/Scene.Tests`,
`tests/Layout.Tests`, `tests/Elmish.Tests`, `tests/SkiaViewer.Tests`, `tests/Rendering.Harness.Tests`,
and the offscreen `tests/Rendering.Harness` project when GL/presentation capabilities are available.

**Target Platform**: Linux/dev and CI for deterministic headless tests. Pixel/offscreen validation uses
the existing SkiaViewer/OpenGL harness when available; missing GL/window-system support must be recorded
as a verification limitation, not treated as success.

**Project Type**: F# UI framework/library with declarative Controls, retained rendering, dependency-light
Scene primitives, Yoga-backed layout, Elmish integration, and a SkiaSharp/OpenGL viewer host.

**Performance Goals**: Preserve or improve retained work avoidance for unchanged frames while maintaining
correctness first. Idle warm retained frames should report zero repaint and remeasure work where existing
tests already expect that. Reuse counts may change only with documented rationale; parity, stale-output
prevention, deterministic fingerprints, and correct invalidation are higher priority than a specific hit
count.

**Constraints**:
- Tier 1 architecture feature. It changes renderer ownership boundaries and observable verification
  evidence, even though public authoring and Scene contracts are expected to remain compatible.
- Public `.fsi` surface remains stable unless an explicit compatibility decision, migration guidance,
  surface baseline update, and versioning rationale is added before implementation readiness.
- Visibility lives in `.fsi`; any new cross-file internal module or type must have a paired curated
  `.fsi`.
- Retained rendering must not reintroduce retained-local clipping, overlay, modifier, cache-boundary,
  glyph-run proof, layer, or portal composition rules.
- Feature scope excludes full text shaping, overlay interaction state, portable serialization,
  compositor promotion/damage-scissored presentation, and intrinsic layout protocol work.
- Any mutation remains inside retained render state, cache, and host interpreter boundaries, with short
  comments for hot-path mutable counters where needed.
- Existing cache parity switches (`MemoEnabled`, `PictureCacheEnabled`, `TextCacheEnabled`) and replay
  evidence remain transparent to rendered output.
- Failed or abandoned retained updates must not expose a partially updated frame; work-in-progress
  retained state should commit atomically.

**Scale/Scope**: One architecture slice across `src/Controls/Control.fsi/fs`,
`src/Controls/RetainedRender.fsi/fs`, `src/Controls/Composition.fsi/fs`,
`src/Controls/Reconcile.fsi/fs`, `src/Controls/Types.fsi/fs`, and host-facing retained usage in
`src/Controls.Elmish` as needed. Verification spans existing Controls parity, retained rendering,
cache, layout, scene, SkiaViewer, package surface, golden/pixel, and rendering harness evidence.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted architecture change)**. The feature changes retained
renderer ownership, cache/reuse evidence, diagnostics, and parity guarantees. It requires the full
artifact chain: spec, plan, `.fsi`-first design for any changed cross-file surface, semantic tests,
implementation, surface baseline check, rendering/golden evidence, compatibility notes for intentional
observable changes, and verification limitations.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec -> FSI -> Semantic Tests -> Implementation | PASS | The spec defines P3/R1b scope, Tier 1 risk, public-surface expectation, and verification outcomes. Tasks must update any changed `.fsi` contract before `.fs` bodies and add semantic/parity tests before relying on implementation. |
| II. Visibility lives in `.fsi` | PASS | Existing retained and control-internal contracts already have paired `.fsi` files. New or reshaped assembly/reuse contracts must remain curated in `.fsi`; no top-level visibility keywords in paired `.fs` files are planned. |
| III. Idiomatic simplicity | PASS | The planned design uses records, closed DUs, pure assembly functions, deterministic maps, and straightforward reconciliation. No SRTP, reflection, custom operators, type providers, or non-trivial computation expressions are planned. |
| IV. Elmish/MVU boundary | PASS | Stateful workflow remains in existing retained state and Controls.Elmish host boundaries. No new external I/O workflow is introduced; retained update stays pure except for local cache/counter state inside the interpreter edge. |
| V. Test evidence mandatory | PASS | Focused parity, invalidation, randomized tree, cache-disabled, deterministic fingerprint, public surface, and readiness evidence are required before implementation readiness. |
| VI. Observability and safe failure | PASS | Reuse, invalidation, fresh assembly fallback, duplicate keys, stale state discard, and verification limitations must remain observable through diagnostics/metrics or readiness records. |

**Gate result**: PASS. No unresolved clarification markers remain.

**Post-design re-check**: PASS. Phase 0/1 artifacts keep public authoring contracts compatible by default,
keep later text/interaction/protocol/compositor/layout work out of scope, and define retained reuse as a
consumer of the single assembly owner rather than a second builder.

## Project Structure

### Documentation (this feature)

```text
specs/141-retained-renderer-unification/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── retained-renderer-unification.md
└── tasks.md                 # Created by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
src/Controls/
├── Control.fsi / Control.fs
│   └── `ControlInternals.assembleCurrentNode`, `paintNode`, `evaluateLayout`,
│       `renderTree`, bounds/event/diagnostic collection
├── RetainedRender.fsi / RetainedRender.fs
│   └── retained identities, previous assembly results, reconciliation, cache
│       parity switches, fingerprints, metrics, atomic frame commit
├── Composition.fsi / Composition.fs
│   └── Feature 140 modifier/layer/portal classification and legacy lowering
├── Reconcile.fsi / Reconcile.fs
│   └── keyed diff and child compatibility decisions
└── Types.fsi / Types.fs
    └── `ControlRenderResult` compatibility surface

src/Controls.Elmish/
└── ControlsElmish.fsi / ControlsElmish.fs
    └── retained rendering host route and runtime state integration, if touched

tests/Controls.Tests/
├── Feature141RetainedRendererUnificationTests.fs       # planned focused feature tests
├── Feature139AssemblyExtractionTests.fs                # shared assembly seam guard
├── Feature140*Tests.fs                                 # composition foundation compatibility
├── Feature091RetainedRenderTests.fs / Feature092...    # retained identity and parity history
├── Audit_MemoCache.fs / Audit_PictureCache.fs / Audit_TextCache.fs
├── Audit_Fingerprint.fs / Audit_Reconcile.fs
└── PublicSurfaceTests.fs

tests/Layout.Tests/
└── incremental layout equivalence and dirty-set compatibility

tests/Scene.Tests/ and tests/SkiaViewer.Tests/
└── Scene fingerprint, glyph-run proof, replay/cache, and renderer compatibility

tests/Rendering.Harness/
└── offscreen/pixel evidence for compatibility and readiness when available
```

**Structure Decision**: Single F# solution. Keep the feature inside the Controls retained rendering and
assembly boundary. `ControlInternals.assembleCurrentNode` or a successor internal assembly result is the
only scene-semantics owner. `RetainedRender` keeps reconciliation and reuse state but receives or stores
assembly results; it must not branch on composition semantics except through shared `Composition` evidence.
No new project or runtime dependency is needed.

## Phase 0: Research Summary

See [research.md](./research.md). Decisions are resolved and no clarification markers remain.

## Phase 1: Design Summary

See [data-model.md](./data-model.md), [contracts/retained-renderer-unification.md](./contracts/retained-renderer-unification.md),
and [quickstart.md](./quickstart.md). The contract is internal framework behavior plus public compatibility
guarantees for `Control.renderTree`, retained rendering, diagnostics, fingerprints, cache parity, and public
surface preservation.

## Complexity Tracking

No constitution violations require justification.
