module ReleaseOnlyTwinLockstepTests

// FS-GG/FS.GG.Rendering#366 — the meta-guard that keeps a PR-time twin paired with the release-only
// rule it mirrors, so the mirror cannot silently DESYNC.
//
// WHY THIS EXISTS. #350, #382 and this issue's BOM slice each hoist a release-only check into a cheap
// static twin in this slnx-resident project, so drift reds the PR instead of only the release lane.
// But a hoisted twin is a COPY, and a copy rots: someone edits the release-only rule (renames it,
// points it at a different source file, deletes it) and forgets the twin — or the reverse. The twin
// then quietly diverges from the rule it claims to mirror, and the PR gate goes back to lying. That
// silent desync is the core risk the issue names.
//
// WHAT IT LOCKS. A registry of (twin, release-only counterpart) pairs. For each pair this asserts,
// STATICALLY (it only reads the test sources as text — it never compiles or runs them):
//   L-EXISTS  both endpoints still exist on disk. Rename or delete either side and this reds, forcing
//             the pair to be reconciled in the same PR.
//   L-NAMES   the twin's header text names its release-only counterpart, so a maintainer standing in
//             front of one is pointed at the other (documentation lockstep — the precedents already
//             do this; this makes it a checked invariant, not a courtesy).
//   L-RULES   the twin asserts the SAME SET OF RULES as its counterpart, compared by `test "…"` name.
//             This is the direct assertion of the thing this guard exists to make — "do the two check
//             the same things?" — and it is what catches an omitted rule.
//   L-INPUTS  the twin reads the same repository source-of-truth paths its counterpart does. A rule
//             re-pointed at a DIFFERENT input keeps its name, so L-RULES cannot see it and this can.
//   L-FORMS   every path-helper call in a registered file reads through a form this file can actually
//             decode. An undecodable read is a HOLE in L-INPUTS, and it fails loudly rather than
//             being skipped in silence (FS-GG/.github#266's fails-open rule, applied to this gate).
//   L-CLOSED  every `*CoherenceTests.fs` in this project is registered here. A new twin cannot be
//             added without declaring the release-only rule it pairs with — so no twin escapes the
//             lockstep guard by simply being new.
//
// WHY L-RULES EXISTS, AND WHY L-INPUTS IS NOT ENOUGH (#612). L-INPUTS came first, and it proxied
// "do the two check the same things?" with "do the two read the same files?". That proxy is not
// merely weaker — it was BLIND to the one failure it most needed to see. `ApiSurfaceMirrorCoherence`
// omitted M-MIR (#437's content check — the rule that catches a mirror teaching a `val` signature
// `src` has replaced) for months, and L-INPUTS was green the whole time. #594 and #616 have since
// hoisted M-MIR and P-PEND, so that instance is closed; what was NOT closed is the reason it went
// unseen, and that is this file's job.
//
// The reason is worth stating exactly, because the obvious fix does not work. M-MIR's one extra input
// is `repositoryPath $"src/{directory}"` — interpolated, which the old literal-only extractor could
// not match at all. But teaching it interpolation does NOT recover the bug: the twin's own
// `isCrossRepo` ALREADY read that same interpolated path, so the input SETS compared equal either
// way. (Checked against the real pre-#616 twin, not reasoned about.) The twin genuinely read every
// input its counterpart read, and simply asserted LESS with them. No sharpening of an input extractor
// reaches that. The rules are the subject; the inputs were only ever a shadow of them — so L-RULES
// asserts the subject directly, and L-INPUTS stays for what it alone can see: a rule that keeps its
// name but is quietly re-pointed at a different file.
//
// Adding a twin: give it the `…CoherenceTests.fs` name, register it below with its release-only
// counterpart, and keep the two asserting the same rules. If a twin must legitimately read a
// different input set than its counterpart, set `sharedInputs = false` and say why; if it cannot be
// compared rule-for-rule at all, set `mirroredRules = false` and say why. Either flag turns a silent
// divergence into a documented, reviewed decision.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repoPath (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

