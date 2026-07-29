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
// added for some other surface — the failure mode this item is about. The `ant-canonical` cases are the
// issue's own demonstration, kept because they are the measured evidence, and each carries a
// non-vacuity control.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private declaredSurfaces = lazy (SkillParity.discoverDefaultSurfaces repositoryRoot)

/// A full default inventory is a real filesystem walk of every declared root, so it is taken ONCE for
/// the file rather than per test.
let private defaultEntries =
    lazy
        (SkillParity.inventorySkills
            (Feature168SkillParityFixtures.repositoryRequest repositoryRoot)
            (declaredSurfaces.Force()))

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

/// What a surface's DECLARATION predicts its bodies will be classified as.
///
/// Restated here independently of `readEntry`, so the assertion is a claim about the RULE rather than a
/// copy of the implementation — including the command case, which is derived from the skill's own NAME
/// (a property of the file, outside this rule's subject) rather than read back off the `EntryKind`
/// under test. Reading `EntryKind` there would make every command entry pass tautologically.
let private predictedKind (surface: SkillParity.SkillSurface) (entry: SkillParity.SkillEntry) =
    if entry.SkillName.StartsWith("speckit-", StringComparison.OrdinalIgnoreCase) then
        SkillParity.CommandEntry
    elif surface.Kind = SkillParity.Canonical then
        SkillParity.CanonicalEntry
    elif entry.WrapperTarget.IsSome then
        SkillParity.WrapperEntry
    else
        SkillParity.WrapperOnlyEntry

// ---------------------------------------------------------------------------------------------
// The source guard (acceptance criterion 4)
// ---------------------------------------------------------------------------------------------

let private skillParitySourcePath =
    Path.Combine(repositoryRoot, "tools", "Rendering.Harness", "SkillParity.fs")

let private skillParitySource () = File.ReadAllLines skillParitySourcePath

/// Comment lines are excluded from both guards below, and deliberately: this item's own commentary
/// QUOTES the branch it deleted, and a guard that could not tell a quotation from a use would forbid
/// explaining the fix.
///
/// LIMITATION, stated rather than hidden: this recognises `//` line comments only. `SkillParity.fs`
/// contains no `(* … *)` block comments, and a block comment quoting a surface id would be reported
/// here as an offender. That fails in the SAFE direction — a false alarm someone must look at, not a
/// silent pass — which is the right way for a guard to be wrong.
let private isComment (line: string) = line.TrimStart().StartsWith("//", StringComparison.Ordinal)

/// A record-field DECLARATION of a surface id — `SurfaceId = "codex-local"`, with whatever list and
/// record punctuation opens the line. F# spells construction and equality with the same `=`, so these
/// must stay legal while comparisons must not.
let private surfaceIdDeclaration =
    Regex(@"^[\[\{\s]*SurfaceId\s*=\s*""", RegexOptions.Compiled)

/// A QUALIFIED `SurfaceId` access used to DECIDE something against a string literal.
///
/// The qualifier is what makes this precise: a bare `SurfaceId = "…"` is a declaration (above), while
/// a comparison always reads the field off something — `entry.SurfaceId`, `surface.SurfaceId`. The
/// alternation covers the spellings an F# author would actually reach for after `=` is forbidden:
/// `.Equals("…")`, `.StartsWith("…")`, `.Contains("…")`, and `match x.SurfaceId with | "…"`.
let private literalSurfaceIdComparison =
    Regex(
        @"\.SurfaceId\s*(=|<>)\s*""|\.SurfaceId\.(Equals|StartsWith|EndsWith|Contains)\s*\(\s*""|match\s+[\w.]*\.SurfaceId\s+with",
        RegexOptions.Compiled
    )

/// The pre-#1137 text of the branch this item removed, and three plausible re-introductions of it.
/// A guard whose pattern no longer matches the defect it was written for is a guard that passes
/// because it broke, and nothing in a green run tells those two apart.
let private removedBranchSamples =
    [ "            elif surface.Kind = Canonical || surface.SurfaceId = \"ant-canonical\" then"
      "        let antCanonicalSelfExposed = entry.SurfaceId = \"ant-canonical\" && surfaceId = \"claude\""
      "        if entry.SurfaceId.Equals(\"ant-canonical\", StringComparison.Ordinal) then"
      "        match entry.SurfaceId with" ]

