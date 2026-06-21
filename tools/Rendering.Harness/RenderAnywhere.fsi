namespace Rendering.Harness

open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

module RenderAnywhere =

    type CorpusItem =
        { ScenarioId: string
          Scene: Scene
          Package: PortableScenePackage }

    type BrowserCandidateVerdict =
        | CandidatePassed
        | CandidateFailed
        | CandidateUnsupportedCapability
        | CandidateMissingResource
        | CandidateEnvironmentLimited

    type BrowserFinalDecision =
        | AcceptedCandidatePath of string
        | DocumentedFallbackPath of string

    type BrowserComparison =
        { ScenarioId: string
          PackageIdentity: string
          ReferenceIdentity: string option
          CandidateIdentity: string option
          Tolerance: float
          DiffMetric: float option
          Verdict: BrowserCandidateVerdict
          Diagnostics: string list }

    type BrowserFeasibilityReport =
        { CandidateBackend: string
          Corpus: string list
          Tolerance: float
          Comparisons: BrowserComparison list
          UnsupportedCapabilities: string list
          Decision: BrowserFinalDecision
          Diagnostics: string list }

    type BrowserFeasibilityModel =
        { OutputDirectory: string
          CandidateBackend: string
          Corpus: CorpusItem list
          ReferenceEvidence: ReferenceRenderingEvidence list
          Report: BrowserFeasibilityReport option
          Diagnostics: string list }

    type BrowserFeasibilityMsg =
        | BrowserStart
        | ReferencesLoaded of ReferenceRenderingEvidence list
        | CandidateCompared of BrowserFeasibilityReport
        | BrowserFallbackSelected of string

    type BrowserFeasibilityEffect =
        | LoadReferenceEvidence of string
        | CompareBrowserCandidate of CorpusItem list * ReferenceRenderingEvidence list * string
        | WriteBrowserReport of BrowserFeasibilityReport * string

    val featureDirectory: string
    val readinessDirectory: string
    val roundTripDirectory: string
    val referenceDirectory: string
    val browserDirectory: string

    val corpus: unit -> CorpusItem list
    val formatReferenceEvidence: evidence: ReferenceRenderingEvidence list -> string list
    val runReferenceCommand: outputDirectory: string -> ReferenceRenderingEvidence list
    val initBrowserFeasibility: outputDirectory: string -> BrowserFeasibilityModel * BrowserFeasibilityEffect list
    val updateBrowserFeasibility: msg: BrowserFeasibilityMsg -> model: BrowserFeasibilityModel -> BrowserFeasibilityModel * BrowserFeasibilityEffect list
    val buildBrowserFeasibilityReport: corpus: CorpusItem list -> references: ReferenceRenderingEvidence list -> candidateBackend: string -> BrowserFeasibilityReport
    val formatBrowserReport: report: BrowserFeasibilityReport -> string list
    val writeBrowserReport: outputDirectory: string -> report: BrowserFeasibilityReport -> string
    val runBrowserFeasibilityCommand: outputDirectory: string -> BrowserFeasibilityReport
