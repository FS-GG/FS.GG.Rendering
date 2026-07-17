#!/usr/bin/env dotnet fsi

// check-symbology-skill-parity.fsx — the content-parity gate for the two symbology skills (#279).
//
// WHY THIS EXISTS
// ---------------
// `fs-gg-symbology` ships as two hand-maintained SKILL.md variants that state ONE body of knowledge:
//   * LIBRARY  src/Symbology/skill/SKILL.md                       (the canonical skill, framework-facing)
//   * PRODUCT  template/product-skills/fs-gg-symbology/SKILL.md    (the generated-product variant)
// Every label feature (196->200) was written twice, once per file, and the two drifted in emphasis
// (#279). The existing skill-parity harness only asserts NAME-level parity (a wrapper exists); it says
// nothing about CONTENT. This gate closes that: it fails when the two variants diverge on the
// LOAD-BEARING content a product author relies on — the public Legibility API the CRITIQUE step calls,
// the per-grammar label budgets, the grammar set, the identity-label invariants, and the escape-hatch
// doctrine — while deliberately IGNORING the parts that differ BY DESIGN (see DELIBERATE DIVERGENCES).
//
// It does NOT prescribe structure (the product may keep its own information architecture) and it does
// NOT touch the SkillParity tool surface, so it stales no spec-168 baseline and regenerates no derived
// file. Assert, not generate — per the #279 decision.
//
// DELIBERATE DIVERGENCES (intentionally NOT checked, so they never false-positive)
//   * The product OMITS the fixed channel-grammar table on purpose ("read `Legibility.table`, never a
//     copy — including this page"). This gate never asserts the table appears in both.
//   * Build/Test commands differ (`dotnet ...` in the library vs `./fake.sh ...` in the product).
//   * Links differ by domain (in-tree relative paths vs GitHub blob/tree URLs).
//   * The Public-Contract POINTER differs (`.fsi` source paths vs `docs/api-surface/`).
//   * The library carries framework-only sections (FSI recipe, the numbered feedback loop, Provenance,
//     Grammar-vs-mapping); the product front-loads a `Usage` block. Section presence is NOT checked.
//
// WHAT IS CHECKED (each is a load-bearing fact that drifting silently would mislead a product author)
//   A. Legibility public-API symbols — every `val`/`type` the LIBRARY names inside an ```fsharp fence
//      must also appear in the PRODUCT (minus SymbolWaivers). Catches a new API documented in one only.
//   B. Canonical facts — a small set of exact doctrine/number strings that MUST appear in BOTH variants
//      (the per-grammar line budget, the Grammar value, the escape-hatch move, the ADR/issue refs).
//   C. Identity-label invariants — every bold lead-phrase of the library's invariant list must appear in
//      the product's (minus InvariantWaivers). Catches an invariant added to one variant only.
//
// Exit 0 = parity holds. Exit 1 = drift (printed). Exit 2 = a probed file is missing (fail-loud).
//
// Run locally:  dotnet fsi scripts/check-symbology-skill-parity.fsx
// The Feature279 test in tests/Rendering.Harness.Tests shells this on every PR gate.

open System
open System.IO
open System.Text.RegularExpressions

// #695 convergence: the ONE markdown-fence reader (#669). This script used to hand-roll its own
// ```fsharp regex — the third fence engine that disagreed with the two the tests already share.
#load "../tests/TestSupport/MarkdownFences.fs"
open FS.GG.TestSupport

let repoRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))

let libraryPath = Path.Combine(repoRoot, "src", "Symbology", "skill", "SKILL.md")
let productPath = Path.Combine(repoRoot, "template", "product-skills", "fs-gg-symbology", "SKILL.md")

// ---------------------------------------------------------------------------------------------------
// Waivers — the ONLY symbols/invariants the product may legitimately omit relative to the library.
// Keep this list short and reasoned; an entry here is a documented, reviewed exception, not a TODO.
// Both are empty today: the current variants are in full load-bearing parity, and this gate freezes
// that. If a future library-only symbol/invariant is genuinely product-irrelevant, add it here WITH a
// reason rather than weakening the check.
// ---------------------------------------------------------------------------------------------------
let SymbolWaivers : Set<string> = Set.empty
let InvariantWaivers : Set<string> = Set.empty

