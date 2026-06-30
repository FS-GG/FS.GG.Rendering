# Quickstart end-to-end evidence (T022)

All six quickstart steps run against this branch. Captured 2026-06-30, Release config.

| Step | What | Result |
|---|---|---|
| 1 | Live ground-truth scaffold (spec-kit + sdd), enumerate produced surface | ✅ PASS — see `produced-surface.md`. spec-kit app carries 38 `.agents/skills` (7 product skills + speckit-* + framework set); `docs/skillist-reference.md` present under spec-kit, absent under sdd. None of the defunct/`fsdocs-*`/`fsharp-*` ids appear. |
| 2 | Check fails on the broken docs (pre-fix) | ✅ PASS — see `regression-evidence.md`. 45 findings, each `id+doc+line`. |
| 3 | Correct docs → check passes | ✅ PASS — `dotnet test … --filter Feature224SkillCatalogCurrency` → **6/6 green** (SC-001/SC-002/SC-005). |
| 4 | Regression: inject dangling id → red; revert → green | ✅ PASS — in-memory regression test asserts both directions; real-file inject also reproduced red naming `fs-gg-does-not-exist` at the injected line (SC-003). |
| 5 | Refresh path passes first run (Option A) | ✅ PASS — the hand-edited catalog passes the check on the first run, no further edits (SC-004 / FR-007). See `refresh-path.md`. |
| 6 | Gating intact (Feature 219/204) | ✅ PASS — `--filter Feature219` 6/6 green, `--filter Feature204` 8/8 green. Catalog still spec-kit-gated; only content changed. |

## No-regression baseline diff (T023)

`scripts/baseline-tests.fsx --config Release` before (`baseline.md`) vs after (`baseline-after.md`):

- Both: **21 projects · 8 green · 13 red.** The 13 reds are the pre-existing FSharp.Core lockfile
  build/restore failures (auto-memory `nu1403-fsharp-core-lockfile-workaround`), unrelated to this change.
- `tests/Package.Tests`: **153 passed/1 failed → 159 passed/1 failed** — +6 new Feature 224 tests, all
  green; the single failure is the documented pre-existing `Surface baselines.FS.GG.UI.Build engine
  baseline` red (Build engine assembly not built in the Debug/Release test lane). **No new reds.**

## Surface / `.fsi` hygiene (T020)

The currency check is a self-contained test consuming the existing public `SkillParity` API
(`defaultRequest`, `discoverDefaultSurfaces`, `inventorySkills`) — **no new public surface added**,
so no `SkillParity.fsi` change and no surface-area baseline delta are required (Principle II
conditional resolved as "no public helper added").
