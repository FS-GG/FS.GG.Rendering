module Feature151RepresentativeCorpusTests

open Expecto
open FS.GG.UI.Layout

[<Tests>]
let tests =
    testList "Feature151RepresentativeCorpus" [
        test "representative corpus covers every required P8 layout category" {
            let required =
                [ "finite-root"
                  "zero-root"
                  "very-small-root"
                  "very-large-root"
                  "measured-leaves"
                  "empty-container"
                  "single-child"
                  "deep-nesting"
                  "dynamic-content"
                  "child-insert-remove"
                  "child-reorder"
                  "visibility-change"
                  "invalid-available"
                  "contradictory-size"
                  "duplicate-node" ]

            let actual = Feature151CorpusFixtures.allCases |> List.map _.CaseId
            Expect.equal (Set.ofList actual) (Set.ofList required) "stable corpus ids"
        }

        test "accepted corpus cases produce finite bounds for every required participant" {
            for item in Feature151CorpusFixtures.acceptedCases do
                let result = Feature151CorpusFixtures.resultOf item
                Expect.isEmpty result.Diagnostics $"{item.CaseId}: no diagnostics"

                for nodeId in item.RequiredNodeIds do
                    let bounds = Feature151CorpusFixtures.boundsOf nodeId result
                    Expect.isTrue (Feature151CorpusFixtures.finiteBounds bounds) $"{item.CaseId}:{nodeId} finite bounds"
        }

        test "visibility case retains hidden nodes and collapses collapsed nodes" {
            let item = Feature151CorpusFixtures.allCases |> List.find (fun item -> item.CaseId = "visibility-change")
            let result = Feature151CorpusFixtures.resultOf item
            let hidden = result.Bounds |> List.find (fun item -> item.NodeId = "hidden-child")
            let collapsed = result.Bounds |> List.find (fun item -> item.NodeId = "collapsed-child")

            Expect.equal hidden.Visibility Hidden "hidden node is retained"
            Expect.equal collapsed.Visibility Collapsed "collapsed node is retained"
            Expect.equal collapsed.Bounds.Width 0.0 "collapsed width"
            Expect.equal collapsed.Bounds.Height 0.0 "collapsed height"
        }

        test "measured leaves record child placement evidence through the public protocol" {
            let item = Feature151CorpusFixtures.allCases |> List.find (fun item -> item.CaseId = "measured-leaves")
            let measured = Layout.measureProtocol (Feature151CorpusFixtures.constraintsFor item) item.Root

            Expect.equal measured.ParticipantId item.Root.Id "participant"
            Expect.equal (measured.ChildPlacements |> List.map _.ChildId) [ "measured-a"; "measured-b" ] "placement ids"
            Expect.stringContains measured.CacheEntryId "MeasuredLayoutEntry" "cache entry kind is encoded"
        }
    ]
