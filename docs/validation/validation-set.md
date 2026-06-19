# Initial validation set

> Migration Stage R3 deliverable. The `import-now` candidates from
> [`justification-records.md`](./justification-records.md), partitioned by frequency. This is
> what Stage R4 actually imports and what contributors run. It is **deliberately small**: the
> **Local inner loop** is the default tier run on every change; heavier and release-only
> checks are separated so routine work stays fast.

## Local inner loop (default — run on every change)

A named, enumerated set of fast checks (no item is "everything else"):

1. `Color.Tests`
2. `Scene.Tests`
3. `Layout.Tests`
4. `Input.Tests`
5. `KeyboardInput.Tests`
6. `Elmish.Tests`
7. `Controls.Tests`
8. `Testing.Tests`
9. `SkiaViewer.Tests`
10. `Smoke.Tests`
11. `Lib.Tests` (runtime-protecting subset)

All are fast and deterministic (9–11 need a GL context, which the dev baseline provides).
This is the tier a contributor runs as routine work.

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
