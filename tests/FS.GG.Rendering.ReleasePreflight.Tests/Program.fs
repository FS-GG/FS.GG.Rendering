open System
open System.IO
open FS.GG.Rendering.ReleasePreflight

let source =
    [
        "FS.GG.UI.Build"; "FS.GG.UI.Canvas"; "FS.GG.UI.Controls"
        "FS.GG.UI.Controls.Elmish"; "FS.GG.UI.DesignSystem"; "FS.GG.UI.Diagnostics"
        "FS.GG.UI.Elmish"; "FS.GG.UI.KeyboardInput"; "FS.GG.UI.Layout"
        "FS.GG.UI.Scene"; "FS.GG.UI.Scene.SvgBrowser"; "FS.GG.UI.SkiaViewer"
        "FS.GG.UI.Symbology"; "FS.GG.UI.Symbology.Render"; "FS.GG.UI.Testing"
        "FS.GG.UI.Themes.AntDesign"; "FS.GG.UI.Themes.Default"
    ]
    |> List.map (fun id -> { Id = id; Kind = "library" })
    |> fun libraries -> libraries @ [ { Id = "FS.GG.UI"; Kind = "bom" }; { Id = "FS.GG.UI.Template"; Kind = "template" } ]

let root = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../.."))
let raw = File.ReadAllText(Path.Combine(root, "eng/release/svg-preview-c-0.31.0.json"))
let mutable passed = 0

let clean label facts target json =
    let findings = Plan.inspect facts target json
    if not findings.IsEmpty then failwithf "%s: unexpected findings %A" label findings
    passed <- passed + 1
    printfn "PASS %s" label

let refused label expected facts target json =
    let findings = Plan.inspect facts target json
    if not (findings |> List.exists (fun issue -> issue.StartsWith(expected, StringComparison.Ordinal))) then
        failwithf "%s: expected %s in %A" label expected findings
    passed <- passed + 1
    printfn "PASS %s" label

[<EntryPoint>]
let main _ =
    clean "current plan matches independent source inventory" source "0.31.0" raw
    clean "source fact order does not change verdict" (List.rev source) "0.31.0" raw

    let missing = raw.Split('\n') |> Array.filter (fun line -> not (line.Contains("\"id\": \"FS.GG.UI.Canvas\""))) |> String.concat "\n"
    refused "missing package row" "package-count" source "0.31.0" missing
    refused "replaced library identity" "package-roster" source "0.31.0" (raw.Replace("\"id\": \"FS.GG.UI.Canvas\"", "\"id\": \"FS.GG.UI.Impostor\""))
    refused "case alias collides with library identity" "package-duplicate" source "0.31.0" (raw.Replace("\"id\": \"FS.GG.UI.Canvas\"", "\"id\": \"fs.gg.ui.build\""))
    refused "wrong package kind" "package-kind-mismatch" source "0.31.0" (raw.Replace("\"id\": \"FS.GG.UI.Canvas\", \"kind\": \"library\"", "\"id\": \"FS.GG.UI.Canvas\", \"kind\": \"bom\""))

    refused "duplicate root JSON property" "duplicate-json:$.version" source "0.31.0" ("{\"version\":\"0.0.0\"," + raw.Substring(1))
    refused "duplicate package JSON property" "duplicate-json:$.packages[0].id" source "0.31.0" (raw.Replace("{ \"id\": \"FS.GG.UI.Build\"", "{ \"id\": \"wrong\", \"id\": \"FS.GG.UI.Build\""))
    refused "newer baseline rolls back target" "baseline-order" source "0.31.0" (raw.Replace("\"baselineVersion\": \"0.30.0\"", "\"baselineVersion\": \"0.32.0\""))
    refused "equal baseline has no new release" "baseline-order" source "0.31.0" (raw.Replace("\"baselineVersion\": \"0.30.0\"", "\"baselineVersion\": \"0.31.0\""))
    refused "requested version differs" "plan-version" source "0.32.0" raw
    refused "missing source package fact" "source-count" (source.Tail) "0.31.0" raw
    refused "case colliding source facts" "source-duplicate" ({ source.Head with Id = "fs.gg.ui.canvas" } :: source) "0.31.0" raw
    refused "malformed JSON" "json-invalid" source "0.31.0" "{"

    printfn "release preflight reducer: %d controls passed" passed
    0
