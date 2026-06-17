# Compatibility Plan: Modifier Layer IR Foundation

## Scope

Feature 140 lands the P2 foundation as an internal Controls composition model plus a small public glyph-run proof surface. It does not expose public modifier, layer, portal, layout-container, portable serialization, compositor, or retained-renderer unification APIs.

## Public Surface Changes

| Package | Change | Compatibility impact | Evidence |
|---|---|---|---|
| `FS.GG.UI.Scene` | Added `GlyphRunGlyph`, `GlyphRunMetrics`, `GlyphRunData`, `GlyphRun`; added `SceneElementKind.GlyphRunElement`; added `SceneNode.GlyphRun`; added `Scene.buildGlyphRun`, `Scene.glyphRunFingerprint`, `Scene.measureGlyphRun`, `Scene.glyphRun`, and `Scene.glyphRunProof`. | Additive public proof surface. Existing text constructors remain unchanged and do not opt into glyph-run behavior automatically. Downstream exhaustive matches over `SceneNode` and `SceneElementKind` must handle the new cases. | `tests/Scene.Tests/Feature140GlyphRunTests.fs`; `tests/surface-baselines/FS.GG.UI.Scene.txt`; `src/Scene/Scene.fsi`. |
| `FS.GG.UI.SkiaViewer` | Added `Fonts.buildGlyphRunData` proof helper. | Additive helper that builds proof data using bundled-font fallback and renderer advances. It is not a full shaper and does not change default text drawing. The current type-name surface baseline does not list module functions, so this is recorded here. | `tests/SkiaViewer.Tests/Feature140GlyphRunRenderingTests.fs`; `src/SkiaViewer/Fonts.fsi`. |
| `FS.GG.UI.Controls` | Added internal `Composition` module. | No public package surface change. Modifier/layer/portal semantics stay assembly-internal. | `src/Controls/Composition.fsi`; `tests/Controls.Tests/Feature140*.fs`. |

## Legacy Forms

| Legacy form | Status | Migration note |
|---|---|---|
| Clipping | Supported unchanged | Internally lowers to `Composition.Clip`; existing constructors and behavior remain available. |
| Translation | Supported unchanged | Internally lowers to `Composition.Offset`; existing constructors and behavior remain available. |
| Perspective | Supported unchanged | Internally lowers to `Composition.Transform`; existing constructors and behavior remain available. |
| Cached subtree | Supported unchanged | Internally lowers to `Composition.CacheBoundary`; cache id is preserved. |
| Text and text-run forms | Supported unchanged | Existing text scenes remain text nodes. Glyph-run proof is explicit opt-in only. |
| Overlay container behavior | Supported unchanged | Existing overlay-like output records layer intent through the internal composition path while preserving visible text/content behavior. |

No legacy form is deprecated by this feature. No intentional pixel delta is recorded.

## Canonical Effect Order and Invalidation

Effects are value records in author order. Normalization removes identity effects and combines supported adjacent equivalent effects without changing diagnostics or fingerprints.

| Effect | Invalidation |
|---|---|
| Clip | Paint |
| Opacity | Paint |
| Offset | Layout and paint |
| Transform | Layout and paint |
| Background | Paint |
| Overlay | Paint and order |
| Cache boundary | Paint |
| Local z-order | Order |
| Layer hint | Order |

`RetainedRender.classifyModifierEffect` delegates to the same table as `Composition.classify`.

## Versioning Recommendation

This repository is still on preview packages. The additive glyph-run proof surface and new public DU cases should ship with a preview patch bump, currently from `0.1.2-preview.1` to `0.1.3-preview.1` during the merge packaging step. The migration note must call out exhaustive-match risk for `SceneNode.GlyphRun` and `SceneElementKind.GlyphRunElement`.

## Deferred Boundary

The following remain explicit non-goals: R1b retained-renderer unification, full HarfBuzz shaping, bidi, line breaking, expanded fallback, overlay interaction state, portable serialization, compositor promotion, and intrinsic layout.
