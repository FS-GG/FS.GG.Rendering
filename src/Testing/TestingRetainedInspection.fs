namespace FS.GG.UI.Testing

open System
open System.IO
open System.Security.Cryptography
open System.Text
open FS.GG.UI.Scene
open SkiaSharp
// Testing.fs was split into per-domain files; re-open the package namespace AFTER the third-party
// opens so the Testing types win unqualified-name resolution exactly as in the original single file.
open FS.GG.UI.Testing

module RetainedInspectionValidation =
    let rule (ruleId: string) : RetainedInspectionRule = { RuleId = ruleId; Required = true }

    let defaultRules : RetainedInspectionRule list =
        [ "retained-node-status-classified"
          "retained-identity-stable"
          "dirty-region-unioned"
          "empty-damage-explicit"
          "damage-localized-to-expected-region"
          "full-surface-localized-change-blocked"
          "broad-damage-requires-exception"
          "shifted-nodes-separated"
          "unsupported-damage-explicit"
          "not-inspected-damage-explicit"
          "antshowcase-structured-evidence-present"
          "screenshot-readiness-counts-preserved" ]
        |> List.map rule

    let private isBlank value = String.IsNullOrWhiteSpace value

    let private finding
        (ruleId: string)
        (severity: VisualInspectionSeverity)
        (transitionId: string)
        (nodeIds: string list)
        (regionIds: string list)
        (message: string)
        (expected: string)
        (actual: string)
        : DamageLocalityFinding =
        RetainedInspection.finding ruleId severity transitionId nodeIds regionIds message expected actual

    let private transitionId (artifact: RetainedInspectionArtifact) =
        artifact.Transition |> Option.map _.TransitionId |> Option.defaultValue artifact.ArtifactId

    let private affectedIds (finding: DamageLocalityFinding) =
        finding.AffectedNodeIds @ finding.AffectedRegionIds |> List.sort

    let private exceptionValid (ex: IntentionalDamageException) =
        not (isBlank ex.ExceptionId)
        && not (isBlank ex.RuleId)
        && not (isBlank ex.ScopeId)
        && not (isBlank ex.TransitionId)
        && not ex.AffectedIds.IsEmpty
        && not (isBlank ex.Reason)

    let private exceptionMatches (finding: DamageLocalityFinding) (ex: IntentionalDamageException) =
        exceptionValid ex
        && ex.RuleId = finding.RuleId
        && ex.TransitionId = finding.TransitionId
        && Set.ofList ex.AffectedIds = Set.ofList (affectedIds finding)

    let private nodeStatusClassified (artifact: RetainedInspectionArtifact) =
        let tid = transitionId artifact

        [ for node in artifact.RetainedNodes do
              if node.Status = RetainedNodeStatus.Unsupported && node.UnsupportedFacts.IsEmpty then
                  finding
                      "retained-node-status-classified"
                      VisualInspectionSeverity.Unsupported
                      tid
                      [ node.NodeId ]
                      []
                      $"retained node `{node.NodeId}` is unsupported without an explicit fact"
                      "unsupported facts include fact name, owner, and reason"
                      "missing unsupported fact"

              match node.Status, node.PriorBounds, node.CurrentBounds with
              | RetainedNodeStatus.Shifted, None, _
              | RetainedNodeStatus.Shifted, _, None
              | RetainedNodeStatus.ShiftedAndRepainted, None, _
              | RetainedNodeStatus.ShiftedAndRepainted, _, None ->
                  finding
                      "retained-node-status-classified"
                      VisualInspectionSeverity.Blocking
                      tid
                      [ node.NodeId ]
                      node.AffectedRegionIds
                      $"shifted retained node `{node.NodeId}` is missing prior or current bounds"
                      "shifted nodes carry prior and current bounds"
                      "bounds missing"
              | RetainedNodeStatus.Added, Some _, _
              | RetainedNodeStatus.Removed, _, Some _ ->
                  finding
                      "retained-node-status-classified"
                      VisualInspectionSeverity.Blocking
                      tid
                      [ node.NodeId ]
                      node.AffectedRegionIds
                      $"retained node `{node.NodeId}` has inconsistent added/removed bounds"
                      "added has no prior bounds and removed has no current bounds"
                      "inconsistent bounds"
              | _ -> () ]

    let private retainedIdentityStable (artifact: RetainedInspectionArtifact) (previous: RetainedInspectionArtifact option) =
        match previous with
        | None -> []
        | Some before ->
            let identities item =
                item.RetainedNodes
                |> List.map (fun node -> node.NodeId, node.RetainedIdentity)
                |> Map.ofList

            if identities before = identities artifact then
                []
            else
                [ finding
                      "retained-identity-stable"
                      VisualInspectionSeverity.Blocking
                      (transitionId artifact)
                      (artifact.RetainedNodes |> List.map _.NodeId)
                      []
                      "retained node identities changed between unchanged artifacts"
                      "same retained identity map"
                      "identity map changed" ]

    let private dirtyRegionUnioned (artifact: RetainedInspectionArtifact) =
        match artifact.Damage with
        | None ->
            [ finding
                  "dirty-region-unioned"
                  VisualInspectionSeverity.Unsupported
                  (transitionId artifact)
                  []
                  []
                  "retained damage evidence is missing"
                  "damage evidence with true union area"
                  "missing damage" ]
        | Some damage ->
            let expected = RetainedInspection.dirtyUnionArea damage.FrameBounds damage.DirtyRectangles
            let frameArea = max 0.0 (damage.FrameBounds.Width * damage.FrameBounds.Height) |> int

            [ if expected <> damage.UnionArea || damage.VisibleDirtyArea <> damage.UnionArea then
                  finding
                      "dirty-region-unioned"
                      VisualInspectionSeverity.Blocking
                      damage.TransitionId
                      damage.AffectedNodeIds
                      damage.AffectedRegionIds
                      "dirty region union area does not match clipped true-union area"
                      (string expected)
                      (string damage.UnionArea)
              if damage.VisibleDirtyArea > frameArea then
                  finding
                      "dirty-region-unioned"
                      VisualInspectionSeverity.Blocking
                      damage.TransitionId
                      damage.AffectedNodeIds
                      damage.AffectedRegionIds
                      "visible dirty area exceeds visible frame area"
                      (string frameArea)
                      (string damage.VisibleDirtyArea) ]

    let private emptyDamageExplicit (artifact: RetainedInspectionArtifact) =
        match artifact.Damage with
        | Some damage when damage.DirtyRectangles.IsEmpty && damage.VisibleDirtyArea = 0 && damage.DamageStatus <> DamageInspectionStatus.Empty && damage.DamageStatus <> DamageInspectionStatus.NotInspected ->
            [ finding
                  "empty-damage-explicit"
                  VisualInspectionSeverity.Blocking
                  damage.TransitionId
                  []
                  []
                  "empty retained damage must use the empty status"
                  "empty"
                  (RetainedInspection.damageStatusText damage.DamageStatus) ]
        | None ->
            [ finding
                  "empty-damage-explicit"
                  VisualInspectionSeverity.Unsupported
                  (transitionId artifact)
                  []
                  []
                  "damage evidence is absent instead of explicit"
                  "empty, localized, broad, full-surface, unsupported, or not-inspected"
                  "missing" ]
        | _ -> []

    let private localizedToExpected (artifact: RetainedInspectionArtifact) (expectedRegions: string list) =
        match artifact.Damage with
        | Some damage ->
            let expected = Set.ofList (expectedRegions @ (artifact.Transition |> Option.map _.ExpectedAffectedRegionIds |> Option.defaultValue []))
            let observed = Set.ofList damage.AffectedRegionIds
            let outside = Set.difference observed expected |> Set.toList

            [ if not expected.IsEmpty && not outside.IsEmpty then
                  finding
                      "damage-localized-to-expected-region"
                      VisualInspectionSeverity.Blocking
                      damage.TransitionId
                      damage.AffectedNodeIds
                      outside
                      "dirty regions escaped the expected affected regions"
                      (String.concat ", " (Set.toList expected))
                      (String.concat ", " outside)
              if damage.DamageStatus = DamageInspectionStatus.Broad then
                  let dirtyPercentage = damage.DirtyPercentage.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "%"

                  finding
                      "damage-localized-to-expected-region"
                      VisualInspectionSeverity.Warning
                      damage.TransitionId
                      damage.AffectedNodeIds
                      damage.AffectedRegionIds
                      "dirty percentage exceeds the declared localized budget"
                      "localized damage within budget"
                      dirtyPercentage ]
        | None -> []

    let private fullSurfaceBlocked (artifact: RetainedInspectionArtifact) =
        match artifact.Damage with
        | Some damage when damage.DamageStatus = DamageInspectionStatus.FullSurface ->
            [ finding
                  "full-surface-localized-change-blocked"
                  VisualInspectionSeverity.Blocking
                  damage.TransitionId
                  damage.AffectedNodeIds
                  damage.AffectedRegionIds
                  "localized retained transition dirtied the full visible surface"
                  "localized dirty region"
                  "full-surface" ]
        | _ -> []

    let private broadDamageRequiresException (artifact: RetainedInspectionArtifact) =
        match artifact.Damage with
        | Some damage when damage.DamageStatus = DamageInspectionStatus.Broad ->
            [ finding
                  "broad-damage-requires-exception"
                  VisualInspectionSeverity.Blocking
                  damage.TransitionId
                  damage.AffectedNodeIds
                  damage.AffectedRegionIds
                  "broad retained damage needs a scoped intentional exception"
                  "localized or accepted-by-exception"
                  "broad" ]
        | _ -> []

    let private shiftedNodesSeparated (artifact: RetainedInspectionArtifact) =
        match artifact.Damage with
        | None -> []
        | Some damage ->
            let shifted = artifact.RetainedNodes |> List.filter _.Shifted |> List.length
            let repainted = artifact.RetainedNodes |> List.filter _.Repainted |> List.length

            [ if shifted <> damage.ShiftedNodeCount then
                  finding
                      "shifted-nodes-separated"
                      VisualInspectionSeverity.Blocking
                      damage.TransitionId
                      (artifact.RetainedNodes |> List.filter _.Shifted |> List.map _.NodeId)
                      damage.AffectedRegionIds
                      "shifted node count does not match shifted retained node facts"
                      (string shifted)
                      (string damage.ShiftedNodeCount)
              if repainted <> damage.RepaintedNodeCount then
                  finding
                      "shifted-nodes-separated"
                      VisualInspectionSeverity.Blocking
                      damage.TransitionId
                      (artifact.RetainedNodes |> List.filter _.Repainted |> List.map _.NodeId)
                      damage.AffectedRegionIds
                      "repainted node count does not match retained node facts"
                      (string repainted)
                      (string damage.RepaintedNodeCount) ]

    let private unsupportedDamageExplicit (artifact: RetainedInspectionArtifact) (environmentLimitations: string list) =
        [ for fact in artifact.UnsupportedFacts do
              if fact.Required then
                  let severity =
                      if fact.EnvironmentLimited || not environmentLimitations.IsEmpty then
                          VisualInspectionSeverity.EnvironmentLimited
                      else
                          VisualInspectionSeverity.Unsupported

                  finding
                      "unsupported-damage-explicit"
                      severity
                      (transitionId artifact)
                      (fact.OwnerId |> Option.map List.singleton |> Option.defaultValue [])
                      []
                      $"required retained/damage fact `{fact.Fact}` is unsupported"
                      "required fact inspectable or explicitly environment-limited"
                      fact.Reason ]

    let private notInspectedDamageExplicit (artifact: RetainedInspectionArtifact) =
        match artifact.Damage with
        | Some damage when damage.DamageStatus = DamageInspectionStatus.NotInspected ->
            if damage.Cause = Some "first-frame-no-prior" then
                []
            else
                [ finding
                      "not-inspected-damage-explicit"
                      VisualInspectionSeverity.Unsupported
                      damage.TransitionId
                      []
                      []
                      "declared retained damage scope was not inspected"
                      "inspected damage or first-frame/no-prior diagnostic"
                      "not-inspected" ]
        | None -> []
        | _ -> []

    let private antshowcaseStructuredEvidencePresent (artifact: RetainedInspectionArtifact) =
        if artifact.Scope.ScopeId.Contains("antshowcase", StringComparison.OrdinalIgnoreCase) then
            let hasStructured =
                artifact.RelatedVisualEvidence
                |> List.exists (fun value -> value.Contains("retained", StringComparison.OrdinalIgnoreCase))

            if hasStructured then
                []
            else
                [ finding
                      "antshowcase-structured-evidence-present"
                      VisualInspectionSeverity.Blocking
                      (transitionId artifact)
                      []
                      []
                      "AntShowcase retained inspection artifact is missing structured evidence reference"
                      "retained structured evidence link"
                      "missing" ]
        else
            []

    let private screenshotReadinessCountsPreserved (artifact: RetainedInspectionArtifact) =
        if artifact.Scope.ScopeId.Contains("antshowcase", StringComparison.OrdinalIgnoreCase) then
            let joined = String.concat " " artifact.RelatedVisualEvidence

            if joined.Contains("preferred=38", StringComparison.OrdinalIgnoreCase)
               && joined.Contains("minimum=12", StringComparison.OrdinalIgnoreCase) then
                []
            else
                [ finding
                      "screenshot-readiness-counts-preserved"
                      VisualInspectionSeverity.Blocking
                      (transitionId artifact)
                      []
                      []
                      "AntShowcase screenshot readiness count parity is missing"
                      "preferred=38 and minimum=12"
                      "missing count parity" ]
        else
            []

    let private findingsForRule (check: RetainedInspectionValidationCheck) (rule: RetainedInspectionRule) : DamageLocalityFinding list =
        match rule.RuleId with
        | "retained-node-status-classified" -> nodeStatusClassified check.Artifact
        | "retained-identity-stable" -> retainedIdentityStable check.Artifact check.PreviousArtifact
        | "dirty-region-unioned" -> dirtyRegionUnioned check.Artifact
        | "empty-damage-explicit" -> emptyDamageExplicit check.Artifact
        | "damage-localized-to-expected-region" -> localizedToExpected check.Artifact check.ExpectedAffectedRegionIds
        | "full-surface-localized-change-blocked" -> fullSurfaceBlocked check.Artifact
        | "broad-damage-requires-exception" -> broadDamageRequiresException check.Artifact
        | "shifted-nodes-separated" -> shiftedNodesSeparated check.Artifact
        | "unsupported-damage-explicit" -> unsupportedDamageExplicit check.Artifact check.EnvironmentLimitations
        | "not-inspected-damage-explicit" -> notInspectedDamageExplicit check.Artifact
        | "antshowcase-structured-evidence-present" -> antshowcaseStructuredEvidencePresent check.Artifact
        | "screenshot-readiness-counts-preserved" -> screenshotReadinessCountsPreserved check.Artifact
        | unknown when rule.Required ->
            [ finding unknown VisualInspectionSeverity.Unsupported (transitionId check.Artifact) [] [] $"rule `{unknown}` is not implemented" "implemented retained inspection rule" "unknown rule" ]
        | _ -> []

    let validateCheck (check: RetainedInspectionValidationCheck) : RetainedInspectionValidationResult =
        let invalidExceptions =
            check.Exceptions
            |> List.filter (exceptionValid >> not)
            |> List.map _.ExceptionId

        let initialFindings =
            check.Rules
            |> List.collect (findingsForRule check)
            |> List.append check.Artifact.Findings
            |> List.sortBy _.FindingId

        let validExceptions = check.Exceptions |> List.filter exceptionValid

        // Feature 186 (US3): delegate to the one shared algorithm with the RETAINED knobs — accept
        // `Blocking || Warning` (line 392 of the former copy) and derive `ReviewRequired` when a
        // `Warning` is present (the severity asymmetry the visual family lacks, FR-005). Byte-identical
        // to the former hand-spelled body.
        let knobs: SharedTesting.InspectionValidationKnobs<DamageLocalityFinding, IntentionalDamageException, RetainedInspectionStatus, RetainedInspectionValidationResult> =
            { SeverityOf = _.Severity
              FindingIdOf = _.FindingId
              MatchException = exceptionMatches
              ExceptionIdOf = _.ExceptionId
              Accept =
                fun severity ->
                    severity = VisualInspectionSeverity.Blocking
                    || severity = VisualInspectionSeverity.Warning
              AcceptFinding =
                fun f ex ->
                    { f with
                        Severity = VisualInspectionSeverity.Pass
                        ExceptionId = Some ex.ExceptionId
                        Diagnostics = f.Diagnostics @ [ $"accepted by retained inspection exception `{ex.ExceptionId}`: {ex.Reason}" ] }
              InvalidWording = fun id -> $"invalid retained inspection exception: {id}"
              UnusedWording = fun id -> $"unused retained inspection exception: {id}"
              DeriveStatus =
                fun has ->
                    if not invalidExceptions.IsEmpty || has VisualInspectionSeverity.Blocking then
                        RetainedInspectionStatus.Blocked
                    elif has VisualInspectionSeverity.EnvironmentLimited then
                        RetainedInspectionStatus.EnvironmentLimited
                    elif has VisualInspectionSeverity.Unsupported then
                        if check.EnvironmentLimitations.IsEmpty then
                            RetainedInspectionStatus.Unsupported
                        else
                            RetainedInspectionStatus.EnvironmentLimited
                    elif has VisualInspectionSeverity.Warning then
                        RetainedInspectionStatus.ReviewRequired
                    else
                        check.Artifact.ReadinessStatus
              MkResult =
                fun status findings appliedIds invalidIds unused diagnostics ->
                    { ArtifactId = check.Artifact.ArtifactId
                      ReadinessStatus = status
                      Findings = findings
                      AppliedExceptions = appliedIds
                      InvalidExceptions = invalidIds
                      UnusedExceptions = unused
                      Diagnostics = diagnostics } }

        SharedTesting.validateCheck
            knobs
            (RetainedInspection.artifactDiagnostics check.Artifact)
            initialFindings
            validExceptions
            invalidExceptions

    let validate (artifact: RetainedInspectionArtifact) (rules: RetainedInspectionRule list) (exceptions: IntentionalDamageException list) : RetainedInspectionValidationResult =
        validateCheck
            { Artifact = artifact
              Rules = rules
              Exceptions = exceptions
              ExpectedAffectedRegionIds = artifact.Transition |> Option.map _.ExpectedAffectedRegionIds |> Option.defaultValue []
              PreviousArtifact = None
              EnvironmentLimitations = [] }

