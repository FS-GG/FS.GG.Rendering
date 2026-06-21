namespace Rendering.Harness

open System.Diagnostics
open System.Text.RegularExpressions
open SkiaSharp

module X11 =

    // Shell a command, capture (exitCode, stdout). None on failure.
    let sh (cmd: string) (args: string) : (int * string) option =
        try
            let psi = ProcessStartInfo(cmd, args)
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true
            psi.UseShellExecute <- false
            match Process.Start psi with
            | null -> None
            | proc ->
                use p = proc
                let out = p.StandardOutput.ReadToEnd()
                p.WaitForExit 5000 |> ignore
                Some(p.ExitCode, out)
        with _ -> None

    let findWindow (title: string) : int option =
        match sh "xdotool" (sprintf "search --name %s" title) with
        | Some(0, out) ->
            out.Split('\n')
            |> Array.tryPick (fun line ->
                match System.Int32.TryParse(line.Trim()) with
                | true, v -> Some v
                | _ -> None)
        | _ -> None

    let geometry (windowId: int) : string option =
        match sh "xdotool" (sprintf "getwindowgeometry %d" windowId) with
        | Some(0, out) ->
            let m = Regex.Match(out, @"(\d+x\d+)")
            if m.Success then Some m.Groups.[1].Value else None
        | _ -> None

    let activateWindow (windowId: int) : unit =
        sh "xdotool" (sprintf "windowactivate --sync %d" windowId) |> ignore

    let screenshotWindow (windowId: int) (path: string) : bool =
        match sh "maim" (sprintf "-i %d %s" windowId path) with
        | Some(0, _) -> System.IO.File.Exists path
        | _ -> false

    let pngNonBlank (path: string) : bool =
        try
            use bmp = SKBitmap.Decode path
            if isNull bmp then false
            else
                let first = bmp.GetPixel(0, 0)
                let stepX = max 1 (bmp.Width / 24)
                let stepY = max 1 (bmp.Height / 24)
                let mutable diff = false
                let mutable x = 0
                while x < bmp.Width && not diff do
                    let mutable y = 0
                    while y < bmp.Height && not diff do
                        if bmp.GetPixel(x, y) <> first then diff <- true
                        y <- y + stepY
                    x <- x + stepX
                diff
        with _ -> false

    let clickAt (windowId: int) (x: int) (y: int) : unit =
        sh "xdotool" (sprintf "mousemove --window %d %d %d click 1" windowId x y) |> ignore

    let sendKey (windowId: int) (key: string) : unit =
        sh "xdotool" (sprintf "key --window %d %s" windowId key) |> ignore
