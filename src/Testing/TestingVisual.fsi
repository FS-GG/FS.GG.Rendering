namespace FS.GG.UI.Testing

open System
open FS.GG.UI.Scene

/// Shared visual-readiness target matrix helpers.
module VisualCaptureMatrix =
    /// Build a stable target id from page/theme/size/path facts.
    val targetId: page: VisualPage -> theme: VisualTheme -> size: VisualSize -> relativePath: string -> string
    /// Expand pages x themes x sizes into deterministic visual capture targets.
    val expand:
        pages: VisualPage list ->
        themes: VisualTheme list ->
        sizes: VisualSize list ->
        pathFor: (VisualPage -> VisualTheme -> VisualSize -> string) ->
            Result<VisualCaptureTarget list, string list>

/// Shared visual screenshot completeness helpers.
module VisualCompleteness =
    /// Stable status token for readiness summaries.
    val statusText: status: VisualCaptureStatus -> string
    /// Build a degraded capture record with safe-failure diagnostics.
    val degraded: target: VisualCaptureTarget -> reason: string -> VisualCaptureRecord
    /// Validate required PNG artifacts below the evidence root and report stale extras.
    val validate: evidenceRoot: string -> targets: VisualCaptureTarget list -> VisualCaptureRecord list * string list

/// Shared reviewer-classification Markdown helpers.
module VisualReviewerClassifications =
    /// Stable severity token for readiness summaries.
    val severityText: severity: VisualReviewerSeverity -> string
    /// Generate a Markdown review table with one row per target.
    val writeTemplate: targets: VisualCaptureTarget list -> string
    /// Parse reviewer Markdown against the current target matrix.
    val parse: markdown: string -> targets: VisualCaptureTarget list -> VisualReviewerValidationResult

/// Shared visual-readiness aggregation helpers.
module VisualReadiness =
    /// Stable status token for readiness summaries.
    val statusText: status: VisualReadinessStatus -> string
    /// Aggregate captures, reviewer records, contact sheets, and caveats into readiness.
    val evaluate:
        runId: string ->
        evidenceRoot: string ->
        targets: VisualCaptureTarget list ->
        captures: VisualCaptureRecord list ->
        reviewerClassifications: VisualReviewerClassification list ->
        contactSheets: VisualContactSheet list ->
        caveats: string list ->
        acceptedExceptions: string list ->
            VisualReadinessReport

/// Shared readiness Markdown/JSON formatting helpers (internal; consumed across Testing domain files).
module internal ReadinessFormatting =
    val esc: text: string -> string
    val q: text: string -> string
    val jsonStringArray: values: string list -> string
    val jsonCounts: values: (string * 'a) list -> string
    val countsText: values: ('a * 'b) list -> string

/// Shared visual-readiness Markdown, JSON, and managed-section helpers.
module VisualReadinessMarkdown =
    /// Managed-section start marker used in human summaries.
    val startMarker: string
    /// Managed-section end marker used in human summaries.
    val endMarker: string
    /// Render a human-readable generated Markdown section.
    val renderSummary: report: VisualReadinessReport -> string
    /// Render a deterministic machine-readable JSON report.
    val renderJson: report: VisualReadinessReport -> string
    /// Update or insert exactly one generated section while preserving manual text.
    val updateManagedSection: existingText: string -> generatedMarkdown: string -> VisualSummarySectionUpdate

/// Structured visual inspection rule vocabulary and validators.
module VisualInspectionValidation =
    /// Create a required validation rule by stable rule id.
    val rule: ruleId: string -> VisualInspectionRule
    /// The initial deterministic visual inspection rule set.
    val defaultRules: VisualInspectionRule list
    /// Validate an artifact with explicit rules, exceptions, expected regions, and optional previous artifact.
    val validateCheck: check: VisualInspectionValidationCheck -> VisualInspectionValidationResult
    /// Validate an artifact with the default check shape.
    val validate:
        artifact: VisualInspectionArtifact ->
        rules: VisualInspectionRule list ->
        exceptions: VisualInspectionException list ->
            VisualInspectionValidationResult

/// Readiness aggregation for one or more visual inspection validation results.
module VisualInspectionReadiness =
    /// Aggregate artifacts and validation results into a reviewer- and machine-readable summary.
    val aggregate:
        runId: string ->
        artifacts: VisualInspectionArtifact list ->
        results: VisualInspectionValidationResult list ->
        relatedVisualEvidence: string list ->
        caveats: string list ->
            VisualInspectionSummary

/// Markdown, JSON, and managed-section helpers for visual inspection evidence.
module VisualInspectionMarkdown =
    /// Managed-section start marker used in human inspection summaries.
    val startMarker: string
    /// Managed-section end marker used in human inspection summaries.
    val endMarker: string
    /// Render a human-readable generated Markdown inspection section.
    val renderSummary: summary: VisualInspectionSummary -> string
    /// Render deterministic machine-readable JSON for an inspection summary.
    val renderJson: summary: VisualInspectionSummary -> string
    /// Update or insert exactly one generated inspection section while preserving manual text.
    val updateManagedSection: existingText: string -> generatedMarkdown: string -> VisualInspectionSummarySectionUpdate

