module Feature1093UnreadableSurfaceTests

// Issue #1093 — a skill file that EXISTS and cannot be READ must be a finding that fails the gate, never
// a silent drop.
//
// The bug this pins: `SkillParity.inventorySkills` was `List.choose (fun path -> try Some (readEntry …)
// with _ -> None)`. The catch was total. It swallowed IO errors, permission errors, and any defect in
// `readEntry`/`parseFrontMatter` alike, and it emitted no finding, no caveat and no diagnostic — the file
// was simply not in the inventory, and nothing anywhere said so.
//
// Why that is a fail-open and not a rough edge. #1086 makes a REQUIRED surface that resolves to zero
// FILES a High finding, and that check is deliberately over `filesForSurface`, which counts files on
// disk. So the residual hole is exact: a surface whose files all exist but all fail to read has a
// non-empty file list (it passes #1086's check) and contributes zero ENTRIES, so every entry-driven
// producer — wrapper parity, canonical drift, manifest coverage, API symbols — has nothing to say about
// it, and the gate reports `passed`. The surface was not empty; it was UNHEARD. Worse than the original
// in one respect: #1086's version at least showed a zero in the surface inventory, whereas this one shows
// files present and findings absent.
//
// The first test below is that scenario end to end. It makes the fixture's canonical body unreadable, and
// the whole three-entry inventory collapses — the two wrappers route THROUGH that body, so their target
// read fails too. Under the old code that run reported `passed` with an empty findings list and an empty
// inventory. Under this one it is three findings and `failed`.
//
// On the trigger. The issue records why a natural one is not available: `File.ReadAllText` does not throw
// on invalid UTF-8, a directory named `SKILL.md` is not returned by `Directory.GetFiles`, and
// permission-based fixtures are unreliable in CI and useless as root (CI runs as root in a container, and
// root reads a 0000 file). And there is NO natural trigger at all for the second case — an unexpected
// exception type is by definition a defect nobody can provoke on demand. So the failure is injected
// through the reader seam `runCheckWith`/`inventorySkillsWith` take, which is deterministic by
// construction and exercises the behaviour rather than asserting it. That is the option the issue names,
// and it is preferred to skipping the test: a fix that cannot be proven red is the thing #1086 was about.

open System
open System.IO
open Expecto
open Rendering.Harness

/// The fixture's aligned, clean case: one canonical body plus a Codex and a Claude wrapper routing to it.
/// The control tests below assert this tree is GREEN with a real reader, so every `Failed` asserted
/// against it is attributable to the injected read failure and nothing else.
let private cleanFixtureRequest root =
    Feature168SkillParityFixtures.request root "passing"

/// A reader that behaves exactly like `File.ReadAllText` except on paths matching `shouldFail`, where it
/// raises `raiseWith`. Note it reads for real otherwise — a reader that failed on everything would prove
/// nothing, because an inventory that is empty for every reason looks the same.
let private readerFailingOn (shouldFail: string -> bool) (raiseWith: string -> exn) : string -> string =
    fun path ->
        if shouldFail path then
            raise (raiseWith path)
        else
            File.ReadAllText path

let private normalize (path: string) = path.Replace('\\', '/')

let private isCanonicalPassingBody (path: string) =
    (normalize path).EndsWith("canonical/passing/SKILL.md", StringComparison.Ordinal)

let private unreadableFindings (report: SkillParity.ParityReport) =
    report.Findings
    |> List.filter (fun finding -> finding.Category = SkillParity.UnreadableSurface)

/// The CLI's own exit rule, restated over a finding: `runCli` exits non-zero when any finding is at least
/// as severe as `FailOnSeverity`, which defaults to `High`. "Fails the gate" is this predicate and not a
/// severity name, so the assertions below cannot drift from what the gate actually does.
let private failsTheGate (request: SkillParity.ParityCheckRequest) (finding: SkillParity.ParityFinding) =
    let rank severity =
        match severity with
        | SkillParity.Info -> 0
        | SkillParity.Warning -> 1
        | SkillParity.High -> 2
        | SkillParity.Critical -> 3

    rank finding.Severity >= rank request.FailOnSeverity

