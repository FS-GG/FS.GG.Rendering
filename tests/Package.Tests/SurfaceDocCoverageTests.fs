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
///
/// THE NAME MAY END IN A PRIME, and this must take it (#654). `checked` is a reserved F# word, so the
/// attribute is spelled `checked'` — and a name pattern of `[a-z][A-Za-z0-9_]*` stops dead at the quote,
/// never reaches the `:`, and matches NOTHING. `CheckBox.checked'` is therefore public, shipped, named in no
/// skill and in no ledger line, and S-DOC passed it anyway: it satisfied "neither documented nor listed" by
/// being UNPARSEABLE. A surface the extractor cannot see is a surface the gate cannot hold, which is this
/// item's whole thesis one level further down. (TemplateConsumesPinnedApiTests learned the same lesson from
/// the other side in #598: "the member may end in a prime, and it must, or the rule invents violations".)
let private publicValRegex =
    Regex(@"^\s+val\s+(?!internal\b)(?:inline\s+)?(?<name>[a-z][A-Za-z0-9_]*'?)\s*:", RegexOptions.Compiled)

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

/// Everything the product is TOLD, as one body of text. Concatenated on purpose: S-DOC asks whether a
/// surface is documented AT ALL, and which skill says it is R-REACH's question, not this one.
let private productSkillText =
    Directory.EnumerateFiles(productSkillsRoot, "SKILL.md", SearchOption.AllDirectories)
    |> Seq.map File.ReadAllText
    |> String.concat "\n"

/// An opening or closing CommonMark fence: any indent, then 3+ backticks or 3+ tildes, then the info string
/// (the language tag, on an opening fence; empty on a closing one).
///
/// THE INDENT IS UNBOUNDED, and it must be (#664). CommonMark allows a fence up to 3 leading spaces *at the top
/// level*, and this was `^ {0,3}` to match — but a fence nested in a list item is legitimately indented past
/// that, and then the block never opens and **every line of its code lands in the prose buffer**, where it can
/// document nothing. A surface cited only in such a block is reported undeclared, and the author is told to
/// ledger a surface a skill genuinely documents — the same "the rule INVENTS violations" failure
/// `TemplateConsumesPinnedApiTests` records for its own extractor (#598). Nothing in the corpus indents a fence
/// that far today; the point is that nothing stops it. Pairing carries the weight instead: a fence closes only
/// on its own character, repeated at least as many times, wherever either sits.
let private fenceRegex = Regex(@"^\s*(?<fence>`{3,}|~{3,})(?<info>.*)$", RegexOptions.Compiled)

/// The language tags that mean "this block is F#" — the only blocks that can CITE an F# API (#664).
///
/// EVERY SPELLING OF F# BELONGS HERE, and the cost of a missing one is not symmetric. A tag this set does not
/// know is dropped from the corpus — and dropped SILENTLY, because it HAS a language and so does not trip
/// `skillsWithUntaggedFence`. An `fsi` transcript citing `respondsProofOf` would therefore document nothing,
/// S-DOC would report the surface undeclared, and the author would be sent to the ledger to excuse a surface
/// their skill genuinely documents — "the rule INVENTS violations" (#598), which is the failure the fence-indent
/// half of this very item exists to close. A tag wrongly INCLUDED costs a homonym; a tag wrongly OMITTED costs a
/// false accusation against a correct doc. So this errs towards inclusion, and covers every extension F# ships
/// under. (`TemplateConsumesPinnedApiTests` reaches the same place from the other side with a case-insensitive
/// `StartsWith "fsharp"`.)
let private fsharpLanguages = set [ "fsharp"; "fs"; "f#"; "fsx"; "fsi" ]

