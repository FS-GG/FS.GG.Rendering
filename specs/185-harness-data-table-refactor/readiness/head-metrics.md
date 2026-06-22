# HEAD Metrics — Harness Data-Table Refactor (185)

Recorded before any production edit (T001/T003). These are the counts the refactor must shrink and
the SC-003 starting points that must drop to zero.

## Build (T001)

`dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Release` → **succeeded**, 0 warnings, 0 errors.

## File sizes (SC-001 target: no file > ~1,500 lines)

| File | HEAD lines |
|---|---|
| `tools/Rendering.Harness/Compositor.fs` | 5,512 |
| `tools/Rendering.Harness/Cli.fs` | 3,928 |
| `tools/Rendering.Harness/ValidationLanes.fs` | 1,376 |
| `tools/Rendering.Harness/Compositor.fsi` | 1,005 |

## SC-003 starting counts (must reach 0)

| Metric | Command | HEAD |
|---|---|---|
| `*ReadinessDirectory` references | `grep -cE 'ReadinessDirectory' Compositor.fs` | 110 |
| top-level `renderFeature*` fns | `grep -cE '^\s*let\s+renderFeature' Compositor.fs` | 85 |
| `Compositor.fsi` exposed vals | `grep -cE '^\s*val ' Compositor.fsi` | 387 |
| per-feature `runFeature*Cmd` handlers | `grep -cE 'runFeature.*Cmd' Cli.fs` (decls + dispatch) | 26 |

## Reference-site blast radius

| Symbol family | Compositor.fs | Cli.fs | tests/ |
|---|---|---|---|
| directory/path constants (`feature###*Directory`/`*Path`) | 220 | 29 | 12 |
| `renderFeature*` | 88 | 119 | 62 |
| `feature###Id` | — | — | 5 |

## After (Polish T040 — all four stories landed)

### SC-001 — no `tools/Rendering.Harness/` `.fs` > ~1,500 lines

`Compositor.fs` (5,512) → 7 modules: Types 599, Config 849, FeatureState 858, Render 1,070,
Render2 514, Render3 990, Render4 654. `Cli.fs` (3,928) → 5 modules: Cli 790, Cli.Shared 133,
Cli.FeatureBuilders 818, Cli.Performance 877, Cli.Readiness 1,382. `ValidationLanes.fs` 1,376 → 1,395
(runLane ~44-line orchestrator + 3 units). **Largest harness `.fs` = `SkillParity.fs` 1,493**
(pre-existing, untouched) ≤ 1,500. ✅

### SC-003 — counts reached 0

| Metric | HEAD | After |
|---|---|---|
| `grep -cE 'ReadinessDirectory' Compositor*.fs` | 110 | **0** |
| `grep -cE '^\s*let\s+renderFeature' Compositor*.fs` | 85 | **0** |
| `grep -rE 'let (private )?runFeature[0-9]+\w*Cmd' Cli*.fs` (SC-002/006) | 26 | **0** |

### SC-005 — semantic equivalence + byte-identity

All 12 features semantic-diff clean vs `/tmp/185-baseline` (`problems=0`); package-validation files
byte-identical. See `semantic-equivalence.md`.

### SC-002 — single-site add proven

One descriptor row → compiles + auto CLI command + zero new handler. See `sc-002-single-site.md`.
