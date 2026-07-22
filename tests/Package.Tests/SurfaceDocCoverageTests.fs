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
// #695 convergence: the one `.fsi` `val` reader lives in TestSupport (prime-aware, internal-excluding).
// This copy was already prime-aware; the only nominal difference was `\s+` (nested-only) vs the canonical
// `\s*`, and a column-0 `val` is illegal F# so the two match identically on any real signature.
let private publicValRegex = SurfaceSignature.publicValRegex

/// A `module M =` inside a signature file. THE INDENT IS LOAD-BEARING: it is the only thing that separates a
/// module nested INSIDE another from a sibling that merely follows it, and `ControlsElmish.fsi` has both —
/// `AdapterCmd` and `ControlsElmish` are two top-level modules of one file (#663 already found that pair),
/// while `Live` and `Perf` are nested inside `ControlsElmish`. A reader that ignored the indent would flatten
/// the two shapes together and could not tell `…Elmish.AdapterCmd` from `…Elmish.ControlsElmish.Perf`.
///
/// EVERY MODIFIER MATTERS, and here a missed one is worse than a missed binding form (#692). A `module
/// internal Foo =` that this does not match is not merely skipped: the line falls through, the scope stack is
/// left alone, and every `val` inside it is attributed to the module ENCLOSING it. A skill that then correctly
/// writes `Foo.bar` finds `Foo` is a suffix of nothing, loses its credit, and S-DOC reports a surface the
/// skill DOES document as undeclared — the gate inventing violations against correct docs (#598). And the
/// `rootless` floor below cannot see it, because the wrong path is still namespace-qualified. The mirror is
/// known to leak `internal` declarations already (S-INT, #585), so this is not a hypothetical shape.
let private moduleAccess = @"(?:rec\s+|public\s+|private\s+|internal\s+)*"

let private moduleRegex =
    Regex(
        $@"^(?<indent>\s*)module\s+{moduleAccess}(?<name>[A-Z][\w']*(?:\.[A-Z][\w']*)*)\s*=",
        RegexOptions.Compiled
    )

/// The `namespace` a signature file opens with — everything below it is qualified by it. The file-level
/// `module A.B.C` form (no `=`) plays exactly the same role, so it is taken here too.
let private rootRegex =
    Regex(
        $@"^(?:namespace|module)\s+{moduleAccess}(?<name>[A-Z][\w']*(?:\.[A-Z][\w']*)*)\s*$",
        RegexOptions.Compiled
    )

/// Every public val one signature file declares: `name, the .fsi, the FULLY-QUALIFIED module that declares it`.
///
/// The module is what #713 turns on, and the old map could not answer it: it keyed `name -> .fsi FILES`, so
/// "which module declares `subscriptions`?" had no answer and `qualifiedCite` had to accept ANY uppercase
/// qualifier — including a product module's. Walking the `module`/`val` indents is what recovers it.
let private shippedValsIn (rel: string) (lines: string seq) =
    (("", [], []), lines)
    ||> Seq.fold (fun (root, scopes, acc) line ->
        // Scopes are held outermost-first and their indents strictly increase, so "pop every module this line
        // has dedented out of" is exactly "keep the ones indented less than this line".
        let enclosing (indent: int) = scopes |> List.filter (fun (i, _) -> i < indent)

        let rootMatch = rootRegex.Match line
        let moduleMatch = moduleRegex.Match line
        let valMatch = publicValRegex.Match line

        if rootMatch.Success then
            rootMatch.Groups.["name"].Value, [], acc
        elif moduleMatch.Success then
            let indent = moduleMatch.Groups.["indent"].Value.Length
            root, enclosing indent @ [ indent, moduleMatch.Groups.["name"].Value ], acc
        elif valMatch.Success then
            let indent = valMatch.Groups.["indent"].Value.Length
            let scopes = enclosing indent

            let declaringModule =
                (if root = "" then [] else [ root ]) @ (scopes |> List.map snd)
                |> String.concat "."

            root, scopes, (valMatch.Groups.["name"].Value, rel, declaringModule) :: acc
        else
            root, scopes, acc)
    |> fun (_, _, acc) -> List.rev acc

let private shippedVals =
    Directory.EnumerateFiles(apiSurfaceRoot, "*.fsi", SearchOption.AllDirectories)
    |> Seq.collect (fun path ->
        let rel = Path.GetRelativePath(apiSurfaceRoot, path).Replace('\\', '/')
        shippedValsIn rel (File.ReadAllLines path))
    |> Seq.toList

let private byName pick =
    shippedVals
    |> List.groupBy (fun (name, _, _) -> name)
    |> List.map (fun (name, entries) -> name, entries |> List.map pick |> Set.ofList)
    |> Map.ofList

/// name -> the shipped `.fsi` files that declare it.
let private shippedSurface = byName (fun (_, file, _) -> file)

