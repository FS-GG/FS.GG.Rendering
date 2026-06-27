# Implementation Plan: Publish FS.GG.UI.Template Carrying the Lifecycle Parameter & Tag the Coherent Set

**Branch**: `206-publish-template-coherent-set` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/206-publish-template-coherent-set/spec.md`

## Summary

Features 204 and 205 added the `lifecycle` choice symbol (`spec-kit|sdd|none`, `spec-kit`
default byte-identical) and made `dotnet new fs-gg-ui` generation side-effect-free by default
(`initGit` opt-in replacing `skipGitInit`, no auto post-actions). Those surfaces live only in this
repo's working tree: the published `FS.GG.UI.Template` package is still `0.1.17-preview.1`, so no
consumer can install them. This feature **publishes** the template at a new version that carries
those surfaces, **tags** a reproducible coherent set binding that template version to the framework
`FS.GG.UI.*` set it scaffolds against, and **reconciles** the cross-repo record so it agrees with
the published reality. It is the `fs-gg-ui-template` analogue of feature 204's `fs-skia-ui-version`
coherence work, and emits no UI (packaging/release-coherence deliverable per the Ant Design
source-of-truth note).

**Resolved release identifiers** (see [research.md](./research.md) R1–R2):

- **Published template version**: `0.1.50-preview.1` — aligns the template package number with the
  framework set it pins (`FS.GG.UI.* 0.1.50-preview.1`, tagged `fs-skia-ui/v0.1.50-preview.1` by
  204), strictly greater than the published `0.1.17-preview.1`, and not yet on the feed.
- **Coherent-set tag**: `fs-gg-ui-template/v0.1.50-preview.1` — a **template-scoped** annotated tag
  namespace, distinct from 204's framework-scoped `fs-skia-ui/...`, recording the published template
  version over the framework set it is coherent with.
- **Dependent cross-repo request**: `FS-GG/FS.GG.SDD#1` (open) — the scaffold-path / side-effect-free
  ask that becomes actionable only once a package carrying these surfaces is installable.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature changes no runtime behavior, but the publish/install/scaffold path itself is the
> "app" here. `/speckit-tasks` MUST schedule an **early live verification** (Foundational phase)
> that packs to the feed and instantiates one profile from the *installed package* (not the working
> tree) before the byte-identical and side-effect-free gates are claimed. Working-tree green does not
> prove the published package is correct.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; release procedure driven by `dotnet` CLI + repo `.fsx`
scripts. No new F# product surface.

**Primary Dependencies**: `dotnet pack` / `dotnet new`; `.template.package/FS.GG.UI.Template.fsproj`
(packs the repo content as `content`); `.template.config/template.json` (carries `lifecycle` +
`initGit`); existing pack tooling (`scripts/refresh-local-feed-and-samples.fsx`, the
`Rendering.Harness` `package-feed` command); `gh` CLI for cross-repo issue/registry work.

**Storage**: Package feed at `~/.local/share/nuget-local/` (the project's existing local/preview
feed — the "publish" target per spec Assumptions). Git tags for the coherent-set anchor. Cross-repo
registry in `FS-GG/.github` (`registry/dependencies.yml` + `docs/registry/compatibility.md`).

**Testing**: Existing template checks against the **installed** package — `Feature204LifecycleTemplateTests`,
`Feature205TemplateSideEffectTests`, and the live validators `scripts/validate-lifecycle-template.fsx`
(`FS_GG_RUN_LIFECYCLE_VALIDATION=1` for byte-diff per profile) — plus `dotnet new fs-gg-ui` →
restore → build → evidence per profile, and `scripts/baseline-tests.fsx` for non-regression.

**Target Platform**: Linux dev host (headless); the feed and tag are platform-neutral.

**Project Type**: Packaging / release-coherence deliverable (no runtime app, no emitted-file change
beyond what 204/205 already introduced).

**Performance Goals**: Default scaffold (no git flag) returns promptly with **zero** spawned
processes and **zero** repositories created (SC-003) — the 205 side-effect-free guarantee, verified
against the published package.

**Constraints**: Publish MUST fail loudly on a version/tag collision rather than overwrite
(FR-002, edge cases); the `spec-kit` default output MUST be byte-identical to the prior published
baseline (FR-005); the framework base it declares coherence over MUST itself be the coherent
published snapshot from 204 (FR-009); on any partial failure the cross-repo record MUST show
in-progress, never falsely coherent (FR-010).

**Scale/Scope**: One package version, one annotated tag, one registry row + projection update, one
cross-repo request response, one board-item transition. Four template profiles
(`app`, `headless-scene`, `governed`, `sample-pack`) × three lifecycle values verified.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

