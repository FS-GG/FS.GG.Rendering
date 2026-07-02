namespace FS.GG.UI.Symbology.Render

open FS.GG.UI.Scene

/// Headless Scene -> PNG bridge for the design loop.
[<RequireQualifiedAccess>]
module Render =

    /// Rasterise `scene` at `size` into `dir`, returning the written image PATH (FR-010/FR-013).
    ///
    /// Fail-loud contract (FR-012, Constitution VI — never a blank success):
    /// returns the path ONLY when the reference verdict is `ReferencePassed` AND an image path is
    /// present; otherwise raises with the joined render diagnostics (covers `ReferenceFailed`,
    /// `ReferenceEnvironmentLimited`, and a `None` image path).
    val toPng: size: Size -> scene: Scene -> dir: string -> string
