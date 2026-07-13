namespace FS.GG.TestSupport

open System
open System.Text.RegularExpressions

/// THE ONE READING OF A MARKDOWN CODE FENCE (#669).
///
/// Three gates parse fences over the same corpus (`template/product-skills/**`) and, before this module, each
/// hand-rolled its own reader — and the three DISAGREED about what an F# block is:
///
///   * `SurfaceDocCoverageTests.codeReferencesIn` (S-DOC)      — backticks OR tildes, paired by char and run
///     length, F# = a tag in {fsharp, fs, f#, fsx, fsi}.
///   * `TemplateConsumesPinnedApiTests.skillFenceSymbols`      — backticks ONLY, toggled, F# = `StartsWith
///     "fsharp"` (so `fsx`/`fsi`/`fs` were NOT F#, and `fsharpx` was).
///   * `TemplateConsumesPinnedApiTests.skillProseSymbols`      — backticks ONLY, toggled on ANY fence.
///
/// The disagreement is observable, and it is not tidiness. A ```fsx block was F# to S-DOC and not-F# to
/// `skillFenceSymbols`; a ```fsharpx block was the reverse. So one block could be *documentation* to one gate
/// and *not code* to another, and neither gate was wrong on its own terms — they were guarding the same claim
/// about the same file from different ideas of what the file said. A surface can then be simultaneously
/// "documented" (S-DOC green) and "not a pinned call site" (R-PIN blind).
///
/// And a fence bug had to be found and fixed THREE times. #664 was exactly that: the list-indent hole and the
/// homonym-in-code hole were fixed in S-DOC's reader, `skillFenceSymbols` already had both right — independently,
/// with no shared code and no way for either to learn from the other — and the next hole would land in whichever
/// of the three nobody happened to look at.
///
/// So: ONE scanner, which answers only the question markdown can answer — *where are the fences, and what
/// language did each one declare?* — and classifies every line. What to DO with a line is the caller's business,
/// and the three callers genuinely differ:
///
///   * S-DOC          keeps F# blocks, DROPS non-F# blocks from both buffers (#664), mines prose for inline spans.
///   * skillFenceSymbols  reads F# blocks only — the code a reader COPIES.
///   * skillProseSymbols  reads prose only — everything OUTSIDE every fence, whatever its language.
///
/// Each of those is a `List.filter` over `Scan.Lines`, not a re-implementation. That is the whole point.
module MarkdownFences =

    /// An opening or closing CommonMark fence: any indent, then 3+ backticks or 3+ tildes, then the info string
    /// (the language tag, on an opening fence; empty on a closing one).
    ///
    /// THE INDENT IS UNBOUNDED, and it must be (#664). CommonMark allows a fence up to 3 leading spaces *at the
    /// top level*, and a reader bounded by `^ {0,3}` misses a fence nested in a list item — the block never opens,
    /// every line of its code lands in the prose buffer where it can document nothing, and a surface cited only
    /// there is reported undeclared. That is the gate INVENTING a violation against a correct doc, which is the
    /// failure #598 and #664 both exist to close. Pairing carries the weight instead (see `closes`).
    let private fenceRegex =
        Regex(@"^\s*(?<fence>`{3,}|~{3,})(?<info>.*)$", RegexOptions.Compiled)

    /// The language tags that mean "this block is F#" — the only blocks that can CITE an F# API (#664).
    ///
    /// EVERY SPELLING OF F# BELONGS HERE, and the cost of a missing one is not symmetric. A tag this set does not
    /// know is dropped from the corpus — and dropped SILENTLY, because it HAS a language and so does not count as
    /// untagged. An `fsi` transcript citing `respondsProofOf` would document nothing, S-DOC would report the
    /// surface undeclared, and the author would be sent to the ledger to excuse a surface their skill genuinely
    /// documents. A tag wrongly INCLUDED costs a homonym; a tag wrongly OMITTED costs a false accusation against a
    /// correct doc. So this errs towards inclusion, and covers every extension F# ships under.
    ///
    /// This is the set S-DOC already used, and it is the one that survives: `skillFenceSymbols`' old
    /// `StartsWith "fsharp"` both missed `fs`/`fsx`/`fsi`/`f#` and matched anything merely PREFIXED `fsharp`.
    let private fsharpLanguages = set [ "fsharp"; "fs"; "f#"; "fsx"; "fsi" ]

    /// The language an opening fence declares, if it declares one: the first word of the info string.
    let language (info: string) =
        info.Split([| ' '; '\t'; ',' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.tryHead
        |> Option.map (fun lang -> lang.ToLowerInvariant())

    /// Can a block with this tag cite an F# API?
    let isFSharpLanguage (lang: string) = fsharpLanguages.Contains(lang.ToLowerInvariant())

    /// What a line IS, once the fences are paired.
    type LineKind =
        /// Outside every fence. The document's prose — where an API is MENTIONED rather than shown.
        | Prose
        /// The fence line itself, opening or closing. Never content, to any caller.
        | Delimiter
        /// Inside a fenced block. `isFSharp` is the one shared answer to "can this block cite an F# API?".
        | Fenced of language: string option * isFSharp: bool

    /// A line of the document, numbered as the ORIGINAL text numbers it — so a failure stays clickable, and so a
    /// caller may index a line-count-preserving transform of the same text (see `skillFenceSymbols`' F# erasure).
    type Line =
        { Number: int
          Text: string
          Kind: LineKind }

    /// The scanned document, plus the two ways the scan can have gone wrong. Both are DEFECTS the caller must
    /// treat as such, not curiosities:
    ///
    ///   * `UnclosedFence` — the document ends inside a block. Every line after the stray fence was read as that
    ///     block's content, so a document's PROSE silently becomes code (or its code silently becomes nothing).
    ///     It cannot be caught by measuring how much is documented, because it makes that number go UP.
    ///   * `UntaggedFences` — a block that does not say what it is. Crediting it reopens the #654 homonym;
    ///     dropping it silently un-documents whatever it cites. Neither is a call a gate may make for the author.
    type Scan =
        { Lines: Line list
          UnclosedFence: bool
          UntaggedFences: int }

    /// A fence closes only on its OWN character, repeated at least as many times as it was opened — wherever
    /// either sits. That is what lets the indent be unbounded: pairing, not position, decides the block.
    ///
    /// KNOWN DEVIATION FROM COMMONMARK, carried over deliberately from all three readers rather than fixed
    /// here: CommonMark says a CLOSING fence may not carry an info string, so a ```bash line inside a ```fsharp
    /// block is content, not a closer. This treats it as a closer. Every reader this module replaces did the
    /// same, so unifying them changes nothing; fixing it would change what all three gates read, which is a
    /// different item with a different blast radius. Nothing in the corpus nests a fence (all 67 F# blocks and
    /// the one yaml block are flat), so it is latent — which is exactly the state the holes in #664 were in
    /// right up until they were not. Named here so the next reader knows it is a decision, not an oversight.
    let private closes (opening: string) (delimiter: string) = delimiter.StartsWith(opening, StringComparison.Ordinal)

    /// Pair the fences and classify every line. This is the ONLY place in the repo that decides what a fence is.
    let scan (markdown: string) : Scan =
        // The delimiter that opened the block, the language it declared, and whether it can cite an F# API.
        let mutable openFence: (string * string option * bool) option = None
        let mutable untaggedFences = 0
        let lines = ResizeArray<Line>()

        (markdown.Replace("\r\n", "\n")).Split('\n')
        |> Array.iteri (fun i text ->
            let number = i + 1
            let fence = fenceRegex.Match text

            let kind =
                match openFence with
                | Some(opening, _, _) when fence.Success && closes opening fence.Groups.["fence"].Value ->
                    openFence <- None
                    Delimiter
                | Some(_, lang, isFSharp) -> Fenced(lang, isFSharp)
                | None when fence.Success ->
                    let lang = language fence.Groups.["info"].Value
                    if Option.isNone lang then untaggedFences <- untaggedFences + 1
                    openFence <- Some(fence.Groups.["fence"].Value, lang, lang |> Option.exists isFSharpLanguage)
                    Delimiter
                | None -> Prose

            lines.Add { Number = number; Text = text; Kind = kind })

        { Lines = List.ofSeq lines
          UnclosedFence = Option.isSome openFence
          UntaggedFences = untaggedFences }

    /// The lines inside an F# block — the code a reader COPIES.
    let fsharpLines (scanned: Scan) =
        scanned.Lines
        |> List.filter (fun line ->
            match line.Kind with
            | Fenced(_, isFSharp) -> isFSharp
            | _ -> false)

    /// The lines outside EVERY fence, whatever its language — the document's prose.
    let proseLines (scanned: Scan) =
        scanned.Lines |> List.filter (fun line -> line.Kind = Prose)
