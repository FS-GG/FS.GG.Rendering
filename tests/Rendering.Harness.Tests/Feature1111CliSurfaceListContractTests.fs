module Feature1111CliSurfaceListContractTests

// Issue #1111 — `## Default Repository Surfaces` in
// `specs/168-skill-parity-evidence/contracts/skill-parity-cli.md` was the SECOND copy of the surface
// declaration, in the second file named `contracts/`, that nothing read and nothing diffed. #1099
// pinned the copy in `skill-surface-inventory.md`; this one survived, with the same stale Ant path,
// because it was excluded from #1099 to buy parallelism with #1098 — and #1098 landed (#1109)
// without touching the section.
//
// MEASURED against `discoverDefaultSurfaces` at `19af8b59`, the section published five bullets for
// six surfaces:
//
//     the document said                                       the resolver reads
//     src/*/skill/SKILL.md                  package-canonical  src        (area-skill-bodies)
//     template/**/SKILL.md                  template-canonical template   (non-mirrored-bodies)
//     .agents/skills/*/SKILL.md             codex-local        .agents/skills (agent-wrappers)
//     .claude/skills/*/SKILL.md             claude             .claude/skills (agent-wrappers)
//         — four bullets writing a SELECTOR into the path, which is the pre-#1092 shape, where the
//           glob stood in for a narrowing that was really a hard-coded branch of the resolver
//     .claude/skills/fs-gg-ant-design/…     ant-canonical      docs/product/ant-design/skill/SKILL.md
//         — not a flattened selector but a STALE PATH: that stopped being the canonical body in
//           #1080/#1082, and the bullet still called it the canonical Ant Design skill
//     — no bullet —                         spec-kit-command   .agents/skills, .claude/skills
//                                                                          (command-wrappers)
//         — the section's closing paragraph did mention Spec Kit command skills, to say they are
//           reported rather than hidden. What it never named was a ROOT: the surface with the widest
//           roots in the repository had no entry in the list of what the checker reads
//
// CORRECTING THE BULLETS IS NOT THE FIX, for the reason #1099 spelled out: rows hand-corrected today
// are stale again on the next surface change, which is exactly how both copies got here. So this file
// re-derives the surfaces from the CODE at runtime and the bullets from the DOCUMENT at runtime and
// compares them, failing in BOTH directions.
//
// ON REUSING #1099'S COMPARISON — RESOLVED BY #1136. #1111's acceptance criterion 2 claimed that
// `mismatches` in `Feature1099SurfaceInventoryContractTests` "is factored out precisely so a second
// document can reuse it". IT WAS NOT: it was `let private` over a `type private InventoryRow`, and
// `private` at module level in F# means private to that module, so no second module could call it.
// #1111's own "Not in scope" fenced off the file where the accessibility would have to change, so
// #1111 landed a deliberate second implementation and filed #1136 rather than quietly crossing a
// written scope boundary. #1136 moved the comparison to `SurfaceContractSupport`, which BOTH files
// now call; neither defines one. `Kind` is optional to it, because this section says what a default
// run READS and restating each surface's kind here would create a third copy of the declaration —
// `skill-surface-inventory.md` is where `Kind` is published and checked.
//
// WHAT THE PARSER MUST NOT DO IS SKIP. Every bullet it cannot read is reported as a disagreement,
// and so is a REPEATED surface id. Neither is a hypothetical: with unreadable bullets dropped, the
// pre-#1111 text would still fail — `missingBullets` fires for all six surfaces — but the verdict
// would be a lie about why, and a malformed bullet naming a surface that does not exist would vanish
// completely. Repeats are the sharper hole: match a surface to its bullet with `tryFind` and a second
// bullet for the same id is compared against nothing, so a correct `claude` bullet followed by a
// `claude` bullet publishing any path at all reads as green — a direct hole in acceptance criterion
// 1, which is that no bullet names a path the resolver does not read.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open Rendering.Harness
open SurfaceContractSupport

let private contractPath =
    Path.Combine(repositoryRoot, "specs", "168-skill-parity-evidence", "contracts", "skill-parity-cli.md")

