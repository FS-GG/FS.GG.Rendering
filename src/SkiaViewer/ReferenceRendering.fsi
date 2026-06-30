namespace FS.GG.UI.SkiaViewer

open FS.GG.UI.Scene

/// Verdict emitted by the Skia-backed portable package reference oracle.
type ReferenceRenderVerdict =
    | ReferencePassed
    | ReferenceFailed
    | ReferenceEnvironmentLimited

/// Classification for non-passed reference rendering evidence.
type ReferenceFailureClassification =
    | ReferenceProductDefect
    | ReferenceUnsupportedEnvironment
    | ReferencePackageResourceIncompatibility
    | ReferenceVerificationDepth

/// Request for rendering portable scene package bytes through the Skia reference path.
type ReferenceRenderingRequest =
    { PackageBytes: byte[]
      OutputDirectory: string
      OutputSize: Size
      Resources: ResourceAvailability list }

/// Metadata emitted by the reference rendering oracle.
type ReferenceRenderingEvidence =
    { PackageIdentity: string
      ProtocolVersion: ProtocolVersion option
      CapabilityProfile: string
      ResourceStatus: string
      OutputSize: Size
      ImagePath: string option
      ImageIdentity: string option
      RendererIdentity: string
      Verdict: ReferenceRenderVerdict
      Classification: ReferenceFailureClassification option
      Diagnostics: string list }

/// Pure workflow state for the reference rendering oracle.
type ReferenceRenderingModel =
    { Request: ReferenceRenderingRequest
      Inspection: PackageInspectionReport option
      Evidence: ReferenceRenderingEvidence option
      Diagnostics: string list }

/// Messages accepted by the pure reference rendering workflow.
type ReferenceRenderingMsg =
    | Start
    | PackageInspected of PackageInspectionReport
    | RenderCompleted of ReferenceRenderingEvidence
    | RenderFailed of ReferenceFailureClassification * string

/// Effects requested by the pure reference rendering workflow.
type ReferenceRenderingEffect =
    | InspectPackage of byte[]
    | RenderPackage of byte[] * Size * string * ResourceAvailability list
    | WriteReferenceEvidence of ReferenceRenderingEvidence * string

/// Skia-backed reference rendering workflow.
module ReferenceRendering =
    /// Feature 221 (US1, FR-001/FR-004): CPU-raster a scene description to a real, decodable PNG with
    /// no GPU/GL/X/display, mapping surface/native failures onto the typed `SceneEvidenceFailure`. This
    /// is the implementation injected into `SceneEvidence.setRealPngRasterizer` (re-entrant/thread-safe).
    val renderScenePngResult: outputSize: Size -> scene: Scene -> Result<byte[], SceneEvidenceFailure>

    /// Create the initial model and startup effects.
    val init: request: ReferenceRenderingRequest -> ReferenceRenderingModel * ReferenceRenderingEffect list

    /// Pure workflow transition.
    val update: msg: ReferenceRenderingMsg -> model: ReferenceRenderingModel -> ReferenceRenderingModel * ReferenceRenderingEffect list

    /// Run the Skia-backed render edge for a package.
    val renderPackage: request: ReferenceRenderingRequest -> ReferenceRenderingEvidence

    /// Write a Markdown evidence summary.
    val writeEvidenceSummary: outputDirectory: string -> evidence: ReferenceRenderingEvidence -> string

    /// Inspect, render, and write evidence summary in one edge helper.
    val run: request: ReferenceRenderingRequest -> ReferenceRenderingEvidence
