namespace FS.GG.UI.Scene

/// Durable protocol version for portable scene packages.
type ProtocolVersion =
    { Major: int
      Minor: int }

/// Whether a package requirement is mandatory or degradable.
type RequirementLevel =
    | Required
    | Optional

/// Consumer action when an optional requirement is unsupported.
type DegradationPolicy =
    | Reject
    | Degrade
    | Ignore

/// Stable capability requirement encoded in a portable scene package.
type CapabilityRequirement =
    { CapabilityId: string
      RequirementLevel: RequirementLevel
      DegradationPolicy: DegradationPolicy
      AffectedScenePaths: string list }

/// Portable resource kind.
type ResourceKind =
    | ImageResource
    | FontResource
    | BinaryResource of string

/// Resource manifest entry. `SourceLabel` is diagnostic-only and never the package identity.
type ResourceEntry =
    { ResourceId: string
      Kind: ResourceKind
      ContentHash: string
      ByteLength: int64 option
      Required: bool
      MediaType: string option
      SourceLabel: string option }

/// Consumer-observed resource state used during inspection.
type ResourceAvailabilityStatus =
    | ResourceAvailable
    | ResourceMissing
    | ResourceCorrupted
    | ResourceHashMismatch
    | ResourceDuplicated

/// Consumer-observed resource metadata used during inspection.
type ResourceAvailability =
    { ResourceId: string
      Kind: ResourceKind option
      ContentHash: string option
      ByteLength: int64 option
      Status: ResourceAvailabilityStatus }

/// Target capability profile used by package inspection.
type TargetCapabilityProfile =
    { ProfileId: string
      SupportedCapabilities: string list }

/// Export options for a portable scene package.
type PackageExportOptions =
    { ProfileId: string
      Resources: ResourceEntry list
      OptionalCapabilities: string list }

/// Inspection options for a portable scene package.
type PackageInspectionOptions =
    { TargetProfile: TargetCapabilityProfile option
      Resources: ResourceAvailability list }

/// Package diagnostic stage.
type PackageDiagnosticStage =
    | Parse
    | Version
    | Capability
    | Resource
    | ScenePayload
    | TextPayload

/// Diagnostic emitted by export, import, inspection, or comparison.
type PackageDiagnostic =
    { Severity: DiagnosticSeverity
      Stage: PackageDiagnosticStage
      Message: string
      ScenePath: string option
      CapabilityId: string option
      ResourceId: string option }

/// Package inspection status.
type PackageInspectionStatus =
    | PackageAccepted
    | PackageAcceptedWithDegradation
    | PackageRejected

/// Per-capability inspection verdict.
type CapabilityVerdict =
    { Requirement: CapabilityRequirement
      Supported: bool
      Degraded: bool
      Diagnostics: PackageDiagnostic list }

/// Per-resource inspection verdict.
type ResourceVerdict =
    { Entry: ResourceEntry
      Availability: ResourceAvailability option
      Accepted: bool
      Degraded: bool
      Diagnostics: PackageDiagnostic list }

/// Parsed portable scene package.
type PortableScenePackage =
    { Version: ProtocolVersion
      ProfileId: string
      Capabilities: CapabilityRequirement list
      Resources: ResourceEntry list
      Scene: Scene
      CanonicalBytes: byte[]
      PackageIdentity: string
      Diagnostics: PackageDiagnostic list }

/// Inspection report for a portable scene package.
type PackageInspectionReport =
    { Status: PackageInspectionStatus
      PackageIdentity: string option
      Version: ProtocolVersion option
      ProfileId: string option
      CapabilityVerdicts: CapabilityVerdict list
      ResourceVerdicts: ResourceVerdict list
      Diagnostics: PackageDiagnostic list }

/// Semantic comparison of two scenes.
type SemanticComparison =
    { Equivalent: bool
      ExpectedCapabilities: SceneElementKind list
      ActualCapabilities: SceneElementKind list
      Diagnostics: PackageDiagnostic list }

/// Public portable scene package functions.
module SceneCodec =
    /// Magic header used by all Feature 146 portable scene packages.
    val magicHeader: string

    /// Supported package protocol version.
    val supportedVersion: ProtocolVersion

    /// Default export options.
    val defaultExportOptions: PackageExportOptions

    /// Default inspection options.
    val defaultInspectionOptions: PackageInspectionOptions

    /// Export a scene into deterministic canonical package bytes and parsed package metadata.
    val exportScene: options: PackageExportOptions -> scene: Scene -> PortableScenePackage

    /// Export with default options.
    val export: scene: Scene -> PortableScenePackage

    /// Parse package bytes into the public package model.
    val importPackage: bytes: byte[] -> Result<PortableScenePackage, PackageDiagnostic list>

    /// Inspect package bytes with default target/resource options.
    val inspect: bytes: byte[] -> PackageInspectionReport

    /// Inspect package bytes with an explicit target profile and resource availability set.
    val inspectWith: options: PackageInspectionOptions -> bytes: byte[] -> PackageInspectionReport

    /// Compare two scenes semantically.
    val compareScenes: expected: Scene -> actual: Scene -> SemanticComparison

    /// Compute the canonical package identity.
    val packageIdentity: bytes: byte[] -> string

    /// Format diagnostics for readiness evidence.
    val formatDiagnostics: diagnostics: PackageDiagnostic list -> string list
