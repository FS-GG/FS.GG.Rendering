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

// Realistic baseline evidence matching the real writers' shape (template/base/src/Product/
// EvidenceCommands.fs): the engine now requires structural tokens on every kind (F-BUILD-2), so a
// fixture must carry them or it is (correctly) a malformed-artifact defect rather than a valid
// baseline. `layoutEvidenceCommand` emits the key/value block below; `sceneEvidence` writes the
// `size=…;capabilities=…;hash=…` metadata value.
let private validLayoutEvidence =
    String.Join(
        "\n",
        [ "status=ok"
          "command=--layout-evidence"
          "profile=headless-governed"
          "scene=AppRoot.Program.view"
          "output-size=640x480"
          "proof-level=structural"
          "text-bounds=3"
          "gameplay-bounds=1"
          "overlap-status=none"
          "measurement-mode=deterministic" ]
    )

let private validSceneEvidence = "size=320x200;capabilities=3;hash=deadbeef"

[<Tests>]
let evidenceTests =
    testList "FS.GG.UI.Build evidence engine" [

        test "EvidenceGraph over a healthy headless readiness surface synthesizes a real graph and passes" {
            let dir = freshFixtureDir ()
            writeReadiness dir "layout-evidence.txt" validLayoutEvidence
            writeReadiness dir "headless-scene-evidence.txt" validSceneEvidence

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
            writeReadiness dir "layout-evidence.txt" validLayoutEvidence
            writeReadiness dir "headless-scene-evidence.txt" validSceneEvidence

            let code = GeneratedRunner.run "EvidenceAudit" dir

            Expect.equal code 0 "EvidenceAudit passes a well-formed surface"
            let audit = readReport dir "evidence-audit.md"
            Expect.stringContains audit "verdict=PASS" "audit carries the required verdict token (PASS)"
        }

        test "empty surface (only build logs present) FAILS the required-baseline floor (F-BUILD-1)" {
            // F-BUILD-1: the audit used to be fail-open on ABSENT evidence — an empty readiness/ (no
            // recognized artifact malformed, because none is present) audited PASS, so a product
            // emitting ZERO evidence passed the gate named Audit green. The required floor
            // (evidence-output-contract.md §EvidenceGraph "required-for-profile") closes that: the
            // headless baseline (layout + scene evidence) MUST be present. A surface holding only the
            // target-completion logs has produced no baseline evidence and is now a product-evidence
            // defect, not a vacuous pass. The graph still lists what exists (it is not an abort).
            let dir = freshFixtureDir ()
            writeReadiness dir "logs/Dev.txt" "Dev completed for generated product."

            let graphCode = GeneratedRunner.run "EvidenceGraph" dir
            let auditCode = GeneratedRunner.run "EvidenceAudit" dir

            Expect.notEqual graphCode 0 "EvidenceGraph fails when the required baseline is absent"
            Expect.notEqual auditCode 0 "EvidenceAudit fails when the required baseline is absent"
            let graph = readReport dir "evidence-graph.md"
            Expect.stringContains graph "readiness/logs/Dev.txt" "graph still lists the actual sensed files"
            Expect.stringContains graph "MISSING (required)" "graph names the absent required baseline"
            let audit = readReport dir "evidence-audit.md"
            Expect.stringContains audit "verdict=FAIL" "an evidence-less surface audits FAIL, not a vacuous PASS"
            Expect.stringContains audit "product-evidence-defect" "absent baseline is classed as a product-evidence defect"
            Expect.stringContains audit "readiness/layout-evidence.txt" "audit names the absent layout baseline"
            Expect.stringContains audit "readiness/headless-scene-evidence.txt" "audit names the absent scene baseline"
        }

        test "partial baseline (layout present, scene absent) FAILS the required floor" {
            // Guards the floor precisely: a product that produced the layout baseline but not the scene
            // baseline is still a defect. Verifies the floor is per-artifact, not "any evidence present".
            let dir = freshFixtureDir ()
            writeReadiness dir "layout-evidence.txt" validLayoutEvidence

            let nodes = Graph.sense dir

            match Audit.evaluate nodes with
            | Verdict.Fail reason ->
                Expect.stringContains reason "headless-scene-evidence.txt" "the absent scene baseline is named"
                Expect.isFalse (reason.Contains "layout-evidence.txt (required") "the PRESENT layout baseline is not reported absent"
            | Verdict.Pass -> failtest "a missing scene baseline must not audit Pass"
        }

        test "EvidenceAudit honest-fail: a present malformed artifact returns non-zero with verdict=FAIL and product-defect class" {
            let dir = freshFixtureDir ()
            // Baseline complete (layout + scene) so the ONLY defect is the malformed artifact — this
            // isolates the present-invalid path from the F-BUILD-1 absent-required floor.
            writeReadiness dir "layout-evidence.txt" validLayoutEvidence
            writeReadiness dir "headless-scene-evidence.txt" validSceneEvidence
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
            Expect.isFalse (audit.Contains "required baseline evidence absent") "with the baseline present, no absent-required reason is emitted"
        }

        test "Graph.sense and Audit.evaluate are usable directly through the public surface" {
            let dir = freshFixtureDir ()
            // A complete headless baseline (layout + scene) so the surface is valid AND satisfies the
            // required floor — Audit.evaluate over it is Pass.
            writeReadiness dir "layout-evidence.txt" validLayoutEvidence
            writeReadiness dir "headless-scene-evidence.txt" validSceneEvidence

            let nodes = Graph.sense dir
            Expect.equal (List.length nodes) 2 "both recognized baseline artifacts sensed"
            Expect.isTrue (nodes |> List.exists (fun n -> n.Kind = "layout")) "the layout node kind is classified"
            Expect.equal (Audit.evaluate nodes) Verdict.Pass "a valid, baseline-complete surface evaluates to Pass"
        }

        test "near-vacuous present baseline (non-whitespace, no structural tokens) FAILS the token contract (F-BUILD-2)" {
            // F-BUILD-2: token-less recognized kinds fell through to "any non-whitespace byte is valid",
            // so a one-byte `layout-evidence.txt` (and ~6 other kinds) audited present-valid. Every kind
            // now carries structural tokens, so a present-but-contentless baseline is a malformed
            // artifact, not a vacuous pass. The scene baseline is complete so the ONLY defect is the
            // stubbed layout artifact — isolating the F-BUILD-2 token floor from the F-BUILD-1 absent floor.
            let dir = freshFixtureDir ()
            writeReadiness dir "layout-evidence.txt" "x"
            writeReadiness dir "headless-scene-evidence.txt" validSceneEvidence

            let nodes = Graph.sense dir

            match nodes |> List.tryFind (fun n -> n.Kind = "layout") with
            | Some { State = EvidenceState.PresentInvalid reason } ->
                Expect.stringContains reason "command=--layout-evidence" "the missing structural token is named"
            | Some { State = EvidenceState.PresentValid } ->
                failtest "a contentless layout baseline must not sense present-valid (F-BUILD-2)"
            | None -> failtest "the layout baseline is present and must be sensed"

            let auditCode = GeneratedRunner.run "EvidenceAudit" dir
            Expect.notEqual auditCode 0 "a present-but-vacuous baseline fails the audit"
            let audit = readReport dir "evidence-audit.md"
            Expect.stringContains audit "verdict=FAIL" "a vacuous present baseline audits FAIL"
            Expect.stringContains audit "layout-evidence.txt" "audit names the vacuous artifact"
            Expect.isFalse (audit.Contains "required baseline evidence absent") "the artifact is present, so it is malformed not absent"
        }

        test "unknown target returns a non-zero diagnostic code" {
            let dir = freshFixtureDir ()
            Expect.notEqual (GeneratedRunner.run "NotATarget" dir) 0 "unknown target is a loud non-zero"
        }
    ]