/// Every line that still spells a DEFAULT surface id as a string literal outside a declaration, with
/// the reason each one is not an id-keyed decision.
///
/// This is the half `literalSurfaceIdComparison` cannot see, and #1137's review is why it exists: a
/// pattern that recognises USES can always be walked around (bind the field to a local, put the
/// literals in a list, compare in a helper), so the second guard fixes the ids themselves and pins the
/// residue EXACTLY. A new line joining this set fails the test, whatever spelling it used.
///
/// Matched on trimmed line TEXT rather than line number, so inserting code above them does not churn
/// this list.
let private knownLiteralSurfaceIdLines =
    set
        [
          // `agentToken` — the report's `Agent` column. The token for the `Claude` agent happens to be
          // spelled the same as the `claude` surface's id; it is not read as one.
          "| Claude -> \"claude\""

          // A fixture surface's declared ROOT — the directory named `claude/` under the synthetic tree,
          // not a surface id.
          "Roots = [ \"claude\" ]"

          // `createFixture` — a path segment of the synthetic wrapper it writes.
          "createWrapper (full [ \"claude\"; \"passing\"; \"SKILL.md\" ]) \"fs-gg-fixture-passing\" \"Aligned fixture skill.\" \"../../canonical/passing/SKILL.md\""

          // ---- #1143: the wrapper-requirement TARGETS, still a literal list ----------------------
          //
          // These four lines in `missingWrapperFindings` and one in `manifestCoverageFindings` are the
          // same family as what #1137 removed — a fact that should be declared, restated as literals —
          // but a different question: which surfaces are the TARGETS of the wrapper requirement, not
          // what an entry IS. Fixing them moves findings outside the default set (measured on #1143:
          // a `--fixture all` run attributes its `missing-wrapper` findings to `claude` and
          // `codex-local`, which are in no Supported Surfaces table that run printed) and needs an
          // explicit decision about `fixture-optional`. They are tracked, not forgotten, and this set
          // must SHRINK when #1143 lands.
          "let codexNames = wrapperNames \"codex-local\" + wrapperNames \"fixture-codex\""
          "let claudeNames = wrapperNames \"claude\" + wrapperNames \"fixture-claude\""
          "[ \"codex-local\", codexNames"
          "\"claude\", claudeNames ]"
          "let roots = [ \"claude\", \".claude\"; \"codex-local\", \".agents\" ]" ]

