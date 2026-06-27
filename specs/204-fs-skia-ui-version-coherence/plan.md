# Implementation Plan: Restore fs-skia-ui-version Cross-Repo Coherence

**Branch**: `204-fs-skia-ui-version-coherence` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/204-fs-skia-ui-version-coherence/spec.md`

## Summary

Close the coherence loop that cross-repo request **FS-GG/FS.GG.Rendering#1** tracks: a downstream
consumer scaffolding `dotnet new fs-gg-ui` must restore + build a working product against **one
reproducible, immutable** `FS.GG.UI.*` snapshot, and the cross-repo record (the `fs-skia-ui-version`
registry row + issue #1) must be brought into agreement with that verified reality.

Feature `201` already refreshed the seed code to the current Scene API and bumped the pin to
`0.1.49-preview.1` (four profiles green against a *local* feed). What remains — and what issue #1
literally complains about — is that **there are no git tags, so the pin points at a moving feed,
not an immutable snapshot**, and the registry still reads `coherent: false`.

**Approach** (chosen mechanism: *git tag + committed lockfile*):

1. **Verify-then-record.** Re-pack the framework at HEAD to its next coherent version (expected
   `0.1.51-preview.1` — the packer fixes the exact value), then for **each** of the four profiles
   *generate → restore → build → evidence* against that set. Every restore/compile failure is a
   concrete coherence defect to fix in the template before anything is declared coherent.
2. **Fix the two phantom pins.** `Directory.Packages.props` pins `FS.GG.UI.Color` (retired in
   Feature 179 — `src/ColorPolicy` is `IsPackable=false`, namespace folded in-assembly) and
   `FS.GG.UI.SkillSupport` (no producing project; absent from the feed at every version). Neither
   package exists; both pins (and their now-false explanatory comments) are removed so the template
   pins only packages that actually ship. They do not break restore today only because the seed
   never `PackageReference`s them — but a consumer who follows the skills' guidance and adds the
   reference would hit NU1101.
3. **Make the pin reproducible** (the heart of US2/FR-003). Cut an annotated git tag at the
   resolution commit (`fs-skia-ui/v<version>`), record a snapshot manifest of the 16 real
   `FS.GG.UI.*` IDs @ that version under `contracts/`, and commit `packages.lock.json` for the
   template with `RestoreLockedMode` so two restores resolve byte-for-byte the same set.
4. **Reconcile the cross-repo record** (US3) — *only after US1/US2 pass*. Flip the
   `fs-skia-ui-version` row to `coherent: true` in `FS-GG/.github` (`registry/dependencies.yml` +
   `docs/registry/compatibility.md`), post a `## Response` on issue #1 naming the option taken and
   linking the evidence, and close it.

> **Standing assumption — coherence is unverified until a product is generated, restored, and built.**
> A green `201` against an older feed is **not** evidence that the *current* pinned set restores and
> builds, nor that the set is coherent (the phantom Color/SkillSupport pins and the per-package
> independent version cadence mean a "0.1.x set" can be partial). The only trustworthy signal is a
> real per-profile *generate → restore → build → evidence* run against a freshly packed feed.
> `/speckit-tasks` MUST schedule that run as the first Foundational step, before any registry/issue
> write. The registry/issue MUST NOT be flipped on a hypothesis (FR-007).

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`); template scaffolding via the dotnet template engine;
build/evidence orchestration via `build.fsx` (FSI, no FAKE).

**Primary Dependencies**: The 16 published `FS.GG.UI.*` packages (`Build`, `Scene`, `Canvas`,
`Controls`, `Controls.Elmish`, `DesignSystem`, `Diagnostics`, `Elmish`, `KeyboardInput`, `Layout`,
`SkiaViewer`, `Symbology`, `Symbology.Render`, `Testing`, `Themes.AntDesign`, `Themes.Default`)
consumed from the local feed `~/.local/share/nuget-local/`, all pinned through the single
`$(FsSkiaUiVersion)`. The template subset actually referenced by the seed: Scene (all profiles);
SkiaViewer, Elmish, KeyboardInput, Layout, Controls, Controls.Elmish, DesignSystem, Themes.Default
(app/sample-pack); Testing (governed).

**Storage**: N/A — source/template files, a snapshot manifest, lock files, and cross-repo record only.

**Testing**: `template/base/tests/Product.Tests/GovernanceTests.fs` (profile-guarded text/structure
assertions incl. the single-source `FsSkiaUiVersion` invariant); `build.fsx` `Verify`
(Dev + generated-guidance + template-drift + evidence + Test); the generated product's evidence CLI
(`--scene-evidence`, `--layout-evidence`, app-profile launch/screenshot). Verification is per-profile
against a freshly **generated** product (not against `template/base` in place — the seed `.fs` carry
both profile branches under `//#if` directives and are not directly compilable).

**Target Platform**: Linux desktop (SkiaSharp over OpenGL); headless-capable for governed/headless-scene
and evidence commands.

**Project Type**: Maintenance of a product-scaffolding template + a cross-repo coordination
deliverable. The seed product is an `Exe` with no `.fsi` (application code, not a packaged surface).

**Performance Goals**: N/A — correctness/coherence task.

**Constraints**:
- `template/base` is **not directly compilable**: validation MUST go through `dotnet new`
  generation (which strips inactive `//#if` branches) before building.
- `FsSkiaUiVersion` MUST remain the only FS.GG.UI version literal (Feature-064 invariant; asserted by
  GovernanceTests). The pin references `$(FsSkiaUiVersion)`; no second literal in pins, docs, or seed.
