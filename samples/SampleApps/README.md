# Sample Apps — Games + Productivity (curated G2 slice)

A runnable, curated slice of the archived games + productivity sample apps — **three games**
(Tetris, Snake, Pong) and **three productivity apps** (Todo, Kanban, Calendar) — built as
**`FS.GG.UI.*`-package consumers** (no `src/` project references). Building and running the
tree *is* the proof that the documented public-consumer path works end to end (SC-006).

This is **Workstream G2**. G1 (Controls Gallery) shipped as feature 123; G3 (Ant restyle) and
G4 (perf-corpus wiring) are out of scope.

## The curated six

| Sample | Family | Loop / pattern | Inputs |
|---|---|---|---|
| `tetris` | game | `Tick`-driven gravity + seeded 7-bag | keyboard, timing-step |
| `snake` | game | `Tick`-driven advance + seeded food | keyboard, timing-step |
| `pong` | game | continuous ball/paddle `Step` + seeded serve | keyboard, timing-step |
| `todo` | productivity | form validation + list + inline edit | keyboard, pointer |
| `kanban` | productivity | columns + pointer card move + inline edit | pointer, keyboard |
| `calendar` | productivity | date-grid nav + validated entry add | keyboard, pointer |

The new capability over G1 is the **persistent, deterministic game loop**: time advances only
through injected `FrameInput.Tick` deltas mapped by the host's `Tick` to a step message, and all
in-game randomness comes from a pure `--seed`-driven PRNG (`Prng.fs`) — no wall-clock, no
`System.Random`.

## Layout

```
samples/SampleApps/
├── nuget.config                 # local packed feed → ~/.local/share/nuget-local/
├── Directory.Build.props        # shadows the repo root; net10.0, FS0078-as-error
├── Directory.Packages.props     # disables central package management for the sample
├── coverage-backlog.md          # committed coverage + 22-spec backlog (rendered, drift-checked)
├── SampleApps.Core/             # pure cores + shared harness (no GL, no I/O)
│   ├── Prng.fs · SampleTheme.fs · Evidence.fs · Harness.fs
│   ├── Games/{Tetris,Snake,Pong}.fs
│   ├── Productivity/{Todo,Kanban,Calendar}.fs
│   └── Registry.fs · Coverage.fs
├── SampleApps.App/              # thin executable: list | interactive | evidence | coverage
└── SampleApps.Tests/            # Expecto suite (outside the default test tier)
```

The **Core** holds the closure-erased sample registry, the seeded PRNG, the pure MVU cores +
their seeded scripts + authored outcomes, the deterministic evidence record, and the
coverage/backlog honesty check. The **App** is the edge that turns Core into a live window
(`runInteractiveApp`), a headless evidence run (`Perf.runScript` + `captureScreenshotEvidence`),
or the coverage report. Tests exercise the public functions exactly as a downstream app would.

## Build

```bash
cd samples/SampleApps
dotnet build SampleApps.App/SampleApps.App.fsproj -c Release   # restores from the local feed (SC-006)
```

## Run (the V1–V8 quickstart)

```bash
# V1 — list the six samples
dotnet run --project SampleApps.App -c Release -- list

# V2 — one game, deterministic evidence
dotnet run --project SampleApps.App -c Release -- evidence --seed 7 --sample tetris

# V3 — byte-identical determinism across two same-seed runs
dotnet run --project SampleApps.App -c Release -- evidence --seed 7 --out /tmp/a
dotnet run --project SampleApps.App -c Release -- evidence --seed 7 --out /tmp/b
diff -r /tmp/a/7 /tmp/b/7 && echo BYTE-IDENTICAL

# V4 — productivity validation + inline edit
dotnet run --project SampleApps.App -c Release -- evidence --seed 7 --sample todo

# V5 — degrade-and-disclose on a no-GL host (unset DISPLAY)
dotnet run --project SampleApps.App -c Release -- evidence --seed 7

# V6 — coverage + 22-spec backlog honesty (exit 1 on tamper)
dotnet run --project SampleApps.App -c Release -- coverage

# V7 — the Expecto suite (the CI signal; headless)
dotnet test SampleApps.Tests -c Release

# V8 — interactive (advisory, GL-gated)
dotnet run --project SampleApps.App -c Release -- interactive tetris --theme dark
```

Evidence is written under `artifacts/sample-apps/<seed>/<sample-id>/`
(`run.json` / `summary.md` / `state.txt` [+ `frame.png` iff a screenshot was proven]) and is
gitignored — regenerated per run.

## Determinism & disclosure

- No wall-clock, no `System.Random` — a seeded PRNG + injected `Tick` deltas only (FR-006).
- Same-seed runs are byte-identical (`run.json` + `state.txt`, SC-002).
- Every game reaches a bounded terminal state within the scripted steps (SC-007).
- Every record carries a non-empty `notAuthoritativeFor`; a no-GL host degrades-and-discloses
  rather than hanging or faking a pass (FR-007/FR-008).

See `PROVENANCE.md` for the `FS.Skia.UI.* → FS.GG.UI.*` rebrand and the authored-outcome
disclosure, and `coverage-backlog.md` for per-sample coverage + the full 22-spec disposition.
