module Feature146BrowserFeasibilityTests

open Expecto
open Rendering.Harness

[<Tests>]
let feature146BrowserFeasibilityTests =
    testList "Feature146 browser feasibility MVU" [
        test "init requests reference evidence loading" {
            let model, effects = RenderAnywhere.initBrowserFeasibility "out/browser"
            Expect.hasLength model.Corpus 3 "model starts with feasibility corpus"
            Expect.equal effects [ RenderAnywhere.LoadReferenceEvidence "out/browser" ] "init requests reference lookup"
        }

        test "references loaded emits candidate comparison effect" {
            let model, _ = RenderAnywhere.initBrowserFeasibility "out/browser"
            let updated, effects = RenderAnywhere.updateBrowserFeasibility (RenderAnywhere.ReferencesLoaded []) model

            Expect.equal updated.ReferenceEvidence [] "references are stored"
            match effects with
            | [ RenderAnywhere.CompareBrowserCandidate(corpus, references, backend) ] ->
                Expect.hasLength corpus 3 "comparison receives corpus"
                Expect.equal references [] "comparison receives references"
                Expect.equal backend "canvaskit-command-stream/proof" "candidate backend is explicit"
            | other -> failtestf "unexpected effects: %A" other
        }

        test "report records fallback decision" {
            let report = RenderAnywhere.buildBrowserFeasibilityReport (RenderAnywhere.corpus ()) [] "canvaskit-command-stream/proof"

            match report.Decision with
            | RenderAnywhere.DocumentedFallbackPath reason ->
                Expect.stringContains reason "CanvasKit" "fallback names CanvasKit path"
            | other -> failtestf "unexpected decision: %A" other

            report.Comparisons
            |> List.iter (fun item ->
                Expect.equal item.Verdict RenderAnywhere.CandidateEnvironmentLimited "environment-limited comparisons do not claim browser acceptance")
        }
    ]
