#load "FeedbackReportTool.fs"

open System
open System.IO
open FsGgFeedbackReportTool

let fail (messages: string list) =
    printfn "feedback-tool: FAIL: validation failed (%d error(s))" messages.Length

    for message in messages do
        eprintfn "feedback-tool: %s" message

    exit 1

let pass message =
    printfn "feedback-tool: PASS: %s" message

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

let requiredList options name =
    required options name
    |> fun value -> value.Split(';')
    |> Array.map (fun value -> value.Trim())
    |> Array.toList

let argv = fsi.CommandLineArgs |> Array.skip 1

match argv with
| [| "digest"; path |] ->
    if not (File.Exists path) then
        fail [ sprintf "file not found: %s" path ]

    File.ReadAllText path |> sha256Text |> printfn "%s"
| [| "validate"; path; "--audit"; auditPath |] ->
    if not (File.Exists path) then
        fail [ sprintf "report not found: %s" path ]

    if not (File.Exists auditPath) then
        fail [ sprintf "audit not found: %s" auditPath ]

    let reportText = File.ReadAllText path

    let audit =
        validateActionabilityAuditAtReportHeadDetailed
            (Directory.GetCurrentDirectory())
            (Path.GetFullPath path)
            reportText
            (File.ReadAllText auditPath)

    let errors = validateReportText reportText @ audit.errors

    if not (List.isEmpty audit.notBound) then
        printfn
            "feedback-tool: %d citation(s) NOT BOUND -- reported rather than checked:"
            audit.notBound.Length

        for citation in audit.notBound do
            printfn "  %s %s" citation.findingId citation.locator
            printfn "    %s" citation.reason

    if List.isEmpty errors then
        if not (List.isEmpty audit.notBound) then
            printfn "feedback-tool: %d citation(s) were not checked (listed above)." audit.notBound.Length

        pass (sprintf "valid actionability-bound schema-v2 report: %s" path)
    else
        fail errors
| [| "validate"; _ |] ->
    fail [ "validate requires --audit <feedback/audits/report.audit.json>" ]
| args when args.Length > 0 && args.[0] = "check-invalidation" ->
    let options = parseOptions args.[1..]
    let root = Map.tryFind "root" options |> Option.defaultValue (Directory.GetCurrentDirectory())
    // The two input forms differ in their AUDIT INDEX, not only in how they learn
    // which paths changed, and that is the whole of #1243. --base/--head indexes the
    // base ref's tree, so an audit the candidate itself introduced is not in it;
    // --changed has no ref to index from and states that its subject is the working
    // tree. Both print the subject they used, so a green verdict never leaves which
    // one it was to be inferred.
    let result =
        match Map.tryFind "changed" options, Map.tryFind "base" options, Map.tryFind "head" options with
        | Some value, None, None ->
            value.Split(';') |> Array.toList |> findInvalidatedAuditBindings root
        | None, Some baseRef, Some headRef -> checkInvalidationBetweenRefs root baseRef headRef
        | _ -> fail [ "check-invalidation requires either --changed \"path;path\" or --base REF --head REF" ]

    if not (List.isEmpty result.errors) then
        fail result.errors
    else
        for disposition in result.dispositions do
            printfn
                "feedback-tool: applied exception %s to %s %s %s -> %s"
                disposition.id
                disposition.audit
                disposition.findingId
                disposition.locator
                disposition.replacementPath

        if List.isEmpty result.invalidated then
            pass (sprintf "no merged feedback-audit bindings were invalidated (audit index: %s)" result.subject)
        else
            printfn
                "feedback-tool: %d merged feedback-audit binding(s) invalidated (audit index: %s):"
                result.invalidated.Length
                result.subject

            for item in result.invalidated do
                eprintfn "feedback-tool: invalidated %s %s %s (%s)" item.audit item.findingId item.locator item.report

            fail [ "commit touches evidence cited by merged feedback audit(s)" ]
| [| "validate-checkpoints"; path |] ->
    let errors = validateCheckpointFile path

    if List.isEmpty errors then
        pass (sprintf "valid checkpoint file: %s" path)
    else
        fail errors
| args when args.Length > 0 && args.[0] = "validate-checkpoint-state" ->
    let options = parseOptions args.[1..]
    let root = Map.tryFind "root" options |> Option.defaultValue (Directory.GetCurrentDirectory())
    let cycle = required options "cycle"
    let errors = validateCheckpointState root cycle

    if List.isEmpty errors then
        pass (sprintf "valid checkpoint state for cycle %s" cycle)
    else
        fail errors
| args when args.Length > 0 && args.[0] = "activate" ->
    let options = parseOptions args.[1..]
    let root = Map.tryFind "root" options |> Option.defaultValue (Directory.GetCurrentDirectory())

    try
        let path =
            appendZeroEventActivation
                root
                (required options "cycle")
                (requiredList options "phases")
                (requiredList options "evidence")
                (required options "reason")

        printfn "feedback-tool: recorded zero-event activation: %s" path
    with ex ->
        fail [ ex.Message ]
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
          "  feedback-tool.fsx -- activate --cycle ID --phases \"PHASE;PHASE\" --evidence \"LOCATOR;LOCATOR\" --reason TEXT [--root PATH]"
          "  feedback-tool.fsx -- digest <text-file>"
          "  feedback-tool.fsx -- validate feedback/<report>.md --audit feedback/audits/<report>.audit.json"
          "  feedback-tool.fsx -- check-invalidation --base REF --head REF [--root PATH]"
          "      audits are indexed from --base's tree; changed paths are derived from --base to --head"
          "  feedback-tool.fsx -- check-invalidation --changed \"path;path\" [--root PATH]"
          "      advanced: audits are indexed from the WORKING TREE and the complete name-status path set is yours to supply"
          "  feedback-tool.fsx -- validate-checkpoints feedback/checkpoints/<cycle>.jsonl"
          "  feedback-tool.fsx -- validate-checkpoint-state --cycle ID [--root PATH]" ]
