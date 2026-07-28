module Feature1110MalformedSurfaceValueTests

// Issue #1110 — a malformed `--surface` value was silently dropped, so a run asked to NARROW ran the
// FULL repository check and exited 0.
//
// MEASURED on `f822e88a` (the #928 merge), against this repository:
//
//     $ ... skill-parity --repo . --surface totally-malformed-no-equals
//     skill-parity status: passed
//     root: /.../wt-1110
//     surfaces: 6 checked of 6 declared
//     findings: critical=0 high=0 warning=0 info=0
//     exit 0
//
// Nothing on stdout, nothing on stderr, and `docs/reports/skills-parity.md` rewritten on the way out.
//
// THIS IS NOT #1098, AND #1098's REMEDY STRUCTURALLY CANNOT REACH IT. #1098 made a narrowed run tell on
// itself on four channels — the `surfaces:` operator line, `surfacesChecked`/`surfacesDeclared` in
// `--json`, the report/summary caveat, and the regenerate line. Every one of those is guarded by
// `request.SurfaceOverrides.IsEmpty`, and a dropped value leaves that list EMPTY. So the run looks, to
// every channel #1098 added, exactly like a run nobody narrowed. The defect is one level below: the
// parser, not the semantics.
//
// THE FIX IS EXIT 2 BEFORE ANYTHING RUNS. `contracts/skill-parity-cli.md` reserves exit `2` for a
// "surface configuration error", and `runCli` already spends it on an unrecognized *option* with a
// comment saying why — an ignored flag "would run a full check and rewrite the committed report". A
// malformed *value* of a recognized option lands there by the same argument.
//
// AND `id=` IS AN ERROR, which is the decision this item was asked to make and record. `index <= 0`
// already rejected `=path`; `id=` was ACCEPTED with an empty path, and an empty root resolves to the
// repository root — so `--surface id=` inventoried the whole tree under one operator id while calling
// itself narrowed. Both spellings made the run as large as possible, which is the fail-open the flag
// exists to prevent.
//
// WHAT THESE TESTS WOULD CATCH, AND IN WHICH DIRECTION. The refused-value corpus and the expected exit
// code are both READ FROM `contracts/skill-parity-cli.md` AT RUNTIME rather than restated here, which is
// acceptance criterion 4 and the pattern `Feature1098SurfaceOverrideContractTests` established. So:
// adding an example to the document that the code accepts is red; changing the documented exit code
// without changing the code is red; changing the code without the document is red. A test that hard-coded
// `2` and a list of three strings would pass on the day the contract was edited out from under it.
//
// Every refusal assertion carries the control that makes it non-vacuous: the SAME argv shape with a
// well-formed value writes the report and does not exit 2. Without it, "no report was written" is equally
// true of a harness that never ran the CLI at all.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private contractPath =
    Path.Combine(repositoryRoot, "specs", "168-skill-parity-evidence", "contracts", "skill-parity-cli.md")

/// A surface root that genuinely exists, so the WELL-FORMED control is a run that had something to read.
let private probeRoot = "docs/product/ant-design/skill/SKILL.md"

let private scratch (name: string) = Feature168SkillParityFixtures.createTempRoot name

let private reportPathIn (outRoot: string) = Path.Combine(outRoot, "out", "report.md")
let private summaryPathIn (outRoot: string) = Path.Combine(outRoot, "out", "summary.json")

/// Every write this CLI performs is redirected into a throwaway tree. `docs/reports/skills-parity.md` is
/// a committed artifact with a CI gate on its exact diff, and the defect under test is precisely that a
/// refused run used to rewrite it.
let private cliArgs (outRoot: string) (extra: string list) =
    [ "--repo"
      repositoryRoot
      "--out"
      Path.Combine(outRoot, "out")
      "--report"
      reportPathIn outRoot
      "--summary-json"
      summaryPathIn outRoot ]
    @ extra

