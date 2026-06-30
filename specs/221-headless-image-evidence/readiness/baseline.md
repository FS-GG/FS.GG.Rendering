# Baseline — no-regression record (T002 / T026)

**Date**: 2026-06-30 · **Branch**: `221-headless-image-evidence`

## Environment caveat (NU1403 lockfile)

A full-graph `scripts/baseline-tests.fsx` run is blocked in this environment by the documented
**NU1403 FSharp.Core lockfile** issue: the committed `packages.lock.json` files pin a content hash the
configured feeds no longer serve, so a locked-mode restore of the `src/*` + `tests/*` graph fails for
~17/21 projects with "build/restore failure". These are **not** real regressions — they are a stale-
lockfile artifact. Workaround (per repo memory): `dotnet restore <proj> --force-evaluate
/p:RestoreLockedMode=false`, then `dotnet test <proj> --no-restore`, then revert the rewritten lockfiles
with `git checkout -- src/*/packages.lock.json tests/*/packages.lock.json`.

Because of this, the baseline is recorded for the **projects this feature actually touches**, each
restored via the workaround and run green. All other projects' status is the pre-existing NU1403 state,
unchanged by this feature.

## Affected-project results (workaround restore, `-c Debug`)

| Project | Pre-change | Post-change | Notes |
|---|---|---|---|
| `tests/Scene.Tests` | green | **78 passed / 0 failed** | Hosts the `renderPng`/Evidence behaviour change; updated to the new honest-failure + `renderHash` determinism contract. |
| `tests/SkiaViewer.Tests` | 207 passed | **213 passed / 0 failed** | +6 new `HeadlessImageEvidence` tests (US1 determinism/dims/non-blank, cross-instance, concurrency; US3 failure/size/disclosure). No existing test regressed. |
| `tests/Controls.Tests` | 949 passed / 1 skipped | **949 passed / 1 skipped** | `renderPng` consumers (Feature091/092) are resilient (capability-hash sidecar path); stale "capability-hash, not a pixel encoder" notes corrected. |
| `tests/Package.Tests` (filter `Surface`) | 34 passed / 1 failed | **34 passed / 1 failed** | The 1 RED is the pre-existing known `FS.GG.UI.Build engine baseline` failure (Build assembly not built in the Debug lane) — not a regression. See `surface-baseline.md`. |

## Conclusion

No new reds attributable to this feature. The only RED in the affected set is the documented pre-existing
Package.Tests Build-engine baseline. T026 (post-change) equals T002 (pre-change) for every affected
project.