// Canonical facts that must appear, verbatim after normalization, in BOTH variants. Normalization
// (below) strips backticks/markdown emphasis, converts the "<=" glyph, and collapses whitespace, so
// these are matched on meaning, not typography.
let CanonicalFacts : (string * string) list =
    [ "per-grammar label line budget", "Token <= 3, Badge <= 2, Ring <= 2"
      "the three-grammar value",        "Grammar = Token | Badge | Ring"
      "the escape-hatch move",          "ask for the channel"
      "the secondary-heading ADR",      "ADR-0102"
      "the escape-hatch worked example (issue #260)", "#260" ]

// ---------------------------------------------------------------------------------------------------

let readFile (path: string) =
    if not (File.Exists path) then
        eprintfn "check-symbology-skill-parity: FILE MISSING — %s" path
        exit 2
    File.ReadAllText path

/// Fold typographic variants onto one form so a fact matches on meaning, not on emphasis/spacing:
/// drop backticks and markdown emphasis, turn the "less-than-or-equal" glyph into "<=", collapse runs
/// of whitespace to a single space.
let normalize (text: string) =
    let stripped = text.Replace("`", "").Replace("*", "").Replace("≤", "<=")
    Regex.Replace(stripped, @"\s+", " ")

/// The body of a section: from its heading to the next heading at `boundary` level or higher (or EOF).
/// Fail-loud (exit 2) if the heading is absent — a structural change to a skill must be looked at, not
/// silently read as an empty section that would make every parity check trivially pass.
let section (name: string) (headingPattern: string) (boundaryPattern: string) (markdown: string) : string =
    let m = Regex.Match(markdown, headingPattern + ".*?(?=" + boundaryPattern + @"|\z)", RegexOptions.Singleline)
    if m.Success then m.Value
    else
        eprintfn "check-symbology-skill-parity: could not locate the '%s' section (skill structure changed?)." name
        exit 2

/// The `## Public Contract` section (to the next h1/h2), where both variants document the module's
/// public surface.
let publicContract = section "## Public Contract" @"(?m)^##\s+Public Contract\b" @"(?m)^#{1,2}\s"

/// The `### The invariants ...` subsection (to the next h1/h2/h3). Both variants carry it with the SAME
/// invariant set, whereas the surrounding sections diverge BY DESIGN (the product compresses the
/// legibility rules into prose and omits the numbered feedback loop), so a whole-document scan would
/// false-positive on that divergence.
let invariantsSection = section "### The invariants" @"(?m)^###\s+The invariants\b" @"(?m)^#{1,3}\s"

/// Every `val NAME` / `type NAME` declared inside an ```fsharp fence WITHIN the Public Contract section.
/// Scoping to that section — rather than the whole document — is deliberate: the FSI-recipe / Usage
/// example fences elsewhere are intentionally DIFFERENT per variant (the product's example is simpler),
/// so scanning them would false-positive on a legitimate example-only edit. Here we compare only the
/// documented public API, which the two variants MUST keep in step.
let fenceSymbols (markdown: string) : Set<string> =
    // Fence-finding is MarkdownFences' one answer now (#695); the val/type NAME extraction stays local —
    // it reads both `val` and `type` for cross-variant parity, a different concern from the `.fsi` val reader.
    let nameRegex = Regex(@"^\s*(?:val|type)\s+([A-Za-z_][A-Za-z0-9_]*)")

    MarkdownFences.scan (publicContract markdown)
    |> MarkdownFences.fsharpLines
    |> List.choose (fun line ->
        let m = nameRegex.Match line.Text
        if m.Success then Some m.Groups.[1].Value else None)
    |> Set.ofSeq

/// The bold lead-phrase of each identity-label invariant bullet in that section: a list item whose text
/// opens with a `**...**` run ("Opt-in, layered zero-drift.", "Inspection-detail.", ...). A new invariant
/// added to one variant only shows up as a lead-phrase present in one set but not the other.
let invariantLeads (markdown: string) : Set<string> =
    Regex.Matches(invariantsSection markdown, @"(?m)^\s*-\s+\*\*(.+?)\*\*")
    |> Seq.map (fun m -> m.Groups.[1].Value.Trim().TrimEnd('.').ToLowerInvariant())
    |> Set.ofSeq

