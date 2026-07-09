module Feature146BrowserFeasibilityTests

open Expecto
open Rendering.Harness
open FS.GG.UI.SkiaViewer

[<Tests>]
let feature146BrowserFeasibilityTests =
    testList "Feature146 browser feasibility MVU" [
        test "init requests reference evidence loading" {
            let model, effects = RenderAnywhere.initBrowserFeasibility "out/browser"
            Expect.hasLength model.Corpus 3 "model starts with feasibility corpus"
            Expect.equal effects [ RenderAnywhere.LoadReferenceEvidence "out/browser" ] "init requests reference lookup"
        }

        test "references loaded emits candidate capability assessment effect" {
            let model, _ = RenderAnywhere.initBrowserFeasibility "out/browser"
            let updated, effects = RenderAnywhere.updateBrowserFeasibility (RenderAnywhere.ReferencesLoaded []) model

            Expect.equal updated.ReferenceEvidence [] "references are stored"
            match effects with
            | [ RenderAnywhere.AssessCandidateCapability(corpus, references, backend) ] ->
                Expect.hasLength corpus 3 "assessment receives corpus"
                Expect.equal references [] "assessment receives references"
                Expect.equal backend "canvaskit-command-stream/proof" "candidate backend is explicit"
            | other -> failtestf "unexpected effects: %A" other
        }

        test "report records fallback decision and never claims a candidate ran" {
            let report =
                RenderAnywhere.buildBrowserCapabilityReport (RenderAnywhere.corpus ()) [] "canvaskit-command-stream/proof"

            let (RenderAnywhere.DocumentedFallbackPath reason) = report.Decision
            Expect.stringContains reason "CanvasKit" "fallback names CanvasKit path"

            report.Scenarios
            |> List.iter (fun item ->
                Expect.equal item.Status RenderAnywhere.CandidateMissingReference "no reference evidence means no citable reference")
        }

        test "a passed reference is cited by identity and still reports the candidate as not executed" {
            let corpus = RenderAnywhere.corpus ()
            let head = List.head corpus

            let references: RenderAnywhere.ReferenceSummaryEntry list =
                [ { PackageIdentity = head.Package.PackageIdentity
                    Verdict = ReferencePassed
                    ImageIdentity = Some "sha256:reference-image" } ]

            let report = RenderAnywhere.buildBrowserCapabilityReport corpus references "canvaskit-command-stream/proof"
            let scenario = report.Scenarios |> List.find (fun item -> item.ScenarioId = head.ScenarioId)

            Expect.equal scenario.ReferenceIdentity (Some "sha256:reference-image") "the passed reference identity reaches the report"
            Expect.equal scenario.Status RenderAnywhere.CandidateNotExecuted "a passed reference does not imply the candidate ran"

            report.Scenarios
            |> List.filter (fun item -> item.ScenarioId <> head.ScenarioId)
            |> List.iter (fun item ->
                Expect.equal item.Status RenderAnywhere.CandidateMissingReference "scenarios without reference evidence stay missing-reference")
        }

        test "a failed reference is not citable as a passed reference" {
            let corpus = RenderAnywhere.corpus ()
            let head = List.head corpus

            let references: RenderAnywhere.ReferenceSummaryEntry list =
                [ { PackageIdentity = head.Package.PackageIdentity
                    Verdict = ReferenceFailed
                    ImageIdentity = Some "sha256:reference-image" } ]

            let report = RenderAnywhere.buildBrowserCapabilityReport corpus references "canvaskit-command-stream/proof"
            let scenario = report.Scenarios |> List.find (fun item -> item.ScenarioId = head.ScenarioId)

            Expect.equal scenario.Status RenderAnywhere.CandidateMissingReference "a failed reference is not a reference"
            Expect.equal scenario.ReferenceIdentity None "a failed reference identity is not cited"
        }
    ]
