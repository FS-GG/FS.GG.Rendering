module SurfaceDocCoverageTests

open System
open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

// Issue #507 (epic FS-GG/.github#423 — "skills describe the intended architecture, not the shipped one").
//
// THE GAP THIS CLOSES. A generated product is GIVEN a signature set — `template/base/docs/api-surface/**`,
// which docs/scaffold-map.md designates authoritative — and is TOLD about it by `template/product-skills/**`.
// Nothing held the two against each other in EITHER direction, so they drifted apart in silence: **242 of
// 428 public `val`s were named in no product skill at all.**
//
// The sharpest instance is a self-contradiction, and it is why this is a gate rather than a cleanup:
// `fs-gg-testing` and `fs-gg-ui-widgets` both MANDATE that "responsiveness evidence must validate pointer
// and keyboard activation separately from screenshot readiness" — and the only instruments that can produce
// it (`respondsProofOf` / `captureRespondsProof`, whose `Inert` verdict is the one thing that tells "renders"
// from "responds", plus the `compositorDiagnostics` / `layoutMetrics` / `responsivenessTimingContribution`
// projection) were public, shipped, and taught in NO skill. The skills demanded the evidence and withheld
// every means of producing it. A product either fabricates it or reads the `.fsi`. (#507 documents them in
// `fs-gg-elmish`, which is the only skill whose profile gate matches the package's reach — see below.)
//
// S-DOC — every public `val` in the shipped api-surface is either NAMED in a product skill, or DECLARED in
//         `surface-doc-ledger.txt`.
//
// Curation is legitimate: not every value wants a paragraph, and #499 records that the typed front door is
// deliberately absent. But it must be a DECISION SOMEBODY MADE rather than an omission nobody noticed, and
// that distinction is the whole issue. The ledger is where the decision is written down.
//
// WHY THE LEDGER CANNOT ROT. A gate whose exemption list only ever grows is a gate that dies of its own
// exemptions, so this one fails THREE ways:
//
//   1. a public val neither documented nor listed        -> the gap grew, and silently. The #507 shape.
//   2. a listed val that a skill NOW documents           -> a stale exemption. Delete the line.
//   3. a listed val the api-surface no longer carries    -> a stale exemption. Delete the line.
//
// (2) and (3) are what make the ledger a ratchet rather than a dumping ground: it can only shrink by
// documenting, and it cannot outlive its subjects. Without them, the honest thing to do with a growing gap
// would be to append to the ledger, and this file would become the record of a problem instead of its fix.
//
// THIS GATE IS THE MIRROR IMAGE OF R-REACH (SkillPackageReachTests, #430), and it belongs beside it:
//   R-REACH  — a skill may not tell you to `open` a package your profile was never given.  (skill => package)
//   S-DOC    — a package you WERE given may not be undocumented by accident.                (package => skill)
// #430's gate was good enough to catch #461's own first draft. This is the direction it never looked.
//
// A NOTE ON WHERE A FIX MAY LAND. Documenting a surface is constrained by R-REACH: a skill may only name
// APIs every profile it materializes on actually receives. `ControlsElmish` reaches [app, sample-pack, game],
// so its instruments go in `fs-gg-elmish` (gated to exactly those) and NOT in `fs-gg-testing`, which also
// ships to `headless-scene` / `governed`. Moving a line off the ledger means finding the skill whose gate
// matches the package's reach — not the skill whose subject matter feels closest.
//
// NOT IN SCOPE HERE, and filed rather than smuggled in: the boundary leaks the OTHER way too — 10
// `val internal` declarations ship into the product api-surface (`ControlsElmish.fsi` 5, `ControlRuntime.fsi`
// 3, `Symbology.fsi` 2), and framework skills teach APIs a product cannot reach. Fixing that means editing
// `src/**` or the mirror machinery, which is outside this item's declared touch-set. S-INT is filed as its
// own item; this file deliberately measures only the PUBLIC surface, so an `internal` leak cannot be
// laundered into "documented" by being listed here.

let private repositoryRoot = RepositoryRoot.value
let private repositoryPath (rel: string) = Path.Combine(repositoryRoot, rel.Replace('/', Path.DirectorySeparatorChar))

let private apiSurfaceRoot = repositoryPath "template/base/docs/api-surface"
let private productSkillsRoot = repositoryPath "template/product-skills"
let private ledgerRel = "tests/Package.Tests/surface-doc-ledger.txt"
let private ledgerPath = repositoryPath ledgerRel

/// A PUBLIC `val` in a shipped signature file. `val internal` is deliberately NOT matched: it is not part of
/// the product's surface (it should not be in the mirror at all — see the note above), and admitting it here
/// would let an internal leak be excused by a ledger line instead of fixed.
let private publicValRegex =
    Regex(@"^\s+val\s+(?!internal\b)(?:inline\s+)?(?<name>[a-z][A-Za-z0-9_]*)\s*:", RegexOptions.Compiled)

/// (name, the shipped .fsi it appears in) for every public val the product is given.
let private shippedSurface =
    Directory.EnumerateFiles(apiSurfaceRoot, "*.fsi", SearchOption.AllDirectories)
    |> Seq.collect (fun path ->
        let rel = Path.GetRelativePath(apiSurfaceRoot, path).Replace('\\', '/')

        File.ReadAllLines path
        |> Seq.choose (fun line ->
            let m = publicValRegex.Match line
            if m.Success then Some(m.Groups.["name"].Value, rel) else None))
    |> Seq.groupBy fst
    |> Seq.map (fun (name, entries) -> name, entries |> Seq.map snd |> Set.ofSeq)
    |> Map.ofSeq

