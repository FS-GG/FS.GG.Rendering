# Render Lag GL Boundary Analysis

Timestamp: 2026-06-19 23:40:41 +0200

Commit analyzed: `313fc24257e665d6678c6466daccd981351dac35`

Scope: live desktop GL path timing for the SecondAntShowcase render-lag
problem. This report extends the earlier code-path analysis in
`docs/reports/20260619-213244+0200-second-antshowcase-input-lag-analysis.md`
with live input-to-GL-success measurements.

## Summary

The observed 500 ms+ lag is real in the live GL path and is larger than the
initial estimate. The diagnostic runs measured approximately 1.264 s from
button activation handling to GL render success and approximately 2.777 s from
page-change activation handling to GL render success.

The dominant cost is not input queueing, model update, product view creation, or
GL buffer presentation. The dominant cost is `RetainedRender.step` inside the
Elmish/retained rendering adapter:

- Button activation: `RetainedRender.step` took 1247.503 ms out of a 1263.920 ms
  input-handle-start to GL-success path.
- Page change: `RetainedRender.step` took 2576.305 ms out of a 2777.261 ms
  input-handle-start to GL-success path.

For the button case, the product view itself took only 0.387 ms, runtime
stamping took 0.053 ms, and the next GL render success was reached about
0.199 ms after the view phase ended. For the page-change case, product view took
14.094 ms and GL paint took 172.751 ms, but those are still secondary compared
with the 2576.305 ms retained step.

Primary hypothesis: model-changing frames are blocked by expensive retained
render recomposition and scene/evidence walks. The live viewer and GL boundary
are not the primary bottleneck for the measured interactions.

## What Was Measured

Two live scripted scenarios were run through the desktop viewer path:

- `button-click`: sends `Enter`, routed by the app host to
  `PageMsg ButtonClicked`.
- `page-change`: sends `F2`, mapped by the render-lag probe host wrapper to
  `NavigateTo "text-numeric-input"`.

The probe uses the same live viewer infrastructure as the desktop app and exits
after the scripted interaction has presented follow-up frames. The probe entry
point and scenario definitions are in:

