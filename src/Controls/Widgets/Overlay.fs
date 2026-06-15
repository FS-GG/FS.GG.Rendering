namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

type TooltipProps<'msg> =
    { Id: ControlId option
      Text: string }

type DialogProps<'msg> =
    { Id: ControlId option
      Title: string option
      IsOpen: bool
      Children: Widget<'msg> list
      OnSelected: (string -> 'msg) option }

type ToastProps<'msg> =
    { Id: ControlId option
      Text: string
      Severity: ValidationState }

type OverlayProps<'msg> =
    { Id: ControlId option
      IsOpen: bool
      Child: Widget<'msg> }

// Key application and the string-event adapter live once in the internal WidgetLowering module.

module Tooltip =
    let defaults: TooltipProps<'msg> = { Id = None; Text = "" }

    let view (props: TooltipProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.Tooltip.create [ FS.GG.UI.Controls.Tooltip.text props.Text ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Dialog =
    let defaults: DialogProps<'msg> =
        { Id = None
          Title = None
          IsOpen = false
          Children = []
          OnSelected = None }

    let view (props: DialogProps<'msg>) : Widget<'msg> =
        let children = props.Children |> List.map Widget.toControl

        let attrs =
            [ yield FS.GG.UI.Controls.Dialog.children children
              match props.Title with
              | Some title -> yield Attr.create "title" Content (TextValue title)
              | None -> ()
              yield Attr.selected props.IsOpen
              match props.OnSelected with
              | Some map -> yield WidgetLowering.onString "onSelected" map
              | None -> () ]

        FS.GG.UI.Controls.Dialog.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Toast =
    let defaults: ToastProps<'msg> =
        { Id = None; Text = ""; Severity = Valid }

    let view (props: ToastProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.Toast.create
            [ FS.GG.UI.Controls.Toast.text props.Text
              Attr.validation props.Severity ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Overlay =
    let defaults (child: Widget<'msg>) : OverlayProps<'msg> =
        { Id = None; IsOpen = false; Child = child }

    let view (props: OverlayProps<'msg>) : Widget<'msg> =
        let attrs =
            [ FS.GG.UI.Controls.Overlay.child (Widget.toControl props.Child)
              Attr.selected props.IsOpen ]

        FS.GG.UI.Controls.Overlay.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl
