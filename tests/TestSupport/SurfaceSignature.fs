namespace FS.GG.TestSupport

open System.Text.RegularExpressions

/// Reading an F# signature file the way the api-surface gates need to read it.
module SurfaceSignature =

    /// A DECLARATION, not a mention: start of line, optional indent, then `val`/`module`/`type` `internal`.
    ///
    /// `type` is in the list and it is not decoration. #585 is written in terms of `val internal` and
    /// `module internal` — those are what its table counts — but a `type internal` is the same defect and
    /// the WORSE one: `RuntimeStampResult<'msg>` shipped in the mirror as the return type of three
    /// `val internal` functions, so a gate that stripped only the functions would leave an ORPHAN type
    /// declared, unreferenced, and still uncallable. Anchoring on the issue's literal wording would have
    /// reported green over a live leak.
    ///
    /// Anchoring matters the other way too. The naive `grep -rc "val internal"` (#585's own reproduce line)
    /// also counts PROSE: the signatures legitimately mention `module internal RetainedRender` and `module
    /// internal Reconcile` inside `///` doc comments when explaining why a PUBLIC member behaves as it
    /// does. Those mentions must survive — a reader that eats them strips the explanation and leaves the
    /// member.
    ///
    /// Group 1 is the INDENT — `stripInternalDeclarations` measures the declaration's nesting from it to
    /// decide how much of what follows (continuations, a module or type body) belongs to it.
    let internalDeclaration = Regex(@"^(\s*)(val|module|type)\s+internal\s")

    /// Drop every `internal` declaration from a signature: the `val`/`module` line itself, the doc comment
    /// and attributes attached above it, its signature continuations, and — for a module — its whole
    /// indented body.
    ///
    /// S-INT (#585). The bundled `template/base/docs/api-surface/**` mirror ships the PRODUCT-VISIBLE
    /// surface only. A product author reads the mirror from their OWN assembly, where an `internal` member
    /// does not exist — and where the `internal` keyword cannot warn them, because in their assembly it is
    /// not "restricted", it is simply ABSENT. So a mirrored `val internal` documents, at length and with a
    /// doc comment indistinguishable from a public one, a symbol they can never call.
    ///
    /// The `src/` originals KEEP these declarations: they are real `InternalsVisibleTo` seams, and an
    /// `.fsi` that merely omits a value makes it PRIVATE to its implementation file rather than internal to
    /// the assembly — so deleting them at the source would break the tests that legitimately reach them.
    /// The mirror is therefore not a byte copy of its original but its original MINUS the internals, and
    /// every gate that compares the two (`ApiSurfaceMirrorTests`, `ApiSurfaceMirrorCoherenceTests`,
    /// `Symbology.Tests.ApiSurfaceMirrorTests`) composes this with its own comparison.
    ///
    /// Defined ONCE, here, rather than copied into each of those three gates. A copy of a subtle text
    /// transform rots — which is the very risk `ReleaseOnlyTwinLockstepTests` exists to police.
    ///
    /// HONESTY CAVEAT (constitution Principle V): a line-shape reader over signature files, not an F#
    /// parser. It is sound for the declaration shapes this repo's `.fsi` files actually use.
    let stripInternalDeclarations (lines: string array) =
        let isBlank (line: string) = line.Trim() = ""
        let indentOf (line: string) = line.Length - line.TrimStart(' ').Length

        let isAttached (line: string) =
            let trimmed = line.TrimStart(' ')
            trimmed.StartsWith "///" || trimmed.StartsWith "[<"

        let kept = ResizeArray<string>()
        let mutable i = 0
        let mutable removedAny = false

        while i < lines.Length do
            let declaration = internalDeclaration.Match lines.[i]

            if declaration.Success then
                removedAny <- true
                let indent = declaration.Groups.[1].Value.Length

                // the doc comment / attributes attached above it
                while kept.Count > 0 && isAttached kept.[kept.Count - 1] && indentOf kept.[kept.Count - 1] = indent do
                    kept.RemoveAt(kept.Count - 1)

                // the declaration, its signature continuations, and (for a module) its whole body
                i <- i + 1

                while i < lines.Length && (isBlank lines.[i] || indentOf lines.[i] > indent) do
                    i <- i + 1

                // the blank that separated it from its neighbour went with it: put one back
                if kept.Count > 0 && not (isBlank kept.[kept.Count - 1]) && i < lines.Length then
                    kept.Add ""
            else
                kept.Add lines.[i]
                i <- i + 1

        // Only the trailing blanks WE created, by consuming a declaration that sat at the end of the file.
        // Guarded on `removedAny` so the transform is the IDENTITY on a signature with no internals: the
        // Symbology gate strips `Legibility.fsi` and `Render.fsi` (zero internals) and compares the result
        // to a byte copy, so an unconditional trim would report a mirror as "drifted" the day a source file
        // happens to end with a blank line, when nothing had drifted at all.
        if removedAny then
            while kept.Count > 0 && isBlank kept.[kept.Count - 1] do
                kept.RemoveAt(kept.Count - 1)

        kept.ToArray()
