module Feature159PromotionDecisionTests

open Expecto
open FS.GG.UI.Controls

// SYNTHETIC: The candidate record isolates pure promotion policy thresholds without live rendering.
let private candidate =
    { Feature159PromotionCandidate.BoundaryId = "expensive-panel"
      ScenarioId = "promotion/static-retained"
      HostProfileId = "probe-08a47c01"
      ObservationWindow = 3
      ObservedStabilityFrames = 3
      ExpectedSavedWork = 100
      MeasuredOverhead = 12
      ReductionPercent = 45.0
      ContentStable = true
      ParityPassed = true
      ResourceLimited = false
      CurrentTier = RetainedTier }

[<Tests>]
let tests =
    testList "Feature159 promotion decision" [
        test "Synthetic stable expensive parity-passing content promotes" {
            let decision = CompositorPolicy.feature159EvaluatePromotion candidate None
            Expect.equal (CompositorPolicy.feature159PromotionStatusToken decision.Status) "promoted" "promoted"
            Expect.isNone decision.PrimaryReason "no rejection reason"
        }

        test "Synthetic stability overhead and parity failures fail closed with stable reason tokens" {
            let observing = CompositorPolicy.feature159EvaluatePromotion { candidate with ObservedStabilityFrames = 2 } None
            let lowBenefit = CompositorPolicy.feature159EvaluatePromotion { candidate with ExpectedSavedWork = 10; MeasuredOverhead = 12 } None
            let parity = CompositorPolicy.feature159EvaluatePromotion { candidate with ParityPassed = false } None

            Expect.equal (CompositorPolicy.feature159PromotionStatusToken observing.Status) "observing" "observing"
            Expect.equal (observing.PrimaryReason |> Option.map CompositorPolicy.feature159ReasonToken) (Some "instability") "instability"
            Expect.equal (CompositorPolicy.feature159PromotionStatusToken lowBenefit.Status) "non-beneficial" "non-beneficial"
            Expect.equal (lowBenefit.PrimaryReason |> Option.map CompositorPolicy.feature159ReasonToken) (Some "overhead-exceeds-saved-work") "overhead"
            Expect.equal (CompositorPolicy.feature159PromotionStatusToken parity.Status) "rejected" "parity rejected"
            Expect.equal (parity.PrimaryReason |> Option.map CompositorPolicy.feature159ReasonToken) (Some "parity-mismatch") "parity"
        }
    ]
