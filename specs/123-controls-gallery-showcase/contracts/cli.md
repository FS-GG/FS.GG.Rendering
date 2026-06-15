# Contract: Controls Gallery CLI

The `ControlsGallery.App` executable exposes three subcommands. This is the gallery's
user/automation-facing contract. The headless paths are the CI-facing surface
(FR-016) and never depend on a display.

## `interactive`

```
ControlsGallery interactive [--theme light|dark] [--accent indigo|teal]
```

- Launches the GL-windowed MVU gallery via `ControlsElmish.runInteractiveApp`.
- **GL-gated.** If `Viewer.runtimeCapability()` reports no live window/GL host, prints
  a disclosed reason and exits **0** without launching (FR-011) — it does not hang and
  does not report a fake success of *interactive* rendering.
- Optional flags set the initial theme mode / accent.

**Exit codes**: `0` launched-and-closed normally, or cleanly skipped with disclosure;
non-zero only on an actual defect (e.g. a real GL init failure classified as a defect,
not a missing desktop).

## `evidence`

```
ControlsGallery evidence --seed <int> [--out <dir>] [--page <page-id>]
```

- Runs the **headless deterministic evidence mode** over all 10 pages (or one page
  with `--page`).
- For each page: replays the seeded `FrameInput` script via `Perf.runScript` (state
  outcome) and captures a screenshot via `Viewer.captureScreenshotEvidence`
  (`OffscreenReadback`), then writes a `PageEvidenceRecord` under
  `<out>/<seed>/<page-id>/` (default `<out>` = `artifacts/controls-gallery`).
- **Deterministic**: same `--seed` ⇒ byte-identical `run.json` + `state.txt` (and
  `frame.png` where GL is present) across runs (FR-009/SC-002).
- **Degrade-and-disclose**: when no display/GL, still writes the deterministic state
  evidence, marks the screenshot `ProvesScreenshot=false` with a stated reason, and
  exits **0** (FR-011/SC-004). Every record carries a non-empty `notAuthoritativeFor`
  (FR-010).

**Exit codes**: `0` evidence produced (including disclosed degraded runs); non-zero
only if a page's seeded run fails its source acceptance criteria (SC-007) or on an
unexpected defect.

## `coverage-check`

```
ControlsGallery coverage-check
```

- Runs `Core.CoverageMap.check` against `Catalog.supportedControls`.
- Prints the result; lists any `Unreferenced` or `Duplicated` control ids.

**Exit codes**: `0` if every catalog control maps to exactly one page (52→10, none
missing, none duplicated, SC-001); **non-zero** on any drift (FR-003).

## Cross-cutting

- No subcommand reads wall-clock or randomness in a way that affects evidence output.
- All diagnostics are structured and actionable (Principle VI); GL/desktop absence is
  reported as a distinct, disclosed condition rather than a generic failure.
