module FS.GG.UI.Scene.Tests.SpatialWorkingSetTests

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
        ]
