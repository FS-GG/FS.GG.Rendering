# Quickstart: Validate the Scene.fs Module Split

A run/validation guide. Implementation detail lives in `tasks.md` (after `/speckit-tasks`). All
commands run from repo root. GL suites require X11 (`DISPLAY=:1`).

## Prerequisites

- .NET `net10.0` SDK; `dotnet fsi` for the surface-baseline script.
- X11 display for GL suites (`DISPLAY=:1`).
- Clean working tree on branch `188-scene-module-split`.

## Step 0 — Capture the pre-refactor baseline (FR-011, BEFORE any production edit)

```bash
# Surface snapshot
dotnet build FS.GG.Rendering.slnx -c Release
dotnet fsi scripts/refresh-surface-baselines.fsx
git stash   # or copy: preserve readiness/surface-baselines/FS.GG.UI.Scene.txt as the reference

# Affected-suite red/green set + artifact corpus (record results)
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release 2>&1 | tee specs/188-scene-module-split/baseline-tests.txt
```

Record: the surface baseline, the red/green set, and the inspection/evidence artifact corpus
(visual/retained inspection + scene/layout evidence) and glyph fingerprints. Every later step diffs
against this. **No production edit precedes this capture.**

## Step 1 — US1: extract `Types.fs` (surface-neutral)

Move the type wall to a namespace-level `Types.fs` (+ `Types.fsi`); update `Scene.fsproj` order.

```bash
dotnet build FS.GG.Rendering.slnx -c Release            # all 17 consumers compile
DISPLAY=:1 dotnet test tests/Scene.Tests -c Release      # round-trip / package / inspection
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff -- readiness/surface-baselines/FS.GG.UI.Scene.txt   # EXPECT: empty
```

**Expected**: whole solution compiles; `Scene.Tests` green; **empty** surface diff; no version bump.
(Acceptance US1 #1–#3.)

## Step 2 — US2: unify shaping trio + relocate measurer (byte-identical)

Create `TextShaping.fs`/`.fsi` (`module Text.Shaping`); collapse the trio to one private core; move
the `realTextMeasurer` seam there; keep `module Scene` public delegations.

```bash
dotnet build FS.GG.Rendering.slnx -c Release
DISPLAY=:1 dotnet test tests/Scene.Tests -c Release      # Feature140/142/136 shaping suites
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff -- readiness/surface-baselines/FS.GG.UI.Scene.txt   # review: only intended shaping changes
```

**Expected**: glyph runs / shaped-text / fingerprints byte-identical; `setRealTextMeasurer`
set/clear/measure lifecycle identical; surface diff (if any) limited to the reviewed shaping/measurer
relocation. **Bump `Scene.fsproj` `<Version>` iff the diff is non-empty.** (Acceptance US2 #1–#2.)

## Step 3 — US3: extract inspection/evidence + finish FR-006 dedup (behavior change)

Move the four modules to `Inspection.fs`/`Evidence.fs` (names preserved); finish the dedup so
findings sharing a `stableFindingId` collapse on BOTH paths.

```bash
dotnet build FS.GG.Rendering.slnx -c Release
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release   # inspection/evidence + consumer + harness suites
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff -- readiness/surface-baselines/FS.GG.UI.Scene.txt   # EXPECT: empty (names preserved)
```

Then perform the **semantic-artifact diff** vs the Step-0 corpus and record the dedup delta as an
approved expected-output change:

- Unchanged-finding inputs ⇒ artifacts semantically equivalent (status/counts/headers).
- Duplicate-finding inputs ⇒ duplicates collapsed exactly as the approved expected output specifies.
- Degenerate/malformed scene ⇒ genuine finding still emitted with the same fail-loud diagnostic.

**Expected**: empty surface diff; test red/green set identical to baseline except the reviewed FR-006
expected-output updates; no assertion weakened. (Acceptance US3 #1–#3; SC-003/SC-007.)

## Step 4 — Final gates

```bash
dotnet build FS.GG.Rendering.slnx -c Release
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release
wc -l src/Scene/Scene.fs src/Scene/Types.fs src/Scene/TextShaping.fs \
      src/Scene/Inspection.fs src/Scene/Evidence.fs           # each ≤ ~1,500 (SC-001)
```

Confirm: SC-001 size guideline met; SC-002 (3→1 builder, single measurer owner); SC-003 (dedup
uniform, zero known-duplicate paths); SC-004 byte-equivalence; SC-005 test parity; SC-006 surface;
SC-007 approved dedup delta recorded.

## References

- Module/ordering/surface/artifact contract: [contracts/module-topology.md](./contracts/module-topology.md)
- Topology + finding-identity model: [data-model.md](./data-model.md)
- Decisions + FR-006 reading: [research.md](./research.md)
- §7 verification discipline: parent report
  `docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md` §7.
