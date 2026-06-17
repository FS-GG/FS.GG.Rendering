# Evidence Map: Modifier Layer IR Foundation

| Contract clause | Evidence |
|---|---|
| Modifier ordering | `tests/Controls.Tests/Feature140ModifierLayerTests.fs`; focused command `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature140 --no-build`. |
| Normalization | `tests/Controls.Tests/Feature140ModifierNormalizationTests.fs`; covers idempotence, diagnostics, fingerprints, and representative chains. |
| Invalidation classification | `src/Controls/Composition.fsi`; `src/Controls/RetainedRender.fsi`; `tests/Controls.Tests/Feature140ModifierLayerTests.fs`. |
| Local z-order | `tests/Controls.Tests/Feature140ZOrderTests.fs`; `Composition.orderSiblings`, `paintOrder`, and `hitOrder`. |
| Portal/layer hosts | `tests/Controls.Tests/Feature140PortalLayerTests.fs`; `Composition.composeLayers`; readiness offscreen harness entry. |
| Legacy lowering | `tests/Controls.Tests/Feature140LegacyCompatibilityTests.fs`; `tests/Controls.Tests/Feature140LegacyCacheTextOverlayTests.fs`; legacy oracle commands in `readiness.md`. |
| Cache parity | `tests/Controls.Tests/Feature140ModifierNormalizationTests.fs`; `tests/Controls.Tests/Feature140LegacyCacheTextOverlayTests.fs`; existing cache audit commands in `readiness.md`. |
| Retained parity | `RetainedRender.classifyModifierEffect`; existing Feature091/Feature092 and audit commands in `readiness.md`. |
| Glyph-run proof | `tests/Scene.Tests/Feature140GlyphRunTests.fs`; `tests/SkiaViewer.Tests/Feature140GlyphRunRenderingTests.fs`; `src/Scene/Scene.fsi`; `src/SkiaViewer/Fonts.fsi`; `src/SkiaViewer/SceneRenderer.fs`. |
| Public surface | `tests/surface-baselines/FS.GG.UI.Scene.txt`; `compatibility-plan.md`; `readiness.md` surface refresh result. |
| Pixel changes | `contracts/rebaseline-ledger.md`; `artifacts/feature140-harness/T1/run.json`; no intentional pixel delta recorded. |

## Coverage Summary

All Feature 140 contract clauses have local test or evidence coverage. The only incomplete validation items are stale/missing repository wrapper gates recorded in `verification-limitations.md` and `readiness.md`.
