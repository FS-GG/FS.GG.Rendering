# Implementation Plan: Finalize the root-buildable template guarantee (release the coherent set + close #9)

**Branch**: `215-root-build-release-closure` | **Date**: 2026-06-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/215-root-build-release-closure/spec.md`

## Summary

This is a **release/closure slice**, not a product-capability change. Feature 212 already made every
`fs-gg-ui`-scaffolded product root-buildable with the stock .NET toolchain (root `.slnx`, `global.json`
SDK pin, `restore|build|test|run|verify|pack` verb wrapper) and merged it to `main` at `b6ac246`. The
capability is *built and live-verified* but *not delivered*: the latest published template tag
`fs-gg-ui-template/v0.1.50-preview.1` predates the root-build work, the org contract-registry coherence
PR `FS-GG/.github#25` is still open (and currently CONFLICTING), and issue #9 / the Coordination board
still read "In review".

**Technical approach**: publish the next coherent set at **`0.1.52-preview.1`** (decision from research —
matches the already-bumped `.template.package` fsproj and advances cleanly past the existing `0.1.51`
framework set) by driving the existing `release.yml` workflow, which packs the whole `FS.GG.Rendering.slnx`
+ the template at one `-p:Version` and runs the release-only `package-tests` + `template-product-tests`
gates before publishing to `nuget.pkg.github.com/FS-GG`. Then bring the coherent set into agreement
(`FsGgUiVersion` → `0.1.52-preview.1`, manual `fs-gg-ui/v0.1.52-preview.1` + `fs-gg-ui-template/v0.1.52-preview.1`
snapshot tags), confirm the Feature 209 staleness guard is green, land `.github#25` pinned to the released
version (rebased to clear the conflict, with or after the release — never before), and close #9 with
released-artifact evidence while flipping the board to Done.

No `.fs`/`.fsi` is edited; the only in-repo source change is the `FsGgUiVersion` bump (a build property)
so the published set, the registry entry, and the org version line all agree.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature ships no fix and forms no defect hypothesis; it releases an already-live-verified
> capability. The "live run" obligation here is satisfied by the **release-only `template-product-tests`
> gate executing on the real release** (FR-002/SC-002) — the gate instantiates the published-shape
> template and runs stock `dotnet build`/`test`/`run` at the product root. `/speckit-tasks` MUST schedule
> that real-release gate run (not a local dry run) as the load-bearing evidence before #9 is closed.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (release/CI plumbing only; no library code changes). YAML
GitHub Actions, FAKE/`build.fsx`, and `dotnet fsi` validation scripts are the working surfaces.

**Primary Dependencies**: `.github/workflows/release.yml` (pack + release-only gates + publish to org
feed); `scripts/validate-version-coherence.fsx` (Feature 209 staleness guard); the `dotnet new fs-gg-ui`
template (`.template.package/FS.GG.UI.Template.fsproj`, `template/base/`); `gh` CLI (issue/PR/board);
the org contract registry in `FS-GG/.github` (`registry/dependencies.yml` + `docs/registry/compatibility.md`).

**Storage**: N/A. State of record lives in: git tags (`fs-gg-ui/v*`, `fs-gg-ui-template/v*`), the org
GitHub Packages feed (`nuget.pkg.github.com/FS-GG`), the `FS-GG/.github` registry files, issue #9, and the
FS-GG "Coordination" Projects v2 board.

**Testing**: Release-only gates — `package-tests` (`tests/Package.Tests`) and `template-product-tests`
(install template → `dotnet new fs-gg-ui` → stock `dotnet build`/`test`/`run` at product root). Version
coherence: `dotnet fsi scripts/validate-version-coherence.fsx` (structural) and the
`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1` restore-grounded proof. Local pre-flight only; the load-bearing
evidence is the gate run on the actual release.

**Target Platform**: GitHub Actions `ubuntu-latest` runners (release workflow); org GitHub Packages feed;
cross-repo registry in `FS-GG/.github`.

