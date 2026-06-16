# Quickstart: Validate the Color Policies (wcag / ant)

A runnable validation guide proving F2 end-to-end. Details live in `data-model.md` and `contracts/`;
this is the run/verify runbook.

## Prerequisites

- .NET `net10.0` SDK (repo `Directory.Build.props`).
- Repo builds clean: `dotnet build FS.GG.Rendering.slnx -c Debug`.
- No GL/display required — all F2 tests run in the default local (deterministic) tier.

## What landed

- `src/Color/ColorPolicy.fs` — `module internal ColorPolicy` (engine; no `.fsi`).
- `Color.fsproj` — `+ Compile ColorPolicy.fs`, `+ InternalsVisibleTo Controls.Tests`.
- `tests/Controls.Tests/Feature127ColorPolicyTests.fs` — the semantic tests.
- `docs/reports/color-policy-wcag.md`, `docs/reports/color-policy-ant.md` — committed reports.
- *(optional)* `scripts/generate-policy-report.fsx` — on-demand regenerator.

## Validate (each maps to a Success Criterion)

```sh
# Build, then run the F2 suite (deterministic, no GL)
dotnet build FS.GG.Rendering.slnx -c Debug
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Debug --no-build \
  --filter "Feature127"
```

Expected outcomes:

1. **SC-001 / FR-002 — wcag ≡ today**: for every catalog pairing, the `wcag` verdict equals
   `Contrast.check`'s verdict byte-for-byte; default policy is `wcag`. *(parity test passes)*
2. **SC-002 / FR-005 — ant ≠ wcag**: ≥1 shared pairing reaches a different verdict under `ant` than
   under `wcag` with identical colors. *(divergence test passes and names the pairing)*
3. **SC-003 / FR-004 — ant families covered**: the `ant` report includes primary / success / warning /
   error / info / text-on-surface, each with rule + measured + verdict.
4. **SC-005 / FR-006 — unknown name rejected**: `byName "material"` / `"Wcag"` / `""` → `Error`,
   never another policy.
5. **FR-010 / FR-011 — disclosure**: out-of-scope pairings show as out-of-scope (not pass); `ant`
   pairings WCAG would fail carry the authority note.

## Validate the report drift gate (SC-004 / FR-008 / FR-009)

```sh
# Drift gate runs as part of the suite above; it re-renders both reports and
# byte-compares against the committed files.
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Debug --no-build \
  --filter "Feature127"          # includes the drift + idempotency tests

# Prove tamper detection: edit a committed report, re-run → the drift test FAILS
#   (then restore the file).

# Regenerate on demand (writes docs/reports/color-policy-*.md via the same evaluator):
UPDATE_POLICY_REPORTS=1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Debug --no-build \
  --filter "Feature127"
#   (optional convenience wrapper, if present: dotnet fsi scripts/generate-policy-report.fsx)
```

Expected: clean run → drift test passes; idempotent (two renders byte-identical); tampered file →
fails and identifies the divergent report.

## Validate neutrality (SC-006 / FR-012)

```sh
# Public surface unchanged: regenerate baselines, expect zero diff.
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff --quiet -- tests/surface-baselines && echo "surface: no drift (PASS)"

# Existing render/gallery + pass/skip counts unchanged: full suite is green with no new skips.
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release   # (GL tier optional; F2 itself needs no GL)
```

Expected: no surface-baseline delta; no `.fsi` changes; existing suites unchanged.

## Done when

- All five SC checks pass; drift gate passes and detects tampering; surface baselines show zero delta;
  no new public rows; existing pass/skip counts unchanged.