- Cross-repo state (registry + issue #1) lives in `FS-GG/.github` and `FS-GG/FS.GG.Rendering` and is
  mutated through the **GitHub-native** coordination protocol (`gh`), not through files in this repo.
- The registry/issue MUST NOT be flipped to coherent unless and until US1+US2 hold (FR-007).
- Scope is coherence restoration only — no new profiles, controls, or product features (Out of Scope).

**Scale/Scope**: 1 version pin; 2 phantom pins to remove; 1 snapshot manifest; ≤5 lock files (one per
generated profile, or a template-level one); 4 profiles to verify; 1 registry row (2 files in a sibling
repo); 1 GitHub issue. The framework `src/**` is the read-only target — not modified by this feature.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ Pass (N/A shape) | No new public library surface is designed; the FS.GG.UI API is the fixed target, not under change. "Tests" map to the template's existing governance + per-profile generate/restore/build/evidence, run as the coherence signal. |
| II. Visibility lives in `.fsi` | ✅ Pass | Seed product is an `Exe` with no `.fsi`; no library `.fsi` added/modified; no access modifiers introduced. |
| III. Idiomatic simplicity | ✅ Pass | Edits are pin removal + a version bump + a committed lockfile/manifest; no operators/SRTP/reflection/CEs. |
| IV. Elmish/MVU boundary | ✅ Pass | The seed's MVU boundary is untouched; this feature changes pins/snapshot/record, not behavior. |
| V. Test evidence is mandatory | ✅ Pass | Verification is real: generate each profile, restore against the packed feed, build, run governance + evidence. No synthetic evidence added. The registry/issue flip is gated on this real evidence (FR-007/FR-008). |
| VI. Observability and safe failure | ✅ Pass | No change to the seed's diagnostics. The coherence decision fails *loud and closed*: a partial/missing snapshot keeps `coherent: false` and the issue open (edge cases). |

**Change classification**: **Tier 1 (contracted change)** — it resolves a versioned cross-repo
contract (`fs-skia-ui-version`), changes the package pin set (removes two pins), and flips a registry
coherence entry. Per Change Classification this needs the full artifact chain (spec, plan, test
evidence, doc/record updates); there are no `.fsi`/surface-area baselines to touch because no public
F# surface changes. No gate violations — **Complexity Tracking not required**.

## Project Structure

### Documentation (this feature)

```text
specs/204-fs-skia-ui-version-coherence/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — snapshot-mechanism decision, phantom-pin finding, version choice
├── data-model.md        # Phase 1 — entities (contract, pin set, snapshot, profile run, registry row, request)
├── quickstart.md        # Phase 1 — verify→record→reconcile run guide
├── contracts/           # Phase 1
│   ├── coherence-verification.md   # the per-profile generate→restore→build→evidence gate (US1)
│   ├── snapshot-manifest.md        # the immutable snapshot: tag + 16 IDs@version + lockfile (US2)
│   └── cross-repo-resolution.md    # registry-row + issue-#1 reconciliation, gated on US1/US2 (US3)
├── checklists/
│   └── requirements.md  # Spec quality checklist (from /speckit-specify, if present)
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
# Template under repair (the in-repo work happens here)
template/base/
├── Directory.Packages.props        # FsSkiaUiVersion pin (bump) + REMOVE phantom FS.GG.UI.Color / FS.GG.UI.SkillSupport pins
├── src/Product/Product.fsproj      # references only real, shipping FS.GG.UI.* packages (verify, opt-in RestoreLockedMode)
├── docs/UPGRADING.md               # illustrative `0.1.68-preview.1` example — confirm it is not a governed literal
└── tests/Product.Tests/GovernanceTests.fs  # single-source FsSkiaUiVersion invariant (re-run, must stay green)

# Reproducible-snapshot artifacts (new)
specs/204-fs-skia-ui-version-coherence/contracts/snapshot-manifest.md  # recorded 16 IDs @ pinned version
template/base/**/packages.lock.json   # committed lock file(s); RestoreLockedMode for byte-reproducible restore
<git tag>  fs-skia-ui/v<version>      # annotated tag at the resolution commit

# Packing / scaffolding tooling (used as-is, not edited)
.template.config/template.json        # `dotnet new fs-gg-ui --profile <app|headless-scene|governed|sample-pack>`
scripts/refresh-local-feed-and-samples.fsx  # packs FS.GG.UI.* to ~/.local/share/nuget-local/
Directory.Build.props                 # repo source <Version> — the version the feed packs at

# Framework source (read-only target — confirms which package IDs actually ship)
src/*/                                # 16 packable FS.GG.UI.* projects; src/ColorPolicy is IsPackable=false

# Cross-repo record (sibling repos, mutated via gh — not files here)
FS-GG/.github : registry/dependencies.yml + docs/registry/compatibility.md   # fs-skia-ui-version row
FS-GG/FS.GG.Rendering#1                                                       # the request issue
```

**Structure Decision**: In-repo edits are confined to `template/base/` (pin bump, phantom-pin removal,
committed lock file) plus the feature's `contracts/` (snapshot manifest). `src/**` and the
packing/scaffolding tooling are used read-only/as-is. The reproducible snapshot is materialized as a
**git tag + committed lockfile + recorded manifest** (the chosen mechanism). The cross-repo record is
mutated in the sibling `FS-GG/.github` and `FS-GG/FS.GG.Rendering` repos through `gh`, gated on verified
US1/US2 evidence.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
