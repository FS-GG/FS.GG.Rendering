namespace FS.GG.UI.Scene

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidenceFormat =
    | Hash
    | Png
    | Metadata

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidenceFailureClassification =
    | UnsupportedEnvironment
    | ProductDefect

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidenceFailure =
    { BlockedStage: string
      Classification: SceneEvidenceFailureClassification
      DiagnosticCategory: string
      Message: string }

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidenceRequest =
    { Scene: Scene
      OutputSize: Size
      Format: SceneEvidenceFormat
      RendererMode: string
      EvidencePath: string option }

/// Public contract type exposed by this FS.GG.UI package.
type SceneEvidence =
    { Format: SceneEvidenceFormat
      OutputSize: Size
      RendererMode: string
      EvidencePath: string option
      Value: string }

/// Public contract module exposed by this FS.GG.UI package.
module SceneEvidence =
    /// Feature 221 (US1, FR-001/FR-004): install (`Some`) or clear (`None`) the real headless PNG
    /// rasterizer used by `renderPng`. The rendering edge (`SkiaViewer`) injects a SkiaSharp CPU
    /// rasterizer so `renderPng` gains real pixels while `src/Scene` stays SkiaSharp-free — mirroring
    /// `Scene.setRealTextMeasurer`. The seam lives in this module (not `Scene`) because its signature
    /// references `SceneEvidenceFailure`, declared here. Default (`None`) ⇒ `renderPng` returns the
    /// typed `UnsupportedEnvironment` failure (FR-005), never a stub. Process-wide; the injected
    /// function MUST be re-entrant/thread-safe (concurrent `renderPng` calls — Edge Case: concurrency).
    val setRealPngRasterizer: rasterizer: (Size -> Scene -> Result<byte[], SceneEvidenceFailure>) option -> unit
    /// Public contract function exposed by this FS.GG.UI package.
    val render: request: SceneEvidenceRequest -> Result<SceneEvidence, SceneEvidenceFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val renderHash: size: Size -> scene: Scene -> Result<SceneEvidence, SceneEvidenceFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val renderPng: size: Size -> scene: Scene -> Result<byte[], SceneEvidenceFailure>

/// Public contract module exposed by this FS.GG.UI package.
module LayoutEvidence =
    /// Public contract function exposed by this FS.GG.UI package.
    val classify: report: LayoutEvidenceReport -> LayoutEvidenceReport
    /// Public contract function exposed by this FS.GG.UI package.
    val fromRenderEvidence: scene: Scene -> evidence: RenderReadbackEvidence -> LayoutEvidenceReport
    /// Public contract function exposed by this FS.GG.UI package.
    val unsupported: scene: Scene -> outputSize: Size -> reason: LayoutUnsupportedReason -> LayoutEvidenceReport
