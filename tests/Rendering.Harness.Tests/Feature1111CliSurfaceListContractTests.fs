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
//     src/*/skill/SKILL.md                  package-canonical  src        (area-skill-bodies)  WRONG
//     template/**/SKILL.md                  template-canonical template   (non-mirrored-bodies) WRONG
//     .claude/skills/fs-gg-ant-design/…     ant-canonical      docs/product/ant-design/skill/SKILL.md
//                                                                                              WRONG
//     .agents/skills/*/SKILL.md             codex-local        .agents/skills (agent-wrappers)  WRONG
//     .claude/skills/*/SKILL.md             claude             .claude/skills (agent-wrappers)  WRONG
//     — no bullet at all —                  spec-kit-command   .agents/skills, .claude/skills
//                                                                          (command-wrappers)   ABSENT
//
// Five of the five bullets flattened `Roots` + `Selector` back into a single prose path — the
// pre-#1092 shape, where a glob stood in for the narrowing and the narrowing was really a hard-coded
// branch of the resolver. One named a path that stopped being the canonical body in #1080/#1082. And
// the surface the section never mentioned is the one whose roots are widest.
//
// CORRECTING THE BULLETS IS NOT THE FIX, for the reason #1099 spelled out: rows hand-corrected today
// are stale again on the next surface change, which is exactly how both copies got here. So this file
// re-derives the surfaces from the CODE at runtime and the bullets from the DOCUMENT at runtime and
// compares them, failing in BOTH directions.
//
// ON REUSING #1099'S COMPARISON. #1111's acceptance criterion 2 says `mismatches` in
// `Feature1099SurfaceInventoryContractTests` "is factored out precisely so a second document can
// reuse it". It is factored out, but it is `let private` over a `type private InventoryRow`, so no
// second module can call it — and #1111's own "Not in scope" fences off that test file, which is
// where the accessibility would have to change. The comparison below is therefore a deliberate
// second implementation, shaped for a document that publishes id/roots/selector and (rightly) not
// `Kind`, which is `skill-surface-inventory.md`'s column. Unlike the declarations these two files
// restate, both copies are executable and both are checked, so neither can drift in silence.
// Factoring them into one shared helper is filed as a follow-up rather than done by quietly crossing
// a written scope boundary.
//
// A MALFORMED BULLET IS A FAILURE, NEVER A SKIP. The parser below reports every bullet it could not
// read as a mismatch. Dropping unparseable bullets on the floor would make the pre-#1111 section
// PASS — five bullets naming five wrong paths would parse as zero bullets and, if zero bullets were
// also allowed, as zero disagreements. The historical-text control at the bottom holds the parser to
// that: the section as it actually stood must be rejected by this file.

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FSharp.Reflection
open Rendering.Harness
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private contractPath =
    Path.Combine(repositoryRoot, "specs", "168-skill-parity-evidence", "contracts", "skill-parity-cli.md")

let private sectionHeading = "## Default Repository Surfaces"

let private declaredSurfaces () = SkillParity.discoverDefaultSurfaces repositoryRoot

// ---------------------------------------------------------------------------------------------
// Reading the document
// ---------------------------------------------------------------------------------------------

/// One bullet of `## Default Repository Surfaces`, projected onto the two halves a surface declares.
/// `Kind` is deliberately absent: this section says what a default run READS, and the kind of each
/// surface is `skill-surface-inventory.md`'s column. Restating it here would create a third copy.
type private SurfaceBullet =
    { SurfaceId: string
      Roots: string list
      Selector: string }

/// Everything the section yielded: the bullets that parsed, the bullets that did not, and every code
/// span in the section including the prose. All three are inputs to the comparison — a bullet that
/// could not be read is a failure, not an absence.
type private ParsedSection =
    { Bullets: SurfaceBullet list
      Malformed: string list
      Spans: string list }

let private codeSpanPattern = Regex(@"`([^`]+)`", RegexOptions.Compiled)

let private spans (text: string) =
    codeSpanPattern.Matches text
    |> Seq.map (fun m -> m.Groups.[1].Value.Trim())
    |> List.ofSeq

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
    |> Option.map (List.takeWhile (fun (line: string) -> not (line.StartsWith("#", StringComparison.Ordinal))))

/// A bullet's data is its FIRST physical line; indented continuation lines are prose and are not
/// parsed. That split is what lets each bullet carry an explanation without the explanation becoming
/// part of the checked claim.
let private isBulletStart (line: string) =
    line.StartsWith("- ", StringComparison.Ordinal)

let private emDash = '—'