let private twinDirectory = "tests/Rendering.Harness.Tests"

/// One hoisted rule: the slnx-resident twin, the release-only rule it mirrors, the token the twin's
/// header must name it by, and whether the two are expected to read the same repository inputs and
/// assert the same rules.
type private TwinPair =
    { /// Repo-relative path to the PR-time twin (in this project).
      Twin: string
      /// Repo-relative path to the release-only counterpart — a Package.Tests source file, or, for the
      /// instantiated launch case, the template's Product.Tests directory.
      ReleaseOnly: string
      /// A token the twin's header must contain so it points a reader at its counterpart.
      HeaderNames: string
      /// True for a text-mirror pair that must read the same source-of-truth inputs (L-INPUTS).
      SharedInputs: bool
      /// True for a pair whose counterpart is a single test source whose rules the twin mirrors
      /// one-for-one (L-RULES). False only where there is no rule set to compare against.
      MirroredRules: bool }

let private registry =
    [ // #350 — the generated product's per-family default launch host. The release-only counterpart is
      // the INSTANTIATED template/base/tests/Product.Tests, so the twin reads the template source rather
      // than mirroring a Package.Tests file's inputs (SharedInputs = false). It is a DIRECTORY of tests
      // that run against a generated product, not a rule set this twin restates, so there is no
      // `test "…"` set to compare either (MirroredRules = false). L-EXISTS/L-NAMES/L-CLOSED guard it.
      { Twin = $"{twinDirectory}/TemplateLaunchExpressionCoherenceTests.fs"
        ReleaseOnly = "template/base/tests/Product.Tests"
        HeaderNames = "Product.Tests"
        SharedInputs = false
        MirroredRules = false }
      // #382 — the ADR-0024 audio profile wiring shape. Text-mirror of a release-only Package.Tests rule.
      { Twin = $"{twinDirectory}/TemplateAudioProfileWiringCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/AudioProfileWiringTests.fs"
        HeaderNames = "AudioProfileWiringTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the FS.GG.UI BOM membership parity. Text-mirror of a release-only Package.Tests rule.
      { Twin = $"{twinDirectory}/BomMembershipCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature207BomMembershipTests.fs"
        HeaderNames = "Feature207BomMembershipTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the bundled api-surface mirror contract (M-REF/M-PTR/M-PROV/M-MIR/P-PEND + the #259
      // completeness checks). Verbatim text-mirror of a release-only Package.Tests rule. THE pair #612
      // was about: the twin silently omitted M-MIR's rules and L-INPUTS could not see it, because
      // M-MIR's only extra input is `repositoryPath $"src/{directory}"` — a path the twin's
      // `isCrossRepo` already read. #594/#616 hoisted the missing rules; L-RULES is what now keeps them
      // hoisted.
      { Twin = $"{twinDirectory}/ApiSurfaceMirrorCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/ApiSurfaceMirrorTests.fs"
        HeaderNames = "ApiSurfaceMirrorTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the skill-manifest materializes-when + supplied-by coherence against template.json.
      // Text-mirror of a release-only Package.Tests rule: both read the skill-manifest and template.json
      // and evaluate their conditions over the same parameter grid, so they share the same two inputs.
      { Twin = $"{twinDirectory}/SkillMaterializesWhenCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs"
        HeaderNames = "Feature238SkillMaterializesWhenTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the collision-safe Vec2 wiring gate (#138). Text-mirror of a release-only Package.Tests
      // rule: both read the fragment source, the base Model.fs, Product.fsproj and template.json, and
      // scan them for the naming invariant + delivery wiring, so they share the same four inputs.
      { Twin = $"{twinDirectory}/CollisionSafeVec2CoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature250CollisionSafeVec2Tests.fs"
        HeaderNames = "Feature250CollisionSafeVec2Tests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the fs-gg-game-core product-skill surface guard (#73). Text-mirror of a release-only
      // Package.Tests rule: both read the shipped SKILL.md, the five packed Game.Core .fsi, and the
      // template's Directory.Packages.props + Product.fsproj, and assert the cited members resolve +
      // the pin/reference exists, so they share the same eight inputs.
      { Twin = $"{twinDirectory}/GameCoreSkillCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature240GameCoreSkillTests.fs"
        HeaderNames = "Feature240GameCoreSkillTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the fs-gg-collision skill + import-and-adapt helper wiring guard (#246). Text-mirror of a
      // release-only Package.Tests rule: both read the collision SKILL.md, the Collision.fs fragment,
      // Product.fsproj, the game-core SKILL.md, template.json and the skill-manifest, and assert the same
      // name/reuse/gate/delete-safe/pointer invariants, so they share the same six inputs.
      { Twin = $"{twinDirectory}/CollisionSkillCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature246CollisionSkillTests.fs"
        HeaderNames = "Feature246CollisionSkillTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the fs-gg-visibility skill + import-and-adapt helper wiring guard (#247). Text-mirror of
      // a release-only Package.Tests rule: both read the visibility SKILL.md, the Visibility.fs fragment,
      // Product.fsproj, the model-swap SKILL.md, the scaffold-map, template.json and the skill-manifest,
      // and assert the same name/reuse/#261-cull/gate/delete-safe/swap-guidance invariants, so they
      // share the same seven inputs.
      { Twin = $"{twinDirectory}/VisibilitySkillCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature247VisibilitySkillTests.fs"
        HeaderNames = "Feature247VisibilitySkillTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the fs-gg-line-drawing skill + import-and-adapt helper wiring guard (#248). Text-mirror of
      // a release-only Package.Tests rule: both read the line-drawing SKILL.md, the LineDrawing.fs
      // fragment, Product.fsproj, the model-swap SKILL.md, the scaffold-map, template.json and the
      // skill-manifest, and assert the same name/Cell-reuse/gate/delete-safe/swap-guidance invariants, so
      // they share the same seven inputs.
      { Twin = $"{twinDirectory}/LineDrawingSkillCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature248LineDrawingSkillTests.fs"
        HeaderNames = "Feature248LineDrawingSkillTests"
        SharedInputs = true
        MirroredRules = true }

      // #366 — the fs-gg-grids skill + import-and-adapt helper wiring guard (#249). Text-mirror of a
      // release-only Package.Tests rule: both read the grids SKILL.md, the Grids.fs fragment,
      // Product.fsproj, the model-swap SKILL.md, the scaffold-map, template.json and the skill-manifest,
      // and assert the same name/Cell+Point-reuse/gate/delete-safe/swap-guidance invariants, so they
      // share the same seven inputs.
      { Twin = $"{twinDirectory}/GridsSkillCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature249GridsSkillTests.fs"
        HeaderNames = "Feature249GridsSkillTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the fs-gg-audio product-skill surface guard (#160, ADR-0024 step 4). Text-mirror of a
      // release-only Package.Tests rule: both read the audio SKILL.md and the whole bundled api-surface
      // tree and assert the cited Audio.<member>s resolve, the cited surface is the shipped one, the
      // retired Canvas audio doc copy stays gone, and every bundled .fsi's namespace carries its package
      // directory. The Audio.Core/Host .fsi paths are read through `repositoryPath ("template/base/" + …)`
      // — a CONCATENATION, which L-INPUTS decodes to its literal prefix, identically on both sides.
      { Twin = $"{twinDirectory}/AudioSkillSurfaceCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/AudioSkillSurfaceTests.fs"
        HeaderNames = "AudioSkillSurfaceTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the swap-checklist + build-help-banner template-authoring gate (spec 242, #75).
      // Text-mirror of a release-only Package.Tests rule: both read the per-family SWAP-CHECKLIST.md
      // files, the raw scaffold sources, and the three build-banner surfaces (build.fsx/build.sh/
      // product.md), and assert the same NO-PHANTOM/COVERAGE/anchor/literal-path + banner-sync
      // invariants. The checklist/scaffold reads go through `repositoryPath (sprintf …)`, which
      // L-INPUTS decodes to the format string, identically on both sides.
      { Twin = $"{twinDirectory}/SwapChecklistCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/SwapChecklistTemplateTests.fs"
        HeaderNames = "SwapChecklistTemplateTests"
        SharedInputs = true
        MirroredRules = true }
      // #366 — the embedded-token substitution guard (#282). Faithful text-mirror of a release-only
      // Package.Tests rule, but SharedInputs = false: its counterpart derives the scanned file set via
      // `ScaffoldSources.substitutionSubjectFiles`, not the `repositoryPath`/`repo` helper L-INPUTS keys
      // on, so the input-set equality cannot be mechanically checked for this shape (a byte-faithful hoist
      // extracts zero literal inputs and would trip L-INPUTS's non-empty guard). L-RULES still compares
      // the two rule-for-rule — the check that actually matters — and L-EXISTS/L-NAMES/L-CLOSED guard the
      // pairing.
      { Twin = $"{twinDirectory}/EmbeddedTokenGuardCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature282EmbeddedTokenGuardTests.fs"
        HeaderNames = "Feature282EmbeddedTokenGuardTests"
        SharedInputs = false
        MirroredRules = true }
      // #366 — the fragment-prose substitution guard (#264). Faithful text-mirror of a release-only
      // Package.Tests rule, SharedInputs = false for the same reason as #282's embedded-token twin: its
      // counterpart derives the scanned set via `ScaffoldSources.files`, not the `repositoryPath`/`repo`
      // helper L-INPUTS keys on. L-RULES still compares the two rule-for-rule.
      { Twin = $"{twinDirectory}/FragmentProseCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/Feature264FragmentProseTests.fs"
        HeaderNames = "Feature264FragmentProseTests"
        SharedInputs = false
        MirroredRules = true }
      // #366 — the scaffold identifier-leak guard (#149/#152). Faithful text-mirror of a release-only
      // Package.Tests rule, SharedInputs = false for the same reason as the #282/#264 token-leak twins:
      // its counterpart derives the scanned set via `ScaffoldSources.files`, not the `repositoryPath`/
      // `repo` helper L-INPUTS keys on. L-RULES still compares the two rule-for-rule.
      { Twin = $"{twinDirectory}/ScaffoldIdentifierLeakCoherenceTests.fs"
        ReleaseOnly = "tests/Package.Tests/ScaffoldIdentifierLeakGuardTests.fs"
        HeaderNames = "ScaffoldIdentifierLeakGuardTests"
        SharedInputs = false
        MirroredRules = true } ]

/// `//` comments, stripped. This is load-bearing, not cosmetic. Three twins' headers literally
/// contain the text `repositoryPath "…"` / `repo "…"` while EXPLAINING that their counterpart does
/// not use that helper — and the old extractor duly read `…` as a repository input. Its doc comment
/// claimed "a path named only in a `//` comment is NOT in this form, so prose references do not leak
/// in"; they were, and did. (Same naive strip the release-only Fsi reader uses: a `//` inside a
/// string literal would truncate its line, which no path-helper call in these files relies on.)
let private code (text: string) =
    text.Split '\n'
    |> Array.map (fun line ->
        match line.IndexOf "//" with
        | -1 -> line
        | i -> line.Substring(0, i))
    |> String.concat "\n"

/// The names a repository path helper goes by across these files. Declared once: it keys BOTH the
/// declaration scan and the declaration strip below, and a third spelling taught to only one of them
/// would silently re-admit the bug the strip exists to prevent (reading `let repositoryPath (rel: …)`
/// as a call whose argument is `(rel: …)`).
let private helperNames = "repositoryPath|repo"

/// The path helpers a test source DECLARES — `let private repositoryPath (rel: string) = …`, or the
/// `repo` spelling two of the pairs use. Keying call-site scanning on the names a file actually
/// declares is what makes L-FORMS's hard fail safe: `repo` is also an ordinary local in these files
/// (`provenanceVersion repo file`), so scanning that name unconditionally would fire on innocent code.
let private declaredHelpers (source: string) =
    Regex.Matches(source, $@"let\s+(?:private\s+)?({helperNames})\s*\(")
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Set.ofSeq

/// A decoded path-helper call. `Resolved` carries a key standing for the input the call reads;
/// `Undecodable` carries the raw argument, and is a hard fail (L-FORMS).
type private Read =
    | Resolved of string
    | Undecodable of string

/// The argument forms that occur in these files, and the key each decodes to:
///
///   repositoryPath "template/base/x"                  -> `template/base/x`               (literal)
///   repositoryPath $"src/{directory}"                 -> `src/{}`                        (interpolated: SHAPE)
///   repositoryPath (sprintf "a/%s/b" x)               -> `a/%s/b`                        (format string)
///   repositoryPath ("template/base/" + citedRelative) -> `template/base/ + <citedRelative>` (concatenation)
///   repositoryPath srcRelative                        -> `<srcRelative>`                 (indirect: the name)
///
/// The last three do not resolve to a path, and that is fine: L-INPUTS compares twin against
/// counterpart, so a form both sides use identically compares equal and a form only ONE side uses
/// does not. What is NOT fine is a form this decoder cannot see at all — hence `Undecodable`, and
/// L-FORMS. The old decoder recognised only the literal, and skipped everything else in SILENCE.
///
/// Each key must keep whatever DISTINGUISHES one read from another, or L-INPUTS silently
/// under-approximates all over again. The concatenation carries its operand for exactly that reason:
/// AudioSkillSurface reads BOTH `"template/base/" + citedSurfaceRelative` and
/// `"template/base/" + citedHostSurfaceRelative`, so keying on the literal prefix alone would collapse
/// two distinct reads into one and let a twin drop the Host surface unseen. That is the #612 bug wearing
/// a different hat.
let private decode (argument: string) : Read =
    let matched pattern = Regex.Match(argument, pattern)
    let literal = matched "^\"([^\"]*)\""
    let interpolated = matched "^\\$\"([^\"]*)\""
    let formatted = matched "^\\(\\s*sprintf\\s+\"([^\"]*)\""
    let concatenated = matched "^\\(\\s*\"([^\"]*)\"\\s*\\+\\s*([A-Za-z_][\\w'.]*)"
    let indirect = matched "^([A-Za-z_][\\w']*)"

    if literal.Success then
        Resolved literal.Groups.[1].Value
    elif interpolated.Success then
        // The SHAPE, not the text: `$"src/{directory}"` -> `src/{}`. Two rules interpolating the same
        // template read the same family of paths, and that is exactly what M-MIR and `isCrossRepo` do.
        Resolved(Regex.Replace(interpolated.Groups.[1].Value, @"\{[^}]*\}", "{}"))
    elif formatted.Success then
        Resolved formatted.Groups.[1].Value
    elif concatenated.Success then
        Resolved $"{concatenated.Groups.[1].Value} + <{concatenated.Groups.[2].Value}>"
    elif indirect.Success then
        Resolved $"<{indirect.Groups.[1].Value}>"
    else
        // Includes a concatenation onto something that is not a plain name — undecodable, so loud.
        Undecodable(argument.Trim())

/// Every path-helper call a test source makes, decoded. Declarations are removed first so the
/// helper's own `let … (rel: string) =` line is not read as a call with a bizarre argument.
/// Pure in the source TEXT, so the decoder is itself testable — see the `#612` list below. A guard
/// that silently under-approximates its own subject is the whole disease #612 names; the cure does
/// not get to be unchecked.
let private readsOfSource (rawSource: string) : Read list =
    let source = code rawSource
    let helpers = declaredHelpers source

    if Set.isEmpty helpers then
        []
    else
        let callSites =
            Regex.Replace(source, $@"(?m)^\s*let\s+(?:private\s+)?(?:{helperNames})\s*\(.*$", "")

        let names = helpers |> String.concat "|"

        // Match the helper NAME, then decode the text that follows it, rather than capturing the rest
        // of the line. A line may hold two calls — `File.Exists(repositoryPath x) || Directory.Exists
        // (repositoryPath y)` is real, elsewhere in Package.Tests — and a rest-of-line capture would
        // swallow the second one into the first's argument and never see it. `decode` is anchored, so
        // handing it the whole remaining text reads exactly one argument.
        Regex.Matches(callSites, $@"\b(?:{names})\s+")
        |> Seq.map (fun m -> decode (callSites.Substring(m.Index + m.Length)))
        |> List.ofSeq

let private readsOf (testSourcePath: string) : Read list =
    readsOfSource (File.ReadAllText testSourcePath)

/// The repository source-of-truth inputs a test source reads (the decodable ones).
let private inputPathsRead (testSourcePath: string) : Set<string> =
    readsOf testSourcePath
    |> List.choose (function
        | Resolved key -> Some key
        | Undecodable _ -> None)
    |> Set.ofList

/// The rules a test source asserts, by `test "…"` name — the direct subject L-INPUTS only proxied.
let private ruleNamesOfSource (rawSource: string) : Set<string> =
    Regex.Matches(code rawSource, "(?m)^\\s*test\\s+\"([^\"]+)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Set.ofSeq

let private ruleNames (testSourcePath: string) : Set<string> =
    ruleNamesOfSource (File.ReadAllText testSourcePath)

let private exists (repoRelative: string) =
    let full = repoPath repoRelative
    File.Exists full || Directory.Exists full

/// A source file that declares the path helper, so `readsOfSource` has a helper to key on.
let private withHelper body =
    "let private repositoryPath (rel: string) = Path.Combine(root, rel)\n" + body

[<Tests>]
let inputDecoderTests =
    testList
        "#612 — the input decoder L-INPUTS/L-FORMS rest on"
        [
          test "it decodes each argument form the registered files actually use" {
              let cases =
                  [ "repositoryPath \"template/base/x\"", Resolved "template/base/x"
                    // The form #612 was ABOUT: invisible to the old literal-only extractor.
                    "repositoryPath $\"src/{directory}\"", Resolved "src/{}"
                    "repositoryPath (sprintf \"a/%s/b\" family)", Resolved "a/%s/b"
                    "repositoryPath (\"template/base/\" + citedRelative)", Resolved "template/base/ + <citedRelative>"
                    "repositoryPath srcRelative", Resolved "<srcRelative>" ]

              for source, expected in cases do
                  Expect.equal
                      (readsOfSource (withHelper source))
                      [ expected ]
                      $"decoding `{source}`"
          }

          // A key must keep what DISTINGUISHES one read from another. AudioSkillSurface really does
          // concatenate the same literal prefix onto two different operands (the Audio.Core and
          // Audio.Host .fsi), so a prefix-only key would collapse them and let a twin drop one of the
          // two reads unseen — #612's under-approximation, reintroduced by its own fix.
          test "it keeps two concatenations onto the same prefix distinct" {
              let reads =
                  readsOfSource (
                      withHelper
                          "let a = repositoryPath (\"template/base/\" + citedSurfaceRelative)\nlet b = repositoryPath (\"template/base/\" + citedHostSurfaceRelative)"
                  )

              Expect.equal
                  (reads |> List.distinct |> List.length)
                  2
                  "two reads sharing a literal prefix must not collapse to one key"
          }

          // The fails-open rule (FS-GG/.github#266) applied to this gate itself. An argument form the
          // decoder cannot read must be a LOUD hole, not a silent one — silence is how #612 lasted.
          test "it reports an unrecognised argument form rather than skipping it" {
              let reads = readsOfSource (withHelper "repositoryPath (Path.Combine(a, b))")

              match reads with
              | [ Undecodable argument ] -> Expect.stringContains argument "Path.Combine" "the raw argument is reported"
              | other -> failtestf "an undecodable read must surface as Undecodable, not be skipped: %A" other
          }

          // The prose leak that was live: three twins' headers contain the TEXT `repositoryPath "…"`
          // while explaining that their counterpart does not use that helper, and the old extractor
          // read `…` as a repository input — under a doc comment claiming prose could not leak in.
          test "it does not read a path-helper call that is only mentioned in a comment" {
              let source = withHelper "// the counterpart derives its set, not via `repositoryPath \"…\"` / `repo \"…\"`"
              Expect.isEmpty (readsOfSource source) "a helper call named only in a comment is prose, not a read"
          }

          // A rest-of-line capture swallows the second call into the first's argument and never sees
          // it — a fails-open hole in the guard whose whole point is not to fail open.
          test "it sees both calls when a line holds two" {
              let reads = readsOfSource (withHelper "File.Exists(repositoryPath \"a\") || File.Exists(repositoryPath \"b\")")
              Expect.equal reads [ Resolved "a"; Resolved "b" ] "both calls on one line are decoded"
          }

          test "it reads the rules a source asserts, and ignores a test name inside a comment" {
              let source =
                  "test \"a real rule\" {\n    ()\n}\n// test \"a rule named only in prose\"\n"

              Expect.equal
                  (ruleNamesOfSource source)
                  (Set.ofList [ "a real rule" ])
                  "L-RULES reads asserted rules, not prose about them"
          }
        ]

[<Tests>]
let releaseOnlyTwinLockstepTests =
    testList
        "#366 — release-only ↔ PR-time twin lockstep"
        [
          // L-EXISTS — neither endpoint may vanish without the other being reconciled in the same PR.
          test "every registered twin and its release-only counterpart still exist" {
              for pair in registry do
                  Expect.isTrue (exists pair.Twin) (sprintf "twin %s exists" pair.Twin)
                  Expect.isTrue
                      (exists pair.ReleaseOnly)
                      (sprintf
                          "release-only counterpart %s exists (if it was renamed or removed, update the twin %s and this registry in lockstep)"
                          pair.ReleaseOnly
                          pair.Twin)
          }

          // L-NAMES — the twin header must point a reader at the rule it mirrors.
          test "every twin header names its release-only counterpart" {
              for pair in registry do
                  let twinText = File.ReadAllText(repoPath pair.Twin)
                  Expect.stringContains
                      twinText
                      pair.HeaderNames
                      (sprintf
                          "twin %s must name its release-only counterpart '%s' so the two stay discoverable from each other"
                          pair.Twin
                          pair.HeaderNames)
          }

          // L-RULES — the check L-INPUTS was only approximating: does the twin assert the same rules?
          // A twin that reads every input its counterpart reads and asserts FEWER rules with them is
          // the real failure mode, and it is the one that actually happened (#612 — M-MIR).
          test "every mirrored pair asserts the same set of rules" {
              for pair in registry |> List.filter (fun p -> p.MirroredRules) do
                  let twinRules = ruleNames (repoPath pair.Twin)
                  let releaseRules = ruleNames (repoPath pair.ReleaseOnly)

                  // Fail loud, never vacuous: an extraction that matched nothing on both sides would
                  // satisfy set-equality trivially while guarding nothing.
                  Expect.isNonEmpty
                      (Set.toList twinRules)
                      (sprintf "twin %s must assert at least one `test \"…\"` rule" pair.Twin)

                  let onlyInTwin = Set.difference twinRules releaseRules |> Set.toList
                  let onlyInRelease = Set.difference releaseRules twinRules |> Set.toList

                  Expect.isEmpty
                      onlyInTwin
                      (sprintf
                          "twin %s asserts rule(s) its release-only counterpart %s does not — the mirror has diverged: %A"
                          pair.Twin
                          pair.ReleaseOnly
                          onlyInTwin)
                  Expect.isEmpty
                      onlyInRelease
                      (sprintf
                          "release-only %s asserts rule(s) the twin %s does NOT — the twin silently checks less than the rule it claims to mirror, so a PR can drift past it and red only the release lane. Hoist them: %A"
                          pair.ReleaseOnly
                          pair.Twin
                          onlyInRelease)
          }

          // L-INPUTS — a faithful text-mirror reads exactly the inputs its counterpart does. L-RULES
          // catches an omitted rule; this catches the rule that KEEPS its name and is quietly
          // re-pointed at a different file.
          test "every text-mirror pair reads the same repository source-of-truth inputs" {
              for pair in registry |> List.filter (fun p -> p.SharedInputs) do
                  let twinInputs = inputPathsRead (repoPath pair.Twin)
                  let releaseInputs = inputPathsRead (repoPath pair.ReleaseOnly)

                  // Fail loud, never vacuous: an extraction that silently matched nothing on both sides
                  // would satisfy set-equality trivially while guarding nothing.
                  Expect.isNonEmpty
                      (Set.toList twinInputs)
                      (sprintf "twin %s must read at least one repository input via the path helper" pair.Twin)

                  let onlyInTwin = Set.difference twinInputs releaseInputs |> Set.toList
                  let onlyInRelease = Set.difference releaseInputs twinInputs |> Set.toList

                  Expect.isEmpty
                      onlyInTwin
                      (sprintf
                          "twin %s reads input(s) its release-only counterpart %s does not — the mirror has diverged: %A"
                          pair.Twin
                          pair.ReleaseOnly
                          onlyInTwin)
                  Expect.isEmpty
                      onlyInRelease
                      (sprintf
                          "release-only %s reads input(s) the twin %s does not — the twin is stale against the rule it mirrors: %A"
                          pair.ReleaseOnly
                          pair.Twin
                          onlyInRelease)
          }

          // L-FORMS — L-INPUTS is a COMPLETENESS claim over inputs, so it must not silently
          // under-approximate its own subject. A path-helper call written in a form the decoder cannot
          // read is invisible to L-INPUTS, and invisibility is how #612 lasted. Fail on it here rather
          // than skipping it: an unreadable read is a decision for a human, not a shrug.
          test "every path-helper call in a registered file reads through a decodable form" {
              let registeredFiles =
                  registry
                  |> List.collect (fun pair -> [ pair.Twin; pair.ReleaseOnly ])
                  |> List.filter (fun path -> File.Exists(repoPath path))
                  |> List.distinct

              let undecodable =
                  registeredFiles
                  |> List.collect (fun path ->
                      readsOf (repoPath path)
                      |> List.choose (function
                          | Undecodable argument -> Some(path, argument)
                          | Resolved _ -> None))

              Expect.isEmpty
                  undecodable
                  (sprintf
                      "these path-helper calls use an argument form L-INPUTS cannot decode, so the inputs they read are INVISIBLE to it — teach `decode` the form (and re-check the pair's L-INPUTS) rather than leaving a hole in the completeness claim: %A"
                      undecodable)
          }

          // L-CLOSED — no twin escapes the guard by being new. Every `*CoherenceTests.fs` here must be
          // registered above, so adding one forces declaring the release-only rule it pairs with.
          test "every *CoherenceTests.fs in this project is registered" {
              let registeredTwins = registry |> List.map (fun p -> Path.GetFileName p.Twin) |> Set.ofList

              let onDisk =
                  Directory.GetFiles(repoPath twinDirectory, "*CoherenceTests.fs")
                  |> Array.map Path.GetFileName
                  |> Set.ofArray

              // Guard against a convention change silently emptying the scan.
              Expect.isNonEmpty
                  (Set.toList onDisk)
                  "at least one *CoherenceTests.fs twin must exist for this lockstep guard to be meaningful"

              let unregistered = Set.difference onDisk registeredTwins |> Set.toList
              Expect.isEmpty
                  unregistered
                  (sprintf
                      "these coherence twins are not registered in ReleaseOnlyTwinLockstepTests — register each with the release-only rule it pairs with: %A"
                      unregistered)
          }
        ]
