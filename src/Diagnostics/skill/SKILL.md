---
name: fs-gg-diagnostics
description: Diagnose FS.GG.Rendering runtime diagnostics, readiness evidence, visual/readback evidence, responsiveness and render-lag traces, package-feed proof, skill-parity checks, and public-surface drift. Use when investigating diagnostics, readiness caveats, visual responsiveness, render lag, environment limitations, or local validation artifacts.
---

# Diagnostics Capability

## Scope

Use this skill for local diagnosis across `FS.GG.UI.Diagnostics`, readiness
artifacts, SecondAntShowcase validation commands, render-lag or responsiveness
traces, package-feed proof, public-surface drift, and skill-parity checks.

Pair it with the package-specific skills when a diagnosis leads into Controls,
Elmish, Scene, SkiaViewer, or Testing implementation work.

## First files

- `src/Diagnostics/Diagnostics.fsi`: public diagnostic taxonomy, readiness
  tokens, summary records, and artifact renderers.
- `src/Diagnostics/README.md`: package boundary; this package is pure
  classification and artifact rendering, not telemetry.
- `scripts/diagnostics-prelude.fsx`: FSI smoke for public diagnostics API.
- `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs`: CLI command map.
- `samples/SecondAntShowcase/SecondAntShowcase.App/Diagnostics.fs`: sample
  diagnostics artifact writer.
- `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs`: live and
  substitute responsiveness evidence writer.
- `samples/SecondAntShowcase/SecondAntShowcase.App/RenderLagProbe.fs`: focused
  render-lag probe.
- `samples/SecondAntShowcase/SecondAntShowcase.App/VisualReadiness.fs`: visual
  readiness and review artifacts.
- `tests/Rendering.Harness/SkillParity.fs` and
  `scripts/check-agent-skill-parity.fsx`: local skill inventory and parity
  diagnostics.
- `scripts/refresh-local-feed-and-samples.fsx`: package-feed proof wrapper.
- `scripts/refresh-surface-baselines.fsx`: public surface baseline refresh.

## Readiness rules

- Treat status tokens as contracts. `Accepted`, `Blocked`, `ReviewRequired`, and
  `EnvironmentLimitedStatus` render as readiness tokens through
  `RuntimeDiagnostics.readinessStatusToken`.
- Do not report canceled, timed-out, skipped, synthetic, substitute, degraded,
  pending-review, or environment-limited outputs as accepted readiness. Keep the
  caveat visible in the summary and final report.
- Live responsiveness acceptance requires a visible, focusable desktop session,
  `--require-live`, measured presentation timing, complete interactive-family
  coverage, passing budgets, complete artifact writes, and agreeing
  `summary.json` / `summary.md`.
- Headless or forced-substitute responsiveness output is useful diagnostic data,
  but it is not accepted live input-to-present evidence.
- `DiagnosticException` changes readiness interpretation; it never hides the
  original diagnostic. Invalid or unmatched exceptions are developer-action
  diagnostics.
- `specs/*/readiness/` paths are ignored by default. If committing readiness
  evidence, add a narrow `.gitignore` allowlist and record `git check-ignore -v`
  proof.
- Avoid concurrent `dotnet test` runs for the same project and configuration
  unless each run has isolated output or a distinct `BaseOutputPath`.

## Diagnostic command map

Build the package before FSI or sample checks that reference compiled
assemblies:

```bash
dotnet build src/Diagnostics/Diagnostics.fsproj -c Release
dotnet fsi scripts/diagnostics-prelude.fsx
dotnet test tests/Diagnostics.Tests/Diagnostics.Tests.fsproj -c Release --filter Feature169
```

Run sample diagnostics and inspect the generated JSON, Markdown, and JSONL
artifacts:

```bash
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- diagnostics --out artifacts/second-ant-showcase/diagnostics --json
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- diagnostics --out artifacts/second-ant-showcase/diagnostics-verbose --verbose
```

For render-lag diagnosis, use the probe first. It sets
`FS_GG_RENDER_LAG_TRACE=1`; set the same environment variable manually when
running other focused repros or tests.

```bash
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- render-lag-probe --scenario button-click --theme light
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- render-lag-probe --scenario page-change --theme dark
```

For responsiveness evidence, prefer all-interactive live runs. If no `DISPLAY`
or `WAYLAND_DISPLAY` is present, expect environment-limited substitute output.

```bash
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --all-interactive --require-live --out artifacts/responsiveness --json
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme dark --all-interactive --require-live --out artifacts/responsiveness --json
```

For visual readiness and showcase regressions:

```bash
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- coverage
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- evidence --seed 1 --out artifacts/second-ant-showcase/evidence
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out artifacts/second-ant-showcase/visual-preferred
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- review-findings --out artifacts/second-ant-showcase --fail-on-unresolved
```

