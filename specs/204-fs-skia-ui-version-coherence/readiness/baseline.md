# Test baseline — full red/green set

- Config: `Debug`
- Projects: 21  ·  Green: 21  ·  Red: 0

| Project | Result | Summary |
|---|---|---|
| `tests/Build.Tests/Build.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 13 ms - Build.Tests.dll (net10.0) |
| `tests/Canvas.Tests/Canvas.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17, Duration: 20 ms - Canvas.Tests.dll (net10.0) |
| `tests/Controls.Tests/Controls.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   949, Skipped:     1, Total:   950, Duration: 19 s - Controls.Tests.dll (net10.0) |
| `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14, Duration: 28 ms - Diagnostics.Tests.dll (net10.0) |
| `tests/Elmish.Tests/Elmish.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   211, Skipped:    17, Total:   228, Duration: 630 ms - Elmish.Tests.dll (net10.0) |
| `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 33 ms - KeyboardInput.Tests.dll (net10.0) |
| `tests/Layout.Tests/Layout.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    79, Skipped:     0, Total:    79, Duration: 810 ms - Layout.Tests.dll (net10.0) |
| `tests/Lib.Tests/Lib.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    30, Skipped:     0, Total:    30, Duration: 58 ms - Lib.Tests.dll (net10.0) |
| `tests/Package.Tests/Package.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   110, Skipped:     0, Total:   110, Duration: 90 ms - Package.Tests.dll (net10.0) |
| `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   209, Skipped:     0, Total:   209, Duration: 616 ms - Rendering.Harness.Tests.dll (net10.0) |
| `tests/Scene.Tests/Scene.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    77, Skipped:     0, Total:    77, Duration: 131 ms - Scene.Tests.dll (net10.0) |
| `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   207, Skipped:     0, Total:   207, Duration: 672 ms - SkiaViewer.Tests.dll (net10.0) |
| `tests/Smoke.Tests/Smoke.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     4, Skipped:     3, Total:     7, Duration: 9 ms - Smoke.Tests.dll (net10.0) |
| `tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 89 ms - Symbology.Render.Tests.dll (net10.0) |
| `tests/Symbology.Tests/Symbology.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   447, Skipped:     0, Total:   447, Duration: 260 ms - Symbology.Tests.dll (net10.0) |
| `tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11, Duration: 38 ms - SymbologyBoard.Tests.dll (net10.0) |
| `tests/Testing.Tests/Testing.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   104, Skipped:     0, Total:   104, Duration: 106 ms - Testing.Tests.dll (net10.0) |
| `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88, Duration: 2 s - AntShowcase.Tests.dll (net10.0) |
| `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34, Duration: 724 ms - ControlsGallery.Tests.dll (net10.0) |
| `samples/SampleApps/SampleApps.Tests/SampleApps.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 258 ms - SampleApps.Tests.dll (net10.0) |
| `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   171, Skipped:     1, Total:   172, Duration: 1 s - SecondAntShowcase.Tests.dll (net10.0) |

---

## Setup & Foundational evidence (Feature 204)

### T001 — No-regression baseline
21 test projects discovered (solution + `tests/Package.Tests` + `samples/**`), **21 green / 0 red**.
No pre-existing reds to disclose. (Table above.)

### T002 — Tooling / profiles
- `dotnet --version` → `10.0.301`.
- `dotnet new fs-gg-ui` installed from `.template.config/template.json`; `--profile` choices are exactly
  `app  headless-scene  governed  sample-pack`.
- Local feed dir `~/.local/share/nuget-local/` present; registered global source `local-feed` → Enabled.

### T003 — Cross-repo write path (no writes yet)
- `gh auth status` → account `EHotwagner`, active, github.com, scopes incl. `repo`.
- `gh api repos/FS-GG/.github` → resolves (`FS-GG/.github`).
- `gh issue view 1 --repo FS-GG/FS.GG.Rendering` → **OPEN**, title
  "[cross-repo] fs-gg-ui template drifted from FS.GG.UI framework HEAD (fs-skia-ui-version)",
  labels `cross-repo`, `cross-repo:request`, `blocked`.

### T005/T006 — Pack + re-pin (resolution version)
**Verified pinned version: `0.1.50-preview.1`.**

Deviation from the plan's *expected* `0.1.51-preview.1`, recorded per the "version is a label on a
verified set" guidance (research R3) and the read-only-`src/**` constraint (plan Structure Decision):

- The in-repo packer (`scripts/refresh-local-feed-and-samples.fsx` → harness `package-feed --pack`)
  packs `dotnet pack FS.GG.Rendering.slnx -c Release`; the package version is MSBuild `<Version>`,
  which in root `Directory.Build.props` is the placeholder `0.1.0-preview.1`. A bare repack therefore
  does **not** mint `0.1.51`; producing `0.1.51` would require a manual `src` version bump, which is
  out of scope (this feature treats `src/**` as read-only and changes no packable project).
- The feed already holds a **complete, coherent 16-package `0.1.50-preview.1` set** — the latest
  framework pack, superseding Feature 201's `0.1.49`. Pinning here makes
  `template pin == framework's latest coherent pack` (no drift), which is exactly what request #1 asks.
- To make the snapshot's reproducibility real (SM-D), the 16 `FS.GG.UI.*` packages were **re-packed
  from current HEAD** at `0.1.50-preview.1`:
  `dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local -p:Version=0.1.50-preview.1`
  → 16/16 `Successfully created package`, no build errors. Re-checkout + re-pack reproduces the set.
- SM-A confirmed: all 16 real IDs present at `0.1.50-preview.1`; phantom `FS.GG.UI.Color` /
  `FS.GG.UI.SkillSupport` **absent** (removed in T004).
