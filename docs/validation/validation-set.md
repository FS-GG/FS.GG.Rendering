# Initial validation set

> Migration Stage R3 deliverable. The `import-now` candidates from
> [`justification-records.md`](./justification-records.md), partitioned by frequency. This is
> what Stage R4 actually imports and what contributors run. It is **deliberately small**: the
> **Local inner loop** is the default tier run on every change; heavier and release-only
> checks are separated so routine work stays fast.

## Local inner loop (default — run on every change)

A named, enumerated set of fast checks (no item is "everything else"). This is exactly the
`tests/*.Tests` set of `FS.GG.Rendering.slnx`; the gate derives its loop from the slnx and
`tests/Build.Tests/CadenceCoverageTests.fs` (Feature 235) asserts this list stays in lockstep — so it
cannot drift again (the retired `Color`/`Input` projects and the six omitted ones were finding
P4 / #47):

1. `Build.Tests`
2. `Canvas.Tests`
3. `Controls.Tests`
4. `Diagnostics.Tests`
5. `Elmish.Tests`
6. `KeyboardInput.Tests`
7. `Layout.Tests`
8. `Lib.Tests` (runtime-protecting subset)
9. `Rendering.Harness.Tests`
10. `Scene.Tests`
11. `Symbology.Tests`
12. `Symbology.Render.Tests`
13. `SymbologyBoard.Tests`
14. `Testing.Tests`
15. `SkiaViewer.Tests`
16. `Smoke.Tests`

Items 1–14 are fast and deterministic (capability `none`); only `SkiaViewer.Tests` and `Smoke.Tests`
(15–16) need a GL context — the dev baseline provides one, and the gate runs them in its GL step under
degrade-and-disclose (skipped, disclosed) on a headless runner. This is the tier a contributor runs as
routine work.

## CI (runs on push / PR)

- `surface-baselines` (+ `refresh-surface-baselines.fsx`) — public `.fsi` surface-drift.
- docs build (`fsdocs`) — the docs site builds from current sources.

## Maintainer validation lanes

The lane runner is an orchestration layer over the direct commands above. It
writes one run directory per invocation with `summary.md`, `summary.json`, and
separate per-lane `log.txt`, `result.json`, and `diagnostics.md` files.

```sh
dotnet fsi scripts/run-validation-lanes.fsx --list
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out artifacts/validation-lanes
dotnet fsi scripts/run-validation-lanes.fsx --required --out artifacts/validation-lanes
```

Required lanes are `build`, `library-tests`, `package-proof`, `controls`,
`rendering-harness`, and `antshowcase-sample`. `aggregate-solution` is optional
and is reported separately so it cannot hide a required lane failure. The runner
fails closed for failed, timed-out, no-progress-timeout, canceled, skipped,
not-run, environment-limited, and infrastructure-error required lanes.

The on-demand `retained-inspection` lane is the maintained entry point for retained-render
inspection and damage-locality readiness:

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane retained-inspection --out specs/170-retained-damage-inspection/readiness/lanes
```

It runs the focused Feature170 Controls, Testing, Rendering.Harness, and AntShowcase checks
sequentially and writes its own per-lane logs, result JSON, diagnostics, TRX files for the
VSTest-backed slices, and direct Expecto output for AntShowcase. It is
optional in the general validation catalog until maintainers deliberately promote it to the
required lane set.

Direct validation commands remain valid for focused debugging. If a direct
command is intentionally used as a targeted substitute for an incomplete lane
run, disclose that in the readiness evidence and keep the incomplete lane
summary visible.

## Release-only (separate from local; runs at packaging/release)

- `Package.Tests` — package restore + consumption contract.
- `Product.Tests` (template) — generated product restores / builds / instantiates.

## Manual / advisory

None in the active set. Heavier on-demand checks (e.g. visual parity) are **not** imported
as-is — they are folded into the Stage R5 harness (see [`harness.md`](./harness.md)) and
tracked in the [`deferral-ledger.md`](./deferral-ledger.md).

## Invariants

- Every member carries exactly one frequency label and appears in exactly one group above.
- **Release-only checks do not appear in the Local group** (no overlap).
- Every member traces to an `import-now` row in [`justification-records.md`](./justification-records.md).
