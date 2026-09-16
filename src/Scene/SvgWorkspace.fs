namespace FS.GG.UI.Scene

open System
open System.Globalization
open System.Text

[<RequireQualifiedAccess>]
type SvgWorkspaceMode =
    | Create
    | Arrange
    | Play
    | Review

[<RequireQualifiedAccess>]
type SvgWorkspaceDock =
    | Left
    | Right
    | Bottom

[<RequireQualifiedAccess>]
type SvgPanelPlacement =
    | Docked of edge: SvgWorkspaceDock * order: int
    | Floating of x: float * y: float * width: float * height: float
    | Collapsed

type SvgWorkspacePanel =
    {
        Id: string
        Preferred: SvgPanelPlacement
        Effective: SvgPanelPlacement
    }

type SvgWorkspaceLayout =
    {
        Schema: string
        Revision: int
        Panels: SvgWorkspacePanel list
    }

[<RequireQualifiedAccess>]
type SvgWorkspaceOverlay =
    | CommandPalette of restoreFocus: string
    | PossibleInputHelp of restoreFocus: string
    | RebindCommand of command: string * restoreFocus: string

type SvgWorkspaceState =
    {
        Mode: SvgWorkspaceMode
        Layout: SvgWorkspaceLayout
        ViewportWidth: float
        FocusTarget: string
        Overlay: SvgWorkspaceOverlay option
    }

[<RequireQualifiedAccess>]
type SvgWorkspaceMessage =
    | SetMode of SvgWorkspaceMode
    | SetViewportWidth of float
    | SetPanelPlacement of panelId: string * placement: SvgPanelPlacement
    | OpenPalette of restoreFocus: string
    | OpenHelp of restoreFocus: string
    | BeginRebind of command: string * restoreFocus: string
    | CloseOverlay
    | ImportLayout of byte[]

[<RequireQualifiedAccess>]
type SvgWorkspaceEffect =
    | ActiveContextsChanged of string list
    | LayoutChanged of SvgWorkspaceLayout
    | PersistLayout of byte[]
    | RequestFocus of string
    | WorkspaceDiagnostic of code: string * message: string

