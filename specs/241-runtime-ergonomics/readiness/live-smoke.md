# Readiness evidence — Feature 241 (runtime ergonomics polish)

Date: 2026-07-04. Branch `241-runtime-ergonomics`. dotnet 10.0.301.

## Summary

| Item | Verified | Evidence |
|---|---|---|
| §3.5 `Cmd.none`/`Sub.none` exist + laws | ✅ real | FSI + Expecto (4/4) |
| §3.5 surface baseline drift gate | ✅ real | `SurfaceAreaTests` 40/40 green with +2 baseline lines |
| §3.4 collision guidance present | ✅ real | `grep KeyboardMsg product.md` hits |
| §3.6 `measureText` surfaced | ✅ real | `grep measureText product.md` + `fs-gg-scene` skill hit; helper already packed |
| skill-manifest digests | ✅ real | regen + `--check` up to date; `Feature231/224/225` green |
| Full Package.Tests | ✅ real | 187/187 |
| Full Elmish.Tests | ✅ real | 219 pass / 17 pre-existing skips |
| **Live scaffold + build of a `game` product** | ⚠️ **DEFERRED — environment-limited** | see caveat |

## §3.5 library + laws (real)

`FS.GG.UI.Controls.Elmish.Authoring.{Cmd,Sub}.none` added. FSI against the freshly built
`bin/Debug` assembly:

```
Cmd.none = [] : true
Sub.none empty : true
productMessages Cmd.none = [] : true
```

Expecto `adapter no-op aliases (§3.5)` — 4/4 passed (`tests/Elmish.Tests/AdapterCmdSubNoneTests.fs`).
Resolution proof: opening `FS.GG.UI.Controls.Elmish` + `…Authoring` (the exact opens added to the
product template) makes `Cmd.none`/`Sub.none` resolve unambiguously — the package's own `open Elmish`
is NOT in a generated product's scope, so there is no Fable-`Elmish.Cmd` clash.

## Surface + gates (real)

- `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` gained exactly
  `FS.GG.UI.Controls.Elmish.Authoring.Cmd` and `…Authoring.Sub` (`git diff` = +2 lines; no other
  baseline drifted). `SurfaceAreaTests` 40/40.
- Skill bodies `fs-gg-scene` + `fs-gg-elmish` edited → `skill-manifest.json` regenerated (2 digests
  changed) → `generate-skill-manifest.fsx --check` = up to date. `Feature231/224/225` green.
- Full `Package.Tests` 187/187; full `Elmish.Tests` 219 pass / 17 skip (pre-existing GL/env skips).

## ⚠️ Live scaffold+build caveat (deferred to release — NOT a defect)

A real `dotnet new fs-gg-ui --profile game` + `dotnet build` was **not** run to green in this
environment, and could not be by construction: the template pins the **released**
`FS.GG.UI.* 0.1.62-preview.1`, which **predates** the new `…Controls.Elmish.Authoring` namespace this
feature adds. A product scaffolded against that feed cannot resolve `Cmd.none`/`Sub.none` until the
feature's packages are republished at the next coherent version and the template pin is bumped — the
standard **publish-before-flip** release step (same shape as Feature 240 wiring Canvas + bumping).
The repo's live scaffold+build loop is itself env-gated (`FS_GG_RUN_LIFECYCLE_VALIDATION=1`), not part
of default CI. No local package feed is configured in this environment.

**Substitute evidence** for the template edits: the `[] → Cmd.none` / `Sub.none` substitutions are
type-identical, and the FSI/Expecto proof above confirms the exact opens the template uses resolve
the aliases. **Residual to confirm at merge/release**: after `speckit-merge` repacks the local feed at
the bumped version and re-pins the template, run `dotnet new fs-gg-ui --profile game` + `dotnet build`
to close US1 (T006), US2 (T009), and US3 (T016) live.
