namespace FS.GG.UI.SkiaViewer

open FS.GG.UI.Scene

module internal WindowBehaviorValidation =
        let optionResult option requested observed status message =
            { Option = option
              Requested = requested
              Observed = observed
              Status = status
              Message = message }

        let validateBehavior (request: ViewerWindowBehaviorRequest) =
            let resize =
                match request.ResizePolicy with
                | Resizable -> optionResult "resize" "resizable" (Some "resizable") Honored "Resize policy can be honored by the viewer host."
                | FixedSize -> optionResult "resize" "fixed-size" (Some "fixed-size") Honored "Fixed-size window policy can be honored by the viewer host."

            let maximize =
                match request.MaximizePolicy with
                | Maximizable -> optionResult "maximize" "maximizable" (Some "maximizable") Honored "Maximize policy can be honored by the viewer host."
                | NotMaximizable -> optionResult "maximize" "not-maximizable" (Some "not-maximizable") Honored "Maximize-disabled policy can be honored by the viewer host."

            let startupState =
                match request.StartupState with
                | ViewerWindowStartupState.Normal -> optionResult "startup-state" "normal" (Some "normal") Honored "Normal startup state can be honored by the viewer host."
                | ViewerWindowStartupState.Maximized -> optionResult "startup-state" "maximized" (Some "maximized") Honored "Maximized startup state can be requested."
                | ViewerWindowStartupState.Minimized -> optionResult "startup-state" "minimized" None UnsupportedOption "Minimized startup is not accepted for visible interactive launch validation."
                | ViewerWindowStartupState.Fullscreen -> optionResult "startup-state" "fullscreen" (Some "fullscreen") Honored "Fullscreen startup can be honored by the viewer host."
                | ViewerWindowStartupState.WindowedFullscreen -> optionResult "startup-state" "windowed-fullscreen" (Some "windowed-fullscreen") Honored "Windowed-fullscreen startup (borderless work-area coverage) can be honored by the viewer host."

            let startupPosition =
                match request.StartupPosition with
                | None -> optionResult "startup-position" "" None UnsupportedOption "No startup position was requested."
                | Some Centered -> optionResult "startup-position" "centered" (Some "centered") Honored "Centered startup can be requested."
                | Some(Coordinates(x, y)) when x < 0 || y < 0 ->
                    optionResult "startup-position" $"{x},{y}" None FailedOption "Startup coordinates must be non-negative."
                | Some(Coordinates(x, y)) ->
                    optionResult "startup-position" $"{x},{y}" (Some $"{x},{y}") Honored "Startup coordinates can be requested."

            let backend =
                match request.BackendPreference with
                | None -> optionResult "backend" "" (Some "default") Degraded "No backend requested; default backend will be selected."
                | Some ViewerBackendPreference.DefaultBackend -> optionResult "backend" "default" (Some "default") Honored "Default backend will be selected."
                | Some ViewerBackendPreference.OpenGL -> optionResult "backend" "opengl" (Some "opengl") Honored "OpenGL backend can be requested."
                | Some ViewerBackendPreference.Vulkan -> optionResult "backend" "vulkan" None UnsupportedOption "Vulkan backend is no longer supported; this viewer host presents through OpenGL (feature 119)."
                | Some ViewerBackendPreference.Software -> optionResult "backend" "software" None UnsupportedOption "Software backend preference is not supported by this viewer host."

            [ resize; maximize; startupState; startupPosition; backend ]

        let validateLaunch (initialSize: Size) request =
            let initialSizeResult =
                if initialSize.Width <= 0 || initialSize.Height <= 0 then
                    optionResult
                        "initial-size"
                        $"{initialSize.Width}x{initialSize.Height}"
                        None
                        FailedOption
                        "Initial window size must be positive before native window creation."
                else
                    optionResult
                        "initial-size"
                        $"{initialSize.Width}x{initialSize.Height}"
                        (Some $"{initialSize.Width}x{initialSize.Height}")
                        Honored
                        "Initial window size is positive and can be requested."

            initialSizeResult :: validateBehavior request
