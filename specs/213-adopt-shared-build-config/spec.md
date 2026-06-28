# Feature Specification: Adopt org-shared .NET build config (unified restore-lock gate)

**Feature Branch**: `213-adopt-shared-build-config`

**Created**: 2026-06-28

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" → resolved to **FS-GG/FS.GG.Rendering#11 — H3 · rendering — Adopt shared-build-config; migrate RestoreLockedMode gate CIB→GITHUB_ACTIONS** (Coordination board status: Ready; contract: `shared-build-config`; upstream source of truth `.github#19` / ADR-0006 is merged).

## User Scenarios & Testing *(mandatory)*

This is a build-configuration coherence feature. The "users" are the repo maintainers and the
CI system. The value is that FS.GG.Rendering stops maintaining a forked copy of the .NET build
baseline and instead takes the org-shared one verbatim, so cross-repo drift becomes impossible to
introduce silently and the lone divergence (Rendering's restore-lock gate) is removed.

### User Story 1 - Adopt the shared baseline without losing repo-specific settings (Priority: P1)

A maintainer replaces the repo's hand-authored root `Directory.Build.props` and
`Directory.Packages.props` with the canonical org-shared files, and adds the shared tool manifest.
Everything specific to FS.GG.Rendering — its package metadata, its fsdocs configuration, its F#
warning promotions, and its non-baseline package versions — is moved into repo-owned `*.local.props`
override files that the canonical files import last, so the repo build behaves exactly as before
apart from the deliberately unified restore-lock gate.

**Why this priority**: This is the substance of the board item. Without the sync-not-fork adoption
there is no shared baseline to converge on, and the repo keeps drifting from the other three repos.

**Independent Test**: After adoption, the three managed files are byte-identical to the canonical
source (a drift check reports zero drift), and a full restore→build→test cycle is green — proving
the repo-specific settings still take effect through the local override files.

**Acceptance Scenarios**:

1. **Given** the repo's hand-authored root build files, **When** the maintainer adopts the canonical
   files, **Then** the prior hand-authored content is relocated to `Directory.Build.local.props` /
   `Directory.Packages.local.props` (nothing is lost) and the managed files carry the org "source of
   truth" marker.
2. **Given** the adopted configuration, **When** a drift check compares the managed files against the
   canonical source, **Then** it reports no drift (clean).
3. **Given** the adopted configuration, **When** restore→build→test runs, **Then** it completes green
   and the repo's warning promotions, package metadata, and fsdocs settings are all still in effect.

---

### User Story 2 - Unify the restore-lock gate on the real CI signal (Priority: P1)

The maintainer migrates the `RestoreLockedMode` gate from `ContinuousIntegrationBuild` to
`GITHUB_ACTIONS`. In CI, restore runs in locked mode so any stale or silently substituted dependency
version fails the build; on a fresh local clone the gate stays off so a first restore can bootstrap
the lockfile instead of failing closed.

**Why this priority**: Rendering is the *lone* CIB outlier across the four FS-GG repos; the whole
point of ADR-0006 is one unified gate spelling. The gate must keep CI enforcement while never
wedging a local clone.

**Independent Test**: The gate evaluates to locked when `GITHUB_ACTIONS=true` and a lockfile exists,
and evaluates to unlocked with no CI environment variable set — verifiable by inspecting restore
behavior in each condition.

**Acceptance Scenarios**:

1. **Given** a checkout with a committed lockfile in a GitHub Actions run, **When** restore runs,
   **Then** it runs in locked mode and a drifted/substituted dependency version fails restore.
2. **Given** a fresh local clone (no CI environment variable, no lockfile yet), **When** restore runs,
   **Then** it is not blocked and bootstraps the lockfile.
3. **Given** the migrated gate, **When** the gate spelling is compared to the other three FS-GG repos,
   **Then** it matches (`GITHUB_ACTIONS`), and Rendering is no longer an outlier.

---

### User Story 3 - Central-package and tool coherence (Priority: P2)

The maintainer removes the repo's local `FSharp.Core` version pin so it is sourced from the single
org baseline, and confirms the shared tool manifest's pinned tool version agrees with the repo's
corresponding library pin.

**Why this priority**: The org baseline is the cross-repo coherence point. A duplicate baseline pin
breaks restore (Central Package Management forbids it), and a tool/library version mismatch reintroduces
exactly the kind of silent incoherence the shared config exists to prevent.

**Independent Test**: Restore succeeds with no duplicate-package error, and the tool manifest's
`fake-cli` version equals the repo's `Fake.Core.*` library pin.

**Acceptance Scenarios**:

1. **Given** the org baseline already pins `FSharp.Core` at `10.1.301`, **When** the repo's local
   `FSharp.Core` pin is removed, **Then** restore resolves `FSharp.Core` from the baseline with no
   duplicate-version error and no change to the effective version.
2. **Given** the shared tool manifest pins `fake-cli 6.1.4` and the repo pins `Fake.Core.* 6.1.4`,
   **When** the two are compared, **Then** they match.

---

### Edge Cases

- **Fresh clone, no lockfile**: the restore-lock gate must stay off (the `Exists(lockfile)` guard), so
  a first restore bootstraps the lockfile rather than failing before one exists.
- **A managed file is later hand-edited**: the drift check must report drift (so a re-sync is required),
  rather than silently tolerating a fork.
- **A project that deliberately opts out of locked restore** (e.g. the package-validation test project,
  which sets `RestoreLockedMode=false` and carries no lockfile because its preview pins churn): must
  continue to work unchanged after adoption.
- **The template's emitted build files** (`template/base/Directory.Build.props`, which intentionally
  keeps the `ContinuousIntegrationBuild` gate for generated products) are governed by a *different*
  contract and must NOT be changed by this feature.
