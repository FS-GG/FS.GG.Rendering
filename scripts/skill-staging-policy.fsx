// Provisional pure policy seam for Rendering's schema-v2 skill staging.
// The Python stager remains the live packing gate. No file copy, feed, or package
// operation occurs here; callers supply source bytes and get a closed-set verdict.
module SkillStagingPolicy

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography

type DeclaredFile = { Path: string; Sha256: string }
type SourceFile = { Path: string; Bytes: byte[] }

let canonicalDigest (raw: byte[]) : string =
    let start =
        if raw.Length >= 3 && raw.[0] = 0xefuy && raw.[1] = 0xbbuy && raw.[2] = 0xbfuy then 3 else 0

    let folded = ResizeArray<byte>()
    let mutable i = start

    while i < raw.Length do
        if raw.[i] = 0x0duy && i + 1 < raw.Length && raw.[i + 1] = 0x0auy then
            folded.Add 0x0auy
            i <- i + 2
        else
            folded.Add raw.[i]
            i <- i + 1

    SHA256.HashData(folded.ToArray()) |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

let private safeRelative (path: string) =
    if String.IsNullOrWhiteSpace path || path.Contains(char 0) || Path.IsPathRooted path ||
       (path.Length >= 2 && Char.IsLetter path.[0] && path.[1] = ':') ||
       (path |> Seq.exists (fun value -> value > char 127)) then
        Error "unsafe-path"
    else
        let parts = path.Replace('\\', '/').Split('/')

        if parts |> Array.exists (fun part -> part = ".." || part = "." || part = "") then
            Error "unsafe-path"
        else
            Ok(String.Join("/", parts))

// Lexical containment only. A filesystem adapter must resolve every existing path
// component and refuse symlink escapes before reading or copying source bytes.
let sourceDirectory (repositoryRoot: string) (suppliedBy: string) : Result<string, string> =
    let trimmed =
        if String.IsNullOrWhiteSpace suppliedBy then ""
        else suppliedBy.TrimEnd('/', '\\')
    match safeRelative trimmed with
    | Error _ -> Error "supplied-by-unsafe"
    | Ok relative ->
        let root = Path.GetFullPath repositoryRoot
        let directory = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)))
        let prefix = root.TrimEnd(Path.DirectorySeparatorChar) + string Path.DirectorySeparatorChar

        if directory.StartsWith(prefix, StringComparison.Ordinal) then Ok directory
        else Error "supplied-by-wrong-root"

let validateSnapshot
    (repositoryRoot: string)
    (suppliedBy: string)
    (bodyDigest: string)
    (declared: DeclaredFile list)
    (actual: SourceFile list)
    : Result<string list, string> =
    // This verdict binds the byte arrays supplied at this call. A later copier must
    // use an immutable snapshot or re-read and revalidate to close the mutation gap.
    match sourceDirectory repositoryRoot suppliedBy with
    | Error reason -> Error reason
    | Ok _ ->
        let normalize path = safeRelative path

        let normalizeDeclared =
            declared
            |> List.map (fun row -> normalize row.Path |> Result.map (fun path -> path, row.Sha256))

        let normalizeActual =
            actual
            |> List.map (fun row -> normalize row.Path |> Result.map (fun path -> path, row.Bytes))

        if List.isEmpty declared || List.isEmpty actual then Error "empty-file-set"
        elif normalizeDeclared |> List.exists Result.isError then Error "declared-path-unsafe"
        elif normalizeActual |> List.exists Result.isError then Error "source-path-unsafe"
        elif actual |> List.exists (fun row -> isNull row.Bytes) then Error "source-bytes-missing"
        else
            let declarations = normalizeDeclared |> List.choose Result.toOption
            let sources = normalizeActual |> List.choose Result.toOption
            let keys rows = rows |> List.map fst
            let distinct rows =
                let seen = HashSet<string>(StringComparer.OrdinalIgnoreCase)
                rows |> keys |> List.forall seen.Add

            if not (distinct declarations) then Error "duplicate-declared-file"
            elif not (distinct sources) then Error "duplicate-source-file"
            elif Set.ofList (keys declarations) <> Set.ofList (keys sources) then Error "file-set-mismatch"
            else
                let sourceByPath = Map.ofList sources
                let declaredByPath = Map.ofList declarations

                match declaredByPath |> Map.tryFind "SKILL.md" with
                | None -> Error "body-missing"
                | Some digest when digest <> bodyDigest -> Error "body-digest-binding"
                | Some _ ->
                    if declarations |> List.exists (fun (path, digest) ->
                        digest <> canonicalDigest sourceByPath.[path]) then
                        Error "source-digest-mismatch"
                    else
                        declarations |> keys |> List.sort |> Ok
