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
// CORRECTING THE ROWS IS NOT THE FIX. Acceptance criterion 2 says so outright: without a test the
// rows are re-corrected today and stale again on the next surface change, which is exactly how they
// got here. So these tests re-derive the surfaces from the CODE at runtime and the rows from the
// DOCUMENT at runtime and compare, failing in BOTH directions — editing the table alone fails, and
// changing `discoverDefaultSurfaces` alone fails too. A test that asserted today's six correct rows
// would pass on the day a seventh surface is added with no row at all, which is this item's whole
// complaint.
//
// The comparison is factored into `mismatches` so the green assertion can carry a real non-vacuity
// control: an empty mismatch list is equally true of a comparison that parsed nothing, so every
// green case below is paired with a PERTURBED surface list that must be rejected.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FSharp.Reflection
open Rendering.Harness
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private contractPath =
    Path.Combine(repositoryRoot, "specs", "168-skill-parity-evidence", "contracts", "skill-surface-inventory.md")

let private declaredSurfaces () = SkillParity.discoverDefaultSurfaces repositoryRoot

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

let private codeSpanPattern = Regex(@"`([^`]+)`", RegexOptions.Compiled)

/// Every code span in a cell, in order. The columns this item pins are DATA, so each one is written
/// as a code span and read back as one — a cell that spells its value in prose yields nothing here
/// and fails, which is the `src/*/skill` and `template/**/skill and template/product-skills` shape.
let private spans (cell: string) =
    codeSpanPattern.Matches cell
    |> Seq.map (fun m -> m.Groups.[1].Value.Trim())
    |> List.ofSeq

let private singleSpan (cell: string) =
    match spans cell with
    | [ only ] -> Some only
    | _ -> None

type private InventoryRow =
    { SurfaceId: string
      Kind: string option
      Roots: string list
      Selector: string option }

let private columnIndex (header: string list) (name: string) =
    header |> List.tryFindIndex (fun cell -> cell.Trim().Equals(name, StringComparison.OrdinalIgnoreCase))

/// The Required Inventory table, projected onto the four columns this contract is about. Read by
/// column NAME rather than position, so reordering or inserting a column does not silently shift
/// what is compared.
let private inventoryRows () =
    match tableUnder "## Required Inventory" with
    | None -> None
    | Some(header, rows) ->
        match columnIndex header "Surface id", columnIndex header "Kind", columnIndex header "Roots", columnIndex header "Selector" with
        | Some idIndex, Some kindIndex, Some rootsIndex, Some selectorIndex ->
            let cell (row: string list) index = if index < List.length row then List.item index row else ""

            rows
            |> List.choose (fun row ->
                match singleSpan (cell row idIndex) with
                | None -> None
                | Some surfaceId ->
                    Some
                        { SurfaceId = surfaceId
                          Kind = singleSpan (cell row kindIndex)
                          Roots = spans (cell row rootsIndex)
                          Selector = singleSpan (cell row selectorIndex) })
            |> Some
        | _ -> None

// ---------------------------------------------------------------------------------------------
// The comparison
// ---------------------------------------------------------------------------------------------

/// Every way the document and the code disagree, as sentences. Empty means they agree. Both
/// directions are enumerated deliberately: a surface with no row is as much a failure as a row with
/// no surface, and only the first of those is what today's staleness looks like.
let private mismatches (rows: InventoryRow list) (surfaces: SkillParity.SkillSurface list) =
    let rowIds = rows |> List.map (fun row -> row.SurfaceId) |> Set.ofList
    let surfaceIds = surfaces |> List.map (fun surface -> surface.SurfaceId) |> Set.ofList

    let missingRows =
        Set.difference surfaceIds rowIds
        |> Set.toList
        |> List.map (sprintf "surface '%s' is declared by discoverDefaultSurfaces and has no row in the Required Inventory table")

    let extraRows =
        Set.difference rowIds surfaceIds
        |> Set.toList
        |> List.map (sprintf "the Required Inventory table has a row for '%s', which discoverDefaultSurfaces does not declare")

    let cellMismatches =
        surfaces
        |> List.collect (fun surface ->
            match rows |> List.tryFind (fun row -> row.SurfaceId = surface.SurfaceId) with
            | None -> []
            | Some row ->
                let expectedKind = SkillParity.surfaceKindToken surface.Kind
                let expectedSelector = SkillParity.surfaceSelectorToken surface.Selector

                [ if row.Roots <> surface.Roots then
                      yield
                          sprintf
                              "surface '%s': the table publishes roots %A and the resolver reads %A"
                              surface.SurfaceId
                              row.Roots
                              surface.Roots
                  if row.Selector <> Some expectedSelector then
                      yield
                          sprintf
                              "surface '%s': the table publishes selector %A and the resolver uses '%s'"
                              surface.SurfaceId
                              row.Selector
                              expectedSelector
                  if row.Kind <> Some expectedKind then
                      yield
                          sprintf
                              "surface '%s': the table publishes kind %A and the resolver declares '%s'"
                              surface.SurfaceId
                              row.Kind
                              expectedKind ])

    missingRows @ extraRows @ cellMismatches

let private remedy =
    "Correct the row, or the declaration, so they agree. This table is the contract: nothing regenerates it, so #1099 pins it instead."

