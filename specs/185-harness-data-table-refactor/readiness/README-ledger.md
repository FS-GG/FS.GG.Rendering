# Readiness Evidence Ledger — Feature 185

This `readiness/` tree is committed deliberately (it is the behavior-preserving refactor's
semantic-equivalence + red/green baseline evidence). Per the Feature 168 repository rule, the
default `specs/*/readiness/` ignore was allowlisted before staging.

## `.gitignore` allowlist proof

Added to `.gitignore`:

```
!specs/185-harness-data-table-refactor/readiness/
!specs/185-harness-data-table-refactor/readiness/**
```

`git check-ignore specs/185-harness-data-table-refactor/readiness/head-metrics.md` →
**not ignored** (allowlist effective) after the rule was added; **ignored** before.

## Contents

- `head-metrics.md` — HEAD line counts + SC-003 starting counts (T001/T003).
- `baseline.md` — full Release test red/green sweep (T002, SC-004 reference).
- `baseline-corpus.md` — live pre-refactor artifact corpus capture notes (T004) + tooling (T007).
- `frozen-literals.md` — byte-identical CI-grepped path/header literal inventory (T005).
- `rehoming-map.md` — descriptor classification / re-homing map (T006).
- `semantic-equivalence.md` — per-story + final semantic-diff results (added at Polish T039).
- `sc-002-single-site.md` — single-descriptor-row add walkthrough (added at Polish T041).

All artifact captures are real harness output; no synthetic substitution. Known pre-existing reds
(`Package.Tests`, `ControlsGallery`) are disclosed in `baseline-corpus.md` and are NOT regressions.
