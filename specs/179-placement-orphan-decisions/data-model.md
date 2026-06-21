# Data Model — Placement & Orphan Decisions (Feature 179)

This refactor has no runtime data model. The "entities" are the **projects/modules being moved or
removed** and their reference graphs — the structures the implementation must keep consistent. Each
entity lists its current state, target state, and the invariants that pin "behavior preserved".

## Entity 1 — `Rendering.Harness` (production CLI)

| Field | Value |
|-------|-------|
| Kind | Executable tooling (`OutputType=Exe`), no package surface |
| Current path | `tests/Rendering.Harness/` (39 `.fs`/`.fsi`, ~18,359 lines) |
| Target path | `tools/Rendering.Harness/` (verbatim move) |
| Inbound refs | `.slnx` (1); `Rendering.Harness.Tests` ProjectReference (1); 4 linked `TestAssertions.fs` includes; 3 helper scripts; 5 FSX evidence scripts; 1 skill doc |
| Internal refs | command literals in `Compositor.fs`, `ValidationLanes.fs`, `Live.fs`, + Feature 170 lane-test assertion — **split**: CLI-path literals → `tools/`, test-project-path literals stay `tests/` |

**State transition**: `tests/Rendering.Harness` → `tools/Rendering.Harness`.
**Invariants**: solution builds; the validation-lane / skill-parity / feed-refresh lanes invoke the
harness at its new path with no behavior change (FR-004); a repo-wide search for
`tests/Rendering.Harness/` (the CLI, trailing slash) returns zero genuine references (SC-002);
`tests/Rendering.Harness.Tests/` references are *unchanged* (that project does not move).

## Entity 2 — `FS.GG.UI.Input` (orphaned published package) — REMOVED

| Field | Value |
|-------|-------|
| Kind | Published package, has surface baseline (Tier 1 removal) |
| Path | `src/Input/` (`KeyboardInput.fs` 1,400, `KeyboardInput.fsi` 452, README) |
| Test | `tests/Input.Tests/` (3 files) — the only consumer |
| Baseline | `readiness/surface-baselines/FS.GG.UI.Input.txt` (77 exports) |
| Manifest row | `scripts/refresh-surface-baselines.fsx`: `"FS.GG.UI.Input", "Input"` |
| Production consumers | **none** (superseded by `FS.GG.UI.KeyboardInput`) |

**State transition**: present → fully removed (project, test, `.slnx` entries ×2, baseline file,
manifest row, `docs/usage.md` inventory line).
**Invariants**: solution builds without it; `SkiaViewer`/`Controls`/`Controls.Elmish` (which use
`src/KeyboardInput/`) build and behave identically (FR-007); surface-drift gate stays consistent —
no orphaned baseline, no unbaselined package (FR-006, SC-004).

## Entity 3 — `FS.GG.UI.Color` public modules (orphaned, unshipped) — RETIRED/RELOCATED

| Module | Lines (.fs/.fsi) | Disposition |
|--------|------------------|-------------|
| `Palettes` | 91 / 40 | **Deleted** — dead (only `PaletteTests` used it) |
| `Contrast` (+ `Role`/`Verdict`/`ContrastResult`) | 96 / 54 | **Relocated** to `src/ColorPolicy` — live dependency of `ColorPolicy` + Feature 108 |
| `tests/Color.Tests/` | ContrastTests + PaletteTests | **Deleted** (coverage note: research R3) |

**State**: `src/Color/` had no surface baseline and was excluded from the feed (verified — no
`FS.GG.UI.Color.txt`). **Invariant**: no surface-baseline or package-feed change for Color (FR-010,
SC-004) — only internal relocation.

## Entity 4 — `ColorPolicy` (internal, live) — PRESERVED + RELOCATED

| Field | Value |
|-------|-------|
| Kind | `module internal ColorPolicy`, no `.fsi`, namespace `FS.GG.UI.Color` (Feature 127) |
| Current home | `src/Color/ColorPolicy.fs` (281 lines), reached by `Controls.Tests` via `InternalsVisibleTo` |
| Target home | `src/ColorPolicy/ColorPolicy.fs` (verbatim; `IsPackable=false`) |
| Depends on | `FS.GG.UI.Scene` (`Role` shadowing handled in-file) + `Contrast` (relocated alongside) |
| Consumers | `Controls.Tests` (Feature 127, 131) via IVT; scripts `validate-design-system-template.fsx` + `generate-policy-report.fsx` via `#load` |

**State transition**: `src/Color/ColorPolicy.fs` → `src/ColorPolicy/ColorPolicy.fs`; the
`InternalsVisibleTo Controls.Tests` grant follows it into `ColorPolicy.fsproj`; consumer code is
**unchanged** (namespace preserved); the two scripts' `#load` paths update.
**Invariants**: `Controls.Tests` compiles and passes its color-policy assertions unchanged (FR-009,
SC-006); the policy report renders byte-identically (the Feature 127 drift gate stays green).

## Reference-graph summary (after the feature)

```text
tools/Rendering.Harness  ◄── Rendering.Harness.Tests (ProjectReference)
                         ◄── Layout/Scene/SkiaViewer/Controls.Tests (linked TestAssertions.fs)
                         ◄── 3 helper scripts, 5 FSX evidence scripts, 1 skill doc

src/KeyboardInput  ◄── SkiaViewer, Controls, Controls.Elmish, KeyboardInput.Tests   (unchanged)

src/ColorPolicy (IsPackable=false, IVT→Controls.Tests, ns FS.GG.UI.Color)
   ├── Contrast (public-within-assembly; .fsi kept)  ◄── ColorPolicy, Controls.Tests/Feature108
   └── ColorPolicy (module internal)                 ◄── Controls.Tests/Feature127,131; 2 scripts
   depends on → src/Scene

(removed: src/Input, tests/Input.Tests, src/Color/Palettes, tests/Color.Tests,
          FS.GG.UI.Input baseline + manifest row)
```
