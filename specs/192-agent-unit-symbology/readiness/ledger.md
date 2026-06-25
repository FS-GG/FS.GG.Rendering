# Readiness ledger — 192-agent-unit-symbology

## Allowlist + git check-ignore proof (Feature-168 rule)

`specs/*/readiness/` is git-ignored by default. This feature's durable evidence (M0 spike PNG, M5
dry-run provenance + golden board + final module + rationale, baseline snapshot) is committed under an
explicit allowlist added to `.gitignore`:

```
!specs/192-agent-unit-symbology/readiness/
!specs/192-agent-unit-symbology/readiness/**
```

`git check-ignore -q <path>` returns exit **1 (not ignored → stageable)** for every readiness file after
the allowlist, e.g.:

- `readiness/golden-board.png` → STAGEABLE
- `readiness/dry-run/design-rationale.md` → STAGEABLE
- `readiness/dry-run/FinalSymbolSet.fsx` → STAGEABLE
- `readiness/m0-spike-evidence.md` → STAGEABLE
- `readiness/baseline.md` → STAGEABLE

## Evidence index

- `baseline.md` — T001 no-regression baseline (pre-existing reds: Package.Tests 8, ControlsGallery 2,
  SecondAntShowcase 1) and T042 re-run diff.
- `m0-spike-evidence.md` + `m0-spike/` — T007 render-bridge live-smoke (ReferencePassed, non-blank PNG).
- `dry-run/` — T038–T040 M5 audit trail: per-round timestamped board PNG + mapping snapshot, golden
  board + identity, final symbol-set module + design rationale.

## Honesty caveats

- The throwaway FSI M0 spike hit an **FSI-only** native-load limitation for SkiaSharp (not a bridge
  defect); the authoritative live render proof is the `Symbology.Render.Tests` project host
  (ReferencePassed, non-blank PNG). Disclosed in `m0-spike-evidence.md`.
- No synthetic, substitute, degraded, canceled, timed-out, or pending-review checks were used for the
  green claims below; all are real `dotnet test` / `dotnet fsi` runs in this checkout.
