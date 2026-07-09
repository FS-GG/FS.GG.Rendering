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

    type ReferenceSummaryEntry =
        { PackageIdentity: string
          Verdict: ReferenceRenderVerdict
          ImageIdentity: string option }

    type CandidateCapabilityStatus =
        | CandidateNotExecuted
        | CandidateMissingReference

    type BrowserFinalDecision = DocumentedFallbackPath of string

    type ScenarioCapability =
        { ScenarioId: string
          PackageIdentity: string
          ReferenceIdentity: string option
          Status: CandidateCapabilityStatus
          Diagnostics: string list }

    type BrowserCapabilityReport =
        { CandidateBackend: string
          Corpus: string list
          Scenarios: ScenarioCapability list
          UnsupportedCapabilities: string list
          Decision: BrowserFinalDecision
          Diagnostics: string list }

    type BrowserFeasibilityModel =
        { OutputDirectory: string
          CandidateBackend: string
          Corpus: CorpusItem list
          ReferenceEvidence: ReferenceSummaryEntry list
          Report: BrowserCapabilityReport option
          Diagnostics: string list }

    type BrowserFeasibilityMsg =
        | BrowserStart
        | ReferencesLoaded of ReferenceSummaryEntry list
        | CapabilityAssessed of BrowserCapabilityReport
        | BrowserFallbackSelected of string

    type BrowserFeasibilityEffect =
        | LoadReferenceEvidence of string
        | AssessCandidateCapability of CorpusItem list * ReferenceSummaryEntry list * string
        | WriteBrowserReport of BrowserCapabilityReport * string

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

    let private verdictOfToken token =
        match token with
        | "passed" -> Some ReferencePassed
        | "failed" -> Some ReferenceFailed
        | "environment-limited" -> Some ReferenceEnvironmentLimited
        | _ -> None

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

    let summaryEntries (evidence: ReferenceRenderingEvidence list) =
        evidence
        |> List.map (fun item ->
            { PackageIdentity = item.PackageIdentity
              Verdict = item.Verdict
              ImageIdentity = item.ImageIdentity })

    /// Parse the `summary.md` written by `runReferenceCommand` back into the subset of reference
    /// evidence a capability report can honestly cite: package identity, verdict, image identity.
    /// Fields the summary does not carry are not reconstructed. A missing or malformed summary
    /// yields `[]`, which the report surfaces as `CandidateMissingReference`, never as acceptance.
    let readReferenceSummary (directory: string) : ReferenceSummaryEntry list =
        let path = Path.Combine(directory, "summary.md")

        if not (File.Exists path) then
            []
        else
            let field (prefix: string) (line: string) =
                let trimmed = line.Trim()

                if trimmed.StartsWith(prefix, StringComparison.Ordinal) then
                    Some(trimmed.Substring(prefix.Length).Trim())
                else
                    None

            let optional value =
                if value = "none" || value = "" then None else Some value

            // A record is a `- package:` line followed by its indented fields, so the next
            // `- package:` (or end of file) closes the record before it.
            let flush (identity, verdict, imageIdentity) =
                match identity, verdict |> Option.bind verdictOfToken with
                | Some identity, Some verdict ->
                    [ { PackageIdentity = identity
                        Verdict = verdict
                        ImageIdentity = imageIdentity |> Option.bind optional } ]
                | _ -> []

            let entries, pending =
                File.ReadAllLines path
                |> Array.fold
                    (fun (entries, (identity, verdict, imageIdentity)) line ->
                        match field "- package:" line with
                        | Some value -> entries @ flush (identity, verdict, imageIdentity), (Some value, None, None)
                        | None ->
                            match field "verdict:" line, field "identity:" line with
                            | Some value, _ -> entries, (identity, Some value, imageIdentity)
                            | _, Some value -> entries, (identity, verdict, Some value)
                            | None, None -> entries, (identity, verdict, imageIdentity))
                    ([], (None, None, None))

            entries @ flush pending

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

    let private capabilityFor (references: ReferenceSummaryEntry list) (item: CorpusItem) : ScenarioCapability =
        let reference =
            references
            |> List.tryFind (fun entry ->
                entry.PackageIdentity = item.Package.PackageIdentity && entry.Verdict = ReferencePassed)

        match reference with
        | Some entry ->
            { ScenarioId = item.ScenarioId
              PackageIdentity = item.Package.PackageIdentity
              ReferenceIdentity = entry.ImageIdentity
              Status = CandidateNotExecuted
              Diagnostics = [ "CanvasKit candidate execution is not configured in this host; no candidate image exists to compare." ] }
        | None ->
            { ScenarioId = item.ScenarioId
              PackageIdentity = item.Package.PackageIdentity
              ReferenceIdentity = None
              Status = CandidateMissingReference
              Diagnostics = [ "No passed reference evidence available; run render-anywhere-reference first." ] }

    let buildBrowserCapabilityReport
        (corpus: CorpusItem list)
        (references: ReferenceSummaryEntry list)
        (candidateBackend: string)
        : BrowserCapabilityReport =
        { CandidateBackend = candidateBackend
          Corpus = corpus |> List.map _.ScenarioId
          Scenarios = corpus |> List.map (capabilityFor references)
          UnsupportedCapabilities = [ "direct browser execution unavailable in current harness" ]
          Decision =
            DocumentedFallbackPath
                "Continue with a generated CanvasKit command-stream proof; do not claim a production browser backend yet."
          Diagnostics =
            [ "This is a capability report, not a perceptual diff: no candidate image is produced, so no image is compared."
              "Cross-backend visual fidelity is UNPROVEN and this report is not evidence of it." ] }

    let updateBrowserFeasibility (msg: BrowserFeasibilityMsg) (model: BrowserFeasibilityModel) =
        match msg with
        | BrowserStart -> model, [ LoadReferenceEvidence model.OutputDirectory ]
        | ReferencesLoaded references ->
            let model = { model with ReferenceEvidence = references }
            model, [ AssessCandidateCapability(model.Corpus, references, model.CandidateBackend) ]
        | CapabilityAssessed report ->
            { model with Report = Some report; Diagnostics = report.Diagnostics },
            [ WriteBrowserReport(report, model.OutputDirectory) ]
        | BrowserFallbackSelected reason ->
            let report =
                { buildBrowserCapabilityReport model.Corpus model.ReferenceEvidence model.CandidateBackend with
                    Decision = DocumentedFallbackPath reason }

            { model with Report = Some report; Diagnostics = report.Diagnostics },
            [ WriteBrowserReport(report, model.OutputDirectory) ]

    let private statusToken status =
        match status with
        | CandidateNotExecuted -> "candidate-not-executed"
        | CandidateMissingReference -> "missing-reference"

    let private decisionText decision =
        match decision with
        | DocumentedFallbackPath value -> "fallback: " + value

    let formatBrowserReport (report: BrowserCapabilityReport) =
        [ "# Feature 146 Browser Capability"
          ""
          "No candidate image is rendered and no perceptual diff is computed. This report records which"
          "corpus scenes have passing reference evidence, and why the browser candidate did not run. It"
          "is NOT cross-backend fidelity evidence."
          ""
          $"- candidate-backend: {report.CandidateBackend}"
          "- comparison: not performed"
          $"- decision: {decisionText report.Decision}"
          ""
          "## Scenarios"
          for scenario in report.Scenarios do
              let referenceIdentity = scenario.ReferenceIdentity |> Option.defaultValue "none"
              $"- {scenario.ScenarioId}: {statusToken scenario.Status}"
              $"  package: {scenario.PackageIdentity}"
              $"  reference: {referenceIdentity}"
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

    let writeBrowserReport (outputDirectory: string) (report: BrowserCapabilityReport) =
        Directory.CreateDirectory(outputDirectory) |> ignore
        let path = Path.Combine(outputDirectory, "browser-feasibility.md")
        File.WriteAllLines(path, formatBrowserReport report)
        path

    let runBrowserCapabilityCommand (referenceDirectory: string) (outputDirectory: string) =
        Directory.CreateDirectory(outputDirectory) |> ignore
        let references = readReferenceSummary referenceDirectory
        let report = buildBrowserCapabilityReport (corpus ()) references "canvaskit-command-stream/proof"
        writeBrowserReport outputDirectory report |> ignore
        report
