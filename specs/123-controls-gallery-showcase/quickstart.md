# Quickstart: Controls Gallery Showcase

Validation/run guide for the sample. It consumes the packed `FS.GG.UI.*` packages
(`0.1.0-preview.1`) from `~/.local/share/nuget-local/` — **no project references into
`src/`**. See [contracts/](./contracts/) for the CLI, page registry, and evidence
schema; [data-model.md](./data-model.md) for entity shapes.

## Prerequisites

- .NET SDK `10.x` (`dotnet --version` ≥ 10.0.301).
- The framework packages packed into the local feed:
  ```sh
  dotnet pack FS.GG.Rendering.slnx -c Release   # output → ~/.local/share/nuget-local/
  ```
  (Already present in this environment.) The sample's `nuget.config` points at that
  feed.
- Interactive mode additionally needs an X11/GL desktop; **headless evidence mode and
  all tests do not**.

## Build

```sh
dotnet build samples/ControlsGallery/ControlsGallery.App/ControlsGallery.App.fsproj
```

A successful build is itself the **public-consumer-path proof** (SC-005): the gallery
compiled against the packages only.

## Scenario 1 — Browse every control (US1 / SC-001, SC-006)

```sh
dotnet run --project samples/ControlsGallery/ControlsGallery.App -- interactive
```

Expected: a windowed gallery with a left rail of **10 pages**; selecting each page
renders its grouped controls with seeded representative content and no rendering
errors. Every catalog control is reachable in ≤ 2 actions (select page, scroll).

If no display/GL: the command prints a disclosed reason and exits 0 (no hang).

## Scenario 2 — Coverage check (US1 / FR-003 / SC-001)

```sh
dotnet run --project samples/ControlsGallery/ControlsGallery.App -- coverage-check
# or, with reporting:
dotnet test samples/ControlsGallery/ControlsGallery.Tests --filter Coverage
```

Expected: exit 0 and "52/52 controls mapped, 10 pages, 0 unreferenced, 0 duplicated".
Adding/removing a catalog control without updating the registry makes this **fail**
(intentional drift detection).

## Scenario 3 — Switch theme & accent (US2 / SC-003)

In interactive mode, toggle Light/Dark in the app bar and pick an accent (Indigo /
Teal on Slate). The whole gallery restyles cohesively.

```sh
dotnet test samples/ControlsGallery/ControlsGallery.Tests --filter ThemeInvariance
```

Expected: for the same page, the control tree shape and accessibility metadata are
**identical** across Light/Dark × accent variants; only resolved visuals differ.

## Scenario 4 — Deterministic headless evidence (US3 / SC-002, SC-004)

```sh
# Run twice with the same seed and diff for byte-identity:
dotnet run --project samples/ControlsGallery/ControlsGallery.App -- evidence --seed 1234 --out /tmp/ev-a
dotnet run --project samples/ControlsGallery/ControlsGallery.App -- evidence --seed 1234 --out /tmp/ev-b
diff -r /tmp/ev-a /tmp/ev-b   # expect: no differences (run.json + state.txt byte-identical)
```

Expected per page under `<out>/1234/<page-id>/`: `run.json`, `summary.md`, `state.txt`,
and `frame.png` (when GL present). Every `run.json` carries a **non-empty
`notAuthoritativeFor`**. On a no-GL host: `frame.png` is omitted,
`screenshot.provesScreenshot=false` with a stated reason, and the run exits 0.

```sh
dotnet test samples/ControlsGallery/ControlsGallery.Tests --filter Determinism
dotnet test samples/ControlsGallery/ControlsGallery.Tests --filter Degrade
```

## Scenario 5 — Pointer/keyboard interaction (US4 / FR-012)

Interactive mode: clicking buttons, typing into text/numeric inputs, toggling
checkboxes/switches, and changing selections produce visible state changes (input →
MVU → repaint). Display-only controls (label, separator, badge) simply render. The
seeded scripts in `evidence` mode exercise the same interaction contract headlessly.

## What this does NOT prove (disclosure)

- Headless evidence proves **determinism, tree-equality, and non-blank offscreen
  pixels** — **not** renderer-vs-desktop pixels, live-host behavior, or timing.
- Interactive mode is **GL-gated and advisory**; it is never part of the required CI
  gate (FR-016). The CI-facing path is the headless `evidence` + `coverage-check` +
  the deterministic Expecto suites.
