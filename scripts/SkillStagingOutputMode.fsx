// Pure mode projection for a successful fresh-target Python skill stage.
// No output tree is created or inspected here. ACLs and ownership are not modeled.
#load "SkillStagingCapturedPlan.fsx"

open System
open System.Collections.Generic
open SkillStagingCapturedPlan

type OutputKind = File | Directory
type OutputModeEntry = { Path: string; Kind: OutputKind; Mode: uint32 }

type OutputModeContract internal (entries: OutputModeEntry list) =
    member _.Entries = entries

type OutputModeRefusal =
    | InvalidUmask of uint32
    | DuplicatePath of string
    | MissingPath of string
    | UnexpectedPath of string
    | WrongKind of string
    | WrongMode of string

/// The live Python stager recreates `out`, writes the manifest with open(wb),
/// and uses shutil.copytree's default copy2/copystat behavior for skill trees.
/// This contract assumes a successful new target under the supplied umask.
let expected (plan: StagePlan) (umask: uint32) : Result<OutputModeContract, OutputModeRefusal> =
    if umask &&& (~~~0o777u) <> 0u then Error(InvalidUmask umask)
    else
        let createdDirectoryMode = 0o777u &&& (~~~umask)
        let createdFileMode = 0o666u &&& (~~~umask)
        let entries =
            [ { Path = "."; Kind = Directory; Mode = createdDirectoryMode }
              { Path = "skills"; Kind = Directory; Mode = createdDirectoryMode }
              { Path = "skill-manifest.json"; Kind = File; Mode = createdFileMode } ]
            @ (plan.Directories |> List.map (fun directory ->
                { Path = directory.Destination; Kind = Directory; Mode = directory.SourceMode }))
            @ (plan.Files |> List.map (fun file ->
                { Path = file.Destination; Kind = File; Mode = file.SourceMode }))
        entries
        |> List.sortWith (fun left right -> StringComparer.Ordinal.Compare(left.Path, right.Path))
        |> OutputModeContract
        |> Ok

/// Check separately observed or proposed path, kind, and mode facts against
/// the contract. This is pure; it does not attest to a staged package.
let verify (contract: OutputModeContract) (observed: OutputModeEntry list) =
    let expected = contract.Entries
    let seen = HashSet<string>(StringComparer.Ordinal)
    match observed |> List.tryFind (fun entry -> not (seen.Add entry.Path)) with
    | Some duplicate -> Error(DuplicatePath duplicate.Path)
    | None ->
        match expected |> List.tryFind (fun want ->
            not (observed |> List.exists (fun item -> item.Path = want.Path))) with
        | Some missing -> Error(MissingPath missing.Path)
        | None ->
            match observed |> List.tryFind (fun item ->
                not (expected |> List.exists (fun want -> want.Path = item.Path))) with
            | Some extra -> Error(UnexpectedPath extra.Path)
            | None ->
                expected
                |> List.tryPick (fun want ->
                    let actual = observed |> List.find (fun item -> item.Path = want.Path)
                    if actual.Kind <> want.Kind then Some(WrongKind want.Path)
                    elif actual.Mode <> want.Mode then Some(WrongMode want.Path)
                    else None)
                |> function Some issue -> Error issue | None -> Ok ()
