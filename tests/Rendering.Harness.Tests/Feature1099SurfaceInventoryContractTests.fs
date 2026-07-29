module Feature1099SurfaceInventoryContractTests

// Issue #1099 — the Required Inventory table in
// `specs/168-skill-parity-evidence/contracts/skill-surface-inventory.md` is a SECOND COPY of the
// surface declaration, published in a file named `contracts/`, that nothing read and nothing diffed.
//
// This is the #1092 shape one layer out. #1092 fixed a resolver that ignored its own declaration;
// the document that is supposed to BE the contract kept restating that declaration in prose, and
// drifted. MEASURED against `discoverDefaultSurfaces` on `19af8b59`, FOUR of the six rows were wrong
// — one more than the issue counted, because it read `spec-kit-command`'s row as an omission in the
// sibling document rather than as a wrong `Root` cell here:
//
//     surface id          document said                                     resolver reads
//     codex-local         .agents/skills                                    .agents/skills                  ok
//     claude              .claude/skills                                    .claude/skills                  ok
//     package-canonical   src/*/skill                                       src        (area-skill-bodies)   WRONG
//     template-canonical  template/**/skill and template/product-skills     template   (non-mirrored-bodies) WRONG
//     ant-canonical       .claude/skills/fs-gg-ant-design/SKILL.md          docs/product/ant-design/skill/SKILL.md
//                                                                                                            WRONG
//     spec-kit-command    .agents/skills/speckit-*, .claude/skills/speckit-* .agents/skills, .claude/skills
//                                                                                      (command-wrappers)    WRONG
//
// Three of the four are the same confusion the `Root` column could not survive before #1092: the
// document wrote the SELECTOR into the root cell (`src/*/skill`, `speckit-*`, `template/**/skill`)
// and one wrote a path that has not been the canonical body since #1082. Since #1092 both halves are
// declared data, so the table finally has something mechanical to be checked against.
//
// ONE CLAIM IN THE ISSUE IS FALSE, and correcting it is part of landing it. #1099's body says of
// `template-canonical` that "`template/product-skills` does not exist in the tree". It does, and it
// did at `e2d860bc`, the commit the issue itself checked against: it was added in #991/#999 and holds
// fifteen canonical bodies today. That row is still wrong, for the reason the rest of the table is —
// it publishes prose in the column the report emits as `Root`, and it omits that the ADR-0011 mirror
// roots under `template/base/.agents|.claude|.codex/skills/` are SUBTRACTED, which is the whole
// content of the `non-mirrored-bodies` selector. It is not wrong because it named a directory that
// was never there.
//
// CORRECTING THE ROWS IS NOT THE FIX. Acceptance criterion 2 says so outright: without a test the
// rows are re-corrected today and stale again on the next surface change, which is exactly how they
// got here. So these tests re-derive the surfaces from the CODE at runtime and the rows from the
// DOCUMENT at runtime and compare, failing in BOTH directions — editing the table alone fails, and
// changing `discoverDefaultSurfaces` alone fails too. A test that asserted today's six correct rows
// would pass on the day a seventh surface is added with no row at all, which is this item's whole
// complaint.
//
// The comparison is factored out so the green assertion can carry a real non-vacuity control: an
// empty mismatch list is equally true of a comparison that parsed nothing, so every green case below
// is paired with a PERTURBED surface list that must be rejected.
//
// #1136 — THE COMPARISON NOW LIVES IN `SurfaceContractSupport`, and this file no longer has one of
// its own. It was `let private mismatches` over a `type private InventoryRow` here, which is private
// to THIS module, so `Feature1111CliSurfaceListContractTests` could not call it and implemented the
// same comparison a second time. Two consequences of that are closed by the move: the shared
// `disagreements` compares EVERY entry for a surface id rather than the first, so a repeated row is
// reported here too (#1111's review found that hole and fixed it only in its own file), and the
// "declared root missing from disk" verdict now names the generated-view remedy instead of blaming
// the document. What stays here is what is genuinely this document's: reading a Markdown TABLE, and
// the rules for its own cells.

open System
open System.IO
open Expecto
open Rendering.Harness
open SurfaceContractSupport

