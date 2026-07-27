module Feature1093UnreadableSurfaceTests

// Issue #1093 — a skill file that EXISTS and cannot be READ must be a finding that fails the gate, never
// a silent drop.
//
// The bug this pins: `SkillParity.inventorySkills` was `List.choose (fun path -> try Some (readEntry …)
// with _ -> None)`. The catch was total. It swallowed IO errors, permission errors, and any defect in
// `readEntry`/`parseFrontMatter` alike, and it emitted no finding, no caveat and no diagnostic — the file
// was simply not in the inventory, and nothing anywhere said so.
//
// Why that is a fail-open and not a rough edge. Every OTHER rule in the module judges bodies that were
// successfully READ — wrapper parity, canonical drift, manifest coverage, API symbols — so a file that
// never became an entry is a file none of them can speak about, and the gate reports `passed`. The
// surface was not empty; it was UNHEARD, which is strictly harder to notice than empty: an empty surface
// at least shows a zero in the inventory, whereas this shows the files present and the findings absent.
//
// #1093 was split out of #1086 and is INDEPENDENT of it. (#1086 is OPEN as this is written — PR #1091 —
// so nothing it proposes is on `main`, and the assertions here are written against the tree that exists
// rather than the one it will produce.) #1086 proposes to fail a REQUIRED surface that resolves to zero
// FILES, a check over `filesForSurface`, which counts files on disk; a surface whose files all exist and
// all fail to read has a non-empty file list, so it would pass that check too. Different cause,
// different remedy, neither subsuming the other.
//
// The second test below is the scenario end to end. It makes the fixture's canonical body unreadable and
// the whole three-entry inventory collapses — the two wrappers route THROUGH that body, so their target
// read fails too — yet all three findings name the CANONICAL path, because that is the file that could
// not be read. Naming the wrapper would send a reader to fix a file that reads perfectly.
//
// On the trigger, where this item's stated premise turned out to be too pessimistic by half.
//
// The issue ruled out the candidates it considered, correctly: `File.ReadAllText` does not throw on
// invalid UTF-8, a directory named `SKILL.md` is not returned by `Directory.GetFiles`, and permission
// bits are useless as root, which is how CI runs. It concluded a seam was the only option. But a
// share-mode LOCK is neither a permission nor a platform quirk — opening the file `FileShare.None` makes
// every other open fail, including one in the same process, and .NET implements that on Unix as well as
// Windows. So there IS a portable deterministic trigger for the unreadable case, and
// "no seam: a genuinely locked skill file fails the real gate" below uses it: the real `File.ReadAllText`,
// a real file it genuinely cannot read, the real `runCheck`.
//
// The seam is still needed and still used, for two things the lock cannot reach. There is NO natural
// trigger at all for an unexpected exception type — a defect nobody can provoke on demand — and that is
// acceptance criterion 3. And it lets a failure be aimed at one specific file inside a larger tree
// without locking anything process-wide, which is what makes the misattribution assertions cheap.

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

