# Quickstart — Validating the Design-System Layer Split

This guide is the **behaviour-neutrality + layering validation** runbook for D1. It proves the
split is real (new packages, acyclic graph) and invisible (identical behaviour, green gates) — the
two co-critical success bars (US1/US3). It references `data-model.md` (carve map),
`contracts/layering-contract.md` (dependency direction), and `contracts/public-surface-migration.md`
(relocation table) rather than restating them.

## Prerequisites

- .NET SDK for `net10.0`; the repo restores cleanly (`dotnet restore FS.GG.Rendering.slnx`).
- A clean working tree on branch `125-designsystem-layer-split`.
- Baseline confidence: before starting, `dotnet build -c Release` is green and the existing suite
  passes (this is the oracle the move must preserve).

## V1 — The split compiles (acyclic graph, SC-006)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

**Expected**: 0 errors, 0 new warnings. A green build proves the `Controls → DesignSystem`,
`Themes.Default → DesignSystem`, `DesignSystem → Scene` graph is acyclic (any forbidden back-edge
would fail to compile). Confirms FR-009, SC-006, and the layering contract's compile-time clause.

## V2 — DesignSystem is catalog-free (US1, SC-002)

Inspect the new package's resolved closure (no controls catalog present):

```bash
dotnet list src/DesignSystem/DesignSystem.fsproj package --include-transitive
```

**Expected**: `FS.GG.UI.Scene` appears; `FS.GG.UI.Controls` does **not** appear anywhere in the
closure. Optionally compile the minimal consumer in V3 to prove the primitives are usable standalone.

## V3 — A standalone consumer can use the primitives (US1.1)

Sketch a throwaway script/project that references **only** `FS.GG.UI.DesignSystem` and exercises the
relocated surface (run in FSI against the built dll, per Principle I):

```fsharp
open FS.GG.UI.DesignSystem
// names resolve from DesignSystem alone — no Controls reference:
let t : Theme = Theme.??? // (the Theme *type*; concrete light/dark come from Themes.Default)
let baseStyle : ResolvedStyle = Unchecked.defaultof<ResolvedStyle>
let _ : ResolvedStyle = Style.resolve t baseStyle [ StyleClass.Variant StyleVariant.Primary ] VisualState.Hover
```

**Expected**: it compiles referencing only DesignSystem (+ Scene). `Theme`, `ResolvedStyle`,
`StyleVariant`, `StyleClass`, `VisualState`, and `Style.resolve` are all in scope; no controls type
is needed. Confirms US1.1 and the relocation table.

## V4 — Default theme is its own layer (US2)

```fsharp
open FS.GG.UI.DesignSystem
open FS.GG.UI.Themes.Default
let light : Theme = Theme.light
let dark  : Theme = Theme.dark
// success/warning roles now present and additive:
let _ = light.Success
let _ = light.Warning
```

**Expected**: `Theme.light`/`dark` resolve from `Themes.Default`, return the same Light/Dark values
as before the split, and expose the new `Success`/`Warning` roles (US1.3, US2.2, FR-004).
`dotnet list src/Themes.Default/Themes.Default.fsproj package --include-transitive` shows
`FS.GG.UI.DesignSystem` and **not** `FS.GG.UI.Controls` (US2.1).

## V5 — Behaviour-neutral: the full suite passes unchanged (US3.1, SC-001)

```bash
dotnet test FS.GG.Rendering.slnx -c Release
```

**Expected**: every test that passed before the split passes after — **zero** tests deleted,
weakened, or newly skipped. The skipped-test count (18 honest `ptest`/`ptestList`) is unchanged.
Confirms FR-005, FR-006, SC-001.

## V6 — Render identity (US3.2, SC-003)

Render the reference scenes / sample gallery and compare to pre-split output:

```bash
dotnet test samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj -c Release
```

**Expected**: rendered output and the accessibility contract are identical (byte-identical where the
render path is deterministic). The `ThemeInvarianceTests`/`PageRenderTests` pass after the gallery's
sources add the relocation `open`s. Confirms SC-003.

## V7 — Surface-drift gate green with atomic baselines (US3.3, SC-004)

```bash
# regenerate baselines from the freshly built assemblies, then diff
dotnet fsi scripts/refresh-surface-baselines.fsx
git status --porcelain tests/surface-baselines/
git diff -- tests/surface-baselines/FS.GG.UI.Controls.txt
```

**Expected**: `tests/surface-baselines/` now contains committed `FS.GG.UI.DesignSystem.txt` and
`FS.GG.UI.Themes.Default.txt`, and `FS.GG.UI.Controls.txt` is regenerated (smaller). After
committing all three, the refresh script reports **no drift** (clean `git status`). The
`FS.GG.UI.Controls.txt` diff shows only the relocated rows **removed** (they now live in the two new
baselines) — relocations only, no removals overall (SC-004, SC-005, FR-007).

## V8 — Docs & template reflect the new layout (US3.4, FR-008/FR-011)

**Expected**:
- `docs/product/decisions/0003-designsystem-namespace-relocation.md` exists and records the
  relocation + no-shim rationale.
- `docs/product/module-map.md` design-system/theme rows read **"owned assembly"** (not "embedded in
  Controls").
- `template/base` product source + `template/base/docs/api-surface/` snapshot are updated so the
  template pack/instantiate check stays green.

## Done when

All of V1–V8 pass: the two packages exist and are catalog-free (V2–V4), the full suite + render +
drift gate are green at the same commit (V5–V7), and the docs/decision record/template reflect the
move (V8) — i.e. the layer boundary is now physical and nothing observable changed.
