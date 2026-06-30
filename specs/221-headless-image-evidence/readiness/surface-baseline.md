# Public-surface baseline (T024) — Tier 1

This is a **Tier 1** change. The two `.fsi`/baseline obligations:

## 1. `.fsi` updated (the seam declaration) — ✅ done

- `src/Scene/Evidence.fsi` — `+ val setRealPngRasterizer: (Size -> Scene -> Result<byte[], SceneEvidenceFailure>) option -> unit`.
- `src/SkiaViewer/ReferenceRendering.fsi` — `+ val renderScenePngResult: Size -> Scene -> Result<byte[], SceneEvidenceFailure>`.
- `src/SkiaViewer/SkiaViewer.fsi` — `+ val installPngRasterizer / clearPngRasterizer` in `module Text`.

> **Design note (deviation from the plan's literal wording).** The plan/contract sketched the seam as
> `Scene.setRealPngRasterizer` in `Scene.fsi`. Two real ordering constraints moved it:
> (a) the seam signature references `SceneEvidenceFailure`, declared in `Evidence.fsi` which compiles
> **after** `Scene.fsi`, so the seam lives in the `SceneEvidence` module; (b) `Fonts.fs` compiles **before**
> `ReferenceRendering.fs`/`SceneRenderer.fs`, so the rasterizer is injected from `SkiaViewer.fs`'s `Text`
> module (compiled last), not from `Fonts.fs`. The seam still mirrors `setRealTextMeasurer` (process-wide,
> `option`-typed install/clear, `src/Scene` stays SkiaSharp-free) and is exercised in tests before `.fs`
> behaviour was relied on.

## 2. Surface-area baseline — ✅ no new type; no baseline edit required

`readiness/surface-baselines/*.txt` is a list of **exported type names** (`assertBaseline` /
`exportedNames` in `tests/Package.Tests/SurfaceAreaTests.fs` use `GetExportedTypes()`). This change added
**functions to existing modules** (`SceneEvidence`, `ReferenceRendering`, `Text`) and **reused** the
existing failure model (`SceneEvidenceFailure` / `SceneEvidenceFailureClassification` / `EvidenceStage`) —
so it exports **no new type**.

Verified by reflecting the freshly-built assemblies against the committed baselines:

- `FS.GG.UI.Scene`: `missing = []`, `unexpected (new types) = []` — **clean**.
- `FS.GG.UI.SkiaViewer`: no new named types (the only "unexpected" entries are pre-existing
  compiler-generated `<>f__AnonymousType…` records, unrelated to this change — no anonymous records were
  added).

`dotnet test tests/Package.Tests --filter Surface` → **34 passed / 1 failed**. The 1 RED is the documented
pre-existing `FS.GG.UI.Build engine baseline exports expected contract names` failure (the Build engine
assembly is not built in the Debug test lane — red at baseline and after; not a regression).

## Conclusion

`.fsi` obligation satisfied; type-name surface baselines are correct as-is (no drift introduced).
