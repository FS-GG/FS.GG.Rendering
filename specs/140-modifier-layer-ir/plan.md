# Implementation Plan: Modifier Layer IR Foundation (Feature 140)

**Branch**: `140-modifier-layer-ir` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/140-modifier-layer-ir/spec.md`

## Summary

Feature 140 starts P2 from the active radical rendering architecture report: prove a modifier/layer
composition foundation after feature 139's shared current-node assembly seam, without jumping straight to
the later retained-renderer unification or broad public protocol work. The feature introduces one internal
model for ordered visual modifiers, effect classification, local z-order, out-of-tree portal layers, legacy
lowering, and deterministic glyph-run proof data. It must preserve existing rendering behavior unless an
intentional compatibility entry, migration note, surface baseline, and pixel disclosure ledger says otherwise.

The technical approach is internal-first. Add a small composition IR behind the existing Controls render
assembly boundary, fold modifier chains in documented order, normalize identity/equivalent effects, and lower
legacy clip/translate/perspective/cache/text/overlay forms into that model before producing today's final
`Scene`. Portal/layer ordering replaces the hardcoded overlay split semantically, but the public
`ControlRenderResult` can remain compatible by deriving paint order and hit order from one internal ordering
evidence stream. Modifier/layer/portal nodes are not public in this feature. The planned Scene-level surface
delta is limited to the glyph-run spike: add the smallest stable public glyph-run data shape plus a drawable
proof node/constructor needed for deterministic measurement, drawing, diagnostics, and fingerprinting evidence.

## Implementation Status

**Status on 2026-06-17**: implemented and validated, with wrapper/package-gate limitations recorded in
[readiness.md](./readiness.md) and [verification-limitations.md](./verification-limitations.md).

Completed scope:

- Added internal `FS.GG.UI.Controls.Composition` for value-based modifiers, invalidation classification,
  normalization, deterministic fingerprints, local z-order, layer hosts, portals, diagnostics, legacy lowering,
  and compatibility evidence.
- Routed current-node assembly through composition normalization and chain application while keeping public
  Controls surface compatible.
- Shared modifier invalidation classification with retained rendering evidence through
  `RetainedRender.classifyModifierEffect`.
- Added the public Scene glyph-run proof data/node/constructor surface and SkiaViewer proof drawing/helper path.
- Added focused Feature 140 tests across Controls, Scene, and SkiaViewer.
- Refreshed public surface baselines; only `FS.GG.UI.Scene` glyph-run type/node additions were emitted.
- Ran focused, legacy oracle, broad deterministic, GL-local, offscreen harness, and surface-refresh validation.

Known limits:

- `./fake.sh` wrapper targets are unavailable in this checkout.
- `tests/Package.Tests` surface filter is stale against the current baseline locations.
- Glyph-run proof is deterministic proof data only; full shaping remains deferred to later text work.
- R1b retained unification, overlay interaction state, portable serialization, compositor work, and intrinsic
  layout remain deferred.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion=latest`, warnings-as-errors including the
constitution-owned `.fsi` visibility rule.

