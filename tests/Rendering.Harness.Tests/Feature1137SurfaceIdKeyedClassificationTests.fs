module Feature1137SurfaceIdKeyedClassificationTests

// Issue #1137 — what an entry IS must come off its surface's DECLARATION, never off the surface's id.
//
// #1092 established the rule for WHERE a surface looks: `filesForSurface` resolves `Roots` and narrows
// by `Selector`, and `SurfaceSelector`'s own comment states it — "this exists so that NOTHING about
// where a surface looks is keyed on `SurfaceId`". `readEntry` broke it one layer over, for WHAT the
// bodies found there are:
//
//     elif surface.Kind = Canonical || surface.SurfaceId = "ant-canonical" then CanonicalEntry
//
// The second disjunct was DEAD for the default set — `ant-canonical` declares `Kind = Canonical` — and
// that is exactly why #1092 did not catch it: every default-set measurement agrees with the rule. It
// was not dead in general. `--surface <id>=<path>` builds a surface whose id is whatever the operator
// TYPED, and `effectiveSurfaces` gives every override `Kind = Mixed`, so an override named
// `ant-canonical` classified its bodies `CanonicalEntry` while an override named anything else, over
// the same directory, did not. The run's answer depended on a string the operator chose.
//
// TWO MORE BRANCHES OF THE SAME DEFECT were removed with it, both in the wrapper-requirement rules:
// `requiresWrapper` listed `package-canonical`/`ant-canonical`/`fixture-canonical` by name (they are
// exactly the canonical surfaces whose declared `Agent` is not `GeneratedProduct`), and
// `missingWrapperFindings` carried an `entry.SurfaceId = "ant-canonical" && surfaceId = "claude"`
// exemption left over from before #1080/#1082, when the Ant canonical body WAS
// `.claude/skills/fs-gg-ant-design/SKILL.md` and satisfied the requirement by being the wrapper.
//
// THE TESTS ARE WRITTEN OVER THE RULE, quantified over `discoverDefaultSurfaces`, not over the id that
// happened to be wrong. A test naming `ant-canonical` would pass on the day a second id-keyed branch is
// added for some other surface — the failure mode this item is about. `theRuleHolds` below is that
// test; the `ant-canonical` cases are the issue's own demonstration, kept because they are the measured
// evidence, and each carries a non-vacuity control.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private declaredSurfaces () = SkillParity.discoverDefaultSurfaces repositoryRoot

let private defaultEntries () =
    SkillParity.inventorySkills (Feature168SkillParityFixtures.repositoryRequest repositoryRoot) (declaredSurfaces ())

/// A request whose report and summary land in a throwaway directory. `docs/reports/skills-parity.md`
/// is a COMMITTED artifact with a CI gate on its diff, so a test must never regenerate it in place.
let private requestWithOverrides (outRoot: string) overrides =
    { SkillParity.defaultRequest repositoryRoot with
        OutDir = Path.Combine(outRoot, "out")
        ReportPath = Path.Combine(outRoot, "out", "report.md")
        SummaryJsonPath = Path.Combine(outRoot, "out", "summary.json")
        SurfaceOverrides = overrides }

/// Everything a run says about what its bodies ARE, with the surface's IDENTITY projected out.
///
/// `SurfaceId` is excluded on purpose, and its exclusion is the experiment: two runs over the SAME
/// directory under two different surface names must agree on every one of these, and can only disagree
/// on the name itself. Include the id and the comparison is trivially false; leave the classification
/// out and it is trivially true.
let private classification (report: SkillParity.ParityReport) =
    report.CanonicalSourceCount,
    report.WrapperCount,
    (report.Findings
     |> List.map (fun finding -> finding.Category, finding.SkillName, finding.CanonicalPath, finding.WrapperPath)
     |> List.sort)

/// What a surface's DECLARATION predicts its bodies will be classified as, restated here independently
/// of `readEntry` so the assertion is a claim about the rule rather than a copy of the implementation.
let private predictedKind (surface: SkillParity.SkillSurface) (entry: SkillParity.SkillEntry) =
    if entry.EntryKind = SkillParity.CommandEntry then
        // A `speckit-*` body is classified by its NAME, which is a property of the file and not of the
        // surface. It is outside this rule's subject; the rule is about what the SURFACE contributes.
        SkillParity.CommandEntry
    elif surface.Kind = SkillParity.Canonical then
        SkillParity.CanonicalEntry
    elif entry.WrapperTarget.IsSome then
        SkillParity.WrapperEntry
    else
        SkillParity.WrapperOnlyEntry

