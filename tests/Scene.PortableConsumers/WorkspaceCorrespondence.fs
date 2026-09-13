module WorkspaceCorrespondence

open FS.GG.UI.Scene

let private placement = function
    | SvgPanelPlacement.Collapsed -> "collapsed"
    | SvgPanelPlacement.Docked(SvgWorkspaceDock.Left, order) -> $"left:{order}"
    | SvgPanelPlacement.Docked(SvgWorkspaceDock.Right, order) -> $"right:{order}"
    | SvgPanelPlacement.Docked(SvgWorkspaceDock.Bottom, order) -> $"bottom:{order}"
    | SvgPanelPlacement.Floating(x, y, width, height) -> $"floating:{x}:{y}:{width}:{height}"

let run () =
    let initial = SvgWorkspace.init SvgWorkspaceMode.Arrange 1200.0
    let narrow, _ = SvgWorkspace.update (SvgWorkspaceMessage.SetViewportWidth 600.0) initial
    let opened, _ = SvgWorkspace.update (SvgWorkspaceMessage.OpenHelp "scene") narrow
    let closed, _ = SvgWorkspace.update SvgWorkspaceMessage.CloseOverlay opened
    let restored = SvgWorkspace.decodeLayout (SvgWorkspace.encodeLayout closed.Layout) |> Result.defaultWith (failwithf "%A")
    [ "contexts=" + (SvgWorkspace.activeContexts closed |> String.concat ",")
      "focus=" + closed.FocusTarget
      "revision=" + string restored.Revision
      yield! closed.Layout.Panels |> List.map (fun panel -> panel.Id + "=" + placement panel.Effective + "/" + placement panel.Preferred) ]
    |> String.concat "\n"
    |> fun value -> value + "\n"