/// The grammar every bullet must satisfy:
///
///     - `<surface-id>` — roots `<root>`[, `<root>`…] — selector `<selector>`
///
/// Three em-dash-separated parts, each of them labelled or a bare span, and every value a code span.
/// A cell that spells its value in prose yields no span and is reported as malformed — which is the
/// `src/*/skill/SKILL.md` shape this item exists to stop.
let private parseBullet (line: string) =
    let body = line.Substring(2).Trim()
    let parts = body.Split(emDash) |> Array.map (fun part -> part.Trim()) |> Array.toList

    let labelled (label: string) (part: string) =
        if part.StartsWith(label, StringComparison.OrdinalIgnoreCase) then
            Some(part.Substring(label.Length).Trim())
        else
            None

    match parts with
    | [ idPart; rootsPart; selectorPart ] ->
        match spans idPart, labelled "roots" rootsPart, labelled "selector" selectorPart with
        | [ surfaceId ], Some rootsText, Some selectorText ->
            match spans rootsText, spans selectorText with
            | (_ :: _ as roots), [ selector ] ->
                Ok
                    { SurfaceId = surfaceId
                      Roots = roots
                      Selector = selector }
            | [], _ -> Error(sprintf "bullet '%s' names no root as a code span" surfaceId)
            | _, selectors ->
                Error(sprintf "bullet '%s' publishes %d selector spans; a surface declares exactly one" surfaceId (List.length selectors))
        | ids, roots, selector ->
            Error(
                sprintf
                    "bullet %A does not read as `<surface-id>` %c roots `<root>`… %c selector `<selector>` (ids: %A, roots part: %A, selector part: %A)"
                    body
                    emDash
                    emDash
                    ids
                    roots
                    selector)
    | _ -> Error(sprintf "bullet %A has %d em-dash-separated parts; the grammar has three" body (List.length parts))

let private parseSection (document: string) =
    match sectionLines document with
    | None -> None
    | Some lines ->
        let parsed = lines |> List.filter isBulletStart |> List.map parseBullet

        Some
            { Bullets = parsed |> List.choose (function Ok bullet -> Some bullet | Error _ -> None)
              Malformed = parsed |> List.choose (function Error problem -> Some problem | Ok _ -> None)
              Spans = lines |> List.collect spans }

let private parseContract () = parseSection (File.ReadAllText contractPath)

// ---------------------------------------------------------------------------------------------
// The comparison
// ---------------------------------------------------------------------------------------------

/// Every way the section and the code disagree, as sentences. Empty means they agree. Both
/// directions are enumerated on purpose: a surface with no bullet is as much a failure as a bullet
/// for no surface, and the first of those is what the pre-#1111 section looked like.
let private mismatches (section: ParsedSection) (surfaces: SkillParity.SkillSurface list) =
    let bulletIds = section.Bullets |> List.map (fun bullet -> bullet.SurfaceId) |> Set.ofList
    let surfaceIds = surfaces |> List.map (fun surface -> surface.SurfaceId) |> Set.ofList

    let malformed =
        section.Malformed
        |> List.map (sprintf "a bullet in %s could not be read as a surface declaration: %s" sectionHeading)

    let missingBullets =
        Set.difference surfaceIds bulletIds
        |> Set.toList
        |> List.map (fun surfaceId ->
            sprintf "surface '%s' is declared by discoverDefaultSurfaces and has no bullet in %s" surfaceId sectionHeading)

    let extraBullets =
        Set.difference bulletIds surfaceIds
        |> Set.toList
        |> List.map (sprintf "%s has a bullet for '%s', which discoverDefaultSurfaces does not declare" sectionHeading)

    let cellMismatches =
        surfaces
        |> List.collect (fun surface ->
            match section.Bullets |> List.tryFind (fun bullet -> bullet.SurfaceId = surface.SurfaceId) with
            | None -> []
            | Some bullet ->
                let expectedSelector = SkillParity.surfaceSelectorToken surface.Selector

                [ if bullet.Roots <> surface.Roots then
                      yield
                          sprintf
                              "surface '%s': the section publishes roots %A and the resolver reads %A"
                              surface.SurfaceId
                              bullet.Roots
                              surface.Roots
                  if bullet.Selector <> expectedSelector then
                      yield
                          sprintf
                              "surface '%s': the section publishes selector '%s' and the resolver uses '%s'"
                              surface.SurfaceId
                              bullet.Selector
                              expectedSelector ])

    malformed @ missingBullets @ extraBullets @ cellMismatches

let private remedy =
    "Correct the bullet, or the declaration, so they agree. Nothing regenerates this section, so #1111 pins it — as #1099 pinned the same declaration in skill-surface-inventory.md."

