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

val create:
    findingId: string ->
    targetIds: string list ->
    category: FindingCategory ->
    severity: Severity ->
    description: string ->
    expected: string ->
    actual: string ->
        VisualFinding
val markFixed: fixReference: string -> finding: VisualFinding -> VisualFinding
val markReviewed: reviewedAt: string -> finding: VisualFinding -> VisualFinding
val close: finding: VisualFinding -> VisualFinding
val unresolved: findings: VisualFinding list -> VisualFinding list
val unresolvedCount: findings: VisualFinding list -> int
val validate: requiredTargetIds: string list -> findings: VisualFinding list -> ValidationResult
val isAccepted: requiredTargetIds: string list -> findings: VisualFinding list -> bool
val statusName: status: FindingStatus -> string
val severityName: severity: Severity -> string
val categoryName: category: FindingCategory -> string
val toMarkdown: findings: VisualFinding list -> string
val emptyLedger: string
