module SecondAntShowcase.Core.ReviewFindings

type FindingCategory =
    | Palette
    | Spacing
    | Typography
    | Contrast
    | Clipping
    | Overlap
    | Alignment
    | State
    | AntConformance
    | StaleState

type Severity =
    | Info
    | Warning
    | Blocking

type FindingStatus =
    | Open
    | Fixed
    | Reviewed
    | Closed

type VisualFinding =
    { FindingId: string
      TargetIds: string list
      Category: FindingCategory
      Severity: Severity
      Status: FindingStatus
      Description: string
      Expected: string
      Actual: string
      FixReference: string option
      ReviewedAt: string option }

type ValidationResult =
    { MalformedFindingIds: string list
      MissingClassificationTargetIds: string list
      UnresolvedFindingIds: string list }

let create findingId targetIds category severity description expected actual =
    { FindingId = findingId
      TargetIds = targetIds
      Category = category
      Severity = severity
      Status = Open
      Description = description
      Expected = expected
      Actual = actual
      FixReference = None
      ReviewedAt = None }

let markFixed fixReference finding =
    { finding with Status = Fixed; FixReference = Some fixReference }

let markReviewed reviewedAt finding =
    { finding with Status = Reviewed; ReviewedAt = Some reviewedAt }

let close finding =
    match finding.Status, finding.ReviewedAt with
    | Reviewed, Some _ -> { finding with Status = Closed }
    | Closed, _ -> finding
    | _ -> finding

let unresolved findings =
    findings
    |> List.filter (fun finding ->
        match finding.Status with
        | Open
        | Fixed -> true
        | Reviewed -> finding.Severity = Blocking
        | Closed -> false)

let unresolvedCount findings = unresolved findings |> List.length

let validate requiredTargetIds findings =
    let required = Set.ofList requiredTargetIds
    let classified =
        findings
        |> List.collect _.TargetIds
        |> Set.ofList
    let malformed =
        findings
        |> List.choose (fun finding ->
            if System.String.IsNullOrWhiteSpace finding.FindingId || List.isEmpty finding.TargetIds then Some finding.FindingId
            else None)
    let missing =
        required
        |> Set.filter (fun target -> not (classified.Contains target))
        |> Set.toList
    { MalformedFindingIds = malformed
      MissingClassificationTargetIds = missing
      UnresolvedFindingIds = unresolved findings |> List.map _.FindingId }

let isAccepted requiredTargetIds findings =
    let result = validate requiredTargetIds findings
    List.isEmpty result.MalformedFindingIds
    && List.isEmpty result.MissingClassificationTargetIds
    && List.isEmpty result.UnresolvedFindingIds

let statusName status =
    match status with
    | Open -> "open"
    | Fixed -> "fixed"
    | Reviewed -> "reviewed"
    | Closed -> "closed"

let severityName severity =
    match severity with
    | Info -> "info"
    | Warning -> "warning"
    | Blocking -> "blocking"

let categoryName category =
    match category with
    | Palette -> "palette"
    | Spacing -> "spacing"
    | Typography -> "typography"
    | Contrast -> "contrast"
    | Clipping -> "clipping"
    | Overlap -> "overlap"
    | Alignment -> "alignment"
    | State -> "state"
    | AntConformance -> "ant-conformance"
    | StaleState -> "stale-state"

let private escapeCell (text: string) = text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ")

let toMarkdown findings =
    [ "# Visual Findings"
      ""
      sprintf "- unresolved findings: %d" (unresolvedCount findings)
      ""
      "| Finding | Targets | Category | Severity | Status | Description | Expected | Actual | Fix | Reviewed |"
      "|---|---|---|---|---|---|---|---|---|---|"
      for finding in findings do
          sprintf
              "| `%s` | %s | %s | %s | %s | %s | %s | %s | %s | %s |"
              finding.FindingId
              (finding.TargetIds |> String.concat ", ")
              (categoryName finding.Category)
              (severityName finding.Severity)
              (statusName finding.Status)
              (escapeCell finding.Description)
              (escapeCell finding.Expected)
              (escapeCell finding.Actual)
              (finding.FixReference |> Option.defaultValue "")
              (finding.ReviewedAt |> Option.defaultValue "") ]
    |> String.concat System.Environment.NewLine

let emptyLedger =
    [ "# Visual Findings"
      ""
      "- unresolved findings: 0"
      ""
      "No live reviewer findings have been recorded for this run."
      "Environment-limited screenshot records remain a visual-readiness limitation, not accepted live visual evidence." ]
    |> String.concat System.Environment.NewLine
