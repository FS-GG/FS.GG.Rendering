// See skill: fs-gg-scene
namespace FS.GG.UI.Scene

/// One bounded semantic item indexed by its world-space bounds.
type SpatialEntry<'value> = { Id: string; Bounds: Rect; Value: 'value }

/// A viewport query plus semantic identities that must remain represented for focus/selection.
type SpatialQuery =
    { Viewport: Rect
      Overscan: float
      PinnedIds: Set<string>
      MaxVisitedChunks: int }

/// Immutable deterministic chunk index. Construction is available through <c>SpatialWorkingSet.create</c>.
type SpatialWorkingSetIndex<'value> = private SpatialWorkingSetIndex of float * SpatialEntry<'value> list * Map<int64 * int64, int list> * Map<string, int>

[<RequireQualifiedAccess>]
type SpatialWorkingSetIssue =
    | InvalidChunkSize of float
    | MissingId of index: int
    | DuplicateId of string
    | InvalidBounds of id: string
    | InvalidViewport
    | InvalidOverscan of float
    | EntryChunkLimitExceeded of id: string
    | QueryChunkLimitExceeded of limit: int

/// A deterministic visible working set and its measurable index work.
type SpatialWorkingSetResult<'value> =
    { Entries: SpatialEntry<'value> list
      VisitedChunkCount: int
      CandidateCount: int
      TotalEntryCount: int }

[<RequireQualifiedAccess>]
module SpatialWorkingSet =
    /// Build an immutable index in input order. Invalid/duplicate entries are refused as a group.
    val create: chunkSize: float -> entries: SpatialEntry<'value> list -> Result<SpatialWorkingSetIndex<'value>, SpatialWorkingSetIssue list>
    /// Query viewport plus overscan, retaining pinned items even when they are outside the viewport.
    val query: query: SpatialQuery -> index: SpatialWorkingSetIndex<'value> -> Result<SpatialWorkingSetResult<'value>, SpatialWorkingSetIssue list>
