# Feature Specification: Placement & Orphan Decisions (Code-Health Refactoring Phase 2)

**Feature Branch**: `179-placement-orphan-decisions`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "start next item in the project" — Phase 2 of the whole-repo
code-health refactoring plan: relocate the mis-filed `Rendering.Harness` production CLI, and resolve
two orphaned packages (`src/Input/`, `src/Color/`) that no production code consumes.

## Overview

The code-health analysis found the repository is clean of rot but structurally heavy, with debt
concentrated in **misplacement** (a production CLI living under `tests/`) and **orphans** (two
packages that ship or build but that no production code references). Phase 1 (feature 178) removed
duplicated helpers. This phase makes the three placement/ownership calls that were deferred to
maintainer sign-off, and carries each call out to a green build + green test state.

The three decisions, confirmed with the owner, are:

1. **Relocate `Rendering.Harness`** from `tests/` to `tools/` — it is a production CLI
   (`OutputType=Exe`, ~18k lines across 24 files), not a test project, and its location misleads
   readers and tooling.
2. **Retire & unpublish `src/Input/`** (the published package `FS.GG.UI.Input`) — an orphaned
   keyboard-input implementation (1,852 lines) superseded by `src/KeyboardInput/`
   (`FS.GG.UI.KeyboardInput`), which is the live path wired into SkiaViewer, Controls, and
   Controls.Elmish. No production code references `src/Input/`.
3. **Retire `src/Color/`, preserving its internal `ColorPolicy`** — an orphaned, unshipped color
   library (562 lines) that no production code references; its only live consumer is the internal
   `ColorPolicy.fs` used by `Controls.Tests` via `InternalsVisibleTo`, which must be preserved.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature changes no runtime behavior of the shipped product; it relocates one project and
> removes two unreferenced ones. It carries **no defect/root-cause hypothesis to confirm against a
> running app**. The "real evidence" is the existing regression machinery: a clean `dotnet build` of
> the solution plus the full `dotnet test` run, captured as a baseline before any change and diffed
> after each decision lands.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Relocate the mis-filed harness CLI (Priority: P1)

A maintainer browsing `tests/` should find tests, not a 18k-line production CLI. Moving
`Rendering.Harness` to `tools/` makes the repository layout honest: `tests/` holds test projects,
`tools/` holds executable tooling. Every script, spec evidence file, and project reference that
points at the old path is updated so the validation lanes, skill-parity checks, and package-feed
workflows that depend on the harness keep running unchanged.

**Why this priority**: Highest structural-clarity payoff and it unblocks correct mental models for
the rest of the refactoring. It is also the highest-touch change (many hardcoded paths), so doing it
first under a captured baseline lets every later step build on a known-good move.

**Independent Test**: With only this story implemented, the harness builds and runs from
`tools/Rendering.Harness`, the solution builds, the full test suite matches baseline, and the three
helper scripts (`check-agent-skill-parity.fsx`, `run-validation-lanes.fsx`,
`refresh-local-feed-and-samples.fsx`) plus the harness-internal command strings resolve the new path.

**Acceptance Scenarios**:

1. **Given** the harness lives at `tests/Rendering.Harness`, **When** the relocation lands, **Then**
   the project lives at `tools/Rendering.Harness`, is listed in the solution at its new path, and no
   reference to the old `tests/Rendering.Harness` path remains anywhere in the repository (scripts,
   harness internals, FSX evidence scripts, test paths, solution).
2. **Given** the dependent test project `Rendering.Harness.Tests`, **When** the harness moves,
   **Then** its `ProjectReference` resolves to the new location and the project builds.
3. **Given** the linked `TestAssertions.fs` (linked into 4 test projects via `Compile Include`),
   **When** the harness moves, **Then** all four links resolve to the new path and those test
   projects build unchanged.
4. **Given** the validation-lane / skill-parity / feed-refresh scripts, **When** they run after the
   move, **Then** they build and invoke the harness at its new path with no behavior change.

---

### User Story 2 - Retire the orphaned `FS.GG.UI.Input` package (Priority: P2)

`src/Input/` is a complete, published keyboard-input package that no production code uses — the live
keyboard path is `src/KeyboardInput/` (`FS.GG.UI.KeyboardInput`), referenced by SkiaViewer, Controls,
and Controls.Elmish. The orphan ships in the package feed and carries a surface baseline, so retiring
it is an intentional **public-package-surface removal** (a breaking change for any external
consumer), approved by the owner. Removing it deletes ~1,852 lines plus its test project and its
published surface.

**Why this priority**: Largest single line-reduction of the phase and removes a genuinely misleading
duplicate (two keyboard-input APIs, only one live). Sequenced after the harness move because it is a
clean deletion with a smaller blast radius.

**Independent Test**: With only this story implemented, `src/Input/` and `tests/Input.Tests/` are
gone, `FS.GG.UI.Input` is removed from the package-feed manifest and its surface baseline file is
removed, the solution builds without them, and the full suite is green — with the documented
package-feed reds being the only non-green entries (unchanged from baseline).