/// The language an opening fence declares, if it declares one: the first word of the info string.
let private fenceLanguage (info: string) =
    info.Split([| ' '; '\t'; ',' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.tryHead
    |> Option.map (fun lang -> lang.ToLowerInvariant())

/// An inline code span: one or more backticks, the shortest run closed by the same number. Confined to a
/// single line, which is what every citation in these skills actually is.
let private inlineCodeRegex = Regex(@"(?<ticks>`+)(?<code>[^\n`](?:[^\n]*?[^\n`])?)\k<ticks>(?!`)", RegexOptions.Compiled)

/// The code a skill SHOWS, split out from the prose it writes: fenced blocks, plus inline code spans taken
/// from the unfenced lines (the fences themselves would otherwise read as inline delimiters).
///
/// #654: this split is the whole gate. `isDocumented` used to be a bare word-boundary match over the full
/// text, which credits a public val whose name is also an ordinary English word to any sentence that happens
/// to use the word, about anything. FS.GG.Game#240 wrote "the block above can bind all four the same way" —
/// about a fenced code block — into the frozen-mirror `fs-gg-audio` canonical, and S-DOC concluded a product
/// skill now documents the RichText combinator `block`, declared its (correct) ledger line stale, and red-ed
/// `main`. It was doing this for 32 surfaces: `arc`, `background`, `count`, `fill`, `image`, `success`,
/// `warning` … all credited by homonym, 31 of them in no ledger line at all. The guard for #507 had a #507
/// inside it.
///
/// A skill that means `RichText.block` writes it as code — in a `fsharp` block or in backticks — because that
/// is what a citation IS. So credit only code, and the English language stops being able to document an API.
///
/// #664: and only code that could be a CITATION. #654 moved the corpus from prose to code; the homonym moved
/// with it, because "code" meant every fence regardless of language. A skill is free to show a `bash` block —
///
///     git push origin main
///
/// — and `push` is `Game.Core.Resolution.push`, which #654 ledgered as a `tracked` gap (#663). The bare word
/// credits it, S-DOC declares that correct ledger line stale, and `main` reds: FS.GG.Game#240's prose bug,
/// one level in. `push`, `fill`, `image`, `count`, `field`, `origin`, `standard` and `success` are all
/// unremarkable words in a `bash` / `json` / `text` block. So the fenced half of the corpus is F# ONLY.
///
/// A non-F# block is therefore DROPPED — not routed to the prose buffer, which would be the obvious thing and
/// is a trap: prose is mined for inline spans, and a shell block is full of backticks (`` echo `date` ``). It
/// would hand the homonym straight back through the inline half. It cannot document an F# API, so it is not
/// part of the corpus at all.
///
/// Inline spans are untouched: a skill writing `` `push` `` in backticks IS citing the API — that is what
/// backticks mean, whatever the surrounding sentence is about.
///
/// Returns the code, plus the two ways the extraction can have gone wrong — an unclosed fence and an untagged
/// one — which the caller must treat as defects, not curiosities. See `skillsWithUnclosedFence` and
/// `skillsWithUntaggedFence`.
type private SkillCode =
    { Code: string
      UnclosedFence: bool
      UntaggedFences: int }

let private codeReferencesIn (markdown: string) =
    // The delimiter that opened the block, and whether the block is F# — i.e. whether it can cite an API.
    let mutable openFence: (string * bool) option = None
    let mutable untaggedFences = 0
    let code = Text.StringBuilder()
    let prose = Text.StringBuilder()

    for line in markdown.Replace("\r\n", "\n").Split '\n' do
        let fence = fenceRegex.Match line

        match openFence with
        // A fence closes only on the same character, repeated at least as many times as it was opened.
        | Some(opening, _) when fence.Success && fence.Groups.["fence"].Value.StartsWith opening -> openFence <- None
        // Inside a block: F# is the corpus, anything else is dropped on the floor (see above — NOT to prose).
        | Some(_, isFSharp) -> if isFSharp then code.AppendLine line |> ignore
        | None when fence.Success ->
            let language = fenceLanguage fence.Groups.["info"].Value
            if Option.isNone language then untaggedFences <- untaggedFences + 1
            openFence <- Some(fence.Groups.["fence"].Value, language |> Option.exists fsharpLanguages.Contains)
        | None -> prose.AppendLine line |> ignore

    for m in inlineCodeRegex.Matches(prose.ToString()) do
        code.AppendLine m.Groups.["code"].Value |> ignore

    { Code = code.ToString()
      UnclosedFence = Option.isSome openFence
      UntaggedFences = untaggedFences }

let private skillCode =
    Directory.EnumerateFiles(productSkillsRoot, "SKILL.md", SearchOption.AllDirectories)
    |> Seq.map (fun path -> Path.GetRelativePath(productSkillsRoot, path).Replace('\\', '/'), codeReferencesIn (File.ReadAllText path))
    |> Seq.toList

/// Everything the product is told IN CODE — the only thing that can document a surface (#654).
let private productSkillCode =
    skillCode |> Seq.map (fun (_, c) -> c.Code) |> String.concat "\n"

/// A skill whose last fence is never closed. This is the one way the split above can fail SILENTLY and in the
/// dangerous direction: every line after the stray fence is read as code, so the skill's PROSE starts
/// documenting APIs again — which is #654 reopening, in the file that closed it. It cannot be caught by
/// measuring how much is documented, because it makes that number go UP, not down. So it is caught here.
///
/// (An unclosed NON-F# fence is just as bad in the other direction — it swallows the rest of the file into a
/// block that documents nothing — so this counts every fence, whatever its language.)
let private skillsWithUnclosedFence =
    skillCode |> List.filter (fun (_, c) -> c.UnclosedFence) |> List.map fst

/// A skill that opens a fenced block with NO language tag. This is the price of the F#-only corpus (#664), and
/// it is worth paying only because it is collected here: an untagged block is ambiguous — credit it and the
/// homonym is back, drop it and a genuine F# example silently stops documenting its surfaces, which reports
/// them undeclared and sends the author to the ledger to excuse a surface their skill already documents.
///
/// Neither, then. An untagged fence is a defect in the SKILL, and a cheap one to fix: say what the block is.
/// Tagging is already the corpus's own habit — all 61 fences say `fsharp` — so this holds a convention that
/// exists rather than imposing one, and it turns a silent loss of credit into a red gate that names the file.
let private skillsWithUntaggedFence =
    skillCode |> List.filter (fun (_, c) -> c.UntaggedFences > 0) |> List.map fst

/// A skill documents a surface when it CITES it as code. Code-only, so a surface is not credited to a skill
/// that merely uses its name as an English word (#654).
///
/// The boundary is `[\w']`, not `\b`, because a prime is part of the NAME and `\b` cannot see that. `\b` sits
/// between a word char and a non-word char, so `\bchecked'\b` demands a word char after the quote and can
/// never match `CheckBox.checked'` at all — while a bare `\bcount\b` happily matches the `count'` in some
/// other symbol. Treating the quote as a name character settles both: `withKey` is still not credited to a
/// skill that shows `withKeyboard`, and `count` is not credited to one that shows `count'`.
let private citesName (corpus: string) (name: string) =
    Regex.IsMatch(corpus, $@"(?<![\w']){Regex.Escape name}(?![\w'])")

let private isDocumented (name: string) = citesName productSkillCode name

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
            Expect.isNonEmpty productSkillText "the product skills carry prose"
            Expect.isNonEmpty productSkillCode "the product skills cite code"
            Expect.isNonEmpty (Map.toList ledger) $"{ledgerRel} declares exemptions"

            Expect.isTrue
                (shippedSurface.Count > 100)
                $"the api-surface parse found {shippedSurface.Count} public vals — far fewer than the ~428 this \
                  repo ships, so the extractor has stopped seeing the surface. That is a defect in this test, \
                  not a smaller surface."

            // The code extractor is the half that can fail SILENTLY. If `codeReferencesIn` ever stops seeing
            // fences or backticks it returns little or nothing, and then NOTHING is documented — which reds
            // rule 1 loudly, but makes both anti-rot rules pass by checking nothing, and those are the two
            // that keep the ledger a ratchet. So measure the credit it actually issues, not just that it ran.
            let documented = shippedSurface |> Map.toList |> List.filter (fun (name, _) -> isDocumented name)

            Expect.isTrue
                (documented.Length > 100)
                $"only {documented.Length} of {shippedSurface.Count} public vals are cited as code by any \
                  product skill — far fewer than the 100+ that were, so the code extractor has stopped seeing \
                  the skills' fenced blocks or backticks. That is a defect in this test, not a documentation \
                  regression."

            // ...and the floor above CANNOT catch the failure that matters most, because that one makes the
            // number go up. An unclosed fence spills the rest of the file into the code corpus, and the
            // skill's prose starts documenting APIs again — silently, which is exactly #654.
            Expect.isEmpty
                skillsWithUnclosedFence
                $"these product skills end with a code fence still OPEN: {commaSep skillsWithUnclosedFence}. \
                  Every line after the stray fence is then read as CODE, so the skill's PROSE can document an \
                  API again by using its name as an English word — the #654 homonym, reopened in the file that \
                  closed it. Close the fence."

            // The corpus is F#-ONLY (#664), so a fence that does not say what it is cannot be placed: crediting
            // it reopens the homonym, dropping it silently un-documents whatever it cites. Neither is a thing a
            // gate may decide on the author's behalf, so it is the author's to say.
            Expect.isEmpty
                skillsWithUntaggedFence
                $"these product skills open a fenced block with NO language tag: {commaSep skillsWithUntaggedFence}. \
                  Only F#-tagged blocks document a surface (#664 — a bare word in a `bash` block credited \
                  `push`, `fill`, `count` … by homonym), so an untagged block is ambiguous: it either documents \
                  by accident or documents nothing, silently. Say what it is — ```fsharp if it is F# and you \
                  want its APIs credited, ```bash / ```text / ```json if it is not."
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
                (productSkillText.Contains mandate)
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

// THE INSTRUMENT, not the corpus (#664). Everything above measures the SKILLS; a gate is only ever as good as
// the extractor underneath it, and `codeReferencesIn` is the half of S-DOC that can fail silently — which is
// why the vacuity floor watches the credit it issues. These tests watch it directly, on markdown written to
// break it.
//
// They are the only thing that CAN. Both holes are latent: no skill indents a fence 4+ spaces, and all 61
// fences say `fsharp`, so the corpus cannot witness either one and every assertion above passes with the bugs
// in place. A fix pinned only by the corpus would be a fix nothing holds, and would rot the first time a skill
// wrote a `bash` block.
[<Tests>]
let surfaceDocExtractorTests =
    let cites markdown name = citesName (codeReferencesIn markdown).Code name

    testList "S-DOC code extractor (#664)" [

        test "a fence indented inside a list item still opens a block" {
            // Hole 1. `^ {0,3}` is CommonMark's indent allowance AT THE TOP LEVEL, and a fence nested in a list
            // item is legitimately indented past it. The block then never opened, every line of its code landed
            // in the PROSE buffer where it can document nothing, and a surface cited only here was reported
            // undeclared — the gate inventing a violation against a doc that is correct.
            let markdown =
                """
- To drive the program headlessly:

      ```fsharp
      let p = Program.program init update view subs
      ```
"""

            Expect.isTrue (cites markdown "program") "a list-indented `fsharp` fence documents the API it shows"
            Expect.isFalse (cites markdown "headlessly") "...and the prose around it still documents nothing"
        }

        test "a bash block cannot document an API by homonym" {
            // Hole 2 — #654's bug, moved one level in. `Resolution.push` is a real public val that #654 ledgered
            // as a `tracked` gap (#663); an ordinary `git push` line credited it as documented, which declares
            // that correct ledger line STALE and reds `main`. Exactly what FS.GG.Game#240's prose did to `block`.
            let markdown =
                """
Publish the product:

```bash
git push origin main
```
"""

            Expect.isFalse (cites markdown "push") "`git push` does not document `Resolution.push`"
            Expect.isFalse (cites markdown "origin") "...nor does any other ordinary word that happens to be a surface"
        }

        test "an F# block documents the API it shows" {
            let markdown =
                """
```fsharp
let model = Resolution.push model shot
```
"""

            Expect.isTrue (cites markdown "push") "an `fsharp` block IS a citation — that is the whole corpus"
        }

        test "every spelling of F# is a citation, because a missed one accuses a correct doc" {
            // The asymmetry in `fsharpLanguages`. A tag the set does not know is dropped SILENTLY — it has a
            // language, so `skillsWithUntaggedFence` does not catch it — and the surfaces it cites are then
            // reported undeclared. That is the gate inventing a violation against a doc that is correct, which
            // is the same failure as hole 1. `fsi` is not hypothetical here: this repo ships FSI transcripts.
            for tag in [ "fsharp"; "fs"; "f#"; "fsx"; "fsi"; "FSharp" ] do
                let markdown = $"```{tag}\nlet model = Resolution.push model shot\n```\n"

                Expect.isTrue (cites markdown "push") $"a ```{tag} block documents the API it shows"
                Expect.equal (codeReferencesIn markdown).UntaggedFences 0 $"a ```{tag} block is tagged, not bare"

            // ...and the exclusion still holds where it must, or the homonym is back.
            for tag in [ "bash"; "json"; "text"; "console" ] do
                let markdown = $"```{tag}\ngit push origin main\n```\n"

                Expect.isFalse (cites markdown "push") $"a ```{tag} block documents nothing"
        }

        test "a non-F# block's backticks cannot leak back in through the prose buffer" {
            // The obvious way to write hole 2's fix — send a non-F# block to the PROSE buffer rather than the
            // code one — hands the homonym straight back, because prose is mined for inline spans and a shell
            // block is full of backticks. So a dropped block is dropped from BOTH. This is that decision, pinned.
            let markdown =
                """
```bash
echo `push` > /dev/null
```
"""

            Expect.isFalse (cites markdown "push") "a backtick inside a dropped block is not an inline citation"
        }

        test "an inline code span still documents, whatever the sentence around it is about" {
            let markdown = "Call `respondsProofOf` to tell \"renders\" from \"responds\"."

            Expect.isTrue (cites markdown "respondsProofOf") "backticks are a citation — that is what they mean (#654)"
        }

        test "prose still documents nothing (#654 holds)" {
            let markdown = "The block above can bind all four the same way."

            Expect.isFalse (cites markdown "block") "an English word is not a citation of `RichText.block`"
        }

        test "an unclosed fence is still caught, at any indent" {
            // The unbounded indent must not cost the #654 guard its teeth: an unclosed fence spills the rest of
            // the file into a block, and that is the one failure that makes the documented count go UP.
            let closed =
                """
```fsharp
let x = 1
```
"""

            let unclosed =
                """
- like so:

      ```fsharp
      let x = 1
"""

            Expect.isFalse (codeReferencesIn closed).UnclosedFence "a closed fence is closed"
            Expect.isTrue (codeReferencesIn unclosed).UnclosedFence "a list-indented fence that never closes is not"
        }

        test "an untagged fence is reported rather than guessed at" {
            // The price of the F#-only corpus, and why `skillsWithUntaggedFence` exists. An untagged block is
            // ambiguous — credit it and the homonym is back; drop it and a genuine F# example silently stops
            // documenting its surfaces, sending the author to the ledger to excuse a surface they DID document.
            // So: drop it (safe direction) AND red the gate by name (so the drop can never be silent).
            let markdown =
                """
```
let model = Resolution.push model shot
```
"""

            let extracted = codeReferencesIn markdown

            Expect.equal extracted.UntaggedFences 1 "the bare fence is counted"
            Expect.isFalse (citesName extracted.Code "push") "an untagged block is not credited — it might not be F#"
        }
    ]
