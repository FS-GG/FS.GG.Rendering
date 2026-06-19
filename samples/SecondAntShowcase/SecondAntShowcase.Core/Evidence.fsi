module SecondAntShowcase.Core.Evidence

open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer

type ScreenshotSummary =
    { ProvesScreenshot: bool
      BlockedStage: string option
      UnsupportedHostReason: string option
      Fallback: string option
      Path: string option }

type PageEvidenceRecord =
    { PageId: string
      Seed: int
      Mode: string
      ControlIds: string list
      ProofLevel: string
      AuthoritativeFor: string list
      NotAuthoritativeFor: string list
      Screenshot: ScreenshotSummary }

type DatePickerOverlayEvidence =
    { ScenarioId: string
      InputStep: string
      ExpectedOverlayState: string
      TopmostHitTarget: string
      FocusState: string
      DispatchSummary: string
      BehavioralEvidenceReference: string
      ReplayLog: string list
      FocusTransitions: (string option * string option) list
      ProductMessages: string list
      Diagnostics: string list
      NoStaleOverlay: bool }

type VisualScreenshotRecord =
    { PageId: string
      ThemeId: string
      Width: int
      Height: int
      RelativePath: string
      CaptureSource: string
      Completeness: string
      DegradedReason: string option }

type VisualReadinessSummary =
    { Seed: int
      Size: string
      AcceptedSizeRole: string
      PageIds: string list
      ThemeIds: string list
      RequiredScreenshotCount: int
      PresentScreenshotCount: int
      CompletenessStatus: string
      CaptureAvailability: string
      ReviewerDefectStatus: string
      VisualReadinessStatus: string
      Screenshots: VisualScreenshotRecord list
      ContactSheets: string list
      Limitations: string list }

type RetainedInspectionEvidenceRecord =
    { PageId: string
      ThemeId: string
      SizeRole: string
      Width: int
      Height: int
      RetainedStatus: string
      DirtyAreaPercentage: float
      RepaintedNodeCount: int
      ShiftedNodeCount: int
      AffectedRegionIds: string list
      ScreenshotPreferredTargetCount: int
      ScreenshotMinimumTargetCount: int
      ReviewerSummary: string }

type ResponsivenessReviewSession =
    { RunId: string
      StartedUtc: System.DateTimeOffset
      CompletedUtc: System.DateTimeOffset option
      Theme: string
      Scope: string
      DesktopSessionStatus: string
      ArtifactsRoot: string
      EnvironmentLimitations: string list }

type ResponsivenessReviewAction =
    { ActionId: string
      PageId: string
      ControlFamily: string
      ControlIds: string list
      ActionType: string
      InputKind: string
      ExpectedVisibleResult: string
      DisplayOnlyReason: string option }

type ResponsivenessEvidenceRecord =
    { Action: ResponsivenessReviewAction
      EnvironmentStatus: string
      VisibleResponse: string
      AcceptanceStatus: string
      Diagnostics: string list }

type ResponsivenessEvidenceSummary =
    { RunId: string
      Scope: string
      OverallReadiness: string
      RequiredInteractiveFamilies: string list
      AcceptedInteractiveFamilies: string list
      DisplayOnlyExclusions: ResponsivenessReviewAction list
      MissingInteractiveFamilies: string list
      EnvironmentLimitations: string list }

val datePickerReferenceOverlayEvidence: unit -> DatePickerOverlayEvidence
val ofScreenshotResult: result: ScreenshotEvidenceResult -> path: string option -> ScreenshotSummary
val degraded: reason: string -> ScreenshotSummary
val notAuthoritativeFor: string list
val build:
    pageId: string ->
    seed: int ->
    mode: string ->
    controlIds: string list ->
    metrics: FrameMetrics list ->
    shot: ScreenshotSummary ->
        PageEvidenceRecord
val goldenState: metrics: FrameMetrics list -> string
val toRunJson: r: PageEvidenceRecord -> string
val toSummaryMd: r: PageEvidenceRecord -> string
val retainedInspectionEvidence:
    pageId: string ->
    themeId: string ->
    sizeRole: string ->
    width: int ->
    height: int ->
        RetainedInspectionEvidenceRecord
val retainedInspectionToJson: evidence: RetainedInspectionEvidenceRecord -> string
val retainedInspectionToMarkdown: evidence: RetainedInspectionEvidenceRecord -> string
val visualSummaryToJson: summary: VisualReadinessSummary -> string
val visualSummaryToMarkdown: summary: VisualReadinessSummary -> string
val reviewerDefectTemplate: pageIds: string list -> themeIds: string list -> string
val responsivenessTargetP95Ms: int
val responsivenessTargetMaxMs: int
val responsivenessActionOfContract:
    contract: SecondAntShowcase.Core.InteractionContracts.InteractionContract ->
        ResponsivenessReviewAction
val responsivenessDisplayOnlyAction: controlId: string -> reason: string -> ResponsivenessReviewAction
val responsivenessSummaryMarkdown: summary: ResponsivenessEvidenceSummary -> string