**Acceptance Scenarios**:

1. **Given** `src/Input/` is referenced only by `tests/Input.Tests/`, **When** the package is
   retired, **Then** both projects are removed, removed from the solution, and nothing else
   referenced them (verified by a clean build).
2. **Given** `FS.GG.UI.Input` is a published package with a surface baseline, **When** it is retired,
   **Then** it no longer appears in the surface-baseline refresh manifest and its baseline file is
   removed, and the surface-baseline gate passes (no orphaned baseline).
3. **Given** the live keyboard path is `src/KeyboardInput/`, **When** `src/Input/` is removed,
   **Then** SkiaViewer, Controls, and Controls.Elmish build and behave identically (they never
   referenced `src/Input/`).

---

### User Story 3 - Retire `src/Color/` while preserving its live internal policy (Priority: P3)

`src/Color/` is an orphaned, unshipped color library (Contrast/Palettes public modules) that no
production code references and that is deliberately excluded from the package feed (no surface
baseline). Its one live consumer is the internal `ColorPolicy.fs` (Feature 127), used by
`Controls.Tests` via `InternalsVisibleTo`. Retiring the orphan means removing the unshipped public
Color package and its test project, while relocating `ColorPolicy` so its sole consumer keeps
working.

**Why this priority**: Smallest of the three and the most delicate (must preserve one internal
consumer), so it lands last. Removes dead public surface that was never shipped while keeping the
color-policy behavior that Controls.Tests asserts.

**Independent Test**: With only this story implemented, the public `Color` package (Contrast,
Palettes) and `tests/Color.Tests/` are removed, `ColorPolicy` lives in its new home, `Controls.Tests`
still compiles and passes the color-policy assertions, and the solution builds green.

**Acceptance Scenarios**:

1. **Given** `src/Color/`'s public surface (Contrast, Palettes) is referenced by no production code,
   **When** it is retired, **Then** those modules and `tests/Color.Tests/` are removed and the
   solution builds without them.
2. **Given** `ColorPolicy.fs` is consumed by `Controls.Tests` via `InternalsVisibleTo`, **When**
   `src/Color/` is retired, **Then** `ColorPolicy` is relocated to a home reachable by
   `Controls.Tests`, and the color-policy tests compile and pass unchanged.
3. **Given** `FS.GG.UI.Color` was never shipped (no surface baseline, excluded from the feed),
   **When** it is retired, **Then** no surface-baseline or package-feed change is required for the
   public Color modules (only the internal policy relocates).

---

### Edge Cases

- **Frozen FSX evidence scripts**: several `specs/<feature>/readiness/fsi/*.fsx` files `#r`/`open`
  the harness at its old path. These are historical evidence artifacts — the path inside them must
  be updated so they still resolve, without altering the recorded evidence semantics. If any cannot
  be safely updated, that must be called out rather than silently left broken.
- **Surface-baseline drift**: removing `FS.GG.UI.Input` must keep the surface-baseline gate
  internally consistent — no baseline file left without a package, and no package left without a
  baseline.
- **Hidden references**: a string search for the old harness path may match non-reference literals
  (e.g. `dotnet build`/`pack` command arguments). Only genuine path references are rewritten;
  command-argument literals that intentionally name the project are updated to the new path as well
  so the commands still succeed.
- **Pre-existing reds**: the baseline already carries documented package-feed reds (Package.Tests,
  ControlsGallery package-feed). These are not regressions and must remain the only non-green
  entries after each story.
- **InternalsVisibleTo**: if `ColorPolicy` moves into an assembly that already grants
  `InternalsVisibleTo` to `Controls.Tests`, no new grant is needed; otherwise the grant must be
  carried along so the tests still see the internal surface.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `Rendering.Harness` project MUST be relocated from `tests/Rendering.Harness` to
  `tools/Rendering.Harness`, including all of its source and `.fsi` files.
- **FR-002**: The solution file MUST list the harness at its new `tools/` path and no longer list it
  under `tests/`.
- **FR-003**: Every reference to the old harness path MUST be updated to the new path, including:
  the dependent test project's `ProjectReference`; the four `TestAssertions.fs` linked-source
  includes; the three helper scripts (`check-agent-skill-parity.fsx`, `run-validation-lanes.fsx`,
  `refresh-local-feed-and-samples.fsx`); the harness-internal hardcoded command paths
  (`Compositor.fs`, `ValidationLanes.fs`, `Live.fs`); the FSX evidence scripts under `specs/**`; and
  any hardcoded test paths (e.g. the Feature 170 retained-inspection lane test).
- **FR-004**: After the harness move, the validation-lane, skill-parity, and feed-refresh workflows
  MUST continue to build and invoke the harness, with no behavior change.
- **FR-005**: `src/Input/` and `tests/Input.Tests/` MUST be removed and de-listed from the solution.
- **FR-006**: The published package `FS.GG.UI.Input` MUST be unpublished: removed from the
  surface-baseline refresh manifest and its surface-baseline file removed, leaving the
  surface-baseline gate internally consistent.
