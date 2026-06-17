# Quickstart — Render Blockers (Clipping, Overlay & Scroll)

A validation/run guide. Implementation detail lives in `tasks.md` (Phase 2) and the code; behavior is in
`contracts/` and `data-model.md`.

## Prerequisites

- .NET `net10.0` SDK; repo builds with `Directory.Build.props` (FS0078-as-error, Nullable=enable).
- For GL screenshot capture: an X11/GL display. All the new behaviour (clipping, cache parity, overlay
  ordering, hit-test, scroll geometry) is headless-deterministic; no-GL hosts record a disclosed degrade.

## V1 — Cache-parity gate FIRST (P-A, the blocker)

The decisive gate. After routing all six composition sites (including `RetainedRender.assemble`) through
`composeContainerScene`:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Debug --filter "Picture cache"
```
Expect: `cache-on ≡ cache-off` byte-identical, hits=3, misses=0, effectiveness margin preserved — GREEN with
clipping enabled. If RED, a composition site was missed (probe: diff `flat off.Render` vs `flat on.Render`).

## V2 — Build the framework

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```
Expect: `FS.GG.UI.Controls` builds; the overlay-pass `.fsi` entry compiles.

## V3 — Framework semantic tests (fail-before / pass-after)

```bash
dotnet test -c Release
```
Key oracles:
- Container bounds: no child paints past its container; full ≡ retained on a clipped tree.
- Overlay: open dropdown paints above an in-flow sibling; items distinct; hit returns the overlay; empty
  overlay group ⇒ byte-identical to the pre-overlay render.
- ScrollViewer: content clipped to box + affordance; bounded-page (nothing outside the content region).

## V4 — Repack + re-capture the 19 pages (P-D)

```bash
dotnet pack FS.GG.Rendering.slnx -c Release          # → ~/.local/share/nuget-local/
cd samples/AntShowcase
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1
```
Expect per page under `artifacts/ant-showcase/1/<page-id>/`: no spill, dropdowns above neighbours, long pages
clipped+scrollable. No-GL host → disclosed degrade.

## V5 — Determinism

```bash
# run V4 twice into two output dirs and diff
diff -r artifacts/ant-showcase/1 /tmp/recap/1
```
Expect: byte-identical (SC-006).

## V6 — Re-baseline + disclose (P-D)

Re-establish G1/G2 golden, the drift gate, and the touched surface baselines
(`scripts/refresh-surface-baselines.fsx`); fill `contracts/rebaseline-ledger.md` (one disclosed row per
changed baseline). Confirm any evidence that should be unchanged is byte-identical.
