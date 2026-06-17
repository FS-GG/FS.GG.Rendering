namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

/// Immutable, compiler-checked authoring surface for a tab strip. `items` required.
type TabsProps<'msg> =
    { Id: ControlId option
      Items: string list
      SelectedKey: string option
      OnChanged: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a command menu. `items` required.
type MenuProps<'msg> =
    { Id: ControlId option
      Items: string list
      OnSelected: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a contextual command menu.
type ContextMenuProps<'msg> =
    { Id: ControlId option
      Items: string list
      OnSelected: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a compact toolbar. `children`
/// required. `OnClick = None` lowers to no binding.
type ToolbarProps<'msg> =
    { Id: ControlId option
      Children: Widget<'msg> list
      OnClick: 'msg option }

/// Typed Props front door for the `Tabs` control.
module Tabs =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: TabsProps<'msg>
    /// Lowers structurally equal to the legacy `Tabs.create` attrs.
    val view: props: TabsProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Menu` control.
module Menu =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: MenuProps<'msg>
    /// Lowers structurally equal to the legacy `Menu.create` attrs and carries
    /// product-owned transient metadata for overlay routing and selection dispatch.
    val view: props: MenuProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `ContextMenu` control.
module ContextMenu =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: ContextMenuProps<'msg>
    /// Lowers structurally equal to `Control.standard (Custom "context-menu")`
    /// and carries product-owned transient metadata for overlay routing.
    val view: props: ContextMenuProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Toolbar` control.
module Toolbar =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: ToolbarProps<'msg>
    /// Lowers children via `Widget.toControl` into `Toolbar.children`, order preserved.
    val view: props: ToolbarProps<'msg> -> Widget<'msg>
