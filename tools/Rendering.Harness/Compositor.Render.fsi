namespace Rendering.Harness.Compositor
    
    module Render =
        
        val renderPresentProof: proof: Types.PresentProof -> string
        
        val renderValidationSummary: model: Types.ReadinessModel -> string
        
        val renderCompatibilityLedger: model: Types.ReadinessModel -> string
        
        val renderArtifacts: artifacts: string list -> string
        
        val emitFeature148LiveProof: proof: Types.PresentProof -> string
        
        val emitFeature148ParityReport: unit -> string
        
        val emitFeature148ReuseReport: unit -> string
        
        val emitFeature148SnapshotReport: unit -> string
        
        val emitFeature148TimingReport: tier: string -> string
        
        val emitFeature148ValidationSummary:
          model: Types.ReadinessModel -> string
        
        val emitFeature148CompatibilityLedger:
          model: Types.ReadinessModel -> string
        
        val emitFeature149LiveProof: proof: Types.PresentProof -> string
        
        val emitFeature149ParityReport: unit -> string
        
        val emitFeature149ReuseReport: unit -> string
        
        val emitFeature149SnapshotReport: unit -> string
        
        val emitFeature149TimingReport: tier: string -> string
        
        val emitFeature149ValidationSummary:
          model: Types.ReadinessModel -> string
        
        val emitFeature149CompatibilityLedger:
          model: Types.ReadinessModel -> string
        
        val emitFeature152LiveProof: proof: Types.PresentProof -> string
        
        val emitFeature152ParityReport: unit -> string
        
        val emitFeature152TimingReport: tier: string -> string
        
        val emitFeature152ValidationSummary:
          model: Types.ReadinessModel -> string
        
        val emitFeature152CompatibilityLedger:
          model: Types.ReadinessModel -> string
        
        val emitFeature153LiveProof: proof: Types.PresentProof -> string
        
        val emitFeature153ProofSet: model: Types.ReadinessModel -> string
        
        val emitFeature153ValidationSummary:
          model: Types.ReadinessModel -> string
        
        val emitFeature153CompatibilityLedger:
          model: Types.ReadinessModel -> string
        
        val renderValidationDoc:
          featureNum: int ->
            kind: string -> status: string -> body: string list -> string
        
        val validationRunsBlock: validationLines: string list -> string
        
        val renderPackageValidation:
          featureNum: int -> validationLines: string list -> string
        
        val renderRegressionValidation:
          featureNum: int -> validationLines: string list -> string
        
        val emitFeature153PackageValidation: unit -> string
        
        val emitFeature153RegressionValidation: unit -> string
