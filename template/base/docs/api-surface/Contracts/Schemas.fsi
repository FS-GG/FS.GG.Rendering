// See skill: fs-gg-testing
// Mirrored from FS-GG/FS.GG.SDD @ 7.5.2 (src/FS.GG.Contracts/Schemas.fsi); regenerate when $(FsGgContractsVersion) moves.
namespace Fsgg

/// One typed source of truth for every `.fsgg` schema shape and its version
/// constant (FR-004/005). SDD-owned version constants equal the value SDD emits
/// today; Governance-owned schemas are declared to their published reference.
/// BCL-only: FSharp.Core types exclusively; no serialization, no I/O.
module Schemas =

    /// The canonical declaration authored before implementation and carried unchanged through
    /// evidence and Governance.
    type PerformanceIntentDeclaration =
        { Id: string
          Disposition: string
          TargetFps: int
          WorkloadIds: string list
          WorkloadDefinitionDigests: string list
          MaximumExpectedScale: string
          MaxP95Ms: decimal
          MaxP99Ms: decimal
          MaxCatchUpFrames: int
          StructuralCostBudgets: string list
          RequiredCapability: string
          LiveCompositorRequired: bool
          DeferralIssue: string option
          EvidenceRefs: string list
          Rationale: string option }