[<Tests>]
let surfaceIdKeyedClassificationTests =
    testList "Feature1137 entry classification is declared, never keyed on SurfaceId" [

        // ---------- Acceptance criterion 2: the default set is UNCHANGED ----------

        test "ant-canonical's bodies are still CanonicalEntry, by declaration rather than by name" {
            let antEntries =
                defaultEntries.Force()
                |> List.filter (fun entry -> entry.SurfaceId = "ant-canonical")

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
                declaredSurfaces.Force()
                |> List.find (fun surface -> surface.SurfaceId = "ant-canonical")

            Expect.equal
                antSurface.Kind
                SkillParity.Canonical
                "and it is canonical because its declaration says Kind = Canonical, which is what readEntry now reads"
        }

        // ---------- The RULE, quantified over every declared surface ----------

        test "every entry's kind is what its surface's declaration predicts, for every declared surface" {
            let entries = defaultEntries.Force()

            let bySurface =
                declaredSurfaces.Force()
                |> List.map (fun surface -> surface.SurfaceId, surface)
                |> Map.ofList

            Expect.isNonEmpty entries "non-vacuity: the default inventory resolved bodies to judge"

            for entry in entries do
                match Map.tryFind entry.SurfaceId bySurface with
                | None ->
                    // Also the assertion `requiresWrapper`'s `Option.defaultValue false` rests on: an
                    // entry whose surface the run did not declare is a state the inventory cannot
                    // produce, and this is where that stops being an assumption.
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

        // ---------- The wrapper-requirement rule, pinned as DATA rather than argued ----------

        test "the surfaces demanding wrapper exposure are exactly the canonical, non-generated-product ones" {
            // `requiresWrapper` used to name `package-canonical`/`ant-canonical`/`fixture-canonical`.
            // The claim this change rests on is that those are EXACTLY the canonical surfaces whose
            // declared `Agent` is not `GeneratedProduct`. That claim is asserted here rather than left
            // in a commit message, because `Agent` had no other reader than the report's Agent column
            // before this item — a surface added with the wrong one now trips a test.
            let demanding =
                declaredSurfaces.Force()
                |> List.filter (fun surface ->
                    surface.Kind = SkillParity.Canonical && surface.Agent <> SkillParity.GeneratedProduct)
                |> List.map (fun surface -> surface.SurfaceId)
                |> Set.ofList

            Expect.equal
                demanding
                (set [ "package-canonical"; "ant-canonical" ])
                "the declaration-derived rule selects exactly the ids the literal list used to name"

            // The exclusion is the risky half, so it gets its own assertion rather than riding on the
            // set equality above: `template-canonical` is exempt BECAUSE it is declared
            // `Agent = GeneratedProduct`, not because it was left off a list.
            let templateSurface =
                declaredSurfaces.Force()
                |> List.find (fun surface -> surface.SurfaceId = "template-canonical")

            Expect.equal
                templateSurface.Agent
                SkillParity.GeneratedProduct
                "template-canonical's exemption is a declared fact — its bodies ship into a generated workspace"
        }

        // ---------- Acceptance criterion 3: the id no longer decides ----------

        test "two overrides over the SAME directory are classified and required identically" {
            // The issue's demonstration. Before #1137 the left run reported a canonical source — and
            // the wrapper-requirement findings that follow from one — while the right run did not, over
            // byte-for-byte the same directory, the only difference being the string the operator typed
            // after `--surface`.
            let outRoot = Feature168SkillParityFixtures.createTempRoot "fsgg-1137-override-name"

            try
                let directory = "docs/product/ant-design/skill"
                let body = "docs/product/ant-design/skill/SKILL.md"

                let asAnt = SkillParity.runCheck (requestWithOverrides outRoot [ "ant-canonical", directory ])
                let asOther = SkillParity.runCheck (requestWithOverrides outRoot [ "some-other-surface", directory ])

                // Non-vacuity, and it must name the BODY. `Findings` being non-empty proves nothing on
                // its own: an override is `IsRequired = true`, so a root resolving to zero files emits
                // an empty-required-surface finding and the list is non-empty either way.
                for label, report in [ "ant-named", asAnt; "differently-named", asOther ] do
                    Expect.isTrue
                        (report.Findings
                         |> List.exists (fun finding ->
                             finding.WrapperPath = Some body || finding.CanonicalPath = Some body))
                        (sprintf "non-vacuity: the %s override resolved the real body and judged it" label)

                Expect.equal
                    (classification asAnt)
                    (classification asOther)
                    "an override's bodies are classified by its declaration; naming it `ant-canonical` buys it nothing"

                // The `requiresWrapper` half, called out separately because the equality above would
                // also hold if BOTH runs wrongly demanded wrappers. Measured on the pre-#1137 code:
                // `asAnt` produced ONE missing-wrapper finding and `asOther` none — one rather than two
                // because the `antCanonicalSelfExposed` branch was suppressing the `claude` side, which
                // is the second removed branch showing up in the same experiment. Both are 0 now.
                //
                // That this producer still FIRES where it should is proven next door, on trees built to
                // make it fire: `Feature223SymbologyParityTests` asserts `MissingWrapper` on both agent
                // surfaces for a product skill whose alias is absent.
                let missingWrapperCount (report: SkillParity.ParityReport) =
                    report.Findings
                    |> List.filter (fun finding -> finding.Category = SkillParity.MissingWrapper)
                    |> List.length

                Expect.equal
                    (missingWrapperCount asAnt, missingWrapperCount asOther)
                    (0, 0)
                    "an override named after a canonical surface does not inherit that surface's wrapper requirement"

                // And the two runs really were distinct — otherwise the equality above compares a run
                // with itself.
                Expect.notEqual
                    (asAnt.SupportedSurfaces |> List.map (fun surface -> surface.SurfaceId))
                    (asOther.SupportedSurfaces |> List.map (fun surface -> surface.SurfaceId))
                    "control: the two runs really did declare different surface ids"
            finally
                Feature168SkillParityFixtures.deleteTempRoot outRoot
        }

        // ---------- Acceptance criterion 4: it cannot come back ----------

        test "no branch in SkillParity.fs decides anything by comparing a SurfaceId to a literal" {
            // Non-vacuity FIRST: the pattern still recognises the defect it was written for, and the
            // re-spellings someone would reach for once `=` is forbidden.
            for sample in removedBranchSamples do
                Expect.isTrue
                    (literalSurfaceIdComparison.IsMatch sample)
                    (sprintf "control: the guard's pattern matches `%s`" (sample.Trim()))

            // And it does NOT match a record-field DECLARATION, which is how `discoverDefaultSurfaces`
            // names its surfaces and must stay legal.
            Expect.isFalse
                (literalSurfaceIdComparison.IsMatch "        [ { SurfaceId = \"codex-local\"")
                "control: the guard does not forbid declaring a surface's id"

            let offenders =
                skillParitySource ()
                |> Array.indexed
                |> Array.filter (fun (_, line) -> not (isComment line) && literalSurfaceIdComparison.IsMatch line)
                |> Array.map (fun (index, line) -> sprintf "  SkillParity.fs:%d  %s" (index + 1) (line.Trim()))
                |> Array.toList

            Expect.isEmpty
                offenders
                ("a surface's meaning must be read off its declaration, never off its id (#1092/#1137). Offending line(s):\n"
                 + String.concat "\n" offenders)
        }

        test "the surface ids still written as literals in SkillParity.fs are exactly the known, tracked set" {
            // The half a use-pattern cannot see. `literalSurfaceIdComparison` recognises SPELLINGS, and
            // any spelling can be walked around — bind the field to a local, put the ids in a list,
            // compare inside a helper. This one fixes the IDS and pins the residue, so a new literal
            // fails the test whatever shape the code around it takes.
            let ids =
                declaredSurfaces.Force() |> List.map (fun surface -> surface.SurfaceId)

            // Non-vacuity: there are ids to look for, and they are the ones the repository declares
            // rather than a hand-written list that would go stale when a surface is added.
            Expect.isNonEmpty ids "non-vacuity: the repository declares surfaces to search for"

            let quoted = ids |> List.map (fun id -> "\"" + id + "\"")

            let present =
                skillParitySource ()
                |> Array.map (fun line -> line.Trim())
                |> Array.filter (fun line ->
                    not (isComment line)
                    && not (surfaceIdDeclaration.IsMatch line)
                    && quoted |> List.exists (fun token -> line.Contains(token, StringComparison.Ordinal)))
                |> Set.ofArray

            let unexpected = Set.difference present knownLiteralSurfaceIdLines
            let departed = Set.difference knownLiteralSurfaceIdLines present

            Expect.isEmpty
                unexpected
                ("a surface id was written as a literal somewhere new. Either derive it from the surface's "
                 + "declaration (#1092/#1137), or — if it is genuinely not an id-keyed decision — add it to "
                 + "`knownLiteralSurfaceIdLines` with the reason. New line(s):\n"
                 + String.concat "\n" (List.ofSeq unexpected))

            Expect.isEmpty
                departed
                ("a line in `knownLiteralSurfaceIdLines` is no longer in `SkillParity.fs`. If #1143 (or a "
                 + "refactor) removed it, delete it from that set — the set is a debt ledger and must "
                 + "shrink, not accumulate entries nothing checks. Missing line(s):\n"
                 + String.concat "\n" (List.ofSeq departed))
        }
    ]
