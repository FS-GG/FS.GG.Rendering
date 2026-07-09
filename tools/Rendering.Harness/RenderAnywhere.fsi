namespace Rendering.Harness

open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

/// Feature 146 render-anywhere evidence.
///
/// The reference path is real: it rasterizes portable scene packages through Skia and writes PNGs.
/// The browser path is a CAPABILITY REPORT, not a comparison — no candidate backend executes here,
/// so no candidate image exists, nothing is diffed, and no tolerance is applied. The types below
/// deliberately cannot express `passed` or `accepted`: a green report means "the reference exists
/// and the candidate did not run", never "the backends agree".
module RenderAnywhere =

    type CorpusItem =
        { ScenarioId: string
          Scene: Scene
          Package: PortableScenePackage }

    /// The subset of `ReferenceRenderingEvidence` that survives a round trip through `summary.md`.
    type ReferenceSummaryEntry =
        { PackageIdentity: string
          Verdict: ReferenceRenderVerdict
          ImageIdentity: string option }

    /// Why the browser candidate produced no image. There is no `passed` case by construction.
    type CandidateCapabilityStatus =
        /// Reference evidence passed, but no candidate backend runs in this harness.
        | CandidateNotExecuted
        /// No passed reference evidence exists to cite for this scenario.
        | CandidateMissingReference

    /// Single case by construction: nothing here can accept a candidate path.
    type BrowserFinalDecision = DocumentedFallbackPath of string

    type ScenarioCapability =
        { ScenarioId: string
          PackageIdentity: string
          ReferenceIdentity: string option
          Status: CandidateCapabilityStatus
          Diagnostics: string list }

    type BrowserCapabilityReport =
        { CandidateBackend: string
          Corpus: string list
          Scenarios: ScenarioCapability list
          UnsupportedCapabilities: string list
          Decision: BrowserFinalDecision
          Diagnostics: string list }

    type BrowserFeasibilityModel =
        { OutputDirectory: string
          CandidateBackend: string
          Corpus: CorpusItem list
          ReferenceEvidence: ReferenceSummaryEntry list
          Report: BrowserCapabilityReport option
          Diagnostics: string list }

    type BrowserFeasibilityMsg =
        | BrowserStart
        | ReferencesLoaded of ReferenceSummaryEntry list
        | CapabilityAssessed of BrowserCapabilityReport
        | BrowserFallbackSelected of string

    type BrowserFeasibilityEffect =
        | LoadReferenceEvidence of string
        | AssessCandidateCapability of CorpusItem list * ReferenceSummaryEntry list * string
        | WriteBrowserReport of BrowserCapabilityReport * string

    val featureDirectory: string
    val readinessDirectory: string
    val roundTripDirectory: string
    val referenceDirectory: string
    val browserDirectory: string

    val corpus: unit -> CorpusItem list
    val formatReferenceEvidence: evidence: ReferenceRenderingEvidence list -> string list

    /// Project in-process reference evidence onto what a capability report may cite.
    val summaryEntries: evidence: ReferenceRenderingEvidence list -> ReferenceSummaryEntry list

    /// Read `<directory>/summary.md` back into reference entries; `[]` when absent or malformed.
    val readReferenceSummary: directory: string -> ReferenceSummaryEntry list

    val runReferenceCommand: outputDirectory: string -> ReferenceRenderingEvidence list
    val initBrowserFeasibility: outputDirectory: string -> BrowserFeasibilityModel * BrowserFeasibilityEffect list

    val updateBrowserFeasibility:
        msg: BrowserFeasibilityMsg -> model: BrowserFeasibilityModel -> BrowserFeasibilityModel * BrowserFeasibilityEffect list

    val buildBrowserCapabilityReport:
        corpus: CorpusItem list -> references: ReferenceSummaryEntry list -> candidateBackend: string -> BrowserCapabilityReport

    val formatBrowserReport: report: BrowserCapabilityReport -> string list
    val writeBrowserReport: outputDirectory: string -> report: BrowserCapabilityReport -> string
    val runBrowserCapabilityCommand: referenceDirectory: string -> outputDirectory: string -> BrowserCapabilityReport
