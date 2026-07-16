namespace Rendering.Harness

/// §7 golden-IMAGE equivalence gate for the god-module decomposition
/// (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md`, §7.1).
///
/// This is the deliberate COMPLEMENT of `RenderAnywhere`'s browser path. That path is a capability
/// report: no candidate backend runs, nothing is diffed, no tolerance is applied. This gate DOES
/// render a candidate — the in-process Skia CPU raster (`ReferenceRendering.renderScenePngResult`,
/// which uses `SKSurface.Create` over an `SKImageInfo`: no GPU/GL/X/display) — and compares it
/// PER-PIXEL against the committed reference PNG under an explicit perceptual tolerance. A change to
/// the render path (which is exactly what the decomposition of `SkiaViewer`/`Control`/`RetainedRender`
/// touches) that alters pixels beyond the tolerance is therefore caught.
///
/// Byte-identity is the norm in a fixed environment (verified: the corpus renders byte-for-byte to the
/// committed references here); the tolerance exists ONLY so a legitimate cross-environment Skia/font
/// drift does not red the gate for a visually-equivalent render, per §7.1's "perceptual tolerance, not
/// byte-equality".
///
/// Every non-match is fail-CLOSED and typed. A `None`/undecodable PNG, a dimension mismatch, or drift
/// beyond tolerance are all non-`Equivalent`. A host that cannot raster at all yields
/// `EnvironmentLimited` — a candidate was NEVER produced, so the gate proves nothing and must not be
/// read as a pass. A candidate that renders but fails to compare yields `RenderFailed`. Neither can
/// masquerade as `Equivalent`.
module GoldenImage =

    /// Per-pixel comparison metrics over two equally-sized decoded images.
    type ImageComparison =
        { Width: int
          Height: int
          TotalPixels: int
          /// Count of pixels whose worst channel (R/G/B/A) absolute delta exceeds `ChannelTolerance`.
          DiffPixelCount: int
          /// Worst single-channel absolute delta anywhere in the image (0..255).
          MaxChannelDelta: int }

    /// The perceptual budget. `ChannelTolerance` is the per-channel absolute delta under which a pixel
    /// counts as unchanged; `MaxDiffPixels` is how many still-changed pixels remain `Equivalent`.
    type GoldenTolerance =
        { ChannelTolerance: int
          MaxDiffPixels: int }

    /// The typed outcome of one comparison. ONLY `Equivalent` is a pass.
    type GoldenOutcome =
        | Equivalent of ImageComparison
        | Drifted of ImageComparison
        | DimensionMismatch of referenceWidth: int * referenceHeight: int * candidateWidth: int * candidateHeight: int
        | Undecodable of reason: string

    /// Whether a candidate image was produced for a scene, and if so how it compared.
    type CandidateStatus =
        | Rendered of GoldenOutcome
        /// The host cannot raster (Skia native unavailable): no candidate exists to compare.
        | EnvironmentLimited of reason: string
        /// A candidate was attempted but could not be produced or paired with a reference.
        | RenderFailed of reason: string

    /// One corpus scene's golden result.
    type SceneGolden =
        { ScenarioId: string
          Status: CandidateStatus
          Diagnostics: string list }

    /// Byte-exact budget (0 channel delta, 0 diff pixels) — the strictest gate.
    val exact: GoldenTolerance

    /// A small perceptual budget for cross-environment robustness (§7.1).
    val perceptual: GoldenTolerance

    /// Pure: decode both PNGs and compare under `tolerance`. Fail-closed on any decode/dimension
    /// problem. Does not touch the filesystem or render anything.
    val compareImages: tolerance: GoldenTolerance -> referencePng: byte[] -> candidatePng: byte[] -> GoldenOutcome

    /// Read the single committed reference PNG for a scenario under `<referenceDirectory>/<scenarioId>/`.
    val referencePng: referenceDirectory: string -> scenarioId: string -> Result<byte[], string>

    /// Render one corpus scene through the in-process CPU raster and compare it against its committed
    /// reference under `tolerance`. Never returns a false pass (see the module doc).
    val gateScene: tolerance: GoldenTolerance -> referenceDirectory: string -> item: RenderAnywhere.CorpusItem -> SceneGolden

    /// `gateScene` over the whole `RenderAnywhere.corpus ()`.
    val gateCorpus: tolerance: GoldenTolerance -> referenceDirectory: string -> SceneGolden list

    /// A human-facing report of a corpus gate run (one block per scene).
    val summarize: results: SceneGolden list -> string list