[<RequireQualifiedAccess>]
module SvgWorkspace =
    let layoutSchema = "fsgg.svg-workspace-layout/1"
    let narrowViewport = 720.0

    let private finite value =
        not (Double.IsNaN value || Double.IsInfinity value)

    let private panel id placement =
        {
            Id = id
            Preferred = placement
            Effective = placement
        }

    let defaultLayout =
        {
            Schema = layoutSchema
            Revision = 0
            Panels =
                [
                    panel "tools" (SvgPanelPlacement.Docked(SvgWorkspaceDock.Left, 0))
                    panel "inspector" (SvgPanelPlacement.Docked(SvgWorkspaceDock.Right, 0))
                    panel "timeline" (SvgPanelPlacement.Docked(SvgWorkspaceDock.Bottom, 0))
                ]
        }

    let private modeContext =
        function
        | SvgWorkspaceMode.Create -> "workspace.create"
        | SvgWorkspaceMode.Arrange -> "workspace.arrange"
        | SvgWorkspaceMode.Play -> "workspace.play"
        | SvgWorkspaceMode.Review -> "workspace.review"

    let activeContexts state =
        [
            yield "workspace"
            yield modeContext state.Mode
            match state.Overlay with
            | Some(SvgWorkspaceOverlay.CommandPalette _) -> yield "workspace.palette"
            | Some(SvgWorkspaceOverlay.PossibleInputHelp _) -> yield "workspace.help"
            | Some(SvgWorkspaceOverlay.RebindCommand _) -> yield "workspace.rebind"
            | None -> ()
        ]

    let private effective width placement =
        match placement with
        | SvgPanelPlacement.Docked(SvgWorkspaceDock.Left, _)
        | SvgPanelPlacement.Docked(SvgWorkspaceDock.Right, _) when width < narrowViewport -> SvgPanelPlacement.Collapsed
        | other -> other

    let private applyViewport width layout =
        { layout with
            Panels =
                layout.Panels
                |> List.map (fun value ->
                    { value with
                        Effective = effective width value.Preferred
                    })
        }

    let init mode viewportWidth =
        let width =
            if finite viewportWidth && viewportWidth >= 0.0 then
                viewportWidth
            else
                0.0

        {
            Mode = mode
            Layout = applyViewport width defaultLayout
            ViewportWidth = width
            FocusTarget = "scene"
            Overlay = None
        }

    let private b64 (value: string) =
        Convert.ToBase64String(Encoding.UTF8.GetBytes value)

    let private unb64 value =
        Encoding.UTF8.GetString(Convert.FromBase64String value)

    let private number (value: float) =
        if value = 0.0 then
            "0"
        else
            let formatted = sprintf "%.12f" value
            let trimmed = formatted.TrimEnd('0').TrimEnd('.')
            if trimmed = "-0" then "0" else trimmed

    let private placementToken =
        function
        | SvgPanelPlacement.Collapsed -> "c"
        | SvgPanelPlacement.Docked(edge, order) ->
            let edgeToken =
                match edge with
                | SvgWorkspaceDock.Left -> "l"
                | SvgWorkspaceDock.Right -> "r"
                | SvgWorkspaceDock.Bottom -> "b"

            $"d,{edgeToken},{order}"
        | SvgPanelPlacement.Floating(x, y, width, height) -> $"f,{number x},{number y},{number width},{number height}"

    let encodeLayout layout =
        let lines =
            [
                yield "fsgg.svg-workspace-layout\t1"
                yield $"schema\t{b64 layout.Schema}"
                yield $"revision\t{layout.Revision}"
                for value in layout.Panels do
                    yield $"panel\t{b64 value.Id}\t{placementToken value.Preferred}"
                yield "end"
            ]

        Encoding.UTF8.GetBytes(String.concat "\n" lines + "\n")

    let private parsePlacement (token: string) =
        let parts = token.Split(',')

        let parseFloat (value: string) =
            match Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture) with
            | true, parsed when finite parsed -> Some parsed
            | _ -> None

        match parts |> Array.toList with
        | [ "c" ] -> Some SvgPanelPlacement.Collapsed
        | [ "d"; edge; order ] ->
            let dock =
                match edge with
                | "l" -> Some SvgWorkspaceDock.Left
                | "r" -> Some SvgWorkspaceDock.Right
                | "b" -> Some SvgWorkspaceDock.Bottom
                | _ -> None

            match dock, Int32.TryParse order with
            | Some value, (true, index) when index >= 0 -> Some(SvgPanelPlacement.Docked(value, index))
            | _ -> None
        | [ "f"; x; y; width; height ] ->
            match parseFloat x, parseFloat y, parseFloat width, parseFloat height with
            | Some px, Some py, Some w, Some h when w > 0.0 && h > 0.0 -> Some(SvgPanelPlacement.Floating(px, py, w, h))
            | _ -> None
        | _ -> None

    let decodeLayout (bytes: byte[]) =
        if bytes.Length > 65536 then
            Error [ "layout-byte-limit" ]
        else
            try
                let lines =
                    Encoding.UTF8.GetString bytes |> fun value -> value.Split('\n') |> Array.toList

                let header = lines |> List.tryHead

                let schemaLineCount =
                    lines |> List.filter (fun line -> line.StartsWith("schema\t")) |> List.length

                let revisionLineCount =
                    lines |> List.filter (fun line -> line.StartsWith("revision\t")) |> List.length

                let endLineCount = lines |> List.filter ((=) "end") |> List.length

                let schema =
                    lines
                    |> List.tryPick (fun (line: string) ->
                        if line.StartsWith("schema\t") then
                            Some(unb64 (line.Substring 7))
                        else
                            None)

                let revision =
                    lines
                    |> List.tryPick (fun (line: string) ->
                        if line.StartsWith("revision\t") then
                            match Int32.TryParse(line.Substring 9) with
                            | true, value -> Some value
                            | _ -> None
                        else
                            None)

                let panels: SvgWorkspacePanel list =
                    lines
                    |> List.choose (fun (line: string) ->
                        let parts = line.Split('\t')

                        if parts.Length = 3 && parts.[0] = "panel" then
                            match parsePlacement parts.[2] with
                            | Some placement ->
                                Some
                                    {
                                        Id = unb64 parts.[1]
                                        Preferred = placement
                                        Effective = placement
                                    }
                            | None -> None
                        else
                            None)

                let panelLineCount =
                    lines |> List.filter (fun line -> line.StartsWith("panel\t")) |> List.length

                let unknownLine =
                    lines
                    |> List.exists (fun line ->
                        line <> ""
                        && line <> "end"
                        && line <> "fsgg.svg-workspace-layout\t1"
                        && not (line.StartsWith("schema\t"))
                        && not (line.StartsWith("revision\t"))
                        && not (line.StartsWith("panel\t")))

                let errors =
                    [
                        if header <> Some "fsgg.svg-workspace-layout\t1" then
                            "unsupported-layout-envelope"
                        if schemaLineCount <> 1 then
                            "invalid-layout-schema-count"
                        if schema <> Some layoutSchema then
                            "unsupported-layout-schema"
                        if revisionLineCount <> 1 then
                            "invalid-layout-revision-count"
                        if revision |> Option.forall (fun value -> value < 0) then
                            "invalid-layout-revision"
                        if panelLineCount <> panels.Length then
                            "invalid-panel-placement"
                        if panels.Length > 32 then
                            "too-many-panels"
                        if
                            panels
                            |> List.exists (fun (value: SvgWorkspacePanel) -> String.IsNullOrWhiteSpace value.Id)
                        then
                            "empty-panel-id"
                        if
                            panels
                            |> List.countBy (fun (value: SvgWorkspacePanel) -> value.Id)
                            |> List.exists (fun (_, count) -> count > 1)
                        then
                            "duplicate-panel-id"
                        if unknownLine then
                            "unknown-layout-entry"
                        if
                            endLineCount <> 1
                            || (lines |> List.filter ((<>) "") |> List.tryLast) <> Some "end"
                        then
                            "invalid-layout-end"
                    ]

                match schema, revision, errors with
                | Some actualSchema, Some actualRevision, [] ->
                    Ok
                        {
                            Schema = actualSchema
                            Revision = actualRevision
                            Panels = panels
                        }
                | _ -> Error errors
            with _ ->
                Error [ "malformed-layout" ]

    let private validPlacement =
        function
        | SvgPanelPlacement.Docked(_, order) -> order >= 0
        | SvgPanelPlacement.Floating(x, y, width, height) ->
            finite x
            && finite y
            && finite width
            && finite height
            && width > 0.0
            && height > 0.0
        | SvgPanelPlacement.Collapsed -> true

    let private validFocus target = String.IsNullOrWhiteSpace target |> not

    let private focusError state =
        state,
        [
            SvgWorkspaceEffect.WorkspaceDiagnostic(
                "invalid-focus-target",
                "Focus targets must be stable non-empty identities."
            )
        ]

    let private overlayFocus =
        function
        | SvgWorkspaceOverlay.CommandPalette _ -> "workspace.command-palette"
        | SvgWorkspaceOverlay.PossibleInputHelp _ -> "workspace.possible-input-help"
        | SvgWorkspaceOverlay.RebindCommand _ -> "workspace.rebind"

    let private opened overlay state =
        let focus = overlayFocus overlay

        let next =
            { state with
                Overlay = Some overlay
                FocusTarget = focus
            }

        next,
        [
            SvgWorkspaceEffect.ActiveContextsChanged(activeContexts next)
            SvgWorkspaceEffect.RequestFocus focus
        ]

    let update message state =
        match message with
        | SvgWorkspaceMessage.SetMode mode ->
            let next = { state with Mode = mode }
            next, [ SvgWorkspaceEffect.ActiveContextsChanged(activeContexts next) ]
        | SvgWorkspaceMessage.SetViewportWidth width when finite width && width >= 0.0 ->
            let next =
                { state with
                    ViewportWidth = width
                    Layout = applyViewport width state.Layout
                }

            next, [ SvgWorkspaceEffect.LayoutChanged next.Layout ]
        | SvgWorkspaceMessage.SetViewportWidth _ ->
            state,
            [
                SvgWorkspaceEffect.WorkspaceDiagnostic(
                    "invalid-viewport",
                    "Viewport width must be finite and non-negative."
                )
            ]
        | SvgWorkspaceMessage.SetPanelPlacement(_, placement) when not (validPlacement placement) ->
            state,
            [
                SvgWorkspaceEffect.WorkspaceDiagnostic(
                    "invalid-panel-placement",
                    "Panel placement must have finite coordinates, positive size and a non-negative dock order."
                )
            ]
        | SvgWorkspaceMessage.SetPanelPlacement(id, placement) ->
            match state.Layout.Panels |> List.tryFind (fun value -> value.Id = id) with
            | None ->
                state,
                [
                    SvgWorkspaceEffect.WorkspaceDiagnostic("unknown-panel", $"Panel '{id}' is not part of this layout.")
                ]
            | Some _ ->
                let layout =
                    { state.Layout with
                        Revision = state.Layout.Revision + 1
                        Panels =
                            state.Layout.Panels
                            |> List.map (fun value ->
                                if value.Id = id then
                                    { value with
                                        Preferred = placement
                                        Effective = effective state.ViewportWidth placement
                                    }
                                else
                                    value)
                    }

                { state with Layout = layout },
                [
                    SvgWorkspaceEffect.LayoutChanged layout
                    SvgWorkspaceEffect.PersistLayout(encodeLayout layout)
                ]
        | SvgWorkspaceMessage.OpenPalette target when validFocus target ->
            opened (SvgWorkspaceOverlay.CommandPalette target) state
        | SvgWorkspaceMessage.OpenHelp target when validFocus target ->
            opened (SvgWorkspaceOverlay.PossibleInputHelp target) state
        | SvgWorkspaceMessage.BeginRebind(command, target) when validFocus command && validFocus target ->
            opened (SvgWorkspaceOverlay.RebindCommand(command, target)) state
        | SvgWorkspaceMessage.OpenPalette _
        | SvgWorkspaceMessage.OpenHelp _
        | SvgWorkspaceMessage.BeginRebind _ -> focusError state
        | SvgWorkspaceMessage.CloseOverlay ->
            match state.Overlay with
            | None -> state, []
            | Some overlay ->
                let target =
                    match overlay with
                    | SvgWorkspaceOverlay.CommandPalette value
                    | SvgWorkspaceOverlay.PossibleInputHelp value
                    | SvgWorkspaceOverlay.RebindCommand(_, value) -> value

                let next =
                    { state with
                        Overlay = None
                        FocusTarget = target
                    }

                next,
                [
                    SvgWorkspaceEffect.ActiveContextsChanged(activeContexts next)
                    SvgWorkspaceEffect.RequestFocus target
                ]
        | SvgWorkspaceMessage.ImportLayout bytes ->
            match decodeLayout bytes with
            | Ok layout ->
                let next =
                    { state with
                        Layout = applyViewport state.ViewportWidth layout
                    }

                next, [ SvgWorkspaceEffect.LayoutChanged next.Layout ]
            | Error errors ->
                state,
                errors
                |> List.map (fun code ->
                    SvgWorkspaceEffect.WorkspaceDiagnostic(code, "The prior workspace layout was preserved."))
