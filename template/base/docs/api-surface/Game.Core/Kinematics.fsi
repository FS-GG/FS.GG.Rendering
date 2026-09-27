// See skill: fs-gg-game-core
// Mirrored from FS-GG/FS.GG.Game @ 0.16.0 (src/Game.Core/Kinematics.fsi); regenerate when $(FsGgGameVersion) moves.
namespace FS.GG.Game.Core

/// A stationary shape in continuous simulation coordinates.
[<RequireQualifiedAccess>]
type KinematicShape =
    | AxisAlignedBox of Rect
    | Circle of Circle
    | Convex of ConvexPolygon

/// Product-owned response applied after detection.
[<RequireQualifiedAccess>]
type KinematicResponse =
    | Trigger
    | Slide
    | Bounce of restitution: float

/// A stable world collider. Shapes use absolute world coordinates.
type KinematicCollider =
    {
        Id: string
        Shape: KinematicShape
        Response: KinematicResponse
    }

/// One detected overlap or swept crossing.
type KinematicHit =
    {
        ColliderId: string
        Contact: Contact
        Sweep: RayHit option
        IsTrigger: bool
    }

/// A moving axis-aligned body and its displacement for one fixed step.
type KinematicMotion = { Bounds: Rect; Displacement: Point }

/// The bounded result of advancing one body through the stationary world.
type KinematicResult =
    {
        Bounds: Rect
        Displacement: Point
        Hits: KinematicHit list
        CandidateIds: string list
    }

/// Pure collision queries and arcade response over the shared geometry vocabulary.
[<RequireQualifiedAccess>]
module Kinematics =

    /// Return the conservative axis-aligned bounds used by the broad phase.
    val bounds: shape: KinematicShape -> Rect

    /// Detect the current overlap of a moving AABB with any supported stationary shape.
    val contact: moving: Rect -> shape: KinematicShape -> Contact option

    /// Cast a point segment against any supported stationary shape.
    val segmentHit: p0: Point -> p1: Point -> shape: KinematicShape -> RayHit option

    /// Return broad-phase candidates in collider insertion order for the whole swept region.
    val candidates:
        cellSize: float -> motion: KinematicMotion -> colliders: KinematicCollider list -> KinematicCollider list

    /// Advance once, collecting triggers and applying the earliest solid slide or bounce response.
    /// Ownership is bounded to one spatial query and one response; callers iterate fixed steps.
    val advance: cellSize: float -> motion: KinematicMotion -> colliders: KinematicCollider list -> KinematicResult
