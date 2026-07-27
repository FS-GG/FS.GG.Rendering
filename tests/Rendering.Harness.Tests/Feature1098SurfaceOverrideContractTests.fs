module Feature1098SurfaceOverrideContractTests

// Issue #1098 — `--surface` was documented as "Add or override a skill surface" and did neither: it
// REPLACED the whole surface set, so one override silently disabled the other five gates.
//
// MEASURED on `e2d860bc`: `--surface ant-canonical=<path>` produced a run whose `SupportedSurfaces` was
// exactly one surface. `codex-local`, `claude`, `package-canonical`, `template-canonical` and
// `spec-kit-command` were not checked at all, and the run still printed `skill-parity status: passed`
// and exited 0. The only trace was the `Supported Surfaces` table having one row instead of six.
//
// THE FIX IS THE DOCUMENT AND THE VISIBILITY, NOT THE BEHAVIOUR, and that is a decision worth stating.
// The issue offered both remedies. The merge reading — "swap the roots of a known id, append an unknown
// one" — has to keep the known id's `Selector`, `Kind`, `Agent` and `IsRequired` and change only
// `Roots`. That is the `Roots`/`Selector` shape #1092 landed, which this item puts explicitly out of
// scope, and it contradicts #1092's landed decision that an override is a FRESH declaration rather than
// a patch. It would also destroy the flag's only real use — isolating one synthetic surface against a
// temporary tree, which is exactly how #1086's, #1092's and #1093's own red cases are written: every
// such run would drag the six repository surfaces in and light them all up as empty-and-required.
//
// So replacement stands, the contract now SAYS replacement, and the replacement stopped being silent.
//
// WHAT THESE TESTS WOULD CATCH. Every assertion below is written against the RULE, and the two
// doc-versus-behaviour tests fail in BOTH directions: they re-derive the promise from
// `contracts/skill-parity-cli.md` at runtime and compare it with a measured run, so editing the
// document back to "add" fails, and changing the code to merge without touching the document fails too.
// That two-way property is acceptance criterion 4; a test that hard-coded "replace" would pass on the
// day someone reintroduced the divergence from the other side.
//
// Every green-direction assertion carries a non-vacuity control: "the run checked one surface" is
// equally true of a run that failed to load any surfaces at all, which is the same fail-open one level
// up.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private contractPath =
    Path.Combine(repositoryRoot, "specs", "168-skill-parity-evidence", "contracts", "skill-parity-cli.md")

let private scratch (name: string) = Feature168SkillParityFixtures.createTempRoot name

/// A request whose report and summary writes land in a throwaway directory. `runCheck`/`runCli` WRITE,
/// and `docs/reports/skills-parity.md` is a committed artifact with a CI gate on its exact diff.
let private requestIn (outRoot: string) overrides =
    { SkillParity.defaultRequest repositoryRoot with
        OutDir = Path.Combine(outRoot, "out")
        ReportPath = Path.Combine(outRoot, "out", "report.md")
        SummaryJsonPath = Path.Combine(outRoot, "out", "summary.json")
        SurfaceOverrides = overrides }

let private cliArgs (outRoot: string) (extra: string list) =
    [ "--repo"
      repositoryRoot
      "--out"
      Path.Combine(outRoot, "out")
      "--report"
      Path.Combine(outRoot, "out", "report.md")
      "--summary-json"
      Path.Combine(outRoot, "out", "summary.json") ]
    @ extra

