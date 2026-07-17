namespace FS.GG.DocFences

open System.IO
open FS.GG.TestSupport

/// THE CORPUS/FENCE MAP (spec 255, T004).
///
/// The two fence-BEARING shipped corpora — product skills (`SKILL.md`) and scaffold sources (F# fences
/// inside `///` comments) — read through the ONE fence engine (`MarkdownFences`, #669). The api-surface
/// mirror is deliberately absent: it is GENERATED (`scripts/refresh-api-surface-mirror.fsx`) and carries no
/// fences, so it stays a `val`/prose metadata check behind the retained oracle, not a fence corpus
/// (spec 255 FR-001/FR-006).
///
/// This module only ANSWERS "where are the F# fences, and what does each contain?". Turning a fence into a
/// compilation unit and building it against the pin is the harness's job (T015/T016) and lives elsewhere.
module Corpus =

    /// Which shipped corpus a fence came from — the two the compiler can subsume.
    type CorpusKind =
        | ProductSkill
        | ScaffoldSource

    /// One F# fence, located so a failure stays clickable.
    type FenceBlock =
        { Kind: CorpusKind
          /// Repo-relative path.
          Doc: string
          /// 1-based line of the opening fence in the original document.
          StartLine: int
          /// The fence's F# body lines, in order (delimiters excluded).
          Body: string list
          /// `Some reason` when an in-fence `docfences:skip` directive (T007) excludes this fence from the
          /// compile set — an illustrative fragment, a signature block, deliberate pseudo-code. Local and
          /// reason-carrying, this REPLACES the divorced `pinned-api-doc-ledger.txt` (FR-005/FR-010).
          Skip: string option
          /// Extra `open`s a `docfences:open <ns>` directive adds for THIS fence, on top of the corpus
          /// preamble (D2) — the auditable answer to "which opens are in scope here".
          ExtraOpens: string list }

    let private repoRoot = RepositoryRoot.value

    let private rel (path: string) =
        Path.GetRelativePath(repoRoot, path).Replace('\\', '/')

    /// An in-fence directive (T007), written as an HTML comment (invisible in rendered markdown) on the
    /// line(s) immediately above a fence's opening delimiter:
    ///   `<!-- docfences:skip <reason> -->`     — exclude this fence from the compile set, recording why.
    ///   `<!-- docfences:open FS.GG.X -->`       — add an `open` for this fence, on top of the preamble.
    type private Directive =
        | Skip of reason: string
        | Open of ns: string

    let private directiveRegex =
        System.Text.RegularExpressions.Regex(
            @"<!--\s*docfences:(?<kind>skip|open)\b[:\s]*(?<arg>.*?)\s*-->",
            System.Text.RegularExpressions.RegexOptions.Compiled)

    let private parseDirective (text: string) : Directive option =
        let m = directiveRegex.Match text
        if not m.Success then
            None
        else
            let arg = m.Groups.["arg"].Value.Trim()
            match m.Groups.["kind"].Value with
            | "skip" -> Some(Skip(if arg = "" then "(no reason given)" else arg))
            | _ -> Some(Open arg)

    /// Group the flat F#-line list of a scan into contiguous blocks. `MarkdownFences.scan` marks each line
    /// with its `Number`; a run of consecutive F# lines whose numbers step by one is one fenced block, and a
    /// gap in the numbers is the delimiter (or prose) between two blocks. Directives are gathered from the
    /// comment lines that sit immediately above each block's opening delimiter.
    let private blocks (doc: string) (kind: CorpusKind) (scan: MarkdownFences.Scan) : FenceBlock list =
        let fsharp = MarkdownFences.fsharpLines scan

        // Text of every line, indexed by its 1-based number, so a block can look at the lines above it.
        let byNumber =
            scan.Lines |> List.map (fun l -> l.Number, l.Text) |> Map.ofList

        // Directives on the contiguous comment/blank lines just above the opening delimiter (which sits at
        // StartLine - 1). Walk upward, collecting directives, until a line is neither blank nor a directive.
        let directivesAbove (startLine: int) : Directive list =
            let rec walk n acc =
                match Map.tryFind n byNumber with
                | Some text ->
                    match parseDirective text with
                    | Some d -> walk (n - 1) (d :: acc)
                    | None when text.Trim() = "" -> walk (n - 1) acc
                    | None -> acc
                | None -> acc

            walk (startLine - 2) []

        let attach (startLine: int) (kindV: CorpusKind) (docV: string) (body: string list) =
            let directives = directivesAbove startLine

            let skip =
                directives |> List.tryPick (function Skip r -> Some r | _ -> None)

            let opens =
                directives |> List.choose (function Open ns -> Some ns | _ -> None)

            { Kind = kindV; Doc = docV; StartLine = startLine; Body = body; Skip = skip; ExtraOpens = opens }

        let rec fold acc current =
            match current with
            | [] -> List.rev acc
            | (line: MarkdownFences.Line) :: rest ->
                match acc with
                | block :: older when line.Number = block.StartLine + List.length block.Body ->
                    // Contiguous with the block being built — extend it.
                    fold ({ block with Body = block.Body @ [ line.Text ] } :: older) rest
                | _ ->
                    // Starts a new block. StartLine is the fence body's first line (the opening `` ``` ``
                    // delimiter sits at StartLine - 1).
                    fold (attach line.Number kind doc [ line.Text ] :: acc) rest

        fold [] fsharp

    /// Parse fences (with their `docfences:` directives) out of a markdown STRING — the same reading
    /// `productSkillFences` applies to a file, exposed so the directive mechanism can be exercised against a
    /// fixture without touching a shipped (frozen, manifest-tracked) skill.
    let parseMarkdown (kind: CorpusKind) (doc: string) (markdown: string) : FenceBlock list =
        blocks doc kind (MarkdownFences.scan (markdown.Replace("\r\n", "\n")))

    /// The product-skill corpus: every F# fence in `template/product-skills/**/*.md`, scanned as markdown.
    let productSkillFences () : FenceBlock list =
        let root = Path.Combine(repoRoot, "template", "product-skills")

        if not (Directory.Exists root) then
            []
        else
            Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            |> Seq.collect (fun path ->
                let text = (File.ReadAllText path).Replace("\r\n", "\n")
                blocks (rel path) ProductSkill (MarkdownFences.scan text))
            |> List.ofSeq

    /// The `///` doc-comment text of a scaffold `.fs`, with the `///` prefix stripped but LINE NUMBERS
    /// PRESERVED (every non-doc line becomes blank), so a fence found here still indexes the original file
    /// and a failure stays clickable. Non-doc lines cannot open or close a fence — only `///` content can.
    let private docCommentText (fsSource: string) : string =
        fsSource.Replace("\r\n", "\n").Split('\n')
        |> Array.map (fun line ->
            let trimmed = line.TrimStart()
            if trimmed.StartsWith("///") then
                // Drop the `///` and one optional following space; keep the rest verbatim.
                let after = trimmed.Substring(3)
                if after.StartsWith(" ") then after.Substring(1) else after
            else
                "")
        |> String.concat "\n"

    /// The scaffold-source corpus: F# fences authored inside `///` comments under `template/base/src` and
    /// `template/fragments`. Currently empty until the fences are authored (spec 255 FR-013 / T014b); the
    /// path is implemented now so authoring lights it up with no further wiring.
    let scaffoldSourceFences () : FenceBlock list =
        [ "template/base/src"; "template/fragments" ]
        |> List.map (fun r -> Path.Combine(repoRoot, r.Replace('/', Path.DirectorySeparatorChar)))
        |> List.filter Directory.Exists
        |> List.collect (fun root ->
            Directory.EnumerateFiles(root, "*.fs", SearchOption.AllDirectories) |> List.ofSeq)
        |> List.collect (fun path ->
            let doc = docCommentText (File.ReadAllText path)
            blocks (rel path) ScaffoldSource (MarkdownFences.scan doc))

    /// Every fence-bearing corpus, paired with the fences found in it — so a caller can assert that a corpus
    /// which is SUPPOSED to carry fences has not silently gone empty (spec 255 FR-001, no-silent-drop).
    let all () : (CorpusKind * FenceBlock list) list =
        [ ProductSkill, productSkillFences ()
          ScaffoldSource, scaffoldSourceFences () ]

    /// A scan defect a caller must treat as a defect, not a curiosity: a document that ends inside a fence.
    /// Reported per-doc so the message names the file.
    let unclosedFenceDocs () : string list =
        let roots =
            [ Path.Combine(repoRoot, "template", "product-skills"), "*.md", id ]

        roots
        |> List.collect (fun (root, pattern, (transform: string -> string)) ->
            if not (Directory.Exists root) then
                []
            else
                Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories)
                |> Seq.choose (fun path ->
                    let text = transform ((File.ReadAllText path).Replace("\r\n", "\n"))
                    if (MarkdownFences.scan text).UnclosedFence then Some(rel path) else None)
                |> List.ofSeq)

