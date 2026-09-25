// Read-only Linux source capture. Every component is opened relative to a held
// directory descriptor with O_NOFOLLOW. Captured bytes pass to pure policy.
// No later copy is bound to this capture, and concurrent ABA remains possible.
#load "skill-staging-policy.fsx"
#load "SkillStagingLinuxDescriptors.fsx"

open System
open System.Collections.Generic
open System.IO
open Microsoft.Win32.SafeHandles
open SkillStagingPolicy
open SkillStagingLinuxDescriptors

let private reason = function
    | LinuxDescriptors.Link -> "source-symlink"
    | LinuxDescriptors.NonRegular -> "source-entry-unsupported"
    | LinuxDescriptors.Unreadable -> "source-io"
    | LinuxDescriptors.Changed -> "source-unstable"

let private safeName (name: string) =
    name <> "" && name <> "." && name <> ".."
    && name |> Seq.forall (fun character -> int character <= 127 && character <> '/' && character <> '\\')

let private capturePinned (beforeOpen: string -> unit) (afterOpen: string -> unit)
                          repositoryRoot source : Result<SourceFile list, string> =
    if not (OperatingSystem.IsLinux()) || not BitConverter.IsLittleEndian then
        Error "source-platform-unsupported"
    else
        let root = Path.GetFullPath repositoryRoot
        let relative = Path.GetRelativePath(root, source)
        let components = relative.Split('/', StringSplitOptions.RemoveEmptyEntries) |> Array.toList
        match LinuxDescriptors.openRoot root with
        | Error issue -> Error(reason issue)
        | Ok repository ->
            use repository = repository
            let seen = HashSet<string>(StringComparer.OrdinalIgnoreCase)
            let rec collect (directory: SafeFileHandle) prefix =
                match LinuxDescriptors.namesStable ignore directory with
                | Error issue -> Error(reason issue)
                | Ok names ->
                    names
                    |> List.fold (fun state name ->
                        state |> Result.bind (fun files ->
                            let relative = if prefix = "" then name else prefix + "/" + name
                            if not (safeName name) then Error "source-path-unsafe"
                            elif not (seen.Add relative) then Error "source-path-alias"
                            else
                                beforeOpen relative
                                match LinuxDescriptors.openChild directory name with
                                | Error issue -> Error(reason issue)
                                | Ok(handle, kind) ->
                                    use handle = handle
                                    afterOpen relative
                                    match kind with
                                    | LinuxDescriptors.Special -> Error "source-entry-unsupported"
                                    | LinuxDescriptors.Directory ->
                                        collect handle relative |> Result.map (fun nested -> nested @ files)
                                    | LinuxDescriptors.Regular ->
                                        match LinuxDescriptors.readBytesStable ignore handle with
                                        | Error issue -> Error(reason issue)
                                        | Ok bytes -> Ok({ Path = relative; Bytes = bytes } :: files))) (Ok [])
            // The recursive use scopes keep every parent live through traversal.
            let rec openSource (parent: SafeFileHandle) remaining =
                match remaining with
                | [] -> collect parent ""
                | name :: tail ->
                    if not (safeName name) then Error "source-path-unsafe"
                    else
                        match LinuxDescriptors.openChild parent name with
                        | Error issue -> Error(reason issue)
                        | Ok(handle, LinuxDescriptors.Directory) ->
                            use handle = handle
                            openSource handle tail
                        | Ok(handle, _) ->
                            handle.Dispose()
                            Error "source-not-directory"
            openSource repository components

/// Test hooks bracket each held-fd child open; ordinary capture supplies no hooks.
let inspectSnapshotWithHooks beforeOpen afterOpen repositoryRoot suppliedBy bodyDigest
                             (declared: DeclaredFile list) =
    try
        match sourceDirectory repositoryRoot suppliedBy with
        | Error issue -> Error issue
        | Ok source ->
            capturePinned beforeOpen afterOpen repositoryRoot source
            |> Result.bind (validateSnapshot repositoryRoot suppliedBy bodyDigest declared)
    with
    | :? IOException
    | :? UnauthorizedAccessException
    | :? ArgumentException
    | :? System.Security.SecurityException -> Error "source-io"

let inspectSnapshot repositoryRoot suppliedBy bodyDigest (declared: DeclaredFile list) =
    inspectSnapshotWithHooks ignore ignore repositoryRoot suppliedBy bodyDigest declared
