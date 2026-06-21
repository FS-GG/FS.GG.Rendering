namespace FS.GG.UI.Testing

open System
open FS.GG.UI.Scene

/// Public contract module exposed by this FS.GG.UI package.
module GeneratedProductAssertions =
    /// Public contract function exposed by this FS.GG.UI package.
    val summarize: expectation: GeneratedProductExpectation -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val validateDefaultInteractiveLaunch: source: string -> GeneratedProductLaunchValidationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val validateWindowDiagnostics: check: GeneratedWindowDiagnosticCheck -> GeneratedWindowDiagnosticValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module LocalConsumerPackages =
    /// Public contract function exposed by this FS.GG.UI package.
    val report: feedPath: string -> packages: LocalConsumerPackage list -> LocalConsumerPackageReport
    /// Public contract function exposed by this FS.GG.UI package.
    val classifyDrift: expected: LocalConsumerPackage list -> actual: LocalConsumerPackage list -> LocalConsumerPackageDrift list

/// Public contract module exposed by this FS.GG.UI package.
module GeneratedConsumerValidation =
    /// Public contract function exposed by this FS.GG.UI package.
    val summarize: result: GeneratedValidationResult -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val verifyPackageResolution: check: PackageResolutionCheck -> PackageResolutionCheckResult
    /// Public contract function exposed by this FS.GG.UI package.
    val verifyGeneratedTests: check: GeneratedTestExecutionCheck -> GeneratedTestExecutionResult
    /// Public contract function exposed by this FS.GG.UI package.
    val selectVisualEvidence: request: VisualEvidenceRequest -> VisualEvidenceResult
    /// Public contract function exposed by this FS.GG.UI package.
    val validateVisualEvidenceCommandOutput: check: GeneratedVisualEvidenceCommandCheck -> GeneratedVisualEvidenceCommandResult
    /// Public contract function exposed by this FS.GG.UI package.
    val buildValidationContractOutput: check: GeneratedValidationContractCheck -> GeneratedValidationContractResult

/// Public contract module exposed by this FS.GG.UI package.
module GeneratedLayoutValidation =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: GeneratedLayoutValidationCheck -> GeneratedLayoutValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module HostWarningClassification =
    /// Public contract function exposed by this FS.GG.UI package.
    val classify: check: HostWarningClassificationCheck -> HostWarningClassificationResult

/// Public contract module exposed by this FS.GG.UI package.
module PersistentLaunchArtifactValidation =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: PersistentLaunchArtifactCheck -> PersistentLaunchArtifactValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module ReadinessFileDiscovery =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: ReadinessFileDiscoveryCheck -> ReadinessFileDiscoveryResult

/// Public contract module exposed by this FS.GG.UI package.
module RuntimeDiagnosticReadiness =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: RuntimeDiagnosticReadinessCheck -> RuntimeDiagnosticReadinessResult

/// Public contract module exposed by this FS.GG.UI package.
module DefaultTextGlyphEvidence =
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: check: DefaultTextGlyphEvidenceCheck -> DefaultTextGlyphEvidenceValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module EvidenceReports =
    /// Public contract function exposed by this FS.GG.UI package.
    val statusText: status: EvidenceReportStatus -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val field: name: string -> value: string -> EvidenceReportField
    /// Public contract function exposed by this FS.GG.UI package.
    val build: request: EvidenceReportRequest -> EvidenceReport
    /// Public contract function exposed by this FS.GG.UI package.
    val write: request: EvidenceReportRequest -> EvidenceReport
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: report: EvidenceReport -> EvidenceReportValidationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val parseScreenshotEvidenceRecord: lines: string list -> ScreenshotEvidenceRecord
    /// Public contract function exposed by this FS.GG.UI package.
    val validateScreenshotArtifact: check: ScreenshotArtifactValidationCheck -> ScreenshotArtifactValidationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val validateScreenshotEvidence: check: ScreenshotEvidenceReportCheck -> ScreenshotEvidenceReportValidationResult

/// Public contract module exposed by this FS.GG.UI package.
module LayoutReadiness =
    /// Public contract function exposed by this FS.GG.UI package.
    val statusText: status: LayoutReadinessStatus -> string
    /// Public contract function exposed by this FS.GG.UI package.
    val validate: report: LayoutReadinessReport -> LayoutReadinessValidationResult

