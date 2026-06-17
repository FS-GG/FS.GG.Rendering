# Quickstart — Games + Productivity Sample Apps (G2)

Build/run/verify guide. The sample tree is a **package consumer** (local NuGet feed) **outside the main
solution** — building it *is* the SC-006 public-consumer proof. Commands are run from
`samples/SampleApps/`. Implementation details live in `tasks.md`; this is a validation guide.

## Prerequisites

- .NET `net10.0` SDK.
- The `FS.GG.UI.*` packages at `0.1.0-preview.1` present in `~/.local/share/nuget-local/` (verified).
  If the public surface a sample consumes changed since the last pack, re-pack + clear the NuGet cache
  (same caveat as G1 — see memory `current-plan` D1 note).
- A local `nuget.config` pointing at the feed (copied from `samples/ControlsGallery/nuget.config`).
- GL/X11 only for **interactive** mode; everything CI-facing runs headless.

## Build

```bash
cd samples/SampleApps
dotnet build -c Release           # restores from the local feed; proves the consumer path (SC-006)
```

## V1 — list the samples (FR-001)

```bash
dotnet run --project SampleApps.App -c Release -- list
```
**Expect**: six rows — `tetris, snake, pong` (game) and `kanban, todo, calendar` (productivity) — each with
its control/input summary. Exit `0`.

## V2 — one game, deterministic evidence (US1 / FR-003/FR-005/SC-001)

```bash
dotnet run --project SampleApps.App -c Release -- evidence --seed 7 --sample tetris
```
**Expect**: `artifacts/sample-apps/7/tetris/{run.json,summary.md,state.txt[,frame.png]}`; `run.json.outcome`
equals Tetris's authored `ExpectedOutcome` (terminal `game-over`, cleared-rows / score pinned);
`notAuthoritativeFor` non-empty; exit `0`.

## V3 — byte-identical determinism (FR-006 / SC-002)

```bash
dotnet run --project SampleApps.App -c Release -- evidence --seed 7 --out /tmp/g2-a
dotnet run --project SampleApps.App -c Release -- evidence --seed 7 --out /tmp/g2-b
diff -r /tmp/g2-a/7 /tmp/g2-b/7 && echo "BYTE-IDENTICAL"
```
**Expect**: no diff across all six samples' `run.json` + `state.txt` (`frame.png` compared only where
proven). Confirms no wall-clock / no `System.Random` leaked in.

## V4 — productivity validation + inline edit (US2 / FR-004 / SC-007)

```bash
dotnet run --project SampleApps.App -c Release -- evidence --seed 7 --sample todo
```
**Expect**: `outcome` shows committed-vs-rejected counts — an invalid draft in the script was **rejected**
(not committed) and an inline edit **committed** to the data state.

## V5 — degrade-and-disclose on a no-GL host (US4 / FR-008 / SC-003)

```bash
# on a host with no display / GL (or unset DISPLAY)
dotnet run --project SampleApps.App -c Release -- evidence --seed 7
```
**Expect**: every record `provesScreenshot=false`, `unsupportedHostReason` stated,
`fallback="deterministic-state-only"`, no `frame.png`; the deterministic state + outcome still written;
exit `0` (never a hang, never a fabricated pass).

## V6 — coverage + 22-spec backlog honesty (US5 / FR-011 / FR-012 / SC-004 / SC-005)

```bash
dotnet run --project SampleApps.App -c Release -- coverage
```
**Expect**: per-sample control/input table (union spans keyboard + pointer + timing-step); a 22-row
adopted/deferred table (6 adopted = the registry, 16 deferred); exit `0`. Output matches the committed
`coverage-backlog.md`. Tamper a row (drop a spec / dangle a control id) ⇒ exit `1`.

## V7 — the Expecto suite (the CI signal)

```bash
dotnet test SampleApps.Tests -c Release
```
**Expect green**: `BuildOutcomeTests` (each sample builds + meets its outcome), `DeterminismTests`
(byte-identity + bounded terminal), `DegradeTests` (clean disclosed skip), `CoverageBacklogTests`
(per-sample coverage + 22-spec accounting), `ValidationTests` (invalid input rejected, inline-edit commits).
Headless — does not depend on a display (FR-014).

## V8 — interactive (advisory, GL-gated)

```bash
dotnet run --project SampleApps.App -c Release -- interactive tetris --theme dark
```
**Expect** (with GL): a live window; arrow keys move/rotate, gravity advances on the tick, score updates,
game-over on stack-out. On a no-GL host it degrades-and-discloses rather than hanging.

## Done When

- V1–V7 pass headlessly (V8 is advisory).
- Two same-seed runs are byte-identical across all six samples.
- The 22-spec backlog is fully accounted; no dangling control id; input union complete.
- `git diff tests/surface-baselines/` and `generate-design-tokens.fsx --check` are **empty/no-drift** (G2 is
  a consumer — zero public-surface/token change).
