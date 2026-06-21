module Feature128DesignSystemTemplateTests

// Feature 128 (Workstream F, F3) — the always-on gate (validation contract Layer 1).
//
// Deterministic, GL-free, NO `dotnet new`: it asserts the committed generated-product validation
// report that the env-gated regenerator (scripts/validate-design-system-template.fsx) writes. The
// heavy live work — `dotnet new` per accepted value + real `dotnet build` + per-policy verdict —
// runs behind FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1, mirroring GeneratedConsumerValidationTests +
// FS_SKIA_RUN_PACKAGE_CONSUMER_SMOKE.
//
// Authored failing-first (Principle V / GV-8): with the report absent the suite is RED; the
// regenerator greens it. Coverage (GV-1) is checked against the template's own designSystem choice
// set, so a new accepted value left unvalidated fails the gate (FR-009/SC-006).

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private validationReportPath =
    repositoryPath "specs/128-design-system-template-param/readiness/design-system-template-validation.md"

// GV-8: the report MUST exist; its absence is a loud failure (failing-first before the regenerator).
let private readValidationReport () =
    Expect.isTrue
        (File.Exists validationReportPath)
        (sprintf
            "design-system validation report missing at %s — regenerate via FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1 dotnet fsi scripts/validate-design-system-template.fsx"
            validationReportPath)

    File.ReadAllText validationReportPath

/// The accepted designSystem choice set, parsed from the template (single coverage source, TP-7).
let private enumeratedChoices () =
    let json = File.ReadAllText(repositoryPath ".template.config/template.json")
    let mi = json.IndexOf("\"designSystem\"", StringComparison.Ordinal)
    let choicesIdx = json.IndexOf("\"choices\"", mi, StringComparison.Ordinal)
    let arrStart = json.IndexOf('[', choicesIdx)
    let arrEnd = json.IndexOf(']', arrStart)
    let body = json.Substring(arrStart, arrEnd - arrStart)
    let token = "\"choice\""
    let rec loop i acc =
        let ci = body.IndexOf(token, i, StringComparison.Ordinal)
        if ci < 0 then
            List.rev acc
        else
            let colon = body.IndexOf(':', ci)
            let q1 = body.IndexOf('"', colon + 1)
            let q2 = body.IndexOf('"', q1 + 1)
            loop (q2 + 1) (body.Substring(q1 + 1, q2 - q1 - 1) :: acc)
    loop 0 []

/// The values listed on the report's `covered-values:` line.
let private coveredValues (report: string) =
    report.Split('\n')
    |> Array.tryFind (fun l -> l.StartsWith "covered-values:")
    |> Option.map (fun l ->
        l.Substring("covered-values:".Length).Split(',')
        |> Array.map (fun s -> s.Trim())
        |> Array.filter (fun s -> s <> "")
        |> Array.toList)
    |> Option.defaultValue []

[<Tests>]
let feature128DesignSystemTemplateTests =
    testList
        "Feature128 design-system template validation"
        [
          // GV-1 (FR-009/SC-006): covered-values equals the enumerated designSystem choice set.
          test "GV-1 coverage equals the template's designSystem choice set" {
              let report = readValidationReport ()
              let covered = coveredValues report |> List.sort
              let expected = enumeratedChoices () |> List.sort
              Expect.equal
                  covered
                  expected
                  "covered-values must equal the template's designSystem choices (no accepted value unvalidated)"
              Expect.stringContains report "covered-values: wcag, ant" "covered-values line lists wcag, ant in declaration order"
          }

          // GV-2 (FR-008/SC-006): every covered value reports build=pass.
          test "GV-2 every covered value reports build=pass" {
              let report = readValidationReport ()
              for v in enumeratedChoices () do
                  Expect.stringContains report (sprintf "%s: build=pass" v) (sprintf "%s reports build=pass" v)
          }

          // GV-3 (FR-003/FR-010/SC-001/SC-003): wcag is diff-vs-today=none with today's verdicts.
          test "GV-3 wcag is byte-identical and keeps today's WCAG verdicts" {
              let report = readValidationReport ()
              [ "wcag: build=pass"
                "diff-vs-today=none"
                "overall=FAIL"
                "authority=WcagCertified" ]
              |> List.iter (fun token -> Expect.stringContains report token (sprintf "wcag line includes %s" token))
          }

          // GV-4 (FR-004/FR-005/SC-002/SC-003): ant records its policy and passes its pairings.
          test "GV-4 ant records its policy and reports overall=PASS" {
              let report = readValidationReport ()
              [ "ant: build=pass"
                "record=ant"
                "overall=PASS"
                "authority=AntExpectation" ]
              |> List.iter (fun token -> Expect.stringContains report token (sprintf "ant line includes %s" token))
          }

          // GV-5 (FR-006/SC-004): a divergent pairing with opposite outcomes under the two policies.
          test "GV-5 a pairing diverges by policy (policy, not palette)" {
              let report = readValidationReport ()
              Expect.stringContains
                  report
                  "divergent-pairing: primary-hover-fg-on-surface wcag=Fail ant=Aa"
                  "the divergent pairing fails under wcag and passes under ant"
          }

          // GV-6 (FR-010): the no-overclaim authority note is disclosed for ant.
          test "GV-6 ant discloses the no-overclaim authority note" {
              let report = readValidationReport ()
              Expect.stringContains
                  report
                  "no-overclaim-note: ant: not WCAG-certified"
                  "ant certify-where-WCAG-fails verdict is disclosed as not WCAG-certified"
          }

          // GV-7 (US3 / Principle VI): result: pass only when GV-1..GV-6 hold.
          test "GV-7 overall result is pass" {
              let report = readValidationReport ()
              Expect.stringContains report "result: pass" "validation result is pass"
          }
        ]
