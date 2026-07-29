module SurfaceContractSupport

// Issue #1136 — the surface-declaration contract comparison was implemented TWICE.
//
// `Feature1099SurfaceInventoryContractTests` pins the `## Required Inventory` table in
// `contracts/skill-surface-inventory.md`; `Feature1111CliSurfaceListContractTests` pins the
// `## Default Repository Surfaces` bullets in `contracts/skill-parity-cli.md`. Both re-derive the
// surfaces from `SkillParity.discoverDefaultSurfaces` at runtime, read a restatement out of a
// document at runtime, and compare the two in both directions. #1111's acceptance criterion 2 said
// #1099's comparison "is factored out precisely so a second document can reuse it" — it was NOT:
// `mismatches` was `let private` over a `type private InventoryRow`, and `private` at module level
// in F# means private to that module, so no second module could call either. #1111 therefore landed
// a second implementation rather than quietly crossing its own written scope boundary, and filed
// this row.
//
// WHAT THAT COST, precisely, and why this is worth a file. Both copies are executable and both are
// checked, so — unlike the two DOCUMENTS they pin — neither could drift in silence. What a second
// copy costs is that a hole found in one is not fixed in the other. #1111's review found exactly
// one: matching a surface to its restatement with `List.tryFind` compares the FIRST entry for an id
// and lets every later one ride in unchecked, so a correct row followed by a wrong row for the same
// id read as green. #1111 fixed it in its own file. #1099's copy still had it, and a duplicated row
// in the Required Inventory table was still unreported there. That hole is closed here once, for
// both documents, by `disagreements` — see `repeated` below.
//
// `Kind` IS OPTIONAL TO THE COMPARISON, and that is a contract, not a convenience.
// `skill-surface-inventory.md` publishes a `Kind` column and must keep being checked on it.
// `skill-parity-cli.md` deliberately does not: that section says what a default run READS, and
// restating each surface's kind there would create a THIRD copy of the same declaration, which is
// the defect both items exist to remove. So the subject a caller passes declares whether `Kind` is
// part of ITS document's contract, and `Feature1099`'s perturbation control proves its own flag is
// on — a silently-flipped `ComparesKind` would otherwise delete a check with nothing turning red.


open System.IO
open System.Text.RegularExpressions
open FSharp.Reflection
open Rendering.Harness
open FS.GG.TestSupport

// ---------------------------------------------------------------------------------------------
// The tree, and what it declares
// ---------------------------------------------------------------------------------------------

let repositoryRoot = RepositoryRoot.value

/// The surfaces a default run reads, re-derived from the CODE on every call. Both documents are
/// compared against this and never against a hand-written expectation: a test that asserted today's
/// six correct entries would pass on the day a seventh surface is added with no entry at all, which
/// is the whole complaint of #1099 and #1111.
let declaredSurfaces () = SkillParity.discoverDefaultSurfaces repositoryRoot

/// Every `SurfaceSelector` case, by reflection, so a case added tomorrow is covered without anyone
/// remembering to extend a test file. All cases are nullary today; a case that ever takes fields is
/// skipped rather than crashing the list, and each caller's count control catches its absence.
let everySelector () =
    FSharpType.GetUnionCases typeof<SkillParity.SurfaceSelector>
    |> Array.toList
    |> List.filter (fun case -> Array.isEmpty (case.GetFields()))
    |> List.choose (fun case ->
        match FSharpValue.MakeUnion(case, [||]) with
        | :? SkillParity.SurfaceSelector as selector -> Some selector
        | _ -> None)

// ---------------------------------------------------------------------------------------------
// Reading data out of prose
// ---------------------------------------------------------------------------------------------

let codeSpanPattern = Regex(@"`([^`]+)`", RegexOptions.Compiled)

/// Every code span in a fragment of Markdown, in order. The columns these contracts pin are DATA,
/// so each value is written as a code span and read back as one — a cell that spells its value in
/// prose yields nothing here and fails, which is the `src/*/skill` shape.
let spans (text: string) =
    codeSpanPattern.Matches text
    |> Seq.map (fun m -> m.Groups.[1].Value.Trim())
    |> List.ofSeq

/// The one code span of a cell that must carry exactly one value, or `None` for zero or many.
let singleSpan (text: string) =
    match spans text with
    | [ only ] -> Some only
    | _ -> None

/// What is left of a fragment once its code spans are removed. A cell that spells part of its value
/// in prose leaves residue here and is rejected by the callers that enforce a grammar.
let residue (text: string) = codeSpanPattern.Replace(text, "").Trim()

// ---------------------------------------------------------------------------------------------
// The comparison
// ---------------------------------------------------------------------------------------------