[<Tests>]
let unreadableSurfaceTests =
    testList "Feature1093 an unreadable skill file is reported, never silently dropped" [

        // The control. Everything below asserts a `Failed` against this tree, so if it were not green to
        // begin with those assertions would prove nothing.
        test "control: the same fixture with a real reader is green and reports no unreadable surface" {
            let root = Feature168SkillParityFixtures.createTempRoot "fsgg-1093-control"

            try
                let request = cleanFixtureRequest root
                let report = SkillParity.runCheckWith File.ReadAllText request

                Expect.equal
                    report.OverallStatus
                    SkillParity.Passed
                    "the aligned fixture passes with a real reader — the baseline every red case below is measured against"

                Expect.isEmpty
                    (unreadableFindings report)
                    "a tree whose files all read cleanly produces no unreadable-surface finding, so the new producer cannot be firing on healthy input"

                Expect.isGreaterThan
                    report.CanonicalSourceCount
                    0
                    "the fixture really did inventory a canonical body (non-vacuity)"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "a skill file that cannot be read fails the gate and names the path and the reason" {
            let root = Feature168SkillParityFixtures.createTempRoot "fsgg-1093-unreadable"

            try
                let request = cleanFixtureRequest root

                let reader =
                    readerFailingOn isCanonicalPassingBody (fun path ->
                        IOException($"synthetic injected read failure for {path}") :> exn)

                let report = SkillParity.runCheckWith reader request
                let findings = unreadableFindings report

                // The exact fail-open: with the canonical body unreadable, all three entries are lost —
                // the wrappers route through it, so their target read fails too — and every entry-driven
                // rule goes quiet. The OLD code reported `passed` with zero findings on this very run.
                Expect.isNonEmpty findings "the unreadable file produces a finding rather than vanishing from the inventory"

                Expect.equal
                    report.CanonicalSourceCount
                    0
                    "no canonical source survived the injected failure — this is precisely the state the old code reported as `passed`"

                Expect.equal
                    report.OverallStatus
                    SkillParity.Failed
                    "the gate FAILS: an inventory that lost every entry to unreadable files is not a clean tree"

                Expect.all
                    findings
                    (failsTheGate request)
                    "every unreadable-surface finding is at or above the default FailOnSeverity, so `runCli` exits non-zero"

                let canonicalFinding =
                    findings
                    |> List.tryFind (fun finding -> isCanonicalPassingBody finding.SkillName)

                match canonicalFinding with
                | None ->
                    failtestf
                        "no finding named the unreadable canonical body; findings named %A"
                        (findings |> List.map (fun f -> f.SkillName))
                | Some finding ->
                    Expect.equal finding.Severity SkillParity.High "an unreadable FILE is a fact about the repository, reported at High"

                    Expect.stringContains
                        finding.Message
                        "IOException"
                        "the finding names the exception TYPE, so the reader can tell an IO error from a permission error without re-running anything"

                    Expect.stringContains
                        finding.Message
                        "synthetic injected read failure"
                        "the finding carries the underlying reason verbatim rather than a generic 'could not read'"

                    Expect.isSome finding.CanonicalPath "the finding carries the offending path as evidence"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        // Acceptance criterion 3. The old catch was total, so a NullReferenceException escaping
        // `parseFrontMatter` was indistinguishable from a chmod — both were `None`. Reporting a harness
        // defect as "this file is unreadable" sends the reader to fix a file that is perfectly fine.
        test "an unexpected exception is reported as a harness defect, not as an unreadable file" {
            let root = Feature168SkillParityFixtures.createTempRoot "fsgg-1093-defect"

            try
                let request = cleanFixtureRequest root

                let reader =
                    readerFailingOn isCanonicalPassingBody (fun _ ->
                        NullReferenceException("synthetic defect escaping the reader") :> exn)

                let report = SkillParity.runCheckWith reader request

                let finding =
                    unreadableFindings report
                    |> List.tryFind (fun finding -> isCanonicalPassingBody finding.SkillName)

                match finding with
                | None -> failtest "an unexpected exception type is still reported — silence is the defect #1093 closed"
                | Some finding ->
                    Expect.equal
                        finding.Severity
                        SkillParity.Critical
                        "a defect in the harness itself outranks an unreadable file: the evidence is not merely missing, the tool is wrong"

                    Expect.isTrue
                        (failsTheGate request finding)
                        "it fails the gate too — an unjudged skill body is an unjudged skill body either way"

                    Expect.stringContains
                        finding.Message
                        "NullReferenceException"
                        "the finding names the unexpected type"

                    Expect.stringContains
                        finding.Message
                        "defect in the skill-parity harness"
                        "and says whose bug it is, rather than blaming the file"

                    Expect.isFalse
                        (finding.Message.Contains "could not be read")
                        "it must NOT be worded as an unreadable file — that is the conflation acceptance criterion 3 forbids"

                Expect.equal
                    report.OverallStatus
                    SkillParity.Failed
                    "and the overall status is failed"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        // The inventory itself, below the report: the entry is genuinely absent (so the surrounding rules
        // really did have nothing to judge) AND the reason is genuinely reported. Both halves matter —
        // returning the failure while quietly keeping a half-built entry would be a different bug.
        test "inventorySkillsWith drops the entry and hands back the reason for it" {
            let root = Feature168SkillParityFixtures.createTempRoot "fsgg-1093-inventory"

            try
                let request = cleanFixtureRequest root
                // Materialize the fixture tree the same way the pipeline does, then inventory it directly.
                SkillParity.createFixture (Path.Combine(request.OutDir, "_skill-parity-fixture")) "passing"
                let fixtureRoot = Path.Combine(request.OutDir, "_skill-parity-fixture")
                let inventoryRequest = { request with RepositoryRoot = fixtureRoot; FixtureMode = None }

                let surfaces: SkillParity.SkillSurface list =
                    [ { SurfaceId = "fixture-canonical"
                        DisplayName = "fixture canonical"
                        RootPath = "canonical"
                        Kind = SkillParity.Canonical
                        Agent = SkillParity.Repository
                        IsRequired = true
                        Notes = [] } ]

                let cleanEntries, cleanFailures =
                    SkillParity.inventorySkillsWith File.ReadAllText inventoryRequest surfaces

                Expect.isNonEmpty cleanEntries "the surface really does inventory a body when the read succeeds (non-vacuity)"
                Expect.isEmpty cleanFailures "and reports no failure for it"

                let reader =
                    readerFailingOn isCanonicalPassingBody (fun path ->
                        UnauthorizedAccessException($"synthetic access denial for {path}") :> exn)

                let entries, failures = SkillParity.inventorySkillsWith reader inventoryRequest surfaces

                Expect.isEmpty
                    (entries |> List.filter (fun entry -> isCanonicalPassingBody entry.Path))
                    "the unreadable body contributes no entry — nothing downstream can judge it, which is why it must be reported here"

                Expect.equal (List.length failures) 1 "exactly one failure, for exactly the one file that threw"

                let failure = List.head failures
                Expect.equal failure.Kind SkillParity.UnreadableFile "UnauthorizedAccessException is the filesystem's answer, not a harness defect"
                Expect.equal failure.ExceptionType "UnauthorizedAccessException" "the concrete type is preserved"
                Expect.stringContains failure.Reason "synthetic access denial" "the message is preserved"

                Expect.isFalse
                    (Path.IsPathRooted failure.Path)
                    "the path is repository-relative, so the finding text does not bake in whichever checkout produced it"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        // Two unreadable files under one surface must be two findings. `classifyFindings` dedupes by
        // FindingId, and the id is built from category+surface+skill — but the skill NAME is precisely
        // what could not be read, so an id that did not carry the PATH would collapse every unreadable
        // file on a surface into one. Same trap the broken-target producer hit.
        test "two unreadable files under one surface are two findings, not one" {
            let root = Feature168SkillParityFixtures.createTempRoot "fsgg-1093-dedupe"

            try
                let request = Feature168SkillParityFixtures.request root "all"

                let reader =
                    readerFailingOn
                        (fun path -> (normalize path).Contains "/canonical/")
                        (fun path -> IOException($"synthetic injected read failure for {path}") :> exn)

                let report = SkillParity.runCheckWith reader request

                let paths =
                    unreadableFindings report
                    |> List.map (fun finding -> normalize finding.SkillName)
                    |> List.distinct

                Expect.isGreaterThan
                    (List.length paths)
                    1
                    "the `all` fixture has several canonical bodies and each unreadable one is reported separately"

                let ids =
                    unreadableFindings report
                    |> List.map (fun finding -> finding.FindingId)

                Expect.equal
                    (List.length ids)
                    (ids |> List.distinct |> List.length)
                    "and their FindingIds are distinct, so the severity-resolving dedupe cannot collapse them"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }
    ]
