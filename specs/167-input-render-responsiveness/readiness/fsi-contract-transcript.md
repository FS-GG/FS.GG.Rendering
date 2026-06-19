# Feature 167 FSI Contract Transcript

Status: passed on 2026-06-19.

The additive public contracts are declared in:

- `src/SkiaViewer/SkiaViewer.fsi`
- `src/SkiaViewer/Host/OpenGl.fsi`
- `src/Controls.Elmish/ControlsElmish.fsi`
- `tests/Rendering.Harness/ValidationLanes.fsi`

Command:

```sh
printf '%s\n' \
  '#I "src/SkiaViewer/bin/Release/net10.0"' \
  '#I "src/Controls.Elmish/bin/Release/net10.0"' \
  '#r "FS.GG.UI.SkiaViewer.dll"' \
  '#r "FS.GG.UI.Controls.Elmish.dll"' \
  'open FS.GG.UI.SkiaViewer' \
  'open FS.GG.UI.Controls.Elmish' \
  'printfn "kind=%s" (Viewer.responsivenessInputKindToken ViewerResponsivenessInputKind.PointerDiscrete)' \
  'printfn "readiness=%s" (Viewer.responsivenessReadinessToken ViewerResponsivenessReadiness.EnvironmentLimited)' \
  'printfn "queueDepth=%d" (Viewer.inputQueueDepth Viewer.emptyInputQueue)' \
  'printfn "receiptBudgetMs=%.1f" Viewer.defaultResponsivenessBudget.InputReceiptP95.TotalMilliseconds' \
  'let disabled = ControlsElmish.diagnosticsDisabledCompatibility [] []' \
  'printfn "disabled=%b/%d/%b" disabled.FrameMetricsUnchanged disabled.RecordsWritten disabled.ClockFreePerfScript' \
  '#quit;;' | dotnet fsi
```

Observed output:

```fsharp
kind=pointer-discrete
readiness=environment-limited
queueDepth=0
receiptBudgetMs=4.0
disabled=true/0/true
```