/// One document's restatement of one surface, projected onto the halves a surface declares.
/// `Kind` is `option` at BOTH levels on purpose: `None` means the document published no kind cell,
/// and whether that is a disagreement is the subject's `ComparesKind`, not this type's business.
type SurfaceRestatement =
    { SurfaceId: string
      Kind: string option
      Roots: string list
      Selector: string option }

/// Everything a document yielded: the restatements that parsed AND the ones that did not. A
/// restatement the parser could not read is a disagreement, never an absence — dropping it would
/// report the surface as merely unmentioned, which is a lie about why, and would make a malformed
/// entry naming a surface that does not exist vanish completely.
type ParsedRestatements =
    { Entries: SurfaceRestatement list
      Unreadable: string list }

/// How one document is named in a verdict, and what it publishes.
type RestatementSubject =
    { /// The document or section, as it should read in a sentence: "the Required Inventory table".
      Document: string
      /// The singular noun for one restatement in it: "row", "bullet".
      Entry: string
      /// Whether this document publishes `Kind`, and is therefore checked on it. See the header.
      ComparesKind: bool }

/// Every way a document's restatement and the code disagree, as sentences. Empty means they agree.
///
/// BOTH DIRECTIONS are enumerated deliberately: a surface with no entry is as much a failure as an
/// entry for no surface, and only the first of those is what staleness has actually looked like in
/// these two documents.
///
/// EVERY entry for an id is compared, not the first — `List.filter`, never `List.tryFind`. Paired
/// with `repeated`, that closes #1111's review finding for both documents at once: a second entry
/// for an id is reported whatever it publishes, so no path can ride in beside a correct one.
let disagreements
    (subject: RestatementSubject)
    (restated: ParsedRestatements)
    (surfaces: SkillParity.SkillSurface list)
    =
    let entryIds = restated.Entries |> List.map (fun entry -> entry.SurfaceId) |> Set.ofList
    let surfaceIds = surfaces |> List.map (fun surface -> surface.SurfaceId) |> Set.ofList

    let unreadable =
        restated.Unreadable
        |> List.map (fun problem ->
            sprintf "a %s in %s could not be read as a surface declaration: %s" subject.Entry subject.Document problem)

    // A surface is declared once, so it is restated once.
    let repeated =
        restated.Entries
        |> List.countBy (fun entry -> entry.SurfaceId)
        |> List.filter (fun (_, count) -> count > 1)
        |> List.map (fun (surfaceId, count) ->
            sprintf
                "%s publishes %d %ss for surface '%s'; a surface is declared once and is restated once"
                subject.Document
                count
                subject.Entry
                surfaceId)

    let missingEntries =
        Set.difference surfaceIds entryIds
        |> Set.toList
        |> List.map (fun surfaceId ->
            sprintf
                "surface '%s' is declared by discoverDefaultSurfaces and has no %s in %s"
                surfaceId
                subject.Entry
                subject.Document)

    let extraEntries =
        Set.difference entryIds surfaceIds
        |> Set.toList
        |> List.map (fun surfaceId ->
            sprintf
                "%s has a %s for '%s', which discoverDefaultSurfaces does not declare"
                subject.Document
                subject.Entry
                surfaceId)

    let cellMismatches =
        surfaces
        |> List.collect (fun surface ->
            restated.Entries
            |> List.filter (fun entry -> entry.SurfaceId = surface.SurfaceId)
            |> List.collect (fun entry ->
                let expectedSelector = SkillParity.surfaceSelectorToken surface.Selector
                let expectedKind = SkillParity.surfaceKindToken surface.Kind

                [ if entry.Roots <> surface.Roots then
                      yield
                          sprintf
                              "surface '%s': %s publishes roots %A and the resolver reads %A"
                              surface.SurfaceId
                              subject.Document
                              entry.Roots
                              surface.Roots
                  if entry.Selector <> Some expectedSelector then
                      yield
                          sprintf
                              "surface '%s': %s publishes selector %A and the resolver uses '%s'"
                              surface.SurfaceId
                              subject.Document
                              entry.Selector
                              expectedSelector
                  if subject.ComparesKind && entry.Kind <> Some expectedKind then
                      yield
                          sprintf
                              "surface '%s': %s publishes kind %A and the resolver declares '%s'"
                              surface.SurfaceId
                              subject.Document
                              entry.Kind
                              expectedKind ]))

    unreadable @ repeated @ missingEntries @ extraEntries @ cellMismatches

