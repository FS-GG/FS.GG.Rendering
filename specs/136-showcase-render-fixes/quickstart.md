# Quickstart — Showcase Rendering Defect Fixes

A validation/run guide. Implementation detail lives in `tasks.md` (Phase 2) and the code. References to
behavior are in `contracts/` and `data-model.md`.

## Prerequisites

- .NET `net10.0` SDK; repo builds with `Directory.Build.props` (FS0078-as-error, Nullable=enable).
- For GL screenshot capture: an X11/GL display. Headless paths (state replay, coverage, measurement,
  fallback) run without a display; no-GL hosts record a disclosed degrade.
- Local NuGet feed at `~/.local/share/nuget-local/` (the AntShowcase consumes packed `FS.GG.UI.*`).

## V0 — Probe first (P-A, probe-driven)

Confirm the two load-bearing facts before building the font system:
1. Does the headless sandbox's `SKTypeface.Default` have glyph coverage today? (Explains how often the 5×7
   vector path fires now.)
2. Does embedded `SKTypeface.FromStream` loading succeed in the GL screenshot path?

Build a minimal standalone probe (do not rely on the heuristic) and record results in `research.md` R1.

## V1 — Build the framework

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```
Expect: `FS.GG.UI.SkiaViewer` builds with the embedded font assets; new `.fsi` surfaces compile.

## V2 — Framework semantic tests (fail-before / pass-after)

```bash
dotnet test -c Release   # renderer/overlay/composite/layout suites in tests/
```
Expect each defect-class test to fail on the pre-fix renderer and pass after. Key oracles:
- `@` renders as `@` (not `7`); `—`/`#`/`▸`/`·` authored-or-deliberate, never the `7`-wildcard.
- Mixed case preserved (`Stable`, not `STABLE`); no mid-word clip.
- data-grid renders a table; menu/combo/descriptions/QR/charts structural oracles (see
  `contracts/composite-controls.md`).
- region non-overlap, container clipping, clipped scroll, overlay z-order.

## V3 — Repack for the consumer sample

```bash
dotnet pack FS.GG.Rendering.slnx -c Release   # → ~/.local/share/nuget-local/
# invalidate the consumer's package cache as in feature 135's R1 if needed
```

## V4 — Re-capture the 19-page evidence (both themes)

```bash
cd samples/AntShowcase
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1
# single page while iterating:
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1 --page text-numeric-input
```
Expect per page under `artifacts/ant-showcase/1/<page-id>/`: `frame.png` (when GL available), `state.txt`,
`run.json`, `summary.md` — with fallback/tofu disclosure populated.

## V5 — Determinism

```bash
# run V4 twice into two output dirs and diff
diff -r artifacts/ant-showcase/1 /tmp/recap/1
```
Expect: byte-identical text rendering (bundled fonts are host-independent) — SC-005.

## V6 — Confirm defect absence

Review the 19 re-captured frames against `contracts/verification.md`: none of the seven defect classes
present; specifically the previously-broken `ada@example.com`, em-dash titles, `Stable`/`Upload`/`Refresh`,
data-grid, dropdowns, descriptions, charts, QR. Repeat under antDark (`--theme dark` interactive, or the
theme-invariance test).

## V7 — Re-baseline + disclose

Re-establish G1/G2 golden evidence, the rendered-output drift gate, and the touched surface-area baselines;
fill `contracts/rebaseline-ledger.md`. Confirm any G1/G2 evidence that should be unchanged is byte-identical
(SC-007).

## V8 — Sample evidence suite

```bash
cd samples/AntShowcase
dotnet test AntShowcase.Tests -c Release   # 19-page re-verification + theme invariance
```
