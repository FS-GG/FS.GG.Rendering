# Contract — `Contrast` + `ColorPolicy` relocation to `src/ColorPolicy`

Owner-selected home (research R3). The relocation is correct **iff** `Controls.Tests` compiles and
passes its color suites unchanged, the policy report stays byte-identical, and no package surface
appears.

## New project: `src/ColorPolicy/`

| File | Source | Notes |
|------|--------|-------|
| `ColorPolicy.fsproj` | NEW | `<IsPackable>false</IsPackable>`; `<InternalsVisibleTo Include="Controls.Tests" />`; ProjectReference `..\Scene\Scene.fsproj`; compile order **`Contrast.fsi`, `Contrast.fs`, `ColorPolicy.fs`** (ColorPolicy depends on Contrast). Assembly name `ColorPolicy`. |
| `Contrast.fsi` | MOVED verbatim from `src/Color/Contrast.fsi` | namespace `FS.GG.UI.Color`; declares `Role`/`Verdict`/`ContrastResult` + `module Contrast`. |
| `Contrast.fs` | MOVED verbatim | unchanged. |
| `ColorPolicy.fs` | MOVED verbatim | `module internal ColorPolicy`, no `.fsi`; unchanged. |

**Namespace preserved (`FS.GG.UI.Color`)** so every consumer is edit-free — this is the core
behavior-preservation lever (SC-006). The project is *named* `ColorPolicy` but its types stay under
`FS.GG.UI.Color`.

## Consumer updates

| Consumer | Old | New |
|----------|-----|-----|
| `tests/Controls.Tests/Controls.Tests.fsproj` (~line 180) | `ProjectReference ..\..\src\Color\Color.fsproj` | `..\..\src\ColorPolicy\ColorPolicy.fsproj` |
| `Controls.Tests` Feature 108/127/131 `.fs` | `FS.GG.UI.Color.*` / `ColorPolicy.*` | **unchanged** (namespace + IVT preserved) |
| `scripts/validate-design-system-template.fsx:31–32` | `#load "../src/Color/Contrast.fs"` / `…/ColorPolicy.fs` | `#load "../src/ColorPolicy/Contrast.fs"` / `…/ColorPolicy.fs` |
| `scripts/generate-policy-report.fsx` | `#load` of `src/Color/Contrast.fs` + `ColorPolicy.fs` | `src/ColorPolicy/…` |

`Controls.Tests` reaches `module internal ColorPolicy` via the IVT grant that now lives in
`src/ColorPolicy/ColorPolicy.fsproj`; it reaches public `Contrast` via the ProjectReference. No new
grant is needed beyond moving the existing one.

## Deletions

- `src/Color/Palettes.fsi`, `src/Color/Palettes.fs` (dead — only `PaletteTests` used them).
- `src/Color/` (`Color.fsproj`, README, the now-moved files' originals) entirely.
- `tests/Color.Tests/` entirely (`ContrastTests.fs`, `PaletteTests.fs`, `Program.fs`,
  `Color.Tests.fsproj`). Coverage note: research R3 — `Contrast` keeps indirect coverage via
  Feature 108 + Feature 127; re-homing `ContrastTests.fs` is an out-of-scope bounded follow-up.
- `.slnx`: remove `src/Color/Color.fsproj` and `tests/Color.Tests/Color.Tests.fsproj`; add
  `src/ColorPolicy/ColorPolicy.fsproj`.

## Invariants

- **No surface change**: `src/ColorPolicy` is `IsPackable=false`, no baseline, no manifest row;
  `FS.GG.UI.Color` never had a baseline (FR-010, SC-004).
- **Package graph**: `src/ColorPolicy` deps = `Scene` only (matches old `src/Color`). The only new
  edge is the test-only `Controls.Tests → src/ColorPolicy` replacing `Controls.Tests → src/Color`.
  No production `src/*` edge is added (none referenced `src/Color`).
- **Behavior**: `Controls.Tests` color suites (Feature 108/127/131) pass unchanged; the committed
  color-policy report renders byte-identically (Feature 127 drift gate green) — SC-006, FR-009.
- **Scripts**: both policy scripts run and produce identical output after the `#load` path update.
