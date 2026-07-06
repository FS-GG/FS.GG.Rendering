namespace FS.GG.Game.Core

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A 2-D point in continuous simulation space. BCL-only (no dependency on FS.GG.UI.Scene): the
/// sim core reaches up to nothing, so it carries its own geometry vocabulary rather than the
/// render `FS.GG.UI.Scene.Point`. `FS.GG.Game.Render` projects this onto a Scene point at the
/// render edge. Structural equality gives a stable identity for deterministic bookkeeping.
type Point = { X: float; Y: float }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// An axis-aligned rectangle in continuous simulation space (min corner `X`/`Y` plus `Width`/
/// `Height`). BCL-only, the sim counterpart of the render `FS.GG.UI.Scene.Rect`. Consumed by
/// `Geometry` (collision/containment) and `SpatialGrid` (range queries).
type Rect =
    { X: float
      Y: float
      Width: float
      Height: float }
