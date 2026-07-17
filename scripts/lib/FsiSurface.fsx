// #752 — THE ONE READER OF AN `.fsi`, AND THE ONE WRITER OF ONE.
//
// `scripts/refresh-api-surface-mirror.fsx` generates `template/base/docs/api-surface/**` from the
// signature files the PINNED packages carry (`api-surface/` in the nupkg, put there by
// `Directory.Build.local.props` and proven by `scripts/check-packed-api-surface.fsx`). This module is
// the parser and the emitter it uses, and it is deliberately the ONLY copy of either.
//
// WHY A PARSER AND NOT THE PE/METADATA WALK THE ITEM ASKED FOR. #752's plan was to lift the
// `MetadataReader` walk out of `tests/Build.Tests/TemplateConsumesPinnedApiTests.fs` and generate from
// IL. It cannot work, and the reason is not a gap but a category error: a metadata walk yields NAMES,
// an `.fsi` needs SIGNATURES. F# type ABBREVIATIONS (`type CommandId = string`) compile to literally
// nothing in IL — the mirror declares five — so no reader of metadata will ever recover them. The
// signature file the compiler already checked IS the artifact; the pin now carries it, so it is read
// rather than reconstructed. That is why nothing here opens a DLL, and why the metadata walk in
// Build.Tests stays exactly where it is: it answers a DIFFERENT question (what does the assembly
// EXPORT — the doc-vs-pin oracle), and merging the two would be the #661 hazard, not the cure for it.
//
// WHAT IT HAS TO GET RIGHT. The mirror is a CURATED SUBSET — measured, it declares 1,292 of the pin's
// 2,575 members — and the curation is per-MEMBER at arbitrary depth (`module Paint` mirrors 13 of the
// pin's 23 vals). So a whole-file copy is wrong and a whole-block copy is wrong; the unit of selection
// has to be the individual `type`/`val`, with `module` as a container the generator can descend into
// and re-emit around a subset of its children.
//
// TYPES ARE ATOMIC, AND THAT IS MEASURED, NOT ASSUMED. All 415 types the mirror declares are present in
// the pin, and 412 of them are byte-identical once whitespace is normalised; the 3 that differ do so
// only by line-wrapping. The mirror has never curated the INSIDE of a type — no dropped union case, no
// dropped record field — which is what makes a type emittable verbatim. If that ever stops being true
// the round-trip check in the generator goes red rather than silently emitting a type with a case the
// pin does not have.
//
// INDENTATION IS THE STRUCTURE. An `.fsi` is offside-rule F#, so a declaration owns every following
// line indented deeper than it. That is the whole parse. It is not a general F# parser and must not
// become one: it reads the narrow, uniform shape this org's `dotnet pack` ships, and anything it cannot
// classify it reports rather than guesses at (`Unparsed`), because a guess here silently changes what
// the scaffold teaches.

open System
open System.IO
open System.Text.RegularExpressions

/// A declaration, with the verbatim text it owns.
///
/// `Lead` is the doc-comment/attribute block immediately above the declaration — it is kept SEPARATE
/// from `Decl` because the prose sidecar (#752) replaces exactly this and nothing else: 201 of the
/// mirror's 3,286 doc lines are mirror-only teaching prose the pin does not carry, and they attach to
/// members. Splicing them in means replacing a member's lead, not patching its text by offset.
type Node =
    { Kind: string // "type" | "val" | "module" | "and"
      Name: string // the dotted path WITHIN the file: "Paint", "Paint.fill"
      Lead: string list
      Decl: string list
      Children: Node list
      /// Was this declaration separated from the one above it by a blank line in the source?
      ///
      /// Emitting one blank between every member looks like harmless normalisation and is not: `src/`
      /// writes runs of related `val`s ADJACENT, and `ApiSurfaceMirrorTests`' in-repo exact-copy rule
      /// compares two mirrors byte-for-byte against their `src/` original. A generator that re-spaces
      /// them fails that rule while changing nothing anyone can see. The pin's own spacing is the
      /// answer, so it is carried rather than invented.
      Spaced: bool
      /// Every line the node owns, lead included, exactly as the pin wrote it.
      Text: string list }

