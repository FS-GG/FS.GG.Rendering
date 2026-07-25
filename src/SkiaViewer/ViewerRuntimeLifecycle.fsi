namespace FS.GG.UI.SkiaViewer

open Silk.NET.Maths
open Silk.NET.Windowing

/// Native-window lifecycle planning behind an injected platform work-area lookup.
module internal ViewerRuntimeLifecycle =
    val applyWindowBehaviorToOptions:
        resolveWorkArea: (unit -> (Vector2D<int> * Vector2D<int>) option) ->
        behavior: ViewerWindowBehaviorRequest ->
        windowOptions: WindowOptions ->
            WindowOptions

    val planRuntimeWindowBehavior:
        resolveWorkArea: (unit -> (Vector2D<int> * Vector2D<int>) option) ->
        behavior: ViewerWindowBehaviorRequest ->
            Host.RuntimeWindowBehavior option * ViewerDiagnosticEvent list
