namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

type ListViewProps<'msg> =
    { Id: ControlId
      Items: string list
      OnSelected: (string -> 'msg) option }

type ListBoxProps<'msg> =
    { Id: ControlId
      Items: string list
      OnSelected: (string -> 'msg) option }

type MultiSelectListProps<'msg> =
    { Id: ControlId
      Items: string list
      OnChanged: (string list -> 'msg) option }

type ComboBoxProps<'msg> =
    { Id: ControlId
      Items: string list
      OnChanged: (string -> 'msg) option }

type TreeViewProps<'msg> =
    { Id: ControlId
      Items: string list
      OnSelected: (string -> 'msg) option }

// File-private lowering helpers. The five selection collections delegate state to
// the SAME existing `Collections` model (FR-004/SC-003) and lower to
// `Control.standard (Custom <id>)` carrying the live model selection + visible
// range. Default row/viewport metrics mirror the 065 DataGrid façade.
module CollectionLowering =
    let rowHeight = 24.0
    let viewportHeight = 240.0

    let initFor (controlId: ControlId) (items: string list) =
        Collections.init controlId items.Length rowHeight viewportHeight

    // The current model selection + visible range, lowered as standard attributes.
    let stateAttrs (model: CollectionModel) : Attr<'msg> list =
        [ Attr.create "selectedKeys" State (StringListValue(model.SelectedKeys |> Set.toList))
          Attr.create "visibleRange" Data (UntypedValue model.VisibleRange) ]

module ListView =
    let defaults (controlId: ControlId) : ListViewProps<'msg> =
        { Id = controlId; Items = []; OnSelected = None }

    let init (props: ListViewProps<'msg>) : CollectionModel * CollectionEffect list =
        CollectionLowering.initFor props.Id props.Items

    let update (msg: CollectionMsg) (model: CollectionModel) : CollectionModel * CollectionEffect list =
        Collections.update msg model

    let view (props: ListViewProps<'msg>) (model: CollectionModel) : Widget<'msg> =
        let attrs =
            [ yield Attr.items props.Items
              yield! CollectionLowering.stateAttrs model
              match props.OnSelected with
              | Some map -> yield WidgetLowering.onString "onSelected" map
              | None -> () ]

        Control.standard (StandardControlKind.Custom "list-view") attrs
        |> Control.withKey props.Id
        |> Widget.ofControl

module ListBox =
    let defaults (controlId: ControlId) : ListBoxProps<'msg> =
        { Id = controlId; Items = []; OnSelected = None }

    let init (props: ListBoxProps<'msg>) : CollectionModel * CollectionEffect list =
        CollectionLowering.initFor props.Id props.Items

    let update (msg: CollectionMsg) (model: CollectionModel) : CollectionModel * CollectionEffect list =
        Collections.update msg model

    let view (props: ListBoxProps<'msg>) (model: CollectionModel) : Widget<'msg> =
        let attrs =
            [ yield Attr.items props.Items
              yield! CollectionLowering.stateAttrs model
              match props.OnSelected with
              | Some map -> yield WidgetLowering.onString "onSelected" map
              | None -> () ]

        Control.standard (StandardControlKind.Custom "list-box") attrs
        |> Control.withKey props.Id
        |> Widget.ofControl

module MultiSelectList =
    let defaults (controlId: ControlId) : MultiSelectListProps<'msg> =
        { Id = controlId; Items = []; OnChanged = None }

    let init (props: MultiSelectListProps<'msg>) : CollectionModel * CollectionEffect list =
        CollectionLowering.initFor props.Id props.Items

    let update (msg: CollectionMsg) (model: CollectionModel) : CollectionModel * CollectionEffect list =
        Collections.update msg model

    let view (props: MultiSelectListProps<'msg>) (model: CollectionModel) : Widget<'msg> =
        let attrs =
            [ yield Attr.items props.Items
              yield! CollectionLowering.stateAttrs model
              match props.OnChanged with
              | Some map -> yield WidgetLowering.onStringList "onChanged" map
              | None -> () ]

        Control.standard (StandardControlKind.Custom "multi-select-list") attrs
        |> Control.withKey props.Id
        |> Widget.ofControl

module ComboBox =
    let defaults (controlId: ControlId) : ComboBoxProps<'msg> =
        { Id = controlId; Items = []; OnChanged = None }

    let init (props: ComboBoxProps<'msg>) : CollectionModel * CollectionEffect list =
        CollectionLowering.initFor props.Id props.Items

    let update (msg: CollectionMsg) (model: CollectionModel) : CollectionModel * CollectionEffect list =
        Collections.update msg model

    let view (props: ComboBoxProps<'msg>) (model: CollectionModel) : Widget<'msg> =
        let attrs =
            [ yield Attr.items props.Items
              yield! CollectionLowering.stateAttrs model
              match props.OnChanged with
              | Some map -> yield WidgetLowering.onString "onChanged" map
              | None -> () ]

        Control.standard (StandardControlKind.Custom "combo-box") attrs
        |> Control.withKey props.Id
        |> Widget.ofControl

module TreeView =
    let defaults (controlId: ControlId) : TreeViewProps<'msg> =
        { Id = controlId; Items = []; OnSelected = None }

    let init (props: TreeViewProps<'msg>) : CollectionModel * CollectionEffect list =
        CollectionLowering.initFor props.Id props.Items

    let update (msg: CollectionMsg) (model: CollectionModel) : CollectionModel * CollectionEffect list =
        Collections.update msg model

    let view (props: TreeViewProps<'msg>) (model: CollectionModel) : Widget<'msg> =
        let attrs =
            [ yield Attr.items props.Items
              yield! CollectionLowering.stateAttrs model
              match props.OnSelected with
              | Some map -> yield WidgetLowering.onString "onSelected" map
              | None -> () ]

        Control.standard (StandardControlKind.Custom "tree-view") attrs
        |> Control.withKey props.Id
        |> Widget.ofControl
