module Issue1039PerformanceEvidenceTests

open System.IO
open Expecto
open FS.GG.TestSupport
open Rendering.Harness

let private root = RepositoryRoot.value

let private read (relative: string) =
    File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))

[<Tests>]
let performanceEvidenceContract =
    testList
        "issue-1039 expected-workload performance evidence"
        [ test "normal-play verdict fails before a MiniTank-scale node remediation and passes afterward" {
              let budget: Perf.ExpectedWorkloadBudget =
                  { P95Ms = 16.67
                    P99Ms = 25.0
                    MaximumSceneNodes = 4096
                    AllowSustainedCatchUp = false }

              let before =
                  Perf.evaluateExpectedWorkload Perf.NormalPlay (Some budget) None 18.0 28.0 3 6000

              let after =
                  Perf.evaluateExpectedWorkload Perf.NormalPlay (Some budget) None 8.0 14.0 0 3000

              Expect.isFalse
                  before.Passed
                  "thousands of repeated fog/minimap nodes and missed timing budgets fail closed"

              Expect.isTrue after.Passed "row-run/static-subtree-scale remediation passes the same target"
          }

          test "stress evidence cannot be mistaken for the normal-play gate" {
              let verdict =
                  Perf.evaluateExpectedWorkload Perf.Stress None None 100.0 200.0 50 10000

              Expect.isTrue verdict.Passed "stress remains classified informational evidence"
              Expect.stringContains (String.concat " " verdict.Reasons) "non-normal" "classification is explicit"
          }

          test "game scaffold emits the command, artifact contract, workloads and Verify wiring" {
              let source = read "template/base/src/Product/PerformanceEvidence.fs"
              let commands = read "template/base/src/Product/EvidenceCommands.fs"
              let build = read "template/base/build.fsx"

              [ "\"idle\""
                "\"movement-aiming\""
                "\"firing\""
                "\"effects-fog\""
                "\"maximum-content\""
                "\"schemaVersion\", 1"
                "\"definitionDigest\""
                "\"definition\""
                "\"warmupFrames\""
                "\"p50Ms\""
                "\"p95Ms\""
                "\"p99Ms\""
                "\"updateCount\""
                "\"presentCount\""
                "\"catchUpFrames\""
                "\"droppedFrames\""
                "\"eventCount\""
                "\"pointerEventCount\""
                "\"sceneNodesByLayer\""
                "\"allocatedBytes\""
                "\"packageVersions\""
                "\"hostProfile\""
                "\"measurementCapability\""
                "\"notAuthoritativeFor\""
                "active normal-play target failed without a blocking debt reference" ]
              |> List.iter (fun token -> Expect.stringContains source token $"performance artifact carries {token}")

              Expect.stringContains commands "--performance-evidence" "the product command is routed"
              Expect.stringContains build "\"PerformanceEvidence\"" "the explicit build target exists"

              Expect.stringContains
                  build
                  "runPerformanceEvidence ()"
                  "Verify invokes the fail-closed Release measurement"
          }

          test "all four generated-product skills teach one bounded-versus-live recipe" {
              [ "template/base/.agents/skills/fs-gg-project/SKILL.md"
                "template/product-skills/fs-gg-testing/SKILL.md"
                "template/product-skills/fs-gg-scene/SKILL.md"
                "template/product-skills/fs-gg-skiaviewer/SKILL.md" ]
              |> List.iter (fun path ->
                  let skill = read path
                  Expect.stringContains skill "PerformanceEvidence" $"{path} names the target"
                  Expect.stringContains skill "bounded headless" $"{path} discloses the bounded capability"
                  Expect.stringContains skill "live compositor" $"{path} keeps live present proof separate")
          } ]