**Change classification**: **Tier 1 (contracted)** — it publishes a cross-repo package contract
(`fs-gg-ui-template`) and updates the registry. However, the contract surfaces (lifecycle symbol,
`initGit` opt-in) were already specified and landed by 204/205; this feature ships and records them.
There is **no new F# public surface**, so the Tier-1 `.fsi` / surface-area-baseline obligations are
satisfied vacuously (nothing to update), and the required evidence is *release-verification* evidence
(install/instantiate/byte-diff/reproducibility) plus the registry/issue reconciliation.

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Tests → Impl | ✅ N/A surface | No new public API; FSI gate trivially met. Verification flows through the packed/installed template, exactly the "honest audience" the principle wants. |
| II. Visibility in `.fsi` | ✅ | No `.fs`/`.fsi` changes. Only `.template.package/FS.GG.UI.Template.fsproj` `<Version>` bumps. |
| III. Idiomatic Simplicity | ✅ | Reuses existing pack/validate scripts; no new abstractions, custom operators, or clever machinery. |
| IV. Elmish/MVU boundary | ✅ N/A | No product-runtime stateful/I-O feature is added. The publish→tag→reconcile sequence is a one-shot **operational runbook** expressed as ordered `tasks.md` steps with hard gates, mirroring 204 (which also added no Elmish). |
| V. Test Evidence | ✅ | Existing template pack/install/instantiate + lifecycle/side-effect tests run against the **published** package; per-profile readiness records captured. No assertions weakened; no synthetic evidence required. |
| VI. Observability & Safe Failure | ✅ | Publish fails loudly on version/tag collision (FR-002); partial failure leaves the record in-progress, never falsely coherent (FR-010). |

**Engineering constraints**: net10.0 ✅ · SkiaSharp pins unchanged ✅ · package identity stays
`FS.GG.UI.*` ✅ · pack output `~/.local/share/nuget-local/` ✅ · no new dependency ✅.

**Result**: PASS — no violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/206-publish-template-coherent-set/
├── plan.md              # This file
├── research.md          # Phase 0 — R1..R6 decisions
├── data-model.md        # Phase 1 — release entities & states
├── quickstart.md        # Phase 1 — runnable publish→tag→verify→reconcile guide
├── contracts/           # Phase 1
│   ├── coherent-set.md            # template↔framework snapshot manifest + tag binding
│   ├── publish-verification.md    # PV-1..PV-6 gates the publish must pass
│   └── cross-repo-resolution.md   # registry flip + SDD#1 response + board transition
├── readiness/           # Implementation-phase evidence (created by /speckit-implement)
│   ├── baseline.md
│   ├── publish-evidence.md         # PV-1/PV-3 rollup (sequential gates)
│   ├── pv2-manifest.md             # PV-2 evidence (parallel gate — own file)
│   ├── pv4-side-effect.md          # PV-4 evidence (parallel gate — own file)
│   ├── pv5-lifecycle.md            # PV-5 evidence (parallel gate — own file)
│   ├── profile-*.md
│   ├── reproducibility.md
│   └── cross-repo-resolution.md
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source / artifacts touched (repository root)

```text
.template.package/FS.GG.UI.Template.fsproj   # <Version> 0.1.17-preview.1 -> 0.1.50-preview.1 (only in-repo file edit)
.template.config/template.json               # already carries lifecycle + initGit (204/205) — packed, not edited
template/base/Directory.Packages.props       # already pins FsSkiaUiVersion=0.1.50-preview.1 (204) — verified, not edited
~/.local/share/nuget-local/                   # publish target: FS.GG.UI.Template.0.1.50-preview.1.nupkg produced here

# Git tag (annotated, immutable):
#   fs-gg-ui-template/v0.1.50-preview.1

# Cross-repo (FS-GG/.github, via coordination protocol — NOT edited as another repo's files directly):
#   registry/dependencies.yml          # fs-gg-ui-template row -> coherent release at 0.1.50-preview.1, link tracking
#   docs/registry/compatibility.md     # projection kept in sync
#   FS-GG/FS.GG.SDD#1                   # response citing published version + tag
```

**Structure Decision**: No new source tree. The single in-repo edit is the template package
`<Version>` bump; everything else is packing existing content, tagging, and reconciling the
cross-repo record through the coordination protocol. This matches feature 204's footprint (a pin +
tag + registry flip) rather than a code feature's `src/`+`tests/` layout.

## Complexity Tracking

Not required — Constitution Check passed with no violations.
