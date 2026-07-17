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
          Body: string list }

    let private repoRoot = RepositoryRoot.value

    let private rel (path: string) =
        Path.GetRelativePath(repoRoot, path).Replace('\\', '/')

    /// Group the flat F#-line list of a scan into contiguous blocks. `MarkdownFences.scan` marks each line
    /// with its `Number`; a run of consecutive F# lines whose numbers step by one is one fenced block, and a
    /// gap in the numbers is the delimiter (or prose) between two blocks.
    let private blocks (doc: string) (kind: CorpusKind) (scan: MarkdownFences.Scan) : FenceBlock list =
        let fsharp = MarkdownFences.fsharpLines scan

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
                    fold
                        ({ Kind = kind; Doc = doc; StartLine = line.Number; Body = [ line.Text ] } :: acc)
                        rest

        fold [] fsharp

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
