# T017 — Gate-condition probes (SC-002 / C-2) (Feature 213)

Date: 2026-06-28. The unified gate lives in the canonical `Directory.Build.props`:
`<RestoreLockedMode Condition="'$(GITHUB_ACTIONS)' == 'true' And Exists('$(MSBuildProjectDirectory)/packages.lock.json')">true</RestoreLockedMode>`.

## T015 — no stale local gate

`grep -nE "ContinuousIntegrationBuild|<RestoreLockedMode|RestorePackagesWithLockFile" Directory.Build.local.props`
→ **zero hits**. The "Restore (211)" group was fully removed by T008, so the effective gate is the
canonical `GITHUB_ACTIONS` spelling — no last-import local CIB gate can silently win (research R2/R5).

## Probe 1 — locked in CI

```
GITHUB_ACTIONS=true dotnet restore FS.GG.Rendering.slnx --locked-mode
→ "All projects are up-to-date for restore."  exit 0
```
With the CI signal set and lockfiles present, locked restore engages and succeeds against the
committed lockfiles. ✅

## Probe 2 — not blocked locally

```
env -u GITHUB_ACTIONS dotnet restore FS.GG.Rendering.slnx
→ Restored …  exit 0
```
With no CI signal, restore is not blocked (and on a fresh clone with no lockfile a first restore would
bootstrap it via `And Exists(lockfile)`). ✅

## SC-005 — cross-repo gate match (by construction)

The `'$(GITHUB_ACTIONS)' == 'true'` gate string lives in the canonical `Directory.Build.props` taken
**verbatim** from the shared FS-GG/.github source (drift-clean `--check`, T010 exit 0). Because all
four FS-GG repos consume the same canonical file, Rendering's gate spelling equals the others' by
construction — no sibling-repo fetch is needed. Rendering is no longer the lone `ContinuousIntegrationBuild`
outlier (ADR-0006).

> Producer note: adopting this canonical file surfaced a producer-side defect — its XML header comment
> contained `` `--check` `` (an illegal `--` inside an XML comment), which MSBuild rejects (`MSB4024`),
> blocking every restore/build. This was **already fixed upstream** on `FS-GG/.github` `main` (b00433c /
> PR #30 / closes #29), which also hardened `sync-build-config.sh` to parse, not just `diff`. The local
> `.github` checkout used here was on a pre-fix branch, so the broken file was hit first; Rendering's
> managed files are byte-identical to producer `main` (post-fix). My duplicate #31 was closed → #29.