/// Every `SurfaceSelector` case, by reflection, so a case added tomorrow is covered without anyone
/// remembering to extend this file. All cases are nullary today; a case that ever takes fields is
/// skipped rather than crashing the list, and the count control below catches its absence.
let private everySelector () =
    FSharpType.GetUnionCases typeof<SkillParity.SurfaceSelector>
    |> Array.toList
    |> List.filter (fun case -> Array.isEmpty (case.GetFields()))
    |> List.choose (fun case ->
        match FSharpValue.MakeUnion(case, [||]) with
        | :? SkillParity.SurfaceSelector as selector -> Some selector
        | _ -> None)

[<Tests>]
let surfaceInventoryContractTests =
    testList "Feature1099 the Required Inventory table and discoverDefaultSurfaces say the same thing" [

        // ---------- Acceptance criteria 1 and 2: the table agrees, and cannot quietly stop ----------

        test "every row of the Required Inventory table agrees with the surface it restates" {
            let rows =
                match inventoryRows () with
                | Some rows -> rows
                | None ->
                    failtestf
                        "non-vacuity: no Required Inventory table with `Surface id`, `Kind`, `Roots` and `Selector` columns was parsed out of %s. This test compares that table with the code, so an unparseable table is a FAILURE and never a pass — a table nothing can read is the same fail-open as a table nothing checks"
                        contractPath

            let surfaces = declaredSurfaces ()

            Expect.isGreaterThanOrEqual
                (List.length rows)
                6
                "non-vacuity: the table's data rows were parsed and are not an empty list"

            Expect.isGreaterThanOrEqual
                (List.length surfaces)
                6
                "non-vacuity: the repository declares at least the six surfaces this contract enumerates"

            match mismatches rows surfaces with
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
            let rows =
                match inventoryRows () with
                | Some rows -> rows
                | None -> failtestf "non-vacuity: the Required Inventory table must parse out of %s" contractPath

            let surfaces = declaredSurfaces ()

            Expect.isEmpty
                (mismatches rows surfaces)
                "baseline: the table and the code agree before any perturbation, so each failure below is caused by the perturbation alone"

            let movedRoot =
                surfaces
                |> List.mapi (fun index surface ->
                    if index = 0 then
                        { surface with Roots = [ "docs/product/ant-design/skill/SKILL.md" ] }
                    else
                        surface)

            Expect.isNonEmpty
                (mismatches rows movedRoot)
                "a surface whose ROOT moved without the table moving with it must be reported — this is #1082 happening again"

            let otherSelector =
                let first = List.head surfaces

                match everySelector () |> List.tryFind (fun selector -> selector <> first.Selector) with
                | Some replacement -> { first with Selector = replacement } :: List.tail surfaces
                | None ->
                    failtest
                        "non-vacuity: SurfaceSelector defines more than one case, so 'the selector changed' is a perturbation that can be expressed at all"

            Expect.isNonEmpty
                (mismatches rows otherSelector)
                "a surface whose SELECTOR changed without the table changing with it must be reported — the half a single `Root` column could never express"

            let seventhSurface =
                { List.head surfaces with SurfaceId = "fsgg-1099-undocumented-surface" } :: surfaces

            Expect.isNonEmpty
                (mismatches rows seventhSurface)
                "a surface declared with NO row at all must be reported; this is the direction a hand-corrected table fails silently in"

            Expect.isNonEmpty
                (mismatches (List.tail rows) surfaces)
                "and a row deleted from the table must be reported too, so the check is not satisfied by an empty document"
        }

        // ---------- Acceptance criterion 1: the cells are data, not prose ----------

        test "no root cell carries prose or a glob, so the published Root is a checkable claim" {
            // The same rule #1092 landed on the code, applied to the document that restates it. The
            // stale rows said `src/*/skill`, `template/**/skill and template/product-skills` and
            // `.agents/skills/speckit-*` — three selectors written into a column that means "where
            // this surface looks". `mismatches` already rejects those by equality; this states the
            // rule generically, so the next cell tempted to explain itself in English fails here with
            // a message that says why.
            let rows =
                match inventoryRows () with
                | Some rows -> rows
                | None -> failtestf "non-vacuity: the Required Inventory table must parse out of %s" contractPath

            Expect.isNonEmpty rows "non-vacuity: there are rows to check"

            for row in rows do
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

                    let absolute = Path.Combine(repositoryRoot, root.Replace('/', Path.DirectorySeparatorChar))

                    Expect.isTrue
                        (File.Exists absolute || Directory.Exists absolute)
                        (sprintf
                            "row '%s' publishes root '%s', which resolves to no file or directory in the repository — `template/product-skills` was published here for as long as it has not existed"
                            row.SurfaceId
                            root)
        }

        // ---------- Acceptance criterion 3: `ant-canonical` names the post-#1082 location ----------

        test "ant-canonical names the post-#1082 canonical location, and the pre-#1082 one appears nowhere" {
            let rows =
                match inventoryRows () with
                | Some rows -> rows
                | None -> failtestf "non-vacuity: the Required Inventory table must parse out of %s" contractPath

            let ant =
                match rows |> List.tryFind (fun row -> row.SurfaceId = "ant-canonical") with
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
