namespace Rendering.Harness

open System
open System.IO
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

module RenderAnywhere =

    type CorpusItem =
        { ScenarioId: string
          Scene: Scene
          Package: PortableScenePackage }

    type BrowserCandidateVerdict =
        | CandidatePassed
        | CandidateFailed
        | CandidateUnsupportedCapability
        | CandidateMissingResource
        | CandidateEnvironmentLimited

    type BrowserFinalDecision =
        | AcceptedCandidatePath of string
        | DocumentedFallbackPath of string

    type BrowserComparison =
        { ScenarioId: string
          PackageIdentity: string
          ReferenceIdentity: string option
          CandidateIdentity: string option
          Tolerance: float
          DiffMetric: float option
          Verdict: BrowserCandidateVerdict
          Diagnostics: string list }

    type BrowserFeasibilityReport =
        { CandidateBackend: string
          Corpus: string list
          Tolerance: float
          Comparisons: BrowserComparison list
          UnsupportedCapabilities: string list
          Decision: BrowserFinalDecision
          Diagnostics: string list }

    type BrowserFeasibilityModel =
        { OutputDirectory: string
          CandidateBackend: string
          Corpus: CorpusItem list
          ReferenceEvidence: ReferenceRenderingEvidence list
          Report: BrowserFeasibilityReport option
          Diagnostics: string list }

    type BrowserFeasibilityMsg =
        | BrowserStart
        | ReferencesLoaded of ReferenceRenderingEvidence list
        | CandidateCompared of BrowserFeasibilityReport
        | BrowserFallbackSelected of string

    type BrowserFeasibilityEffect =
        | LoadReferenceEvidence of string
        | CompareBrowserCandidate of CorpusItem list * ReferenceRenderingEvidence list * string
        | WriteBrowserReport of BrowserFeasibilityReport * string

    let featureDirectory = "specs/146-render-anywhere-protocol"
    let readinessDirectory = Path.Combine(featureDirectory, "readiness")
    let roundTripDirectory = Path.Combine(readinessDirectory, "roundtrip")
    let referenceDirectory = Path.Combine(readinessDirectory, "reference")
    let browserDirectory = Path.Combine(readinessDirectory, "browser")

    let private package scenarioId scene =
        { ScenarioId = scenarioId
          Scene = scene
          Package = SceneCodec.export scene }

    let corpus () =
        let primitive =
            Scene.group
                [ Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 160.0; Height = 96.0 } (Colors.rgb 16uy 24uy 32uy)
                  Scene.circle { X = 44.0; Y = 42.0 } 20.0 (Colors.rgb 230uy 120uy 52uy)
                  Scene.line { X = 10.0; Y = 84.0 } { X = 150.0; Y = 16.0 } (Paint.stroke Colors.white 2.0) ]

        let layered =
            let child =
                Scene.group
                    [ Scene.rectangle (8.0, 8.0, 72.0, 48.0) (Colors.rgb 34uy 110uy 160uy)
                      Scene.sizedText (14.0, 38.0) "P6" 20.0 Colors.white ]

            Scene.group
                [ Scene.clipped (RectClip { X = 0.0; Y = 0.0; Width = 96.0; Height = 64.0 }) child
                  Scene.translate 80.0 24.0 child ]

        let shaped =
            Scene.glyphRunProof
                { X = 8.0; Y = 48.0 }
                "Render anywhere"
                { Family = Some "Noto Sans"; Size = 20.0; Weight = Some 400 }
                (Paint.fill (Colors.rgb 244uy 244uy 248uy))

        [ package "basic-primitives" primitive
          package "layered-portal" layered
          package "shaped-text" shaped ]

    let private verdictToken verdict =
        match verdict with
        | ReferencePassed -> "passed"
        | ReferenceFailed -> "failed"
        | ReferenceEnvironmentLimited -> "environment-limited"

    let formatReferenceEvidence (evidence: ReferenceRenderingEvidence list) =
        [ "# Feature 146 Reference Corpus Evidence"
          ""
          for item in evidence do
              let imagePath = item.ImagePath |> Option.defaultValue "none"
              let imageIdentity = item.ImageIdentity |> Option.defaultValue "none"
              $"- package: {item.PackageIdentity}"
              $"  verdict: {verdictToken item.Verdict}"
              $"  image: {imagePath}"
              $"  identity: {imageIdentity}" ]

    let runReferenceCommand outputDirectory =
        Directory.CreateDirectory(outputDirectory) |> ignore

        let evidence =
            corpus ()
            |> List.map (fun item ->
                let out = Path.Combine(outputDirectory, item.ScenarioId)
                ReferenceRendering.run
                    { PackageBytes = item.Package.CanonicalBytes
                      OutputDirectory = out
                      OutputSize = { Width = 192; Height = 128 }
                      Resources = [] })

        File.WriteAllLines(Path.Combine(outputDirectory, "summary.md"), formatReferenceEvidence evidence)
        evidence

    let initBrowserFeasibility outputDirectory =
        let items = corpus ()
        { OutputDirectory = outputDirectory
          CandidateBackend = "canvaskit-command-stream/proof"
          Corpus = items
          ReferenceEvidence = []
          Report = None
          Diagnostics = [] },
        [ LoadReferenceEvidence outputDirectory ]

    let private comparisonFor (tolerance: float) (references: ReferenceRenderingEvidence list) (item: CorpusItem) : BrowserComparison =
        let reference =
            references
            |> List.tryFind (fun evidence -> evidence.PackageIdentity = item.Package.PackageIdentity && evidence.Verdict = ReferencePassed)

        match reference with
        | Some evidence ->
            { ScenarioId = item.ScenarioId
              PackageIdentity = item.Package.PackageIdentity
              ReferenceIdentity = evidence.ImageIdentity
              CandidateIdentity = None
              Tolerance = tolerance
              DiffMetric = None
              Verdict = CandidateEnvironmentLimited
              Diagnostics = [ "CanvasKit candidate execution is not configured in this host; fallback decision recorded." ] }
        | None ->
            { ScenarioId = item.ScenarioId
              PackageIdentity = item.Package.PackageIdentity
              ReferenceIdentity = None
              CandidateIdentity = None
              Tolerance = tolerance
              DiffMetric = None
              Verdict = CandidateEnvironmentLimited
              Diagnostics = [ "No passed reference evidence available; browser candidate cannot claim acceptance." ] }

    let buildBrowserFeasibilityReport (corpus: CorpusItem list) (references: ReferenceRenderingEvidence list) (candidateBackend: string) : BrowserFeasibilityReport =
        let tolerance = 0.015
        let comparisons = corpus |> List.map (comparisonFor tolerance references)

        { CandidateBackend = candidateBackend
          Corpus = corpus |> List.map _.ScenarioId
          Tolerance = tolerance
          Comparisons = comparisons
          UnsupportedCapabilities = [ "direct browser execution unavailable in current harness" ]
          Decision = DocumentedFallbackPath "Continue with a generated CanvasKit command-stream proof; do not claim a production browser backend yet."
          Diagnostics = [ "Candidate path is evidence-only for Feature 146."; "Environment-limited browser results cannot count as accepted candidate evidence." ] }

    let updateBrowserFeasibility (msg: BrowserFeasibilityMsg) (model: BrowserFeasibilityModel) =
        match msg with
        | BrowserStart -> model, [ LoadReferenceEvidence model.OutputDirectory ]
        | ReferencesLoaded references ->
            let model = { model with ReferenceEvidence = references }
            model, [ CompareBrowserCandidate(model.Corpus, references, model.CandidateBackend) ]
        | CandidateCompared report ->
            { model with Report = Some report; Diagnostics = report.Diagnostics },
            [ WriteBrowserReport(report, model.OutputDirectory) ]
        | BrowserFallbackSelected reason ->
            let report =
                { buildBrowserFeasibilityReport model.Corpus model.ReferenceEvidence model.CandidateBackend with
                    Decision = DocumentedFallbackPath reason }

            { model with Report = Some report; Diagnostics = report.Diagnostics },
            [ WriteBrowserReport(report, model.OutputDirectory) ]

    let private candidateVerdictToken verdict =
        match verdict with
        | CandidatePassed -> "passed"
        | CandidateFailed -> "failed"
        | CandidateUnsupportedCapability -> "unsupported-capability"
        | CandidateMissingResource -> "missing-resource"
        | CandidateEnvironmentLimited -> "environment-limited"

    let private decisionText decision =
        match decision with
        | AcceptedCandidatePath value -> "accepted: " + value
        | DocumentedFallbackPath value -> "fallback: " + value

    let formatBrowserReport (report: BrowserFeasibilityReport) =
        [ "# Feature 146 Browser Feasibility"
          ""
          $"- candidate-backend: {report.CandidateBackend}"
          $"- tolerance: {report.Tolerance}"
          $"- decision: {decisionText report.Decision}"
          ""
          "## Comparisons"
          for comparison in report.Comparisons do
              let referenceIdentity = comparison.ReferenceIdentity |> Option.defaultValue "none"
              let candidateIdentity = comparison.CandidateIdentity |> Option.defaultValue "none"
              let diffMetric = comparison.DiffMetric |> Option.map string |> Option.defaultValue "none"
              $"- {comparison.ScenarioId}: {candidateVerdictToken comparison.Verdict}"
              $"  package: {comparison.PackageIdentity}"
              $"  reference: {referenceIdentity}"
              $"  candidate: {candidateIdentity}"
              $"  diff: {diffMetric}"
          ""
          "## Unsupported Capabilities"
          yield!
              if report.UnsupportedCapabilities.IsEmpty then
                  [ "- none" ]
              else
                  report.UnsupportedCapabilities |> List.map (fun item -> "- " + item)
          ""
          "## Diagnostics"
          yield!
              if report.Diagnostics.IsEmpty then
                  [ "- none" ]
              else
                  report.Diagnostics |> List.map (fun item -> "- " + item) ]

    let writeBrowserReport (outputDirectory: string) (report: BrowserFeasibilityReport) =
        Directory.CreateDirectory(outputDirectory) |> ignore
        let path = Path.Combine(outputDirectory, "browser-feasibility.md")
        File.WriteAllLines(path, formatBrowserReport report)
        path

    let runBrowserFeasibilityCommand outputDirectory =
        Directory.CreateDirectory(outputDirectory) |> ignore
        let references =
            if Directory.Exists referenceDirectory then
                []
            else
                []

        let report = buildBrowserFeasibilityReport (corpus ()) references "canvaskit-command-stream/proof"
        writeBrowserReport outputDirectory report |> ignore
        report
