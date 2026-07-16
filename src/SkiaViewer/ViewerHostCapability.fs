namespace FS.GG.UI.SkiaViewer

open System

module internal HostCapability =
        let envOption name : string option =
            match Environment.GetEnvironmentVariable name with
            | null -> None
            | value when String.IsNullOrWhiteSpace value -> None
            | value -> Some value

        let desktopSessionDiagnostic () =
            let runtimeDirectory = envOption "XDG_RUNTIME_DIR"

            let runtimeDirectoryExists =
                runtimeDirectory |> Option.exists IO.Directory.Exists

            let displayVariable =
                let wayland = envOption "WAYLAND_DISPLAY"
                let x11 = envOption "DISPLAY"

                match wayland, x11 with
                | Some value, _ -> Some $"WAYLAND_DISPLAY={value}"
                | None, Some value -> Some $"DISPLAY={value}"
                | None, None -> None

            let displaySocket =
                let wayland = envOption "WAYLAND_DISPLAY"
                let x11 = envOption "DISPLAY"

                match runtimeDirectory, wayland, x11 with
                | Some runtimeDir, Some wayland, _ ->
                    Some(IO.Path.Combine(runtimeDir, wayland))
                | _, _, Some display ->
                    let number = display.TrimStart(':').Split('.').[0]
                    Some($"/tmp/.X11-unix/X{number}")
                | _ -> None

            let displaySocketExists =
                displaySocket |> Option.exists IO.File.Exists

            let sessionBus = envOption "DBUS_SESSION_BUS_ADDRESS"

            let fallback = IO.Path.Combine(IO.Path.GetTempPath(), "fs-gg-ui-runtime")

            let blockedReason =
                if not (OperatingSystem.IsLinux()) then
                    None
                elif runtimeDirectory.IsNone then
                    Some "XDG_RUNTIME_DIR is missing; interactive Linux launch is blocked before app lifecycle debugging."
                elif not runtimeDirectoryExists then
                    Some "XDG_RUNTIME_DIR does not exist; interactive Linux launch is blocked before app lifecycle debugging."
                elif displayVariable.IsNone then
                    Some "DISPLAY or WAYLAND_DISPLAY is missing; interactive Linux launch is blocked before app lifecycle debugging."
                elif displaySocket.IsSome && not displaySocketExists then
                    Some "Display socket is missing; interactive Linux launch is blocked before app lifecycle debugging."
                else
                    None

            let diagnosticClass, message =
                if not (OperatingSystem.IsLinux()) then
                    "environment-session-not-required", "Desktop session diagnostic is not required on this host."
                else
                    match blockedReason with
                    | Some reason -> "unsupported-host", reason
                    | None -> "environment-session-ready", "Desktop session prerequisites are present."

            { RuntimeDirectory = runtimeDirectory
              RuntimeDirectoryExists = runtimeDirectoryExists
              RuntimeDirectoryOwnerSuitable = runtimeDirectoryExists
              RuntimeDirectoryPermissionsSuitable = runtimeDirectoryExists
              DisplayVariable = displayVariable
              DisplaySocket = displaySocket
              DisplaySocketExists = displaySocketExists
              SessionBus = sessionBus
              FallbackRuntimeDirectory = Some fallback
              FallbackIsFullDesktopSession = false
              DiagnosticClass = diagnosticClass
              Message = message }

        let unsupportedHostReasons () =
            let reasons = ResizeArray<string>()

            if not (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()) then
                reasons.Add($"persistent windows are unsupported on {Environment.OSVersion.Platform}")

            if OperatingSystem.IsLinux() then
                let diagnostic = desktopSessionDiagnostic()

                if diagnostic.DiagnosticClass = "unsupported-host" then
                    reasons.Add(diagnostic.Message)

            List.ofSeq reasons

        let runtimeCapability () =
            let unsupportedReasons = unsupportedHostReasons ()

            { PersistentWindow = List.isEmpty unsupportedReasons
              BoundedSmoke = true
              KeyboardInput = true
              // Name the backend that actually initializes (single source of truth), not a
              // guessed label — this host always presents through OpenGL (#135).
              RendererMode = Host.GlHost.backendLabel
              UnsupportedHostReasons = unsupportedReasons
              MissingPackageCapabilities = [] }