/// Every `SurfaceSelector` case, by reflection, so a case added tomorrow is covered without anyone
/// remembering to extend this file.
let private everySelector () =
    FSharpType.GetUnionCases typeof<SkillParity.SurfaceSelector>
    |> Array.toList
    |> List.filter (fun case -> Array.isEmpty (case.GetFields()))
    |> List.choose (fun case ->
        match FSharpValue.MakeUnion(case, [||]) with
        | :? SkillParity.SurfaceSelector as selector -> Some selector
        | _ -> None)

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

            let movedRoot =
                surfaces
                |> List.mapi (fun index surface ->
                    if index = 0 then
                        { surface with Roots = [ "docs/product/ant-design/skill/SKILL.md" ] }
                    else
                        surface)

            Expect.isNonEmpty
                (mismatches section movedRoot)
                "a surface whose ROOT moved without the section moving with it must be reported — this is #1082 happening again"

            let otherSelector =
                let first = List.head surfaces

                match everySelector () |> List.tryFind (fun selector -> selector <> first.Selector) with
                | Some replacement -> { first with Selector = replacement } :: List.tail surfaces
                | None ->
                    failtest
                        "non-vacuity: SurfaceSelector defines more than one case, so 'the selector changed' is a perturbation that can be expressed at all"

            Expect.isNonEmpty
                (mismatches section otherSelector)
                "a surface whose SELECTOR changed without the section changing with it must be reported — the half a single prose path could never express"

            let seventhSurface =
                { List.head surfaces with SurfaceId = "fsgg-1111-undocumented-surface" } :: surfaces

            Expect.isNonEmpty
                (mismatches section seventhSurface)
                "a surface declared with NO bullet must be reported; that is exactly how `spec-kit-command` went unmentioned here for three issues"

            Expect.isNonEmpty
                (mismatches { section with Bullets = List.tail section.Bullets } surfaces)
                "and a bullet deleted from the section must be reported too, so the check is not satisfied by an empty document"

            Expect.isNonEmpty
                (mismatches { section with Malformed = [ "synthetic" ] } surfaces)
                "a bullet the parser could not read must count as a disagreement, never as a bullet that is not there"
        }

        test "the section as it stood before #1111 is rejected by this check" {
            // The regression control, and the strongest statement this file can make: the parser is
            // fed the ACTUAL pre-item text, and must reject it. A parser that silently dropped the
            // bullets it cannot read would find zero bullets, zero disagreements, and pass on the
            // very document this item was filed about.
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

        test "every path-shaped code span in the section is a root the resolver actually reads" {
            // `mismatches` already pins the roots by equality. This states the rule over the WHOLE
            // section, prose included, so a path cannot re-enter through an explanatory sentence the
            // way `.claude/skills/fs-gg-ant-design/SKILL.md` did — it survived #1080, #1082, #1098 and
            // #1099 sitting in a bullet nothing compared. A span carrying a separator is a claim about
            // where the checker looks; anything else in this section is a token or an identifier.
            let section = parsedOrFail ()
            let surfaces = declaredSurfaces ()
            let declaredRoots = surfaces |> List.collect (fun surface -> surface.Roots) |> Set.ofList

            Expect.isNonEmpty section.Spans "non-vacuity: the section's code spans were read and are not an empty list"
            Expect.isNonEmpty declaredRoots "non-vacuity: the resolver declares at least one root to compare against"

            for span in section.Spans |> List.filter (fun span -> span.Contains '/') do
                Expect.isFalse
                    (span.Contains '*' || span.Contains '?')
                    (sprintf
                        "the section publishes path `%s`, which contains a glob metacharacter — narrowing is the surface's SELECTOR, and a root is where it LOOKS. This is the pre-#1092 shape"
                        span)

                Expect.isTrue
                    (declaredRoots.Contains span)
                    (sprintf
                        "the section publishes path `%s`, which is not a root any surface declares. Declared roots are %A. %s"
                        span
                        (Set.toList declaredRoots)
                        remedy)

            for bullet in section.Bullets do
                for root in bullet.Roots do
                    let absolute = Path.Combine(repositoryRoot, root.Replace('/', Path.DirectorySeparatorChar))

                    Expect.isTrue
                        (File.Exists absolute || Directory.Exists absolute)
                        (sprintf "bullet '%s' publishes root '%s', which resolves to no file or directory in the repository" bullet.SurfaceId root)
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
                        "'%s' omits `spec-kit-command` entirely, which is the omission #1111 was filed for: the section says what the checker reads and left out one of the surfaces it reads"
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
                (SkillParity.surfaceSelectorToken declared.Selector)
                "the `speckit-` narrowing is named as the surface's SELECTOR"

            for root in commandBullet.Roots do
                Expect.isFalse
                    (root.Contains "speckit")
                    (sprintf
                        "root '%s' writes the `speckit-` narrowing into a path. Before #1092 this surface's declared root WAS `.agents/skills/speckit-*`: English describing a hard-coded branch of the resolver, in the field the report publishes as `Root`"
                        root)
        }
    ]