let private sectionHeading = "## Default Repository Surfaces"

/// This document's half of the shared comparison. `ComparesKind` is FALSE, and that is a decision,
/// not an omission: this section says what a default run READS, and each surface's kind is
/// `skill-surface-inventory.md`'s published column. Restating it here would create a third copy of
/// the declaration, which is the defect #1099 and #1111 exist to remove.
let private bulletSubject: RestatementSubject =
    { Document = sectionHeading
      Entry = "bullet"
      ComparesKind = false }

// ---------------------------------------------------------------------------------------------
// Reading the document
// ---------------------------------------------------------------------------------------------

/// Everything the section yielded: the bullets that parsed, the bullets that did not, and every code
/// span appearing in a bullet — its data line and the prose lines beneath it alike. All three are
/// inputs to the comparison; a bullet that could not be read is a failure, not an absence.
///
/// A bullet is a `SurfaceContractSupport.SurfaceRestatement` with `Kind = None`, which is exactly
/// what "this document does not publish a kind" means to the shared comparison.
type private ParsedSection =
    { Bullets: SurfaceRestatement list
      Malformed: string list
      BulletSpans: string list }

/// A Markdown ATX heading, which is what ends the section — NOT any line beginning with `#`. This
/// file's house style wraps prose at about 80 columns and is dense with `#NNNN` issue references, so
/// one unlucky wrap putting `#1092` in column 0 would otherwise truncate the section and quietly
/// shrink what is checked.
let private headingPattern = Regex(@"^#{1,6}\s", RegexOptions.Compiled)

/// The lines of `sectionHeading`, stopping at the next heading of any level so a later section's
/// bullets can never be mistaken for this one's.
let private sectionLines (document: string) =
    let lines = document.Replace("\r\n", "\n").Split('\n') |> Array.toList

    let rec after rest =
        match rest with
        | (line: string) :: tail when line.Trim() = sectionHeading -> Some tail
        | _ :: tail -> after tail
        | [] -> None

    after lines
    |> Option.map (List.takeWhile (fun (line: string) -> not (headingPattern.IsMatch line)))

/// Indentation is not a hiding place: a nested `- …` is a bullet of this section too, and is parsed
/// and compared like any other. Continuation prose is indented but never starts with a list marker.
let private isBulletStart (line: string) =
    line.TrimStart().StartsWith("- ", StringComparison.Ordinal)

/// The section's bullets, each as its data line followed by the prose lines beneath it. The split is
/// what lets a bullet carry an explanation without the explanation becoming part of the checked
/// claim — while still being read for the path rule, so a path cannot re-enter through a sentence.
let private bulletBlocks (lines: string list) =
    let closed current acc =
        match current with
        | Some block -> List.rev block :: acc
        | None -> acc

    let rec go acc current rest =
        match rest with
        | [] -> List.rev (closed current acc)
        | (line: string) :: tail ->
            if isBulletStart line then go (closed current acc) (Some [ line ]) tail
            elif line.Trim() = "" then go (closed current acc) None tail
            else
                match current with
                | Some block -> go acc (Some(line :: block)) tail
                | None -> go acc None tail

    go [] None lines

/// The grammar every bullet must satisfy:
///
///     - `<surface-id>` — roots `<root>`[, `<root>`…] — selector `<selector>`
///
/// Three em-dash-separated parts; the label of the second and third is a whole word; every value is a
/// code span and nothing but code spans and separators. A cell that spells its value in prose yields
/// residue, or no span at all, and is reported as malformed — the `src/*/skill/SKILL.md` shape this
/// item exists to stop.
let private parseBullet (line: string) : Result<SurfaceRestatement, string> =
    let body = line.TrimStart().Substring(2).Trim()
    let parts = body.Split('—') |> Array.map (fun part -> part.Trim()) |> Array.toList

    let labelled (label: string) (part: string) =
        if part.StartsWith(label + " ", StringComparison.OrdinalIgnoreCase) then
            Some(part.Substring(label.Length).Trim())
        else
            None

    let malformed reason = Error(sprintf "bullet %A %s" body reason)

    match parts with
    | [ idPart; rootsPart; selectorPart ] ->
        match spans idPart, labelled "roots" rootsPart, labelled "selector" selectorPart with
        | [ surfaceId ], Some rootsText, Some selectorText ->
            let roots = spans rootsText
            let selectors = spans selectorText

            if residue idPart <> "" then
                malformed (sprintf "carries prose %A beside its surface id; the id is the whole cell" (residue idPart))
            elif List.isEmpty roots then
                malformed "names no root as a code span"
            elif residue rootsText |> Seq.exists (fun c -> c <> ',' && not (Char.IsWhiteSpace c)) then
                malformed (sprintf "carries prose %A in its roots cell; roots are comma-separated code spans and nothing else" (residue rootsText))
            else
                match selectors with
                | [ selector ] when residue selectorText = "" ->
                    Ok
                        { SurfaceId = surfaceId
                          Kind = None
                          Roots = roots
                          Selector = Some selector }
                | [ _ ] ->
                    malformed (sprintf "carries prose %A in its selector cell; a surface declares one selector and nothing else" (residue selectorText))
                | _ ->
                    malformed (sprintf "publishes %d selector spans in %A; a surface declares exactly one" (List.length selectors) selectorText)
        | ids, roots, selector ->
            malformed (
                sprintf
                    "does not read as `<surface-id>` — roots `<root>`… — selector `<selector>` (ids: %A, roots cell: %A, selector cell: %A)"
                    ids
                    roots
                    selector)
    | _ -> malformed (sprintf "has %d em-dash-separated parts; the grammar has three" (List.length parts))

let private parseSection (document: string) =
    match sectionLines document with
    | None -> None
    | Some lines ->
        let blocks = bulletBlocks lines
        let parsed = blocks |> List.map (List.head >> parseBullet)

        Some
            { Bullets = parsed |> List.choose (function Ok bullet -> Some bullet | Error _ -> None)
              Malformed = parsed |> List.choose (function Error problem -> Some problem | Ok _ -> None)
              BulletSpans = blocks |> List.collect (List.collect spans) }

let private parseContract () = parseSection (File.ReadAllText contractPath)

// ---------------------------------------------------------------------------------------------
// The comparison
// ---------------------------------------------------------------------------------------------

/// The one comparison, in the one place both files call (#1136). This is a projection onto its
/// argument shape and nothing else — there is no second comparison in this file. `Malformed` is
/// carried into it as `Unreadable`, so a bullet the parser could not read counts as a disagreement
/// rather than as a bullet that is simply not there.
let private mismatches (section: ParsedSection) (surfaces: SkillParity.SkillSurface list) =
    disagreements bulletSubject { Entries = section.Bullets; Unreadable = section.Malformed } surfaces

let private remedy =
    "Correct the bullet, or the declaration, so they agree. Nothing regenerates this section, so #1111 pins it — as #1099 pinned the same declaration in skill-surface-inventory.md."

let private parsedOrFail () =
    match parseContract () with
    | Some section -> section
    | None ->
        failtestf
            "non-vacuity: no '%s' section was found in %s. This file compares that section with the code, so a section that cannot be located is a FAILURE and never a pass"
            sectionHeading
            contractPath

/// The section exactly as it stood at `19af8b59`, before this item. It is the regression control for
/// the parser: every assertion below is only worth its message if this text is REJECTED.
let private preItemSection =
    String.concat
        "\n"
        [ sectionHeading
          ""
          "When no `--surface` is supplied, the checker reads:"
          ""
          "- canonical package skills under `src/*/skill/SKILL.md`"
          "- canonical template and generated-product skills under `template/**/SKILL.md`"
          "- the canonical Ant Design skill at `.claude/skills/fs-gg-ant-design/SKILL.md`"
          "- Codex/local-agent wrappers under `.agents/skills/*/SKILL.md`"
          "- Claude wrappers under `.claude/skills/*/SKILL.md`"
          ""
          "Spec Kit command skills that exist as wrappers without package/template"
          "canonical sources are reported as command-surface entries, not hidden."
          ""
          "## Next Section" ]

