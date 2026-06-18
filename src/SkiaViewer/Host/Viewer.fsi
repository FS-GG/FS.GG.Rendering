namespace FS.GG.UI.SkiaViewer.Host

open System
open Elmish
open FS.GG.UI.Scene
// Open the host namespace last so `RenderDiagnostic` resolves to the host's own type, not Scene's.
open FS.GG.UI.SkiaViewer.Host

/// The Elmish viewer host edge (moved from the FS.GG.UI monolith with identical function shapes).
module Viewer =
    /// Public contract function exposed by this FS.GG.UI package.
    val defaultConfiguration: title: string -> initialSize: Size -> ViewerConfiguration

    /// Public contract function exposed by this FS.GG.UI package.
    val create:
        configuration: ViewerConfiguration ->
        init: (unit -> 'model * Cmd<'msg>) ->
        update: ('msg -> 'model -> 'model * Cmd<'msg>) ->
        view: ('model -> Scene) ->
            ViewerProgram<'model, 'msg>

    /// Public contract function exposed by this FS.GG.UI package.
    val withSubscription:
        subscription: ('model -> (string list * (Dispatch<'msg> -> IDisposable)) list) ->
        program: ViewerProgram<'model, 'msg> ->
            ViewerProgram<'model, 'msg>

    /// Public contract function exposed by this FS.GG.UI package.
    val withEventMapping:
        mapper: (ViewerEvent -> 'msg option) ->
        program: ViewerProgram<'model, 'msg> ->
            ViewerProgram<'model, 'msg>

    /// Public contract function exposed by this FS.GG.UI package.
    val withEffectMapping:
        mapper: ('msg -> ViewerEffect<'msg> option) ->
        program: ViewerProgram<'model, 'msg> ->
            ViewerProgram<'model, 'msg>

    /// Feature 153: whether the viewer program shape can host live proof effects.
    val liveProofInterpreterSupported: program: ViewerProgram<'model, 'msg> -> bool

    /// Public contract function exposed by this FS.GG.UI package.
    val run: program: ViewerProgram<'model, 'msg> -> Result<unit, RenderDiagnostic>
