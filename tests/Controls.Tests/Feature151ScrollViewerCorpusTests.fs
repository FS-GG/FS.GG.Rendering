module Feature151ScrollViewerCorpusTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature151ScrollViewerCorpus" [
        test "ScrollViewer corpus covers the 11 required P8 cases" {
            let required =
                [ "empty-content"
                  "smaller-than-viewport"
                  "exact-fit"
                  "barely-overflowing"
                  "substantially-overflowing"
                  "nested-scroll"
                  "clipped-parent"
                  "layered-parent"
                  "text-natural-size"
                  "dynamic-content-change"
                  "invalid-intrinsic-fallback" ]

            let actual = Feature151ScrollViewerFixtures.cases |> List.map _.CaseId
            Expect.equal (Set.ofList actual) (Set.ofList required) "required scroll case ids"
        }

        test "every ScrollViewer case exposes finite viewport extent and non-negative offsets" {
            for item in Feature151ScrollViewerFixtures.cases do
                match Feature151ScrollViewerFixtures.viewportOf item with
                | Some viewport ->
                    Expect.isGreaterThanOrEqual viewport.ContentWidth viewport.Viewport.Width $"{item.CaseId}: content width"
                    Expect.isGreaterThanOrEqual viewport.ContentHeight viewport.Viewport.Height $"{item.CaseId}: content height"
                    Expect.isGreaterThanOrEqual viewport.MaxHorizontalOffset 0.0 $"{item.CaseId}: horizontal offset"
                    Expect.isGreaterThanOrEqual viewport.MaxVerticalOffset 0.0 $"{item.CaseId}: vertical offset"
                    Expect.equal viewport.Offset viewport.OffsetY $"{item.CaseId}: legacy offset alias"
                    Expect.equal viewport.MaxOffset viewport.MaxVerticalOffset $"{item.CaseId}: legacy max alias"
                | None -> failtestf "missing scroll viewport for %s" item.CaseId
        }

        test "overflowing cases report positive vertical scroll range from intrinsic extent" {
            for item in Feature151ScrollViewerFixtures.cases |> List.filter _.ExpectedVerticalOverflow do
                match Feature151ScrollViewerFixtures.viewportOf item with
                | Some viewport ->
                    Expect.equal viewport.ExtentSource IntrinsicContentExtent $"{item.CaseId}: intrinsic source"
                    Expect.isGreaterThan viewport.MaxVerticalOffset 0.0 $"{item.CaseId}: vertical overflow"
                | None -> failtestf "missing scroll viewport for %s" item.CaseId
        }

        test "dynamic content changes the accepted extent without changing the viewport box" {
            let item = Feature151ScrollViewerFixtures.cases |> List.find (fun item -> item.CaseId = "dynamic-content-change")

            match Feature151ScrollViewerFixtures.viewportOf item, Feature151ScrollViewerFixtures.changedViewportOf item with
            | Some baseline, Some changed ->
                Expect.equal changed.Viewport baseline.Viewport "viewport remains fixed"
                Expect.isGreaterThan changed.ContentHeight baseline.ContentHeight "content extent increases"
                Expect.isGreaterThan changed.MaxVerticalOffset baseline.MaxVerticalOffset "max offset increases"
            | _ -> failtest "missing dynamic ScrollViewer viewport"
        }

        test "empty content is classified with the empty extent source" {
            let item = Feature151ScrollViewerFixtures.cases |> List.find (fun item -> item.CaseId = "empty-content")

            match Feature151ScrollViewerFixtures.viewportOf item with
            | Some viewport ->
                Expect.equal viewport.ExtentSource EmptyContentExtent "empty source"
                Expect.equal viewport.MaxVerticalOffset 0.0 "empty max offset"
            | None -> failtest "missing empty ScrollViewer viewport"
        }
    ]
