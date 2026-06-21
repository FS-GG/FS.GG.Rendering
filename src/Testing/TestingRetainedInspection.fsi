namespace FS.GG.UI.Testing

open System
open FS.GG.UI.Scene

/// Retained/damage inspection rule vocabulary and validators.
module RetainedInspectionValidation =
    /// Create a required validation rule by stable rule id.
    val rule: ruleId: string -> RetainedInspectionRule
    /// Deterministic retained/damage rule set.
    ///
    /// The default set checks required facts, dirty-region locality, broad or
    /// full-surface damage, expected affected regions, and exception hygiene.
    val defaultRules: RetainedInspectionRule list
    /// Validate an artifact with explicit rules, exceptions, expected regions, and optional previous artifact.
    val validateCheck: check: RetainedInspectionValidationCheck -> RetainedInspectionValidationResult
    /// Validate an artifact with the default check shape.
    ///
    /// This convenience entry point uses the artifact transition's expected
    /// affected regions and records exception diagnostics in the result.
    val validate:
        artifact: RetainedInspectionArtifact ->
        rules: RetainedInspectionRule list ->
        exceptions: IntentionalDamageException list ->
            RetainedInspectionValidationResult

/// Readiness aggregation for retained/damage inspection validation results.
module RetainedInspectionReadiness =
    /// Aggregate artifacts and validation results into a retained inspection summary.
    ///
    /// Command evidence and caveats are preserved in the summary so generated
    /// Markdown/JSON can be used directly in readiness reports.
    val aggregate:
        runId: string ->
        artifacts: RetainedInspectionArtifact list ->
        results: RetainedInspectionValidationResult list ->
        relatedVisualEvidence: string list ->
        commandEvidence: (string * string) list ->
        caveats: string list ->
            RetainedInspectionSummary

/// Markdown, JSON, and managed-section helpers for retained inspection evidence.
module RetainedInspectionMarkdown =
    /// Managed-section start marker used in human retained inspection summaries.
    val startMarker: string
    /// Managed-section end marker used in human retained inspection summaries.
    val endMarker: string
    /// Render a human-readable generated retained inspection section.
    val renderSummary: summary: RetainedInspectionSummary -> string
    /// Render deterministic machine-readable JSON for a retained inspection summary.
    val renderJson: summary: RetainedInspectionSummary -> string
    /// Update or insert exactly one generated retained inspection section while preserving manual text.
    val updateManagedSection: existingText: string -> generatedMarkdown: string -> RetainedInspectionSummarySectionUpdate