// The pre-#1137 text of the branch this item removed, used as the non-vacuity control for the source
// guard below. A guard whose pattern no longer matches the defect it was written for is a guard that
// passes because it broke, and nothing in a green run tells those two apart.
let private removedBranchSample =
    "            elif surface.Kind = Canonical || surface.SurfaceId = \"ant-canonical\" then"

/// A QUALIFIED `SurfaceId` access compared against a string literal, in either direction.
///
/// The qualifier is what makes this precise: F# spells record CONSTRUCTION and EQUALITY with the same
/// `=`, so a bare `SurfaceId = "codex-local"` is a declaration in `discoverDefaultSurfaces` and must
/// stay legal. A comparison always reads a field off something — `entry.SurfaceId`, `surface.SurfaceId`
/// — so the leading `.` separates the two without a whitelist of line numbers to keep up to date.
let private literalSurfaceIdComparison =
    Regex(@"\.SurfaceId\s*(=|<>)\s*""", RegexOptions.Compiled)

let private skillParitySourcePath =
    Path.Combine(repositoryRoot, "tools", "Rendering.Harness", "SkillParity.fs")

/// Comment lines are excluded, and deliberately: this item's own commentary QUOTES the branch it
/// deleted, and a guard that could not tell a quotation from a use would forbid explaining the fix.
let private isComment (line: string) =
    let trimmed = line.TrimStart()
    trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith("///", StringComparison.Ordinal)

