# Implementation Plan: Publish & Make-Readable the productName-Enabled Template

**Branch**: `218-publish-readable-template` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/218-publish-readable-template/spec.md`

## Summary

Land the **two coupled gates** on the SDD-orchestrated composition path (FS-GG/FS.GG.Rendering **#29** publish + **#26** visibility) as one coherent release: cut a `FS.GG.UI.Template` coherent-set version **> 0.1.52-preview.1** that carries the already-merged Feature-217 `--productName` symbol (commit `6df0d39`) onto the org feed, **and** make that package readable by ordinary org-consumer CI tokens (visibility `private → internal`). The two are independent failure modes — a new-but-private version still fails consumer install with **exit 103**, and a readable-but-old version still rejects `--productName` with **exit 127** — so "done" requires a single version that is *both* Feature-217-bearing *and* feed-readable (FR-004). The producer machinery already exists (`release.yml` `publish-packages` packs the whole `FS.GG.Rendering.slnx` + `.template.package/FS.GG.UI.Template.fsproj` at one version `V` and pushes to `nuget.pkg.github.com/FS-GG` with `GITHUB_TOKEN`; `template-dispatch.yml` notifies Templates). So this feature is **release-cadence + package-visibility + cross-repo-registry**, not new product code: no `.fs`/`.fsi` changes.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature's "app" is the **release/feed/scaffold path**, not the Skia viewer. Every gate below
> (exit-127 fixed by Feature 217 once published; exit-103 fixed by `internal` visibility; the merge
> bump producing `0.1.53-preview.1`) is provisional until proven against the **real org feed and a real
> consumer token** — a green local pack and a passing `dotnet new` say nothing about what the feed
> serves or whether a foreign token can read it (cf. Feature 175/216: deterministic local checks pass
> while the cross-repo path stays red). `/speckit-tasks` MUST front-load a **live feed/visibility probe**
> in the Foundational phase (read the actual feed listing + `gh api … visibility` for the package, and
> attempt a consumer-token install) that confirms or replaces these hypotheses **before** any release
> tag is pushed.

## Technical Context

**Language/Version**: No application language change. Artifacts are GitHub Actions YAML (already authored), the in-repo version pins (MSBuild `.props`/`.fsproj`), git tags, GitHub Packages settings, and the cross-repo registry YAML in `FS-GG/.github`. F# `net10.0` framework is unchanged.

**Primary Dependencies**: Existing `release.yml` (publish-packages job, `GITHUB_TOKEN` `packages: write`), `template-dispatch.yml` (Feature 216 reusable App-token sender), `scripts/derive-template-version.sh`, the repo's `speckit-merge` flow (version bump + tag), `gh` CLI / GitHub Packages org settings, and the `FS-GG/.github` `registry/dependencies.yml` + `docs/registry/compatibility.md` projection.

**Storage**: N/A (no datastore). The "state" is the org feed's served versions, the package's visibility flag, and the registry entry.

**Testing**: Evidence is **live cross-repo proof**, not new unit tests (consistent with the Tier-1 release Features 215/216): feed-listing query, consumer-token `dotnet new install` (assert exit 0 / no 103), `dotnet new fs-gg-ui --productName <P>` (assert exit 0 / no 127). The existing release-only gates (`package-tests`, `template-product-tests`) run in CI before publish; no assertion is weakened.

**Target Platform**: GitHub Actions `ubuntu-latest` (canonical repo `FS-GG/FS.GG.Rendering` only — the `if: github.repository == …` guard), the org GitHub Packages NuGet feed, and org package settings.

**Project Type**: Single repo; release/packaging + cross-repo coordination feature (no `src/` change).

**Performance Goals**: N/A (a release is event-driven, not throughput-bound).

**Constraints**:
- **Monotonic version** — published version MUST be strictly `> 0.1.52-preview.1` (FR-001); expected `0.1.53-preview.1` (the merge bump from 0.1.52), but the merge/release flow fixes the exact literal — the plan does not hard-code it beyond the `>` constraint (Feature 204 precedent: the packer fixes the value).
- **Coherent set** — every `FS.GG.UI.*` package *and* the template pack at the same `V` (FR-006); the two in-repo pins (`template/base/Directory.Packages.props` `<FsGgUiVersion>`, `.template.package/FS.GG.UI.Template.fsproj` `<Version>`) must both move to `V` so the `template-product-tests` gate's local-feed restore resolves.
- **Surface-additive only** — no `fs-gg-ui-template` contract *surface* change; `productName` was specified in Feature 217 and is merely *exposed* by publishing (FR-009).
- **No half-landing** — FR-004: both gates or not done.

**Scale/Scope**: One coherent-set release (17 `FS.GG.UI.*` packables + 1 template package), one package-visibility flip, one registry entry update in `FS-GG/.github`, and the closure of two issues + two board items.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change Classification — Tier 1 (contracted change).** The feature changes the `fs-gg-ui-template` cross-repo contract's released coordinates (`version` / `package-version` / `package-tag`), flips the productName "UNRELEASED on the feed" note to released, and changes the package's consumer-reachability (visibility). Per ADR-0001 it MUST update the registry (`FS-GG/.github` `registry/dependencies.yml` + compatibility projection) as part of resolution (FR-008).

**No F# public-surface impact.** No `.fs`/`.fsi` is added/removed/changed; the producer code shipped in Feature 217. Therefore:
- **Principle I (Spec → FSI → Semantic Tests → Implementation)** — N/A for new surface; there is no new API to sketch in FSI. The "design through use" here is the consumer's `dotnet new install` + `--productName` invocation, validated live (quickstart).
- **Principle II (Visibility in `.fsi`)** and **surface-area baselines** — N/A; no module surface changes, so no baseline updates and no surface-drift implications.
- **Principle IV (Elmish/MVU boundary)** — N/A; no stateful F# workflow is added (the release/dispatch I/O lives in already-authored, fail-loud workflows).
- **Principle V (Test Evidence Is Mandatory)** — satisfied by **real** evidence (live feed/install/scaffold proof + registry update); no synthetic evidence, no weakened assertion. A gate that cannot run live (e.g. the publish requires a real tag push by an operator with rights) is **disclosed and deferred**, not faked.
- **Principle VI (Observability and Safe Failure)** — preserved: `release.yml` and `template-dispatch.yml` already fail loud (`set -euo pipefail`, canonical-repo guards, `derive-template-version.sh` refuses to send on a bad ref). This feature adds no new silent path.

**Engineering constraints** — package identity stays `FS.GG.UI.*` (unchanged); no new dependency; no code; `net10.0` unchanged; pack output / release path is the existing `release.yml`. **Gate: PASS — no violations, Complexity Tracking left empty.**

## Project Structure

### Documentation (this feature)

```text
specs/218-publish-readable-template/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output — version/tag-set, visibility mechanism, evidence model, registry delta
├── data-model.md        # Phase 1 output — entities (version, tag-set, visibility, registry entry, issues/board) + state transitions
├── quickstart.md        # Phase 1 output — runnable live-validation guide (feed / no-103 / no-127 / registry / closure)
├── contracts/
│   └── fs-gg-ui-template-release.md   # the contract delta: released coordinates + visibility + coherence note
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

