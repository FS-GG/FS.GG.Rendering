# M0 render-bridge spike evidence (T007)

**Date**: 2026-06-25 | **Feature**: `192-agent-unit-symbology` | **Gate**: the live-smoke analogue
that must confirm the public Scene→PNG render path works in THIS checkout before US2 render code is trusted.

## What was driven

A one-token gallery `Scene` (`Symbology.gallery 1 90.0 [ … ]`) was exported to canonical bytes
(`(SceneCodec.export scene).CanonicalBytes`) and rendered through the **public** oracle
`FS.GG.UI.SkiaViewer.ReferenceRendering.run` — no internal `SceneRenderer` entry.

## Result — PASS (authoritative, project test host)

The render bridge is exercised end-to-end by `tests/Symbology.Render.Tests` (the live render smoke).
The pass-path test produced a **non-blank PNG** with verdict **`ReferencePassed`**:

```
- verdict: passed
- output-size: 360x180
- renderer-identity: FS.GG.UI.SkiaViewer.SceneRenderer/skia-reference
- image-path: …/sha256-9aa1045e6979ed6dbd8a1f2f9f83a817b33704f12f7720d5ee8246d33a814d4d.png  (8056 bytes)
- image-identity: sha256:4848b0816c5452a965a612d3bdef31e954f18eeecd7db4df4c9f8d60c6010a9b
- diagnostics: none
```

Captured artifacts (committed under this readiness dir):
`m0-spike/sha256-….png` (the non-blank board) and `m0-spike/reference-evidence.md` (the oracle's own
evidence summary). `dotnet test tests/Symbology.Render.Tests` → **3/3 passed**.

## FSI-environment caveat (not a bridge defect)

A throwaway `dotnet fsi` spike that `#r`'d the built DLLs returned `ReferenceFailed` with the single
diagnostic *"Could not load file or assembly 'SkiaSharp …'"*. This is an **FSI packaging limitation**
(FSI does not honour the library's `deps.json`, so the native SkiaSharp managed assembly is not on its
probe path) — **not** a render-bridge failure. Two things are notable and good:

1. The same scene renders `ReferencePassed` with a non-blank PNG in the proper project test host
   (`SkiaViewer.Tests` is likewise green in the no-regression baseline), so the bridge works in this
   checkout.
2. Under the FSI limitation the bridge **failed loud** — it surfaced `ReferenceFailed` + a diagnostic
   rather than emitting a blank image as success — which is exactly the Constitution VI / FR-012
   contract `Render.toPng` enforces.

**Gate decision: render bridge confirmed live (ReferencePassed, non-blank PNG). US2 render work cleared.**
