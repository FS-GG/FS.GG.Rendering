/// CLI dispatch (contracts/cli.md): `list | interactive <id> | evidence --seed N | coverage`.
/// The headless `list` / `evidence` / `coverage` paths are the CI-facing surface and never
/// depend on a display (FR-014).
module SampleApps.App.Program

open System.IO
open FS.GG.UI.Themes.Default.Theming
open SampleApps.Core
open SampleApps.Core.Harness

let private usage () =
    printfn "Sample Apps — usage:"
    printfn "  SampleApps list"
    printfn "  SampleApps interactive <sample-id> [--theme light|dark]"
    printfn "  SampleApps evidence --seed <int> [--sample <sample-id>] [--out <dir>]"
    printfn "  SampleApps coverage [--out <file>]"

/// Tiny flag reader: value following `--name`, if present.
let private flag (name: string) (args: string list): string option =
    let rec loop =
        function
        | k :: v :: _ when k = name -> Some v
        | _ :: rest -> loop rest
        | [] -> None
    loop args

let private parseMode (args: string list): ThemeMode =
    match flag "--theme" args with
    | Some "dark" -> Dark
    | _ -> Light

let private listSamples () =
    if List.isEmpty Registry.all then
        printfn "sample-apps: 0 samples registered."
    for e in Registry.all do
        printfn
            "  %-10s %-13s %-26s inputs=[%s] controls=[%s]"
            e.Id
            e.Family
            e.Title
            (String.concat "; " e.Inputs)
            (String.concat "; " e.Controls)

[<EntryPoint>]
let main argv =
    match Array.toList argv with
    | "list" :: _ ->
        listSamples ()
        0

    | "coverage" :: rest ->
        let report = Coverage.render ()
        printfn "%s" report
        match flag "--out" rest with
        | Some path -> File.WriteAllText(path, report)
        | None -> ()
        if Coverage.isClean (Coverage.check ()) then 0 else 1

    | "interactive" :: id :: rest -> Interactive.run id (parseMode rest)
    | [ "interactive" ] ->
        eprintfn "sample-apps: interactive requires a <sample-id>."
        1

    | "evidence" :: rest ->
        match flag "--seed" rest with
        | Some seedStr ->
            match System.Int32.TryParse seedStr with
            | true, seed ->
                let outDir = flag "--out" rest |> Option.defaultValue "artifacts/sample-apps"
                let sampleFilter = flag "--sample" rest
                Evidence.run seed outDir sampleFilter
            | _ ->
                eprintfn "sample-apps: --seed must be an integer (got '%s')." seedStr
                2
        | None ->
            eprintfn "sample-apps: evidence requires --seed <int>."
            2

    | [] ->
        usage ()
        1
    | other :: _ ->
        eprintfn "sample-apps: unknown subcommand '%s'." other
        usage ()
        1
