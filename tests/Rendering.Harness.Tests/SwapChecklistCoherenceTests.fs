module SwapChecklistCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only swap-checklist / build-help-banner
// template-authoring gate.
//
// WHY THIS EXISTS. The generated per-family `SWAP-CHECKLIST.md` is only useful if the symbols it tells a
// developer to re-point actually EXIST in the shipped scaffold source (no phantoms) and it covers every
// durable must-re-point reader function; and the build-target help banner is only honest if its
// load-bearing Dev/Test/Verify semantics stay in sync across `build.fsx`, `build.sh`, and
// `docs/product.md`. `SwapChecklistTemplateTests` (tests/Package.Tests) enforces both, but Package.Tests
// is RELEASE-ONLY: it is not in `FS.GG.Rendering.slnx` and runs only under `dotnet test … -c Release` in
// release.yml. So a checklist that names a phantom reader, drops a known durable reader, or lists a
// wrong-profile symbol — or a build-banner phrase that drifts out of sync across the three surfaces —
// all compile green on a PR and only red the release lane. That is the "PR-gated tests must be in the
// slnx" gap #350/#382 already closed for the launch host and the audio wiring, and
// #388/#397/#401/#402/#405/#408/#410/#417/#418/#419 have been closing rule-by-rule for the other twins.
//
// WHAT IT LOCKS. SwapChecklistTemplateTests is already fully static and self-contained (no pack, no
// restore, no assembly load): it re-derives everything from the shipped per-family `SWAP-CHECKLIST.md`
// files, the raw scaffold sources (walking the profile `//#if`/`//#else`/`//#endif` markers so
// NO-PHANTOM is profile-aware), and the three build-banner surfaces. So this hoist is a faithful mirror
// — it reads the SAME source-of-truth inputs STATICALLY and asserts the SAME invariants, one gate
// earlier:
//   swap-checklist    — each family checklist ships + references the swap-map anchors; COVERAGE (lists
//                       every known durable re-point reader); NO-PHANTOM (every named reader resolves in
//                       THAT family's scaffold branch); and keeps Product-scoped paths literal.
//   build-help-banner — build.fsx exposes the bare `help` token side-effect-free; build.sh exposes
//                       --help/-h/help; and every load-bearing target phrase is in sync across
//                       build.fsx, build.sh, and docs/product.md.
//
// Kept in deliberate lockstep with tests/Package.Tests/SwapChecklistTemplateTests.fs: the two read the
// same repo inputs and assert the same parity, so a real drift fails BOTH — the release check is
// mirrored earlier, never weakened. ReleaseOnlyTwinLockstepTests guards that pairing (L-INPUTS) so the
// mirror cannot silently desync.

open System.IO
open System.Text
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private checklistPath family =
    repositoryPath (sprintf "template/fragments/swap-checklist/%s/SWAP-CHECKLIST.md" family)

let private checklistText family = File.ReadAllText(checklistPath family)

/// The raw scaffold source files carrying the durable re-point readers, each with ALL profile
/// branches present (the `//#if (profile == …) … //#else … //#endif` markers still in place).
let private rawScaffoldSources =
    [ "LayoutEvidence.fs"; "EvidenceCommands.fs"; "Model.fs"; "View.fs" ]
    |> List.map (fun f -> File.ReadAllText(repositoryPath (sprintf "template/base/src/Product/%s" f)))

/// Slice out only the lines a given family's instantiated tree actually ships, by walking the nested
/// `//#if`/`//#else`/`//#endif` markers. A `//#if` includes lines for `family` when its condition
/// names that profile; the `//#else` is the negation. This is what makes NO-PHANTOM profile-aware:
/// a symbol that exists only in a DIFFERENT profile's branch is excluded, so a wrong-profile listing
/// is caught (the union of all branches would have hidden it). Family names must match the profile
/// literals in the conditions (game / app / governed) — the app family stands in for the app +
/// sample-pack `//#else`, and governed for governed + headless-scene, since those share a branch.
let private branchFor (family: string) (text: string) =
    let namesFamily (cond: string) = cond.Contains(sprintf "\"%s\"" family)
    // Each active nesting level: (this-#if names the family, are we past its #else?).
    let mutable levels : (bool * bool) list = []
    let included () = levels |> List.forall (fun (named, inElse) -> if inElse then not named else named)
    let sb = StringBuilder()

    for raw in text.Replace("\r\n", "\n").Split('\n') do
        let trimmed = raw.TrimStart()
        if trimmed.StartsWith "//#if" then
            levels <- (namesFamily trimmed, false) :: levels
        elif trimmed.StartsWith "//#else" then
            match levels with
            | (named, _) :: rest -> levels <- (named, true) :: rest
            | [] -> ()
        elif trimmed.StartsWith "//#endif" then
            match levels with
            | _ :: rest -> levels <- rest
            | [] -> ()
        elif included () then
            sb.AppendLine raw |> ignore

    sb.ToString()