[<Tests>]
let surfaceIdKeyedClassificationTests =
    testList "Feature1137 entry classification is declared, never keyed on SurfaceId" [

        // ---------- Acceptance criterion 2: the default set is UNCHANGED ----------

        test "ant-canonical's bodies are still CanonicalEntry, by declaration rather than by name" {
            let entries = defaultEntries ()

            let antEntries =
                entries |> List.filter (fun entry -> entry.SurfaceId = "ant-canonical")

            // Non-vacuity: there is something to classify. Without this, an inventory that resolved
            // `ant-canonical` to nothing at all would satisfy the assertion below by having no subject.
            Expect.isNonEmpty antEntries "the ant-canonical surface still inventories at least one body"

            for entry in antEntries do
                Expect.equal
                    entry.EntryKind
                    SkillParity.CanonicalEntry
                    (sprintf "'%s' is still canonical — the DECLARATION says so (Kind = Canonical)" entry.Path)

            // And the reason is the declaration, not the removed disjunct: assert the fact the branch
            // now rests on, so a future edit that flipped `ant-canonical` to a non-canonical Kind while
            // leaving this test green would have to trip here instead.
            let antSurface =
                declaredSurfaces () |> List.find (fun surface -> surface.SurfaceId = "ant-canonical")

            Expect.equal
                antSurface.Kind
                SkillParity.Canonical
                "and it is canonical because its declaration says Kind = Canonical, which is what readEntry now reads"
        }

        // ---------- The RULE, quantified over every declared surface ----------

        test "every entry's kind is what its surface's declaration predicts, for every declared surface" {
            let surfaces = declaredSurfaces ()
            let entries = defaultEntries ()

            let bySurface =
                surfaces |> List.map (fun surface -> surface.SurfaceId, surface) |> Map.ofList

            Expect.isNonEmpty entries "non-vacuity: the default inventory resolved bodies to judge"

            for entry in entries do
                match Map.tryFind entry.SurfaceId bySurface with
                | None ->
                    failtestf "entry '%s' claims surface '%s', which no declaration produced" entry.Path entry.SurfaceId
                | Some surface ->
                    Expect.equal
                        entry.EntryKind
                        (predictedKind surface entry)
                        (sprintf
                            "'%s' on surface '%s' (Kind = %A): its classification must follow the surface's declaration"
                            entry.Path
                            surface.SurfaceId
                            surface.Kind)
        }

        // ---------- Acceptance criterion 3: the id no longer decides ----------

        test "two overrides over the SAME directory classify identically, whatever the operator named them" {
            // The issue's demonstration. Before #1137 the left run reported a canonical source (and the
            // wrapper-requirement findings that follow from one) and the right run did not, over byte-for-byte
            // the same directory — the only difference being the string the operator typed after `--surface`.
            let outRoot = Feature168SkillParityFixtures.createTempRoot "fsgg-1137-override-name"

            try
                let directory = "docs/product/ant-design/skill"

                let asAnt = SkillParity.runCheck (requestWithOverrides outRoot [ "ant-canonical", directory ])
                let asOther = SkillParity.runCheck (requestWithOverrides outRoot [ "some-other-surface", directory ])

                // Non-vacuity: both runs actually READ the directory. Two runs that each resolved to
                // nothing would agree here for the least interesting reason there is.
                Expect.isNonEmpty asAnt.Findings "non-vacuity: the ant-named override resolved a body and judged it"
                Expect.isNonEmpty asOther.Findings "non-vacuity: the differently-named override resolved a body and judged it"

                Expect.equal
                    (classification asAnt)
                    (classification asOther)
                    "an override's bodies are classified by its declaration; naming it `ant-canonical` buys it nothing"

                // And the surfaces really were distinct — otherwise the equality above is comparing a run
                // with itself.
                Expect.notEqual
                    (asAnt.SupportedSurfaces |> List.map (fun surface -> surface.SurfaceId))
                    (asOther.SupportedSurfaces |> List.map (fun surface -> surface.SurfaceId))
                    "control: the two runs really did declare different surface ids"
            finally
                Feature168SkillParityFixtures.deleteTempRoot outRoot
        }

        test "an override named after a canonical surface does not inherit that surface's wrapper requirement" {
            // The `requiresWrapper` half of the same defect: `entry.SurfaceId = "ant-canonical"` made an
            // override's bodies demand agent wrappers, and the identically-rooted override next to it did not.
            let outRoot = Feature168SkillParityFixtures.createTempRoot "fsgg-1137-override-requires"

            try
                let directory = "docs/product/ant-design/skill"

                let missingWrapperOf (report: SkillParity.ParityReport) =
                    report.Findings
                    |> List.filter (fun finding -> finding.Category = SkillParity.MissingWrapper)

                let asAnt = SkillParity.runCheck (requestWithOverrides outRoot [ "ant-canonical", directory ])
                let asOther = SkillParity.runCheck (requestWithOverrides outRoot [ "some-other-surface", directory ])

                Expect.equal
                    (missingWrapperOf asAnt |> List.length)
                    (missingWrapperOf asOther |> List.length)
                    "the wrapper requirement follows the surface's declaration, not the name the operator typed"
            finally
                Feature168SkillParityFixtures.deleteTempRoot outRoot
        }

        // ---------- Acceptance criterion 4: it cannot come back ----------

        test "no branch in SkillParity.fs compares a SurfaceId against a string literal" {
            // Non-vacuity FIRST: the pattern still recognises the defect it was written for. A guard
            // whose regex has rotted is indistinguishable, in a green run, from a guard that passed.
            Expect.isTrue
                (literalSurfaceIdComparison.IsMatch removedBranchSample)
                "control: the guard's pattern matches the pre-#1137 branch it exists to forbid"

            // And it does NOT match a record-field DECLARATION, which is how `discoverDefaultSurfaces`
            // names its surfaces and must stay legal.
            Expect.isFalse
                (literalSurfaceIdComparison.IsMatch "        [ { SurfaceId = \"codex-local\"")
                "control: the guard does not forbid declaring a surface's id"

            let offenders =
                File.ReadAllLines skillParitySourcePath
                |> Array.indexed
                |> Array.filter (fun (_, line) -> not (isComment line) && literalSurfaceIdComparison.IsMatch line)
                |> Array.map (fun (index, line) -> sprintf "  SkillParity.fs:%d  %s" (index + 1) (line.Trim()))
                |> Array.toList

            Expect.isEmpty
                offenders
                ("a surface's meaning must be read off its declaration, never off its id (#1092/#1137). Offending line(s):\n"
                 + String.concat "\n" offenders)
        }
    ]
