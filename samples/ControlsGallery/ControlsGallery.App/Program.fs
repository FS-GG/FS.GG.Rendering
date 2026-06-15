/// CLI dispatch (contracts/cli.md): `interactive | evidence | coverage-check`. The
/// headless `evidence` + `coverage-check` paths are the CI-facing surface and never
/// depend on a display (FR-016).
module ControlsGallery.App.Program

open FS.GG.UI.Themes.Default.Theming
open ControlsGallery.Core
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private usage () =
    printfn "Controls Gallery — usage:"
    printfn "  ControlsGallery interactive [--theme light|dark] [--accent indigo|teal]"
    printfn "  ControlsGallery evidence --seed <int> [--out <dir>] [--page <page-id>]"
    printfn "  ControlsGallery coverage-check"

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

let private parseAccent (args: string list) =
    match flag "--accent" args with
    | Some "teal" -> GalleryTheme.teal
    | _ -> GalleryTheme.indigo

[<EntryPoint>]
let main argv =
    match Array.toList argv with
    | "coverage-check" :: _ ->
        let result = CoverageMap.check ()
        printfn "%s" (CoverageMap.summary ())
        if CoverageMap.isClean result then 0 else 1

    | "interactive" :: rest -> Interactive.run (parseMode rest) (parseAccent rest)

    | "evidence" :: rest ->
        match flag "--seed" rest with
        | Some seedStr ->
            match System.Int32.TryParse seedStr with
            | true, seed ->
                let outDir = flag "--out" rest |> Option.defaultValue "artifacts/controls-gallery"
                let pageFilter = flag "--page" rest
                Evidence.run seed outDir pageFilter
            | _ ->
                eprintfn "controls-gallery: --seed must be an integer (got '%s')." seedStr
                2
        | None ->
            eprintfn "controls-gallery: evidence requires --seed <int>."
            2

    | [] ->
        usage ()
        1
    | other :: _ ->
        eprintfn "controls-gallery: unknown subcommand '%s'." other
        usage ()
        1