/// Holds `path` open with `FileShare.None` for the duration of `f`, so every other open of it fails.
/// This is the portable deterministic "cannot be read" the issue thought did not exist: a share-mode lock
/// is not a permission bit, .NET enforces it on Unix as well as Windows, and root cannot read through it.
let private whileLocked (path: string) (f: unit -> 'a) =
    use _lock = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)
    f ()

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
                // rule goes quiet, which is the state the old code reported as `passed`.
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

                // EVERY finding names the file that could not be read — not one of the wrappers that
                // merely READ it. A wrapper resolves its canonical target and reads that too, so a naive
                // producer attributes the throw to whichever file the SURFACE enumerated and tells the
                // reader to go fix `claude/passing/SKILL.md`, which is perfectly readable.
                Expect.all
                    findings
                    (fun finding -> isCanonicalPassingBody finding.SkillName)
                    (sprintf
                        "every finding names the canonical body that actually threw; got %A"
                        (findings |> List.map (fun f -> f.SkillName)))

                // One per surface: the canonical surface enumerated it, and each wrapper surface reached
                // it through its own wrapper. Three surfaces, three findings, one path.
                Expect.equal (List.length findings) 3 "one finding per surface that could not read it, and no more"

                Expect.equal
                    (findings |> List.map (fun f -> f.SurfaceId) |> List.distinct |> List.length)
                    3
                    "and they are three DIFFERENT surfaces, not one surface reported three times"

                let finding = List.head findings

                Expect.equal finding.Severity SkillParity.High "an unreadable FILE is a fact about the repository, reported at High"

                Expect.stringContains
                    finding.Message
                    "IOException"
                    "the finding names the exception TYPE, so the reader can tell an IO error from a permission error without re-running anything"

                Expect.stringContains
                    finding.Message
                    "synthetic injected read failure"
                    "the finding carries the underlying reason rather than a generic 'could not read'"

                Expect.isSome finding.CanonicalPath "the finding carries the offending path as evidence"

                // The reason is OS-supplied text and it lands in the COMMITTED report, so it must not
                // carry the absolute path of whichever checkout produced it.
                Expect.all
                    findings
                    (fun f -> not (f.Message.Contains root))
                    "no finding message embeds the temp-root path the run happened to use"

                // The wrapper surfaces say how they got there; the canonical surface enumerated it
                // directly and has nothing to add.
                let viaFindings =
                    findings |> List.filter (fun f -> f.Message.Contains "Reached from")

                Expect.equal
                    (List.length viaFindings)
                    2
                    "the two wrapper surfaces name the wrapper that routed to the unreadable body, so the reader can find the route as well as the file"
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

        // NO SEAM. This one drives the real `File.ReadAllText` over a real file it genuinely cannot read,
        // so the whole pipeline is proven without trusting the injection point at all.
        //
        // The issue reasoned that no portable deterministic trigger existed, and it ruled out the ones it
        // considered correctly — `ReadAllText` does not throw on invalid UTF-8, a directory named
        // `SKILL.md` is not returned by `Directory.GetFiles`, and permission bits are useless as root,
        // which is how CI runs. But a share-mode LOCK is neither a permission nor a platform quirk:
        // opening the file `FileShare.None` makes every other open fail, including one in the same
        // process, and .NET implements that on Unix as well as Windows. Verified on this repository's
        // runtime: `IOException: The process cannot access the file '…' because it is being used by
        // another process.` So the premise was too pessimistic for THIS half, and the seam is kept for the
        // half that really has no natural trigger — an unexpected exception type, which is by definition a
        // defect nobody can provoke on demand.
        test "no seam: a genuinely locked skill file fails the real gate" {
            let root = Feature168SkillParityFixtures.createTempRoot "fsgg-1093-locked"

            try
                let fixtureRoot = Path.Combine(root, "tree")
                SkillParity.createFixture fixtureRoot "passing"

                let request =
                    { SkillParity.defaultRequest fixtureRoot with
                        OutDir = Path.Combine(root, "out")
                        ReportPath = Path.Combine(root, "out", "report.md")
                        SummaryJsonPath = Path.Combine(root, "out", "summary.json")
                        SurfaceOverrides = [ "fixture-canonical", "canonical" ] }

                let surfaces = [ Path.Combine(fixtureRoot, "canonical", "passing", "SKILL.md") ]
                Expect.isTrue (File.Exists surfaces.Head) "the fixture wrote the canonical body"

                let canonicalSurface: SkillParity.SkillSurface list =
                    [ { SurfaceId = "fixture-canonical"
                        DisplayName = "fixture canonical"
                        // #1092: `RootPath` became `Roots` + `Selector`. `EverySkillBody` is the
                        // literal equivalent of what this surface got before — `fixture-canonical` had
                        // no branch in the old `filesForSurface`, so it fell through to the recursive
                        // glob of its declared root, unnarrowed.
                        Roots = [ "canonical" ]
                        Selector = SkillParity.EverySkillBody
                        Kind = SkillParity.Canonical
                        Agent = SkillParity.Repository
                        IsRequired = true
                        Notes = [] } ]

                // Green first, through the same real reader, so the red below is the lock and nothing else.
                let before = SkillParity.runCheck request
                Expect.isEmpty (unreadableFindings before) "unlocked, the same tree reports no unreadable surface"

                Expect.isNonEmpty
                    (SkillParity.inventorySkills request canonicalSurface)
                    "and the body really is inventoried when it can be read (non-vacuity for everything below)"

                let report = whileLocked surfaces.Head (fun () -> SkillParity.runCheck request)
                let findings = unreadableFindings report
                Expect.equal (List.length findings) 1 "the locked body is reported exactly once"

                let finding = List.head findings
                Expect.equal finding.Severity SkillParity.High "at High, so the default FailOnSeverity blocks"
                Expect.equal report.OverallStatus SkillParity.Failed "and the gate fails"

                Expect.stringContains
                    (normalize finding.SkillName)
                    "canonical/passing/SKILL.md"
                    "naming the file that could not be read"

                Expect.stringContains finding.Message "IOException" "and the real exception type the OS raised"

                Expect.isFalse
                    (finding.Message.Contains fixtureRoot)
                    "with the checkout's absolute path scrubbed out of the reason — this text is rendered into a COMMITTED report whose gate diffs it"

                // The compatibility entry point must not quietly re-open the hole under a new name.
                whileLocked surfaces.Head (fun () ->
                    Expect.throwsT<IOException>
                        (fun () -> SkillParity.inventorySkills request canonicalSurface |> ignore)
                        "inventorySkills RAISES on a file it cannot read rather than dropping it — two repository gates read this inventory and would otherwise pass vacuously over a skill nobody could read")

                // And unlocked it is green again, so the lock is what the assertions above measured.
                let after = SkillParity.runCheck request
                Expect.isEmpty (unreadableFindings after) "released, the tree is clean again"
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
                        // #1092: `RootPath` became `Roots` + `Selector`. `EverySkillBody` is the
                        // literal equivalent of what this surface got before — `fixture-canonical` had
                        // no branch in the old `filesForSurface`, so it fell through to the recursive
                        // glob of its declared root, unnarrowed.
                        Roots = [ "canonical" ]
                        Selector = SkillParity.EverySkillBody
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

        // N unreadable files under ONE surface must be N findings. `classifyFindings` groups by FindingId
        // and keeps one per group, and the id is category+surface+skill — but the skill NAME is precisely
        // what could not be read, so an id that did not carry the PATH would collapse every unreadable file
        // on a surface into one. Same trap the broken-target producer hit.
        //
        // The assertion is a COUNT against the files on disk, not "the ids are distinct": ids are distinct
        // by construction after `groupBy`, so that assertion can never fail and would pass on the very
        // regression it claims to guard.
        test "every unreadable file under one surface is its own finding" {
            let root = Feature168SkillParityFixtures.createTempRoot "fsgg-1093-dedupe"

            try
                let request = Feature168SkillParityFixtures.request root "all"

                let reader =
                    readerFailingOn
                        (fun path -> (normalize path).Contains "/canonical/")
                        (fun path -> IOException($"synthetic injected read failure for {path}") :> exn)

                let report = SkillParity.runCheckWith reader request

                // The oracle: how many canonical bodies the `all` fixture actually wrote. Counted from
                // disk rather than hard-coded, so adding a fixture case cannot quietly weaken this.
                let fixtureRoot = Path.Combine(request.OutDir, "_skill-parity-fixture")

                let unreadableFiles =
                    Directory.GetFiles(Path.Combine(fixtureRoot, "canonical"), "SKILL.md", SearchOption.AllDirectories)
                    |> Array.length

                Expect.isGreaterThan unreadableFiles 1 "the `all` fixture writes more than one canonical body (non-vacuity)"

                let canonicalSurfaceFindings =
                    unreadableFindings report
                    |> List.filter (fun finding -> finding.SurfaceId = "fixture-canonical")

                Expect.equal
                    (List.length canonicalSurfaceFindings)
                    unreadableFiles
                    "one finding per unreadable file on the canonical surface — a FindingId that dropped the path would collapse them all into one"

                Expect.equal
                    (canonicalSurfaceFindings |> List.map (fun f -> f.SkillName) |> List.distinct |> List.length)
                    unreadableFiles
                    "and each names a different file"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }
    ]
