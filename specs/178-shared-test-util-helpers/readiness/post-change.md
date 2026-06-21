# Test baseline — full red/green set

- Config: `Debug`
- Projects: 18  ·  Green: 16  ·  Red: 2

| Project | Result | Summary |
|---|---|---|
| `tests/Color.Tests/Color.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15, Duration: 93 ms - Color.Tests.dll (net10.0) |
| `tests/Controls.Tests/Controls.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   932, Skipped:     1, Total:   933, Duration: 1 m 17 s - Controls.Tests.dll (net10.0) |
| `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14, Duration: 64 ms - Diagnostics.Tests.dll (net10.0) |
| `tests/Elmish.Tests/Elmish.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   209, Skipped:    17, Total:   226, Duration: 1 s - Elmish.Tests.dll (net10.0) |
| `tests/Input.Tests/Input.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12, Duration: 128 ms - Input.Tests.dll (net10.0) |
| `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 75 ms - KeyboardInput.Tests.dll (net10.0) |
| `tests/Layout.Tests/Layout.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    79, Skipped:     0, Total:    79, Duration: 1 s - Layout.Tests.dll (net10.0) |
| `tests/Lib.Tests/Lib.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    30, Skipped:     0, Total:    30, Duration: 126 ms - Lib.Tests.dll (net10.0) |
| `tests/Package.Tests/Package.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     8, Passed:    98, Skipped:     0, Total:   106, Duration: 212 ms - Package.Tests.dll (net10.0) |
| `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   201, Skipped:     0, Total:   201, Duration: 729 ms - Rendering.Harness.Tests.dll (net10.0) |
| `tests/Scene.Tests/Scene.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    70, Skipped:     0, Total:    70, Duration: 294 ms - Scene.Tests.dll (net10.0) |
| `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   207, Skipped:     0, Total:   207, Duration: 922 ms - SkiaViewer.Tests.dll (net10.0) |
| `tests/Smoke.Tests/Smoke.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     4, Skipped:     3, Total:     7, Duration: 21 ms - Smoke.Tests.dll (net10.0) |
| `tests/Testing.Tests/Testing.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   104, Skipped:     0, Total:   104, Duration: 472 ms - Testing.Tests.dll (net10.0) |
| `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88, Duration: 1 m 4 s - AntShowcase.Tests.dll (net10.0) |
| `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     2, Passed:    32, Skipped:     0, Total:    34, Duration: 6 s - ControlsGallery.Tests.dll (net10.0) |
| `samples/SampleApps/SampleApps.Tests/SampleApps.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 8 s - SampleApps.Tests.dll (net10.0) |
| `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   171, Skipped:     1, Total:   172, Duration: 2 s - SecondAntShowcase.Tests.dll (net10.0) |

## Red projects (known pre-existing failures unless this is a regression)
- `tests/Package.Tests/Package.Tests.fsproj` (exit 1): Failed!  - Failed:     8, Passed:    98, Skipped:     0, Total:   106, Duration: 212 ms - Package.Tests.dll (net10.0)
- `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` (exit 1): Failed!  - Failed:     2, Passed:    32, Skipped:     0, Total:    34, Duration: 6 s - ControlsGallery.Tests.dll (net10.0)

---

## Feature 178 — refactor evidence (SC-001 … SC-006)

**No-regression (SC-001).** Post-change red/green set is **identical** to `baseline.md`: 16 green,
2 red. The two reds are the documented pre-existing package-feed failures, unchanged in count and
identity:
- `tests/Package.Tests` — Feature128 design-system template validation (GV-1…GV-7) + Feature163
  package-feed pin check (`AntShowcase … package pins match source-controlled versions`). Neither
  touches finders/hashing/clamp.
- `samples/ControlsGallery/ControlsGallery.Tests` — stale-pin package-feed red.
No new failures. The surface-sensitive `Package.Tests` SurfaceAreaTests / PackageApiReferenceTests
**pass**, so adding the non-packed `tests/TestSupport` assembly did not perturb any public-surface
baseline.

**Public surface unchanged (SC-005, FR-007).** `git diff -- '*.fsi'` vs `main` is empty — no
published-surface signature changed. The three new helpers are `module internal` (Hashing, Numeric)
or a non-packed test assembly (TestSupport); none adds package surface.

**Duplicate elimination (SC-002/003/004).**
- US1: repo-wide grep finds **zero** local repository-root finders outside `tests/TestSupport`
  (Families A `findRepositoryRoot`, B inline `FS.GG.Rendering.slnx` walk, C `repoRoot`/`Directory.Packages.props`
  all removed across 55 files / 9 projects). Remaining `FS.GG.Rendering.slnx` occurrences are
  `dotnet build/pack` command-argument strings, not finders.
- US2: `0xcbf29ce484222325UL` appears **only** in `src/Controls/Internal/Hashing.fs`; the four folds
  (Composition.fnv1a, Control.hashScene, Control.fingerprint*, RetainedRender.feature159Hash) draw
  the constants + core `step` from it with byte-identical output (Feature159 + Fingerprint suites
  green).
- US3: one `let clamp` remains, in `src/Shared/Numeric.fs` (linked into Controls + SkiaViewer); the
  three local copies are gone. `Layout.clampNonNegative` (a different function) is untouched.

**Net source reduction (SC-005).** Code-only diff vs `main`: **268 insertions / 542 deletions
(net −274 lines)**, replacing copy-pasted duplication with 77 lines of shared, single-sourced
helpers (RepositoryRoot.fs 34, Hashing.fs 31, Numeric.fs 12).

**Independent shippability (SC-006).** Each story is a separate, individually-green change unit
(US1 → tests only; US2 → Controls folds; US3 → Controls + SkiaViewer clamp) and is independently
revertible; the full build + suite stayed green after each.
