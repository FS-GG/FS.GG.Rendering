namespace FS.GG.UI.Scene

/// Dependency-light helpers for structured visual inspection evidence.
module VisualInspection =
    /// Stable lowercase token for an inspection readiness status.
    val statusText: status: VisualInspectionStatus -> string
    /// Stable lowercase token for a finding severity.
    val severityText: severity: VisualInspectionSeverity -> string
    /// Stable lowercase token for a measurement mode.
    val measurementModeText: mode: VisualInspectionMeasurementMode -> string
    /// Stable lowercase token for text fit status.
    val fitStatusText: status: VisualInspectionFitStatus -> string
    /// Stable lowercase token for a node kind.
    val nodeKindText: kind: VisualInspectionNodeKind -> string
    /// Stable lowercase token for paint role.
    val paintRoleText: role: VisualInspectionPaintRole -> string
    /// Stable lowercase token for surface role.
    val surfaceRoleText: role: VisualInspectionSurfaceRole -> string
    /// Stable lowercase token for clipping status.
    val clipStatusText: status: VisualInspectionClipStatus -> string
    /// Stable lowercase token for paint coverage status.
    val coverageStatusText: status: VisualInspectionCoverageStatus -> string
    /// Create an explicit unsupported fact.
    val unsupportedFact:
        fact: string ->
        ownerId: string option ->
        required: bool ->
        reason: string ->
        diagnostic: string ->
        environmentLimited: bool ->
            VisualInspectionUnsupportedFact
    /// Build a stable finding id from a rule id and affected ids.
    val stableFindingId: ruleId: string -> affectedIds: string list -> string
    /// Create a deterministic finding with a generated stable id.
    val finding:
        ruleId: string ->
        severity: VisualInspectionSeverity ->
        affectedNodeIds: string list ->
        affectedRegionIds: string list ->
        message: string ->
        expected: string ->
        actual: string ->
            VisualInspectionFinding
    /// Validate artifact identity, ordering, and unsupported-fact disclosure.
    val artifactDiagnostics: artifact: VisualInspectionArtifact -> string list
    /// Sort nodes, regions, text runs, findings, and unsupported facts deterministically.
    val normalizeArtifact: artifact: VisualInspectionArtifact -> VisualInspectionArtifact

/// Dependency-light helpers for retained-render and damage-locality inspection.
module RetainedInspection =
    /// Stable lowercase token for a retained readiness status.
    val statusText: status: RetainedInspectionStatus -> string
    /// Stable lowercase token for a retained node status.
    val nodeStatusText: status: RetainedNodeStatus -> string
    /// Stable lowercase token for a damage status.
    val damageStatusText: status: DamageInspectionStatus -> string
    /// Create an explicit retained/damage unsupported fact.
    val unsupportedFact:
        fact: string ->
        ownerId: string option ->
        required: bool ->
        reason: string ->
        diagnostic: string ->
        environmentLimited: bool ->
            VisualInspectionUnsupportedFact
    /// Build a stable retained finding id from a rule, transition, and affected ids.
    val stableFindingId: ruleId: string -> transitionId: string -> affectedIds: string list -> string
    /// Create a deterministic retained/damage finding.
    val finding:
        ruleId: string ->
        severity: VisualInspectionSeverity ->
        transitionId: string ->
        affectedNodeIds: string list ->
        affectedRegionIds: string list ->
        message: string ->
        expected: string ->
        actual: string ->
            DamageLocalityFinding
    /// Compute the true visible union area of dirty rectangles clipped to a frame.
    val dirtyUnionArea: frameBounds: Rect -> dirtyRectangles: Rect list -> int
    /// Compute the bounding rectangle of clipped dirty rectangles.
    val dirtyUnionBounds: frameBounds: Rect -> dirtyRectangles: Rect list -> Rect option
    /// Build visible damage evidence from dirty rectangles and retained counters.
    ///
    /// The returned `DirtyPercentage` is computed from the true clipped dirty
    /// union area divided by the visible frame area.
    val damageRegion:
        transitionId: string ->
        frameBounds: Rect ->
        dirtyRectangles: Rect list ->
        expectedAffectedRegionIds: string list ->
        affectedNodeIds: string list ->
        nodeCounts: DamageNodeCounts ->
        cause: string option ->
        maximumDirtyPercentage: float option ->
            DamageRegionInspection
    /// Validate artifact identity, retained node bounds, and unsupported-fact disclosure.
    val artifactDiagnostics: artifact: RetainedInspectionArtifact -> string list
    /// Sort retained nodes, damage facts, findings, and unsupported facts deterministically.
    val normalizeArtifact: artifact: RetainedInspectionArtifact -> RetainedInspectionArtifact
