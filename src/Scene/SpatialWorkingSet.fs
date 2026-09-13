namespace FS.GG.UI.Scene

open System

type SpatialEntry<'value> = { Id: string; Bounds: Rect; Value: 'value }
type SpatialQuery =
    { Viewport: Rect
      Overscan: float
      PinnedIds: Set<string>
      MaxVisitedChunks: int }
type SpatialWorkingSetResult<'value> =
    { Entries: SpatialEntry<'value> list
      VisitedChunkCount: int
      CandidateCount: int
      TotalEntryCount: int }

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

type SpatialWorkingSetIndex<'value> =
    private SpatialWorkingSetIndex of float * SpatialEntry<'value> list * Map<int64 * int64, int list> * Map<string, int>

[<RequireQualifiedAccess>]
module SpatialWorkingSet =
    let private finite value = not (Double.IsNaN value || Double.IsInfinity value)
    let private validRect value =
        finite value.X && finite value.Y && finite value.Width && finite value.Height
        && value.Width >= 0.0 && value.Height >= 0.0
        && finite (value.X + value.Width) && finite (value.Y + value.Height)

    let private validChunkAddress size bounds =
        let limit = float Int64.MaxValue
        [ bounds.X / size; bounds.Y / size; (bounds.X + bounds.Width) / size; (bounds.Y + bounds.Height) / size ]
        |> List.forall (fun value -> finite value && abs value <= limit)

    let private chunkRange size start length =
        let first = int64 (floor (start / size))
        let last = int64 (floor ((start + length) / size))
        [ first .. last ]

    let private chunkCount size bounds =
        let axis start length = floor ((start + length) / size) - floor (start / size) + 1.0
        axis bounds.X bounds.Width * axis bounds.Y bounds.Height

    let private maxEntryChunks = 4096.0

    let create chunkSize entries =
        let duplicates =
            entries
            |> List.groupBy _.Id
            |> List.choose (fun (id, values) -> if values.Length > 1 && not (String.IsNullOrWhiteSpace id) then Some id else None)
            |> List.sort
        let issues =
            [ if not (finite chunkSize) || chunkSize <= 0.0 then SpatialWorkingSetIssue.InvalidChunkSize chunkSize
              for index, entry in List.indexed entries do
                  if String.IsNullOrWhiteSpace entry.Id then SpatialWorkingSetIssue.MissingId index
                  if not (validRect entry.Bounds)
                     || (finite chunkSize && chunkSize > 0.0 && not (validChunkAddress chunkSize entry.Bounds)) then
                      SpatialWorkingSetIssue.InvalidBounds entry.Id
                  elif finite chunkSize && chunkSize > 0.0 && chunkCount chunkSize entry.Bounds > maxEntryChunks then
                      SpatialWorkingSetIssue.EntryChunkLimitExceeded entry.Id
              for id in duplicates do SpatialWorkingSetIssue.DuplicateId id ]
        if not issues.IsEmpty then Error issues
        else
            let buckets =
                entries
                |> List.indexed
                |> List.fold (fun state (index, entry) ->
                    seq {
                        for x in chunkRange chunkSize entry.Bounds.X entry.Bounds.Width do
                            for y in chunkRange chunkSize entry.Bounds.Y entry.Bounds.Height do yield x, y
                    }
                    |> Seq.fold (fun current key ->
                        let values = Map.tryFind key current |> Option.defaultValue []
                        Map.add key (values @ [ index ]) current) state) Map.empty
            let identities = entries |> List.indexed |> List.map (fun (index, entry) -> entry.Id, index) |> Map.ofList
            Ok(SpatialWorkingSetIndex(chunkSize, entries, buckets, identities))

    let query query (SpatialWorkingSetIndex(chunkSize, entries, buckets, identities)) =
        let issues =
            [ if not (validRect query.Viewport) then SpatialWorkingSetIssue.InvalidViewport
              if not (finite query.Overscan) || query.Overscan < 0.0 then SpatialWorkingSetIssue.InvalidOverscan query.Overscan ]
        if not issues.IsEmpty then Error issues
        else
            let expanded =
                { X = query.Viewport.X - query.Overscan
                  Y = query.Viewport.Y - query.Overscan
                  Width = query.Viewport.Width + query.Overscan * 2.0
                  Height = query.Viewport.Height + query.Overscan * 2.0 }
            if not (validRect expanded) || not (validChunkAddress chunkSize expanded) then
                Error [ SpatialWorkingSetIssue.InvalidViewport ]
            elif query.MaxVisitedChunks <= 0 || chunkCount chunkSize expanded > float query.MaxVisitedChunks then
                Error [ SpatialWorkingSetIssue.QueryChunkLimitExceeded query.MaxVisitedChunks ]
            else
                let chunks =
                    [ for x in chunkRange chunkSize expanded.X expanded.Width do
                          for y in chunkRange chunkSize expanded.Y expanded.Height do yield x, y ]
                let candidateIndices =
                    chunks |> List.collect (fun key -> Map.tryFind key buckets |> Option.defaultValue []) |> Set.ofList
                let pinnedIndices = query.PinnedIds |> Seq.choose (fun id -> Map.tryFind id identities) |> Set.ofSeq
                let intersects left right =
                    left.X <= right.X + right.Width && right.X <= left.X + left.Width
                    && left.Y <= right.Y + right.Height && right.Y <= left.Y + left.Height
                let selected =
                    Set.union candidateIndices pinnedIndices
                    |> Set.toList
                    |> List.sort
                    |> List.choose (fun index ->
                        let entry = entries[index]
                        if Set.contains index pinnedIndices || intersects entry.Bounds expanded then Some entry else None)
                Ok
                    { Entries = selected
                      VisitedChunkCount = chunks.Length
                      CandidateCount = candidateIndices.Count
                      TotalEntryCount = entries.Length }
