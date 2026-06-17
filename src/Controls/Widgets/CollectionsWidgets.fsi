namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

/// Immutable, compiler-checked authoring surface for a bounded list view. `items`
/// required. Reuses the existing `Collections` model — no parallel state type
/// (FR-004/SC-003). `OnSelected = None` lowers to no binding.
type ListViewProps<'msg> =
    { Id: ControlId
      Items: string list
      OnSelected: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a single-selection list box.
type ListBoxProps<'msg> =
    { Id: ControlId
      Items: string list
      OnSelected: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a multi-selection list.
type MultiSelectListProps<'msg> =
    { Id: ControlId
      Items: string list
      OnChanged: (string list -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a compact combo box.
type ComboBoxProps<'msg> =
    { Id: ControlId
      Items: string list
      OnChanged: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a hierarchical tree view.
type TreeViewProps<'msg> =
    { Id: ControlId
      Items: string list
      OnSelected: (string -> 'msg) option }

/// Typed Props front door for the `ListView` control.
module ListView =
    /// Authoring defaults for the given required `Id`.
    val defaults: controlId: ControlId -> ListViewProps<'msg>
    /// Delegates to `Collections.init` — initial model + effects equal the existing control.
    val init: props: ListViewProps<'msg> -> CollectionModel * CollectionEffect list
    /// Delegates to `Collections.update` — pure transition, no I/O.
    val update: msg: CollectionMsg -> model: CollectionModel -> CollectionModel * CollectionEffect list
    /// Lowers structurally equal to `Control.standard (Custom "list-view")` for the current model state.
    val view: props: ListViewProps<'msg> -> model: CollectionModel -> Widget<'msg>

/// Typed Props front door for the `ListBox` control.
module ListBox =
    /// Authoring defaults for the given required `Id`.
    val defaults: controlId: ControlId -> ListBoxProps<'msg>
    /// Delegates to `Collections.init`.
    val init: props: ListBoxProps<'msg> -> CollectionModel * CollectionEffect list
    /// Delegates to `Collections.update`.
    val update: msg: CollectionMsg -> model: CollectionModel -> CollectionModel * CollectionEffect list
    /// Lowers structurally equal to `Control.standard (Custom "list-box")` for the current model state.
    val view: props: ListBoxProps<'msg> -> model: CollectionModel -> Widget<'msg>

/// Typed Props front door for the `MultiSelectList` control.
module MultiSelectList =
    /// Authoring defaults for the given required `Id`.
    val defaults: controlId: ControlId -> MultiSelectListProps<'msg>
    /// Delegates to `Collections.init`.
    val init: props: MultiSelectListProps<'msg> -> CollectionModel * CollectionEffect list
    /// Delegates to `Collections.update`.
    val update: msg: CollectionMsg -> model: CollectionModel -> CollectionModel * CollectionEffect list
    /// Lowers structurally equal to `Control.standard (Custom "multi-select-list")` for the current model state.
    val view: props: MultiSelectListProps<'msg> -> model: CollectionModel -> Widget<'msg>

/// Typed Props front door for the `ComboBox` control.
module ComboBox =
    /// Authoring defaults for the given required `Id`.
    val defaults: controlId: ControlId -> ComboBoxProps<'msg>
    /// Delegates to `Collections.init`.
    val init: props: ComboBoxProps<'msg> -> CollectionModel * CollectionEffect list
    /// Delegates to `Collections.update`.
    val update: msg: CollectionMsg -> model: CollectionModel -> CollectionModel * CollectionEffect list
    /// Lowers structurally equal to `Control.standard (Custom "combo-box")`
    /// for the current product-owned model state and carries transient dropdown
    /// metadata for overlay routing.
    val view: props: ComboBoxProps<'msg> -> model: CollectionModel -> Widget<'msg>

/// Typed Props front door for the `TreeView` control.
module TreeView =
    /// Authoring defaults for the given required `Id`.
    val defaults: controlId: ControlId -> TreeViewProps<'msg>
    /// Delegates to `Collections.init`.
    val init: props: TreeViewProps<'msg> -> CollectionModel * CollectionEffect list
    /// Delegates to `Collections.update`.
    val update: msg: CollectionMsg -> model: CollectionModel -> CollectionModel * CollectionEffect list
    /// Lowers structurally equal to `Control.standard (Custom "tree-view")` for the current model state.
    val view: props: TreeViewProps<'msg> -> model: CollectionModel -> Widget<'msg>