/// name -> the fully-qualified module(s) that DECLARE it: `subscriptions` is
/// `FS.GG.UI.Controls.Elmish.ControlsElmish`, and `runScriptToModel` is that module's nested `….Perf`.
let private declaringModules = byName (fun (_, _, declaringModule) -> declaringModule)

/// Everything the product is TOLD, as one body of text. Concatenated on purpose: S-DOC asks whether a
/// surface is documented AT ALL, and which skill says it is R-REACH's question, not this one.
let private productSkillText =
    Directory.EnumerateFiles(productSkillsRoot, "SKILL.md", SearchOption.AllDirectories)
    |> Seq.map File.ReadAllText
    |> String.concat "\n"

// WHERE THE FENCE READER WENT (#669). The fence regex, the F#-language set and the pairing rule used to live
// here, and were hand-mirrored — differently — in `TemplateConsumesPinnedApiTests`' two extractors. They now
// live once, in `FS.GG.TestSupport.MarkdownFences`, which is the ONLY thing in the repo that decides what a
// fence is. The #664 lessons this file paid for — the unbounded indent, the F#-only corpus, every spelling of
// F# — are written up there, at the definitions that hold them.
//
// What is left below is what is genuinely S-DOC's OWN: the code/prose SPLIT (an F# block is the corpus; a
// non-F# block is dropped from BOTH buffers; prose is mined for inline spans). That is a choice over the
// scanner's classified lines, not a second reading of the document.

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
    let scanned = MarkdownFences.scan markdown
    let code = Text.StringBuilder()
    let prose = Text.StringBuilder()

    for line in scanned.Lines do
        match line.Kind with
        // F# is the corpus.
        | MarkdownFences.Fenced(_, true) -> code.AppendLine line.Text |> ignore
        // Anything else fenced is dropped on the floor — NOT routed to prose (see above).
        | MarkdownFences.Fenced _ -> ()
        // The fence line itself is neither.
        | MarkdownFences.Delimiter -> ()
        | MarkdownFences.Prose -> prose.AppendLine line.Text |> ignore

    for m in inlineCodeRegex.Matches(prose.ToString()) do
        code.AppendLine m.Groups.["code"].Value |> ignore

    { Code = code.ToString()
      UnclosedFence = scanned.UnclosedFence
      UntaggedFences = scanned.UntaggedFences }

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

/// An UNQUALIFIED citation: the bare name, reached through no module's dot.
///
/// The boundary is `[\w']`, not `\b`, because a prime is part of the NAME and `\b` cannot see that. `\b` sits
/// between a word char and a non-word char, so `\bchecked'\b` demands a word char after the quote and can
/// never match `CheckBox.checked'` at all — while a bare `\bcount\b` happily matches the `count'` in some
/// other symbol. Treating the quote as a name character settles both: `withKey` is still not credited to a
/// skill that shows `withKeyboard`, and `count` is not credited to one that shows `count'`.
///
/// THE DOT IS IN THE LOOKBEHIND, and that is #713's other half. `.` is not a word character, so the plain
/// `(?<![\w'])` happily matches the `subscriptions` inside `AppRoot.Model.subscriptions` — i.e. the bare-name
/// matcher cannot see that the occurrence is qualified by somebody else's module, and would credit the shipped
/// surface to it. Excluding the dot here means a QUALIFIED occurrence is judged by `qualifiedCite` (which
/// checks WHICH module) and by nothing else.
let private bareCite (corpus: string) (name: string) =
    Regex.IsMatch(corpus, $@"(?<![\w'.]){Regex.Escape name}(?![\w'])")

/// Every qualifier a citation of `name` is written with: `ControlsElmish.subscriptions` yields
/// `ControlsElmish`, `AppRoot.Model.subscriptions` yields `AppRoot.Model`. The leading `(?<![\w'.])` makes the
/// match start at the FIRST segment, so a fully-spelled `FS.GG.UI.Scene.Scene.describe` is read whole rather
/// than clipped to its last segment.
let private citedQualifiers (corpus: string) (name: string) =
    Regex.Matches(corpus, $@"(?<![\w'.])(?<qualifier>(?:[A-Z][\w']*\.)+){Regex.Escape name}(?![\w'])")
    |> Seq.map (fun m -> m.Groups.["qualifier"].Value.TrimEnd '.')

/// Is `qualifier` a legal way to WRITE `modulePath`? It is exactly when it is a contiguous SUFFIX of it.
///
/// This is not a heuristic — it is what `open` does. `open FS.GG.UI.Controls.Elmish` makes the declaring
/// module `FS.GG.UI.Controls.Elmish.ControlsElmish` writable as `ControlsElmish`; opening THAT makes its
/// nested `…ControlsElmish.Perf` writable as `Perf`. Every legal spelling drops some PREFIX of the declared
/// path and keeps the rest, so an honest citation's qualifier is always a suffix — and the check must accept
/// every one of them, or it un-credits correct docs and "the rule INVENTS violations" (#598).
///
/// `AppRoot.Model` is a suffix of nothing this package ships. That is the whole of #713.
let private isSpellingOf (modulePath: string) (qualifier: string) =
    let segments (s: string) = s.Split '.' |> Array.filter (fun part -> part <> "")
    let declared = segments modulePath
    let written = segments qualifier

    written.Length > 0
    && written.Length <= declared.Length
    && Array.forall2 (=) written declared.[declared.Length - written.Length ..]

