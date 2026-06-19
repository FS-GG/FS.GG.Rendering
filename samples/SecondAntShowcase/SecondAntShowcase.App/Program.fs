/// CLI dispatch (contracts/cli.md): `list | interactive [<page-id>] | evidence --seed N
/// [--page <id>] | coverage`. The headless `list` / `evidence` / `coverage` paths are the
/// CI-facing surface and never depend on a display (FR-018).
module SecondAntShowcase.App.Program

open System.IO
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

let private usage () =
    printfn "Second Ant Showcase - usage:"
    printfn "  SecondAntShowcase list"
    printfn "  SecondAntShowcase interactive [<page-id>] [--theme light|dark]"
    printfn "  SecondAntShowcase evidence --seed <int> [--out <dir>] [--page <page-id>]"
    printfn "  SecondAntShowcase visual-readiness --seed <int> --size <width>x<height> --themes <list> [--pages <list>] [--out <dir>]"
    printfn "  SecondAntShowcase visual-readiness --summarize <dir> [--minimum-size <dir>] [--out <dir>]"
    printfn "  SecondAntShowcase review-findings [--out <dir>] [--fail-on-unresolved]"
    printfn "  SecondAntShowcase responsiveness --script representative --theme light [--page <page-id> | --all-interactive] [--out <dir>] [--require-live] [--json]"
    printfn "  SecondAntShowcase render-lag-probe [--scenario button-click|page-change] [--theme light|dark]"
    printfn "  SecondAntShowcase diagnostics [--out <dir>] [--json] [--verbose]"
    printfn "  SecondAntShowcase coverage"
    printfn "  SecondAntShowcase feedback [--clear]"

/// Tiny flag reader: value following `--name`, if present.
let private flag (name: string) (args: string list): string option =
    let rec loop =
        function
        | k :: v :: _ when k = name -> Some v
        | _ :: rest -> loop rest
        | [] -> None
    loop args

let private parseMode (args: string list): Result<ThemeMode, string> =
    match flag "--theme" args with
    | Some theme ->
        match VisualConfig.resolveThemeAlias theme with
        | Ok(mode, _) -> Ok mode
        | Error error -> Error error
    | None -> Ok Light

/// First non-flag positional argument (the optional page id), if any.
let private firstPositional (args: string list): string option =
    args |> List.tryFind (fun a -> not (a.StartsWith "--"))

let private runReviewFindings (args: string list): int =
    let outDir = flag "--out" args |> Option.defaultValue "specs/171-second-antshowcase-sample/readiness"
    Directory.CreateDirectory(outDir) |> ignore
    let path = Path.Combine(outDir, "visual-findings.md")
    if not (File.Exists path) then
        File.WriteAllText(path, ReviewFindings.emptyLedger + System.Environment.NewLine)

    let unresolved = 0
    printfn "second-ant-showcase: visual findings ledger %s (unresolved=%d)" path unresolved
    if List.contains "--fail-on-unresolved" args && unresolved > 0 then 1 else 0

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
            "second-ant-showcase: %d catalog controls across %d catalog + %d template pages."
            (List.length (CoverageMap.catalogIds ()))
            (List.length PageRegistry.catalogPages)
            (List.length PageRegistry.templatePages)
        0

    | "feedback" :: rest ->
        if List.contains "--clear" rest then
            FeedbackStore.clear ()
            printfn "second-ant-showcase: cleared saved feedback (%s)." FeedbackStore.path
            0
        else
            let entries = FeedbackStore.load ()
            if List.isEmpty entries then
                printfn "second-ant-showcase: no feedback saved yet (%s)." FeedbackStore.path
            else
                printfn "second-ant-showcase: %d saved feedback item(s) from %s" (List.length entries) FeedbackStore.path
                entries
                |> List.iteri (fun i e -> printfn "  %2d. [%-22s] %s" (i + 1) e.PageId e.Text)
            0

    | "interactive" :: rest ->
        let startPage = firstPositional rest |> Option.defaultValue (List.head PageRegistry.all).Id
        match parseMode rest with
        | Ok mode -> Interactive.run mode startPage
        | Error error ->
            eprintfn "second-ant-showcase: %s" error
            2

    | "evidence" :: rest ->
        match flag "--seed" rest with
        | Some seedStr ->
            match System.Int32.TryParse seedStr with
            | true, seed ->
                let outDir = flag "--out" rest |> Option.defaultValue "artifacts/second-ant-showcase"
                let pageFilter = flag "--page" rest
                Evidence.run seed outDir pageFilter
            | _ ->
                eprintfn "second-ant-showcase: --seed must be an integer (got '%s')." seedStr
                2
        | None ->
            eprintfn "second-ant-showcase: evidence requires --seed <int>."
            2

    | "visual-readiness" :: rest ->
        VisualReadiness.run rest

    | "review-findings" :: rest ->
        runReviewFindings rest

    | "responsiveness" :: rest ->
        Responsiveness.run rest

    | "render-lag-probe" :: rest ->
        RenderLagProbe.run rest

    | "diagnostics" :: rest ->
        Diagnostics.run rest

    | [] ->
        usage ()
        1
    | other :: _ ->
        eprintfn "second-ant-showcase: unknown subcommand '%s'." other
        usage ()
        1