/// `Console.Out` is process-global, which is why the whole list below is `testSequenced`.
let private captureStdout (action: unit -> 'a) =
    let original = Console.Out
    use writer = new StringWriter()

    try
        Console.SetOut writer
        let result = action ()
        Console.Out.Flush()
        result, writer.ToString()
    finally
        Console.SetOut original

let private lines (text: string) =
    text.Replace("\r\n", "\n").Split('\n') |> Array.toList

let private lineStartingWith (prefix: string) (text: string) =
    lines text |> List.tryFind (fun line -> line.StartsWith(prefix, StringComparison.Ordinal))

let private declaredSurfaces () = SkillParity.discoverDefaultSurfaces repositoryRoot

/// The row of the options table that IS the contract sentence this item is about.
let private surfaceOptionRow () =
    File.ReadAllLines contractPath
    |> Array.tryFind (fun line -> line.StartsWith("| `--surface", StringComparison.Ordinal))

/// The bullets under `## Operator Output`, each of which names a line the CLI claims to print. Read
/// from the document rather than listed here, so a bullet added tomorrow is checked without anyone
/// remembering to extend this file.
let private operatorOutputPrefixes () =
    let all = File.ReadAllLines contractPath |> Array.toList

    let rec after heading rest =
        match rest with
        | (line: string) :: tail when line.Trim() = heading -> tail
        | _ :: tail -> after heading tail
        | [] -> []

    after "## Operator Output" all
    |> List.takeWhile (fun line -> not (line.StartsWith("## ", StringComparison.Ordinal)))
    |> List.choose (fun line ->
        let m = Regex.Match(line, @"^- `([^`]+)`")
        if m.Success then Some m.Groups.[1].Value else None)

[<Tests>]
let surfaceOverrideContractTests =
    testSequenced (
        testList "Feature1098 --surface and its contract say the same thing, out loud" [

            // ---------- Acceptance criteria 1 and 4: the doc and the behaviour, compared ----------

            test "the documented meaning of --surface is the measured meaning of --surface" {
                let outRoot = scratch "fsgg-1098-doc-vs-behaviour"

                try
                    let row =
                        match surfaceOptionRow () with
                        | Some row -> row
                        | None ->
                            failtestf
                                "non-vacuity: no `--surface` row in %s — this test compares the document with the code, so a missing row is a failure and not a pass"
                                contractPath

                    let mentions (word: string) =
                        Regex.IsMatch(row, @"\b" + word + @"\w*\b", RegexOptions.IgnoreCase)

                    let docPromisesAddition = mentions "add" || mentions "merge" || mentions "append"
                    let docPromisesReplacement = mentions "replace"

                    Expect.notEqual
                        docPromisesAddition
                        docPromisesReplacement
                        (sprintf
                            "the `--surface` row must state exactly ONE meaning; it reads %s. Saying both is how this item's defect was published in the first place"
                            row)

                    let declared = declaredSurfaces ()

                    Expect.isGreaterThan
                        (List.length declared)
                        1
                        "non-vacuity: the repository declares more than one surface, so addition and replacement are distinguishable at all"

                    // An id BELONGING TO NO DECLARED SURFACE, so the two readings cannot be confused:
                    // under addition the run inspects every declared surface AND this one; under
                    // replacement it inspects this one alone.
                    let probeId = "fsgg-1098-probe"
                    let probeRoot = "docs/product/ant-design/skill/SKILL.md"
                    let report = SkillParity.runCheck (requestIn outRoot [ probeId, probeRoot ])

                    let inspected = report.SupportedSurfaces |> List.map (fun surface -> surface.SurfaceId)

                    Expect.contains
                        inspected
                        probeId
                        "non-vacuity: the override reached the run at all — without this, 'the declared surfaces are absent' is equally consistent with `--surface` having been dropped on the floor"

                    let behaviourAdds =
                        declared
                        |> List.forall (fun surface -> inspected |> List.contains surface.SurfaceId)

                    let behaviourReplaces = inspected = [ probeId ]

                    Expect.equal
                        behaviourReplaces
                        docPromisesReplacement
                        (sprintf
                            "the document and the code disagree about REPLACEMENT: the row reads %s, and a run overriding one unknown id inspected %A"
                            row
                            inspected)

                    Expect.equal
                        behaviourAdds
                        docPromisesAddition
                        (sprintf
                            "the document and the code disagree about ADDITION: the row reads %s, and a run overriding one unknown id inspected %A"
                            row
                            inspected)
                finally
                    Feature168SkillParityFixtures.deleteTempRoot outRoot
            }

            test "every line the contract says the CLI prints is a line the CLI prints" {
                // The second, unnoticed divergence in the same file: `## Operator Output` has promised
                // "checked repository root" and "checked surfaces" since Feature 168, and the CLI printed
                // NEITHER — status, report, summary-json, findings and nothing else. Acceptance criterion 2
                // asks for the surface census on the operator output "because it is already there"; it was
                // not, and this pins that it is now, from the document's own list.
                let outRoot = scratch "fsgg-1098-operator-output"

                try
                    let prefixes = operatorOutputPrefixes ()

                    Expect.isGreaterThanOrEqual
                        (List.length prefixes)
                        6
                        "non-vacuity: the contract's Operator Output list was parsed and is not empty — an unparsed list makes every assertion below vacuously true"

                    let _, stdout = captureStdout (fun () -> SkillParity.runCli (cliArgs outRoot []))

                    for prefix in prefixes do
                        Expect.isSome
                            (lineStartingWith prefix stdout)
                            (sprintf
                                "the contract lists `%s` under Operator Output, and no line of the CLI's stdout starts with it. stdout was:\n%s"
                                prefix
                                stdout)
                finally
                    Feature168SkillParityFixtures.deleteTempRoot outRoot
            }

            // ---------- Acceptance criterion 2: a narrowed run is distinguishable without a table ----------

            test "a narrowed run says so on stdout, and a full run says it was full" {
                let narrowRoot = scratch "fsgg-1098-narrow"
                let fullRoot = scratch "fsgg-1098-full"

                try
                    let declaredCount = List.length (declaredSurfaces ())

                    let _, fullOut = captureStdout (fun () -> SkillParity.runCli (cliArgs fullRoot []))

                    let fullLine =
                        match lineStartingWith "surfaces:" fullOut with
                        | Some line -> line
                        | None -> failtestf "the unnarrowed run printed no `surfaces:` line. stdout was:\n%s" fullOut

                    // The CONTROL. Without it, "the narrowed run says 1 of 6" is equally consistent with a
                    // line that always says 1 of 6, and the narrowing would still be invisible.
                    Expect.equal
                        fullLine
                        (sprintf "surfaces: %i checked of %i declared" declaredCount declaredCount)
                        "an unnarrowed run reports that it checked everything the repository declares, and does not cry NARROWED"

                    Expect.isFalse
                        (fullLine.Contains "NARROWED")
                        "and it is not labelled NARROWED, or the label would carry no information"

                    let _, narrowOut =
                        captureStdout (fun () ->
                            SkillParity.runCli (
                                cliArgs narrowRoot [ "--surface"; "fsgg-1098-probe=docs/product/ant-design/skill/SKILL.md" ]
                            ))

                    let narrowLine =
                        match lineStartingWith "surfaces:" narrowOut with
                        | Some line -> line
                        | None -> failtestf "the narrowed run printed no `surfaces:` line. stdout was:\n%s" narrowOut

                    Expect.stringStarts
                        narrowLine
                        (sprintf "surfaces: 1 checked of %i declared" declaredCount)
                        "a run that inspected one of the declared surfaces says so in words, not by the row count of a table"

                    Expect.stringContains
                        narrowLine
                        "NARROWED"
                        "and names the flag that narrowed it, because the run still prints `passed` and exits 0"
                finally
                    Feature168SkillParityFixtures.deleteTempRoot narrowRoot
                    Feature168SkillParityFixtures.deleteTempRoot fullRoot
            }

            test "the narrowing reaches the machine-readable channels too, and names what went unchecked" {
                // stdout is for a human at a terminal. A caller reading `--json`, and a reader of the
                // committed report or the JSON summary, must be able to tell the same thing — an operator
                // line alone would leave every automated consumer exactly as blind as before.
                let jsonRoot = scratch "fsgg-1098-json"
                let reportRoot = scratch "fsgg-1098-report"

                try
                    let declared = declaredSurfaces ()
                    let declaredCount = List.length declared

                    let _, jsonOut =
                        captureStdout (fun () ->
                            SkillParity.runCli (
                                cliArgs
                                    jsonRoot
                                    [ "--json"
                                      "--surface"
                                      "fsgg-1098-probe=docs/product/ant-design/skill/SKILL.md" ]
                            ))

                    Expect.stringContains jsonOut "\"surfacesChecked\":1" "the --json object reports how many surfaces were checked"

                    Expect.stringContains
                        jsonOut
                        (sprintf "\"surfacesDeclared\":%i" declaredCount)
                        "and how many the repository declares, so the two can be compared without a second run"

                    let report =
                        SkillParity.runCheck (requestIn reportRoot [ "fsgg-1098-probe", "docs/product/ant-design/skill/SKILL.md" ])

                    let caveat =
                        report.Caveats
                        |> List.tryFind (fun caveat -> caveat.Contains "--surface")

                    match caveat with
                    | None ->
                        failtestf
                            "the report of a narrowed run carries no caveat naming `--surface`; its caveats were %A"
                            report.Caveats
                    | Some caveat ->
                        // Naming the SKIPPED ids is the point: a count alone tells a reader that gates were
                        // dropped but not which, and the gates are not interchangeable.
                        for surface in declared do
                            Expect.stringContains
                                caveat
                                surface.SurfaceId
                                (sprintf
                                    "the caveat must name every declared surface this run did not check; '%s' is missing from: %s"
                                    surface.SurfaceId
                                    caveat)

                    // The regenerate line is the report's claim about what produced it. Omitting the
                    // override published a command that regenerates a WIDER run than the report it sits in.
                    Expect.stringContains
                        report.Command
                        "--surface fsgg-1098-probe=docs/product/ant-design/skill/SKILL.md"
                        "the regenerate command reproduces the narrowed run, rather than a different, fuller one"

                    // And the control, on the same channel: an unnarrowed run must stay silent, or the
                    // committed `docs/reports/skills-parity.md` would gain a caveat and its diff gate would
                    // fail on every run.
                    let fullReport = SkillParity.runCheck (requestIn reportRoot [])

                    Expect.isNone
                        (fullReport.Caveats |> List.tryFind (fun caveat -> caveat.Contains "--surface"))
                        "an unnarrowed run adds no narrowing caveat, so the committed report is unchanged by this item"

                    Expect.isFalse
                        (fullReport.Command.Contains "--surface")
                        "and its regenerate line is unchanged too"
                finally
                    Feature168SkillParityFixtures.deleteTempRoot jsonRoot
                    Feature168SkillParityFixtures.deleteTempRoot reportRoot
            }

            // ---------- Acceptance criterion 3: `--fixture` plus `--surface` has ONE meaning ----------

            test "--fixture with --surface replaces the FIXTURE set, resolved beneath the fixture root" {
                // The combination the issue called out as undefined: `--fixture` materialized the tree and
                // rewrote `RepositoryRoot`, but `fixtureSurfaces` was never consulted, so the run inspected
                // the fixture root through the operator's surface. That IS the defined meaning now, and the
                // half that was genuinely missing is which baseline the run reports itself against: the
                // fixture's surfaces, not the repository's.
                let root = scratch "fsgg-1098-fixture"

                try
                    let fixtureRequest overrides =
                        { SkillParity.defaultRequest repositoryRoot with
                            OutDir = Path.Combine(root, "out")
                            ReportPath = Path.Combine(root, "out", "report.md")
                            SummaryJsonPath = Path.Combine(root, "out", "summary.json")
                            FixtureMode = Some "passing"
                            SurfaceOverrides = overrides }

                    // Control: the fixture set alone, so every number below is a fact about the override
                    // and not about a fixture that resolves to nothing.
                    let control = SkillParity.runCheck (fixtureRequest [])
                    let fixtureIds = control.SupportedSurfaces |> List.map (fun surface -> surface.SurfaceId)

                    Expect.isGreaterThan
                        (List.length fixtureIds)
                        1
                        "non-vacuity: --fixture alone inspects the whole synthetic surface set"

                    Expect.isNone
                        (control.Caveats |> List.tryFind (fun caveat -> caveat.Contains "--surface"))
                        "and an unnarrowed fixture run claims no narrowing"

                    let overridden = SkillParity.runCheck (fixtureRequest [ "fixture-canonical", "canonical" ])

                    Expect.equal
                        (overridden.SupportedSurfaces |> List.map (fun surface -> surface.SurfaceId, surface.Roots))
                        [ "fixture-canonical", [ "canonical" ] ]
                        "the override replaces the fixture set, exactly as it replaces the repository set — one rule, not two"

                    // The half of the combination that has to be pinned by a FILE, not a count: the
                    // override's relative root is resolved beneath the materialized fixture tree, not
                    // beneath the repository. Counting canonical sources cannot say this — an override
                    // surface is `Mixed`, and a fixture body with no wrapper route classifies as neither a
                    // canonical source nor a wrapper — so the assertion is over the inventory itself.
                    Expect.notEqual
                        (SkillParity.defaultRequest overridden.RepositoryRoot).RepositoryRoot
                        (SkillParity.defaultRequest repositoryRoot).RepositoryRoot
                        "--fixture re-rooted the run away from the repository"

                    let inventoried =
                        SkillParity.inventorySkills
                            (SkillParity.defaultRequest overridden.RepositoryRoot)
                            overridden.SupportedSurfaces
                        |> List.map (fun entry -> entry.Path.Replace('\\', '/'))

                    Expect.contains
                        inventoried
                        "canonical/passing/SKILL.md"
                        "the override's relative root resolves BENEATH the materialized fixture tree, so the synthetic body is what it inspected"

                    let caveat =
                        match overridden.Caveats |> List.tryFind (fun caveat -> caveat.Contains "--surface") with
                        | Some caveat -> caveat
                        | None -> failtestf "the narrowed fixture run carries no narrowing caveat; caveats were %A" overridden.Caveats

                    Expect.stringContains
                        caveat
                        "the fixture set"
                        "the baseline a fixture run reports itself against is the FIXTURE set — saying 'this repository' here would compare a synthetic run against six surfaces it was never going to check"

                    Expect.stringContains
                        caveat
                        (sprintf "instead of the %i" (List.length fixtureIds))
                        "and the declared count is the fixture set's, not the repository's"

                    for fixtureId in fixtureIds |> List.filter (fun id -> id <> "fixture-canonical") do
                        Expect.stringContains
                            caveat
                            fixtureId
                            (sprintf "the caveat names the fixture surface '%s' that the override dropped" fixtureId)
                finally
                    Feature168SkillParityFixtures.deleteTempRoot root
            }
        ]
    )
