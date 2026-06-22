namespace Rendering.Harness.Compositor
    
    module FeatureState =
        
        val initReadiness:
          unit -> Types.ReadinessModel * Types.ReadinessEffect list
        
        val updateReadiness:
          msg: Types.ReadinessMsg ->
            model: Types.ReadinessModel ->
            Types.ReadinessModel * Types.ReadinessEffect list
        
        val initFeature154:
          unit -> Types.Feature154Model * Types.Feature154Effect list
        
        val updateFeature154:
          msg: Types.Feature154Msg ->
            model: Types.Feature154Model ->
            Types.Feature154Model * Types.Feature154Effect list
        
        val feature156VerdictToken:
          verdict: Types.Feature156ScenarioVerdict -> string
        
        val feature156OverallVerdict:
          reports: Types.Feature156ScenarioReport list ->
            Types.Feature156ScenarioVerdict
        
        val initFeature156:
          warmupCount: int ->
            measuredRepetitions: int ->
            Types.Feature156Model * Types.Feature156Effect list
        
        val updateFeature156:
          msg: Types.Feature156Msg ->
            model: Types.Feature156Model ->
            Types.Feature156Model * Types.Feature156Effect list
        
        val feature157StatusToken:
          status: Types.Feature157DamageStatus -> string
        
        val feature157ScenarioFileName: scenarioId: string -> string
        
        val feature157OverallStatus:
          summary: Types.Feature157DamageSummary -> Types.Feature157DamageStatus
        
        val initFeature157:
          unit -> Types.Feature157Model * Types.Feature157Effect list
        
        val feature157StatusFrom:
          attempts: Types.Feature157DamageAttempt list ->
            fallbacks: Types.Feature157Fallback list ->
            diagnostics: string list -> Types.Feature157DamageStatus
        
        val updateFeature157:
          msg: Types.Feature157Msg ->
            model: Types.Feature157Model ->
            Types.Feature157Model * Types.Feature157Effect list
        
        val feature158StatusToken:
          status: Types.Feature158ReadinessStatus -> string
        
        val feature158ScenarioFileName: scenarioId: string -> string
        
        val feature158StatusFromReports:
          reports: Types.Feature158ScenarioReport list ->
            diagnostics: string list -> Types.Feature158ReadinessStatus
        
        val initFeature158:
          warmupCount: int ->
            measuredRepetitions: int ->
            Types.Feature158Model * Types.Feature158Effect list
        
        val updateFeature158:
          msg: Types.Feature158Msg ->
            model: Types.Feature158Model ->
            Types.Feature158Model * Types.Feature158Effect list
        
        val feature159StatusToken:
          status: Types.Feature159ReadinessStatus -> string
        
        val feature159ScenarioFileName: scenarioId: string -> string
        
        val feature159StatusFromAttempts:
          attempts: Types.Feature159Attempt list ->
            diagnostics: string list -> Types.Feature159ReadinessStatus
        
        val initFeature159:
          unit -> Types.Feature159Model * Types.Feature159Effect list
        
        val updateFeature159:
          msg: Types.Feature159Msg ->
            model: Types.Feature159Model ->
            Types.Feature159Model * Types.Feature159Effect list
        
        val feature160StatusToken:
          status: Types.Feature160ReadinessStatus -> string
        
        val feature160ScenarioFileName: scenarioId: string -> string
        
        val feature160IterationFileName: iterationId: string -> string
        
        val feature160AcceptedSamplePolicy:
          sample: Types.Feature158TimingSample -> bool
        
        val feature160IterationAccepted:
          iteration: Types.Feature160Iteration -> bool
        
        val feature160FullValidationStatus:
          record: Types.Feature160FullValidationRecord option -> string
        
        val feature160FullValidationAccepts:
          record: Types.Feature160FullValidationRecord option -> bool
        
        val feature160FocusedThroughputStatus:
          summary: Types.Feature160ThroughputSummary ->
            Types.Feature160ReadinessStatus
        
        val feature160OverallStatus:
          summary: Types.Feature160ThroughputSummary ->
            Types.Feature160ReadinessStatus
        
        val initFeature160:
          attempts: int ->
            maxIterationMinutes: int ->
            Types.Feature160Model * Types.Feature160Effect list
        
        val feature160StatusFromModel:
          model: Types.Feature160Model -> Types.Feature160ReadinessStatus
        
        val updateFeature160:
          msg: Types.Feature160Msg ->
            model: Types.Feature160Model ->
            Types.Feature160Model * Types.Feature160Effect list
        
        val feature161StatusToken:
          status: Types.Feature161ReadinessStatus -> string
        
        val feature161HostFactsFileName: entryId: string -> string
        
        val feature161LedgerEntryFileName: entryId: string -> string
        
        val feature161LaneIdFromFacts:
          facts: Types.Feature161HostFacts -> string
        
        val feature161ValidateHostFacts:
          facts: Types.Feature161HostFacts ->
            Rendering.Harness.Perf.ExclusionReason option
        
        val feature161LedgerEntryAccepted:
          entry: Types.Feature161LedgerEntry -> bool
        
        val feature161ScopeFromEntries:
          entries: Types.Feature161LedgerEntry list ->
            Types.Feature161ClaimScope
        
        val feature161OverallStatus:
          summary: Types.Feature161Summary -> Types.Feature161ReadinessStatus
        
        val initFeature161:
          sourceThroughput: string option ->
            Types.Feature161Model * Types.Feature161Effect list
        
        val feature161StatusFromModel:
          model: Types.Feature161Model -> Types.Feature161ReadinessStatus
        
        val updateFeature161:
          msg: Types.Feature161Msg ->
            model: Types.Feature161Model ->
            Types.Feature161Model * Types.Feature161Effect list
        
        val artifactPath: directory: string -> name: string -> string
        
        val feature148ArtifactPath: directory: string -> name: string -> string
        
        val feature149ArtifactPath: directory: string -> name: string -> string
        
        val feature152ArtifactPath: directory: string -> name: string -> string
        
        val feature153ArtifactPath: directory: string -> name: string -> string
        
        val feature154ArtifactPath: directory: string -> name: string -> string
        
        val feature155ArtifactPath: directory: string -> name: string -> string
        
        val feature156ArtifactPath: directory: string -> name: string -> string
        
        val feature157ArtifactPath: directory: string -> name: string -> string
        
        val feature158ArtifactPath: directory: string -> name: string -> string
        
        val feature159ArtifactPath: directory: string -> name: string -> string
        
        val feature160ArtifactPath: directory: string -> name: string -> string
        
        val feature161ArtifactPath: directory: string -> name: string -> string
        
        val feature156ScenarioFileName: scenarioId: string -> string
        
        val feature158OverallStatus:
          summary: Types.Feature158TimingSummary ->
            Types.Feature158ReadinessStatus
        
        val feature159OverallStatus:
          summary: Types.Feature159Summary -> Types.Feature159ReadinessStatus
        
        val feature156FormatMs: value: float -> string
        
        val feature156DistributionRow:
          distribution: Types.Feature156PathDistribution option -> string
        
        val feature158DistributionRow:
          distribution: Types.Feature158PathDistribution option -> string
