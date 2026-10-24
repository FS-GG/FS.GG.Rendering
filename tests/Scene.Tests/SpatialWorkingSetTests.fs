module FS.GG.UI.Scene.Tests.SpatialWorkingSetTests

open System
open System.Diagnostics
open System.Reflection
open Expecto
open FS.GG.UI.Scene

let private entry id x y =
    {
        Id = id
        Bounds =
            {
                X = x
                Y = y
                Width = 8.0
                Height = 8.0
            }
        Value = id
    }

let private create values =
    SpatialWorkingSet.create 64.0 values
    |> Result.defaultWith (fun issues -> failtestf "%A" issues)


let private box id x y width height =
    {
        Id = id
        Bounds =
            {
                X = x
                Y = y
                Width = width
                Height = height
            }
        Value = id
    }

let private rect x y width height =
    {
        X = x
        Y = y
        Width = width
        Height = height
    }

let private query viewport overscan pinned limit =
    {
        Viewport = viewport
        Overscan = overscan
        PinnedIds = Set.ofList pinned
        MaxVisitedChunks = limit
    }

/// An independent, deliberately naive linear semantic reference. It walks every source entry in
/// order for every query and shares no code or data structure with the index under test.
module private LinearReference =
    let private span size start length =
        floor (start / size), floor ((start + length) / size)

    let query size (entries: SpatialEntry<string> list) (q: SpatialQuery) =
        let expanded =
            rect
                (q.Viewport.X - q.Overscan)
                (q.Viewport.Y - q.Overscan)
                (q.Viewport.Width + q.Overscan * 2.0)
                (q.Viewport.Height + q.Overscan * 2.0)

        let qx0, qx1 = span size expanded.X expanded.Width
        let qy0, qy1 = span size expanded.Y expanded.Height

        let inVisitedChunks (entry: SpatialEntry<string>) =
            let ex0, ex1 = span size entry.Bounds.X entry.Bounds.Width
            let ey0, ey1 = span size entry.Bounds.Y entry.Bounds.Height
            ex0 <= qx1 && qx0 <= ex1 && ey0 <= qy1 && qy0 <= ey1

        let touches (entry: SpatialEntry<string>) =
            let b = entry.Bounds

            b.X <= expanded.X + expanded.Width
            && expanded.X <= b.X + b.Width
            && b.Y <= expanded.Y + expanded.Height
            && expanded.Y <= b.Y + b.Height

        let selected =
            entries
            |> List.filter (fun entry -> Set.contains entry.Id q.PinnedIds || (inVisitedChunks entry && touches entry))

        selected, int ((qx1 - qx0 + 1.0) * (qy1 - qy0 + 1.0)), entries |> List.filter inVisitedChunks |> List.length

/// Far-away filler cells never intersect the viewports used below and each occupy their own chunk.
let private filler ordinal =
    box $"filler-{ordinal}" (1.0e6 + float ordinal * 100.0) 1.0e6 8.0 8.0

/// Place named entries at exact source ordinals inside a population of the given total size.
let private populate total (placements: (int * SpatialEntry<string>) list) =
    let slots = Array.init total filler

    for ordinal, placed in placements do
        slots[ordinal] <- placed

    List.ofArray slots

let private queryOk q index =
    SpatialWorkingSet.query q index
    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

/// Join the real index to the linear reference: entries (by reference), order and every counter.
let private expectReference size (entries: SpatialEntry<string> list) q message =
    let index =
        SpatialWorkingSet.create size entries
        |> Result.defaultWith (fun issues -> failtestf "%A" issues)

    let actual = queryOk q index
    let selected, visited, candidates = LinearReference.query size entries q

    Expect.equal
        (actual.Entries |> List.map _.Id)
        (selected |> List.map _.Id)
        $"{message}: selected identities and order"

    Expect.isTrue
        (List.forall2 (fun (a: SpatialEntry<string>) b -> obj.ReferenceEquals(a, b)) actual.Entries selected)
        $"{message}: entries are the caller's own records"

    Expect.equal actual.VisitedChunkCount visited $"{message}: visited chunk counter"
    Expect.equal actual.CandidateCount candidates $"{message}: candidate counter"
    Expect.equal actual.TotalEntryCount entries.Length $"{message}: total entry counter"
    index, actual

