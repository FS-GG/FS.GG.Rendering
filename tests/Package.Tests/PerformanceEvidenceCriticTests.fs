module PerformanceEvidenceCriticTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport
open Rendering.Harness

let private root = RepositoryRoot.value

let private read (relative: string) =
    File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))

[<Tests>]
let performanceRepresentativeness =
    testList
        "performance representativeness"
        [ test "provenance and production composition are machine-bound" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"

              [ "RunnerIssuedJourney of JourneyReceipt"
                "SyntheticConstructed of reason: string"
                "JourneyReceipt.compositionAuthority receipt"
                "JourneyReceipt.traceDigest receipt"
                "JourneyReceipt.result receipt <> JourneyResult.Passed"
                "cannot establish shipped-route normal-play or maximum-scale coverage"
                "ComponentOnlySupplemental of reason: string"
                "cannot claim complete normal-play composition" ]
              |> List.iter (fun token -> Expect.stringContains source token $"provenance contract carries {token}")

              Expect.isFalse
                  (source.Contains("issueProvenanceReceipt", StringComparison.Ordinal))
                  "caller-authored labels and hashes cannot mint normal-play provenance"
          }

          test "independent cost inventory fails closed and cross-checks runtime visuals" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"

              [ "let performanceCostDrivers ="
                "let private costDriverProblems"
                "GameplayVisualInventory.all"
                "performance visual coverage differs from GameplayVisualInventory"
                "let private observeCostScale"
                "ScaleObserver: (RoutedStimulus -> Model -> int option) option"
                "has no product-declared exact scale observer"
                "GameplayVisualInventory.project model"
                "has no required workload binding"
                "maximum scale is underrepresented"
                "names unknown cost driver" ]
              |> List.iter (fun token -> Expect.stringContains source token $"coverage contract carries {token}")

              let observation =
                  source.Substring(
                      source.IndexOf("let private observeCostScale", StringComparison.Ordinal),
                      source.IndexOf("let private runWorkload", StringComparison.Ordinal)
                      - source.IndexOf("let private observeCostScale", StringComparison.Ordinal)
                  )

              Expect.isFalse
                  (observation.Contains("| Simulation, _ -> 1", StringComparison.Ordinal))
                  "simulation does not receive a category-level constant that can mask zero query pressure"
          }

          test "Rogue2-shaped confident evidence is rejected by named machine reasons" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"

              [ "declared event count"
                "observed routed count"
                "declared pointer event count"
                "RawInputSampleCount"
                "Unsupported \"bounded-headless route has no compositor presentation capability\""
                "Unsupported \"bounded-headless route has no swapchain/drop observation capability\""
                "\"status\", \"unsupported\""
                "\"synthetic-constructed\""
                "\"component-only-supplemental\"" ]
              |> List.iter (fun token -> Expect.stringContains source token $"negative fixture gate carries {token}")

              Expect.isFalse
                  (source.Contains("PresentCount = 0", StringComparison.Ordinal)
                   || source.Contains("DroppedFrames = 0", StringComparison.Ordinal))
                  "unsupported host metrics are never emitted as observed zero"
          }

          test "critic request is digest-bound and cannot be self-attested in the authored tree" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"

              [ "performance-representativeness-v1"
                "\"status\", \"external-review-required\""
                "attributable review system at the exact landing commit"
                "\"preferredMode\", \"fresh-context-subagent\""
                "\"fallbackMode\", \"separated-pass-with-independence-disclosure\""
                "in-repo JSON, author-entered identity, or a same-context mode string cannot establish independence"
                "\"underrepresentative\""
                "\"synthetic-only\""
                "\"unmeasured\""
                "\"misclassified\""
                "\"ambiguous\""
                "\"representativeReady\", false"
                "\"evidenceArtifactDigest\""
                "JourneyReceipt.compositionAuthority receipt"
                "provenanceToken workload.Provenance"
                "provenanceDefinitionToken workload.Provenance"
                "P95Ms:R"
                "capabilityMetricToken result.PresentCount"
                "declaredPackageVersions ()"
                "result.Verdict.Passed" ]
              |> List.iter (fun token -> Expect.stringContains source token $"critic contract carries {token}")

              Expect.isFalse
                  (source.Contains("CriticReviewed", StringComparison.Ordinal))
                  "authored source has no path that can mint an independent approval"
          }

          test "schema migration, command, ADR, and all four skills explain the ordered gates" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"
              let commands = read "template/base/src/Product/EvidenceCommands.fs"
              let build = read "template/base/build.fsx"
              let decision = read "docs/product/decisions/0108-performance-representativeness.md"

              Expect.stringContains source "\"schemaVersion\", 3" "new evidence has an additive schema"
              Expect.stringContains source "\"legacy-unreviewed\"" "old claim meaning stays explicit"
              Expect.stringContains commands "--performance-critic-request" "critic request is routable"
              Expect.stringContains build "PerformanceCriticRequest" "critic request is discoverable"
              Expect.stringContains decision "Rendering publishes the additive scaffold" "rollout is durable"

              [ "template/base/.agents/skills/fs-gg-project/SKILL.md"
                "template/product-skills/fs-gg-testing/SKILL.md"
                "template/product-skills/fs-gg-scene/SKILL.md"
                "template/product-skills/fs-gg-skiaviewer/SKILL.md" ]
              |> List.iter (fun path ->
                  let skill = read path
                  Expect.stringContains skill "machine" $"{path} orders deterministic evidence first"
                  Expect.stringContains skill "critic" $"{path} preserves the judgment layer")
          } ]
