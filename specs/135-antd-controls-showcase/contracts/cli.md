# Contract: CLI (`AntShowcase.App`)

Mirrors G1's CLI. The App is the only edge that performs I/O (window, screenshot, file writes).

## Subcommands

| Command | Behavior | Mode |
|---|---|---|
| `list` | Print every page id + title + kind (`Catalog`/`Template`) and the catalog/page counts. Pure; always exit 0. | headless |
| `interactive [<page-id>]` | Open a GL window (`runInteractiveApp`) starting on `<page-id>` (default: first family page) under the Ant theme; app-bar toggles antLight/antDark. **GL-gated.** | windowed |
| `evidence --seed <N> [--page <id>]` | For each page (or one), replay its seeded `FrameInput` script via `Perf.runScript`, capture an offscreen screenshot via `captureScreenshotEvidence`, and write the per-page evidence record. **CI-facing path.** | headless |
| `coverage` | Run `CoverageMap.check` over Catalog pages; print the summary; exit non-zero on drift. | headless |

## Exit codes

| Code | Meaning |
|---|---|
| 0 | success — incl. a **clean degrade-and-disclose** when no display/GL is available (FR-013): the run discloses the reason (`UnsupportedHostReason`/`BlockedStage`), writes records with `ProvesScreenshot=false`, and exits 0 (never hangs, never fabricates a pass). |
| non-zero | a real failure: coverage drift (`coverage`), a page that fails to render, a seed/script mismatch, or a missing required package (e.g. `FS.GG.UI.Themes.AntDesign` not on the feed — see quickstart V0). |

## Determinism (FR-011 / SC-004)

`evidence --seed N` uses only the seeded `FrameInput` scripts and pure `update` — no wall-clock, no RNG.
Two runs with the same seed produce **byte-identical** `run.json`/`state.txt` (the `frame.png` byte-identity
holds where a stable GL/offscreen surface is available; otherwise the disclosed-degrade record is itself
deterministic).
