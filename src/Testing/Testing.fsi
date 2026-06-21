namespace FS.GG.UI.Testing

open System
open FS.GG.UI.Scene

/// Feature 159 promotion/reuse readiness helper.
module Feature159Readiness =
    /// Feature 159: stable status token for readiness summaries.
    val statusText: status: Feature159ReadinessStatus -> string
    /// Feature 159: validate promotion/reuse readiness packages without accepting broader performance.
    val validate: check: Feature159ReadinessCheck -> Feature159ReadinessValidationResult

/// Feature 160 throughput-readiness helper.
module Feature160ThroughputReadiness =
    /// Feature 160: stable status token for readiness summaries.
    val statusText: status: Feature160ThroughputReadinessStatus -> string
    /// Feature 160: validate focused throughput packages while preserving the performance-claim boundary.
    val validate: check: Feature160ThroughputReadinessCheck -> Feature160ThroughputReadinessValidationResult

/// Feature 161 host-lane-readiness helper.
module Feature161HostLaneReadiness =
    /// Feature 161: stable status token for readiness summaries.
    val statusText: status: Feature161HostLaneReadinessStatus -> string
    /// Feature 161: validate host lane readiness packages without broadening performance claims across lanes.
    val validate: check: Feature161HostLaneReadinessCheck -> Feature161HostLaneReadinessValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module PackageInspectionAssertions =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: PackageInspectionAssertionCheck -> PackageInspectionAssertionResult
