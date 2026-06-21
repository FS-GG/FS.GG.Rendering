# Quickstart — Validating Feature 179 (Placement & Orphan Decisions)

This feature changes no runtime behavior, so validation = **a captured build/test baseline diffed
after each story**. There is no app to smoke (see plan's standing-assumption note).

## Prerequisites

- .NET `net10.0` SDK; repo restored.
- Local NuGet feed at `~/.local/share/nuget-local/` (only relevant to the package-feed lanes).
- A clean working tree on `179-placement-orphan-decisions`.

## Step 0 — Capture the baseline (BEFORE any change)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
dotnet test  FS.GG.Rendering.slnx -c Release
```

Record green/red counts in `readiness/baseline.md`. Expect the two **documented** package-feed reds
(Package.Tests, ControlsGallery package-feed) and everything else green. These two reds are the only
non-green entries allowed after every story (FR-011, SC-005).

## Step 1 — After US1 (harness → `tools/`)

```bash
dotnet build FS.GG.Rendering.slnx -c Release        # solution builds at new path
dotnet test  FS.GG.Rendering.slnx -c Release        # matches baseline

# No genuine reference to the moved CLI remains (the .Tests project did NOT move):
rg -n "tests/Rendering\.Harness/"                    # → zero CLI hits

# The three lanes invoke the harness at its new path with no behavior change (FR-004):
dotnet fsi scripts/run-validation-lanes.fsx
dotnet fsi scripts/check-agent-skill-parity.fsx
dotnet fsi scripts/refresh-local-feed-and-samples.fsx
```

Expected: build green; test diff = baseline; the Feature 170 retained-inspection lane test passes
(its assertion targets the unmoved `.Tests` project). See `contracts/harness-path-map.md`.

## Step 2 — After US2 (retire `FS.GG.UI.Input`)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
dotnet test  FS.GG.Rendering.slnx -c Release         # surface-drift gate passes

# Surface gate is internally consistent — only Input removed:
git diff --stat readiness/surface-baselines/         # only FS.GG.UI.Input.txt deleted
rg -n "FS.GG.UI.Input" scripts/refresh-surface-baselines.fsx   # → no manifest row
```

Expected: `src/Input/` + `tests/Input.Tests/` gone; `SkiaViewer`/`Controls`/`Controls.Elmish`
(live `src/KeyboardInput/` path) build unchanged (FR-007). See
`contracts/package-surface-changes.md`.

## Step 3 — After US3 (retire `src/Color/`, preserve `ColorPolicy`)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
dotnet test  tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "Feature108|Feature127|Feature131"

# The two policy scripts run against the relocated files:
dotnet fsi scripts/validate-design-system-template.fsx
dotnet fsi scripts/generate-policy-report.fsx        # report byte-identical (Feature127 gate)
```

Expected: `src/ColorPolicy/` builds (`IsPackable=false`, no baseline); `Controls.Tests` color suites
pass unchanged (SC-006, FR-009); `src/Color/` + `tests/Color.Tests/` gone; no surface baseline
changed for Color (FR-010). See `contracts/colorpolicy-relocation.md`.

## Final acceptance (all stories)

```bash
dotnet build FS.GG.Rendering.slnx -c Release && dotnet test FS.GG.Rendering.slnx -c Release
```

- `tests/` has zero `OutputType=Exe` **production** CLIs (SC-001); `tools/Rendering.Harness` is the
  only relocated CLI.
- Exactly one published surface changed — `FS.GG.UI.Input` removed; gate consistent (SC-004).
- Net source reduction on the order of the plan's estimate, no production code deleted (SC-003).
- Full test run = baseline; the two documented package-feed reds are the only non-green entries
  (SC-005). Record the post state in `readiness/post-change.md`.
