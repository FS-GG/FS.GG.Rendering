module Feature170RetainedInspectionSurfaceTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Scene
open FS.GG.UI.Testing

type private Msg = Noop

[<Tests>]
let tests =
    testList
        "Feature170 retained inspection surface"
        [ test "Scene Controls and Testing expose retained inspection authoring names" {
              let scope: VisualInspectionScope = { ScopeId = "surface"; Title = "Surface"; Required = true }
              let size: Size = { Width = 100; Height = 100 }
              let rect: Rect = { X = 0.0; Y = 0.0; Width = 10.0; Height = 10.0 }

              let transition: RetainedFrameTransition =
                  { TransitionId = "t"
                    PriorFrameId = Some "before"
                    CurrentFrameId = "after"
                    InteractionId = Some "hover"
                    ExpectedAffectedRegionIds = [ "content" ]
                    MaximumDirtyPercentage = Some 25.0
                    IntentionalExceptions = [] }

              let retainedNode: RetainedNodeInspection =
                  { NodeId = "node"
                    ParentId = None
                    RetainedIdentity = Some "retained:1"
                    Kind = "button"
                    OwnerId = Some "button"
                    Status = RetainedNodeStatus.Reused
                    PriorBounds = Some rect
                    CurrentBounds = Some rect
                    AffectedRegionIds = []
                    Repainted = false
                    Shifted = false
                    UnsupportedFacts = []
                    Diagnostics = [] }

              let damage =
                  RetainedInspection.damageRegion "t" { X = 0.0; Y = 0.0; Width = 100.0; Height = 100.0 } [ rect ] [ "content" ] [ "node" ] 1 0 1 None (Some 25.0)

              let artifact: RetainedInspectionArtifact =
                  { ArtifactId = "artifact"
                    RunId = "run"
                    Scope = scope
                    OutputSize = size
                    Presentation = "light"
                    Transition = Some transition
                    FinalVisualArtifact = None
                    RetainedNodes = [ retainedNode ]
                    Damage = Some damage
                    Findings = []
                    UnsupportedFacts = []
                    RelatedVisualEvidence = []
                    ReadinessStatus = RetainedInspectionStatus.Accepted
                    Diagnostics = []
                    GeneratedAtUtc = "2026-06-19T00:00:00Z" }

              let rule: RetainedInspectionRule = RetainedInspectionValidation.rule "dirty-region-unioned"
              let validation = RetainedInspectionValidation.validate artifact [ rule ] []
              let summary = RetainedInspectionReadiness.aggregate "run" [ artifact ] [ validation ] [] [] []

              Expect.equal (RetainedInspection.statusText summary.OverallStatus) "accepted" "status token"
              Expect.equal (RetainedInspection.nodeStatusText retainedNode.Status) "reused" "node token"
              Expect.equal (RetainedInspection.damageStatusText damage.DamageStatus) "localized" "damage token"
              Expect.stringContains (RetainedInspectionMarkdown.renderJson summary) "\"overallStatus\"" "json renderer available"

              let request: RetainedControlTransition<Msg> =
                  { TransitionId = "t"
                    PriorControl = None
                    CurrentControl = Control.create "text-block" []
                    InteractionId = None
                    ExpectedAffectedRegionIds = []
                    MaximumDirtyPercentage = None
                    IntentionalExceptions = [] }

              Expect.equal request.TransitionId "t" "Controls retained request shape is public"
          } ]