- **FR-007**: Removal of `src/Input/` MUST NOT alter the live keyboard-input path (`src/KeyboardInput/`)
  or any project that depends on it (SkiaViewer, Controls, Controls.Elmish).
- **FR-008**: The public `src/Color/` modules (Contrast, Palettes) and `tests/Color.Tests/` MUST be
  removed and de-listed from the solution.
- **FR-009**: The internal `ColorPolicy` (Feature 127) MUST be preserved and relocated to a home
  reachable by its existing consumer, `Controls.Tests` (via `InternalsVisibleTo`), with the
  color-policy tests compiling and passing unchanged.
- **FR-010**: No public package surface other than the intentional `FS.GG.UI.Input` removal MAY
  change. In particular, `FS.GG.UI.Color` was never shipped, so retiring its public modules MUST NOT
  touch any surface baseline; and the harness relocation MUST NOT change any package surface.
- **FR-011**: A baseline `dotnet build` + `dotnet test` capture MUST be recorded before any change,
  and each of the three stories MUST be diffed against it; the documented pre-existing package-feed
  reds MUST remain the only non-green entries after each story.
- **FR-012**: Each user story MUST be independently shippable — the solution builds and the test
  suite matches baseline after each story lands, in priority order.

### Key Entities *(include if feature involves data)*

- **Rendering.Harness (production CLI)**: An executable tooling project (`OutputType=Exe`) currently
  under `tests/`, moving to `tools/`. Consumed by scripts, FSX evidence, one test project, and four
  linked-source test projects (`TestAssertions.fs`). Carries no package surface.
- **`FS.GG.UI.Input` (orphaned published package)**: `src/Input/` — a complete keyboard-input package
  (~1,852 lines) with a surface baseline, referenced only by `tests/Input.Tests/`. Superseded by
  `FS.GG.UI.KeyboardInput`. Being retired and unpublished.
- **`FS.GG.UI.Color` (orphaned unshipped package)**: `src/Color/` — Contrast/Palettes public modules
  (562 lines total incl. the internal policy), never shipped (no surface baseline), referenced only
  by tests. Public modules being retired.
- **`ColorPolicy` (internal, live)**: `ColorPolicy.fs` (Feature 127) inside `src/Color/` — the one
  live consumer surface, reached by `Controls.Tests` via `InternalsVisibleTo`. Being preserved and
  relocated, not removed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After the feature, `tests/` contains zero production-executable (`OutputType=Exe`)
  projects; the only relocated CLI lives under `tools/`.
- **SC-002**: A repository-wide search for the old path `tests/Rendering.Harness` returns zero
  genuine references (project refs, linked includes, script paths, FSX `#r`/`open`, test paths,
  solution entries).
- **SC-003**: The orphan line-count reduction is realized: `src/Input/` (~1,852 lines) and the public
  `src/Color/` modules are removed, for a net source reduction on the order of the plan's
  ≈ −1,400-line estimate or larger, with no production code deleted.
- **SC-004**: Exactly one published package surface changes — `FS.GG.UI.Input` is removed — and the
  surface-baseline gate remains internally consistent (no baseline without a package, no package
  without a baseline). No other package surface changes.
- **SC-005**: `dotnet build` of the solution succeeds and `dotnet test` matches the captured
  baseline after each of the three stories; the documented pre-existing package-feed reds are the
  only non-green entries.
- **SC-006**: `Controls.Tests` continues to compile and pass its color-policy assertions after
  `ColorPolicy` is relocated, demonstrating the live internal consumer was preserved.

## Assumptions

- **Owner decisions confirmed**: relocate the harness to `tools/` now; retire & unpublish
  `FS.GG.UI.Input`; retire `src/Color/`'s public modules while preserving the internal `ColorPolicy`.
  These were the three sign-off decisions the plan deferred to the maintainer.
- **`FS.GG.UI.Input` has no in-repo production consumer**, so retiring it is safe within the repo;
  the breaking impact is limited to hypothetical external package consumers, which the owner has
  accepted.
- **`FS.GG.UI.Color` was never shipped** (no surface baseline, excluded from the feed), so retiring
  its public modules requires no package-feed or surface-baseline change for Color.
- **Baseline reds are stable**: the pre-existing Package.Tests / ControlsGallery package-feed reds
  are environmental/baseline, not regressions, and remain unchanged by this feature.
- **Tier classification**: the `FS.GG.UI.Input` removal is a **Tier-3** (public package surface)
  change; the harness move and the Color/ColorPolicy work are **Tier-2** (internal placement) since
  they touch no shipped package surface. Exact relocation homes for the harness and `ColorPolicy`
  are implementation decisions for `/speckit-plan`.
- **Scope boundary**: this feature does not refactor the harness internals, the keyboard-input
  behavior, or color logic — it only relocates and removes. Behavior is preserved throughout.
