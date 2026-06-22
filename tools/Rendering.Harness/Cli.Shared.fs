module Rendering.Harness.CliShared

open System
open System.IO
open Rendering.Harness
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let outDir (rest: string list) =
    let rec find xs =
        match xs with
        | "--out" :: d :: _ -> Some d
        | _ :: tl -> find tl
        | [] -> None
    match find rest with
    | Some d -> d
    | None -> Path.Combine("artifacts", "harness", "run-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"))

let flagValue (flag: string) (rest: string list) =
    let rec find xs =
        match xs with
        | f :: v :: _ when f = flag -> Some v
        | _ :: tl -> find tl
        | [] -> None
    find rest

// Feature 181 (US2): feature selection routes through the single FeatureCatalog descriptor table
// instead of 12 hand-duplicated alias predicates. `tryByAlias` accepts the same "NNN"/"featureNNN"/
// slug forms the old isFeature### predicates accepted (C-CT-3/C-FD-4; locked by FeatureCatalogTests).
let selectFeature (rest: string list) =
    flagValue "--feature" rest
    |> Option.bind FeatureCatalog.FeatureDescriptor.tryByAlias

let isFeatureId (id: int) (rest: string list) =
    selectFeature rest |> Option.exists (fun d -> d.Id = id)

let isFeature148 rest = isFeatureId 148 rest
let isFeature149 rest = isFeatureId 149 rest
let isFeature152 rest = isFeatureId 152 rest
let isFeature153 rest = isFeatureId 153 rest
let isFeature154 rest = isFeatureId 154 rest
let isFeature155 rest = isFeatureId 155 rest
let isFeature156 rest = isFeatureId 156 rest
let isFeature157 rest = isFeatureId 157 rest
let isFeature158 rest = isFeatureId 158 rest
let isFeature159 rest = isFeatureId 159 rest
let isFeature160 rest = isFeatureId 160 rest
let isFeature161 rest = isFeatureId 161 rest

let attemptCount (rest: string list) =
    match flagValue "--attempt-count" rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> 1
    | None -> 1

let positiveIntFlag flag fallback rest =
    match flagValue flag rest with
    | Some value ->
        match Int32.TryParse value with
        | true, parsed when parsed > 0 -> parsed
        | _ -> fallback
    | None -> fallback

let proofSize: Size = { Width = 640; Height = 480 }

let sentinelScene () =
    SceneNode.Group
        [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 12uy 18uy 30uy)
          Scene.rectangle (32.0, 32.0, 132.0, 92.0) (Colors.rgb 64uy 220uy 144uy)
          Scene.rectangle (320.0, 200.0, 96.0, 96.0) (Colors.rgb 220uy 180uy 64uy)
          Scene.rectangle (500.0, 48.0, 72.0, 144.0) (Colors.rgb 72uy 96uy 210uy) ]

let damageScene () =
    SceneNode.Group
        [ Scene.rectangle (0.0, 0.0, 640.0, 480.0) (Colors.rgb 12uy 18uy 30uy)
          Scene.rectangle (32.0, 32.0, 132.0, 92.0) (Colors.rgb 64uy 220uy 144uy)
          Scene.rectangle (320.0, 200.0, 96.0, 96.0) (Colors.rgb 236uy 80uy 96uy)
          Scene.rectangle (500.0, 48.0, 72.0, 144.0) (Colors.rgb 72uy 96uy 210uy) ]

let captureProofImage command app hostFacts path scene =
    let options: ViewerOptions =
        { Title = "Feature155 native proof capture"
          InitialSize = proofSize
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }

    let request: ScreenshotEvidenceRequest =
        { Command = command
          AppOrSample = app
          OutputPath = path
          Width = proofSize.Width
          Height = proofSize.Height
          RendererMode = "skia"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = hostFacts
          Timeout = TimeSpan.FromSeconds 10.0 }

    Viewer.captureScreenshotEvidence request options scene

let colorToken (color: SkiaSharp.SKColor) =
    $"{color.Red}-{color.Green}-{color.Blue}-{color.Alpha}"

let tryPixel (path: string) (x: int) (y: int) =
    try
        use bitmap = SkiaSharp.SKBitmap.Decode(path)
        if Object.ReferenceEquals(bitmap, null) || x < 0 || y < 0 || x >= bitmap.Width || y >= bitmap.Height then
            None
        else
            Some(bitmap.GetPixel(x, y) |> colorToken)
    with _ ->
        None

let fileDecodableNonBlank (path: string) =
    try
        use bitmap = SkiaSharp.SKBitmap.Decode(path)
        if Object.ReferenceEquals(bitmap, null) then
            false
        else
            let mutable nonBlank = false
            let mutable y = 0
            while not nonBlank && y < bitmap.Height do
                let mutable x = 0
                while not nonBlank && x < bitmap.Width do
                    let pixel = bitmap.GetPixel(x, y)
                    if pixel.Alpha <> 0uy && (pixel.Red <> 0uy || pixel.Green <> 0uy || pixel.Blue <> 0uy) then
                        nonBlank <- true
                    x <- x + 16
                y <- y + 16
            nonBlank
    with _ ->
        false
