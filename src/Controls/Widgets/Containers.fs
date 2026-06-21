namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

type GridProps<'msg> =
    { Id: ControlId option
      Children: Widget<'msg> list }

type DockProps<'msg> =
    { Id: ControlId option
      Children: Widget<'msg> list }

type WrapProps<'msg> =
    { Id: ControlId option
      Orientation: StackOrientation
      Spacing: float
      Children: Widget<'msg> list }

type BorderProps<'msg> =
    { Id: ControlId option
      Thickness: float
      Padding: float
      Child: Widget<'msg> }

type PanelProps<'msg> =
    { Id: ControlId option
      Header: Widget<'msg> option
      Footer: Widget<'msg> option
      Children: Widget<'msg> list }

type ScrollViewerProps<'msg> =
    { Id: ControlId
      Child: Widget<'msg>
      OnChanged: (float -> 'msg) option }

type SplitViewProps<'msg> =
    { Id: ControlId option
      Orientation: StackOrientation
      Children: Widget<'msg> list
      OnChanged: (float -> 'msg) option }

// File-private lowering helpers — children/content lower through `Widget.toControl`
// with order preserved (the 065 Stack pattern). Hidden by absence from Containers.fsi.
module ContainerLowering =
    let orientationName orientation =
        match orientation with
        | Vertical -> "vertical"
        | Horizontal -> "horizontal"

    let orientationAttr orientation : Attr<'msg> =
        Attr.create "orientation" Layout (TextValue(orientationName orientation))

    let spacingAttr (spacing: float) : Attr<'msg> =
        Attr.create AttrKeys.LayoutSpacing Layout (FloatValue spacing)

    let onFloat (eventKind: string) (map: float -> 'msg) : Attr<'msg> =
        Attr.onWith eventKind (fun event -> ControlEvent.navValue event |> Option.defaultValue 0.0 |> map)

module Grid =
    let defaults: GridProps<'msg> = { Id = None; Children = [] }

    let view (props: GridProps<'msg>) : Widget<'msg> =
        let children = props.Children |> List.map Widget.toControl

        FS.GG.UI.Controls.Grid.create [ FS.GG.UI.Controls.Grid.children children ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Dock =
    let defaults: DockProps<'msg> = { Id = None; Children = [] }

    let view (props: DockProps<'msg>) : Widget<'msg> =
        let children = props.Children |> List.map Widget.toControl

        FS.GG.UI.Controls.Dock.create [ FS.GG.UI.Controls.Dock.children children ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Wrap =
    let defaults: WrapProps<'msg> =
        { Id = None; Orientation = Horizontal; Spacing = 0.0; Children = [] }

    let view (props: WrapProps<'msg>) : Widget<'msg> =
        let children = props.Children |> List.map Widget.toControl

        let attrs =
            [ ContainerLowering.orientationAttr props.Orientation
              ContainerLowering.spacingAttr props.Spacing
              FS.GG.UI.Controls.Wrap.children children ]

        FS.GG.UI.Controls.Wrap.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Border =
    let defaults (child: Widget<'msg>) : BorderProps<'msg> =
        { Id = None; Thickness = 1.0; Padding = 0.0; Child = child }

    let view (props: BorderProps<'msg>) : Widget<'msg> =
        let attrs =
            [ FS.GG.UI.Controls.Border.child (Widget.toControl props.Child)
              Attr.create "thickness" Layout (FloatValue props.Thickness)
              Attr.padding props.Padding ]

        FS.GG.UI.Controls.Border.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Panel =
    let defaults: PanelProps<'msg> = { Id = None; Header = None; Footer = None; Children = [] }

    let view (props: PanelProps<'msg>) : Widget<'msg> =
        let children = props.Children |> List.map Widget.toControl
        // Feature 095 (E5): the ordered (region-name, fill) pairs for the chrome slots filled.
        // `None` for both ⇒ `[]` ⇒ no slot attribute ⇒ `lowerSlots` is a no-op ⇒ byte-identical.
        let slots =
            [ match props.Header with
              | Some w -> yield "header", Widget.toControl w
              | None -> ()
              match props.Footer with
              | Some w -> yield "footer", Widget.toControl w
              | None -> () ]

        let attrs =
            [ yield FS.GG.UI.Controls.Panel.children children
              match slots with
              | [] -> ()
              | fills -> yield ControlInternals.slotFill fills ]

        FS.GG.UI.Controls.Panel.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> ControlInternals.lowerSlots
        |> Widget.ofControl

module ScrollViewer =
    let defaults (controlId: ControlId) (child: Widget<'msg>) : ScrollViewerProps<'msg> =
        { Id = controlId; Child = child; OnChanged = None }

    let view (props: ScrollViewerProps<'msg>) : Widget<'msg> =
        let attrs =
            [ yield Attr.child (Widget.toControl props.Child)
              match props.OnChanged with
              | Some map -> yield ContainerLowering.onFloat "onChanged" map
              | None -> () ]

        Control.standard (StandardControlKind.Custom "scroll-viewer") attrs
        |> Control.withKey props.Id
        |> Widget.ofControl

module SplitView =
    let defaults: SplitViewProps<'msg> =
        { Id = None; Orientation = Horizontal; Children = []; OnChanged = None }

    let view (props: SplitViewProps<'msg>) : Widget<'msg> =
        let children = props.Children |> List.map Widget.toControl

        let attrs =
            [ yield Attr.children children
              yield ContainerLowering.orientationAttr props.Orientation
              match props.OnChanged with
              | Some map -> yield ContainerLowering.onFloat "onChanged" map
              | None -> () ]

        Control.standard (StandardControlKind.Custom "split-view") attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl
