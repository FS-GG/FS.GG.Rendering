#load "FeedbackReportTool.fs"

open System
open System.IO
open FsGgFeedbackReportTool

let fail messages =
    for message in messages do
        eprintfn "feedback-tool: %s" message

    exit 1

let parseOptions (args: string array) =
    let rec loop index options =
        if index >= args.Length then
            options
        elif not (args.[index].StartsWith "--") then
            fail [ sprintf "expected --option, got '%s'" args.[index] ]
        elif index + 1 >= args.Length then
            fail [ sprintf "missing value for %s" args.[index] ]
        else
            loop (index + 2) (Map.add (args.[index].Substring 2) args.[index + 1] options)

    loop 0 Map.empty

let required options name =
    match Map.tryFind name options with
    | Some value when not (String.IsNullOrWhiteSpace value) -> value
    | _ -> fail [ sprintf "missing --%s" name ]

let argv = fsi.CommandLineArgs |> Array.skip 1

match argv with
| [| "validate"; path |] ->
    if not (File.Exists path) then
        fail [ sprintf "report not found: %s" path ]

    let errors = File.ReadAllText path |> validateReportText

    if List.isEmpty errors then
        printfn "feedback-tool: valid schema-v2 report: %s" path
    else
        fail errors
| [| "validate-checkpoints"; path |] ->
    let errors = validateCheckpointFile path

    if List.isEmpty errors then
        printfn "feedback-tool: valid checkpoint file: %s" path
    else
        fail errors
| args when args.Length > 0 && args.[0] = "checkpoint" ->
    let options = parseOptions args.[1..]
    let root = Map.tryFind "root" options |> Option.defaultValue (Directory.GetCurrentDirectory())

    try
        let path =
            appendCheckpoint
                root
                (required options "cycle")
                (required options "phase")
                (required options "surface")
                (required options "kind")
                (required options "summary")
                (required options "evidence")
                (required options "cost")
                (required options "owner")

        printfn "feedback-tool: appended checkpoint: %s" path
    with :? ArgumentException as ex ->
        fail [ ex.Message ]
| _ ->
    fail
        [ "usage:"
          "  feedback-tool.fsx -- checkpoint --cycle ID --phase PHASE --surface ID --kind KIND --summary TEXT --evidence TEXT --cost TEXT --owner TEXT [--root PATH]"
          "  feedback-tool.fsx -- validate feedback/<report>.md"
          "  feedback-tool.fsx -- validate-checkpoints feedback/checkpoints/<cycle>.jsonl" ]

