# Implementation Plan: Republish the `game`-Profile-Bearing Template (Release Feature 220)

**Branch**: `222-republish-game-template` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/222-republish-game-template/spec.md`

## Summary

Cut a new coherent-set release that carries the already-merged Feature-220 **`game` profile** (commit `b78e72a`, on `main`) onto the org GitHub Packages feed at a single version strictly **> 0.1.53-preview.1** (established next preview: `0.1.54-preview.1`), then flip the `FS-GG/.github` registry entry for `fs-gg-ui-template` from **UNRELEASED → released** at that version and regenerate the `docs/registry/compatibility.md` projection — **publish first, then flip** (FR-007). The producer machinery already exists and is unchanged: `release.yml` `publish-packages` packs the whole `FS.GG.UI.*` set **and** the template at one version `V` from a `v*` / `fs-gg-ui-template/v*` tag and pushes to `nuget.pkg.github.com/FS-GG` with `GITHUB_TOKEN` (`packages: write`); `scripts/derive-template-version.sh` + the Feature-216 reusable dispatch-sender notify Templates. Package visibility was already resolved org-readable in Feature 218 and carries forward. So this feature is **release-cadence + cross-repo-registry**, not new product code: no `.fs`/`.fsi` changes. Resolving this closes the `Ready`, Rendering-owned `contract-change` board item **#33**, clears the `Blocked` mirror on **#31**, and notifies the downstream SDD default-flip **SDD#44**.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature's "app" is the **release/feed/scaffold path**, not the Skia viewer. Every gate below
> (a `> 0.1.53-preview.1` version that *contains* `b78e72a`; the feed serving it to an ordinary
> `packages: read` consumer; the `game` profile being scaffold-selectable; the registry flip following
> a confirmed listing) is provisional until proven against the **real org feed and a real consumer
> token** — a green local pack and a passing `dotnet new` say nothing about what the feed serves or
> whether a foreign token can read it (cf. Features 175 / 216 / 218: deterministic local checks pass
> while the cross-repo path stays red). `/speckit-tasks` MUST front-load a **live feed probe** in the
> Foundational phase (read the actual feed listing for `FS.GG.UI.Template`, confirm `0.1.53-preview.1`
> lacks `b78e72a`, and attempt a consumer-token `dotnet new install`) that confirms or replaces these
> hypotheses **before** any release tag is pushed, and a **post-publish content+scaffold probe** before
> the registry flips.

## Technical Context

**Language/Version**: No application language change. Artifacts are the in-repo version pins (MSBuild `.props`/`.fsproj`), git tags, the already-authored GitHub Actions YAML, and the cross-repo registry YAML in `FS-GG/.github`. F# `net10.0` framework is unchanged; the `game`-profile producer code shipped in Feature 220.

**Primary Dependencies**: Existing `release.yml` (`publish-packages` job, `GITHUB_TOKEN` `packages: write`, triggered by `v*` tag push / `workflow_dispatch`), `scripts/derive-template-version.sh` + the Feature-216 org reusable dispatch-sender (`FS-GG/.github/.github/workflows/dispatch-sender.yml`) that notifies Templates, the repo's `speckit-merge` flow (version bump + tag), `gh` CLI / GitHub Packages org feed, and the `FS-GG/.github` `registry/dependencies.yml` + `docs/registry/compatibility.md` projection.

**Storage**: N/A (no datastore). The "state" is the org feed's served versions, the registry entry's released coordinates, and the Coordination board / issue states.

**Testing**: Evidence is **live cross-repo proof**, not new unit tests (consistent with the Tier-1 release Features 215 / 216 / 218): feed-listing query (a `FS.GG.UI.Template` version `> 0.1.53-preview.1` is served), **content** verification (`git merge-base --is-ancestor b78e72a <release-tag>` and the packed template actually carries the `game` choice), consumer-token `dotnet new install` (assert exit 0 / no exit 103), scaffold with the `game` profile selected (assert accepted / no missing-profile error), and the generated `game` product building + passing governance with **zero** `GovernanceTests` edits (FR-004). The existing release-only gates (`package-tests`, `template-product-tests`) run in CI before publish; no assertion is weakened. The three non-game profiles' diff-identity (FR-005, SC-003) reuses Feature 220's diff-verified baseline.

**Target Platform**: GitHub Actions `ubuntu-latest` (canonical repo `FS-GG/FS.GG.Rendering` only — the `if: github.repository == …` guard), the org GitHub Packages NuGet feed (`nuget.pkg.github.com/FS-GG`).

**Project Type**: Single repo; release/packaging + cross-repo coordination feature (no `src/` change).

**Performance Goals**: N/A (a release is event-driven, not throughput-bound).

**Constraints**:
- **Monotonic version** — published version MUST be strictly `> 0.1.53-preview.1` (FR-001); expected `0.1.54-preview.1` (the next preview in the established cadence), but the merge/release flow fixes the exact literal — the plan does not hard-code it beyond the `>` constraint (Feature 204/218 precedent: the packer fixes the value). NuGet feeds are append-only; never re-tag `0.1.53-preview.1`.
- **Content gate** — the released `FS.GG.UI.Template` MUST contain Feature 220 (`git merge-base --is-ancestor b78e72a <release-tag>` true); cut from a `main` commit that contains `b78e72a` (already on `main`). Verified by content inspection, not just the version string (SC-002, edge case).
- **Coherent set** — every `FS.GG.UI.*` package *and* the template pack at the same `V` (FR-001); the two in-repo pins (`template/base/Directory.Packages.props` `<FsGgUiVersion>`, `.template.package/FS.GG.UI.Template.fsproj` `<Version>`, both currently `0.1.53-preview.1`) must both move to `V` so the `template-product-tests` gate's local-feed restore resolves.
- **Publish-before-flip ordering** — FR-007: the registry flip MUST follow a confirmed feed listing, so the contract record never claims "released" for a version the feed 404s.
- **Profile additivity** — the `game` profile is additive (Feature 220); the four non-`game` profiles (`app`, `headless-scene`, `governed`, `sample-pack`) MUST be unaffected (FR-005); `headless-scene`/`governed`/`sample-pack` stay byte-identical to Feature 220's diff-verified output.
- **No new product code / surface** — FR-010: reuse the existing producer machinery; no new `FS.GG.UI.*` public surface, no new workflow.

**Scale/Scope**: One coherent-set release (every `FS.GG.UI.*` packable + 1 template package), one registry entry update + compatibility projection in `FS-GG/.github`, and the closure of #33 + the clearing of #31's `Blocked` mirror + a notification to SDD#44. No package-visibility action (resolved in Feature 218).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change Classification — Tier 1 (contracted change).** The feature advances the `fs-gg-ui-template` cross-repo contract's released coordinates (`version` / `package-version` / `package-tag`) and flips the `game`-profile "UNRELEASED until the next republish" note → released + the `coherence` entry. Per ADR-0001 it MUST update the registry (`FS-GG/.github` `registry/dependencies.yml` + compatibility projection) as part of resolution (FR-006), landing as a `contract-change` PR.

**No F# public-surface impact.** No `.fs`/`.fsi` is added/removed/changed; the producer code shipped in Feature 220 (commit `b78e72a`). Therefore:
- **Principle I (Spec → FSI → Semantic Tests → Implementation)** — N/A for new surface; there is no new API to sketch in FSI. The "design through use" here is the consumer's `dotnet new install` + `game`-profile scaffold, validated live (quickstart).
- **Principle II (Visibility in `.fsi`)** and **surface-area baselines** — N/A; no module surface changes, so no baseline updates and no surface-drift implications. (Feature 220 already made the entrypoint assertion family-agnostic with zero `GovernanceTests` edits; this feature merely publishes it.)
- **Principle IV (Elmish/MVU boundary)** — N/A; no stateful F# workflow is added (the release/dispatch I/O lives in already-authored, fail-loud workflows). The `game` profile's own MVU skeleton is Feature 220's, not new here.
- **Principle V (Test Evidence Is Mandatory)** — satisfied by **real** evidence (live feed/install/scaffold proof + content ancestry check + registry update); no synthetic evidence, no weakened assertion. A gate that cannot run live (the publish requires a real tag push by an operator with release rights; the registry PR requires `FS-GG/.github` merge rights) is **disclosed and deferred**, not faked.
- **Principle VI (Observability and Safe Failure)** — preserved: `release.yml` and the dispatch path already fail loud (`set -euo pipefail`, canonical-repo guards, `derive-template-version.sh` refuses to send on a bad ref). This feature adds no new silent path.

**Engineering constraints** — package identity stays `FS.GG.UI.*` (unchanged); no new dependency; no code; `net10.0` unchanged; pack output / release path is the existing `release.yml`. **Gate: PASS — no violations, Complexity Tracking left empty.**

## Project Structure

### Documentation (this feature)

```text
specs/222-republish-game-template/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output — version/tag-set, content-gate mechanism, evidence model, registry delta, ordering
├── data-model.md        # Phase 1 output — entities (coherent set, version, tag-set, registry entry, board/issues) + state transitions
├── quickstart.md        # Phase 1 output — runnable live-validation guide (feed / content / no-103 / game-scaffold / governance / registry / closure)
├── checklists/
│   └── requirements.md  # spec-quality checklist (all items pass)
├── contracts/
│   └── fs-gg-ui-template-release.md   # the contract delta: released coordinates + game-profile release-state + coherence flip
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

