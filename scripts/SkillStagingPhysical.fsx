// Read-only, observation-time source adapter for the provisional skill policy.
// Path checks and reads are separate operations: this is not a race-free staging
// authority. A copier needs handle-bound reads or an immutable validated snapshot.
#load "skill-staging-policy.fsx"

open System
open System.IO
open SkillStagingPolicy

let private isLink (entry: FileSystemInfo) =
    not (isNull entry.LinkTarget)
    || (entry.Exists && entry.Attributes.HasFlag FileAttributes.ReparsePoint)

let private directoryChain (path: string) =
    let rec collect (current: string) =
        let parent = Path.GetDirectoryName current
        if String.IsNullOrEmpty parent || parent = current then [ current ]
        else collect parent @ [ current ]
    collect path

let private checkSourceDirectories (path: string) =
    directoryChain path
    |> List.fold (fun result part ->
        result |> Result.bind (fun () ->
            let directory = DirectoryInfo part
            if isLink directory then Error "source-symlink"
            elif not directory.Exists then Error "source-not-directory"
            else Ok ())) (Ok ())

let rec private collectFiles (directory: DirectoryInfo) (prefix: string) : Result<SourceFile list, string> =
    directory.EnumerateFileSystemInfos()
    |> Seq.sortBy (fun entry -> entry.Name)
    |> Seq.fold (fun result entry ->
        result |> Result.bind (fun files ->
            let relative = if prefix = "" then entry.Name else prefix + "/" + entry.Name
            if isLink entry then Error "source-symlink"
            elif entry.Attributes.HasFlag FileAttributes.Directory then
                collectFiles (DirectoryInfo entry.FullName) relative
                |> Result.map (fun nested -> nested @ files)
            elif entry :? FileInfo then
                Ok ({ Path = relative; Bytes = File.ReadAllBytes entry.FullName } :: files)
            else Error "source-entry-unsupported")) (Ok [])

/// Inspect one current source tree without writing to it. Every path component
/// and every enumerated entry must be a non-link before bytes reach the pure
/// policy. This does not bind those checks to later reads or copying.
let inspectSnapshot repositoryRoot suppliedBy bodyDigest (declared: DeclaredFile list) =
    try
        match sourceDirectory repositoryRoot suppliedBy with
        | Error reason -> Error reason
        | Ok source ->
            match checkSourceDirectories source with
            | Error reason -> Error reason
            | Ok () ->
                collectFiles (DirectoryInfo source) ""
                |> Result.bind (validateSnapshot repositoryRoot suppliedBy bodyDigest declared)
    with
    | :? IOException
    | :? UnauthorizedAccessException
    | :? ArgumentException
    | :? System.Security.SecurityException -> Error "source-io"