/// Everything the product is TOLD, as one body of prose. Concatenated on purpose: S-DOC asks whether a
/// surface is documented AT ALL, and which skill says it is R-REACH's question, not this one.
let private productSkillProse =
    Directory.EnumerateFiles(productSkillsRoot, "SKILL.md", SearchOption.AllDirectories)
    |> Seq.map File.ReadAllText
    |> String.concat "\n"

/// Word-boundary match, so `withKey` is not credited to a skill that merely says `withKeyboard`.
let private isDocumented (name: string) =
    Regex.IsMatch(productSkillProse, $@"\b{Regex.Escape name}\b")

/// The declared exemptions: `<category>  <name>` lines, `#` comments and blanks ignored.
let private ledger =
    File.ReadAllLines ledgerPath
    |> Array.map (fun l -> l.Trim())
    |> Array.filter (fun l -> l <> "" && not (l.StartsWith "#"))
    |> Array.choose (fun l ->
        let parts = l.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
        if parts.Length >= 2 then Some(parts.[1], parts.[0]) else None)
    |> Map.ofArray

let private commaSep (items: string seq) = String.Join(", ", Seq.sort items)

[<Tests>]
let surfaceDocCoverageTests =
    testList "Surface documentation coverage (S-DOC, #507)" [

        // Every input must be non-empty, or every assertion below passes by checking nothing — which is the
        // failure mode this whole item is about (FS-GG/.github#266).
        test "the inputs are real (S-DOC is not vacuous)" {
            Expect.isNonEmpty (Map.toList shippedSurface) "the shipped api-surface declares public vals"
            Expect.isNonEmpty productSkillProse "the product skills carry prose"
            Expect.isNonEmpty (Map.toList ledger) $"{ledgerRel} declares exemptions"

            Expect.isTrue
                (shippedSurface.Count > 100)
                $"the api-surface parse found {shippedSurface.Count} public vals — far fewer than the ~428 this \
                  repo ships, so the extractor has stopped seeing the surface. That is a defect in this test, \
                  not a smaller surface."
        }

        // S-DOC. The rule.
        test "every public api-surface val is documented in a product skill, or declared in the ledger" {
            let undeclared =
                shippedSurface
                |> Map.toList
                |> List.filter (fun (name, _) -> not (isDocumented name) && not (ledger.ContainsKey name))
                |> List.map (fun (name, files) -> $"{name} ({commaSep files})")

            Expect.isEmpty
                undeclared
                $"these public vals ship to a generated product and are named in NO product skill and in NO \
                  ledger line — so a product author cannot find them and nobody decided they should not. \
                  Document the surface in the skill whose profile gate matches the package's reach (R-REACH), \
                  or add it to {ledgerRel} with a category and a reason. Undeclared: {commaSep undeclared}"
        }

        // The first anti-rot rule: an exemption a skill has since made good must be DELETED, not left to imply
        // the surface is still undocumented. Without this the ledger records the problem forever.
        test "no ledger entry names a surface a product skill now documents" {
            let stale =
                ledger
                |> Map.toList
                |> List.map fst
                |> List.filter isDocumented

            Expect.isEmpty
                stale
                $"these are listed in {ledgerRel} as undocumented, and a product skill now documents them. The \
                  ledger only shrinks: delete the line. Stale: {commaSep stale}"
        }

        // The second: an exemption for a val that no longer exists is a line nobody will ever remove, and it
        // quietly makes the ledger look bigger — i.e. the gap look worse — than it is.
        test "no ledger entry names a surface the api-surface no longer ships" {
            let phantom =
                ledger
                |> Map.toList
                |> List.map fst
                |> List.filter (fun name -> not (shippedSurface.ContainsKey name))

            Expect.isEmpty
                phantom
                $"these are listed in {ledgerRel} but are not public vals in the shipped api-surface — the \
                  surface moved and the exemption outlived it. Delete the line. Phantom: {commaSep phantom}"
        }

        // The responsiveness self-contradiction, asserted where it can be seen. The skills MANDATE this
        // evidence; these are the only surfaces that can produce it. If they ever fall back off the skills,
        // the mandate becomes unmeetable again and this says so by name rather than as one of 236.
        test "the instruments for the responsiveness evidence the skills MANDATE are documented" {
            let mandate = "Responsiveness evidence must validate pointer and keyboard activation"

            Expect.isTrue
                (productSkillProse.Contains mandate)
                "a product skill still mandates responsiveness evidence (if this mandate is ever dropped, drop \
                 this test with it — but do not drop the instruments and keep the mandate, which is #507)"

            for instrument in
                [ "respondsProofOf" // Responded | Inert — the one class that tells "renders" from "responds"
                  "captureRespondsProof"
                  "compositorDiagnostics" // the latency half: routing vs update vs render vs present
                  "layoutMetrics"
                  "responsivenessTimingContribution" ] do
                Expect.isTrue
                    (isDocumented instrument)
                    $"`{instrument}` is public, ships to a generated product, and is the instrument for the \
                      responsiveness evidence the product skills MANDATE — so it must be documented in a skill, \
                      not merely absent from the ledger. It belongs in a skill whose profile gate matches \
                      FS.GG.UI.Controls.Elmish's reach [app, sample-pack, game] — i.e. fs-gg-elmish, NOT \
                      fs-gg-testing (which also ships to headless-scene/governed and may not name it, R-REACH)."
        }
    ]
