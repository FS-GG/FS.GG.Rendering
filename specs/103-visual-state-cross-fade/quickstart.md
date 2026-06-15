# Quickstart — Validating Feature 103 (Visual-State Cross-Fade)

This is a **conformance backfill**: the code and tests already exist. Validation = build green + the 103 suite
green (it self-writes its readiness) + zero public-surface-baseline delta.

## Prerequisites

- .NET `net10.0` SDK; repo restored. No GL context required (103's proofs are deterministic/headless:
  structural scene equality, descriptive-scene colour/alpha inspection, work-count invariants).

## 1. Build (Release, zero warnings)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

Expected: 0 warnings, 0 errors.

## 2. Run the 103 suite

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "103"
```

Expected: `Feature103CrossFadeTests` green (10 tests across 4 lists):
- **US1** — mid-flight, the prior colour fades OUT under the next fading IN; the displayed colour is strictly
  between the endpoints (SC-001/INV-3). Red on the pre-R6 fade-in (prior colour absent).
- **US2** — at-rest and settled output byte-identical to the static render; settle/fast path recomputes 0
  nodes (SC-002/SC-003/INV-1/INV-2).
- **US3** — determinism: a fixed 7-frame injected-delta replay + 60 FsCheck random sequences reproduce
  identical frames; a non-positive delta never rewinds (SC-004/INV-4).
- **US4** — edges: retarget continuity (INV-5), held-state scoped repaint (INV-6), return-to-Normal drop,
  no-colour-delta no artifact (SC-006).

> Note: the filter string is the feature number `"103"` (matched against test/list names).

## 3. Confirm the readiness evidence regenerated

The suite **self-writes** its readiness under `specs/103-visual-state-cross-fade/readiness/` on each run:
`mid-flight-interpolation.md` (SC-001), `at-rest-byte-identity.md` (SC-002), `final-frame-identity.md`
(SC-003), `determinism.md` (SC-004). Confirm each shows `status=pass` after the run.

> The readiness directory is gitignored (`specs/*/readiness/`) — it is transient test output, regenerated on
> every run, never committed. This is the repo convention for all backfills.

## 4. Confirm zero public-surface-baseline delta (FR-009)

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx   # regenerate, then confirm no diff
git diff --stat tests/surface-baselines/            # MUST be empty
```

Expected: `tests/surface-baselines/FS.GG.UI.Controls.txt` and `FS.GG.UI.Controls.Elmish.txt` unchanged — the
whole cross-fade seam is `internal`.

> Conformance-pass note: `refresh-surface-baselines.fsx` has no real `--check` mode (it always rewrites), so
> confirm "no delta" via the VCS diff (or md5 before/after), not a script exit code.

## 5. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release
```

Expected: 0 failures (18 honest `ptest`/`ptestList` skips remain — perf-corpus + FSI fixture, unrelated to 103).

## What this validation does NOT prove

- No pixel-level or desktop-visibility claim — parity is **structural scene equality** + descriptive
  colour/alpha inspection, captured deterministically (consistent with the 091/092/099/097 backfills).
- 103 owns the **two-snapshot cross-fade composite** only — not the live single-channel clock (099) or the
  no-alloc idle `advanceStateClocks` (121), which share the same seam.

## Success = the C4 conformance bar

Build green; the 103 suite green; readiness regenerates `status=pass`; zero public-surface delta;
`/speckit-analyze` reports cross-artifact consistency.
</content>