let private declRx =
    Regex(@"^(?<ind>\s*)(?<kw>type|module|val|and)\s+(?<rest>.*)$", RegexOptions.Compiled)

/// The declared name, stripped of the noise a signature file puts around it: accessibility
/// (`module internal Reconcile`) and the `=`/`:` that ends it. `module internal` matters — the pin ships
/// three, they are NOT public surface, and a mirror must never declare one.
///
/// ARITY IS PART OF THE NAME, and dropping it is not a cosmetic loss — it silently picks the WRONG TYPE.
/// The pinned FS.GG.UI.SkiaViewer exports BOTH `type ViewerEffect` (Viewer.Types.fsi — the closed effect
/// DU the mirror teaches, carrying `PlayAudio`/`Persist`) and an unrelated `type ViewerEffect<'msg>`
/// (Host/Diagnostics.fsi). Keyed on the bare name they collide, and a lookup then returns whichever the
/// file order happened to put first: measured, this generator emitted the Diagnostics one — a mirror
/// that taught a `ViewerEffect` with no `Persist` case, from a package where `Persist` is the whole
/// point of #535. `TemplateConsumesPinnedApiTests.fs` names this exact pair as the reason its own oracle
/// keys on arity (and #594's ledger with it); a second reader of the same surface that did NOT would be
/// two copies of one rule disagreeing about what a type IS — the #648 shape.
///
/// Rendered IL-style (``Foo`1``) because that is what the rest of the tree already writes.
let private nameOf (rest: string) =
    let rest = Regex.Replace(rest, @"^\s*(internal|private|public)\s+", "")
    let m = Regex.Match(rest, @"^\(?\s*(?<n>[A-Za-z_][A-Za-z0-9_']*|\([^)]*\))(?<g><[^>]*>)?")

    if not m.Success then
        ""
    else
        let name = m.Groups.["n"].Value
        let g = m.Groups.["g"]

        if not g.Success then
            name
        else
            // Only a TYPE declaration carries its generic parameters right after the name; a `val`
            // writes `val f: Foo<'a> -> …`, where the `<` follows the colon and never the name.
            let inner = g.Value.Trim([| '<'; '>' |])

            if inner.Trim() = "" then
                name
            else
                let arity = inner.Split(',').Length
                $"{name}`{arity}"

let private isLead (s: string) =
    s.StartsWith("///", StringComparison.Ordinal)
    || s.StartsWith("[<", StringComparison.Ordinal)
    || s.StartsWith("//", StringComparison.Ordinal)

