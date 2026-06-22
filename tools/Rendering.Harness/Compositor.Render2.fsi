namespace Rendering.Harness.Compositor
    
    module Render2 =
        
        val emitFeature154LiveProof: proof: Types.PresentProof -> string
        
        val emitFeature154ProofSet: model: Types.ReadinessModel -> string
        
        val feature154ParityScenarioVerdict: scenario: string -> string
        
        val emitFeature154ParityReport: unit -> string
        
        val emitFeature154TimingReport:
          tier: string -> scenarioCount: int -> repetitions: int -> string
        
        val emitFeature154ValidationSummary:
          model: Types.ReadinessModel -> string
        
        val emitFeature154CompatibilityLedger:
          model: Types.ReadinessModel -> string
        
        val emitFeature154PackageValidation: unit -> string
        
        val emitFeature154RegressionValidation: unit -> string
        
        val emitFeature155LiveProof: proof: Types.PresentProof -> string
        
        val feature155AcceptedProofs:
          model: Types.ReadinessModel -> Types.PresentProof list
        
        val emitFeature155ProofSet: model: Types.ReadinessModel -> string
        
        val feature155ParityScenarioVerdict: scenario: string -> string
        
        val emitFeature155ParityReport: unit -> string
        
        val emitFeature155TimingReport:
          tier: string -> scenarioCount: int -> repetitions: int -> string
        
        val emitFeature155ValidationSummary:
          model: Types.ReadinessModel -> string
        
        val emitFeature155CompatibilityLedger:
          model: Types.ReadinessModel -> string
        
        val emitFeature155PackageValidation: unit -> string
        
        val emitFeature155RegressionValidation: unit -> string
        
        val feature156Reasons: report: Types.Feature156ScenarioReport -> string
