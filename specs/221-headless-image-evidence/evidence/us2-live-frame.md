# US2 live-frame capture proof (T017 / SC-003)

**Goal**: prove the documented live-window capture path produces a non-black image of the current frame.

## Result: ⚠️ environment-limited (GL required) — substitute recorded

US2's route — `PresentMode = ViewerPresentMode.OffscreenReadback` → `renderSceneToPixels`
(`src/SkiaViewer/Host/OpenGl.fs:790-826`) → `Viewer.captureScreenshotEvidence` — **requires a GL context /
virtual display** (it calls `SKSurface.Create(context: GRContext, …)`). This runner is a bare **no-GL**
container (no GPU/OpenGL/X/virtual display per `docs/harness/capability-baseline.md`), so a live capture is
**environment-limited** here. This is consistent with the spec Assumption that a virtual display *may*
exist for the live-window path but is **not** required for the deterministic offscreen path (US1).

## Disclosed substitute

1. **Documented path** — the end-to-end capture route is written in `docs/usage.md` ("Pixel proof of the
   live game window — GL/virtual-display required"), with every step explicit and the GL prerequisite
   stated; no step requires binary inspection or trial-and-error (SC-003's documentation criterion).
2. **Code-verified route** — `renderSceneToPixels` renders to an offscreen GL surface, flushes/submits,
   and `ReadPixels` back to CPU (returns `Ok pixels`), then `captureScreenshotEvidence` encodes the PNG.
   The path exists and is wired; only the GL host is absent here.
3. **Portable alternative proven** — the no-GL `SceneEvidence.renderPng` path (US1) produces a real,
   non-blank, decodable frame image headlessly (`representative-game-scene.png`), which is the supported
   way to get pixel proof when no GL/display exists.

## To complete on a GL/virtual-display host

```bash
# In an Xvfb + EGL session (DISPLAY set), run the viewer with OffscreenReadback and capture:
#   Viewer.captureScreenshotEvidence screenshotRequest { options with PresentMode = OffscreenReadback } scene
# Expected: a non-black PNG of the current frame written to the requested OutputPath.
```