/// Parse one `.fsi` into the tree of declarations it makes, keyed by dotted path within the file.
///
/// `namespace` and `open` are file-level furniture rather than declarations, so they are neither nodes
/// nor parse failures. The generator does NOT infer the emitted opens from them: the mirror curates its
/// opens too (`FS.GG.UI.SkiaViewer` mirrors five of the pin's six — `open SkiaSharp` is deliberately not
/// taught, since no mirrored signature mentions it), so the manifest declares them and this is only what
/// keeps them out of `Unparsed`.
///
/// FAILS LOUD, NOT OPEN. A line this cannot classify at a level it is reading is returned in the
/// `Unparsed` list rather than dropped. A parser that silently skips what it does not understand emits
/// a mirror that is missing exactly the thing nobody looked at (#266), and the generator treats a
/// non-empty `Unparsed` for a file it draws from as a hard error.
let parseFsi (lines: string list) : Node list * string list =
    let arr = List.toArray lines
    let unparsed = ResizeArray<string>()

    let indentOf (s: string) = s.Length - s.TrimStart().Length

    // Parse the declarations at `minIndent` between [i, stop). Returns nodes and the index reached.
    let rec parseLevel (i: int) (stop: int) (minIndent: int) (prefix: string) : Node list * int =
        let nodes = ResizeArray<Node>()
        let mutable idx = i
        let mutable lead: string list = []

        while idx < stop do
            let raw = arr.[idx]
            let s = raw.Trim()

            if s = "" then
                lead <- lead @ [ raw ]
                idx <- idx + 1
            elif isLead s && indentOf raw >= minIndent then
                lead <- lead @ [ raw ]
                idx <- idx + 1
            else
                let m = declRx.Match raw

                if m.Success && indentOf raw = minIndent then
                    let kw = m.Groups.["kw"].Value
                    let name = nameOf (m.Groups.["rest"].Value)

                    // The block this declaration owns: every following line indented deeper, and the
                    // blank lines between them. A blank line does NOT end a block — it separates
                    // members inside one.
                    let mutable j = idx + 1

                    while j < stop
                          && (arr.[j].Trim() = "" || indentOf arr.[j] > minIndent) do
                        j <- j + 1

                    // Trailing blanks belong to the SEPARATOR, not to the block.
                    let mutable e = j

                    while e > idx + 1 && arr.[e - 1].Trim() = "" do
                        e <- e - 1

                    let declEnd =
                        // A declaration can wrap (`val addBodies:` + an indented continuation). Its own
                        // lines are those before the first child declaration, if any.
                        let mutable k = idx + 1
                        let mutable stopAt = e

                        while k < e do
                            let cm = declRx.Match arr.[k]

                            if cm.Success && arr.[k].Trim() <> "" && indentOf arr.[k] > minIndent then
                                stopAt <- min stopAt k
                                k <- e
                            else
                                k <- k + 1

                        // ...but the child's DOC BLOCK is the child's, not the parent's. Stopping at the
                        // child's `type`/`val` line hands its `///` lines to the parent's `Decl`, and the
                        // first child of every module then parses with an EMPTY lead — its prose still
                        // renders (as part of the module shell), so nothing looks broken, while the doc
                        // silently belongs to the wrong declaration and no override can ever address it.
                        // Measured: this is what dropped `Wav.PcmData`'s "Decoded PCM payload of a WAV
                        // file." on the floor. Walk back over the contiguous lead the child owns.
                        let mutable b = stopAt

                        while b > idx + 1 && isLead (arr.[b - 1].Trim()) do
                            b <- b - 1

                        b

                    let path = if prefix = "" then name else prefix + "." + name

                    let children, _ =
                        if kw = "module" && declEnd < e then
                            let childIndent =
                                [ for k in declEnd .. e - 1 do
                                    if arr.[k].Trim() <> "" then yield indentOf arr.[k] ]
                                |> function
                                    | [] -> minIndent + 4
                                    | xs -> List.min xs

                            parseLevel declEnd e childIndent path
                        else
                            [], declEnd

                    // Leading blanks in `lead` are separation, not the node's own.
                    let ownLead = lead |> List.skipWhile (fun l -> l.Trim() = "")

                    nodes.Add
                        { Kind = kw
                          Name = path
                          Lead = ownLead
                          Decl = [ for k in idx .. declEnd - 1 -> arr.[k] ]
                          Children = children
                          Spaced = lead.Length > ownLead.Length
                          Text = ownLead @ [ for k in idx .. e - 1 -> arr.[k] ] }

                    lead <- []
                    // `e`, not `j`: `j` is past the block's TRAILING BLANKS, and those blanks are the
                    // separator between this declaration and the next one — they belong to the next
                    // node's lead, which is where `Spaced` reads them from. Skipping to `j` swallows
                    // them here, every following member parses as unspaced, and the emitter then packs
                    // the file solid.
                    idx <- e
                else
                    if s <> ""
                       && not (s.StartsWith("namespace", StringComparison.Ordinal))
                       && not (s.StartsWith("open ", StringComparison.Ordinal)) then
                        unparsed.Add raw

                    lead <- []
                    idx <- idx + 1

        List.ofSeq nodes, idx

    // Top level: the shallowest indent any declaration sits at (0 in every file the pin ships).
    let nodes, _ = parseLevel 0 arr.Length 0 ""
    nodes, List.ofSeq unparsed

/// Flatten to (path, node) for every declaration at any depth.
let rec flatten (nodes: Node list) : (string * Node) list =
    nodes
    |> List.collect (fun n -> (n.Name, n) :: flatten n.Children)

/// Re-indent a verbatim block from the indent it was written at to the one it is emitted at.
let reindent (delta: int) (lines: string list) =
    if delta = 0 then
        lines
    else
        lines
        |> List.map (fun l ->
            if l.Trim() = "" then ""
            elif delta > 0 then String(' ', delta) + l
            else
                let strip = min -delta (l.Length - l.TrimStart().Length)
                l.Substring strip)