No `src/` changes. The files this feature *touches* (version bump only — owned by the merge/release flow) and the evidence sources it *reads*:

```text
.template.config/template.json                 # Feature 217 productName symbol — already on main; READ-ONLY here (verify it packs)
template/base/Directory.Packages.props         # <FsGgUiVersion> pin → bump to V at merge/release
.template.package/FS.GG.UI.Template.fsproj     # <Version> pin → bump to V at merge/release; packs .template.config + content
.github/workflows/release.yml                  # publish-packages (READ-ONLY — already authored; triggered by tag push)
.github/workflows/template-dispatch.yml        # notify Templates (READ-ONLY — fires on fs-gg-ui-template/v* tag)
scripts/derive-template-version.sh             # dispatch version derivation (READ-ONLY)

# Cross-repo (FS-GG/.github), updated as the contract-change landing point:
registry/dependencies.yml                      # fs-gg-ui-template: version/package-version/package-tag → V; productName feed-note → released; coherence
docs/registry/compatibility.md                 # projection of the above
```

**Structure Decision**: This is a release/coordination feature, not a code feature — there is no module to place. Work is (1) ensure the merge/release flow bumps the two version pins to `V` and pushes the release tag-set, (2) confirm the org feed serves `V` and the packed template carries Feature 217, (3) flip the package visibility `private → internal`, (4) prove the combined consumer path (no 103, no 127), (5) update the `FS-GG/.github` registry + projection, and (6) close #29/#26 and move the two board items to `Done`.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
