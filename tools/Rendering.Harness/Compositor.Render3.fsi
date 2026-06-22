namespace Rendering.Harness.Compositor
    
    module Render3 =
        
        val emitFeature156ScenarioReport:
          report: Types.Feature156ScenarioReport -> string
        
        val emitFeature156TimingSummary:
          summary: Types.Feature156TimingSummary -> string
        
        val emitFeature156CompatibilityLedger: unit -> string
        
        val emitFeature156ValidationSummary:
          summary: Types.Feature156TimingSummary -> string
        
        val emitFeature156UnsupportedHostReport: reason: string -> string
        
        val emitFeature157Artifacts: artifacts: string list -> string
        
        val emitFeature157AttemptReport:
          attempt: Types.Feature157DamageAttempt -> string
        
        val emitFeature157FallbackReport:
          fallback: Types.Feature157Fallback -> string
        
        val emitFeature157ParityReport:
          attempt: Types.Feature157DamageAttempt -> string
        
        val feature157ScenarioRows:
          summary: Types.Feature157DamageSummary -> string
        
        val emitFeature157DamageSummary:
          summary: Types.Feature157DamageSummary -> string
        
        val escapeJson: value: string -> string
        
        val emitFeature157DamageSummaryJson:
          summary: Types.Feature157DamageSummary -> string
        
        val emitFeature157CompatibilityLedger: unit -> string
        
        val emitFeature157ValidationSummary:
          summary: Types.Feature157DamageSummary -> string
        
        val emitFeature157UnsupportedHostReport: reason: string -> string
        
        val feature158SampleRows:
          samples: Types.Feature158TimingSample list -> string
        
        val feature158Distribution:
          distribution: Types.Feature158PathDistribution option -> string
        
        val emitFeature158ScenarioReport:
          report: Types.Feature158ScenarioReport -> string
        
        val emitFeature158ExcludedSamplesReport:
          reason: Rendering.Harness.Perf.ExclusionReason ->
            samples: Types.Feature158TimingSample list -> string
        
        val emitFeature158ProofProbeReport:
          evidence: Types.Feature158ProofProbeEvidence list -> string
        
        val feature158ScenarioRows:
          summary: Types.Feature158TimingSummary -> string
        
        val feature158ExcludedReasons:
          summary: Types.Feature158TimingSummary -> string
        
        val emitFeature158TimingSummary:
          summary: Types.Feature158TimingSummary -> string
        
        val emitFeature158TimingSummaryJson:
          summary: Types.Feature158TimingSummary -> string
        
        val emitFeature158CompatibilityLedger: unit -> string
        
        val emitFeature158ValidationSummary:
          summary: Types.Feature158TimingSummary -> string
        
        val emitFeature158UnsupportedHostReport: reason: string -> string
        
        val emitFeature159AttemptReport:
          attempt: Types.Feature159Attempt -> string
        
        val feature159AttemptRows: summary: Types.Feature159Summary -> string
        
        val emitFeature159PromotionSummary:
          summary: Types.Feature159Summary -> string
        
        val emitFeature159CounterReport:
          summary: Types.Feature159Summary -> string
        
        val emitFeature159CompatibilityLedger: unit -> string
        
        val emitFeature159ValidationSummary:
          summary: Types.Feature159Summary -> string
        
        val emitFeature159UnsupportedHostReport: reason: string -> string
        
        val feature160IterationRows:
          summary: Types.Feature160ThroughputSummary -> string
        
        val feature160ScenarioRows:
          iteration: Types.Feature160Iteration -> string
