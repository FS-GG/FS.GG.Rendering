# Phase 0 Research: Controls Gallery Showcase

All NEEDS CLARIFICATION items from the Technical Context are resolved below. Each
entry records the **Decision**, **Rationale**, and **Alternatives considered**.

---

## R1 — How the gallery consumes the framework (package vs project reference)

**Decision**: Consume the framework **only as packed `FS.GG.UI.*` NuGet packages**
(`0.1.0-preview.1`) from `~/.local/share/nuget-local/` via a local `nuget.config` in
`samples/ControlsGallery/`. No `ProjectReference` into `src/`.

**Rationale**: FR-013 and SC-005 require the gallery to build/run against the public
package surface with no privileged internal access — that is the whole point of the
sample (it proves the documented consumption path). All required packages are already
present in the local feed (`FS.GG.UI.Controls`, `.Controls.Elmish`, `.SkiaViewer`,
`.Testing`, `.Color`). The constitution pins pack output to `~/.local/share/nuget-local/`.

**Alternatives considered**:
- *Project references for dev velocity* (floated in plan §10.3) — rejected: it would
  grant internal visibility and bypass the public surface, violating FR-013/SC-005.
- *Template-generated sample* (`dotnet new fs-gg-ui`) — deferred: useful as a second
  consumer-path proof but not required for G1; the hand-authored gallery already
  exercises the package surface. Can be added later without reworking this design.

---

## R2 — Interactive windowed mode wiring

**Decision**: Wire the gallery through
`FS.GG.UI.Controls.Elmish.ControlsElmish.runInteractiveApp` using an
`InteractiveAppHost<Model,Msg>` whose `Init`/`Update` come from the pure `Core.Model`
and whose `View: Size -> Model -> Control<Msg>` is `Core.Shell.view`. Theme is set
from the model via `Theming.toTheme (Theming.resolve mode accent)`.

**Rationale**: `runInteractiveApp` is the documented public entry for a Controls-based
MVU window; `InteractiveAppHost` is the exact bridge type (size-aware view, pointer +
keyboard seams, per-frame metrics sink). It keeps `update` pure and confines I/O to the
viewer interpreter — satisfying Principle IV.

**Alternatives considered**:
- `Viewer.runApp` with a hand-built scene — rejected: lower-level, bypasses the
  Controls/Elmish integration the gallery is meant to demonstrate.
- `runInteractiveAppWithWindowBehavior` — available if a fixed startup size/position
  is wanted; default `runInteractiveApp` is sufficient for the MVP.

---

## R3 — Headless deterministic evidence mode

**Decision**: For each page, the headless run:
1. Drives state via **`ControlsElmish.Perf.runScript host size script`** with a
   per-page seeded `FrameInput<Msg> list` (keys, pointers, ticks with **injected**
   `TimeSpan` deltas) → deterministic `FrameMetrics list` = the **state outcome**.
2. Captures the screenshot via **`Viewer.captureScreenshotEvidence`** (with
   `ViewerPresentMode.OffscreenReadback`) → `frame.png` of the required surfaces.
3. Writes a **Page Evidence Record** (`run.json` + `summary.md` + `frame.png` +
   `state.txt`) including a non-empty `notAuthoritativeFor` disclosure.

Determinism comes from: golden `FrameMetrics` count/bool fields only (the wall-clock
`*Duration` fields are excluded from the record), no randomness, and seeded input with
injected time. Byte-identity is verified the same way the existing harness does it —
run twice with the same seed and compare bytes.

**Rationale**: `Perf.runScript` is the framework's pure, headless frame driver and its
count metrics are already golden-asserted as deterministic; pairing it with
`captureScreenshotEvidence` mirrors the proven Tier-0/Tier-1 harness pattern
(`tests/Rendering.Harness`) without depending on that internal test project.

**Alternatives considered**:
- `Viewer.runForFrames` alone — rejected: produces a frame but not the per-frame state
  metrics needed for the "state outcome" half of the evidence record.
- Reusing `tests/Rendering.Harness/Evidence.fs` directly — rejected: it is an internal
  test project, not a public package; the gallery re-implements the *same schema* in
  its own `App/Evidence.fs` (a tiny, disclosed copy), keeping the consumer-only rule.

---

## R4 — Degrade-and-disclose when no display/GL

**Decision**: Before attempting interactive mode, query
`Viewer.runtimeCapability()`. In headless evidence mode, rely on
`ScreenshotEvidenceResult.{ProvesScreenshot, BlockedStage, UnsupportedHostReason,
Fallback, Diagnostics}`. When GL/display is unavailable: still produce the
deterministic **state** evidence (which needs no GL), mark the screenshot portion
`ProvesScreenshot=false` with the disclosed reason, write the record, and **exit 0**
(non-hang). Interactive mode on a no-GL host prints the disclosed reason and exits
without launching.

**Rationale**: FR-011/FR-016/SC-004 require a clean, disclosed skip/fallback and that
the CI gate never depend on a display. The Testing package already surfaces the exact
disclosure fields; `runtimeCapability` distinguishes a missing window-system from a
real defect (Principle VI).

**Alternatives considered**:
- Failing the run when GL is absent — rejected: makes CI depend on a display
  (violates FR-016).
- Silently skipping with no record — rejected: violates the no-overclaim disclosure
  rule (FR-010).

---

## R5 — Theme and accent ("Indigo & Teal on Slate")

