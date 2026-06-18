# Ant Design Controls Showcase (G3)

A runnable sample that renders **all 96 catalog controls** across **13 navigable family
pages** under the shipped **`FS.GG.UI.Themes.AntDesign`** theme in both **Ant light and
Ant dark**, plus the **six enterprise template pages** (workbench, list, detail, form,
result, exception) composed **only from catalog controls**. It consumes the framework
**only through its packed `FS.GG.UI.*` package surface** (no `src/` project references) —
building and running it against the local feed *is* the proof the Ant-theme consumer path
works end to end (SC-006).

The headline payoff of the Workstream-F design-system arc: the *same* semantic control set
as the G1 gallery, restyled to the Ant visual language, **with no control forks** — controls
never branch on theme identity; antLight↔antDark differ only by resolved visuals (FR-008).

This is **Workstream G3 only** (Ant restyle + enterprise templates). G4 (wiring sample runs
into the perf/CI corpus) and dedicated Ant Charts dashboards (feature 133) are out of scope.

## Precondition — refresh the local feed (quickstart V0 / research R1)

AntShowcase consumes the framework through the configured local NuGet feed, not through
`src/` project references. Before building, pack the current FS.GG.UI package set into the
feed and clear the global package cache so stale package contents are not reused:

```sh
# from the repo root:
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx -c Release --no-restore
dotnet pack FS.GG.Rendering.slnx -c Release --no-build -o ~/.local/share/nuget-local
dotnet nuget locals global-packages --clear
```

The sample currently pins FS.GG.UI packages to `0.1.23-preview.1`. Verify the feed with:
`ls ~/.local/share/nuget-local/FS.GG.UI.Themes.AntDesign.0.1.23-preview.1.nupkg`.

## Layout

```
samples/AntShowcase/
├── nuget.config                 # local packed feed → ~/.local/share/nuget-local/
├── Directory.Build.props        # shadows the repo root; net10.0, FS0078-as-error
├── Directory.Packages.props     # disables central package management for the sample
├── AntShowcase.Core/            # pure, testable core (no GL, no I/O)
├── AntShowcase.App/             # thin executable: the edge / interpreter
└── AntShowcase.Tests/           # Expecto suite (outside the default test tier)
```

The **Core** holds the page registry (13 catalog + 6 template pages), the coverage map,
seeded demo state, the pure MVU `Model`/`update` (incl. the form-validation transitions),
the `AntTheme` resolver, the `Shell` view, the seeded headless scripts, and the
deterministic evidence record. The **App** is the edge that turns Core into either a live
window (`runInteractiveApp`) or a headless evidence run (`Perf.runScript` +
`captureScreenshotEvidence`). Tests exercise the public functions exactly as a downstream
app would.

## Two modes

1. **Interactive windowed mode** — a GL-gated MVU app: top app bar (antLight/antDark
   toggle), left nav rail of all 19 pages, scrolling content, bottom status strip. The
   preferred inspection size is `1600x1000`; the documented minimum accepted size is
   `1280x800`. On a host with no live window/GL it discloses the reason and exits 0.
2. **Headless deterministic evidence mode** — the CI-facing path. Per page it replays a
   seeded `FrameInput` script for the golden state outcome and captures an offscreen
   screenshot, writing a per-page record that **discloses what it is not authoritative
   for** and **degrades cleanly when no display/GL is present**.
3. **Visual-readiness mode** — the maintainer inspection path. It captures every requested
   page/theme screenshot, validates completeness, writes per-theme contact sheets, and
   blocks accepted readiness until reviewer classifications are present and clear.

## Build & run

```sh
cd samples/AntShowcase
dotnet build AntShowcase.App/AntShowcase.App.fsproj -c Release

dotnet run --project AntShowcase.App -c Release -- coverage      # 96/96 mapped, 0 drift
dotnet run --project AntShowcase.App -c Release -- list          # 13 catalog + 6 template pages
dotnet run --project AntShowcase.App -c Release -- evidence --seed 1   # byte-identical, disclosed
dotnet run --project AntShowcase.App -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out ../../specs/162-enhance-showcase-visuals/readiness/visual-evidence
dotnet run --project AntShowcase.App -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --pages data-collections,charts-statistical,charts-advanced,feedback-status,tpl-form,tpl-exception --out ../../specs/162-enhance-showcase-visuals/readiness/minimum-size
dotnet run --project AntShowcase.App -c Release -- visual-readiness --summarize ../../specs/162-enhance-showcase-visuals/readiness/visual-evidence --minimum-size ../../specs/162-enhance-showcase-visuals/readiness/minimum-size --out ../../specs/162-enhance-showcase-visuals/readiness
dotnet run --project AntShowcase.App -c Release -- interactive display-typography --theme dark
dotnet test AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release      # outside the default tier
```

Per-page evidence lands under `artifacts/ant-showcase/<seed>/<page-id>/`
(`run.json` / `state.txt` / `summary.md` / `frame.png`), gitignored.
Feature 162 visual-readiness evidence is intentionally committed under
`specs/162-enhance-showcase-visuals/readiness/`; degraded capture, missing screenshots, or
missing reviewer classification must remain visible in that readiness summary rather than
being treated as accepted proof.

See `coverage-report.md` for the committed control→page map and `PROVENANCE.md` for the
rebrand + template-recipe source disclosure.