No `src/` changes. The files this feature *touches* (version bump only — owned by the merge/release flow) and the evidence sources it *reads*:

```text
.template.config/template.json                 # Feature 220 `game` profile choice — already on main; READ-ONLY here (verify it packs & is selectable)
template/base/Directory.Packages.props         # <FsGgUiVersion> pin (0.1.53-preview.1) → bump to V at merge/release
.template.package/FS.GG.UI.Template.fsproj     # <Version> pin (0.1.53-preview.1) → bump to V at merge/release; packs .template.config + content
.github/workflows/release.yml                  # publish-packages (READ-ONLY — already authored; triggered by v* tag push / workflow_dispatch)
scripts/derive-template-version.sh             # dispatch version derivation (READ-ONLY)

# Cross-repo (FS-GG/.github), updated as the contract-change landing point (publish-then-flip, FR-007):
registry/dependencies.yml                      # fs-gg-ui-template: version/package-version/package-tag → V; game-profile UNRELEASED → released; coherence flip
docs/registry/compatibility.md                 # regenerated projection of the above (no stale 0.1.53-preview.1 for this surface)
```

**Structure Decision**: This is a release/coordination feature, not a code feature — there is no module to place. Work is (1) front-load a **live feed probe** confirming the current served `0.1.53-preview.1` lacks `b78e72a` and the `game` profile is not yet feed-selectable; (2) ensure the merge/release flow bumps the two version pins to `V (> 0.1.53-preview.1)` from a `main` commit containing `b78e72a` and pushes the release tag-set so `publish-packages` packs+pushes the coherent set; (3) confirm the org feed serves `V` and the packed template **content** carries Feature 220 (ancestry + `game` choice present); (4) prove the consumer path live (no 103; `game` scaffold accepted; generated `game` product builds + passes governance with zero `GovernanceTests` edits; three non-game profiles byte-identical); (5) **only then** update the `FS-GG/.github` registry + projection (publish-before-flip); (6) close #33 with the version + registry PR linked, move the board item to `Done`, clear #31's `Blocked` mirror, and notify SDD#44.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