let private contractPath =
    Path.Combine(repositoryRoot, "specs", "168-skill-parity-evidence", "contracts", "skill-surface-inventory.md")

/// This document's half of the shared comparison. `ComparesKind` is TRUE: the Required Inventory
/// table publishes a `Kind` column, and #1099 pinned it. The perturbation control below proves this
/// flag is on, so it cannot be flipped off and delete a check with nothing turning red.
let private inventorySubject: RestatementSubject =
    { Document = "the Required Inventory table"
      Entry = "row"
      ComparesKind = true }

/// The one comparison, in the one place both files call. This is a projection onto its argument
/// shape and nothing else — there is no second comparison in this file.
let private mismatches (table: ParsedRestatements) (surfaces: SkillParity.SkillSurface list) =
    disagreements inventorySubject table surfaces

// ---------------------------------------------------------------------------------------------
// Reading the document
// ---------------------------------------------------------------------------------------------

/// The cells of a Markdown table row, trimmed, with the leading/trailing pipe padding dropped.
let private cells (line: string) =
    line.Trim().Trim('|').Split('|') |> Array.map (fun cell -> cell.Trim()) |> Array.toList

let private isSeparatorRow (line: string) =
    cells line |> List.forall (fun cell -> cell.Length > 0 && cell |> Seq.forall (fun c -> c = '-' || c = ':'))

/// The first Markdown table beneath `heading`, as header cells plus data rows. Stops at the next
/// heading of any level, so a later table in the same section cannot be mistaken for this one.
let private tableUnder (heading: string) =
    let lines = File.ReadAllLines contractPath |> Array.toList

    let rec after rest =
        match rest with
        | (line: string) :: tail when line.Trim() = heading -> Some tail
        | _ :: tail -> after tail
        | [] -> None

    match after lines with
    | None -> None
    | Some body ->
        let section = body |> List.takeWhile (fun line -> not (line.StartsWith("#", StringComparison.Ordinal)))

        let table =
            section
            |> List.skipWhile (fun line -> not (line.TrimStart().StartsWith("|", StringComparison.Ordinal)))
            |> List.takeWhile (fun line -> line.TrimStart().StartsWith("|", StringComparison.Ordinal))

        match table with
        | header :: separator :: rows when isSeparatorRow separator -> Some(cells header, rows |> List.map cells)
        | _ -> None

let private columnIndex (header: string list) (name: string) =
    header |> List.tryFindIndex (fun cell -> cell.Trim().Equals(name, StringComparison.OrdinalIgnoreCase))

/// The Required Inventory table, projected onto the four columns this contract is about. Read by
/// column NAME rather than position, so reordering or inserting a column does not silently shift
/// what is compared.
///
/// #1136 REVIEW — WHAT THIS MUST NOT DO IS SKIP, which is the rule #1111 already lived by and this
/// file did not. Every data row whose `Surface id` cell is not exactly one code span, and nothing
/// else, is reported as UNREADABLE rather than dropped. Measured on the real document before this
/// was fixed: appending
///
///     | legacy-speckit-wrapper | `wrapper` | `.agents/skills/speckit-*` | `agent-wrappers` | … |
///
/// left the whole file GREEN. A prose id, a glob root — one of the exact pre-#1092 shapes #1099
/// exists to stop — and a surface the resolver does not declare, and none of it was reported,
/// because `List.choose` deleted the row before any rule could see it. The row-level glob, prose
/// and root-resolution rules below cannot catch it either: they iterate the rows that PARSED.
let private inventoryRows () =
    match tableUnder "## Required Inventory" with
    | None -> None
    | Some(header, rows) ->
        match columnIndex header "Surface id", columnIndex header "Kind", columnIndex header "Roots", columnIndex header "Selector" with
        | Some idIndex, Some kindIndex, Some rootsIndex, Some selectorIndex ->
            let cell (row: string list) index = if index < List.length row then List.item index row else ""

            let read row : Result<SurfaceRestatement, string> =
                let idCell = cell row idIndex

                match singleSpan idCell with
                | Some surfaceId when residue idCell = "" ->
                    Ok
                        { SurfaceId = surfaceId
                          Kind = singleSpan (cell row kindIndex)
                          Roots = spans (cell row rootsIndex)
                          Selector = singleSpan (cell row selectorIndex) }
                | _ ->
                    Error(
                        sprintf
                            "row %A publishes a `Surface id` cell of %A; the id is one code span and the whole cell"
                            (String.concat " | " row)
                            idCell)

            let parsed = rows |> List.map read

            Some
                { Entries = parsed |> List.choose (function Ok row -> Some row | Error _ -> None)
                  Unreadable = parsed |> List.choose (function Error problem -> Some problem | Ok _ -> None) }
        | _ -> None