/// A citation that names the module the surface actually comes FROM. This is what reaches PAST a local binding
/// of the same name, which is exactly what it is for.
///
/// IT NOW CHECKS **WHICH** MODULE (#713). It used to require only that the qualifier start uppercase, which a
/// PRODUCT module satisfies as readily as a shipped one — so `fs-gg-elmish`, which binds the product's own
/// `let subscriptions _ = Sub.none` and writes `AppRoot.Model.subscriptions` in its `program` example, credited
/// the *shipped* `ControlsElmish.subscriptions` (the keyboard+controls MERGE helper). That surface was taught
/// by no skill and declared in no ledger line, and it passed S-DOC by being documented by a homonym OF THE
/// PRODUCT'S OWN FUNCTION — the `visible` failure of #692, one hop further out, surviving through the very
/// hatch #692 opened.
let private qualifiedCite (corpus: string) (name: string) =
    let declaring = declaringModules |> Map.tryFind name |> Option.defaultValue Set.empty

    citedQualifiers corpus name
    |> Seq.exists (fun qualifier -> declaring |> Set.exists (fun declared -> isSpellingOf declared qualifier))

/// A skill documents a surface when it CITES it as code — bare, or through a module that really declares it.
/// Code-only, so a surface is not credited to a skill that merely uses its name as an English word (#654).
let private citesName (corpus: string) (name: string) =
    bareCite corpus name || qualifiedCite corpus name

/// The F# forms that BIND a name rather than cite one (#692). `let`/`and`/`use` with any modifier order,
/// `member` with an optional self-identifier, and the `fun`/`for` binders — the shapes a skill's own example
/// helper and its locals actually take.
///
/// EVERY FORM MATTERS, because a form left out is a form the homonym walks back in through. `fun`/`for` are
/// latent today (no surface is credited by a lambda or loop variable), and so was `let` until it deleted a
/// blessed ledger line — so they are here rather than waiting for their turn. Adding them un-credits nothing.
///
/// KNOWN GAP: a function PARAMETER (`let onScreen (bounds: Rect list) = … bounds …`) is a binding this cannot
/// see, and matching one properly means parsing F# rather than pattern-matching it. Nothing is credited that
/// way today, and it is the last member of this family still standing (#713 closed the qualified-module one).
let private bindsName (corpus: string) (name: string) =
    let n = Regex.Escape name

    Regex.IsMatch(
        corpus,
        $@"(?<![\w'])(?:let|and|use)\s+(?:rec\s+|mutable\s+|inline\s+|private\s+|internal\s+)*{n}(?![\w'])"
        + $@"|(?<![\w'])member\s+(?:val\s+)?(?:[\w']+\.)?{n}(?![\w'])"
        + $@"|(?<![\w'])fun\s+{n}\s*->"
        + $@"|(?<![\w'])for\s+{n}\s+in(?![\w'])"
    )

/// Does THIS skill cite `name` as the shipped API? (#692 — the SAME-LANGUAGE homonym.)
///
/// #654 found that an English word credited a surface, and moved the corpus from prose to code. #664 found
/// that a word in a `bash` fence still did, and restricted the corpus to F#-tagged fences. Both narrowed
/// WHERE a match may come from. Neither changed what a match MEANS — so the homonym walked in one level
/// further, in F# itself: a skill that **defines** `let describe` is not citing `Scene.describe`, and the
/// matcher could not tell a binding site from a usage. The anti-rot rule then declared the (correct,
/// BLESSED `vocabulary`) ledger line stale, and "the ledger only shrinks" made deleting it the only way to a
/// green gate — a one-way door onto a fact that was still true (#692, and PR #691 walked through it).
///
/// SHADOWING IS THE RULE, because shadowing is the FACT. Once a skill writes `let visible = …`, every
/// unqualified `visible` in that skill IS the local one — that is not a heuristic, it is what F# scoping
/// says. So within a skill that binds the name, only a QUALIFIED citation (`Attr.visible`) reaches the API;
/// an unqualified occurrence is the binding, or a use of it. A skill that does NOT bind the name is
/// unchanged: a bare `` `respondsProofOf` `` in backticks still cites, which is what backticks mean.
///
/// PER SKILL, and that matters. The corpus is concatenated for the question "is this documented AT ALL", but
/// shadowing is not a property of the concatenation — one skill's local helper must not silence ANOTHER
/// skill's honest citation. `describe` is the live case: `fs-gg-persistence` binds `let describe` for an
/// unrelated example, and `fs-gg-scene` writes `Scene.describe hud`. The first credits nothing; the second
/// credits, and `describe` is documented — correctly, and for the first time actually because a skill
/// documents it.
///
/// The weaker rule — "count occurrences, subtract definition sites" — is NOT enough, and the corpus proves
/// it: `fs-gg-game-core` binds `let visible : Rect` and then uses `visible` on the next line inside its
/// culling example, so occurrences (2) exceed definitions (1) and `Attr.visible` stays credited by a local
/// rectangle in a skill about game simulation. Counting cannot see scope. Shadowing can.
///
/// #713 — AND THE QUALIFIER MUST NAME THE RIGHT MODULE, on BOTH branches. The escape hatch above only asked
/// that a qualifier exist, so `AppRoot.Model.subscriptions` — a PRODUCT module — reached the shipped
/// `ControlsElmish.subscriptions`. `qualifiedCite` now holds the qualifier against the module that DECLARES
/// the surface, which closes the bound branch.
///
/// The unbound branch needed it too, and that is the subtler half. `citesName` matched the bare name with a
/// `[\w']` lookbehind, and `.` is not a word character — so `AppRoot.Model.subscriptions` ALSO satisfied the
/// bare matcher. Fixing only the bound branch would therefore have left a fix that holds by COINCIDENCE:
/// delete the unrelated `let subscriptions _ = Sub.none` from the skill and `subscriptions` stops being bound,
/// the unbound branch takes over, and the same false credit walks straight back in. So `bareCite` excludes the
/// dot, and every qualified occurrence — on either branch — is judged by the module it names. It cost the
/// corpus nothing: no surface today is credited only by a foreign qualifier.
let private skillCites (code: string) (name: string) =
    if bindsName code name then
        qualifiedCite code name
    else
        citesName code name