module RetainedInspectionReadiness =
    let private statusRank (status: RetainedInspectionStatus) =
        match status with
        | RetainedInspectionStatus.Blocked -> 0
        | RetainedInspectionStatus.Unsupported -> 1
        | RetainedInspectionStatus.EnvironmentLimited -> 2
        | RetainedInspectionStatus.ReviewRequired -> 3
        | RetainedInspectionStatus.NotRun -> 4
        | RetainedInspectionStatus.NotInspected -> 5
        | RetainedInspectionStatus.Accepted -> 6

    let private worstStatus statuses =
        statuses
        |> List.sortBy statusRank
        |> List.tryHead
        |> Option.defaultValue RetainedInspectionStatus.Accepted

    let private countBy (values: string list) =
        values |> List.countBy id |> List.sortBy fst

    let aggregate
        (runId: string)
        (artifacts: RetainedInspectionArtifact list)
        (results: RetainedInspectionValidationResult list)
        (relatedVisualEvidence: string list)
        (commandEvidence: (string * string) list)
        (caveats: string list)
        : RetainedInspectionSummary =
        let resultByArtifact = results |> List.map (fun result -> result.ArtifactId, result) |> Map.ofList
        let statuses =
            artifacts
            |> List.map (fun artifact ->
                resultByArtifact
                |> Map.tryFind artifact.ArtifactId
                |> Option.map _.ReadinessStatus
                |> Option.defaultValue artifact.ReadinessStatus)

        let findings = results |> List.collect _.Findings

        { RunId = runId
          OverallStatus = worstStatus statuses
          ArtifactCount = artifacts.Length
          InspectedScopes =
            artifacts
            |> List.filter (fun a -> a.ReadinessStatus <> RetainedInspectionStatus.NotInspected && a.ReadinessStatus <> RetainedInspectionStatus.NotRun)
            |> List.map _.Scope.ScopeId
            |> List.sort
          NotInspectedScopes =
            artifacts
            |> List.filter (fun a -> a.ReadinessStatus = RetainedInspectionStatus.NotInspected || a.ReadinessStatus = RetainedInspectionStatus.NotRun)
            |> List.map _.Scope.ScopeId
            |> List.sort
          StatusCounts = statuses |> List.map RetainedInspection.statusText |> countBy
          DamageStatusCounts =
            artifacts
            |> List.choose _.Damage
            |> List.map (fun damage -> RetainedInspection.damageStatusText damage.DamageStatus)
            |> countBy
          NodeStatusCounts =
            artifacts
            |> List.collect _.RetainedNodes
            |> List.map (fun node -> RetainedInspection.nodeStatusText node.Status)
            |> countBy
          DirtyAreaSummaries =
            artifacts
            |> List.choose _.Damage
            |> List.map (fun damage -> damage.TransitionId, damage.DirtyPercentage, damage.AffectedRegionIds)
            |> List.sortBy (fun (transitionId, _, _) -> transitionId)
          BlockingFindings = findings |> List.filter (fun finding -> finding.Severity = VisualInspectionSeverity.Blocking)
          UnsupportedFacts = artifacts |> List.collect _.UnsupportedFacts
          AcceptedExceptions = results |> List.collect _.AppliedExceptions |> List.distinct |> List.sort
          InvalidExceptions = results |> List.collect _.InvalidExceptions |> List.distinct |> List.sort
          RelatedVisualEvidence = (relatedVisualEvidence @ (artifacts |> List.collect _.RelatedVisualEvidence)) |> List.distinct |> List.sort
          CommandEvidence = commandEvidence |> List.distinct |> List.sortBy fst
          Caveats = caveats
          Diagnostics = results |> List.collect _.Diagnostics |> List.distinct }