- [RenderLagProbe.fs:19](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.App/RenderLagProbe.fs#L19) - command options.
- [RenderLagProbe.fs:25](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.App/RenderLagProbe.fs#L25) - live window behavior.
- [RenderLagProbe.fs:51](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.App/RenderLagProbe.fs#L51) - host wrapper that maps `F2` to `NavigateTo "text-numeric-input"`.
- [RenderLagProbe.fs:64](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.App/RenderLagProbe.fs#L64) - scripted key sequences.
- [RenderLagProbe.fs:75](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.App/RenderLagProbe.fs#L75) - live script runner invocation.

Raw trace files from this run:

- `/tmp/fs-gg-render-lag-probe2/button.trace`
- `/tmp/fs-gg-render-lag-probe2/button.stdout`
- `/tmp/fs-gg-render-lag-probe2/page.trace`
- `/tmp/fs-gg-render-lag-probe2/page.stdout`

Probe stdout:

```text
render-lag-probe: scenario=button-click status=ok firstFramePresented=true metrics=1
render-lag-probe: scenario=page-change status=ok firstFramePresented=true metrics=1
```

Commands used:

```bash
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- render-lag-probe --scenario button-click --theme light >/tmp/fs-gg-render-lag-probe2/button.stdout 2>/tmp/fs-gg-render-lag-probe2/button.trace
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- render-lag-probe --scenario page-change --theme light >/tmp/fs-gg-render-lag-probe2/page.stdout 2>/tmp/fs-gg-render-lag-probe2/page.trace
```

## Measurement Instrumentation

The live timing data came from env-gated trace events emitted only when
`FS_GG_RENDER_LAG_TRACE=1`.

Source links:

- [SkiaViewer.fs:17](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/SkiaViewer/SkiaViewer.fs#L17) - viewer trace helper.
- [SkiaViewer.fs:2110](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/SkiaViewer/SkiaViewer.fs#L2110) - input queue trace.
- [SkiaViewer.fs:2135](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/SkiaViewer/SkiaViewer.fs#L2135) - scripted input pump trace.
- [SkiaViewer.fs:2171](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/SkiaViewer/SkiaViewer.fs#L2171) - input drain and queue-delay trace.
- [SkiaViewer.fs:2247](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/SkiaViewer/SkiaViewer.fs#L2247) - render-frame-requested trace.
- [SkiaViewer.fs:3517](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/SkiaViewer/SkiaViewer.fs#L3517) - update/view/effects timing.
- [SkiaViewer.fs:3552](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/SkiaViewer/SkiaViewer.fs#L3552) - key routing trace.
- [OpenGl.fs:27](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/SkiaViewer/Host/OpenGl.fs#L27) - GL trace helper.
- [OpenGl.fs:1216](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/SkiaViewer/Host/OpenGl.fs#L1216) - GL render-start and render-success timing.
- [ControlsElmish.fs:11](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls.Elmish/ControlsElmish.fs#L11) - Elmish trace helper.
- [ControlsElmish.fs:1153](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls.Elmish/ControlsElmish.fs#L1153) - product view timing.
- [ControlsElmish.fs:1169](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls.Elmish/ControlsElmish.fs#L1169) - retained init timing.
- [ControlsElmish.fs:1199](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls.Elmish/ControlsElmish.fs#L1199) - retained step timing and work-reduction counters.

## Data

### Button Activation

Scenario: `Enter` key routed to `PageMsg ButtonClicked`.

Relevant app source:

- [Host.fs:27](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.Core/Host.fs#L27) - `Enter` and `Space` map to `PageMsg ButtonClicked`.
- [Model.fs:172](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.Core/Model.fs#L172) - `ButtonClicked` update branch.

| Segment | Duration |
| --- | ---: |
| Input queued to input handle start | 1.863 ms |
| Queue delay reported at handle start | 0.668 ms |
| Input handle start to model update start | 12.425 ms |
| Model update | 1.981 ms |
| Product view | 0.387 ms |
| Runtime stamp | 0.053 ms |
| `RetainedRender.step` | 1247.503 ms |
| Full view phase | 1249.256 ms |
| Effects | 0.005 ms |
| View end to GL render success | 0.199 ms |
| GL paint | 0.237 ms |
| GL present | 5.916 ms |
| Input handle start to GL render success | 1263.920 ms |

Retained work counters:

| Counter | Value |
| --- | ---: |
| `remeasured` | 0 |
| `repainted` | 53 |
| `dirtyRects` | 53 |
| `replayHits` | 0 |
| `replayMisses` | 0 |

Interpretation: the button model update and product view are negligible. There
is no retained layout remeasurement in this scenario, yet the retained step
still takes 1.247 s while repainting 53 retained nodes. That strongly points to
paint/build/scene/evidence work rather than input queueing or layout.

### Page Change

Scenario: `F2` mapped by the probe host wrapper to
`NavigateTo "text-numeric-input"`.

Relevant app source:

- [Model.fs:225](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.Core/Model.fs#L225) - `NavigateTo` update branch.
- [Shell.fs:60](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.Core/Shell.fs#L60) - nav item dispatches `NavigateTo`.
- [Shell.fs:79](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.Core/Shell.fs#L79) - page content selection.
- [Shell.fs:133](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/samples/SecondAntShowcase/SecondAntShowcase.Core/Shell.fs#L133) - shell view assembly.

| Segment | Duration |
| --- | ---: |
| Input queued to input handle start | 1.869 ms |
| Queue delay reported at handle start | 0.667 ms |
| Input handle start to model update start | 11.639 ms |
| Model update | 0.413 ms |
| Product view | 14.094 ms |
| Runtime stamp | 0.095 ms |
| `RetainedRender.step` | 2576.305 ms |
| Full view phase | 2591.736 ms |
| Effects | 0.005 ms |
| View end to GL render success | 173.444 ms |
| GL paint | 172.751 ms |
| GL present | 0.311 ms |
| Input handle start to GL render success | 2777.261 ms |

Retained work counters:

| Counter | Value |
| --- | ---: |
| `remeasured` | 77 |
| `repainted` | 113 |
| `dirtyRects` | 113 |
| `replayHits` | 0 |
| `replayMisses` | 0 |

Interpretation: page navigation has meaningful extra GL paint cost after the
retained view is produced, but the retained step is still the overwhelming
bottleneck. Product view construction is 14.094 ms, so the application-level
`Shell.view` path is not the source of the multi-second delay.

### Initial Frame Baseline

Both runs also showed slow initial retained setup and first GL paint:

| Scenario | Product view | Runtime stamp | Retained init | Render retained init | First GL paint |
| --- | ---: | ---: | ---: | ---: | ---: |
| Button probe | 65.913 ms | 69.261 ms | 1141.912 ms | 1220.819 ms | 222.148 ms |
| Page probe | 66.440 ms | 69.704 ms | 1119.783 ms | 1199.463 ms | 216.142 ms |

This suggests a cold-start or first-frame cost exists too. However, the
model-changing button activation after startup still took 1247.503 ms inside
`RetainedRender.step`, so the observed lag is not only first-frame
initialization.

## Timeline

### Button Activation Timeline

The model-changing event started at `input-handle-start` for sequence 1:

```text
input-handle-start seq=1 kind=key-down payload=Enter queueDelayMs=0.668
key-routed key=Enter isDown=True messageCount=1
model-update-start msg=PageMsg_ButtonClicked
model-update-end msg=PageMsg_ButtonClicked durationMs=1.981
view-start msg=PageMsg_ButtonClicked
elmish-product-view-end durationMs=0.387
elmish-runtime-stamp-end path=step durationMs=0.053
elmish-retained-step-end durationMs=1247.503 remeasured=0 repainted=53 dirtyRects=53 replayHits=0 replayMisses=0
view-end msg=PageMsg_ButtonClicked durationMs=1249.256
gl-render-success paintMs=0.237 presentMs=5.916 readbackBytes=0
```

The gap is concentrated between `elmish-runtime-stamp-end` and
`elmish-retained-step-end`.

### Page Change Timeline

The page navigation event followed the same shape:

```text
input-handle-start seq=1 kind=key-down payload=F2 queueDelayMs=0.667
key-routed key=F2 isDown=True messageCount=1
model-update-start msg=NavigateTo_"text-numeric-input"
model-update-end msg=NavigateTo_"text-numeric-input" durationMs=0.413
view-start msg=NavigateTo_"text-numeric-input"
elmish-product-view-end durationMs=14.094
elmish-runtime-stamp-end path=step durationMs=0.095
elmish-retained-step-end durationMs=2576.305 remeasured=77 repainted=113 dirtyRects=113 replayHits=0 replayMisses=0
view-end msg=NavigateTo_"text-numeric-input" durationMs=2591.736
gl-render-success paintMs=172.751 presentMs=0.311 readbackBytes=0
```

The page change has a visible post-view GL paint cost, but it is not the main
cause of the total delay.

## Source Hotspots

The retained rendering step performs several kinds of work that can plausibly
add up to the measured stalls:

- [RetainedRender.fs:1304](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1304) - `step` begins by diffing retained state.
- [RetainedRender.fs:1313](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1313) - layout dirty set and incremental layout path.
- [RetainedRender.fs:1330](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1330) - text cache hook setup.
- [RetainedRender.fs:1425](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1425) - fresh retained node build path.
- [RetainedRender.fs:1441](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1441) - carry path for shifted subtree recomputation.
- [RetainedRender.fs:1461](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1461) - reuse-driven build/update path.
- [RetainedRender.fs:1625](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1625) - picture cache and replay walk.
- [RetainedRender.fs:1690](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1690) - offscreen diagnostic walk.
- [RetainedRender.fs:1722](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1722) - state and animation collection.
- [RetainedRender.fs:1761](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1761) - cached subtree emission, replay emission, and scene assembly.
- [RetainedRender.fs:1811](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1811) - render result assembly.
- [RetainedRender.fs:1836](https://github.com/FS-GG/FS.GG.Rendering/blob/313fc24257e665d6678c6466daccd981351dac35/src/Controls/RetainedRender.fs#L1836) - `WorkReduction` fields reported by the trace.

The important clue is that button activation reports `remeasured=0` but still
spends 1247.503 ms in `RetainedRender.step`. That makes layout an unlikely
primary cause for the button case. The remaining likely sources are retained
node rebuild/repaint, diagnostic/evidence walks, picture/replay bookkeeping,
scene assembly, and repeated full-tree descriptions/counts.

## Hypotheses

### H1: Retained Render Recomposition Dominates the Lag

Confidence: high.

Evidence:

- Button activation spends 1247.503 ms in `RetainedRender.step` and only
  0.387 ms in product view.
- Page navigation spends 2576.305 ms in `RetainedRender.step` and only
  14.094 ms in product view.
- Queue delay is under 1 ms for both cases.
- Model update is under 2 ms for both cases.
- GL presentation is under 6 ms for the button case and 1 ms for the page
  navigation case.

The lag appears after the product model has updated and before the new scene is
available to GL.

### H2: The Earlier Measurement Missed the Presentation Boundary

Confidence: high.

The earlier report was explicit that it was a code-path analysis and had not
collected live desktop timing. The now-committed render-lag probe uses the live
viewer path and records both the application update/view phases and the GL
render success phase. This explains why the earlier analysis could only
hypothesize about 500 ms stalls, while the current run can locate the measured
stall.

### H3: Layout Is Not the Button-Click Driver

Confidence: high for the button scenario, medium for page navigation.

The button activation reports `remeasured=0` but still takes 1247.503 ms inside
`RetainedRender.step`. That excludes layout remeasurement as the main button
lag source. Page navigation does remeasure 77 nodes, so layout may contribute
there, but the retained step is too broad to assign the full 2576.305 ms to
layout without finer phase timing.

### H4: Full-Tree Walks or Evidence Construction Are Expensive

Confidence: medium-high.

The retained step contains multiple broad operations after layout: reuse/build
walks, picture/replay walks, offscreen diagnostic walks, state and animation
collection, cached subtree emission, scene assembly, and render result
assembly. The work counters show only 53 repainted nodes for the button and 113
for the page change, but elapsed time is in seconds. That pattern suggests
either very expensive per-node work, repeated full-tree traversal, or diagnostic
construction that scales with the total retained scene rather than only the
dirty subset.

### H5: Replay Caching Is Not Helping These Frames

Confidence: medium.

Both scenarios report `replayHits=0` and `replayMisses=0`. If replay caching is
expected to accelerate these interactions, the cacheable boundaries may not
cover the changed subtrees or the replay mechanism may not be activated for this
content. This does not prove replay is broken, but it does show it is not
reducing the measured button or page-change frames.

### H6: GL Paint Is a Secondary Page-Change Cost

Confidence: high.

Page navigation has a 172.751 ms GL paint after the 2591.736 ms view phase.
That is too large for a smooth frame and should be investigated, but it is not
the main explanation for the multi-second lag. The button case has only
0.237 ms GL paint on the relevant post-update frame.

## Recommended Next Measurements

Add a second trace layer inside `RetainedRender.step` and keep it gated behind
`FS_GG_RENDER_LAG_TRACE=1`. The next split should report:

1. Diff duration.
2. Layout dirty-set construction duration.
3. Incremental layout duration.
4. Text cache hook duration.
5. Retained build/reuse duration.
6. Dirty rect union duration.
7. Picture cache and replay walk duration.
8. Offscreen diagnostic walk duration.
9. State and animation collection duration.
10. Scene assembly duration.
11. Render result assembly duration.
12. Any `Control.count`, `Scene.describe`, fingerprinting, hashing, or evidence
    construction durations.

The acceptance target for this next diagnostic should be simple: for the same
button and page-change scenarios, the phase split must account for at least
95 percent of the measured `RetainedRender.step` duration.

## Fix Candidates

Likely fixes depend on the next phase split, but the current evidence supports
these candidates:

1. Gate diagnostic/evidence scene walks so they do not run on every interactive
   frame unless explicitly requested.
2. Avoid repeated full-tree descriptions, counts, fingerprints, or hash walks
   during `RetainedRender.step`.
3. Narrow repaint/build work to changed retained identities and avoid rebuilding
   shifted but visually unchanged subtrees.
4. Add or verify cacheable boundaries around the shell content/page subtree so
   page navigation can reuse stable chrome and inactive regions.
5. Investigate why replay hit/miss counters are both zero in these scenarios.
6. After retained-step time is reduced, profile the page-change GL paint cost,
   especially scene size and Skia draw command volume on the first changed
   frame.

## Validation

Build and package validation performed before the probe data was captured:

```text
dotnet build src/SkiaViewer/SkiaViewer.fsproj -c Release --no-restore
dotnet build src/Controls.Elmish/Controls.Elmish.fsproj -c Release --no-restore
dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release --no-restore
dotnet pack src/SkiaViewer/SkiaViewer.fsproj -c Release -o /home/developer/.local/share/nuget-local --no-restore
dotnet pack src/Controls.Elmish/Controls.Elmish.fsproj -c Release -o /home/developer/.local/share/nuget-local --no-restore
dotnet restore samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj
```

The live probes ran against the rebuilt sample app and successfully reported
`firstFramePresented=true`.

## Caveats

- The button scenario uses keyboard `Enter` rather than pointer coordinates.
  This is intentional: the sample host maps `Enter` and `Space` to the same
  `PageMsg ButtonClicked` model update, and earlier representative pointer
  coordinates did not reliably hit a real button.
- The page-change scenario uses `F2` mapped in the diagnostic host wrapper
  rather than a nav-rail pointer click. It isolates the model update and
  rerender path after `NavigateTo "text-numeric-input"`.
- The trace writes to stderr. Logging overhead exists, but it is unlikely to
  explain second-scale retained-step timings because the same instrumentation
  reports sub-millisecond product view, runtime stamp, queue delay, and GL paint
  for adjacent phases.
- The raw trace files live under `/tmp`, so this report preserves the key data
  in repository documentation.

## Conclusion

The problem is now localized enough to act on: the live input-to-visible delay
is dominated by the retained render step. The next engineering task should not
be another broad input-lag investigation. It should be a phase-level profiler
inside `RetainedRender.step`, followed by removal or gating of the expensive
walk that accounts for the 1.25 s button frame and 2.58 s page-change frame.