let private remedy =
    "Correct the row, or the declaration, so they agree. This table is the contract: nothing regenerates it, so #1099 pins it instead."

[<Tests>]
let surfaceInventoryContractTests =
    testList "Feature1099 the Required Inventory table and discoverDefaultSurfaces say the same thing" [

        // ---------- Acceptance criteria 1 and 2: the table agrees, and cannot quietly stop ----------

        test "every row of the Required Inventory table agrees with the surface it restates" {
            let table =
                match inventoryRows () with
                | Some table -> table
                | None ->
                    failtestf
                        "non-vacuity: no Required Inventory table with `Surface id`, `Kind`, `Roots` and `Selector` columns was parsed out of %s. This test compares that table with the code, so an unparseable table is a FAILURE and never a pass — a table nothing can read is the same fail-open as a table nothing checks"
                        contractPath

            let surfaces = declaredSurfaces ()

            Expect.isGreaterThanOrEqual
                (List.length table.Entries + List.length table.Unreadable)
                6
                "non-vacuity: the table's data rows were located and are not an empty list"

            Expect.isGreaterThanOrEqual
                (List.length surfaces)
                6
                "non-vacuity: the repository declares at least the six surfaces this contract enumerates"

            match mismatches table surfaces with
            | [] -> ()
            | problems -> failtestf "the contract and the resolver disagree:\n  %s\n\n%s" (String.concat "\n  " problems) remedy
        }

        test "the comparison rejects a surface list the table does not describe" {
            // THE control for the assertion above. "No mismatches" is equally true of a comparison
            // that parsed nothing, compared nothing, or read a column that is always absent — the
            // fail-open one level up, and the reason a corrected-but-unpinned table is not the fix.
            //
            // Each perturbation below is a divergence that has ACTUALLY happened to this document:
            // a root moved (#1082), a selector written into the root cell (#1092), and a surface
            // added with no row (the shape #1099 exists to stop).
            let table =
                match inventoryRows () with
                | Some table -> table
                | None -> failtestf "non-vacuity: the Required Inventory table must parse out of %s" contractPath

            let surfaces = declaredSurfaces ()

            Expect.isEmpty
                (mismatches table surfaces)
                "baseline: the table and the code agree before any perturbation, so each failure below is caused by the perturbation alone"

            Expect.isNonEmpty
                (mismatches table (withMovedRoot surfaces))
                "a surface whose ROOT moved without the table moving with it must be reported — this is #1082 happening again"

            let otherSelector =
                match withOtherSelector surfaces with
                | Some perturbed -> perturbed
                | None ->
                    failtest
                        "non-vacuity: SurfaceSelector defines more than one case, so 'the selector changed' is a perturbation that can be expressed at all"

            Expect.isNonEmpty
                (mismatches table otherSelector)
                "a surface whose SELECTOR changed without the table changing with it must be reported — the half a single `Root` column could never express"

            // #1136 — the control for `inventorySubject.ComparesKind`. This document publishes a
            // `Kind` column and is checked on it, while `skill-parity-cli.md` deliberately is not;
            // the difference is one boolean, and without this a flipped boolean would delete the
            // kind check with nothing turning red. `Kind` is the ONLY column whose comparison is
            // conditional, so it is the only one that needs its own control.
            let otherKind =
                match withOtherKind surfaces with
                | Some perturbed -> perturbed
                | None ->
                    failtest
                        "non-vacuity: SurfaceKind defines more than one case, so 'the kind changed' is a perturbation that can be expressed at all"

            Expect.isNonEmpty
                (mismatches table otherKind)
                "a surface whose KIND changed without the table changing with it must be reported — this table publishes Kind, so it is compared on it"

            Expect.isNonEmpty
                (mismatches table (withUndeclaredSurface "fsgg-1099-undocumented-surface" surfaces))
                "a surface declared with NO row at all must be reported; this is the direction a hand-corrected table fails silently in"

            Expect.isNonEmpty
                (mismatches { table with Entries = List.tail table.Entries } surfaces)
                "and a row deleted from the table must be reported too, so the check is not satisfied by an empty document"

            // The other direction of the id-set comparison, which no perturbation above reaches: a
            // row the resolver does not declare fails through the extra-entry clause.
            Expect.isNonEmpty
                (mismatches
                    { table with Entries = { List.head table.Entries with SurfaceId = "fsgg-1099-invented-row" } :: table.Entries }
                    surfaces)
                "a row for a surface that does not exist must be reported; a table may not add surfaces the checker never reads"
        }

        test "a SECOND row for a surface that already has one is a disagreement, whatever it publishes" {
            // #1136 acceptance criterion 2, and the hole this row was filed to close here. #1111's
            // review found it in the sibling file: matching a surface to its restatement with
            // `List.tryFind` compares the FIRST row for an id and every later one rides in
            // unchecked, so a correct `claude` row followed by a `claude` row publishing any roots
            // at all read as green. #1111 fixed it in its own copy only, because the comparison was
            // `private` and could not be shared. It is now fixed in the one shared `disagreements`,
            // which counts repeats AND compares every matching row rather than the first.
            let table =
                match inventoryRows () with
                | Some table -> table
                | None -> failtestf "non-vacuity: the Required Inventory table must parse out of %s" contractPath

            let surfaces = declaredSurfaces ()

            Expect.isEmpty
                (mismatches table surfaces)
                "baseline: the table and the code agree before the row is duplicated"

            // The ORDER is the whole point, and getting it wrong makes this control vacuous: the
            // wrong row is APPENDED, after the correct one. `tryFind` returns the FIRST match, so a
            // wrong row placed first is compared and reported even under the broken implementation;
            // only a wrong row placed SECOND reproduces the hole. Measured both ways while writing
            // this — prepending left the assertion below green with `tryFind` restored.
            let duplicatedWithDifferentRoots =
                { table with Entries = table.Entries @ [ { List.head table.Entries with Roots = [ "docs" ] } ] }

            let problems = mismatches duplicatedWithDifferentRoots surfaces

            Expect.isNonEmpty
                problems
                "a duplicated row publishing DIFFERENT roots must fail — under `tryFind` the correct row was found first and this one was compared against nothing"

            // #1136 REVIEW — and it must fail through the ROOTS clause, not merely through the
            // repeat count. `isNonEmpty` alone is satisfied by `repeated` and stays green if the
            // cell comparison reverts to `List.tryFind`, which is the very hole this closes. So the
            // sentence about the second row's roots is asserted by name, and `List.filter` in
            // `disagreements` is measured rather than merely described. Verified by mutation:
            // restoring `tryFind |> Option.toList` turns THIS assertion red and nothing else.
            Expect.isTrue
                (problems |> List.exists (fun problem -> problem.Contains "publishes roots"))
                "the SECOND row's roots must be reported in their own right: under `tryFind` only the first row for an id was compared, and this assertion is what pins `List.filter`"

            // And the sharper case the roots clause alone cannot catch: a byte-identical duplicate
            // disagrees with nothing cell by cell, and is still a defect. A surface is declared
            // once, so it is restated once — otherwise "which row is the contract" has no answer.
            Expect.isNonEmpty
                (mismatches { table with Entries = List.head table.Entries :: table.Entries } surfaces)
                "an IDENTICAL duplicated row must fail too: a surface is declared once and is restated once"
        }

        test "a row the parser cannot read is a disagreement, never a row that is not there" {
            // #1136 REVIEW, and the fail-open this file carried in its own right. `inventoryRows`
            // used to `List.choose` away every row whose `Surface id` cell was not exactly one code
            // span, so a malformed row was DELETED before any rule could see it. Measured on the
            // real document: appending
            //
            //     | legacy-speckit-wrapper | `wrapper` | `.agents/skills/speckit-*` | … |
            //
            // left this file green — a prose id, a glob root, and a surface that does not exist,
            // all unreported. The glob and prose rules below cannot catch it, because they iterate
            // the rows that PARSED. `Feature1111` already reported its unreadable bullets; this is
            // the same rule, now that both files share the comparison that carries it.
            let table =
                match inventoryRows () with
                | Some table -> table
                | None -> failtestf "non-vacuity: the Required Inventory table must parse out of %s" contractPath

            let surfaces = declaredSurfaces ()

            Expect.isEmpty
                table.Unreadable
                "baseline: every row of the real table parses today, so the failures below are caused by the perturbation alone"

            Expect.isEmpty (mismatches table surfaces) "baseline: the table and the code agree"

            Expect.isNonEmpty
                (mismatches { table with Unreadable = [ "synthetic" ] } surfaces)
                "a row the parser could not read must count as a disagreement, never as a row that is simply not present"

            // The structural statement of the same rule, over the REAL document rather than a
            // synthetic list, and the one that would have caught this in the first place: every
            // data row the table has is either read or reported. `List.choose` made those two
            // numbers differ, silently, and nothing here noticed.
            let dataRows =
                match tableUnder "## Required Inventory" with
                | Some(_, rows) -> rows
                | None -> failtestf "non-vacuity: the Required Inventory table must be located in %s" contractPath

            Expect.isNonEmpty dataRows "non-vacuity: the table has data rows for this rule to be about"

            Expect.equal
                (List.length table.Entries + List.length table.Unreadable)
                (List.length dataRows)
                "every data row is either read or reported as unreadable; a row this parser drops is a row no rule in this file can ever see"
        }

        test "the unresolved-root verdict names the view generator, not the document" {
            // #1136's routed finding, pinned rather than merely written. `.agents/skills` is a
            // gitignored generated VIEW and is absent from a bare worktree, so the root-resolution
            // rule below fails on a fresh clone. The failure is correct; for two issues its MESSAGE
            // was not — it reported the declaration as disagreeing with the tree, which reads as
            // "this document is wrong" when the cause is "this tree has not generated its view".
            // Nothing pinned that wording, which is exactly how it stayed wrong, so this asserts
            // the remedy is named and the diagnosis is not asserted as the document's fault.
            let message = unresolvedRootMessage "row" "codex-local" ".agents/skills"

            Expect.stringContains
                message
                "scripts/skill-view generate"
                "the verdict names the generator that resolves the view, so whoever hits it on a bare clone is one command from green rather than reading the document for a defect that is not there"

            Expect.stringContains
                message
                ".agents/skills"
                "and it names the root that is a generated view, so the reader can tell whether this failure is theirs"

            // Both files must say it, because both check the same declared roots. That they share
            // one helper is what makes that true without a second copy to keep in step.
            Expect.equal
                (unresolvedRootMessage "bullet" "codex-local" ".agents/skills")
                (message.Replace("row '", "bullet '"))
                "the two documents' verdicts differ only in the noun for one entry; the rest is one string in one place"
        }

        // ---------- Acceptance criterion 1: the cells are data, not prose ----------

        test "no root cell carries prose or a glob, so the published Root is a checkable claim" {
            // The same rule #1092 landed on the code, applied to the document that restates it. The
            // stale rows said `src/*/skill`, `template/**/skill and template/product-skills` and
            // `.agents/skills/speckit-*` — three selectors written into a column that means "where
            // this surface looks". `mismatches` already rejects those by equality; this states the
            // rule generically, so the next cell tempted to explain itself in English fails here with
            // a message that says why.
            let table =
                match inventoryRows () with
                | Some table -> table
                | None -> failtestf "non-vacuity: the Required Inventory table must parse out of %s" contractPath

            Expect.isNonEmpty table.Entries "non-vacuity: there are rows to check"

            for row in table.Entries do
                Expect.isNonEmpty
                    row.Roots
                    (sprintf "row '%s' publishes no root as a code span — a root is data, and prose in this column is the defect #1099 is about" row.SurfaceId)

                for root in row.Roots do
                    Expect.isFalse
                        (root |> Seq.exists Char.IsWhiteSpace)
                        (sprintf "row '%s' publishes root '%s', which contains whitespace — one span per root, and prose belongs in the Role column" row.SurfaceId root)

                    Expect.isFalse
                        (root.Contains '*' || root.Contains '?')
                        (sprintf
                            "row '%s' publishes root '%s', which contains a glob metacharacter — narrowing is the surface's Selector, and this column is where it LOOKS"
                            row.SurfaceId
                            root)

                    // #1136 routed finding: the message, not the verdict, was the defect here. The
                    // failure is correct — the root does not resolve — but `.agents/skills` is a
                    // gitignored GENERATED VIEW, absent from a bare worktree, so on a fresh clone
                    // this reported the DOCUMENT as wrong when the tree had simply not generated
                    // its view. `unresolvedRootMessage` names the generator first, and both files
                    // now say the same thing because they call the same helper.
                    Expect.isTrue (rootResolves root) (unresolvedRootMessage "row" row.SurfaceId root)
        }

        // ---------- Acceptance criterion 3: `ant-canonical` names the post-#1082 location ----------

        test "ant-canonical names the post-#1082 canonical location, and the pre-#1082 one appears nowhere" {
            let table =
                match inventoryRows () with
                | Some table -> table
                | None -> failtestf "non-vacuity: the Required Inventory table must parse out of %s" contractPath

            let ant =
                match table.Entries |> List.tryFind (fun row -> row.SurfaceId = "ant-canonical") with
                | Some row -> row
                | None -> failtest "non-vacuity: the table still has an `ant-canonical` row to check"

            let declared =
                declaredSurfaces ()
                |> List.find (fun surface -> surface.SurfaceId = "ant-canonical")

            Expect.equal
                ant.Roots
                declared.Roots
                "the ant-canonical row publishes the root the resolver reads"

            // Stated as a separate, named fact rather than left to the equality above: the equality
            // would also pass if the canonical moved BACK into `.claude/skills`, and #1082's decision
            // is that it cannot — a byte-identical three-root union has no room for a canonical the
            // other roots route into.
            Expect.isFalse
                (declared.Roots |> List.exists (fun root -> root.StartsWith(".claude/skills", StringComparison.Ordinal)))
                "the Ant canonical body does not live under an agent-skill root; #1082 moved it out and made fs-gg-ant-design an ordinary wrapper"

            let document = File.ReadAllText contractPath

            Expect.isFalse
                (document.Contains(".claude/skills/fs-gg-ant-design", StringComparison.Ordinal))
                "the pre-#1082 path is named nowhere in this contract: it was published in the Root column of a file called `contracts/` for two issues after it stopped being the canonical body"
        }

        // ---------- The vocabulary this document now restates is pinned too ----------

        test "the Selector vocabulary table lists exactly the selectors the code defines" {
            // #1099 is about a restatement nothing checked. Adding a selector-vocabulary table to the
            // same document creates a SECOND one, so it is pinned in the same breath rather than left
            // to become next year's version of this issue. Quantified over the union by reflection,
            // so a sixth selector fails here without anyone remembering to extend the document.
            let documented =
                match tableUnder "### Selector vocabulary" with
                | Some(_, rows) -> rows |> List.choose (fun row -> row |> List.tryHead |> Option.bind singleSpan) |> Set.ofList
                | None -> failtestf "non-vacuity: no Selector vocabulary table was parsed out of %s" contractPath

            let defined = everySelector () |> List.map SkillParity.surfaceSelectorToken |> Set.ofList

            Expect.isGreaterThanOrEqual
                (Set.count defined)
                5
                "non-vacuity: reflection enumerated the SurfaceSelector cases and did not return an empty set"

            Expect.equal
                documented
                defined
                "the documented selector vocabulary and the SurfaceSelector union must be the same set — a selector defined but undocumented, or documented but deleted, is the drift this item is about"
        }
    ]
