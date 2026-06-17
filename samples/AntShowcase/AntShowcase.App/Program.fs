/// CLI dispatch (contracts/cli.md): `list | interactive [<page-id>] | evidence --seed N
/// [--page <id>] | coverage`. The headless `list` / `evidence` / `coverage` paths are the
/// CI-facing surface and never depend on a display (FR-018).
module AntShowcase.App.Program

open AntShowcase.Core
open AntShowcase.Core.Model

let private usage () =
    printfn "Ant Design Controls Showcase — usage:"
    printfn "  AntShowcase list"
    printfn "  AntShowcase interactive [<page-id>] [--theme light|dark]"
    printfn "  AntShowcase evidence --seed <int> [--out <dir>] [--page <page-id>]"
    printfn "  AntShowcase coverage"
    printfn "  AntShowcase feedback [--clear]"

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

/// First non-flag positional argument (the optional page id), if any.
let private firstPositional (args: string list): string option =
    args |> List.tryFind (fun a -> not (a.StartsWith "--"))

[<EntryPoint>]
let main argv =
    match Array.toList argv with
    | "coverage" :: _ ->
        let result = CoverageMap.check ()
        printfn "%s" (CoverageMap.summary ())
        if CoverageMap.isClean result then 0 else 1

    | "list" :: _ ->
        for p in PageRegistry.all do
            let kind =
                match p.Kind with
                | Catalog -> "catalog"
                | Template -> "template"
            printfn "  %-22s %-9s %s" p.Id kind p.Title
        printfn
            "ant-showcase: %d catalog controls across %d catalog + %d template pages."
            (List.length (CoverageMap.catalogIds ()))
            (List.length PageRegistry.catalogPages)
            (List.length PageRegistry.templatePages)
        0

    | "feedback" :: rest ->
        if List.contains "--clear" rest then
            FeedbackStore.clear ()
            printfn "ant-showcase: cleared saved feedback (%s)." FeedbackStore.path
            0
        else
            let entries = FeedbackStore.load ()
            if List.isEmpty entries then
                printfn "ant-showcase: no feedback saved yet (%s)." FeedbackStore.path
            else
                printfn "ant-showcase: %d saved feedback item(s) from %s" (List.length entries) FeedbackStore.path
                entries
                |> List.iteri (fun i e -> printfn "  %2d. [%-22s] %s" (i + 1) e.PageId e.Text)
            0

    | "interactive" :: rest ->
        let startPage = firstPositional rest |> Option.defaultValue (List.head PageRegistry.all).Id
        Interactive.run (parseMode rest) startPage

    | "evidence" :: rest ->
        match flag "--seed" rest with
        | Some seedStr ->
            match System.Int32.TryParse seedStr with
            | true, seed ->
                let outDir = flag "--out" rest |> Option.defaultValue "artifacts/ant-showcase"
                let pageFilter = flag "--page" rest
                Evidence.run seed outDir pageFilter
            | _ ->
                eprintfn "ant-showcase: --seed must be an integer (got '%s')." seedStr
                2
        | None ->
            eprintfn "ant-showcase: evidence requires --seed <int>."
            2

    | [] ->
        usage ()
        1
    | other :: _ ->
        eprintfn "ant-showcase: unknown subcommand '%s'." other
        usage ()
        1
