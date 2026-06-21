module TestingCapability.Feature170DamageLocalityValidationTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Testing

let private frame: Rect = { X = 0.0; Y = 0.0; Width = 100.0; Height = 100.0 }
let private rect x y w h : Rect = { X = x; Y = y; Width = w; Height = h }

let private scope scopeId : VisualInspectionScope =
    { ScopeId = scopeId
      Title = scopeId
      Required = true }

let private transition =
    { TransitionId = "hover"
      PriorFrameId = Some "before"
      CurrentFrameId = "after"
      InteractionId = Some "hover"
      ExpectedAffectedRegionIds = [ "button" ]
      MaximumDirtyPercentage = Some 20.0
      IntentionalExceptions = [] }

let private node nodeId status =
    { NodeId = nodeId
      ParentId = None
      RetainedIdentity = Some("retained:" + nodeId)
      Kind = "button"
      OwnerId = Some nodeId
      Status = status
      PriorBounds = Some(rect 0.0 0.0 10.0 10.0)
      CurrentBounds = Some(rect 0.0 0.0 10.0 10.0)
      AffectedRegionIds = [ "button" ]
      Repainted = status = RetainedNodeStatus.Repainted
      Shifted = status = RetainedNodeStatus.Shifted
      UnsupportedFacts = []
      Diagnostics = [] }

let private artifact scopeId damage facts =
    { ArtifactId = scopeId + ":artifact"
      RunId = "run"
      Scope = scope scopeId
      OutputSize = { Width = 100; Height = 100 }
      Presentation = "light"
      Transition = Some transition
      FinalVisualArtifact = None
      RetainedNodes = [ node "button" RetainedNodeStatus.Repainted ]
      Damage = Some damage
      Findings = []
      UnsupportedFacts = facts
      RelatedVisualEvidence = []
      ReadinessStatus = RetainedInspectionStatus.Accepted
      Diagnostics = []
      GeneratedAtUtc = "2026-06-19T00:00:00Z" }

let private validate art =
    RetainedInspectionValidation.validate art RetainedInspectionValidation.defaultRules []

[<Tests>]
let tests =
    testList
        "Feature170 retained damage locality validation"
        [ test "full-surface localized damage is blocked and true-union evidence is checked" {
              let damage =
                  RetainedInspection.damageRegion "hover" frame [ rect 0.0 0.0 100.0 100.0 ] [ "button" ] [ "button" ] { Repainted = 1; Shifted = 0; Unaffected = 0 } None (Some 20.0)

              let result = artifact "fixture" damage [] |> validate

              Expect.equal result.ReadinessStatus RetainedInspectionStatus.Blocked "full surface blocks localized readiness"
              Expect.exists result.Findings (fun finding -> finding.RuleId = "full-surface-localized-change-blocked") "full-surface finding"
          }

          test "broad damage can be accepted by a scoped intentional exception" {
              let damage =
                  RetainedInspection.damageRegion "hover" frame [ rect 0.0 0.0 80.0 80.0 ] [ "button" ] [ "button" ] { Repainted = 1; Shifted = 0; Unaffected = 0 } None (Some 20.0)

              let art = artifact "fixture" damage []
              let broad =
                  RetainedInspection.finding
                      "broad-damage-requires-exception"
                      VisualInspectionSeverity.Blocking
                      "hover"
                      [ "button" ]
                      [ "button" ]
                      "broad"
                      "exception"
                      "broad"

              let exceptionRecord: IntentionalDamageException =
                  { ExceptionId = "accepted-broad-hover"
                    RuleId = "broad-damage-requires-exception"
                    ScopeId = "fixture"
                    TransitionId = "hover"
                    AffectedIds = [ "button"; "button" ] |> List.distinct
                    Reason = "root theme probe is intentionally broad"
                    ExpiresWith = None }

              let result =
                  RetainedInspectionValidation.validate
                      { art with Findings = [ broad ] }
                      [ RetainedInspectionValidation.rule "broad-damage-requires-exception" ]
                      [ exceptionRecord ]

              Expect.equal result.ReadinessStatus RetainedInspectionStatus.Accepted "exception accepts broad finding"
              Expect.equal result.AppliedExceptions [ "accepted-broad-hover" ] "exception id recorded"
          }

          test "unsupported required retained facts do not become accepted readiness" {
              let damage = RetainedInspection.damageRegion "hover" frame [ rect 0.0 0.0 10.0 10.0 ] [ "button" ] [ "button" ] { Repainted = 1; Shifted = 0; Unaffected = 0 } None None
              let fact = RetainedInspection.unsupportedFact "damage-regions" (Some "button") true "not available" "fixture" false
              let result = artifact "fixture" damage [ fact ] |> validate

              Expect.equal result.ReadinessStatus RetainedInspectionStatus.Unsupported "required unsupported fact blocks acceptance"
              Expect.exists result.Findings (fun finding -> finding.RuleId = "unsupported-damage-explicit") "unsupported finding"
          }

          test "summary markdown json and managed section expose reviewer fields" {
              let damage =
                  RetainedInspection.damageRegion "hover" frame [ rect 0.0 0.0 10.0 10.0 ] [ "button" ] [ "button" ] { Repainted = 1; Shifted = 0; Unaffected = 1 } None (Some 20.0)

              let art = artifact "fixture" damage []
              let result = validate art
              let summary =
                  RetainedInspectionReadiness.aggregate
                      "run"
                      [ art ]
                      [ result ]
                      [ "screens/preferred=38 minimum=12" ]
                      [ "command", "dotnet test --filter Feature170" ]
                      []

              let markdown = RetainedInspectionMarkdown.renderSummary summary
              let json = RetainedInspectionMarkdown.renderJson summary
              let update = RetainedInspectionMarkdown.updateManagedSection "# Manual\n" markdown

              Expect.stringContains markdown "Dirty Area" "dirty area reviewer table"
              Expect.stringContains markdown "node counts" "node status counts visible"
              Expect.stringContains json "\"dirtyAreaSummaries\"" "dirty area JSON emitted"
              Expect.isTrue update.SafeToWrite "managed section can be inserted"
              Expect.stringContains update.UpdatedText RetainedInspectionMarkdown.startMarker "start marker inserted"
          } ]