/// `Console.Out`/`Console.Error` are process-global, which is why the whole list below is `testSequenced`.
let private captureStreams (action: unit -> 'a) =
    let originalOut = Console.Out
    let originalErr = Console.Error
    use outWriter = new StringWriter()
    use errWriter = new StringWriter()

    try
        Console.SetOut outWriter
        Console.SetError errWriter
        let result = action ()
        Console.Out.Flush()
        Console.Error.Flush()
        result, outWriter.ToString(), errWriter.ToString()
    finally
        Console.SetOut originalOut
        Console.SetError originalErr

let private lines (text: string) =
    text.Replace("\r\n", "\n").Split('\n') |> Array.toList

// ---------- The contract, read at runtime ----------

let private contractLines () = File.ReadAllLines contractPath |> Array.toList

/// The `## Exit Codes` table, as `code -> meaning`. The codes are what the CLI must actually return, so
/// they are parsed rather than repeated: a row edited to say `0` must fail this file, not slip past it.
let private exitCodeRows () =
    let rec after heading rest =
        match rest with
        | (line: string) :: tail when line.Trim() = heading -> tail
        | _ :: tail -> after heading tail
        | [] -> []

    after "## Exit Codes" (contractLines ())
    |> List.takeWhile (fun line -> not (line.StartsWith("## ", StringComparison.Ordinal)))
    |> List.choose (fun line ->
        let m = Regex.Match(line, @"^\|\s*`(\d+)`\s*\|\s*(.+?)\s*\|\s*$")

        if m.Success then
            Some(int m.Groups.[1].Value, m.Groups.[2].Value)
        else
            None)

/// The code the document reserves for a surface configuration error — found by its MEANING, so renumbering
/// the table moves this test with it rather than breaking it into a false red.
let private documentedSurfaceErrorCode () =
    exitCodeRows ()
    |> List.tryPick (fun (code, meaning) ->
        if meaning.IndexOf("surface configuration error", StringComparison.OrdinalIgnoreCase) >= 0 then
            Some code
        else
            None)

let private refusedSectionHeading = "### A malformed `--surface` value is refused"

/// Every `--surface <value>` spelling the document lists as an error. THIS IS THE TEST CORPUS: a case the
/// contract names is a case the CLI must refuse, and nobody has to remember to extend this file when the
/// document grows a fourth bullet.
///
/// Scoped to the section rather than swept from the whole file, so a bullet elsewhere that happens to show
/// a VALID `--surface` example cannot be press-ganged into the refusal corpus. If the heading is renamed
/// this returns nothing and the non-vacuity assertions below say so by name — a loud red, not a silent
/// zero-iteration pass.
let private documentedRefusedValues () =
    let rec after heading rest =
        match rest with
        | (line: string) :: tail when line.Trim() = heading -> tail
        | _ :: tail -> after heading tail
        | [] -> []

    after refusedSectionHeading (contractLines ())
    |> List.takeWhile (fun line -> not (line.StartsWith("#", StringComparison.Ordinal)))
    |> List.choose (fun line ->
        let m = Regex.Match(line, @"^- `--surface ([^`]+)`")
        if m.Success then Some m.Groups.[1].Value else None)

[<Tests>]
let malformedSurfaceValueTests =
    testSequenced (
        testList "Feature1110 a malformed --surface value is refused, not dropped" [

            // ---------- Acceptance criteria 1, 2 and 4 ----------

            test "every --surface value the contract calls an error exits with the documented code and writes nothing" {
                let controlRoot = scratch "fsgg-1110-control"

                try
                    let expectedCode =
                        match documentedSurfaceErrorCode () with
                        | Some code -> code
                        | None ->
                            failtestf
                                "non-vacuity: no `## Exit Codes` row in %s means a 'surface configuration error' — this test compares the document with the code, so a missing row is a failure and not a pass"
                                contractPath

                    let refused = documentedRefusedValues ()

                    Expect.isGreaterThanOrEqual
                        (List.length refused)
                        3
                        (sprintf
                            "non-vacuity: the '%s' section of %s was found and its refused-`--surface` bullets parsed; it yielded %A. An unparsed list makes the loop below run zero times and pass"
                            refusedSectionHeading
                            contractPath
                            refused)

                    // THE CONTROL, and it is the whole reason the refusals below mean anything: the same
                    // argv shape with a WELL-FORMED value runs, does not exit with the error code, and
                    // WRITES the report. Without it, "no report exists" is equally true of a CLI that
                    // cannot write reports at all, and "exit 2" is equally true of one that refuses
                    // everything.
                    let controlExit, controlOut, _ =
                        captureStreams (fun () ->
                            SkillParity.runCli (cliArgs controlRoot [ "--surface"; "fsgg-1110-probe=" + probeRoot ]))

                    Expect.notEqual
                        controlExit
                        expectedCode
                        (sprintf
                            "a well-formed `--surface fsgg-1110-probe=%s` is not a configuration error. stdout was:\n%s"
                            probeRoot
                            controlOut)

                    Expect.isTrue
                        (File.Exists(reportPathIn controlRoot))
                        "and it WRITES the report — the file the refusals below must be missing is a file this CLI demonstrably produces"

                    Expect.stringContains
                        controlOut
                        "NARROWED"
                        "and the run it produced was the narrowed one the operator asked for, not a full one"

                    for value in refused do
                        let outRoot = scratch "fsgg-1110-refused"

                        try
                            let exitCode, stdout, stderr =
                                captureStreams (fun () -> SkillParity.runCli (cliArgs outRoot [ "--surface"; value ]))

                            Expect.equal
                                exitCode
                                expectedCode
                                (sprintf
                                    "`--surface %s` is listed as an error in %s, and the Exit Codes table reserves %i for a surface configuration error. stderr was:\n%s"
                                    value
                                    contractPath
                                    expectedCode
                                    stderr)

                            // Acceptance criterion 1: a diagnostic naming the offending value, on stderr.
                            Expect.stringContains
                                stderr
                                value
                                (sprintf
                                    "the refusal must NAME the offending value; stderr for `--surface %s` was:\n%s"
                                    value
                                    stderr)

                            // Acceptance criterion 2: nothing regenerated on that path.
                            Expect.isFalse
                                (File.Exists(reportPathIn outRoot))
                                (sprintf
                                    "a run that refused `--surface %s` must not have written a report on its way out"
                                    value)

                            Expect.isFalse
                                (File.Exists(summaryPathIn outRoot))
                                (sprintf
                                    "nor the JSON summary — `--summary-json` is regenerated by the same path as the report"
                                    )

                            // And it must not CLAIM a result either. The defect's signature was a
                            // `skill-parity status: passed` line for a check that never ran as asked.
                            Expect.isNone
                                (lines stdout
                                 |> List.tryFind (fun line -> line.StartsWith("skill-parity status:", StringComparison.Ordinal)))
                                (sprintf
                                    "a refused run reports no status; stdout for `--surface %s` was:\n%s"
                                    value
                                    stdout)
                        finally
                            Feature168SkillParityFixtures.deleteTempRoot outRoot
                finally
                    Feature168SkillParityFixtures.deleteTempRoot controlRoot
            }

            // ---------- Acceptance criterion 3: the decision about `=path` and `id=`, recorded ----------

            test "the contract's refused list names both the empty-id and the empty-path spelling" {
                // The issue asked for a DECISION — `parseSurfaceOverride` treated `index <= 0` as
                // unparseable but accepted an empty path — and for the decision to be stated in the
                // contract. Both are errors, and this pins that the document still says so. The shapes are
                // derived from the parsed values, so renaming the examples is fine and dropping one is not.
                let refused = documentedRefusedValues ()

                Expect.isNonEmpty
                    refused
                    (sprintf "non-vacuity: the '%s' section of %s parsed" refusedSectionHeading contractPath)

                Expect.isTrue
                    (refused |> List.exists (fun value -> value.StartsWith("=", StringComparison.Ordinal)))
                    (sprintf
                        "the contract must name the EMPTY-ID spelling (`--surface =path`) among its errors; it lists %A"
                        refused)

                Expect.isTrue
                    (refused
                     |> List.exists (fun value ->
                         value.EndsWith("=", StringComparison.Ordinal)
                         && value.Length > 1))
                    (sprintf
                        "and the EMPTY-PATH spelling (`--surface id=`), which used to be ACCEPTED and inventoried the whole repository root; it lists %A"
                        refused)

                Expect.isTrue
                    (refused |> List.exists (fun value -> not (value.Contains "=")))
                    (sprintf "and the no-`=` spelling this item was filed for; it lists %A" refused)
            }

            // ---------- The second silent route: a `--surface` with no value at all ----------

            test "a trailing --surface with no value is refused too, and is not an unknown option" {
                // `flagValues` matches `flag :: value :: tail`, so an option at the END of argv never
                // reaches the parser — it falls through and disappears. Same full run, same exit 0, by a
                // route the value check alone does not cover. The `unknown` guard cannot catch it either:
                // `--surface` IS a known flag, so its message would be the wrong one even if it fired.
                let outRoot = scratch "fsgg-1110-no-value"

                try
                    let expectedCode =
                        match documentedSurfaceErrorCode () with
                        | Some code -> code
                        | None -> failtestf "non-vacuity: no surface-configuration-error row in %s" contractPath

                    let exitCode, stdout, stderr =
                        captureStreams (fun () -> SkillParity.runCli (cliArgs outRoot [ "--surface" ]))

                    Expect.equal
                        exitCode
                        expectedCode
                        (sprintf "a `--surface` with no value is a surface configuration error. stderr was:\n%s" stderr)

                    Expect.stringContains
                        stderr
                        "--surface"
                        "and the diagnostic names the flag that was left dangling"

                    Expect.isFalse
                        (stderr.Contains "unknown option")
                        "and it is not reported as an UNKNOWN option — `--surface` is a known flag whose VALUE is missing, and telling an operator otherwise sends them to fix the wrong thing"

                    Expect.isFalse
                        (File.Exists(reportPathIn outRoot))
                        "and nothing was regenerated"

                    Expect.isNone
                        (lines stdout
                         |> List.tryFind (fun line -> line.StartsWith("skill-parity status:", StringComparison.Ordinal)))
                        (sprintf "and no status was claimed; stdout was:\n%s" stdout)
                finally
                    Feature168SkillParityFixtures.deleteTempRoot outRoot
            }

            // ---------- The refusal is per-INVOCATION, not per-value ----------

            test "one malformed value refuses the whole run, even alongside a well-formed one" {
                // `--surface` is repeatable. Dropping the bad one and honouring the good one would run a
                // check over a surface set the operator did not ask for and report it as theirs — the same
                // lie in a smaller room. The run is refused, and the diagnostic names the offender rather
                // than the whole argv.
                let outRoot = scratch "fsgg-1110-mixed"

                try
                    let expectedCode =
                        match documentedSurfaceErrorCode () with
                        | Some code -> code
                        | None -> failtestf "non-vacuity: no surface-configuration-error row in %s" contractPath

                    let exitCode, _, stderr =
                        captureStreams (fun () ->
                            SkillParity.runCli (
                                cliArgs
                                    outRoot
                                    [ "--surface"
                                      "fsgg-1110-probe=" + probeRoot
                                      "--surface"
                                      "no-equals-here" ]
                            ))

                    Expect.equal
                        exitCode
                        expectedCode
                        (sprintf "a malformed value anywhere in argv refuses the invocation. stderr was:\n%s" stderr)

                    Expect.stringContains
                        stderr
                        "no-equals-here"
                        "and names the offending value"

                    Expect.isFalse
                        (stderr.Contains "fsgg-1110-probe")
                        "and NOT the well-formed one it was written beside, which would send an operator to edit the argument that was fine"

                    Expect.isFalse
                        (File.Exists(reportPathIn outRoot))
                        "and the well-formed half did not run on its own"
                finally
                    Feature168SkillParityFixtures.deleteTempRoot outRoot
            }
        ]
    )
