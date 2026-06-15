namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

/// Immutable, compiler-checked authoring surface for a structured grid container.
type GridProps<'msg> =
    { Id: ControlId option
      Children: Widget<'msg> list }

/// Immutable, compiler-checked authoring surface for a docked-region container.
type DockProps<'msg> =
    { Id: ControlId option
      Children: Widget<'msg> list }

/// Immutable, compiler-checked authoring surface for a wrapping container.
type WrapProps<'msg> =
    { Id: ControlId option
      Orientation: StackOrientation
      Spacing: float
      Children: Widget<'msg> list }

/// Immutable, compiler-checked authoring surface for a single-child border.
type BorderProps<'msg> =
    { Id: ControlId option
      Thickness: float
      Padding: float
      Child: Widget<'msg> }

/// Immutable, compiler-checked authoring surface for a general-purpose panel.
/// Feature 095 (E5): `Header` / `Footer` are the two CLOSED, typed chrome-region slots a consumer
/// may fill with their own `Widget<'msg>` to re-skin the container's SHAPE (a custom header band /
/// footer bar around the content). Each defaults `None`, which lowers to NO slot attribute (so an
/// unfilled panel is byte-identical, FR-003). A filled `Header` lands ahead of `Children`, a
/// filled `Footer` after — both inheriting E1–E4 + E2 retained identity by construction (FR-005).
type PanelProps<'msg> =
    { Id: ControlId option
      Header: Widget<'msg> option
      Footer: Widget<'msg> option
      Children: Widget<'msg> list }

/// Immutable, compiler-checked authoring surface for a scrollable viewport. `child`
/// required. `OnChanged = None` lowers to no binding.
type ScrollViewerProps<'msg> =
    { Id: ControlId
      Child: Widget<'msg>
      OnChanged: (float -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a resizable two-region split.
type SplitViewProps<'msg> =
    { Id: ControlId option
      Orientation: StackOrientation
      Children: Widget<'msg> list
      OnChanged: (float -> 'msg) option }

/// Typed Props front door for the `Grid` control.
module Grid =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: GridProps<'msg>
    /// Lowers children via `Widget.toControl` into `Grid.children`, order preserved.
    val view: props: GridProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Dock` control.
module Dock =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: DockProps<'msg>
    /// Lowers children via `Widget.toControl` into `Dock.children`, order preserved.
    val view: props: DockProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Wrap` control.
module Wrap =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: WrapProps<'msg>
    /// Lowers children via `Widget.toControl` into `Wrap.children`, order preserved.
    val view: props: WrapProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Border` control.
module Border =
    /// Authoring defaults for the given required child; optional fields take their value from here.
    val defaults: child: Widget<'msg> -> BorderProps<'msg>
    /// Lowers its single child via `Widget.toControl` into `Border.child`.
    val view: props: BorderProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Panel` control.
module Panel =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: PanelProps<'msg>
    /// Lowers children via `Widget.toControl` into `Panel.children`, order preserved.
    val view: props: PanelProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `ScrollViewer` control.
module ScrollViewer =
    /// Authoring defaults for the given required `Id` and child.
    val defaults: controlId: ControlId -> child: Widget<'msg> -> ScrollViewerProps<'msg>
    /// Lowers structurally equal to `Control.standard (Custom "scroll-viewer")`.
    val view: props: ScrollViewerProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `SplitView` control.
module SplitView =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: SplitViewProps<'msg>
    /// Lowers structurally equal to `Control.standard (Custom "split-view")`.
    val view: props: SplitViewProps<'msg> -> Widget<'msg>
