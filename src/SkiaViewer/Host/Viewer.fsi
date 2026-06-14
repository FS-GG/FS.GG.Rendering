namespace FS.Skia.UI.SkiaViewer.Host

open System
open Elmish
open FS.Skia.UI.Scene
// Open the host namespace last so `RenderDiagnostic` resolves to the host's own type, not Scene's.
open FS.Skia.UI.SkiaViewer.Host

/// The Elmish viewer host edge (moved from the FS.Skia.UI monolith with identical function shapes).
module Viewer =
    /// Public contract function exposed by this FS.Skia.UI package.
    val defaultConfiguration: title: string -> initialSize: Size -> ViewerConfiguration

    /// Public contract function exposed by this FS.Skia.UI package.
    val create:
        configuration: ViewerConfiguration ->
        init: (unit -> 'model * Cmd<'msg>) ->
        update: ('msg -> 'model -> 'model * Cmd<'msg>) ->
        view: ('model -> Scene) ->
            ViewerProgram<'model, 'msg>

    /// Public contract function exposed by this FS.Skia.UI package.
    val withSubscription:
        subscription: ('model -> (string list * (Dispatch<'msg> -> IDisposable)) list) ->
        program: ViewerProgram<'model, 'msg> ->
            ViewerProgram<'model, 'msg>

    /// Public contract function exposed by this FS.Skia.UI package.
    val withEventMapping:
        mapper: (ViewerEvent -> 'msg option) ->
        program: ViewerProgram<'model, 'msg> ->
            ViewerProgram<'model, 'msg>

    /// Public contract function exposed by this FS.Skia.UI package.
    val withEffectMapping:
        mapper: ('msg -> ViewerEffect<'msg> option) ->
        program: ViewerProgram<'model, 'msg> ->
            ViewerProgram<'model, 'msg>

    /// Public contract function exposed by this FS.Skia.UI package.
    val run: program: ViewerProgram<'model, 'msg> -> Result<unit, RenderDiagnostic>
