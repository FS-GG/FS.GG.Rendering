// Linux-only, read-only manifest capture for the provisional skill copy plan.
// This pins the manifest read to opened descriptors; it does not make the
// manifest and all product-root captures one atomic filesystem snapshot.
#load "SkillStagingCapturedPlan.fsx"

open System
open System.IO
open Microsoft.Win32.SafeHandles
open SkillStagingLinuxDescriptors
open SkillStagingCapturedPlan

let private manifestComponents = [ "template"; "skill-manifest"; "skill-manifest.json" ]

let private reason = function
    | LinuxDescriptors.Link -> "manifest-symlink"
    | LinuxDescriptors.NonRegular -> "manifest-nonregular"
    | LinuxDescriptors.Unreadable -> "manifest-io"
    | LinuxDescriptors.Changed -> "manifest-unstable"

/// Hooks are for deterministic race controls; ordinary capture passes ignore.
let captureManifestWithHooks (beforeOpen: string -> unit) (afterOpen: string -> unit)
                             (afterFirstChunk: unit -> unit) repositoryRoot : Result<byte[], string> =
    if not (OperatingSystem.IsLinux()) || not BitConverter.IsLittleEndian then
        Error "manifest-platform-unsupported"
    else
        try
            match LinuxDescriptors.openRoot (Path.GetFullPath repositoryRoot) with
            | Error issue -> Error(reason issue)
            | Ok repository ->
                use repository = repository
                let rec descend (parent: SafeFileHandle) prefix remaining =
                    match remaining with
                    | [] -> Error "manifest-io"
                    | name :: tail ->
                        let relative = if prefix = "" then name else prefix + "/" + name
                        beforeOpen relative
                        match LinuxDescriptors.openChild parent name with
                        | Error issue -> Error(reason issue)
                        | Ok(handle, kind) ->
                            use handle = handle
                            afterOpen relative
                            match tail, kind with
                            | [], LinuxDescriptors.Regular ->
                                LinuxDescriptors.readBytesStable afterFirstChunk handle
                                |> Result.map (fun (raw, _) -> Array.copy raw)
                                |> Result.mapError reason
                            | [], _ -> Error "manifest-nonregular"
                            | _ :: _, LinuxDescriptors.Directory -> descend handle relative tail
                            | _ -> Error "manifest-nondirectory"
                descend repository "" manifestComponents
        with
        | :? IOException
        | :? UnauthorizedAccessException
        | :? ArgumentException
        | :? System.Security.SecurityException -> Error "manifest-io"

let captureManifest repositoryRoot =
    captureManifestWithHooks ignore ignore ignore repositoryRoot

let preparePinnedWithHooks beforeOpen afterOpen afterFirstChunk repositoryRoot =
    captureManifestWithHooks beforeOpen afterOpen afterFirstChunk repositoryRoot
    |> Result.bind (prepareCurrent repositoryRoot)

let preparePinned repositoryRoot =
    preparePinnedWithHooks ignore ignore ignore repositoryRoot

/// Reopen the current manifest by descriptor, then recheck the owned file plan.
/// A change after this check, or an ABA during capture, is still possible.
let verifyPinnedCurrent (plan: StagePlan) repositoryRoot =
    captureManifest repositoryRoot
    |> Result.bind (verifyCurrent plan repositoryRoot)
