# Quickstart: Input/Render Responsiveness

## Prerequisites

- .NET SDK for `net10.0`
- Restored repository:

```sh
dotnet restore FS.GG.Rendering.slnx
```

- Live responsiveness evidence requires a desktop session with a visible GL/OpenGL-capable window. Headless hosts must report `environment-limited` rather than accepted live readiness.

## 1. Run Focused Scheduler Tests

```sh
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --no-restore --filter Feature167
```

Expected outcome:

- input envelopes receive stable sequence ids
- discrete pointer/key events preserve order
- continuous pointer movement coalesces with counts
- input receipt classification stays below budget in deterministic/synthetic receipt tests where applicable
- frame drain folds input and marks dirty state without rendering once per native callback

## 2. Run Focused Adapter/Timing Tests

```sh
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --no-restore --filter Feature167
```

Expected outcome:

- pointer activation produces the expected latency-record shape
- keyboard activation uses the same record fields as pointer activation
- one input that produces multiple product messages folds updates before one recomposition
- no-state-change input records no-visible-response
- disabled diagnostics leave frame metrics and rendered output unchanged
- existing deterministic `Perf.runScript` clock-free assertions remain stable

## 3. Run Existing Interaction Compatibility Tests

```sh
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore --filter "Interaction|Pointer|Focus"
dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj -c Release --no-restore
```

Expected outcome:

- pointer activation behavior is unchanged
- focus routing remains compatible
- key-down Enter/Space activation paths remain compatible
- key-up remains non-activating unless a focused control handles it

## 4. Run AntShowcase Deterministic Responsiveness Shape

```sh
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Interaction|Responsiveness"
```

Expected outcome:

- representative scripts include pointer activation and keyboard activation
- AntShowcase still maps key-down Enter and Space to the representative state-changing command
- deterministic evidence does not claim live input-to-present acceptance when no live surface is used

## 5. Capture Live AntShowcase Responsiveness Evidence

```sh
dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- \
  responsiveness \
  --page buttons \
  --theme light \
  --script representative \
  --out specs/167-input-render-responsiveness/readiness/responsiveness \
  --json
```

Expected outcome on a live-capable host:

- output prints `summary.json` path and readiness token
- `records.jsonl` contains pointer activation, keyboard activation, movement coalescing, no-visible-response, and long-frame facts
- `summary.md` names p50, p95, max latency, long-frame count, first failed budget if any, and three slowest interactions
- accepted readiness requires p95 input-to-visible below 50 ms and input receipt within 4 ms p95 / 16 ms max

Expected outcome on a headless/environment-limited host:

- command writes `environment.md` and `summary.json`
- readiness is `environment-limited`, `blocked`, or `incomplete`, not accepted
- missing timing boundaries are named

## 6. Verify Machine-Readable Summary

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out artifacts/validation-lanes
```

Expected outcome after tasks wire the lane or direct summary validator:

- validation can read responsiveness `summary.json` without parsing Markdown
- non-accepted responsiveness summaries block their configured scope
- optional aggregate lanes cannot hide required responsiveness failures

## 7. Verify Public Surface and Package Compatibility

```sh
./fake.sh build -t PackageSurfaceCheck
./fake.sh build -t PackLocal
```

Expected outcome:

- every public diagnostic addition appears in `.fsi`
- surface baselines are updated intentionally
- packages pack successfully
- compatibility notes state that existing product host behavior is unchanged unless diagnostics are explicitly enabled

## 8. Readiness Evidence Package

Feature readiness should include:

- `specs/167-input-render-responsiveness/readiness/fsi-contract-transcript.md`
- `specs/167-input-render-responsiveness/readiness/compatibility.md`
- `specs/167-input-render-responsiveness/readiness/scheduler-tests.md`
- `specs/167-input-render-responsiveness/readiness/synthetic-evidence.md` when synthetic fixtures are used
- `specs/167-input-render-responsiveness/readiness/responsiveness/<run-id>/summary.md`
- `specs/167-input-render-responsiveness/readiness/responsiveness/<run-id>/summary.json`
- `specs/167-input-render-responsiveness/readiness/responsiveness/<run-id>/records.jsonl`

Evidence must disclose whether it is live, headless substitute, or synthetic. Synthetic tests must carry `Synthetic` in their names and comments explaining the real-evidence path or limitation.