// ---------------------------------------------------------------------------------------------
// Perturbation controls
// ---------------------------------------------------------------------------------------------
//
// "No disagreements" is equally true of a comparison that parsed nothing, compared nothing, or read
// a field that is always absent — the fail-open one level up, and the reason a hand-corrected
// document is not the fix. So every green assertion in both files is paired with a PERTURBED
// surface list that must be rejected. Each perturbation below is a divergence that has ACTUALLY
// happened to these documents.

/// A canonical body moved and the document did not move with it — #1082 happening again.
let withMovedRoot (surfaces: SkillParity.SkillSurface list) =
    surfaces
    |> List.mapi (fun index surface ->
        if index = 0 then
            { surface with Roots = [ "docs/product/ant-design/skill/SKILL.md" ] }
        else
            surface)

/// A surface's SELECTOR changed and the document did not — the half a single path column could
/// never express. `None` when `SurfaceSelector` has fewer than two nullary cases, which each caller
/// reports as a non-vacuity failure in its own words rather than passing vacuously.
let withOtherSelector (surfaces: SkillParity.SkillSurface list) =
    match surfaces with
    | [] -> None
    | first :: rest ->
        everySelector ()
        |> List.tryFind (fun selector -> selector <> first.Selector)
        |> Option.map (fun replacement -> { first with Selector = replacement } :: rest)

/// A surface declared with NO entry at all — the direction a hand-corrected document fails silently
/// in, and how `spec-kit-command` went unmentioned in `skill-parity-cli.md` for three issues.
let withUndeclaredSurface (surfaceId: string) (surfaces: SkillParity.SkillSurface list) =
    match surfaces with
    | [] -> []
    | first :: _ -> { first with SurfaceId = surfaceId } :: surfaces

/// A surface whose KIND changed and the document did not. Only meaningful for a subject with
/// `ComparesKind = true`; it is that flag's own control. `None` when `SurfaceKind` offers no second
/// token to swap in, which the caller reports rather than passing vacuously.
let withOtherKind (surfaces: SkillParity.SkillSurface list) =
    match surfaces with
    | [] -> None
    | first :: rest ->
        FSharpType.GetUnionCases typeof<SkillParity.SurfaceKind>
        |> Array.toList
        |> List.filter (fun case -> Array.isEmpty (case.GetFields()))
        |> List.choose (fun case ->
            match FSharpValue.MakeUnion(case, [||]) with
            | :? SkillParity.SurfaceKind as kind -> Some kind
            | _ -> None)
        |> List.tryFind (fun kind -> kind <> first.Kind)
        |> Option.map (fun replacement -> { first with Kind = replacement } :: rest)

// ---------------------------------------------------------------------------------------------
// Resolving a declared root against the tree
// ---------------------------------------------------------------------------------------------

/// Whether a repository-relative root names something that exists in this tree.
let rootResolves (root: string) =
    let absolute = Path.Combine(repositoryRoot, root.Replace('/', Path.DirectorySeparatorChar))
    File.Exists absolute || Directory.Exists absolute

/// What to say when it does not — and, FIRST, what to check.
///
/// ROUTED FINDING on #1136, from the worker that landed #1111. `.agents/skills` is a gitignored
/// GENERATED VIEW of the tracked `.claude/skills` (ADR-0067 §6/§8/§9 phase 4, `.gitignore`'s
/// `/.agents/skills`), resolved at checkout by `scripts/skill-view` from
/// `.config/kit/FS.GG.Kit.receiver.proj`. It is ABSENT from a bare worktree until something
/// generates it, so both documents' `.agents/skills` roots fail this check on a fresh clone. The
/// failure itself is correct; what was wrong is what it SAID. #1099's message reported the
/// declaration as disagreeing with the tree, which reads as "this document is wrong" when the cause
/// is "this tree has not generated its view yet" — a detour whoever hits it pays for in full.
///
/// This is NOT `.github#1881`. That one is a `Paths:` token matching no file going UNNOTICED: a
/// declaration-vs-tree gap nothing reports. This one does report, correctly, and was merely wrong
/// about the cause. Fixing either leaves the other standing.
let unresolvedRootMessage (entry: string) (surfaceId: string) (root: string) =
    sprintf
        "%s '%s' publishes root '%s', which resolves to no file or directory in this tree. CHECK THE GENERATED VIEW FIRST: `.agents/skills` is a gitignored VIEW of `.claude/skills` (ADR-0067 §6/§8/§9 phase 4) and is absent from a bare checkout until it is resolved — run `bash scripts/skill-view generate --source .claude/skills --roots \".agents/skills\"` and re-run. Only once every generated view root exists is this the document disagreeing with the tree."
        entry
        surfaceId
        root