/// THE PER-CORPUS PREAMBLE (spec 255, T006 / D2).
///
/// The declared default `open` set a fence compiles under — the auditable answer to "which `open`s are in
/// scope here", the information #683 proved is NOT in the token stream. It is deliberately the broad set the
/// skills already write against (measured: 72/77 skill fences compile clean under it); a fence that needs
/// more says so with a `docfences:open` directive (`FenceBlock.ExtraOpens`), and one that cannot compile at
/// all is marked `docfences:skip`. Widen this only when a REAL fence needs it — never pre-emptively.
module Preamble =

    open Corpus

    /// The namespaces the pinned surface exposes that skill/scaffold fences bind against. Ordered so a
    /// later `open` shadows an earlier one predictably (F# resolves an unqualified name to the last `open`).
    let private productSkill =
        [ "FS.GG.UI.Scene"
          "FS.GG.UI.Layout"
          "FS.GG.UI.KeyboardInput"
          "FS.GG.UI.DesignSystem"
          "FS.GG.UI.Symbology"
          "FS.GG.UI.Testing"
          "FS.GG.Game.Core"
          "FS.GG.UI.Controls"
          "FS.GG.UI.Elmish" ]

    /// The default `open`s for a corpus, before a fence's own `docfences:open` additions.
    let forKind (kind: CorpusKind) : string list =
        match kind with
        | ProductSkill -> productSkill
        | ScaffoldSource -> productSkill // same surface; scaffold sources teach the same packages