**Primary Dependencies**: Existing in-repo packages: `FS.GG.UI.Scene`, `FS.GG.UI.Controls`,
`FS.GG.UI.Layout`, `FS.GG.UI.DesignSystem`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Controls.Elmish`, and existing
test harnesses. No new runtime dependency is planned for modifier/layer semantics. The glyph-run spike must
use existing bundled-font/SkiaSharp text machinery; HarfBuzz and full shaping remain out of scope.

**Storage**: N/A. The feature changes transient render assembly, diagnostics, fingerprints, and evidence
artifacts only. No persisted state or external data store is introduced.

**Testing**: Expecto/FsCheck through existing test projects. Focused validation starts in
`tests/Controls.Tests/Controls.Tests.fsproj` for modifier chains, normalization, invalidation classification,
local z-order, portal layers, legacy lowering, cache parity, and hit/paint ordering. Scene glyph-run proof
coverage belongs in `tests/Scene.Tests` and `tests/SkiaViewer.Tests` if a Scene node or Skia drawing path is
introduced. Broad compatibility uses existing Controls, Layout, Elmish, SkiaViewer, Rendering.Harness, and
package-surface gates.

**Target Platform**: Linux/dev and CI. Focused modifier/layer and glyph-run shape tests are deterministic and
headless. Pixel evidence uses the existing SkiaViewer/offscreen rendering harness when a GL/window-system
surface is available; unavailable GL must be recorded as an environment limitation, not silently skipped.

**Project Type**: F# UI framework/library with a dependency-light Scene package, declarative Controls runtime,
retained rendering path, OpenGL-backed Skia viewer, and deterministic headless verification.

**Performance Goals**: No new wall-clock benchmark target. Required performance constraints are structural:
normalization must not increase rendered work for equivalent chains, paint-only/order-only changes must not
produce false layout invalidation, cache-enabled versus cache-disabled parity must remain valid, and
full-versus-retained compatibility scenes must stay equivalent. Work-reduction metrics should continue to
report bounded recomputation for localized paint/order changes.

**Constraints**:
- Tier 1 architecture feature. Public surface may change only through curated `.fsi` edits, semantic tests,
  surface-baseline evidence, compatibility impact, migration guidance, and versioning recommendation.
- Internal-first rollout: modifier/layer/portal semantics should be proven behind feature 139's assembly seam
  before exposing broad public Scene IR churn. Avoid putting layout containers into public `SceneNode`.
- Scene remains dependency-light: no references from `src/Scene` to Controls, Layout, Elmish, SkiaViewer,
  Silk.NET, Yoga.Net, or YamlDotNet.
- SkiaViewer remains the interpreter edge. Any new drawable Scene node requires an exhaustive
  `SceneRenderer.paintNode` case and image evidence, not only structural `Scene.describe` checks.
- Controls owns authoring/lowering, local z-order, portal layer ordering, hit-test ordering, and compatibility
  with existing overlay behavior.
- Visibility lives in `.fsi`; any new module with public or internal cross-file surface needs a paired curated
  `.fsi`.
- Full text shaping, bidi, font fallback expansion, line breaking, portable serialization, compositor
  promotion, overlay interaction state, retained-renderer unification, and intrinsic layout are out of scope.

**Scale/Scope**: One architecture slice across `src/Controls/Control.fsi/fs`,
`src/Controls/RetainedRender.fsi/fs`, a new paired `src/Controls/Composition.fsi/fs`,
`src/Scene/Scene.fsi/fs` for the glyph-run data/proof surface only, `src/SkiaViewer/SceneRenderer.fs` and
`src/SkiaViewer/Fonts.fs` for glyph-run drawing proof, plus focused tests under `tests/Controls.Tests`,
`tests/Scene.Tests`, `tests/SkiaViewer.Tests`, and evidence from `tests/Rendering.Harness`.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted architecture change)**. This feature can alter observable
rendering semantics and may add public Scene glyph-run data or Scene node surface. It therefore requires the
full artifact chain: spec, plan, `.fsi`-first design for any surface, failing semantic tests, implementation,
surface baseline evidence, rendering/golden evidence, compatibility plan, migration guidance, and disclosure
ledger for intentional pixel or public-surface changes.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec -> FSI -> Semantic Tests -> Implementation | PASS | The spec names Tier 1 scope, public-surface risk, and verification. Implementation tasks must sketch any public/internal cross-file surface in `.fsi`, add failing semantic/parity tests, then implement. |
| II. Visibility lives in `.fsi` | PASS | New cross-file composition, glyph-run, or renderer contracts must be declared in paired `.fsi` files. No top-level `.fs` visibility keywords are planned. |
| III. Idiomatic simplicity | PASS | The planned foundation uses closed DUs/records and pure folds over modifier lists. No SRTP, reflection, custom operators, type providers, or non-trivial computation expressions are planned. |
| IV. Elmish/MVU boundary | PASS | No new stateful or I/O workflow is introduced. Existing Controls/Elmish runtime state is consumed only for retained and hit-test compatibility evidence. |
| V. Test evidence mandatory | PASS | Focused tests must cover supported modifier categories, at least 12 normalization cases, portal/layer paint and hit order, legacy lowering, cache parity, retained parity, glyph-run proof cases, and surface/golden evidence. |
| VI. Observability and safe failure | PASS | Missing portal targets, missing anchor evidence, unsupported glyph-run proof inputs, and verification limitations must produce actionable diagnostics or explicit evidence records. |

**Gate result**: PASS. No unresolved clarification markers remain. Public surface risk is accepted as
Tier 1 and fenced by `.fsi`/surface-baseline/migration evidence.

**Post-design re-check**: PASS. Phase 0/1 artifacts keep later R1b/R4/R5/R6/R7 work out of scope, preserve the
Scene package boundary, and define compatibility obligations for any public change.

## Project Structure

### Documentation (this feature)

```text
specs/140-modifier-layer-ir/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── modifier-layer-foundation.md
└── tasks.md                 # Created by /speckit-tasks, not by /speckit-plan
```

### Source Code (repository root)

```text
src/Controls/
├── Control.fsi / Control.fs               # feature 139 assembly seam; legacy lowering; renderTree and hitTest
├── RetainedRender.fsi / RetainedRender.fs # retained fragments, fingerprints, cache parity, retained hit-test
├── Types.fsi / Types.fs                   # ControlRenderResult compatibility guard, no planned result-shape delta
└── Composition.fsi / Composition.fs       # internal modifier/layer/portal model

