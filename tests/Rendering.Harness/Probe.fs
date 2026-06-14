namespace Rendering.Harness

open System
open System.Diagnostics
open System.Globalization
open System.Text.RegularExpressions

module Probe =

    // Run a process, capture stdout; return None on any failure (safe degradation).
    let tryRun (cmd: string) (args: string) : string option =
        try
            let psi = ProcessStartInfo(cmd, args)
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true
            psi.UseShellExecute <- false
            match Process.Start(psi) with
            | null -> None
            | proc ->
                use p = proc
                let out = p.StandardOutput.ReadToEnd()
                p.WaitForExit(5000) |> ignore
                if p.HasExited && p.ExitCode = 0 then Some out else None
        with _ -> None

    let env (name: string) =
        match Environment.GetEnvironmentVariable(name) with
        | null -> None
        | "" -> None
        | v -> Some v

    let knownExtensions = [ "XTEST"; "Present"; "RANDR"; "DRI3"; "XInputExtension"; "XInput" ]

    let probe () : ProbeFacts =
        let display = env "DISPLAY"
        let xdpy = tryRun "xdpyinfo" ""
        let xrandr = tryRun "xrandr" ""
        let glx = tryRun "glxinfo" "-B"

        // backend: X11 only when DISPLAY is set AND xdpyinfo answers; else Wayland if WAYLAND_DISPLAY; else none
        let backend =
            match display, xdpy with
            | Some _, Some _ -> X11
            | _ ->
                match env "WAYLAND_DISPLAY" with
                | Some _ -> Wayland
                | None -> NoDisplay

        let extensions =
            match xdpy with
            | None -> []
            | Some text -> knownExtensions |> List.filter (fun e -> text.Contains(e))

        // refresh + connected output from xrandr (the current '*' mode line and the "connected" output)
        let refreshHz =
            match xrandr with
            | None -> None
            | Some text ->
                let m = Regex.Match(text, @"(\d+\.\d+)\*")
                if m.Success then
                    match Double.TryParse(m.Groups.[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture) with
                    | true, v -> Some v
                    | _ -> None
                else None

        let vblankSource =
            match xrandr with
            | None -> None
            | Some text ->
                let m = Regex.Match(text, @"^(\S+) connected", RegexOptions.Multiline)
                if m.Success then Some m.Groups.[1].Value else None

        let glRenderer =
            match glx with
            | None -> None
            | Some text ->
                let m = Regex.Match(text, @"OpenGL renderer string:\s*(.+)")
                if m.Success then Some (m.Groups.[1].Value.Trim()) else None

        let glVersion =
            match glx with
            | None -> None
            | Some text ->
                let m = Regex.Match(text, @"OpenGL version string:\s*(.+)")
                if m.Success then Some (m.Groups.[1].Value.Trim()) else None

        let glDirect =
            match glx with
            | Some text -> text.Contains("direct rendering: Yes")
            | None -> false

        { EffectiveBackend = backend
          Display = display
          GlRenderer = glRenderer
          GlVersion = glVersion
          GlDirect = glDirect
          RefreshHz = refreshHz
          Extensions = extensions
          SwapControl = None // requires a live GL context; populated by T3 when available
          VblankSource = vblankSource
          UinputAvailable = IO.File.Exists("/dev/uinput") && IO.Directory.Exists("/dev/input") }
