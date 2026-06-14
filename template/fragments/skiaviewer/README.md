# SkiaViewer Fragment

Adds viewer host package references and generated product viewer guidance.

Viewer-backed graphical profiles must use the persistent generated host as the
default executable path:

```fsharp
Viewer.runApp viewerOptions Product.Program.generatedHost
```

Bounded smoke, first-frame, frame-count, scene metadata, and unsupported-host
diagnostics are CI and reviewer-diagnostic helpers. They do not substitute for
supported-host persistent graphical launch readiness. Successful default launch
output must include `mode=interactive-window`, `window-visible=observed:true`,
and `accessible-window=true`; unsupported hosts must report diagnostics instead
of claiming accessibility.

Screenshot evidence is a separate command/report kind from deterministic scene
metadata and persistent launch evidence. Unsupported screenshot capture should
name `fallback=deterministic-scene-evidence` and must not claim screenshot
proof. A report with `evidence-kind=screenshot` proves a screenshot only when
it records live viewer-window capture after first-frame presentation;
deterministic-scene-evidence must not claim screenshot proof.

For Linux desktop review sessions where the viewer should keep running after
the terminal exits, preserve launch diagnostics with:

```bash
setsid dotnet run --project src/Product/Product.fsproj > readiness/logs/viewer-launch.log 2>&1 < /dev/null &
```

Keep the `readiness/logs/viewer-launch.log` path in the review notes so stdout,
stderr, and startup diagnostics remain inspectable.
