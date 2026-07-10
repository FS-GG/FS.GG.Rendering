namespace FS.GG.TestSupport

open System.IO
open System.Text.Json
open System.Text.RegularExpressions

/// FS-GG/FS.GG.Rendering#264: the single shared enumeration of the SUBSTITUTION-SUBJECT scaffold
/// sources — the trees `dotnet new` rewrites the `Product`/`product` tokens in.
///
/// Two guards depend on this exact set and on the same premise, from opposite directions:
///   * `ScaffoldIdentifierLeakGuardTests` (#149/#152) — the token must not land in an F# *identifier*
///     (a hyphenated product name is a legal name but an illegal identifier);
///   * `Feature264FragmentProseTests` (#264) — the token must not land inside a mathematical *term of
///     art* (`cross product`), where the rewrite destroys the meaning instead of carrying it.
///
/// They were built from two hand-copied enumerations. That is a drift surface of exactly the kind
/// `RepositoryRoot` (Feature 178) was extracted to close: add a substitution-subject tree, update one
/// copy, and the other guard silently narrows and passes VACUOUSLY — the failure mode both guards'
/// "must not narrow to zero" backstops exist to make loud. One list, one premise.
///
/// Out of scope by construction: the `copyOnly` trees (the product-skill `SKILL.md` bodies, the
/// `docs/**` reference trees). The engine does not substitute into them, so their prose is safe and
/// neither guard should read them.
module ScaffoldSources =

    /// The non-`copyOnly` scaffold roots, repo-relative with `/` separators: `template/base/{src,tests}`
    /// plus every capability fragment's `src`/`tests` under `template/fragments/*/`. `tests/` is in
    /// scope because #152's leak (`let readProductFile`) shipped there while a src-only gate looked away.
    let roots (repositoryRoot: string) : string list =
        let repositoryPath (relativePath: string) =
            Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

        [ "template/base/src"; "template/base/tests" ]
        @ (let fragments = repositoryPath "template/fragments"

           if Directory.Exists fragments then
               Directory.GetDirectories fragments
               |> Array.collect (fun d -> [| Path.Combine(d, "src"); Path.Combine(d, "tests") |])
               |> Array.filter Directory.Exists
               |> Array.map (fun d -> Path.GetRelativePath(repositoryRoot, d).Replace('\\', '/'))
               |> Array.toList
           else
               [])

    /// Every `.fs`/`.fsi` source under `roots`, as absolute paths, excluding build output. A caller
    /// that finds this empty must fail loudly rather than pass vacuously.
    let files (repositoryRoot: string) : string list =
        roots repositoryRoot
        |> List.collect (fun root ->
            let full = Path.Combine(repositoryRoot, root.Replace('/', Path.DirectorySeparatorChar))

            if Directory.Exists full then
                [ "*.fs"; "*.fsi" ]
                |> List.collect (fun pattern -> Directory.GetFiles(full, pattern, SearchOption.AllDirectories) |> Array.toList)
                |> List.filter (fun p ->
                    let n = p.Replace('\\', '/')
                    not (n.Contains "/obj/") && not (n.Contains "/bin/"))
            else
                [])

    // ---------------------------------------------------------------------------------------------
    // FS-GG/FS.GG.Rendering#282: the substitution-subject set of EVERY file type, derived from
    // `.template.config/template.json` rather than restated here.
    //
    // `roots`/`files` above answer "which F# sources does the engine rewrite?" — the question both
    // pre-existing guards ask, because a leaked token only breaks compilation or a term of art in
    // F#. #282's class is wider than F#: `replaces` is a plain SUBSTRING match, so the English word
    // `products` mangles to `<Name>s` wherever it lands, and it lands in `Directory.Build.props`,
    // `Directory.Packages.props`, `build.fsx` and `docs/product.md` — none of which `files` can see.
    // Hardcoding that wider list is exactly the drift this module was extracted to close, and it is
    // worse here: the answer is already written down, authoritatively, in `template.json`'s `sources`.
    // So read it. A source ships substituted iff it is NOT `copyOnly`; `include`/`exclude` narrow it.
    //
    // `condition` is deliberately IGNORED — the union over every lane. A file that ships substituted
    // in ANY profile/lifecycle can be mangled in that lane, so scanning the union is the conservative
    // direction: it can only over-scan (a loud false positive), never under-scan (a silent miss).

    /// Translate the bounded glob vocabulary `template.json` actually uses (`**/*`, `dir/**`,
    /// `**/bin/**`, `speckit-*/**`, and bare filenames) into an anchored regex over a `/`-separated
    /// path relative to the source root. `**/` spans zero or more directories; `*` stops at `/`.
    let private globRegex (pattern: string) : Regex =
        let translated = System.Text.StringBuilder()
        let mutable index = 0

        while index < pattern.Length do
            let remaining = pattern.Length - index

            if remaining >= 3 && pattern.[index] = '*' && pattern.[index + 1] = '*' && pattern.[index + 2] = '/' then
                // `**/` spans ZERO or more leading directories, so `**/bin/**` matches a top-level
                // `bin/x` as well as a nested `a/bin/x`.
                translated.Append "(?:.*/)?" |> ignore
                index <- index + 3
            elif remaining >= 2 && pattern.[index] = '*' && pattern.[index + 1] = '*' then
                translated.Append ".*" |> ignore
                index <- index + 2
            elif pattern.[index] = '*' then
                // A single `*` stops at a separator: `speckit-*/**` must not leak across directories.
                translated.Append "[^/]*" |> ignore
                index <- index + 1
            else
                translated.Append(Regex.Escape(string pattern.[index])) |> ignore
                index <- index + 1

        Regex("^" + translated.ToString() + "$", RegexOptions.Compiled)

    /// A JSON string that the schema requires. `null` means `template.json` is malformed, which must
    /// fail loudly here rather than silently narrow the scanned set downstream.
    let private requiredString (element: JsonElement) (context: string) : string =
        match element.GetString() with
        | null -> failwithf "template.json: expected a string for %s, found %A" context element.ValueKind
        | value -> value

    /// The `sources` entries of `.template.config/template.json`, as
    /// `(root, include, exclude, copyOnly)` with the glob lists precompiled.
    let private templateSources (repositoryRoot: string) =
        let templateJson = Path.Combine(repositoryRoot, ".template.config", "template.json")

        use document = JsonDocument.Parse(File.ReadAllText templateJson)

        let globList (element: JsonElement) (name: string) =
            match element.TryGetProperty name with
            | true, array ->
                array.EnumerateArray()
                |> Seq.map (fun p -> globRegex (requiredString p (name + " entry")))
                |> Seq.toList
            | _ -> []

        document.RootElement.GetProperty("sources").EnumerateArray()
        |> Seq.map (fun source ->
            let root = (requiredString (source.GetProperty "source") "sources[].source").TrimEnd '/'
            root, globList source "include", globList source "exclude", globList source "copyOnly")
        |> Seq.toList

    /// Every file the template engine rewrites the `Product`/`product` tokens in, as absolute paths,
    /// across ALL file types (`.fs`, `.props`, `.fsx`, `.md`, ...). Derived from `template.json`, so a
    /// newly added substituted tree is scanned the day it is declared. A caller that finds this empty
    /// — or suspiciously small — must fail loudly rather than pass vacuously.
    let substitutionSubjectFiles (repositoryRoot: string) : string list =
        let sources = templateSources repositoryRoot

        let resolved =
            sources
            |> List.map (fun (root, includes, excludes, copyOnly) ->
                root, Path.Combine(repositoryRoot, root.Replace('/', Path.DirectorySeparatorChar)), includes, excludes, copyOnly)

        // A declared source root that does not exist on disk is NOT "zero files to scan" — it is a
        // renamed or deleted tree that the scan would silently stop covering while staying green.
        // That vacuous pass is precisely what this module was extracted to prevent, and no
        // "must not narrow to zero" backstop downstream can see it (the other roots keep the count
        // healthy). Fail here, naming the root, rather than narrow in silence.
        match resolved |> List.filter (fun (_, full, _, _, _) -> not (Directory.Exists full)) with
        | [] -> ()
        | missing ->
            missing
            |> List.map (fun (root, _, _, _, _) -> root)
            |> String.concat ", "
            |> failwithf
                "template.json declares source root(s) that do not exist: %s — the substitution-subject scan would silently narrow"

        resolved
        |> List.collect (fun (_, full, includes, excludes, copyOnly) ->
            Directory.GetFiles(full, "*", SearchOption.AllDirectories)
            |> Array.toList
            |> List.filter (fun path ->
                let relative = Path.GetRelativePath(full, path).Replace('\\', '/')
                let matches (patterns: Regex list) = patterns |> List.exists (fun rx -> rx.IsMatch relative)

                // `bin/`/`obj/` are excluded by entry 1's globs but NOT by the other source entries,
                // so this is load-bearing rather than belt-and-braces.
                let underBuildOutput =
                    relative.StartsWith "bin/"
                    || relative.StartsWith "obj/"
                    || relative.Contains "/bin/"
                    || relative.Contains "/obj/"

                not underBuildOutput
                && (List.isEmpty includes || matches includes)
                && not (matches excludes)
                && not (matches copyOnly)))
        |> List.distinct
