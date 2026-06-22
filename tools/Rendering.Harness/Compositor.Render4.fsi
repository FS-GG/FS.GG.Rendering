namespace Rendering.Harness.Compositor
    
    module Render4 =
        
        val emitFeature160IterationReport:
          iteration: Types.Feature160Iteration -> string
        
        val emitFeature160ExcludedEvidenceReport:
          reason: Rendering.Harness.Perf.ExclusionReason ->
            iterations: Types.Feature160Iteration list -> string
        
        val emitFeature160ThroughputSummary:
          summary: Types.Feature160ThroughputSummary -> string
        
        val emitFeature160ThroughputSummaryJson:
          summary: Types.Feature160ThroughputSummary -> string
        
        val emitFeature160CompatibilityLedger: unit -> string
        
        val emitFeature160FullValidationRecord:
          record: Types.Feature160FullValidationRecord option -> string
        
        val emitFeature160ValidationSummary:
          summary: Types.Feature160ThroughputSummary -> string
        
        val emitFeature160UnsupportedHostReport: reason: string -> string
        
        val emitFeature161HostFacts: facts: Types.Feature161HostFacts -> string
        
        val emitFeature161LedgerEntry:
          entry: Types.Feature161LedgerEntry -> string
        
        val emitFeature161ExcludedEvidenceReport:
          reason: Rendering.Harness.Perf.ExclusionReason ->
            entries: Types.Feature161LedgerEntry list -> string
        
        val feature161EntryRows: summary: Types.Feature161Summary -> string
        
        val emitFeature161LaneLedgerSummary:
          summary: Types.Feature161Summary -> string
        
        val emitFeature161LaneLedgerSummaryJson:
          summary: Types.Feature161Summary -> string
        
        val emitFeature161CompatibilityLedger: unit -> string
        
        val emitFeature161FullValidationRecord: status: string -> string
        
        val emitFeature161ValidationSummary:
          summary: Types.Feature161Summary -> string
        
        val emitFeature161UnsupportedHostReport: reason: string -> string
