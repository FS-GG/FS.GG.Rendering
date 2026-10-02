module SceneSvgBrowserPolicyTestsProgram

open System
open System.IO
open System.Security.Cryptography
open Expecto
open FS.GG.UI.Scene.SvgBrowser

let private digest path =
    use stream = File.OpenRead path

    SHA256.HashData stream
    |> Convert.ToHexString
    |> fun value -> value.ToLowerInvariant()

[<EntryPoint>]
let main argv =
    let supplied =
        Environment.GetEnvironmentVariable "SVG_EXTERNAL_PRODUCTION_DLL"
        |> Option.ofObj
        |> Option.filter (String.IsNullOrWhiteSpace >> not)
        |> Option.defaultValue (
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "..",
                "..",
                "src",
                "Scene.SvgBrowser",
                "bin",
                "Release",
                "net10.0",
                "FS.GG.UI.Scene.SvgBrowser.dll"
            )
        )

    let selected = Path.GetFullPath supplied
    let loaded = typeof<SvgExternalSessionState>.Assembly.Location |> Path.GetFullPath

    if digest selected <> digest loaded then
        failwith
            $"selected production assembly did not match loaded policy assembly: selected={selected}; loaded={loaded}"

    Tests.runTestsInAssemblyWithCLIArgs [] argv
