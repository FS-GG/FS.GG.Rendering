namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

type TabsProps<'msg> =
    { Id: ControlId option
      Items: string list
      SelectedKey: string option
      OnChanged: (string -> 'msg) option }

type MenuProps<'msg> =
    { Id: ControlId option
      Items: string list
      OnSelected: (string -> 'msg) option }

type ContextMenuProps<'msg> =
    { Id: ControlId option
      Items: string list
      OnSelected: (string -> 'msg) option }

type ToolbarProps<'msg> =
    { Id: ControlId option
      Children: Widget<'msg> list
      OnClick: 'msg option }

// `menu` and `context-menu` are distinct per-id modules over the same legacy menu
// mechanic; `context-menu` lowers via `Control.standard (Custom "context-menu")`. Key
// application and the string-event adapter live once in the internal WidgetLowering module.

module Tabs =
    let defaults: TabsProps<'msg> =
        { Id = None; Items = []; SelectedKey = None; OnChanged = None }

    let view (props: TabsProps<'msg>) : Widget<'msg> =
        let attrs =
            [ yield FS.GG.UI.Controls.Tabs.items props.Items
              match props.SelectedKey with
              | Some key -> yield FS.GG.UI.Controls.Tabs.selected key
              | None -> ()
              match props.OnChanged with
              | Some map -> yield FS.GG.UI.Controls.Tabs.onChanged map
              | None -> () ]

        FS.GG.UI.Controls.Tabs.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Menu =
    let defaults: MenuProps<'msg> = { Id = None; Items = []; OnSelected = None }

    let view (props: MenuProps<'msg>) : Widget<'msg> =
        let surfaceId = props.Id |> Option.defaultValue "menu"
        let triggerId = surfaceId + "-trigger"

        let attrs =
            [ yield FS.GG.UI.Controls.Menu.items props.Items
              yield
                  WidgetLowering.transientMetadata
                      TransientSurfaceKind.Menu
                      surfaceId
                      triggerId
                      true
                      true
                      10
                      false
                      (Some "onSelected")
              match props.OnSelected with
              | Some map -> yield FS.GG.UI.Controls.Menu.onSelected map
              | None -> () ]

        FS.GG.UI.Controls.Menu.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module ContextMenu =
    let defaults: ContextMenuProps<'msg> = { Id = None; Items = []; OnSelected = None }

    let view (props: ContextMenuProps<'msg>) : Widget<'msg> =
        let surfaceId = props.Id |> Option.defaultValue "context-menu"
        let triggerId = surfaceId + "-trigger"

        let attrs =
            [ yield Attr.items props.Items
              yield
                  WidgetLowering.transientMetadata
                      TransientSurfaceKind.ContextMenu
                      surfaceId
                      triggerId
                      true
                      true
                      20
                      false
                      (Some "onSelected")
              match props.OnSelected with
              | Some map -> yield WidgetLowering.onString "onSelected" map
              | None -> () ]

        Control.standard (StandardControlKind.Custom "context-menu") attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Toolbar =
    let defaults: ToolbarProps<'msg> = { Id = None; Children = []; OnClick = None }

    let view (props: ToolbarProps<'msg>) : Widget<'msg> =
        let children = props.Children |> List.map Widget.toControl

        let attrs =
            [ yield FS.GG.UI.Controls.Toolbar.children children
              match props.OnClick with
              | Some msg -> yield Attr.on "onClick" msg
              | None -> () ]

        FS.GG.UI.Controls.Toolbar.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl
