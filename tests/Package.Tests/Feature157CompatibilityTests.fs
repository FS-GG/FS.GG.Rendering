module Feature157CompatibilityTests

open System
open System.IO
open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host
open FS.GG.UI.Testing
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList
        "Feature157 compatibility package"
        [ test "SkiaViewer exposes additive damage decision and diagnostics vocabulary" {
              Expect.equal (Viewer.damageDecisionToken ViewerDamageDecision.FullRedraw) "full-redraw" "viewer damage token"
              let diagnostic = Diagnostics.damageScopedDecision "damage-scoped-accepted" None
              Expect.stringContains diagnostic.Message "Feature157" "diagnostic mentions feature"
          }

          test "Testing exposes Feature157 damage readiness validation without accepting performance" {
              let check =
                  { Feature = "157-no-clear-damage-scissor"
                    RequiredScenarioIds = [ "damage/static-preserved" ]
                    Scenarios =
                      [ { ScenarioId = "damage/static-preserved"
                          Status = CompositorDamageAccepted
                          AcceptedAttemptCount = 3
                          ArtifactPaths = [ "damage/attempts/static.md" ]
                          FallbackReason = None } ]
                    AcceptedAttemptCount = 3
                    UnsupportedHostStatus = CompositorDamageEnvironmentLimited
                    AcceptedPartialRedrawArtifacts = 0
                    CompatibilityAccepted = true
                    PackageAccepted = true
                    RegressionAccepted = true
                    PerformanceClaim = "performance-not-accepted"
                    Limitations = [] }

              let result = CompositorDamageReadiness.validate check
              Expect.isTrue result.Accepted "damage correctness accepted"
              Expect.equal (CompositorDamageReadiness.statusText result.Status) "accepted" "status token"
          }

          test "compatibility ledger documents additive public surface and no shipped performance claim" {
              let ledgerPath = repo "specs/157-no-clear-damage-scissor/readiness/compatibility-ledger.md"
              Expect.isTrue (File.Exists ledgerPath) $"Feature157 ledger exists at {ledgerPath}"
              let ledger = File.ReadAllText ledgerPath
              Expect.stringContains ledger "GlHost" "SkiaViewer host surface"
              Expect.stringContains ledger "CompositorDamageReadiness" "Testing helper surface"
              Expect.stringContains ledger "performance-not-accepted" "performance boundary"
          }

          test "package validation records Feature157 commands and surface boundary" {
              let packagePath = repo "specs/157-no-clear-damage-scissor/readiness/package-validation.md"
              Expect.isTrue (File.Exists packagePath) $"Feature157 package validation exists at {packagePath}"
              let package = File.ReadAllText packagePath
              Expect.stringContains package "compositor-readiness --feature 157" "readiness command"
              Expect.stringContains package "SkiaViewer, Testing, and harness signatures" "surface note"
          } ]