let private isDocumented (name: string) =
    skillCode |> List.exists (fun (_, c) -> skillCites c.Code name)

/// The declared exemptions: `<category>  <name>` lines, `#` comments and blanks ignored.
let private ledger =
    File.ReadAllLines ledgerPath
    |> Array.map (fun l -> l.Trim())
    |> Array.filter (fun l -> l <> "" && not (l.StartsWith "#"))
    |> Array.choose (fun l ->
        let parts = l.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
        if parts.Length >= 2 then Some(parts.[1], parts.[0]) else None)
    |> Map.ofArray

/// The categories a ledger line may carry. Adding one is a DELIBERATE act — a line whose category is not here
/// reds `every ledger category is a known disposition`, so a typo (`traked`, `owner-docmented`) is caught
/// rather than silently minting a one-off category that no rule and no reader understands. That is the
/// silent-append failure this whole file refuses, applied to the category column itself.
///
/// PENDING vs TERMINAL is the distinction that matters, and it is enforced by the RULES rather than by this
/// set — every category satisfies coverage (rule 1), and they differ only in what makes a line stale:
///
///   PENDING   `tracked`          A real gap a product author wants and cannot find (#507). It RETIRES the day
///                                a product skill HERE documents it — rule 2 is watching for exactly that.
///   TERMINAL  `vocabulary`       Composed straight from the `.fsi`; a skill would be a worse copy of it.
///             `owner-sourced`    Documented by an owner-sourced skill (`fs-gg-game-core` / `fs-gg-audio` /
///                                `fs-gg-persistence`) whose local copy ADR-0063 (#965) retired from this
///                                provider — the body still reaches the product, just not this corpus.
///             `owner-documented` #998's addition. The 37 Ai/Ballistics/Dice/Effects vals VENDORED into this
///                                mirror by #984 and documented in FS.GG.Game's `fs-gg-game-core` BODY
///                                (FS.GG.Game#466/#472) — invisible to S-DOC here because ADR-0063 retired the
///                                local copy and S-DOC reads only `template/product-skills/**`. No skill in
///                                this repo's corpus can ever cite them, so the line is PERMANENTLY satisfied
///                                by cross-repo owner documentation: never `undeclared` (it is listed), never
///                                `tracked`-stale (rule 2 cannot fire — nothing local cites it). It retires
///                                only if #984 un-vendors the surface (rule 3) or a Rendering-owned skill ever
///                                documents one locally (rule 2). Adjudicated option 3 of #998, 2026-07-22.
let private knownCategories =
    set [ "tracked"; "vocabulary"; "owner-sourced"; "owner-documented" ]