[<Tests>]
let cliSurfaceListContractTests =
    testList "Feature1111 Default Repository Surfaces and discoverDefaultSurfaces say the same thing" [

        // ---------- Acceptance criteria 1 and 2: the section agrees, and cannot quietly stop ----------

        test "every bullet of Default Repository Surfaces agrees with the surface it restates" {
            let section = parsedOrFail ()
            let surfaces = declaredSurfaces ()

            Expect.isGreaterThanOrEqual
                (List.length section.Bullets + List.length section.Malformed)
                6
                "non-vacuity: the section's bullets were located and are not an empty list"

            Expect.isGreaterThanOrEqual
                (List.length surfaces)
                6
                "non-vacuity: the repository declares at least the six surfaces this section enumerates"

            match mismatches section surfaces with
            | [] -> ()
            | problems -> failtestf "the contract and the resolver disagree:\n  %s\n\n%s" (String.concat "\n  " problems) remedy
        }

        test "the comparison rejects a surface list the section does not describe" {
            // THE control for the assertion above. "No mismatches" is equally true of a comparison
            // that parsed nothing, compared nothing, or matched on a field that is always absent —
            // the fail-open one level up, and the reason correcting the bullets without pinning them
            // is not the fix. Each perturbation is a divergence that has ACTUALLY happened to this
            // document: a canonical body moved (#1082), a selector written into the path (#1092), and
            // a surface declared with no bullet at all (`spec-kit-command`, until this item).
            let section = parsedOrFail ()
            let surfaces = declaredSurfaces ()

            Expect.isEmpty
                (mismatches section surfaces)
                "baseline: the section and the code agree before any perturbation, so each failure below is caused by the perturbation alone"

            Expect.isNonEmpty
                (mismatches section (withMovedRoot surfaces))
                "a surface whose ROOT moved without the section moving with it must be reported — this is #1082 happening again"

            let otherSelector =
                match withOtherSelector surfaces with
                | Some perturbed -> perturbed
                | None ->
                    failtest
                        "non-vacuity: SurfaceSelector defines more than one case, so 'the selector changed' is a perturbation that can be expressed at all"

            Expect.isNonEmpty
                (mismatches section otherSelector)
                "a surface whose SELECTOR changed without the section changing with it must be reported — the half a single prose path could never express"

            // #1136 — the OTHER side of `bulletSubject.ComparesKind = false`, stated so the decision
            // is executable rather than a comment. This section publishes no kind, so a surface
            // whose KIND changes must NOT turn this file red; `skill-surface-inventory.md` is where
            // that is checked, and its own control proves it. Without this, "we do not compare Kind"
            // is an assertion nobody has measured.
            let otherKind =
                match withOtherKind surfaces with
                | Some perturbed -> perturbed
                | None ->
                    failtest
                        "non-vacuity: SurfaceKind defines more than one case, so 'the kind changed' is a perturbation that can be expressed at all"

            Expect.isEmpty
                (mismatches section otherKind)
                "a surface whose KIND changed alone must NOT be reported here: this section deliberately does not restate Kind, and adding it would be a third copy of the declaration"

            Expect.isNonEmpty
                (mismatches section (withUndeclaredSurface "fsgg-1111-undocumented-surface" surfaces))
                "a surface declared with NO bullet must be reported; that is exactly how `spec-kit-command` went without an entry here for three issues"

            Expect.isNonEmpty
                (mismatches { section with Bullets = List.tail section.Bullets } surfaces)
                "and a bullet deleted from the section must be reported too, so the check is not satisfied by an empty document"

            // The other direction of the id-set comparison, which no perturbation above reaches: a
            // bullet the resolver does not declare fails through `extraBullets`, not `missingBullets`.
            let inventedBullet =
                { List.head section.Bullets with SurfaceId = "fsgg-1111-invented-bullet" } :: section.Bullets

            Expect.isNonEmpty
                (mismatches { section with Bullets = inventedBullet } surfaces)
                "a bullet for a surface that does not exist must be reported; a section may not add surfaces the checker never reads"

            // The repeat hole, stated as its own control. Before this was handled, `tryFind` compared
            // the FIRST bullet for an id and every later one rode in unchecked.
            let repeatedBullet =
                let first = List.head section.Bullets
                { first with Roots = [ "docs" ]; Selector = Some "every-skill-body" } :: section.Bullets

            Expect.isNonEmpty
                (mismatches { section with Bullets = repeatedBullet } surfaces)
                "a SECOND bullet for a surface that already has one must be reported, whatever it publishes — otherwise any path at all rides in beside a correct bullet"

            Expect.isNonEmpty
                (mismatches { section with Malformed = [ "synthetic" ] } surfaces)
                "a bullet the parser could not read must count as a disagreement, never as a bullet that is not there"
        }

        test "the section as it stood before #1111 is rejected by this check" {
            // The regression control, and the strongest statement this file can make: the parser is
            // fed the ACTUAL pre-item text and must reject it, bullet by bullet, rather than merely
            // noticing that six surfaces went unmentioned.
            let section =
                match parseSection preItemSection with
                | Some section -> section
                | None -> failtest "non-vacuity: the historical section text is located by the same parser the contract is read with"

            Expect.isEmpty
                section.Bullets
                "none of the five pre-#1111 bullets satisfies the grammar: each wrote a prose path where a surface id, its roots and its selector belong"

            Expect.equal
                (List.length section.Malformed)
                5
                "all five historical bullets are reported as unreadable rather than skipped"

            Expect.isNonEmpty
                (mismatches section (declaredSurfaces ()))
                "and the historical section therefore FAILS the comparison — every surface missing a bullet, plus five bullets that could not be read"
        }

        // ---------- Acceptance criterion 1: no bullet names a path the resolver does not read ----------

        test "every path-shaped code span in a bullet is a root the resolver actually reads" {
            // `mismatches` already pins the roots of the bullets it can parse. This states the rule
            // over the whole of every bullet, its explanatory prose included, so a path cannot
            // re-enter through a sentence the way `.claude/skills/fs-gg-ant-design/SKILL.md` did — it
            // survived #1080, #1082, #1098 and #1099 sitting in a bullet nothing compared. A span
            // carrying a path separator is a claim about where the checker looks; anything else in a
            // bullet is a token or an identifier. Scoped to bullets, per the acceptance criterion's
            // own words, so the section's surrounding paragraphs stay free to discuss paths that are
            // not roots — `non-mirrored-bodies` subtracts some, and they have to be nameable.
            let section = parsedOrFail ()
            let surfaces = declaredSurfaces ()
            let declaredRoots = surfaces |> List.collect (fun surface -> surface.Roots) |> Set.ofList

            Expect.isNonEmpty section.BulletSpans "non-vacuity: the bullets' code spans were read and are not an empty list"
            Expect.isNonEmpty declaredRoots "non-vacuity: the resolver declares at least one root to compare against"

            let pathShaped = section.BulletSpans |> List.filter (fun span -> span.Contains '/')

            Expect.isNonEmpty pathShaped "non-vacuity: some bullet span looks like a path, so this rule has a subject"

            for span in pathShaped do
                Expect.isFalse
                    (span.Contains '*' || span.Contains '?')
                    (sprintf
                        "a bullet publishes path `%s`, which contains a glob metacharacter — narrowing is the surface's SELECTOR, and a root is where it LOOKS. This is the pre-#1092 shape"
                        span)

                Expect.isTrue
                    (declaredRoots.Contains span)
                    (sprintf
                        "a bullet publishes path `%s`, which is not a root any surface declares. Declared roots are %A. %s"
                        span
                        (Set.toList declaredRoots)
                        remedy)
        }

        test "every root a bullet publishes resolves in the repository" {
            // Separate from the rule above because it fails for a different reason and has a
            // different remedy: the path is a declared root and still does not exist. NOTE that
            // `.agents/skills` is a generated VIEW (ADR-0067 §6/§8/§9 phase 4) and is absent from a
            // bare checkout, so this — like `Feature1099`'s equivalent — presumes the tree has been
            // resolved by `.github/actions/skill-view`, as every job that reads the runtime skill
            // roots does.
            let section = parsedOrFail ()

            Expect.isNonEmpty section.Bullets "non-vacuity: there are bullets whose roots can be resolved"

            for bullet in section.Bullets do
                for root in bullet.Roots do
                    // #1136 — the remedy this file already named now lives in the one place both
                    // files call, so `Feature1099`'s equivalent stops reporting an ungenerated view
                    // as the document being wrong.
                    Expect.isTrue (rootResolves root) (unresolvedRootMessage "bullet" bullet.SurfaceId root)
        }

        // ---------- Acceptance criterion 3: `ant-canonical` names the post-#1082 location ----------

        test "ant-canonical names the post-#1082 canonical location, and the pre-#1082 path appears nowhere in the file" {
            let section = parsedOrFail ()

            let ant =
                match section.Bullets |> List.tryFind (fun bullet -> bullet.SurfaceId = "ant-canonical") with
                | Some bullet -> bullet
                | None -> failtest "non-vacuity: the section still has an `ant-canonical` bullet to check"

            let declared =
                declaredSurfaces () |> List.find (fun surface -> surface.SurfaceId = "ant-canonical")

            Expect.equal ant.Roots declared.Roots "the ant-canonical bullet publishes the root the resolver reads"

            // Stated separately from the equality above, which would also pass if the canonical moved
            // BACK under an agent skill root. #1082's decision is that it cannot: a byte-identical
            // three-root union has no room for a canonical the other roots route into.
            Expect.isFalse
                (declared.Roots
                 |> List.exists (fun root ->
                     root.StartsWith(".claude/skills", StringComparison.Ordinal)
                     || root.StartsWith(".agents/skills", StringComparison.Ordinal)))
                "the Ant canonical body does not live under an agent-skill root; #1082 moved it out and made fs-gg-ant-design an ordinary wrapper"

            // The whole file, not just the section: acceptance criterion 3 says the pre-#1082 path is
            // not described as canonical ANYWHERE here, and the cheapest way to guarantee that is for
            // the path not to appear at all — the same rule #1099 landed on the sibling document.
            let document = File.ReadAllText contractPath

            Expect.isFalse
                (document.Contains(".claude/skills/fs-gg-ant-design", StringComparison.Ordinal))
                "the pre-#1082 path is named nowhere in this contract: it was published as the canonical Ant skill in a file called `contracts/` for four issues after it stopped being one"
        }

        // ---------- Acceptance criterion 4: `spec-kit-command` is present, with roots and a selector ----------

        test "spec-kit-command has a bullet with both of its roots, and the speckit narrowing is its selector" {
            let section = parsedOrFail ()

            let commandBullet =
                match section.Bullets |> List.tryFind (fun bullet -> bullet.SurfaceId = "spec-kit-command") with
                | Some bullet -> bullet
                | None ->
                    failtestf
                        "'%s' has no `spec-kit-command` bullet, which is the omission #1111 was filed for: the section lists what the checker reads and the surface with the widest roots was named in no entry of it"
                        sectionHeading

            let declared =
                declaredSurfaces () |> List.find (fun surface -> surface.SurfaceId = "spec-kit-command")

            Expect.equal
                (List.length declared.Roots)
                2
                "non-vacuity: the resolver declares spec-kit-command over two roots, which is what the bullet must publish"

            Expect.equal commandBullet.Roots declared.Roots "the spec-kit-command bullet publishes both roots the resolver reads, in full"

            Expect.equal
                commandBullet.Selector
                (Some(SkillParity.surfaceSelectorToken declared.Selector))
                "the `speckit-` narrowing is named as the surface's SELECTOR"

            for root in commandBullet.Roots do
                Expect.isFalse
                    (root.Contains "speckit")
                    (sprintf
                        "root '%s' writes the `speckit-` narrowing into a path. Before #1092 this surface's declared root WAS `.agents/skills/speckit-*`: English describing a hard-coded branch of the resolver, in the field the report publishes as `Root`"
                        root)
        }
    ]