let library = readFile libraryPath
let product = readFile productPath
let libraryN = normalize library
let productN = normalize product

let mutable failures : string list = []
let fail msg = failures <- msg :: failures

let render (s: Set<string>) = s |> Set.toList |> List.sort |> String.concat ", "

// A. Public-Contract API symbols: a `val`/`type` documented in one variant but not the other. Checked
// in BOTH directions — drift is drift whichever variant grew the symbol.
let libSymbols = fenceSymbols library
let prodSymbols = fenceSymbols product
let symbolsOnlyInLib = Set.difference libSymbols prodSymbols - SymbolWaivers
let symbolsOnlyInProd = Set.difference prodSymbols libSymbols - SymbolWaivers
if not (Set.isEmpty symbolsOnlyInLib) then
    fail (sprintf
            "A. API-symbol drift: %d Public-Contract symbol(s) in the LIBRARY skill are absent from the PRODUCT skill: %s\n   -> mirror them into template/product-skills/fs-gg-symbology/SKILL.md, or add a reasoned entry to SymbolWaivers."
            symbolsOnlyInLib.Count (render symbolsOnlyInLib))
if not (Set.isEmpty symbolsOnlyInProd) then
    fail (sprintf
            "A. API-symbol drift: %d Public-Contract symbol(s) in the PRODUCT skill are absent from the LIBRARY skill: %s\n   -> mirror them into src/Symbology/skill/SKILL.md, or add a reasoned entry to SymbolWaivers."
            symbolsOnlyInProd.Count (render symbolsOnlyInProd))

// B. Canonical facts present in both variants.
for (label, fact) in CanonicalFacts do
    let factN = normalize fact
    let inLib = libraryN.Contains factN
    let inProd = productN.Contains factN
    if not (inLib && inProd) then
        let where =
            match inLib, inProd with
            | false, false -> "NEITHER variant"
            | false, true -> "the LIBRARY variant"
            | true, false -> "the PRODUCT variant"
            | true, true -> "(unreachable)"
        fail (sprintf
                "B. Canonical-fact drift: %s (\"%s\") is missing from %s.\n   -> the two skills must agree on this load-bearing fact."
                label fact where)

// C. Identity-label invariants: a bold lead-phrase present in one variant but not the other. Bidirectional.
let libLeads = invariantLeads library
let prodLeads = invariantLeads product
let leadsOnlyInLib = Set.difference libLeads prodLeads - InvariantWaivers
let leadsOnlyInProd = Set.difference prodLeads libLeads - InvariantWaivers
let renderLeads (s: Set<string>) = s |> Set.toList |> List.sort |> List.map (sprintf "\"%s\"") |> String.concat ", "
if not (Set.isEmpty leadsOnlyInLib) then
    fail (sprintf
            "C. Invariant drift: %d invariant lead-phrase(s) in the LIBRARY skill are absent from the PRODUCT skill: %s\n   -> mirror the invariant, or add a reasoned entry to InvariantWaivers."
            leadsOnlyInLib.Count (renderLeads leadsOnlyInLib))
if not (Set.isEmpty leadsOnlyInProd) then
    fail (sprintf
            "C. Invariant drift: %d invariant lead-phrase(s) in the PRODUCT skill are absent from the LIBRARY skill: %s\n   -> mirror the invariant, or add a reasoned entry to InvariantWaivers."
            leadsOnlyInProd.Count (renderLeads leadsOnlyInProd))

match List.rev failures with
| [] ->
    printfn "check-symbology-skill-parity: OK — the two fs-gg-symbology variants are in load-bearing content parity."
    printfn "  library %s" (Path.GetRelativePath(repoRoot, libraryPath))
    printfn "  product %s" (Path.GetRelativePath(repoRoot, productPath))
    printfn "  checked: %d fenced API symbols, %d canonical facts, %d identity-label invariants."
        libSymbols.Count (List.length CanonicalFacts) libLeads.Count
    exit 0
| msgs ->
    eprintfn "check-symbology-skill-parity: DRIFT — the two fs-gg-symbology variants have diverged on load-bearing content:\n"
    msgs |> List.iteri (fun i m -> eprintfn "  [%d] %s\n" (i + 1) m)
    eprintfn "The two skills restate one body of knowledge (#279). A load-bearing change to one MUST land in the other."
    exit 1