**Project Type**: Release/coordination closure within a single repo (the F# UI framework + template product),
with one cross-repo registry-coherence handshake.

**Performance Goals**: N/A (no runtime/perf surface).

**Constraints**:
- Coherent-set version is **`0.1.52-preview.1`** for all three numbers (published template version =
  registry coherence-entry version = org `FsGgUiVersion`), so the Feature 209 guard reports no straggler
  (FR-004/SC-003).
- Ordering is hard: the registry change (`.github#25`) lands **with or after** the release, never before
  (FR-006/SC-004) — no window advertising an unreleased guarantee.
- The release-gate evidence MUST come from the **real release**, not a local/dry run (FR-002/SC-002).
- **No product-capability change**: the published template must be byte/behaviorally identical to the
  `main`-built template (FR-003/FR-011); the only source edit is the `FsGgUiVersion` property bump.

**Scale/Scope**: One coherent-set release; one in-repo property bump; two snapshot tags; one cross-repo PR
rebase+merge; one issue close + one board transition; one downstream unblock signal (FS.GG.SDD H1).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change Classification**: **Tier 2 (closure/release; no public API surface change).** This feature adds,
removes, and modifies **no** public `.fsi` surface and changes **no** observable framework behavior — it
delivers an already-merged capability through a released artifact and reconciles version/registry/closure
state. The single source edit is the `FsGgUiVersion` build property. Per the constitution's Tier 2 rule,
`.fsi` files and surface-area baselines remain untouched (and must stay untouched — touching them would be
FR-011 scope creep).

| Principle / Constraint | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | N/A | No new public surface; nothing to draft in FSI. The "honest audience" here is the release-only `template-product-tests` gate exercising the published template as a real consumer. |
| II. Visibility in `.fsi` | PASS (no-op) | No `.fs`/`.fsi` edited; no access modifiers introduced. |
| III. Idiomatic Simplicity | PASS | Reuses the existing `release.yml`, coherence guard, and `gh`/registry flow; no new machinery, no clever abstractions (Assumption: release infra already exists). |
| IV. Elmish/MVU boundary | N/A | No stateful/I/O feature code; release is one-shot CI plumbing. |
| V. Test Evidence Is Mandatory | PASS | Load-bearing evidence is the **real-release** gate run (FR-002) plus the green coherence guard; both are real-dependency checks, not synthetic. No synthetic evidence used. |
| VI. Observability & Safe Failure | PASS | `release.yml` `set -euo pipefail` blocks on any gate failure; partial publish leaves #9 open (Edge Case). |
| Engineering: package identity `FS.GG.UI.*` | PASS | Release publishes the `FS.GG.UI.*` set + `FS.GG.UI.Template`; no rebrand. |
| Engineering: SkiaSharp pin / `net10.0` | PASS (no-op) | No dependency or TFM change. |
| Engineering: surface-area baselines per public module | PASS (no-op) | No surface change → baselines unchanged (changing them would violate FR-011). |
| Repo-owned checks pay for themselves | PASS | Exercises existing release-packaging + template instantiate/build checks + version-coherence guard; adds none. |

**Gate result: PASS.** No violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/215-root-build-release-closure/
├── plan.md              # This file (/speckit-plan command output)
├── spec.md              # Feature spec (input)
├── research.md          # Phase 0 output (/speckit-plan)
├── data-model.md        # Phase 1 output (/speckit-plan) — release/coherence/closure entities & states
├── quickstart.md        # Phase 1 output (/speckit-plan) — runnable release+closure validation guide
├── contracts/           # Phase 1 output (/speckit-plan)
│   ├── release-gate.md          # release-only template-product-tests + package-tests gate contract
│   ├── coherent-set.md          # version-trio agreement contract (Feature 209 guard)
│   └── registry-coherence.md    # FS-GG/.github#25 registry entry + ordering contract
├── checklists/          # pre-existing (requirements checklist)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root) — touched surfaces

This feature edits **no library `.fs`/`.fsi`**. The surfaces it drives or changes:

```text
FS.GG.Rendering/
├── .github/workflows/release.yml          # DRIVEN (not edited): pack slnx+template at -p:Version,
│                                           #   release-only package-tests + template-product-tests gates,
│                                           #   publish to nuget.pkg.github.com/FS-GG
├── .template.package/FS.GG.UI.Template.fsproj   # already <Version>0.1.52-preview.1</Version> (verify)
├── template/base/Directory.Packages.props # EDIT: <FsGgUiVersion>0.1.51 → 0.1.52-preview.1</FsGgUiVersion>
├── scripts/validate-version-coherence.fsx # RUN: structural + restore-grounded coherence proof
└── (git tags)                             # CREATE: fs-gg-ui/v0.1.52-preview.1,
                                           #         fs-gg-ui-template/v0.1.52-preview.1

# Cross-repo (FS-GG/.github, via gh + cross-repo-coordination skill):
.github/
├── registry/dependencies.yml              # fs-gg-ui-template: root-buildable surface + coherence entry
│                                           #   coherent:true, version:0.1.52-preview.1, tracking #9
└── docs/registry/compatibility.md         # matching projection (discoverable guarantee + tracker #9)
# carried by PR #25 (rebase to clear CONFLICTING; land with/after release)
```

**Structure Decision**: No new directories or projects. The feature operates through (a) one in-repo
build-property edit (`FsGgUiVersion`), (b) the existing `release.yml` release pipeline + manual coherent-set
snapshot tags, (c) the existing Feature 209 coherence guard, and (d) the cross-repo registry + issue/board
closure via `gh` and the `cross-repo-coordination` skill. This matches the constitution's "standard Spec Kit,
no external governance graph" workflow and keeps the change Tier 2.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