src/Scene/
└── Scene.fsi / Scene.fs                   # glyph-run proof data/node surface only; no public modifier/layer nodes

src/SkiaViewer/
├── SceneRenderer.fs                       # exhaustive painter case for the glyph-run proof node
└── Fonts.fs                               # bundled-font glyph-run proof, not full shaping

tests/Controls.Tests/
├── Feature140ModifierLayerTests.fs        # modifier ordering, classification, normalization, z-order, portals
├── Feature139AssemblyExtractionTests.fs   # shared assembly seam remains the composition entry
├── Feature137ClippingTests.fs             # legacy clipping/overlay compatibility guard
└── Audit_* / Feature09x-12x suites        # cache, retained, visual-state, and metrics compatibility guards

tests/Scene.Tests/
└── Feature140GlyphRunTests.fs             # glyph-run data, diagnostics, measurement, fingerprint proof

tests/SkiaViewer.Tests/
└── Feature140GlyphRunRenderingTests.fs    # draw proof for the glyph-run proof node

tests/Rendering.Harness/
└── offscreen evidence for intentional pixel changes and portal/layer scenarios
```

**Structure Decision**: Single F# solution. The modifier/layer/portal foundation should sit behind
`ControlInternals.assembleCurrentNode` in a new paired internal `Controls.Composition` module consumed by
`Control` and `RetainedRender`. Keep public `ControlRenderResult` compatible and derive final paint order and
hit-test priority from one internal ordering stream. Do not add public Scene modifier/layer/portal nodes in
this feature. The intentional public Scene delta is the glyph-run proof surface; it must keep legacy text
constructors working and ship with migration/versioning evidence.

## Phase 0: Research Summary

See [research.md](./research.md). Decisions are resolved and no clarification markers remain.

## Phase 1: Design Summary

See [data-model.md](./data-model.md), [contracts/modifier-layer-foundation.md](./contracts/modifier-layer-foundation.md),
and [quickstart.md](./quickstart.md). The contract is primarily internal for modifiers/layers/portals and
public for the glyph-run Scene proof surface.

## Complexity Tracking

No constitution violations require justification.
