namespace FS.GG.UI.SkiaViewer

module internal DiagnosticsFiltering =
        let levelRank level =
            match level with
            | ViewerDiagnosticLevel.Error -> 0
            | ViewerDiagnosticLevel.Warning -> 1
            | ViewerDiagnosticLevel.Info -> 2
            | ViewerDiagnosticLevel.Debug -> 3
            | ViewerDiagnosticLevel.Trace -> 4

        let frameAllowed options (diagnostic: ViewerDiagnosticEvent) =
            match diagnostic.Category, options.FrameLogLimit, diagnostic.FrameIndex with
            | ViewerDiagnosticCategory.Frame, Some limit, Some frameIndex -> limit > 0 && frameIndex <= limit
            | ViewerDiagnosticCategory.Frame, Some limit, None -> limit <> 0
            | ViewerDiagnosticCategory.Frame, None, _ -> true
            | _ -> true

        let shouldCapture options (diagnostic: ViewerDiagnosticEvent) =
            let categoryAllowed =
                options.Verbose
                || Set.isEmpty options.Categories
                || Set.contains diagnostic.Category options.Categories

            levelRank diagnostic.Level <= levelRank options.MinimumLevel
            && categoryAllowed
            && frameAllowed options diagnostic

        let capture options (diagnostic: ViewerDiagnosticEvent) =
            if shouldCapture options diagnostic then
                options.Sink |> Option.iter (fun sink -> sink diagnostic)
                Some diagnostic
            else
                None
