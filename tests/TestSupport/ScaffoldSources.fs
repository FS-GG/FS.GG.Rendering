namespace FS.GG.TestSupport

open System.IO

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