/// A public val is COVERED when a product skill documents it, or the ledger declares it under ANY category —
/// the exemption side of S-DOC rule 1. Factored out so the guard tests can exercise the EXACT predicate the
/// rule uses, rather than a paraphrase that could drift from it.
let private isCovered (name: string) = isDocumented name || ledger.ContainsKey name

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

            // ...and the MODULE half of that parse, which #713 turns the whole qualified-citation rule on. If
            // the `module`/`namespace` walk ever stops seeing a declaration, the vals under it get an empty or
            // truncated path, no qualifier can be a suffix of it, and every QUALIFIED citation silently stops
            // crediting — reporting correct, documented surfaces as undeclared. That is the gate inventing
            // violations (#598), and it is the direction this fix can fail in, so it is asserted by name.
            let rootless =
                shippedVals
                |> List.filter (fun (_, _, declaringModule) -> not (declaringModule.Contains "."))
                |> List.map (fun (name, file, declaringModule) -> $"{name} ({file}: '{declaringModule}')")
                |> List.distinct

            Expect.isEmpty
                rootless
                $"these public vals resolved to no namespace-qualified declaring module, so the `module` / \
                  `namespace` walk has stopped seeing the shape of the signature file. Every qualified citation \
                  of them would silently STOP crediting — which reports correct docs as undeclared, rather than \
                  letting an undocumented surface through. Fix the walk, not the docs. Rootless: {commaSep rootless}"

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
                |> List.filter (fun (name, _) -> not (isCovered name))
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

        // Every category is one the ledger's format legend and this file both KNOW. A category is a promise
        // about a line's future — pending `tracked` vs terminal `vocabulary` / `owner-sourced` /
        // `owner-documented` — so an invented or mistyped one is a line no rule and no reader can honour. That
        // is the silent-append failure this file refuses, in the category column: adding a disposition must be
        // a decision (a line in `knownCategories`, with the paragraph that says what it promises), not a typo.
        test "every ledger category is a known disposition" {
            let unknown =
                ledger
                |> Map.toList
                |> List.filter (fun (_, category) -> not (knownCategories.Contains category))
                |> List.map (fun (name, category) -> $"{name} ({category})")

            Expect.isEmpty
                unknown
                $"these {ledgerRel} lines carry a category not in knownCategories ({commaSep knownCategories}) — \
                  a typo, or a new disposition added without a decision. Fix the spelling, or add the category \
                  to knownCategories with the paragraph that says what it promises. Unknown: {commaSep unknown}"
        }

        // #998 — the `owner-documented` TERMINAL disposition. The 37 Ai/Ballistics/Dice/Effects vals are
        // VENDORED into this mirror (#984) but documented in FS.GG.Game's `fs-gg-game-core` BODY
        // (FS.GG.Game#466/#472), a corpus S-DOC cannot read here because ADR-0063 (#965) retired the local
        // copy. Adjudicated (option 3 of #998) as a PERMANENT category, distinct from the pending `tracked`
        // gap they sat in through #984/#993. These two guards pin what the category PROMISES: that a val in it
        // is covered and never stale, and — its mirror — that the exemption is not a blanket.
        test "the `owner-documented` category satisfies coverage and is terminal (#998)" {
            let ownerDocumented =
                ledger
                |> Map.toList
                |> List.filter (fun (_, category) -> category = "owner-documented")
                |> List.map fst

            // Non-vacuous: the category must actually be populated, or this guard passes by checking nothing —
            // the FS-GG/.github#266 failure. #998 moved 37 rows into it; if they were retired or renamed,
            // retire this guard with them rather than letting it go quietly green.
            Expect.isNonEmpty
                ownerDocumented
                $"no {ledgerRel} line carries the `owner-documented` category — #998 moved the 37 \
                  Ai/Ballistics/Dice/Effects vals into it. If the category is genuinely empty now, delete this \
                  guard; do not let it pass vacuously."

            for name in ownerDocumented do
                // Rule 1: COVERED. A product author is not left unable to find them — the ledger records the
                // cross-repo documentation decision, so S-DOC does not report them undeclared.
                Expect.isTrue
                    (isCovered name)
                    $"`{name}` is `owner-documented` but S-DOC does not count it as covered — the coverage \
                      predicate and the ledger disagree, which would red rule 1 against a declared line."

                // TERMINAL, not pending. The whole point: the citations live in FS.GG.Game's `fs-gg-game-core`,
                // which this corpus cannot read, so NO local skill can cite these — rule 2 can never fire and
                // the line is never `tracked`-stale. If one ever DID become locally documented, that is a real
                // event (delete its line); this guard names it rather than letting rule 2 red cryptically.
                Expect.isFalse
                    (isDocumented name)
                    $"`{name}` is `owner-documented` — permanently satisfied by cross-repo owner documentation — \
                      yet a LOCAL product skill now cites it. The surface is documented HERE now: delete its \
                      ledger line (rule 2 would otherwise red). `owner-documented` is for surfaces no local \
                      skill can reach."

                // ...and it still SHIPS, or it is a phantom (rule 3) and the category has outlived its subject.
                Expect.isTrue
                    (shippedSurface.ContainsKey name)
                    $"`{name}` is `owner-documented` but the api-surface no longer ships it. #984's vendoring is \
                      the reason the category exists; if the surface was un-vendored, retire the line (rule 3)."
        }

        // The MIRROR of the coverage guard, and the reason the exemption cannot be a blanket. A surface neither
        // documented nor listed under ANY category — the #507 shape — must STILL red rule 1. #998 widened the
        // set of terminal dispositions; it must not have widened what counts as covered to everything.
        test "a val in NO category and documented by no skill still reds" {
            let probe = "zzUnlistedSurfaceProbe998"

            Expect.isFalse (ledger.ContainsKey probe) "the probe name is deliberately absent from the ledger"
            Expect.isFalse (isDocumented probe) "...and cited by no product skill"

            Expect.isFalse
                (isCovered probe)
                "a val in no ledger category and documented nowhere is NOT covered — S-DOC rule 1 must still \
                 red for it, or the exemption has become a blanket that excuses every undocumented surface, \
                 which is the #507 gap this gate exists to hold."
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

    // What a SKILL credits, shadowing included (#692) — the question `isDocumented` actually asks, per skill.
    let skillDocuments markdown name = skillCites (codeReferencesIn markdown).Code name

    testList "S-DOC code extractor (#664)" [

        // ---- #692: the SAME-LANGUAGE homonym. -------------------------------------------------------
        //
        // These are latent in the corpus too, in the direction that matters: with the bug in place every
        // assertion above still passes, because the false credit makes a surface look DOCUMENTED — and a
        // surface that looks documented is one the gate stops asking about. That is the failure mode #654
        // records ("it makes the number go UP, not down"), one language in. So it is pinned here.

        test "a skill that DEFINES `let describe` does not thereby document `Scene.describe`" {
            // The bug, exactly as it shipped. `fs-gg-persistence`'s canonical body (a FROZEN MIRROR of
            // FS.GG.Game's — we cannot edit it) carries an unrelated example helper called `describe`. That
            // binding credited `Scene.describe`, a BLESSED `vocabulary` line, and the anti-rot rule then
            // demanded the line be deleted — a one-way door onto a fact that was still true (#692, PR #691).
            let markdown =
                """
```fsharp
let describe (outcome: PersistenceOutcome) : string =
    match outcome with
    | PersistenceOutcome.Saved slot -> $"saved to {slot}"
```
"""

            Expect.isFalse
                (skillDocuments markdown "describe")
                "a skill DEFINING `describe` is not citing `Scene.describe` — a binding site is not a citation"
        }

        test "...and a USE of that local binding does not document it either" {
            // Why counting is not enough, and this is the case that settles the design. `fs-gg-game-core`
            // binds `let visible : Rect` and then USES it on the next line — so "occurrences (2) exceed
            // definition sites (1)" credits it, and `Attr.visible` (a real shipped control attribute, beside
            // `enabled`/`readOnly`) is documented by a rectangle in a skill about game simulation. Once the
            // name is bound, every unqualified use of it is the LOCAL one: that is F# shadowing, not a guess.
            let markdown =
                """
```fsharp
let visible : Rect = { X = 0.0; Y = 0.0; Width = 1280.0; Height = 720.0 }

let onScreen (bounds: Rect list) : Rect list =
    bounds |> List.filter (fun b -> Geometry.intersects b visible)
```
"""

            Expect.isFalse
                (skillDocuments markdown "visible")
                "a local binding and its own uses cannot document `Attr.visible` — counting cannot see scope"
        }

        test "a QUALIFIED citation reaches past a local binding of the same name" {
            // The other half, or the rule would be a blunt instrument: a skill may legitimately bind a helper
            // AND cite the API it shadows. Naming the module is exactly how you say which one you mean.
            let markdown =
                """
```fsharp
let describe (outcome: Outcome) : string = "..."

let kinds = Scene.describe hud
```
"""

            Expect.isTrue
                (skillDocuments markdown "describe")
                "`Scene.describe` names its module, so it cites the API even where `describe` is also bound"
        }

        test "a skill that does NOT bind the name still cites it bare" {
            // The rule must not cost the corpus its ordinary citations. Backticks are a citation (#654), and
            // most of the corpus cites unqualified — so shadowing may only bite where a binding really exists.
            let markdown = "Call `respondsProofOf` to tell \"renders\" from \"responds\"."

            Expect.isTrue
                (skillDocuments markdown "respondsProofOf")
                "no binding, so the bare name still cites — shadowing narrows nothing it should not"
        }

        test "every binding form is a binding, not a citation" {
            // The modifiers are not decoration: miss one and the homonym walks straight back through it.
            for binding in
                [ "let describe x = x"
                  "let rec describe x = x"
                  "let private describe x = x"
                  "let inline describe x = x"
                  "let mutable describe = 1"
                  "and describe x = x"
                  "use describe = foo ()"
                  "member this.describe x = x"
                  "member _.describe x = x"
                  "member val describe = 1"
                  "let f = items |> List.map (fun describe -> describe)"
                  "for describe in scenes do ignore describe" ] do
                let markdown = $"```fsharp\n{binding}\n```\n"

                Expect.isFalse
                    (skillDocuments markdown "describe")
                    $"`{binding}` BINDS `describe`; it does not cite `Scene.describe`"

            // ...and the shape that is NOT a binding must still cite, or the regex is over-reaching and would
            // silently un-credit honest docs — the "rule INVENTS violations" failure (#598), inverted.
            Expect.isTrue
                (skillDocuments "```fsharp\nlet kinds = describe hud\n```\n" "describe")
                "`describe` on the RIGHT of a binding is a USE of it — the binder here is `kinds`"
        }
        // ---- end #692 -------------------------------------------------------------------------------


        // ---- #713: the qualified hatch did not check WHICH module. --------------------------------------
        //
        // #692's escape hatch asked only that a qualifier EXIST and start uppercase. A product module
        // satisfies that as readily as a shipped one, so the homonym walked out through the hatch itself —
        // and, as ever, in the direction that makes a surface look DOCUMENTED, which is the direction the
        // gate stops asking about (#654: "it makes the number go UP").
        //
        // These are the bug and its cascade. The cascade cases are not decoration: a naive "the qualifier
        // must EQUAL the declaring module" would red the gate against docs that are correct, which is the
        // "rule INVENTS violations" failure (#598) and the reason this could not be a line in #692.

        test "a PRODUCT module's homonym does not document the shipped surface it shadows" {
            // The bug, exactly as it shipped. `fs-gg-elmish` binds the product's own `subscriptions` and cites
            // the product's own module — and thereby credited the shipped `ControlsElmish.subscriptions`, the
            // keyboard+controls MERGE helper, which no skill taught and no ledger line declared.
            let markdown =
                """
```fsharp
let subscriptions _ : AdapterSubscription<Msg> list = Sub.none

let adapterProgram =
    ControlsElmish.program AppRoot.Model.init AppRoot.Model.update view AppRoot.Model.subscriptions
```
"""

            Expect.isFalse
                (skillDocuments markdown "subscriptions")
                "`AppRoot.Model` is a PRODUCT module — it declares nothing this package ships, so it cannot \
                 document `ControlsElmish.subscriptions`. An uppercase qualifier is not a citation of the API; \
                 naming the module the API COMES FROM is."

            // ...and the same block's honest citation is untouched, or the rule is a blunt instrument.
            Expect.isTrue
                (skillDocuments markdown "program")
                "`ControlsElmish.program` names the module that really declares `program` — it still cites"
        }

        test "a NESTED module qualifies the surface it declares" {
            // Cascade 1, and the one that would have broken first. `runScriptToModel` is declared in
            // `FS.GG.UI.Controls.Elmish.ControlsElmish.Perf` — a module nested INSIDE `ControlsElmish` — and
            // `fs-gg-elmish` cites it as `Perf.runScriptToModel`, which is what `open`ing the parent gives you.
            // A rule demanding the qualifier EQUAL the declaring module would un-credit this correct doc.
            let markdown = "Call `Perf.runScriptToModel` to fold a script to a final model."

            Expect.isTrue
                (skillDocuments markdown "runScriptToModel")
                "`Perf` is the LAST segment of the declaring module's path, which is exactly how F# lets you \
                 write it once its parent is opened — a suffix of the path is a spelling of the module"

            Expect.isTrue
                (skillDocuments "```fsharp\nlet m = ControlsElmish.Perf.runScriptToModel script\n```\n" "runScriptToModel")
                "...and so is the longer spelling, which names the parent too"
        }

        test "a SIBLING module qualifies the surface it declares" {
            // Cascade 2. `AdapterCmd` and `ControlsElmish` are two top-level modules of ONE signature file
            // (#663 found that pair), so a reader keyed on the FILE cannot tell them apart — which is precisely
            // why the old map, keyed `name -> .fsi files`, could not answer "which module?" at all.
            Expect.isTrue
                (skillDocuments "Route `AdapterCmd.diagnostics` at the host boundary." "diagnostics")
                "`AdapterCmd` declares `diagnostics`, so it cites it — a sibling module in the same file is \
                 still the declaring module"
        }

        test "the fully-spelled path is a spelling too, and a foreign PREFIX is not" {
            // A qualifier is legal exactly when it is a contiguous SUFFIX of the declaring module's path,
            // because every legal spelling drops a PREFIX (whatever you opened) and keeps the rest.
            Expect.isTrue
                (skillDocuments "```fsharp\nlet p = FS.GG.UI.Controls.Elmish.ControlsElmish.program a b c d\n```\n" "program")
                "the whole path names the declaring module — nothing is dropped, so nothing is wrong"

            // The mirror. A path that shares the package's PREFIX but not its tail is a different module, and
            // suffix-matching is what tells them apart — `Elmish.Program` is not `Elmish.ControlsElmish`.
            Expect.isFalse
                (skillDocuments "```fsharp\nlet p = FS.GG.UI.Controls.Elmish.Program.program a b c d\n```\n" "program")
                "a real-looking prefix does not make `Program` the declaring module — it declares nothing"
        }

        test "an unbound name is not credited by a foreign qualifier either" {
            // The half that keeps the fix from holding by COINCIDENCE. Shadowing only routes a name through
            // `qualifiedCite` when the skill BINDS it — and `AppRoot.Model.subscriptions` also satisfies the
            // bare-name matcher, because `.` is not a word character. So a skill that cites the product module
            // WITHOUT binding the name (delete the `let subscriptions` line above and this is the corpus) would
            // walk the same false credit straight back in through the other branch.
            let markdown =
                """
```fsharp
let adapterProgram =
    ControlsElmish.program AppRoot.Model.init AppRoot.Model.update view AppRoot.Model.subscriptions
```
"""

            Expect.isFalse
                (skillDocuments markdown "subscriptions")
                "nothing here BINDS `subscriptions`, so the unbound branch judges it — and it must reach the \
                 same verdict, or #713's fix would depend on an unrelated `let` staying where it is"

            // ...and the bare citation the unbound branch exists for still works. Backticks are a citation.
            Expect.isTrue
                (skillDocuments "Call `respondsProofOf` to tell renders from responds." "respondsProofOf")
                "an unqualified name in backticks still cites — excluding the dot narrows nothing it should not"
        }

        // The walk underneath all of the above. Everything #713 decides rests on `shippedValsIn` resolving the
        // right declaring module, and the three shapes below are the ones it has to tell apart. The DEDENT is
        // the one nothing else pins: `program` sits back in `ControlsElmish` after `Perf` closed, and a fold
        // that forgot to pop would file it under `…ControlsElmish.Perf` and un-credit every honest citation.
        test "the module walk sees siblings, nesting, and the dedent back out" {
            let lines =
                [ "namespace FS.GG.UI.Controls.Elmish"
                  ""
                  "module AdapterCmd ="
                  "    val diagnostics: int"
                  ""
                  "module ControlsElmish ="
                  "    val subscriptions: int"
                  ""
                  "    module Perf ="
                  "        val runScriptToModel: int"
                  ""
                  "    val program: int" ]

            Expect.equal
                (shippedValsIn "ControlsElmish.fsi" lines |> List.map (fun (name, _, m) -> name, m))
                [ "diagnostics", "FS.GG.UI.Controls.Elmish.AdapterCmd"
                  "subscriptions", "FS.GG.UI.Controls.Elmish.ControlsElmish"
                  "runScriptToModel", "FS.GG.UI.Controls.Elmish.ControlsElmish.Perf"
                  "program", "FS.GG.UI.Controls.Elmish.ControlsElmish" ]
                "two top-level modules in one file are SIBLINGS, `Perf` is NESTED, and `program` DEDENTS back \
                 into `ControlsElmish` — the three shapes the whole qualified-citation rule is decided on"
        }

        test "an access-modified module still declares its members" {
            // Latent: the api-surface carries no `module internal` today, so the CORPUS cannot witness this and
            // every assertion above passes with the bug in place. It is pinned here because the failure is
            // silent and lands in the dangerous direction — a module the walk fails to match does not vanish,
            // it hands its vals to the module ENCLOSING it, so a skill that correctly cites `Foo.bar` finds
            // `Foo` is a suffix of nothing, loses its credit, and S-DOC accuses a correct doc (#598). The
            // mirror is already known to leak `internal` declarations (S-INT, #585), so the shape is real.
            let lines =
                [ "namespace FS.GG.UI.Controls"
                  ""
                  "module internal Foo ="
                  "    val bar: int -> int" ]

            Expect.equal
                (shippedValsIn "Fake.fsi" lines)
                [ "bar", "Fake.fsi", "FS.GG.UI.Controls.Foo" ]
                "an access modifier does not change WHICH module a val belongs to — `bar` is `Foo`'s, and a \
                 walk that skipped the line would silently file it under the namespace instead"
        }
        // ---- end #713 -------------------------------------------------------------------------------


        test "a fence indented inside a list item still opens a block" {
            // Hole 1. `^ {0,3}` is CommonMark's indent allowance AT THE TOP LEVEL, and a fence nested in a list
            // item is legitimately indented past it. The block then never opened, every line of its code landed
            // in the PROSE buffer where it can document nothing, and a surface cited only here was reported
            // undeclared — the gate inventing a violation against a doc that is correct.
            //
            // The fixture says `ControlsElmish.program`, and it MUST, now that #713 checks which module a
            // qualifier names. It used to say `Program.program` — and there is no `Program` module in the
            // shipped api-surface, so that citation was never one. The fiction was invisible while any
            // uppercase qualifier would do; the rule that catches `AppRoot.Model` catches this too, and the
            // fix is to cite the module the surface actually comes from.
            let markdown =
                """
- To drive the program headlessly:

      ```fsharp
      let p = ControlsElmish.program init update view subs
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
            // The asymmetry in `MarkdownFences`' F#-language set (#669 moved it there, and the fence-level
            // conformance now lives beside it in `MarkdownFencesTests`; this case stays because it is S-DOC's
            // own composition — tag set THROUGH the code/prose split — not the scanner's).
            //
            // A tag the set does not know is dropped SILENTLY — it has a
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