module RetainedInspectionMarkdown =
    open ReadinessFormatting

    let startMarker = "<!-- FS.GG RETAINED INSPECTION START -->"
    let endMarker = "<!-- FS.GG RETAINED INSPECTION END -->"

    let renderSummary (summary: RetainedInspectionSummary) =
        let sb = StringBuilder()
        let line (text: string) = sb.AppendLine(text) |> ignore

        line "## Retained Inspection"
        line ""
        line $"- run: `{summary.RunId}`"
        line $"- status: **{RetainedInspection.statusText summary.OverallStatus}**"
        line $"- artifacts: `{summary.ArtifactCount}`"
        let inspectedScopesText = String.concat ", " summary.InspectedScopes
        line $"- inspected scopes: `{inspectedScopesText}`"
        line $"- status counts: `{countsText summary.StatusCounts}`"
        line $"- node counts: `{countsText summary.NodeStatusCounts}`"
        line $"- damage counts: `{countsText summary.DamageStatusCounts}`"

        if not summary.DirtyAreaSummaries.IsEmpty then
            line ""
            line "### Dirty Area"
            line "| transition | dirty area | affected regions |"
            line "|---|---:|---|"
            for transitionId, percentage, regions in summary.DirtyAreaSummaries do
                let percentageText = percentage.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                let regionText = String.concat ", " regions
                line ("| `" + transitionId + "` | " + percentageText + "% | `" + regionText + "` |")

        if not summary.BlockingFindings.IsEmpty then
            line ""
            line "### Blocking Findings"
            line "| finding | rule | affected | message |"
            line "|---|---|---|---|"
            for finding in summary.BlockingFindings do
                let affected = String.concat ", " (finding.AffectedRegionIds @ finding.AffectedNodeIds)
                line $"| `{finding.FindingId}` | `{finding.RuleId}` | `{affected}` | {finding.Message} |"

        if not summary.UnsupportedFacts.IsEmpty then
            line ""
            line "### Unsupported Facts"
            for fact in summary.UnsupportedFacts do
                let owner = fact.OwnerId |> Option.defaultValue "scope"
                line $"- `{fact.Fact}` on `{owner}`: {fact.Reason}"

        if not summary.CommandEvidence.IsEmpty then
            line ""
            line "### Command Evidence"
            for name, value in summary.CommandEvidence do
                line $"- `{name}`: {value}"

        if not summary.Caveats.IsEmpty then
            line ""
            line "### Caveats"
            for caveat in summary.Caveats do
                line $"- {caveat}"

        if not summary.Diagnostics.IsEmpty then
            line ""
            line "### Diagnostics"
            for diagnostic in summary.Diagnostics do
                line $"- {diagnostic}"

        sb.ToString()

    let renderJson (summary: RetainedInspectionSummary) =
        let dirtyJson =
            summary.DirtyAreaSummaries
            |> List.map (fun (transitionId, percentage, regions) ->
                let percentageText = percentage.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                $"    {{ \"transitionId\": {q transitionId}, \"dirtyPercentage\": {percentageText}, \"affectedRegionIds\": {jsonStringArray regions} }}")
            |> String.concat ",\n"

        let findingJson =
            summary.BlockingFindings
            |> List.map (fun finding ->
                let affected = finding.AffectedRegionIds @ finding.AffectedNodeIds
                $"    {{ \"findingId\": {q finding.FindingId}, \"ruleId\": {q finding.RuleId}, \"severity\": {q (VisualInspection.severityText finding.Severity)}, \"affectedIds\": {jsonStringArray affected}, \"message\": {q finding.Message} }}")
            |> String.concat ",\n"

        String.concat
            "\n"
            [ "{"
              $"  \"runId\": {q summary.RunId},"
              $"  \"overallStatus\": {q (RetainedInspection.statusText summary.OverallStatus)},"
              $"  \"artifactCount\": {summary.ArtifactCount},"
              $"  \"inspectedScopes\": {jsonStringArray summary.InspectedScopes},"
              $"  \"notInspectedScopes\": {jsonStringArray summary.NotInspectedScopes},"
              "  \"statusCounts\": {"
              jsonCounts summary.StatusCounts
              "  },"
              "  \"damageStatusCounts\": {"
              jsonCounts summary.DamageStatusCounts
              "  },"
              "  \"nodeStatusCounts\": {"
              jsonCounts summary.NodeStatusCounts
              "  },"
              "  \"dirtyAreaSummaries\": ["
              dirtyJson
              "  ],"
              "  \"blockingFindings\": ["
              findingJson
              "  ],"
              $"  \"acceptedExceptions\": {jsonStringArray summary.AcceptedExceptions},"
              $"  \"invalidExceptions\": {jsonStringArray summary.InvalidExceptions},"
              $"  \"relatedVisualEvidence\": {jsonStringArray summary.RelatedVisualEvidence},"
              $"  \"caveats\": {jsonStringArray summary.Caveats},"
              $"  \"diagnostics\": {jsonStringArray summary.Diagnostics}"
              "}" ]
        + "\n"

    // Feature 186 (US4): delegate to the one shared managed-section updater; byte-identical to the
    // former per-writer copy (FR-006/FR-011).
    let updateManagedSection (existingText: string) (generatedMarkdown: string) =
        SharedTesting.updateManagedSection
            startMarker
            endMarker
            "retained inspection managed markers are reversed"
            "retained inspection managed section must contain exactly one start marker and one end marker"
            (fun text safe inserted diagnostics ->
                { UpdatedText = text
                  SafeToWrite = safe
                  InsertedMarkers = inserted
                  Diagnostics = diagnostics })
            existingText
            generatedMarkdown

