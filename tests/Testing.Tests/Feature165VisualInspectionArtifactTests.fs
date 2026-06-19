module TestingCapability.Feature165VisualInspectionArtifactTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Testing

let private unsupported =
    VisualInspection.unsupportedFact "transform-bounds" (Some "card") true "not representable" "explicit unsupported fact" false

let private blocking =
    VisualInspection.finding "required-region-painted" VisualInspectionSeverity.Blocking [] [ "root" ] "missing paint" "painted" "missing"

let private summary: VisualInspectionSummary =
    { RunId = "run"
      OverallStatus = VisualInspectionStatus.Blocked
      ArtifactCount = 1
      InspectedScopes = [ "sample" ]
      NotInspectedScopes = []
      NotRunScopes = []
      StatusCounts = [ "blocked", 1 ]
      FindingCounts = [ "blocking", 1 ]
      BlockingFindings = [ blocking ]
      UnsupportedFacts = [ unsupported ]
      AcceptedExceptions = [ "overlay-reviewed" ]
      InvalidExceptions = [ "invalid-exception" ]
      RelatedVisualEvidence = [ "screens/sample.png" ]
      Caveats = [ "bounded representative evidence" ]
      Diagnostics = [ "diagnostic" ] }

[<Tests>]
let tests =
    testList
        "Feature165 visual inspection artifacts"
        [ test "Markdown summary includes required review fields" {
              let markdown = VisualInspectionMarkdown.renderSummary summary
              Expect.stringContains markdown "Visual Inspection" "heading"
              Expect.stringContains markdown "status: **blocked**" "overall status"
              Expect.stringContains markdown "Blocking Findings" "blocking section"
              Expect.stringContains markdown "Unsupported Facts" "unsupported section"
              Expect.stringContains markdown "screens/sample.png" "related visual evidence"
              Expect.stringContains markdown "overlay-reviewed" "accepted exception"
          }

          test "JSON summary exposes machine-readable fields without parsing Markdown" {
              let json = VisualInspectionMarkdown.renderJson summary
              Expect.stringContains json "\"runId\": \"run\"" "run id"
              Expect.stringContains json "\"overallStatus\": \"blocked\"" "status"
              Expect.stringContains json "\"statusCounts\"" "status counts"
              Expect.stringContains json "\"blockingFindings\"" "blocking findings"
              Expect.stringContains json "\"unsupportedFacts\"" "unsupported facts"
              Expect.stringContains json "\"relatedVisualEvidence\"" "related evidence"
          }

          test "managed section insertion replacement and unsafe marker states are deterministic" {
              let generated = VisualInspectionMarkdown.renderSummary summary
              let inserted = VisualInspectionMarkdown.updateManagedSection "Manual" generated
              Expect.isTrue inserted.SafeToWrite "missing markers inserted safely"
              Expect.isTrue inserted.InsertedMarkers "markers inserted"
              Expect.stringContains inserted.UpdatedText VisualInspectionMarkdown.startMarker "start marker"

              let replacement = VisualInspectionMarkdown.updateManagedSection inserted.UpdatedText "## Visual Inspection\n\nreplacement"
              Expect.isTrue replacement.SafeToWrite "single section replaced safely"
              Expect.stringContains replacement.UpdatedText "replacement" "new content"
              Expect.isFalse (replacement.UpdatedText.Contains("bounded representative evidence")) "old generated section replaced"

              let malformed = VisualInspectionMarkdown.updateManagedSection (VisualInspectionMarkdown.startMarker + "\n" + VisualInspectionMarkdown.startMarker) generated
              Expect.isFalse malformed.SafeToWrite "multiple starts are unsafe"
              Expect.isNonEmpty malformed.Diagnostics "unsafe diagnostic"
          } ]
