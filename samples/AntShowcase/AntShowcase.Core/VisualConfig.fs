module AntShowcase.Core.VisualConfig

open FS.GG.UI.Scene
open AntShowcase.Core.Model

type AcceptedSizeRole =
    | Preferred
    | Minimum
    | Custom

let preferredSize: Size = { Width = 1600; Height = 1000 }
let minimumSize: Size = { Width = 1280; Height = 800 }

let sizeText (size: Size): string =
    sprintf "%dx%d" size.Width size.Height

let preferredSizeText = sizeText preferredSize
let minimumSizeText = sizeText minimumSize

let supportedThemeIds = [ "antLight"; "antDark" ]

let minimumRepresentativePageIds =
    [ "data-collections"
      "charts-statistical"
      "charts-advanced"
      "feedback-status"
      "tpl-form"
      "tpl-exception" ]

let visualReadinessStatusAccepted = "accepted"
let visualReadinessStatusBlocked = "blocked"
let visualReadinessStatusEnvironmentLimited = "environment-limited"

let parseSize (text: string): Result<Size, string> =
    match text.Split('x') with
    | [| widthText; heightText |] ->
        match System.Int32.TryParse widthText, System.Int32.TryParse heightText with
        | (true, width), (true, height) when width > 0 && height > 0 -> Result.Ok { Width = width; Height = height }
        | _ -> Result.Error(sprintf "unsupported size '%s'; expected <width>x<height>" text)
    | _ -> Result.Error(sprintf "unsupported size '%s'; expected <width>x<height>" text)

let classifySize (size: Size): AcceptedSizeRole =
    if size = preferredSize then Preferred
    elif size = minimumSize then Minimum
    else Custom

let roleName (role: AcceptedSizeRole): string =
    match role with
    | Preferred -> "preferred"
    | Minimum -> "minimum"
    | Custom -> "custom"

let resolveThemeAlias (text: string): Result<ThemeMode * string, string> =
    match text.Trim().ToLowerInvariant() with
    | "light"
    | "antlight" -> Result.Ok(Light, "antLight")
    | "dark"
    | "antdark" -> Result.Ok(Dark, "antDark")
    | other -> Result.Error(sprintf "unsupported theme '%s'; expected light,dark,antLight,antDark" other)

let resolveThemeList (text: string): Result<(ThemeMode * string) list, string> =
    let parts = text.Split(',', System.StringSplitOptions.RemoveEmptyEntries ||| System.StringSplitOptions.TrimEntries) |> Array.toList
    if List.isEmpty parts then
        Result.Error "at least one theme is required"
    else
        let resolved = parts |> List.map resolveThemeAlias
        let errors = resolved |> List.choose (function Result.Error e -> Some e | Result.Ok _ -> None)
        if not (List.isEmpty errors) then
            Result.Error(String.concat "; " errors)
        else
            Result.Ok(resolved |> List.choose (function Result.Ok v -> Some v | Result.Error _ -> None))
