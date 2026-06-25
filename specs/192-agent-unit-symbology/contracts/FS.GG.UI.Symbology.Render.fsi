// CONTRACT SKETCH — Phase 1 (/speckit-plan). Authored .fsi-first per Constitution I/II.
// The thin IO/raster helper. Lives in its OWN project (src/Symbology.Render/) so the pure library
// stays Scene-only (FR-011). References FS.GG.UI.Symbology + FS.GG.UI.SkiaViewer. Pinned by
// readiness/surface-baselines/FS.GG.UI.Symbology.Render.txt.
//
// It backs onto the PUBLIC headless path FS.GG.UI.SkiaViewer.ReferenceRendering.run via a SceneCodec
// round-trip (D2/R2) — it does NOT reach any internal entry (SceneRenderer is internal) (FR-010).

namespace FS.GG.UI.Symbology.Render

open FS.GG.UI.Scene   // Scene, Size

/// Headless Scene → PNG bridge for the design loop.
[<RequireQualifiedAccess>]
module Render =

    /// Rasterise `scene` at `size` into `dir`, returning the written image PATH (FR-010/FR-013).
    ///
    /// Implementation contract (FR-012, Constitution VI — fail loud, never a blank success):
    ///   1. bytes  = (SceneCodec.export scene).CanonicalBytes
    ///   2. ev     = ReferenceRendering.run { PackageBytes = bytes; OutputDirectory = dir;
    ///                                        OutputSize = size; Resources = [] }
    ///   3. return p   WHEN  ev.Verdict = ReferencePassed  AND  ev.ImagePath = Some p
    ///   4. otherwise RAISE  with ev.Diagnostics joined — covers ReferenceFailed,
    ///      ReferenceEnvironmentLimited, AND ImagePath = None (the real verdict has THREE cases —
    ///      only ReferencePassed-with-a-path is success).
    /// Bonus: ReferenceRendering also writes a content-hash PNG + reference-evidence.md, giving each
    /// call a reusable regression identity (FR-013).
    val toPng: size: Size -> scene: Scene -> dir: string -> string
