namespace FS.GG.UI.Testing

open System
open FS.GG.UI.Scene

/// Public contract module exposed by this FS.GG.UI package.
module CompositorReadiness =
    /// Public contract function exposed by this FS.GG.UI package.
    val statusText: status: CompositorReadinessStatus -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: report: CompositorReadinessReport -> CompositorReadinessValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module CompositorTimingAssertions =
    /// Public contract function exposed by this FS.GG.UI package.
    val verdictText: verdict: CompositorTimingVerdict -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val validateSummary: check: CompositorTimingSummaryCheck -> CompositorTimingSummaryValidationResult

/// Feature 157 damage-scoped readiness helper.
module CompositorDamageReadiness =
    /// Feature 157: stable status token for readiness summaries.
    val statusText: status: CompositorDamageReadinessStatus -> string
    /// Feature 157: validate accepted, fallback-only, rejected, and environment-limited damage packages.
    val validate: check: CompositorDamageReadinessCheck -> CompositorDamageReadinessValidationResult

