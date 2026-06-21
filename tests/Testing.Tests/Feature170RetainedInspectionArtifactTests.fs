module TestingCapability.Feature170RetainedInspectionArtifactTests

open Expecto
open FS.GG.UI.Scene

let private frame: Rect = { X = 0.0; Y = 0.0; Width = 100.0; Height = 100.0 }
let private rect x y w h : Rect = { X = x; Y = y; Width = w; Height = h }

let private scope: VisualInspectionScope =
    { ScopeId = "retained-artifact"
      Title = "Retained Artifact"
      Required = true }

let private node nodeId status prior current =
    { NodeId = nodeId
      ParentId = None
      RetainedIdentity = Some("retained:" + nodeId)
      Kind = "text-block"
      OwnerId = Some nodeId
      Status = status
      PriorBounds = prior
      CurrentBounds = current
      AffectedRegionIds = [ "content" ]
      Repainted = status = RetainedNodeStatus.Repainted || status = RetainedNodeStatus.ShiftedAndRepainted
      Shifted = status = RetainedNodeStatus.Shifted || status = RetainedNodeStatus.ShiftedAndRepainted
      UnsupportedFacts = []
      Diagnostics = [] }

let private transition =
    { TransitionId = "t1"
      PriorFrameId = Some "f0"
      CurrentFrameId = "f1"
      InteractionId = Some "hover"
      ExpectedAffectedRegionIds = [ "content" ]
      MaximumDirtyPercentage = Some 50.0
      IntentionalExceptions = [] }

let private artifact nodes damage facts =
    { ArtifactId = "artifact"
      RunId = "run"
      Scope = scope
      OutputSize = { Width = 100; Height = 100 }
      Presentation = "light"
      Transition = Some transition
      FinalVisualArtifact = None
      RetainedNodes = nodes
      Damage = Some damage
      Findings = []
      UnsupportedFacts = facts
      RelatedVisualEvidence = []
      ReadinessStatus = RetainedInspectionStatus.Accepted
      Diagnostics = []
      GeneratedAtUtc = "2026-06-19T00:00:00Z" }

[<Tests>]
let tests =
    testList
        "Feature170 retained inspection artifact"
        [ test "normalization keeps stable node finding and unsupported-fact ordering" {
              let damage =
                  RetainedInspection.damageRegion "t1" frame [ rect 20.0 0.0 20.0 20.0 ] [ "content" ] [ "b" ] { Repainted = 1; Shifted = 0; Unaffected = 1 } None (Some 50.0)

              let factA = RetainedInspection.unsupportedFact "zeta" None true "reason" "diagnostic" false
              let factB = RetainedInspection.unsupportedFact "alpha" None true "reason" "diagnostic" false
              let findingA = RetainedInspection.finding "rule-b" VisualInspectionSeverity.Blocking "t1" [ "b" ] [] "b" "expected" "actual"
              let findingB = RetainedInspection.finding "rule-a" VisualInspectionSeverity.Blocking "t1" [ "a" ] [] "a" "expected" "actual"

              let normalized =
                  RetainedInspection.normalizeArtifact
                      { artifact
                            [ node "b" RetainedNodeStatus.Repainted (Some(frame)) (Some(frame))
                              node "a" RetainedNodeStatus.Reused (Some(frame)) (Some(frame)) ]
                            damage
                            [ factA; factB ] with
                          Findings = [ findingA; findingB ] }

              Expect.equal (normalized.RetainedNodes |> List.map _.NodeId) [ "a"; "b" ] "retained nodes sorted"
              Expect.equal (normalized.Findings |> List.map _.RuleId) [ "rule-a"; "rule-b" ] "findings sorted"
              Expect.equal (normalized.UnsupportedFacts |> List.map _.Fact) [ "alpha"; "zeta" ] "facts sorted"
          }

          test "artifact diagnostics report shifted nodes missing bounds and duplicate ids" {
              let damage = RetainedInspection.damageRegion "t1" frame [] [] [] { Repainted = 0; Shifted = 1; Unaffected = 0 } None None
              let shifted = node "dup" RetainedNodeStatus.Shifted None (Some(rect 1.0 1.0 10.0 10.0))

              let diagnostics =
                  artifact [ shifted; shifted ] damage [] |> RetainedInspection.artifactDiagnostics

              Expect.exists diagnostics (fun item -> item.Contains("duplicate retained inspection node id")) "duplicate node id"
              Expect.exists diagnostics (fun item -> item.Contains("missing prior or current bounds")) "shifted missing bounds"
          } ]
