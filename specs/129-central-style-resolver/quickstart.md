# Quickstart — Central Visual-State Style Resolver (F4)

Validation runbook proving F4 is behaviour-neutral, total, and seam-capable. Headless and
deterministic (no GL required). Run from repo root.

## Prerequisites

- .NET `net10.0` SDK; solution `FS.GG.Rendering.slnx` builds clean
  (`TreatWarningsAsErrors=true`).
- No new dependency, no template install, no GL context needed.

## V1 — Build is warning-clean

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```
**Expect**: 0 warnings, 0 errors. Confirms the new internal module + IVT grant + `buttonGeom`
migration compile under `TreatWarningsAsErrors`.

## V2 — Default-theme parity (byte-identity)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release \
  --filter "Feature129&parity"
```
**Expect**: green. For `light` and `dark`, across `{button, icon-button} × {primary, secondary,
danger, ghost} × {8 visual states}`, the migrated `StyleResolver.resolveDefault` output
byte-equals the pre-migration oracle (`ResolvedStyle` **and** emitted `Scene`). → SC-001 (G1/G2).

## V3 — Totality + determinism

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release \
  --filter "Feature129&totality"
```
**Expect**: green. The exhaustive cross-product — including an unknown/`Custom` kind and an
unknown intent string — returns a concrete style with zero exceptions; two runs are equal.
→ SC-003 (G3).

## V4 — Intent consumed + divergence reachable without control edits

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release \
  --filter "Feature129&divergence"
```
**Expect**: green. Under `neutralPolicy`, `danger` resolves identically to `primary` (today's drop
preserved). Under a non-default policy supplied directly to `StyleResolver.resolve`, `danger`
differs from `primary` — with **zero** edits to any control render function and no new control
type. → SC-002, SC-006, SC-007 (G4/G5).

## V5 — Zero public-surface delta + token-drift green

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
git status --porcelain tests/surface-baselines/      # expect: empty
```
**Expect**: no baseline file changes (internal module + IVT are invisible to the surface gate);
the design-token-drift gate stays green (F4 regenerates no tokens). → SC-004 (G6).

## V6 — Full-suite integrity + render-loop neutrality

```bash
dotnet test FS.GG.Rendering.slnx -c Release
```
**Expect**: pass/skip counts unchanged vs. pre-F4; no test removed/skipped/weakened. At-rest,
animation, layout, identity, memoization, virtualization, cache, and fingerprint/replay behaviour
unchanged for identical inputs (the change is confined to style assembly). → SC-005, SC-008
(G7/G8).

## Success summary

| Validation | Proves | Criteria |
|------------|--------|----------|
| V1 | Compiles warning-clean | Constitution build gate |
| V2 | Default-theme byte-identity | SC-001 |
| V3 | Total + deterministic | SC-003 |
| V4 | Intent consumed; seam diverges without control edits | SC-002, SC-006, SC-007 |
| V5 | Zero surface/token-drift delta | SC-004 |
| V6 | Suite integrity + render-loop neutrality | SC-005, SC-008 |
