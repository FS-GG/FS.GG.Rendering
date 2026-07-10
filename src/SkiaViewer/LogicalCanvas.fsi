namespace FS.GG.UI.SkiaViewer

open FS.GG.UI.Scene

/// Issue #246: how a fixed logical canvas maps onto the actual output surface — a uniform
/// scale plus the centering offset that puts the unused surface into letterbox bars.
type LogicalCanvasFit =
    { Scale: float
      OffsetX: float
      OffsetY: float }

/// Issue #246: the letterbox seam for a fixed-logical-resolution product.
///
/// The game family withholds the window `Size` from `view` on purpose, so a product renders
/// in its own logical coordinate space and the host fits that space to whatever surface it
/// was given. `ViewerOptions.LogicalSize` turns that fit on; everything here is the pure
/// arithmetic behind it, so a product can assert the mapping with no window and no device.
[<RequireQualifiedAccess>]
module LogicalCanvas =

    /// The uniform (aspect-preserving) fit of `logical` centered inside `actual`.
    /// A non-positive extent on either size yields the identity fit rather than a division by zero.
    val fit: logical: Size -> actual: Size -> LogicalCanvasFit

    /// Wrap a scene authored in `logical` coordinates so it renders scaled and centered in `actual`.
    /// Content is always clipped to the logical canvas — a product cannot draw into the letterbox
    /// bars, and that does not become a fact about the current window size. Under the identity fit
    /// the scale is elided and the clip is a visual no-op, so an unscaled render stays
    /// pixel-identical to one taken without a `LogicalSize`.
    val present: logical: Size -> actual: Size -> node: SceneNode -> SceneNode

    /// Map a point in `actual` (window/surface) coordinates back into `logical` coordinates —
    /// the inverse of `present`, for routing pointer input to a letterboxed product. Points inside
    /// a letterbox bar map outside the logical canvas, which is faithful: nothing is drawn there.
    val toLogicalPoint: logical: Size -> actual: Size -> x: float -> y: float -> float * float