For package-consuming sample proof, include the sample explicitly:

```bash
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase --mode proof --out artifacts/package-feed/second-ant-showcase
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- package-feed --sample samples/SecondAntShowcase --mode proof --out artifacts/package-feed/second-ant-showcase
```

For local skill inventory and wrapper drift, write reports outside tracked docs
unless intentionally updating committed reports:

```bash
dotnet fsi scripts/check-agent-skill-parity.fsx --out /tmp/fs-gg-skill-parity --report /tmp/fs-gg-skill-parity/report.md --summary-json /tmp/fs-gg-skill-parity/summary.json --fail-on high
```

For intended public API changes, build Debug first and refresh baselines:

```bash
dotnet build FS.GG.Rendering.slnx -c Debug
dotnet fsi scripts/refresh-surface-baselines.fsx
git status --short readiness/surface-baselines
```

## Drive interaction → capture the resulting frame (closed loop)

To self-verify an interaction bug end-to-end — "click/scroll/focus here, then see the frame that
results" — drive the real host headlessly and capture the POST-interaction frame. This is
deterministic (the state fold needs no GL) and the PNG readback degrades-and-discloses on a no-GL
host (do not report an unproven capture as green).

The enabling primitive is `ControlsElmish.Perf.runScriptToModel` (Feature 175 S1): it folds a
`FrameInput` script (clicks/keys/ticks) through the REAL retained route and returns the FINAL model,
so you can render the frame AFTER the interaction (the plain `Perf.runScript` returns only metrics; the
sample `evidence` command captures the static initial page, not the post-script frame). Recipe:

```fsharp
// 1. DRIVE: fold a click/scroll/focus script to the final model (pure, headless, byte-stable).
let finalModel, _metrics = ControlsElmish.Perf.runScriptToModel host size script
// 2. CAPTURE: render the post-interaction frame and read it back offscreen → PNG.
let scene = SceneNode.Group [ (Control.renderTree host.Theme size (host.View size finalModel)).Scene ]
let result = Viewer.captureScreenshotEvidence request { options with PresentMode = ViewerPresentMode.OffscreenReadback } scene
// result.ProvesScreenshot = a real PNG was written; otherwise the capture is environment-limited (disclose it).
```

For per-interaction assertions (the dispatched messages, resolved focus, and scene after each click,
threaded across steps), use the reusable repro harness `InteractionRepro` (Feature 175 S2):
`start size host |> click "control-id"` then read `.LastMsgs` / `focusAt` / `scene`.

Worked, runnable examples (the templates to copy):

- `tests/SkiaViewer.Tests/Feature175InteractionCaptureTests.fs` — drive a click script, assert the
  post-interaction model, capture the resulting frame to a PNG (or disclose environment-limited).
- `tests/Elmish.Tests/Feature175InteractionReproTests.fs` — the `InteractionRepro` harness reproducing
  the toggle flip-both-ways bug in a few lines (generalises `Feature175NavFocusTests`/`Feature175ToggleTests`).

## Unkeyed-sibling visual-state bleed (silent)

If hover/focus/press appears on the WRONG control or on ALL same-kind siblings at once, suspect the
unkeyed-sibling collapse: visual state stamps by `Key ?? Kind`, so unkeyed interactive siblings of the
same kind share one stamp id (routing uses the stable `RetainedId` and still works, so the bug is
silent). Run the analyzer `FS.GG.UI.Controls.Diagnostics.unkeyedInteractiveSiblings root` over the
control tree — it returns one `MissingStableKey` warning per colliding (parent, kind) group. Fix:
give each sibling a distinct `Control.withKey`.

## Triage workflow

1. Identify the lane: runtime diagnostics, render-lag, responsiveness, visual
   readiness, package-feed, surface drift, or skill-parity.
2. Read the first file for that lane, then run the smallest command that writes
   durable artifacts.
3. Inspect status tokens, exit code, artifact-write status, environment facts,
   caveats, and whether JSON and Markdown agree.
4. Separate implementation defects from environment limitations. Missing window
   system, hidden or unfocusable windows, missing presentation boundaries, and
   headless substitutes keep readiness non-accepted.
5. Report the exact command, exit code, key artifact paths, readiness token,
   accepted-vs-diagnostic-only decision, and any next developer action.

## Related

- `[[fs-gg-testing]]` owns retained inspection and validation helper contracts.
- `[[fs-gg-skiaviewer]]` owns live viewer and screenshot presentation behavior.
- `[[fs-gg-ui-widgets]]` owns Controls rendering, retained render, and
  inspection producers.
- `[[fs-gg-elmish]]` owns Elmish state/update/effect boundaries.
- `[[speckit-implement]]` owns feature task execution and readiness reporting.