let private visibleBlock =
    [
        box "single" 10.0 10.0 8.0 8.0
        // spans several 64-unit chunks in both axes, so it is reachable from many buckets
        box "wide" -40.0 20.0 300.0 150.0
        // touches the expanded viewport edge exactly (inclusive intersection)
        box "edge-right" 200.0 40.0 8.0 8.0
        box "edge-left" -8.0 40.0 8.0 8.0
        // just beyond the edge: shares the viewport's chunks but must not be selected
        box "just-outside" 200.000001 40.0 8.0 8.0
        box "just-outside-left" -8.000001 60.0 8.0 8.0
        // the same inclusive edges on the vertical axis
        box "edge-top" 40.0 200.0 8.0 8.0
        box "edge-bottom" 60.0 -8.0 8.0 8.0
        box "just-outside-top" 80.0 200.000001 8.0 8.0
        box "just-outside-bottom" 100.0 -8.000001 8.0 8.0
        box "tall" 120.0 -30.0 6.0 400.0
    ]

let private viewport = rect 0.0 0.0 200.0 200.0

[<Tests>]
let tests =
    testList
        "SVG spatial working set"
        [
            testCase "10x world extent keeps the same visible working set and query work"
            <| fun _ ->
                let visible =
                    [
                        for index in 0..99 ->
                            entry $"visible-{index}" (float (index % 10) * 10.0) (float (index / 10) * 10.0)
                    ]

                let near = create visible

                let extended =
                    create (
                        visible
                        @ [
                            for index in 0..899 -> entry $"far-{index}" (10000.0 + float index * 10.0) 10000.0
                        ]
                    )

                let query =
                    {
                        Viewport =
                            {
                                X = 0.0
                                Y = 0.0
                                Width = 100.0
                                Height = 100.0
                            }
                        Overscan = 0.0
                        PinnedIds = Set.empty
                        MaxVisitedChunks = 16
                    }

                let first =
                    SpatialWorkingSet.query query near
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                let second =
                    SpatialWorkingSet.query query extended
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                Expect.equal
                    (second.Entries |> List.map _.Id)
                    (first.Entries |> List.map _.Id)
                    "visible identities are unchanged"

                Expect.equal
                    second.VisitedChunkCount
                    first.VisitedChunkCount
                    "index work depends on viewport chunks, not world extent"

                Expect.equal second.CandidateCount first.CandidateCount "far chunks do not enter the candidate set"

            testCase "offscreen focused and selected identities remain represented"
            <| fun _ ->
                let index =
                    create
                        [
                            entry "visible" 10.0 10.0
                            entry "focused" 5000.0 5000.0
                            entry "selected" -5000.0 -5000.0
                        ]

                let result =
                    SpatialWorkingSet.query
                        {
                            Viewport =
                                {
                                    X = 0.0
                                    Y = 0.0
                                    Width = 100.0
                                    Height = 100.0
                                }
                            Overscan = 16.0
                            PinnedIds = Set.ofList [ "focused"; "selected" ]
                            MaxVisitedChunks = 16
                        }
                        index
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                Expect.equal
                    (result.Entries |> List.map _.Id)
                    [ "visible"; "focused"; "selected" ]
                    "input order remains the stable semantic order"

            testCase "invalid geometry and duplicate identities are refused deterministically"
            <| fun _ ->
                let bad =
                    [
                        entry "same" 0.0 0.0
                        entry "same" 1.0 1.0
                        { entry "broken" 0.0 0.0 with
                            Bounds =
                                {
                                    X = nan
                                    Y = 0.0
                                    Width = 1.0
                                    Height = 1.0
                                }
                        }
                        entry "" 2.0 2.0
                    ]

                Expect.equal
                    (SpatialWorkingSet.create 64.0 bad)
                    (Error
                        [
                            SpatialWorkingSetIssue.InvalidBounds "broken"
                            SpatialWorkingSetIssue.MissingId 3
                            SpatialWorkingSetIssue.DuplicateId "same"
                        ])
                    "issues retain field order then duplicate order"

            testCase "query work above the caller limit is refused before candidate traversal"
            <| fun _ ->
                let index = create [ entry "visible" 0.0 0.0 ]

                let query =
                    {
                        Viewport =
                            {
                                X = 0.0
                                Y = 0.0
                                Width = 200.0
                                Height = 200.0
                            }
                        Overscan = 0.0
                        PinnedIds = Set.empty
                        MaxVisitedChunks = 4
                    }

                Expect.equal
                    (SpatialWorkingSet.query query index)
                    (Error [ SpatialWorkingSetIssue.QueryChunkLimitExceeded 4 ])
                    "viewport queries require an explicit bounded chunk budget"

            testCase
                "identical visible population gives the reference result at the beginning and the end of a larger source list"
            <| fun _ ->
                let total = 5000
                let q = query viewport 0.0 [] 64
                let count = visibleBlock.Length

                let atStart = populate total (visibleBlock |> List.mapi (fun i e -> i, e))

                let atEnd =
                    populate total (visibleBlock |> List.mapi (fun i e -> total - count + i, e))

                let _, first = expectReference 64.0 atStart q "visible block first"
                let _, last = expectReference 64.0 atEnd q "visible block last"

                Expect.equal
                    (last.Entries |> List.map _.Id)
                    (first.Entries |> List.map _.Id)
                    "the same visible identities are selected wherever the block sits in the source list"

                Expect.equal
                    (last.VisitedChunkCount, last.CandidateCount, last.TotalEntryCount)
                    (first.VisitedChunkCount, first.CandidateCount, first.TotalEntryCount)
                    "counters do not depend on source position"

                Expect.equal
                    (first.Entries |> List.map _.Id)
                    [
                        "single"
                        "wide"
                        "edge-right"
                        "edge-left"
                        "edge-top"
                        "edge-bottom"
                        "tall"
                    ]
                    "inclusive boundary hits are selected and just-outside entries are not"

            testCase "sparse late ordinals are selected in source order"
            <| fun _ ->
                let total = 8000

                let placements =
                    [
                        0, box "first" 5.0 5.0 8.0 8.0
                        1999, box "mid" 70.0 70.0 8.0 8.0
                        4096, box "four-k" 130.0 20.0 8.0 8.0
                        7998, box "late-b" 20.0 130.0 8.0 8.0
                        7999, box "last" 100.0 100.0 8.0 8.0
                    ]

                let entries = populate total placements

                let _, result =
                    expectReference 64.0 entries (query viewport 0.0 [] 64) "sparse ordinals"

                Expect.equal
                    (result.Entries |> List.map _.Id)
                    [ "first"; "mid"; "four-k"; "late-b"; "last" ]
                    "ordinals 0 and 7999 and everything between keep source order"

                Expect.equal result.TotalEntryCount total "the total reports the whole source population"

            testCase "an entry overlapping many chunks is counted and returned once"
            <| fun _ ->
                let entries =
                    populate 3000 [ 2900, box "wide" -40.0 20.0 300.0 150.0; 2950, box "after" 30.0 30.0 8.0 8.0 ]

                let _, result =
                    expectReference 64.0 entries (query viewport 0.0 [] 64) "multi-chunk overlap"

                Expect.equal result.Entries.Length 2 "deduplicated across chunks"
                Expect.equal result.CandidateCount 2 "one candidate per distinct entry"
                Expect.equal result.VisitedChunkCount 16 "4 x 4 viewport chunks"

            testCase "offscreen and missing pinned identities follow the reference at late ordinals"
            <| fun _ ->
                let entries =
                    populate
                        6000
                        [
                            10, box "visible-early" 10.0 10.0 8.0 8.0
                            5990, box "pinned-late-offscreen" 5000.0 5000.0 8.0 8.0
                            5995, box "visible-late" 40.0 40.0 8.0 8.0
                            5999, box "pinned-and-visible" 50.0 50.0 8.0 8.0
                        ]

                let q =
                    query viewport 16.0 [ "pinned-late-offscreen"; "pinned-and-visible"; "never-existed" ] 64

                let _, result = expectReference 64.0 entries q "pins"

                Expect.equal
                    (result.Entries |> List.map _.Id)
                    [
                        "visible-early"
                        "pinned-late-offscreen"
                        "visible-late"
                        "pinned-and-visible"
                    ]
                    "pinned entries keep source order, a pinned visible entry appears once and a missing pin is ignored"

            testCase "repeated queries do not change the index or each other"
            <| fun _ ->
                let entries =
                    populate 4000 (visibleBlock |> List.mapi (fun i e -> 3000 + i * 10, e))

                let index =
                    SpatialWorkingSet.create 64.0 entries
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                let near = query viewport 0.0 [ "filler-0" ] 64
                let elsewhere = query (rect 1.0e6 1.0e6 450.0 20.0) 8.0 [ "tall" ] 64

                let nearFirst = queryOk near index
                let elsewhereFirst = queryOk elsewhere index
                let nearSecond = queryOk near index
                let elsewhereSecond = queryOk elsewhere index

                Expect.equal nearSecond nearFirst "the same query on the same index is repeatable"
                Expect.equal elsewhereSecond elsewhereFirst "interleaved queries do not disturb each other"
                Expect.isGreaterThan elsewhereFirst.Entries.Length 1 "the far query is a different non-trivial result"

                for q in [ near; elsewhere ] do
                    let selected, visited, candidates = LinearReference.query 64.0 entries q
                    let actual = queryOk q index
                    Expect.equal (actual.Entries |> List.map _.Id) (selected |> List.map _.Id) "reference identities"

                    Expect.equal
                        (actual.VisitedChunkCount, actual.CandidateCount)
                        (visited, candidates)
                        "reference counters"

            testCase "result entries and values are the caller's own records"
            <| fun _ ->
                let payload = obj ()

                let inputs =
                    [
                        {
                            Id = "a"
                            Bounds = rect 0.0 0.0 8.0 8.0
                            Value = payload
                        }
                        {
                            Id = "b"
                            Bounds = rect 10.0 0.0 8.0 8.0
                            Value = payload
                        }
                    ]

                let index =
                    SpatialWorkingSet.create 64.0 inputs
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                let result = queryOk (query viewport 0.0 [] 64) index

                Expect.equal result.Entries.Length 2 "both entries are selected"

                List.iter2
                    (fun (expected: SpatialEntry<obj>) (actual: SpatialEntry<obj>) ->
                        Expect.isTrue (obj.ReferenceEquals(expected, actual)) "the same entry record"
                        Expect.isTrue (obj.ReferenceEquals(expected.Value, actual.Value)) "the same value")
                    inputs
                    result.Entries

            testCase "refusal order is unchanged for a large invalid population"
            <| fun _ ->
                let entries =
                    populate
                        2000
                        [
                            1500, box "" 0.0 0.0 8.0 8.0
                            1700, box "dup" 1.0 1.0 8.0 8.0
                            1800, box "dup" 2.0 2.0 8.0 8.0
                        ]

                Expect.equal
                    (SpatialWorkingSet.create 0.0 entries)
                    (Error
                        [
                            SpatialWorkingSetIssue.InvalidChunkSize 0.0
                            SpatialWorkingSetIssue.MissingId 1500
                            SpatialWorkingSetIssue.DuplicateId "dup"
                        ])
                    "chunk size, then missing identity by ordinal, then duplicate identity"

                Expect.equal
                    (SpatialWorkingSet.create 64.0 [ box "huge" 0.0 0.0 1.0e6 1.0e6 ]
                     |> Result.map ignore)
                    (Error [ SpatialWorkingSetIssue.EntryChunkLimitExceeded "huge" ])
                    "per-entry chunk limit is still refused"

            testCase "invalid and over-limit queries are refused before any entry is selected, for any population size"
            <| fun _ ->
                let index =
                    SpatialWorkingSet.create 64.0 (populate 3000 [ 2999, box "late" 1.0 1.0 8.0 8.0 ])
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                Expect.equal
                    (SpatialWorkingSet.query (query (rect nan 0.0 1.0 1.0) -1.0 [ "late" ] 4) index)
                    (Error
                        [
                            SpatialWorkingSetIssue.InvalidViewport
                            SpatialWorkingSetIssue.InvalidOverscan -1.0
                        ])
                    "viewport then overscan"

                Expect.equal
                    (SpatialWorkingSet.query (query viewport 0.0 [ "late" ] 0) index)
                    (Error [ SpatialWorkingSetIssue.QueryChunkLimitExceeded 0 ])
                    "a non-positive chunk limit is refused"

            testCase "the index retains its entries in random-access storage"
            <| fun _ ->
                let index =
                    SpatialWorkingSet.create 64.0 (populate 100 [])
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                let storage =
                    typeof<SpatialWorkingSetIndex<string>>
                        .GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)
                    |> Array.filter (fun field -> field.FieldType = typeof<SpatialEntry<string>[]>)

                Expect.equal storage.Length 1 "exactly one private array of entries is retained"

                let retained =
                    Option.ofObj (storage[0].GetValue index) |> Option.get :?> SpatialEntry<string>[]

                Expect.equal retained.Length 100 "the array holds every validated entry"

                Expect.equal
                    (retained |> Array.map _.Id |> Array.toList)
                    (List.init 100 (fun i -> $"filler-{i}"))
                    "in source order"

                Expect.isFalse storage[0].IsPublic "the storage is not exposed"

            testCase "lookup cost does not depend on source position or source length"
            <| fun _ ->
                // Control for position sensitivity. Selecting by ordinal from a list and reporting its length
                // are both linear, so with a list-backed index these queries cost time proportional to the
                // source length (and, for late selections, to the ordinal as well). The index is built
                // once per population and queried repeatedly; the minimum per-query time over many rounds
                // absorbs scheduler and GC noise, and the bound is deliberately loose (a list-backed index
                // exceeds it by one to three orders of magnitude at this size).
                let large = 300000
                let small = 1000
                let hundred = 100

                let visibleAt k =
                    box $"visible-{k}" (float (k % 10) * 36.0) (float (k / 10) * 36.0) 8.0 8.0

                let first count total =
                    populate total [ for k in 0 .. count - 1 -> k, visibleAt k ]

                let last count total =
                    populate total [ for k in 0 .. count - 1 -> total - count + k, visibleAt k ]

                let build entries =
                    SpatialWorkingSet.create 64.0 entries
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                let largeFirst = build (first hundred large)
                let largeLast = build (last hundred large)
                let smallFirst = build (first hundred small)

                let single ordinal total = populate total [ ordinal, visibleAt 0 ]

                let largeSingleLast = build (single (large - 1) large)
                let smallSingleFirst = build (single 0 small)

                let pinnedLast =
                    build (populate large [ large - 1, box "pinned" 5000.0 5000.0 8.0 8.0 ])

                let box400 = rect 0.0 0.0 400.0 400.0
                let visibleQuery = query box400 0.0 [] 64
                let pinnedQuery = query (rect 0.0 0.0 1.0 1.0) 0.0 [ "pinned" ] 64

                let perQueryMicroseconds q index =
                    let iterations = 4

                    let one () =
                        let start = Stopwatch.GetTimestamp()

                        for _ in 1..iterations do
                            queryOk q index |> ignore

                        Stopwatch.GetElapsedTime(start).TotalMilliseconds * 1000.0 / float iterations

                    one () |> ignore
                    [ for _ in 1..40 -> one () ] |> List.min

                for index, expected in
                    [
                        largeFirst, hundred
                        largeLast, hundred
                        smallFirst, hundred
                        largeSingleLast, 1
                        smallSingleFirst, 1
                    ] do
                    Expect.equal (queryOk visibleQuery index).Entries.Length expected "visible selection size"

                Expect.equal (queryOk pinnedQuery pinnedLast).Entries.Length 1 "late pin selection size"

                let bound reference =
                    max (reference * 6.0) (reference + 50.0)

                let compare label late reference =
                    let lateCost = perQueryMicroseconds (fst late) (snd late)
                    let referenceCost = perQueryMicroseconds (fst reference) (snd reference)

                    Expect.isLessThan
                        lateCost
                        (bound referenceCost)
                        $"{label}: {lateCost:F1}us per query against a reference of {referenceCost:F1}us"

                compare
                    "100 visible at the last ordinals vs the first ordinals, 300000 entries"
                    (visibleQuery, largeLast)
                    (visibleQuery, largeFirst)

                compare
                    "100 visible at the first ordinals, 300000 vs 1000 entries"
                    (visibleQuery, largeFirst)
                    (visibleQuery, smallFirst)

                compare
                    "one visible entry at the last of 300000 ordinals vs the first of 1000"
                    (visibleQuery, largeSingleLast)
                    (visibleQuery, smallSingleFirst)

                compare
                    "one late pinned entry in 300000 vs one early visible entry in 1000"
                    (pinnedQuery, pinnedLast)
                    (visibleQuery, smallSingleFirst)

            testCase "a population sharing one chunk follows the reference with pins at every position"
            <| fun _ ->
                // Every entry lies inside chunk (0, 0) of a 64-unit grid, so one bucket holds the whole
                // population. A narrow viewport inside that chunk makes all of them candidates and selects
                // only those whose bounds touch it.
                let total = 3000

                let entries =
                    [
                        for i in 0 .. total - 1 ->
                            box $"dense-{i}" (float (i % 50)) (float ((i / 50) % 50) * 0.5) 4.0 4.0
                    ]

                let q = query (rect 10.0 10.0 6.0 6.0) 0.0 [] 4

                let _, result = expectReference 64.0 entries q "one dense chunk"

                Expect.equal result.CandidateCount total "the whole population is a candidate"
                Expect.equal result.VisitedChunkCount 1 "a single chunk is visited"
                Expect.isLessThan result.Entries.Length total "only touching entries are selected"
                Expect.isGreaterThan result.Entries.Length 0 "some entries are selected"

                Expect.equal
                    (result.Entries |> List.map _.Id)
                    (result.Entries |> List.map _.Id |> List.sortBy (fun id -> int (id.Substring 6)))
                    "selected entries keep source order"

                let pins = [ "dense-0"; $"dense-{total - 1}"; $"dense-{total / 2}"; "absent" ]

                expectReference 64.0 entries (query (rect 10.0 10.0 6.0 6.0) 0.0 pins 4) "pins in a dense chunk"
                |> ignore

                expectReference
                    64.0
                    entries
                    (query (rect 0.0 0.0 64.0 64.0) 5.0 [] 16)
                    "whole dense chunk with overscan"
                |> ignore

            testCase "a deterministic mixed population of dense and chunk-spanning entries follows the reference"
            <| fun _ ->
                // Seeded pseudo-random rectangles of very different sizes in a world a few chunks wide, so
                // buckets are dense and entries span partial and overlapping chunk sets.
                let random = Random 20261025

                let entries =
                    [
                        for i in 0..1999 ->
                            let width =
                                if i % 17 = 0 then
                                    40.0 + random.NextDouble() * 140.0
                                else
                                    random.NextDouble() * 12.0

                            let height =
                                if i % 23 = 0 then
                                    40.0 + random.NextDouble() * 140.0
                                else
                                    random.NextDouble() * 12.0

                            box
                                $"mixed-{i}"
                                (-200.0 + random.NextDouble() * 600.0)
                                (-200.0 + random.NextDouble() * 600.0)
                                width
                                height
                    ]

                let viewports =
                    [
                        rect 0.0 0.0 64.0 64.0
                        rect -10.0 -10.0 20.0 20.0
                        rect 63.0 63.0 2.0 2.0
                        rect 64.0 0.0 0.0 0.0
                        rect 100.0 -150.0 130.0 90.0
                        rect -200.0 -200.0 600.0 600.0
                        rect 300.0 300.0 40.0 40.0
                    ]

                for chunk in [ 16.0; 64.0; 200.0 ] do
                    for v in viewports do
                        for overscan in [ 0.0; 7.5 ] do
                            for pins in [ []; [ "mixed-0"; "mixed-1999"; "mixed-1000"; "missing" ] ] do
                                expectReference
                                    chunk
                                    entries
                                    (query v overscan pins 100000)
                                    $"chunk {chunk} viewport {v} overscan {overscan} pins {pins.Length}"
                                |> ignore

            testCase "every bucket lists exactly its member ordinals in ascending source order"
            <| fun _ ->
                // Structure control against an independent direct scan. Query results are sorted by ordinal,
                // so a bucket that lost its order would not change them; this pins the construction itself.
                let random = Random 7
                let size = 32.0

                let entries =
                    [
                        for i in 0..799 ->
                            box
                                $"member-{i}"
                                (random.NextDouble() * 300.0)
                                (random.NextDouble() * 300.0)
                                (random.NextDouble() * 90.0)
                                (random.NextDouble() * 90.0)
                    ]

                let index =
                    SpatialWorkingSet.create size entries
                    |> Result.defaultWith (fun issues -> failtestf "%A" issues)

                let bucketField =
                    typeof<SpatialWorkingSetIndex<string>>
                        .GetFields(BindingFlags.Instance ||| BindingFlags.NonPublic ||| BindingFlags.Public)
                    |> Array.filter (fun field -> field.FieldType = typeof<Map<int64 * int64, int list>>)

                Expect.equal bucketField.Length 1 "exactly one private bucket map is retained"

                let actual =
                    Option.ofObj (bucketField[0].GetValue index) |> Option.get :?> Map<int64 * int64, int list>

                let expected = Collections.Generic.Dictionary<int64 * int64, ResizeArray<int>>()

                entries
                |> List.iteri (fun ordinal e ->
                    let first start = int64 (floor (start / size))
                    let last start length = int64 (floor ((start + length) / size))

                    for x in first e.Bounds.X .. last e.Bounds.X e.Bounds.Width do
                        for y in first e.Bounds.Y .. last e.Bounds.Y e.Bounds.Height do
                            match expected.TryGetValue((x, y)) with
                            | true, members -> members.Add ordinal
                            | _ -> expected[(x, y)] <- ResizeArray [ ordinal ])

                Expect.equal actual.Count expected.Count "the same chunks are populated"

                for KeyValue(key, members) in expected do
                    Expect.equal
                        (Map.find key actual)
                        (List.ofSeq members)
                        $"bucket {key} holds its ordinals in ascending order"

            testCase "construction cost does not grow quadratically with the population of one chunk"
            <| fun _ ->
                // Control for the per-bucket append. With `values @ [ index ]` the cost of building one
                // bucket of n entries is quadratic, so a single-chunk population is orders of magnitude slower
                // than a spread population of the same size and doubling it roughly quadruples the time. The
                // minimum over a few rounds absorbs scheduler and GC noise; the bounds are deliberately loose.
                let dense n =
                    [
                        for i in 0 .. n - 1 -> box $"dense-{i}" (float (i % 60)) (float ((i / 60) % 60)) 2.0 2.0
                    ]

                let spread n =
                    [ for i in 0 .. n - 1 -> box $"spread-{i}" (float i * 100.0) 0.0 8.0 8.0 ]

                let milliseconds entries =
                    let one () =
                        let start = Stopwatch.GetTimestamp()

                        SpatialWorkingSet.create 64.0 entries
                        |> Result.defaultWith (fun issues -> failtestf "%A" issues)
                        |> ignore

                        Stopwatch.GetElapsedTime(start).TotalMilliseconds

                    one () |> ignore
                    [ for _ in 1..3 -> one () ] |> List.min

                let denseSmall = milliseconds (dense 15000)
                let denseLarge = milliseconds (dense 30000)
                let spreadLarge = milliseconds (spread 30000)

                Expect.isLessThan
                    denseLarge
                    (max (spreadLarge * 4.0) (spreadLarge + 150.0))
                    $"30000 entries in one chunk: {denseLarge:F1}ms against {spreadLarge:F1}ms for 30000 spread entries"

                Expect.isLessThan
                    denseLarge
                    (max (denseSmall * 3.2) (denseSmall + 150.0))
                    $"doubling a one-chunk population: {denseSmall:F1}ms to {denseLarge:F1}ms"
        ]
