# Quickstart / Validation Runbook: Control.fs Decomposition

**Feature**: 189-control-module-split | **Date**: 2026-06-22

Validation-only runbook. No implementation code here — see [data-model.md](./data-model.md) and
[contracts/module-topology.md](./contracts/module-topology.md). Run from repo root; GL suites need X11
(`DISPLAY=:1`).

## Prerequisites

- .NET `net10.0` SDK; X11 display at `:1` for GL suites.
- Solution: `FS.GG.Rendering.slnx`. Baselines: `readiness/surface-baselines/`.

## 0. Pre-refactor baseline capture (FR-014 — BEFORE any production edit)

```bash
# Test red/green baseline
DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release \
  --out specs/189-control-module-split/readiness/baseline/test-baseline.md
# Public surface snapshot
dotnet fsi scripts/refresh-surface-baselines.fsx
cp readiness/surface-baselines/FS.GG.UI.Controls.txt \
   specs/189-control-module-split/readiness/baseline/FS.GG.UI.Controls.pre.txt
# Reference scene-hashes / fingerprints / faithful-content + inspection artifacts:
DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release \
  --filter "Fingerprint|RetainedRender|Inspection|KindRegistry|Layout|Rendering" \
  > specs/189-control-module-split/readiness/baseline/controls-corpus.log
```
**Expected**: a captured red/green set (per 188, expect a small known pre-existing red set — record it),
a `FS.GG.UI.Controls.pre.txt` snapshot, and a corpus log of hashes/artifacts to diff each story against.

## Foundational — compile probe (research D5; before US1)

Stand up the proposed `Controls.fsproj` topology with **empty/stub** new modules in the C1 order and
confirm the whole solution compiles (no back-edge). This fixes `ContentRender`↔`NodeAssembly` order and
the painter-field-vs-separate-map shape **empirically**.
```bash
dotnet build FS.GG.Rendering.slnx -c Release   # MUST succeed with stub modules in place
```
**Expected**: clean build → the ordering hypothesis (plan standing-assumption) is confirmed before any
body moves.

## US1 — `ChartGeometry`/`WidgetGeometry` + `withPoints`

Move the ~40 `*Geom` (and `emptyState`/`pillGeom`) into the two groupings; factor the ~17 empty-points
guards into `withPoints`.
```bash
dotnet build FS.GG.Rendering.slnx -c Release
DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release \
  --filter "Rendering|TextShaping|Chart|Fingerprint"
```
**Expected (SC-002)**: build clean; every chart/widget `Scene list` **byte-identical** to the corpus
log; empty-point charts produce the identical `emptyState` scene; `Control.fs` shrinks by the relocated
geometry; surface baseline unchanged.

## US2 — `SceneHash`/`LayoutEval`/`NodeAssembly`

Recast `hashScene` as `SceneHasher`; move layout evaluators + assembly functions.
```bash
DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release \
  --filter "Fingerprint|RetainedRender|Layout|Rendering"
```
**Expected (SC-004)**: `hashScene` byte-identical for the corpus (incl. `hashScene []` canary);
`evaluateLayout` bounds byte-identical (INV-1); `renderScene`/`paintNode` scenes equivalent. Any
legitimate hash reorder → record in `readiness/golden-hash-review.md` with picture-cache proof
(SC-008); otherwise no expected-output edits.

## US3 — registry painter + 6 `match …Kind` sites

Add `Painter` to `ControlKindEntry` (table bound in `ContentRender`); route `faithfulContent` + the 6
switches through the registry; extend the oracle.
```bash
DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release \
  --filter "KindRegistry|Catalog|Inspection|Rendering|Fingerprint"
```
**Expected (SC-003/SC-007)**: every catalog kind renders the same faithful geometry through the painter
as through the old `match`; the 6 former sites resolve via one table read each; the completeness oracle
**fails loudly** if a catalog kind lacks a painter (verify by temporarily deleting one entry — it must
go red — then restore). Non-catalog runtime kind hits the pre-refactor default.

## US4 — `Control.Helpers` (conditional, FR-008 / D6)

Collapse tail-module bodies; measure line delta.
```bash
git diff --stat src/Controls/Control.fs    # MUST show a net reduction; else DROP US4
DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release \
  --filter "PublicSurface|TypedControlContract"
```
**Expected**: every public `create`/`text` yields an identical `Control` value; surface baseline empty
diff for the tail modules; **net reduction** — if not, revert US4, US1–US3 stand alone.

## Final gates (all stories)

```bash
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release          # full suite
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff readiness/surface-baselines/FS.GG.UI.Controls.txt      # target: EMPTY
```
**Expected (SC-005/SC-006)**: full red/green set matches the captured baseline except explicitly
reviewed golden-hash expected-output updates; surface diff empty ⇒ **no version bump**; if non-empty,
review + bump `FS.GG.UI.Controls` per the gate. Each new file ≤ ~1,500 lines (SC-001).
