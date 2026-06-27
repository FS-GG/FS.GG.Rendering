# Implementation Plan: Make the FS.GG.UI Version-Staleness Bug Class Structurally Impossible

**Branch**: `209-version-staleness-guard` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/209-version-staleness-guard/spec.md`

## Summary

Drift between the single FS.GG.UI version source (`<FsGgUiVersion>` in
`template/base/Directory.Packages.props`), the published coherent snapshot it claims to match
(`fs-gg-ui/v<V>` git tags + local feed), the full `FS.GG.UI.*` member set (`src/**`), the template's
11 consumed pins, and the BOM/metapackage exact `[V]` pins (`src/Meta/FS.GG.UI.nuspec`) is today
**silent** — it only surfaces as a downstream consumer's broken build (Feature 204,
`FS-GG/FS.GG.Rendering#1`). This feature makes that drift a **loud, local, automatic failure in this
repo's own merge-blocking gate**, before any consumer scaffolds a product.

**Technical approach.** Add an env-free **structural coherence verdict** as a fast script
(`scripts/validate-version-coherence.fsx`) wired into `.github/workflows/gate.yml` as a new
merge-blocking step — mirroring the existing surface-baseline-drift step and the two-layer pattern of
`scripts/validate-bom-consumer.fsx`. The verdict re-derives, from the repo + pushed git tags, that:
the single literal is well-formed and present exactly once; all template pins resolve through
`$(FsGgUiVersion)` (no hardcoded literal); the packable `FS.GG.UI.*` set in `src/**` equals the BOM
nuspec dependency set equals the template's consumed pin set (modulo the published-16 / consumed-11
distinction); the BOM uses the single `[$version$]` exact-bracket token; and `FsGgUiVersion`
corresponds to an existing `fs-gg-ui/v<V>` tag and does **not lag** the latest such tag (version
comparison, not string). It exits non-zero naming the specific mismatch expected-vs-actual.

A **restore-grounded proof** (FR-008, anti-text-grep) packs the framework + BOM from source to a
throwaway feed at the pinned version and restores `FS.GG.UI@V` in a clean consumer, asserting the
complete member set resolves to exactly `V` — the cheap "the pin resolves to the complete real
coherent set" proof runs in the gate; the deeper full **generate→restore→build of a product from the
template** continues on the release lane (`release.yml` Package.Tests / product-from-template).

This is a **Tier 2** change (versioning/validation machinery + CI): no `src/**` public surface change,
no `.fsi`/baseline change, no runtime/rendering behavior change.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The "drift is silent" claim and the "structural derivation already collapses to one literal"
> hypothesis below are provisional. `/speckit-tasks` MUST schedule an **early live verification** in
> the Foundational phase (right after the coherence-surface map, before building the guard): run a
> pack-from-source generate→restore→build at the current pinned version to confirm today's tree is
> actually coherent and restorable, and deliberately re-introduce the Feature-204 drift to confirm it
> is *currently silent*. Do not build the guard on the unverified assumption that derivation is
> already fully structural — a stray hardcoded pin would invalidate it.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; guard authored as `dotnet fsi` scripts (`.fsx`) +
optional xUnit wrapper in `tests/Package.Tests`.

**Primary Dependencies**: existing repo machinery only — MSBuild property `$(FsGgUiVersion)`, NuGet
pack/restore, `git tag`, `System.Text.RegularExpressions`, `NuGet.Versioning`-style preview-aware
comparison (implemented locally if a package reference is undesirable in a script). No new runtime
dependency.

**Storage**: N/A. Inputs are repo files (`template/base/Directory.Packages.props`,
`src/Meta/FS.GG.UI.nuspec`, `src/**/*.fsproj`), pushed git tags (`fs-gg-ui/v*`), and a throwaway
pack feed under the system temp dir. Authority for "latest published coherent set" = the
`fs-gg-ui/v<V>` tag namespace (per spec Assumptions); the local feed `~/.local/share/nuget-local/`
is the dev-side feed, reproduced in CI by pack-from-source.

**Testing**: env-free verdict-core script that exits non-zero on drift (gate step, like
`refresh-surface-baselines.fsx`); env-gated live restore proof (`FS_GG_RUN_*=1`, mirroring
`validate-bom-consumer.fsx`); optional `tests/Package.Tests` xUnit wrapper for the release lane and
local dev.

**Target Platform**: GitHub Actions `ubuntu-latest` (gate + release lanes) and local `dotnet fsi`.

**Project Type**: F# UI-framework monorepo — build/release/validation machinery + CI workflow change.

**Performance Goals**: structural verdict completes in well under the existing gate's overhead
(text/git only, no pack). Restore-grounded proof in the gate adds one Release pack + one clean
restore (target a few minutes, comparable to the existing Debug build step), keeping the gate "fast,
deterministic" per `docs/ci/cadence-map.md`.

**Constraints**: must block merge to `main` (FR-006); must fail **independently of**
`warnings-as-errors` consumer policy (FR-004) — i.e. the in-repo verdict compares pins directly
rather than relying on NU1605/NU1608 loudness; must name the specific location expected-vs-actual
(FR-007); must not accept a text-grep alone as coherence evidence (FR-008); preview-aware version
ordering, not string compare (Edge Cases).

**Scale/Scope**: 16 published `FS.GG.UI.*` members + 1 BOM metapackage; 11 consumed pins in the
template; 2 current snapshot tags (`fs-gg-ui/v0.1.50-preview.1`, `fs-gg-ui/v0.1.51-preview.1`);
current `FsGgUiVersion = 0.1.50-preview.1`; repo-root `<Version> = 0.1.0-preview.1` (decoupled).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment |
|-----------|------------|
| I. Spec → FSI → Semantic Tests → Implementation | **N/A to public surface** — no new public F# module. The "interface" is the guard's CLI/exit-code + failure-message contract, captured in `contracts/`. Verdict logic is exercised through `dotnet fsi` the same way a maintainer/CI runs it (the honest audience), and through forced-drift fixtures (re-introduced 204 state, half-bump). ✅ |
| II. Visibility in `.fsi` | **No `.fs` public modules added.** Guard ships as `.fsx` scripts + an optional `Package.Tests` xUnit wrapper. No `.fsi`, no access modifiers. ✅ |
| III. Idiomatic Simplicity | Plain F#: regex reads of existing literals, set comparisons for membership parity, a small preview-aware version comparator. No SRTP/reflection/type-providers/custom operators. Any `mutable` in the comparator disclosed at use site. ✅ |
| IV. Elmish/MVU boundary | The guard is a **pure verdict** over file/tag inputs producing a result + messages; the only I/O (read files, `git tag`, pack/restore) is at the edge of the script. No durable stateful workflow → no Elmish ceremony required (Principle IV exempts simple pure functions). The verdict is structured as data (a `Verdict` record) computed by a pure function, interpreted (printed / exit-coded) at the edge — honoring the separation. ✅ |
| V. Test Evidence Is Mandatory | Tests fail before / pass after: forced-drift fixtures (stale pin = 204 case; one-BOM-pin half-bump; unwired new member; phantom version with no tag) MUST make the verdict red; the coherent baseline MUST pass. Live restore proof prefers **real** pack/restore over mocks. Any unavoidable synthetic (e.g. simulating a not-yet-pushed tag) carries the `Synthetic` token + use-site disclosure. ✅ |
| VI. Observability and Safe Failure | The verdict **fails loud** with structured, actionable messages (named location, expected-vs-actual) and never swallows a mismatch; a missing tag / undefined property fails fast rather than reporting partial success (FR-007/FR-008). ✅ |
| Change Classification | **Tier 2** (internal/machinery): no public API, no inter-package contract change, no observable runtime behavior change. The cross-repo `fs-gg-ui-version` / `fs-gg-ui-bom` registry contract is *upheld* (FR-010), not modified. ✅ |
| Engineering Constraints | F#/.NET only; no new runtime dependency; pack output stays at `~/.local/share/nuget-local/`; repo-owned check is narrow and pays for itself (it protects the package-coherence contract that Feature 204 proved is otherwise discovered downstream) — justification recorded per the Development Workflow §. ✅ |

**Gate result: PASS** — no violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/209-version-staleness-guard/
├── spec.md              # Feature specification (input)
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — design decisions + rationale
├── data-model.md        # Phase 1 — coherence entities + derivation/lockstep rules
├── quickstart.md        # Phase 1 — run the guard; reproduce 204 drift & half-bump
├── contracts/
│   └── version-coherence-guard.md   # Phase 1 — verdict CLI/exit-code + message + gate-step contract
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
scripts/
├── validate-version-coherence.fsx      # NEW — env-free structural verdict-core (gate step) +
│                                        #       env-gated (FS_GG_RUN_VERSION_COHERENCE_SMOKE=1)
│                                        #       restore-grounded proof; writes a readiness report
├── validate-bom-consumer.fsx           # EXISTING — reused for the BOM clean-consumer restore layer
└── refresh-surface-baselines.fsx       # EXISTING — pattern reference for a gate script step

template/base/
└── Directory.Packages.props            # READ-ONLY input: <FsGgUiVersion> (line 9) + 11 pins

src/Meta/
└── FS.GG.UI.nuspec                     # READ-ONLY input: 16 exact [$version$] dependency pins

.github/workflows/
├── gate.yml                            # EDIT — add merge-blocking "Version coherence guard" step
│                                       #        (+ ensure tags are fetched: fetch-depth: 0 / fetch-tags)
└── release.yml                         # (unchanged baseline; deeper generate→restore→build remains here)

tests/Package.Tests/                    # OPTIONAL — Feature209VersionCoherenceTests.fs xUnit wrapper
                                        #            (release-lane / local dev; re-derives verdict env-free)

specs/209-version-staleness-guard/readiness/
└── version-coherence.md                # NEW — regenerated verdict report (provenance: verdict-core | live)
```

**Structure Decision**: Extend, don't replace. The guard lands as one new script
(`scripts/validate-version-coherence.fsx`) following the established env-free-verdict-core +
env-gated-live-proof shape of `validate-bom-consumer.fsx`, wired into the existing `gate.yml` as a
new merge-blocking step (the same place surface-baseline drift is enforced). No new project, no
`src/**` change, no `.fsi`/baseline change. The repo-root `<Version>` (`0.1.0-preview.1`) stays
**decoupled** per spec Assumptions and is explicitly *not* part of the lockstep set (the verdict
does not compare it); if that assumption is later overturned it joins the set under FR-005.

## Complexity Tracking

> No Constitution Check violations — this section intentionally empty.