**Decision**: Use the built-in **Light/Dark** themes via `Theming.resolve mode accent`
→ `Theming.toTheme`. The neutral base is the shipped **slate** ramp
(`FS.GG.UI.Color.Palettes`). Because the Color package ships only the `slate` ramp,
the gallery **defines its accent `Color` values as literals** in `Core.GalleryTheme`
— an **Indigo** primary accent and a **Teal** secondary accent — and exposes an accent
selector that switches between them (and any future additions). This is legitimate:
the gallery is a *consumer* and may define its own palette constants.

**Rationale**: FR-005/FR-014 require Light/Dark + accent using only what exists today,
with no Ant/Fluent/Material dependence. `Theming.resolve` is the public accent seam;
the slate ramp gives the cohesive neutral; indigo/teal are simple consumer-owned
constants. The "Indigo & Teal on Slate" name comes from the archived showcase spec
(adopted per FR-015).

**Alternatives considered**:
- Waiting for shipped indigo/teal ramps — rejected: unnecessary coupling to future
  Color-package work; a consumer defining accent constants is the documented path
  (`Theme.withAccent` / `Theming.resolve` both take a `Color`).

---

## R6 — Mapping 52 controls onto exactly 10 pages

**Decision**: Group by the §10.1 families. The 11 catalog categories distribute across
10 pages so each control lands on exactly one page (full table in
[data-model.md](./data-model.md) and [contracts/page-registry.md](./contracts/page-registry.md)):

| # | Page (family) | Controls | Count |
|---|---------------|----------|-------|
| 1 | Display / Typography | text-block, rich-text, label, image, icon, separator, badge | 7 |
| 2 | Buttons | button, icon-button, toggle-button, split-button | 4 |
| 3 | Text & Numeric Input | text-box, text-area, numeric-input, slider, date-picker, time-picker | 6 |
| 4 | Selection & Toggles | check-box, radio-group, switch, list-box, multi-select-list, combo-box, color-picker | 7 |
| 5 | Data & Collections | list-view, tree-view, data-grid | 3 |
| 6 | Layout & Containers | stack, grid, dock, wrap, border, panel, scroll-viewer, split-view | 8 |
| 7 | Navigation & Menus | tabs, menu, context-menu, toolbar | 4 |
| 8 | Overlays & Feedback | tooltip, dialog, overlay, toast, progress-bar, spinner, validation-message | 7 |
| 9 | Charts | line-chart, bar-chart, pie-chart, scatter-plot | 4 |
| 10 | Pointer Playground / Custom | graph-view, custom-control | 2 |

**Total = 52, exactly 10 pages, every control once.** The mapping is the single source
of truth for the coverage check (FR-003).

**Rationale**: Matches the §10.1 family list exactly while honoring the catalog's real
category counts (Input 10 splits across pages 2+3 by button-vs-field semantics; Overlay
3 + Feedback 4 combine on page 8; Graph 1 + Custom 1 form the pointer-playground page).

**Alternatives considered**:
- Strict one-category-per-page — rejected: there are 11 categories but only 10 pages,
  and Input (10) is too large for one page; family grouping is the spec's own intent.

---

## R7 — Coverage check placement (FR-003)

**Decision**: Implement the coverage check **once** in `Core.CoverageMap` as a pure
function over `Catalog.supportedControls` and the page registry, surfaced **two ways**:
(a) an Expecto test (`CoverageTests`) that fails the build on drift, and (b) a
`coverage-check` CLI subcommand that exits non-zero on drift for CI/scripts.

**Rationale**: FR-003 + SC-001 demand an automated check that fails on any unreferenced
or duplicated control. Centralizing the logic and exposing it via both a test and a CLI
keeps a single source of truth while serving both `dotnet test` and shell pipelines.

**Alternatives considered**:
- Test-only — rejected: the spec frames the gallery as runnable; a CLI self-check is
  cheap and useful for the headless CI path.
- CLI-only — rejected: loses the fast `dotnet test` signal and Expecto reporting.

---

## R8 — Sample placement relative to the solution and test tier

**Decision**: The `samples/ControlsGallery/` tree is **not** added to
`FS.GG.Rendering.slnx` and **not** part of the default `tests/` gate iteration. It
builds/tests independently against the local NuGet feed.

**Rationale**: Plan §10.3 places samples outside the default test tier (like
`Package.Tests`), and FR-016 requires the required gate never depend on a display. A
decoupled tree means the main solution build never depends on packed output, and the
gallery's GL-touching paths stay advisory — its deterministic coverage/determinism/
invariance tests can run in CI while interactive mode remains GL-gated.

**Alternatives considered**:
- Adding it to the `.slnx` — rejected: creates a build-order coupling (solution build
  would require packages to be packed first) and risks pulling a sample into the
  required gate.

---

## R9 — Provenance / rebrand (FR-015)

**Decision**: Record in `PROVENANCE.md` (or a feature provenance note) that the
10-page structure, the "Indigo & Teal on Slate" palette name, the pointer-interaction
contract, and the per-page evidence requirements are **adopted and rebranded** from the
archived `EHotwagner/FS-Skia-UI` showcase specs (`docs/testSpecs/Showcase/01`–`10`),
with `FS.Skia.UI.*` identifiers mapped to `FS.GG.UI.*`. Those archive specs are **not
present locally**; where a detail is unavailable, plan §10.1 and `src/Controls/Catalog.fs`
are authoritative (per the spec's Assumptions).

**Rationale**: FR-015 requires the rebrand and a provenance record; the archive is the
stated source of the adopted material.

**Alternatives considered**: None — provenance recording is a stated requirement.