/// The scaffold source a `family` product actually compiles (shared preamble + that family's branch).
let private familyScaffold family =
    rawScaffoldSources |> List.map (branchFor family) |> String.concat "\n"

/// Per family: the durable must-re-point reader functions (data-model.md) the checklist MUST list,
/// each of which MUST also resolve in the scaffold source.
let private familyReaders =
    [ "game",
      [ "activeGameplayBoundsForSize"; "spawnUsesGameplayRegion"; "scoreTextBounds"
        "layoutEvidenceForSize"; "movementUsesGameplayRegion"; "collisionUsesGameplayRegion"
        "validateGeneratedLayout"; "mapKey"; "layoutEvidenceCommand"; "sceneEvidence" ]
      "app",
      [ "contentLayout"; "activeGameplayBoundsForSize"; "spawnUsesGameplayRegion"; "hudTextBounds"
        "layoutEvidenceForSize"; "movementUsesGameplayRegion"; "collisionUsesGameplayRegion"
        "validateGeneratedLayout"; "mapKey"; "layoutEvidenceCommand"; "sceneEvidence" ]
      "governed",
      [ "layoutEvidenceForSize"; "layoutEvidenceCommand"; "sceneEvidence" ] ]

/// A symbol "resolves" when `source` defines it — tolerant of the normal F# binding forms
/// (`let`, `let private`/`rec`/`inline` in any order, or an `and` in a recursive group) and of the
/// name being followed by a space, `(`, or `:` — so a legitimate definition is not false-failed on
/// style (brittle `"let <sym> "` substring matching would miss `let rec`/`and`/`let <sym>(`).
let private definedIn (source: string) (sym: string) =
    Regex.IsMatch(source, sprintf @"(?m)^\s*(let(\s+(rec|inline|private|mutable))*\s+|and\s+)%s\s*[\s(:]" (Regex.Escape sym))

[<Tests>]
let swapChecklistCoherenceTests =
    testList "#366 twin — swap-checklist-template" [
        test "every family checklist ships and references the swap-map anchors" {
            for family, _ in familyReaders do
                let text = checklistText family
                for anchor in [ "LayoutEvidence.fs"; "EvidenceCommands.fs"; "Model.fs"; "View.fs"; "scaffold-map.md" ] do
                    Expect.stringContains text anchor (sprintf "%s checklist references %s" family anchor)
        }

        test "COVERAGE: each family checklist lists every known durable re-point reader" {
            for family, readers in familyReaders do
                let text = checklistText family
                for reader in readers do
                    Expect.stringContains text reader (sprintf "%s checklist lists re-point reader %s" family reader)
        }

        test "NO PHANTOM: every reader a checklist names is defined in THAT family's scaffold branch" {
            for family, readers in familyReaders do
                let source = familyScaffold family
                for reader in readers do
                    Expect.isTrue (definedIn source reader) (sprintf "%s checklist reader %s resolves in the %s branch a generated tree actually ships" family reader family)
        }

        test "checklists keep Product-scoped paths literal (copyOnly, no sourceName rewrite)" {
            for family, _ in familyReaders do
                let text = checklistText family
                Expect.stringContains text "<ProductDir>" (sprintf "%s checklist keeps the literal <ProductDir> token" family)
        }
    ]

// Feature 242 §2.3: the build-target help banner. Template-authoring gate — the .fsx entry, the
// shell wrapper, and docs/product.md must all carry the load-bearing Dev/Test/Verify semantics, in
// sync (a drifted narrative fails here before it ever ships).
let private buildFsx = File.ReadAllText(repositoryPath "template/base/build.fsx")
let private buildSh = File.ReadAllText(repositoryPath "template/base/build.sh")
let private productMd = File.ReadAllText(repositoryPath "template/base/docs/product.md")

/// The agreed load-bearing phrases that must appear in every surface (no drift).
let private bannerPhrases =
    [ "completion-marker"; "does not compile"; "first real"; "merge-gate audit"; "hard-block" ]

[<Tests>]
let buildHelpBannerCoherenceTests =
    testList "#366 twin — build-help-banner-template" [
        test "build.fsx exposes the bare `help` token (fsi reserves --help/-h on the script path)" {
            Expect.stringContains buildFsx "\"help\"" "build.fsx matches the bare help token"
            Expect.isFalse (buildFsx.Contains("writeLog \"help\"")) "help path writes no completion-marker log (side-effect-free)"
        }

        test "build.sh exposes --help/-h/help at the shell level" {
            Expect.stringContains buildSh "-h|--help|help" "build.sh case handles -h/--help/help"
            Expect.stringContains buildSh "print_help" "build.sh has a help banner function"
        }

        test "SYNC: every load-bearing target phrase appears in build.fsx, build.sh, and product.md" {
            for phrase in bannerPhrases do
                Expect.stringContains buildFsx phrase (sprintf "build.fsx banner states: %s" phrase)
                Expect.stringContains buildSh phrase (sprintf "build.sh banner states: %s" phrase)
                Expect.stringContains productMd phrase (sprintf "docs/product.md agrees on: %s" phrase)
        }
    ]