- **A baseline package re-declared locally**: must surface as a Central Package Management duplicate
  error, not resolve silently — confirming the baseline is authoritative.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The repo MUST take the org-shared managed build files — `Directory.Build.props`,
  `Directory.Packages.props`, and `.config/dotnet-tools.json` — verbatim from the canonical source of
  truth (identified by the "Source of truth: FS-GG/.github" marker), rather than maintaining
  hand-authored forks of them.
- **FR-002**: The `RestoreLockedMode` gate MUST be conditioned on `GITHUB_ACTIONS` (the unified
  spelling) AND the existence of a lockfile — not on `ContinuousIntegrationBuild`.
- **FR-003**: Every repo-specific build setting currently in the root `Directory.Build.props` MUST be
  preserved by relocating it to a repo-owned `Directory.Build.local.props` (imported last so it can
  override org defaults). This includes at least: target framework / language / nullable / unsafe
  settings; `TreatWarningsAsErrors` plus the F# warning promotions (FS0025, FS0026, FS0052, FS0064,
  FS0078); package metadata (version, authors, repository URLs, license, readme packing); and the
  fsdocs (`FsDocs*`) documentation-site properties.
- **FR-004**: Every repo-specific `PackageVersion` item MUST be preserved in a repo-owned
  `Directory.Packages.local.props`, EXCEPT `FSharp.Core`, which MUST be removed because it is now
  provided by the org baseline (`10.1.301`).
- **FR-005**: The repo MUST NOT re-declare any package pinned by the org baseline; a duplicate baseline
  pin MUST fail restore (Central Package Management duplicate error) rather than resolve silently.
- **FR-006**: The shared tool manifest's `fake-cli` version (`6.1.4`) MUST match the repo's
  `Fake.Core.*` library pin (`6.1.4`); adopting the manifest MUST NOT break the repo's existing
  compiled-FAKE build path.
- **FR-007**: After adoption, a drift check of the managed files against the canonical source MUST
  report no drift.
- **FR-008**: After adoption, a full restore→build→test cycle MUST pass, and a repeated restore MUST be
  byte-reproducible (the lockfile is unchanged on a second restore).
- **FR-009**: The restore-phase warning promotions that fail on silent substitution / out-of-range
  versions (NU1603, NU1608) MUST remain in effect after adoption.
- **FR-010**: The template's emitted build files under `template/base/` MUST remain unchanged by this
  feature (they are governed by the separate template contract for generated products).
- **FR-011**: Projects that deliberately opt out of locked restore MUST continue to opt out unchanged
  after adoption.

### Key Entities

- **Managed file set**: the three files taken verbatim from the org source of truth —
  `Directory.Build.props`, `Directory.Packages.props`, `.config/dotnet-tools.json`. Marked "DO NOT
  EDIT"; any local edit is drift.
- **Local override files**: repo-owned `Directory.Build.local.props` and
  `Directory.Packages.local.props`, imported last by the managed files; the home for everything
  specific to FS.GG.Rendering.
- **Org baseline**: the cross-repo coherence point — versions every FS-GG repo must agree on (today:
  `FSharp.Core 10.1.301`), declared once in the shared `Directory.Packages.props`.
- **Unified restore-lock gate**: the single condition (`GITHUB_ACTIONS` AND lockfile exists) that turns
  on locked restore in CI while leaving fresh local clones bootstrappable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The three managed build files are byte-identical to the canonical org source — a drift
  check reports zero drift.
- **SC-002**: In a CI run with a committed lockfile, a deliberately stale or substituted dependency
  version fails restore; on a fresh local clone with no CI environment variable, restore completes
  without being blocked.
- **SC-003**: A full restore→build→test cycle completes green with no errors.
- **SC-004**: Two consecutive restores produce an identical lockfile (no diff) — restore is reproducible.
- **SC-005**: Rendering's restore-lock gate spelling matches the other three FS-GG repos
  (`GITHUB_ACTIONS`); Rendering is no longer the lone `ContinuousIntegrationBuild` outlier.
- **SC-006**: The repo build behaves identically to before adoption apart from the gate spelling —
  the same warnings are promoted to errors, the same package metadata and fsdocs settings apply, and
  `FSharp.Core` resolves to the same effective version (`10.1.301`) with no duplicate-pin error.

## Assumptions

- The canonical source of truth (FS-GG/.github `dist/dotnet/`, ADR-0006, `.github#19`) is merged and
  available — verified: a sibling `.github` checkout with `dist/dotnet/` and `scripts/sync-build-config.sh`
  is present alongside this repo.
- The org baseline pins `FSharp.Core` at `10.1.301`, identical to the repo's current pin, so adoption
  introduces no functional version change.
- This repo's CI runs on GitHub Actions, where `GITHUB_ACTIONS` is set automatically, so migrating the
  gate to that signal preserves CI enforcement of locked restore.
- Adoption is done with the canonical sync tooling (`sync-build-config.sh --adopt` / `--check`) rather
  than by hand-copying, so the managed files keep their "source of truth" marker and the drift check is
  meaningful.
- The centralized reusable drift-check workflow (`.github#18`) is not yet available (Coordination board:
  Backlog/blocked). This feature delivers a drift-clean adoption that *passes* `--check`; wiring the
  org-level coherence workflow into every repo's CI is tracked separately and is out of scope here.
- The `.config/dotnet-tools.json` `fake-cli` manifest is a coherence artifact only; the repo's build
  continues to run via its compiled FAKE front-end (`build/Build.fsproj` via `dotnet run`), which the
  manifest does not disturb.
- Changes to the template's emitted build files (`template/base/`) are out of scope — they are a separate
  contract for generated products and intentionally retain their own gate.
