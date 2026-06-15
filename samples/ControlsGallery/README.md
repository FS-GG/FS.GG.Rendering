# Controls Gallery Showcase (Light / Dark)

A runnable sample that renders **all 52 catalog controls** across **exactly 10
navigable pages** on the framework's Light/Dark themes with an Indigo/Teal accent
selector — consuming the framework **only through its packed `FS.GG.UI.*` package
surface** (no `src/` project references). Building and running it *is* the proof that
the documented consumer path works end to end (SC-005).

This is **Workstream G1** (the gallery on Light/Dark). Games/productivity samples,
the Ant restyle, and feeding gallery runs into the perf corpus are out of scope.

## Layout

```
samples/ControlsGallery/
├── nuget.config                 # local packed feed → ~/.local/share/nuget-local/
├── Directory.Build.props        # shadows the repo root; net10.0, FS0078-as-error
├── Directory.Packages.props     # disables central package management for the sample
├── ControlsGallery.Core/        # pure, testable core (no GL, no I/O)
├── ControlsGallery.App/         # thin executable: the edge / interpreter
└── ControlsGallery.Tests/       # Expecto suite (outside the default test tier)
```

The **Core** holds the page registry, coverage map, seeded demo state, the pure MVU
`Model`/`update`, the `Shell` view, the seeded headless scripts, and the deterministic
evidence record. The **App** is the edge that turns Core into either a live window
(`runInteractiveApp`) or a headless evidence run (`Perf.runScript` +
`captureScreenshotEvidence`). Tests exercise the public functions exactly as a
downstream app would.

## Two modes

1. **Interactive windowed mode** — a GL-gated MVU app: top app bar (theme toggle +
   accent selector), left nav rail of the 10 pages, scrolling content, bottom status
   strip. On a host with no live window/GL it discloses the reason and exits 0.
2. **Headless deterministic evidence mode** — the CI-facing path. Per page it replays a
   seeded `FrameInput` script for the golden state outcome and captures an offscreen
   screenshot, writing a per-page record that **discloses what it is not authoritative
   for** and **degrades cleanly when no display/GL is present**.

## Build & run

```sh
# Build (this alone is the public-consumer-path proof, SC-005):
dotnet build samples/ControlsGallery/ControlsGallery.App/ControlsGallery.App.fsproj

# Browse every control (GL-gated; degrades-and-discloses with exit 0 if headless):
dotnet run --project samples/ControlsGallery/ControlsGallery.App -- interactive [--theme light|dark] [--accent indigo|teal]

# Coverage check — exits non-zero on any catalog↔registry drift (FR-003):
dotnet run --project samples/ControlsGallery/ControlsGallery.App -- coverage-check

# Deterministic headless evidence (same seed ⇒ byte-identical run.json + state.txt):
dotnet run --project samples/ControlsGallery/ControlsGallery.App -- evidence --seed 1234 [--out <dir>] [--page <page-id>]

# Tests:
dotnet test samples/ControlsGallery/ControlsGallery.Tests
```

## Pointer & keyboard interaction contract (FR-012)

Interactive controls respond visibly to input via input → MVU `update` → repaint; the
seeded `evidence` scripts exercise the same contract headlessly. The contract per
interactive family:

| Family | Interaction | Visible state change |
|--------|-------------|----------------------|
| Buttons (`button`, `icon-button`, `toggle-button`, `split-button`) | click / `Space`/`Enter` activation | click count increments; toggle flips |
| Text & numeric (`text-box`, `text-area`, `numeric-input`, `slider`) | type / drag | model value updates |
| Selection & toggles (`check-box`, `radio-group`, `switch`, `combo-box`, `list-box`, `multi-select-list`, `color-picker`) | select / toggle | selected value(s) update |
| Collections & data (`list-view`, `tree-view`, `data-grid`) | select row/node | selection state updates |
| Navigation (`tabs`, `menu`, `context-menu`, `toolbar`) | choose item | active item / command updates |

**Display-only controls are exempt** — they simply render and carry no interaction
message: `label`, `text-block`, `rich-text`, `image`, `icon`, `separator`, `badge`,
`tooltip`, `progress-bar`, `spinner`, `validation-message`, plus the layout containers
(`stack`, `grid`, `dock`, `wrap`, `border`, `panel`).

## What this does NOT prove (disclosure)

- Headless evidence proves **determinism, tree-equality, and non-blank offscreen
  pixels** — **not** renderer-vs-desktop pixels, live-host behavior, or timing. Every
  `run.json` carries a non-empty `notAuthoritativeFor`.
- Interactive mode is **GL-gated and advisory**; it is never part of the required CI
  gate (FR-016). The CI-facing path is the headless `evidence` + `coverage-check` + the
  deterministic Expecto suites.
