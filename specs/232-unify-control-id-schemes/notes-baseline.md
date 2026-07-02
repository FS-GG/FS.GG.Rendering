# Feature 232 — test baseline & result ledger

**Branch**: `232-unify-control-id-schemes` · **Date**: 2026-07-02

## Path-derivation SSOT (T002)

Every newly path-threaded walk reuses the canonical derivation from
`Control.eventBindingsOf`/`collectBoundsWith`: **root `"0"`, child *i* → `path + "." + string i`**.
Applied identically in `Focus.order`, `ControlRuntime.applyRuntimeVisualState`/`finalVisualState`/
`targetedWalk`/`applyScrollOffsets`, `RetainedRender.retainedCanonicalId`, and the Elmish
`retainedIdOfControl`.

## Pre-existing failure (KNOWN — not a feature-232 regression)

- **`Rendering.Harness.Tests` → `Feature168 SkillInventory.repository parity has no unresolved
  findings`** — expected `Passed`, actual `WarningStatus`.
- **Verified pre-existing**: `git stash --include-untracked` (removing ALL feature-232 changes) → the
  same test still fails identically on the clean tree (Failed 1 / Passed 3). It scans the repo's skill
  inventory (`SkillParity.runCheck` over `.claude`/template/codex/ant skill surfaces); feature 232
  touches **zero** skill/template/tool files. Unrelated to this feature.

## Full-suite result WITH feature 232 (`dotnet test FS.GG.Rendering.slnx`)

| Project | Result |
|---|---|
| Controls.Tests | ✅ 957 passed, 1 skipped (was 946; +8 new Feature232 + 3 fixed Feature072 goldens) |
| Elmish.Tests | ✅ 214 passed, 17 skipped (was 211; +3 new Feature232) |
| Diagnostics.Tests | ✅ 14 passed |
| KeyboardInput.Tests | ✅ 20 passed |
| Layout / Scene / Canvas / Lib / Testing / Symbology(×3) / SkiaViewer / SymbologyBoard / Build / Smoke | ✅ all passed |
| Rendering.Harness.Tests | ⚠️ 211 passed, **1 pre-existing SkillInventory warning** (see above) |

**Net**: feature 232 introduces **0 regressions**. Every previously-green test stays green; the only
red is the pre-existing skill-inventory parity warning, provably independent of this change.

## Notes

- `Package.Tests` is not part of the default `slnx` test run (package-feed proof; not exercised here).
- No `dotnet test` concurrency on the same project/config (repo evidence rule) — suites run sequentially
  or on distinct projects.
