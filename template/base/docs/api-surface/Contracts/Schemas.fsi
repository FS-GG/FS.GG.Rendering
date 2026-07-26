// See skill: fs-gg-testing
// Mirrored from FS-GG/FS.GG.SDD @ 7.0.0 (src/FS.GG.Contracts/Schemas.fsi); regenerate when $(FsGgContractsVersion) moves.
namespace Fsgg

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
