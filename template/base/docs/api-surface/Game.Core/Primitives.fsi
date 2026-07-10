// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.2.0 (src/Game.Core/Primitives.fsi); regenerate when $(FsGgGameVersion) moves.
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

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A collision manifold — the minimum translation that separates two overlapping shapes. `Normal`
/// is a unit axis vector (one of (±1,0)/(0,±1)) pointing from the first shape toward the second
/// along the axis of least penetration; `Depth` is the positive penetration distance along it.
/// This is a *detection-only value* (the game-logic corpus's "detection returns manifests as
/// values" rule): resolution/response is a separate layer that consumes a `Contact`, never a thing
/// this core produces. Structural equality makes it a deterministic golden-testable value.
type Contact = { Normal: Point; Depth: float }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A circle in continuous simulation space — a centre `Point` and a `Radius`. The circular
/// counterpart of `Rect` for narrow-phase collision; consumed by `Geometry.circleContact` /
/// `circleAabbContact`. A non-positive or NaN radius is a no-contact input (the detection functions
/// return `None` rather than throwing).
type Circle = { Center: Point; Radius: float }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// The result of a segment-cast query — `T` is the parameter along the segment (`p0 + (p1 − p0)·T`,
/// in `[0,1]`) at the first forward surface crossing, `Point` is that hit position, and `Normal` is
/// the outward unit surface normal there. A *query value*, not a penetration manifold; produced by
/// `Geometry.segmentAabbHit` / `segmentCircleHit`. Structural equality makes it golden-testable.
type RayHit = { T: float; Point: Point; Normal: Point }

/// Public contract type exposed by the FS.GG.Game.Core package.
/// A convex polygon in continuous simulation space — a ring of `Vertices`. Convention (an input
/// assumption, not a runtime-enforced invariant): the vertices are convex and CCW-wound. It is the
/// arbitrary-convex counterpart of `Rect`/`Circle` for narrow-phase collision, consumed by
/// `Geometry.polygonContact`; build the oriented-bounding-box case with `Geometry.obbPolygon`. A
/// degenerate ring (fewer than 3 vertices, zero area, or a NaN coordinate) is a no-contact input —
/// `polygonContact` returns `None` rather than throwing. Structural equality makes it golden-testable.
type ConvexPolygon = { Vertices: Point[] }
