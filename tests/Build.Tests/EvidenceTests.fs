module EvidenceTests

open System
open System.IO
open Expecto
open FS.GG.UI.Build.Evidence

// Feature 202 semantic tests. They exercise the engine through its real public surface —
// `GeneratedRunner.run` (the same reflected entrypoint build.fsx calls) plus Graph/Audit — against
// real fixture `readiness/` trees on the filesystem (no mocks; not synthetic). Pass case (US1 /
// T010) and honest-fail case (US3 / T019).

let private freshFixtureDir () =
    let dir = Path.Combine(Path.GetTempPath(), "fsggbuild-tests", Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(Path.Combine(dir, "readiness")) |> ignore
    dir

let private writeReadiness (dir: string) (relName: string) (content: string) =
    let target = Path.Combine(dir, "readiness", relName)
    Directory.CreateDirectory(Path.GetDirectoryName target |> Option.ofObj |> Option.defaultValue dir) |> ignore
    File.WriteAllText(target, content)

let private readReport (dir: string) (relName: string) =
    File.ReadAllText(Path.Combine(dir, "readiness", relName))

[<Tests>]
let evidenceTests =
    testList "FS.GG.UI.Build evidence engine" [

        test "EvidenceGraph over a healthy headless readiness surface synthesizes a real graph and passes" {
            let dir = freshFixtureDir ()
            writeReadiness dir "layout-evidence.txt" "root 800x600; nodes=12; deterministic-layout"
            writeReadiness dir "headless-scene-evidence.txt" "RendererMode = deterministic-scene; nodes=12"

            let code = GeneratedRunner.run "EvidenceGraph" dir

            Expect.equal code 0 "EvidenceGraph passes on a well-formed available surface"
            let graph = readReport dir "evidence-graph.md"
            Expect.stringContains graph "# Evidence graph" "graph is a real synthesized report"
            Expect.stringContains graph "readiness/layout-evidence.txt" "graph names the sensed layout artifact"
            Expect.stringContains graph "present-valid" "graph records the artifact's derived state"
            Expect.isFalse (graph.Contains "completed for generated product") "graph is not a completion-only log stub"
        }

        test "EvidenceAudit over a healthy surface emits verdict=PASS and returns 0" {
            let dir = freshFixtureDir ()
            writeReadiness dir "layout-evidence.txt" "root 800x600; nodes=12"
            writeReadiness dir "headless-scene-evidence.txt" "RendererMode = deterministic-scene"

            let code = GeneratedRunner.run "EvidenceAudit" dir

            Expect.equal code 0 "EvidenceAudit passes a well-formed surface"
            let audit = readReport dir "evidence-audit.md"
            Expect.stringContains audit "verdict=PASS" "audit carries the required verdict token (PASS)"
        }

        test "Verify-shaped empty surface (only build logs present) still graphs and passes" {
            // Mirrors the real Verify sequence: the gate does NOT pre-produce evidence, so at gate
            // time readiness/ holds only the target-completion logs. The engine must graph what
            // exists and pass — not abort because optional evidence artifacts are absent.
            let dir = freshFixtureDir ()
            writeReadiness dir "logs/Dev.txt" "Dev completed for generated product."

            let graphCode = GeneratedRunner.run "EvidenceGraph" dir
            let auditCode = GeneratedRunner.run "EvidenceAudit" dir

            Expect.equal graphCode 0 "EvidenceGraph passes when only build logs are present"
            Expect.equal auditCode 0 "EvidenceAudit passes when nothing present is malformed"
            let graph = readReport dir "evidence-graph.md"
            Expect.stringContains graph "readiness/logs/Dev.txt" "graph lists the actual sensed files"
            Expect.stringContains (readReport dir "evidence-audit.md") "verdict=PASS" "empty-but-clean surface audits PASS"
        }

        test "EvidenceAudit honest-fail: a present malformed artifact returns non-zero with verdict=FAIL and product-defect class" {
            let dir = freshFixtureDir ()
            // window-options.md/.txt requires an `option=` token per evidence-formats.md; present but
            // malformed (no required token) is a defect in the product's OWN evidence.
            writeReadiness dir "window-options.txt" "this file is present but carries no required token"

            let auditCode = GeneratedRunner.run "EvidenceAudit" dir
            let graphCode = GeneratedRunner.run "EvidenceGraph" dir

            Expect.notEqual auditCode 0 "a malformed present artifact fails the audit"
            Expect.notEqual graphCode 0 "a malformed present artifact fails the graph"
            let audit = readReport dir "evidence-audit.md"
            Expect.stringContains audit "verdict=FAIL" "audit carries verdict=FAIL on a malformed artifact"
            Expect.stringContains audit "product-evidence-defect" "audit classes the failure as a product-evidence defect"
            Expect.stringContains audit "framework/feed" "audit distinguishes a framework/feed condition from a product defect"
            Expect.stringContains audit "window-options.txt" "audit names the failing artifact"
        }

        test "Graph.sense and Audit.evaluate are usable directly through the public surface" {
            let dir = freshFixtureDir ()
            writeReadiness dir "layout-evidence.txt" "root 800x600"

            let nodes = Graph.sense dir
            Expect.equal (List.length nodes) 1 "one recognized artifact sensed"
            Expect.equal nodes.[0].Kind "layout" "node kind is classified"
            Expect.equal (Audit.evaluate nodes) Verdict.Pass "a valid surface evaluates to Pass"
        }

        test "unknown target returns a non-zero diagnostic code" {
            let dir = freshFixtureDir ()
            Expect.notEqual (GeneratedRunner.run "NotATarget" dir) 0 "unknown target is a loud non-zero"
        }
    ]
