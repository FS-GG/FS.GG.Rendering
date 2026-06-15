# Quickstart: Validate Feature 091 (Wire the Keyed Reconciler onto the Render Path)

A validation/run guide for the **already-implemented** 091 contract. Because this feature is a
backfill, "validating" means confirming the existing authoritative tests are green and the
public-surface delta is zero — not building anything new. Contract details live in
[`contracts/retained-render.md`](./contracts/retained-render.md); entities in
[`data-model.md`](./data-model.md).

## Prerequisites

- .NET SDK with `net10.0` (see `Directory.Build.props`).
- No GL context / display required — every 091 proof is deterministic and headless (structural scene
  equality + work-count invariants). `DISPLAY` is unnecessary for this suite.

## 1. Build the Controls assembly + its tests

```sh
dotnet build FS.GG.Rendering.slnx -c Release
```

Expected: builds clean. `tests/Controls.Tests` references `src/Controls` and reaches the internal
`RetainedRender`/`Reconcile` surface via `[<assembly: InternalsVisibleTo("Controls.Tests")>]`.

## 2. Run the authoritative 091 suite

Run the whole Controls suite (the 091 test lists are prefixed `091 US…`):

```sh
dotnet test tests/Controls.Tests -c Release
```

Or, running the Expecto runner directly, filter to 091:

```sh
dotnet run --project tests/Controls.Tests -c Release -- --filter "091"
```

**Expected outcome** — all four 091 test lists pass:

| Test list | Proves | SC |
|---|---|---|
| `091 US1 identity survives an unrelated re-render` | stable id across an unrelated change / positional shift; Kind-change ⇒ fresh id | SC-001 |
| `091 US2 focus + animation survive an unrelated re-render` | focus + in-flight clock survive a shift; carried clock advances; rebuild baseline fails | SC-002 |
| `091 US3 partial update + golden parity` | `Recomputed ≤ ChangedSubtreeBound < N`; wired render byte-identical to a full rebuild | SC-003, SC-004 |
| `091 US4 invariants on the wired path (FsCheck, ≥1000 cases)` | round-trip, determinism, totality, identity-at-rest; `KeyCollision` Warning surfaces | SC-005, SC-006 |

The FsCheck properties run **≥1000 cases each** (`Config.QuickThrowOnFailure.WithMaxTest 1000`).

## 3. Confirm zero public-surface delta (FR-010 / C10)

```sh
dotnet test tests/Controls.Tests -c Release --filter "surface"   # or the repo surface-drift check
# and: scripts/refresh-surface-baselines.fsx must report NO changes for Controls
```

Expected: the `Controls` public-surface baseline (`tests/surface-baselines`) is **unchanged** — the
091 surface is `internal` and contributes nothing to it. A non-zero delta is a contract failure.

## 4. Read the captured readiness evidence (optional)

Pre-captured under `specs/091-wire-reconciler-render-path/readiness/` (gitignored, transient):

- `retained-parity/` — `status=pass`, golden-diff parity (`scene-diff=zero`), with honest disclosure
  that it proves **structural** scene equality, **not** pixels/desktop visibility.
- `work-reduction/` — `status=pass`, e.g. `RecomputedNodeCount(1) ≤ ChangedSubtreeBound(1) <
  BaselineNodeCount(13)`.
- `survives-proof/` — `status=pass`, focus/clock survive an unrelated re-render; baseline fails.

Each artifact names its `authoritative-test=…` so the evidence traces back to the test that produced
it. Treat the **tests** (step 2) as authoritative; the readiness files are the recorded summary.

## Done when

- Step 2 is fully green (all four 091 lists, ≥1000 FsCheck cases each).
- Step 3 shows zero `Controls` surface delta.
- No test was skipped or weakened to pass (Principle V).
