// See skill: fs-gg-ui-widgets
namespace FS.GG.UI.Controls

/// Core authoring and rendering verbs for `Control<'msg>` — construction, standard/custom
/// lowering, keying, single-control preview `render` and nested `renderTree`.
module Control =
    /// Build a `Control<'msg>` from an arbitrary `ControlKind` and its attribute list — the
    /// general constructor the per-kind `*.create` builders are sugar over.
    val create: kind: ControlKind -> attrs: Attr<'msg> list -> Control<'msg>
    /// Build a control from a `StandardControlKind` (the framework's built-in catalog kinds),
    /// keeping the kind on the typed enum rather than a free-form string.
    val standard: kind: StandardControlKind -> attrs: Attr<'msg> list -> Control<'msg>
    /// Build a consumer-defined control whose kind is a free-form `kind` string, for control
    /// families outside the built-in `StandardControlKind` catalog.
    val customControl: kind: string -> attrs: Attr<'msg> list -> Control<'msg>
    /// Lower a `standard`-kind control into its primitive composition (the expansion the renderer
    /// consumes); a control whose kind needs no expansion is returned unchanged.
    val lowerStandard: control: Control<'msg> -> Control<'msg>
    /// Lower a `customControl` into its primitive composition; the custom-kind counterpart to
    /// `lowerStandard`.
    val lowerCustom: control: Control<'msg> -> Control<'msg>
    /// Stamp a stable identity `key` onto a control so the keyed reconciler tracks it across
    /// sibling-shifting re-renders (the `withKey` anchor read by `nearestAuthored`).
    val withKey: key: ControlId -> control: Control<'msg> -> Control<'msg>
    /// Feature 108 (US5, FR-014): change ONLY the message type of a control. `Kind`, `Key`,
    /// `Content`, `Accessibility`, and the `Children` shape are preserved exactly; every `Attr`'s
    /// `AttrValue` is rewritten so its `'a`-bearing handler (`MessageValue`, `EventValue`) maps
    /// through `f` and its nested controls (`ChildValue`/`ChildrenValue`/`SlotFillsValue`) recurse.
    /// The result lowers structurally equal to a control authored directly in `'b` (SC-007), so a
    /// page authored as a self-contained `Control<PageMsg>` folds into a shell via one
    /// `Control.map PageMsg`. Keys / focus identity survive — only the message type changes.
    val map: f: ('a -> 'b) -> control: Control<'a> -> Control<'b>
    /// Render a SINGLE control to a `ControlRenderResult<'msg>` preview at intrinsic size
    /// (Feature 080); use `renderTree` to lay out and paint nested children.
    val render: theme: Theme -> control: Control<'msg> -> ControlRenderResult<'msg>
    /// Faithfully rasterize a NESTED control tree to a Scene using real Yoga layout and paint
    /// at the given output size (distinct from `render`, the Feature-080 single-control
    /// PREVIEW). Lays out and paints nested containers AND their children at their computed
    /// bounds, so two structurally different trees produce visibly different scenes. The
    /// returned `Layout`/`EventBindings` correlate by `ControlId` for host hit-testing.
    /// Additive: `render` and `Widget.render` are unchanged (FR-001/FR-002/FR-003).
    ///
    /// Feature 091 (behavioral note, signature unchanged): the interactive host loops no
    /// longer call `renderTree` afresh every frame — each next frame is produced by diffing
    /// the next lowered tree against a retained previous tree (`module internal
    /// RetainedRender`) and reusing the unchanged subtrees' cached render fragments. The
    /// per-node measure/paint here is factored into `ControlInternals.evaluateLayout` /
    /// `paintNode`, which the retained path reuses, so a full `renderTree` and the retained
    /// partial render are byte-for-byte identical (FR-005).
    val renderTree:
        theme: Theme -> size: FS.GG.UI.Scene.Size -> control: Control<'msg> -> ControlRenderResult<'msg>
    /// Resolve which rendered control (if any) contains the point (x, y), from the public
    /// `renderTree` result alone. `None` when the point lies in a gap. Layered over
    /// `Layout.hitTestComputed` against the evaluated `Bounds` (FR-012).
    val hitTest: result: ControlRenderResult<'msg> -> x: float -> y: float -> ControlId option
    /// Resolve a structural hit `ControlId` (the id a `PointerInteraction`/`hitTest` carries — a
    /// `Key` for an authored node, else the positional path `renderTree` assigns) to the nearest
    /// ancestor (incl. self) the consumer authored with a `withKey`, as that ancestor's authored
    /// `ControlId`. A click inside a container-keyed composite recovers the container's id (so the
    /// interactive host can route its binding); a directly-keyed leaf resolves to itself. `None`
    /// when no keyed ancestor exists on the hit node's path — the host then falls back to
    /// `MapPointer` with the raw interaction, never inventing an id. Pure/total/deterministic; reads
    /// the `renderTree` layout tree only, no layout-math change (FR-004/FR-004a/FR-005, feature 090).
    val nearestAuthored: result: ControlRenderResult<'msg> -> hit: ControlId -> ControlId option
    /// Collect the `ControlDiagnostic` list a control's tree reports (e.g. authoring issues),
    /// for surfacing in tooling without rendering.
    val diagnostics: control: Control<'msg> -> ControlDiagnostic list
    /// Translate an incoming `ControlEvent` into the `'msg` list a control's bindings emit — the
    /// dispatch step the interactive host runs to feed the MVU update loop.
    val dispatch: event: ControlEvent -> control: Control<'msg> -> 'msg list
    /// Count the total nodes in a control's tree (self plus all descendants); a structural metric
    /// used by tests and tooling.
    val count: control: Control<'msg> -> int

/// Builders for the `TextBlock` control — a multi-line, wrapping run of body text.
module TextBlock =
    /// Build a `TextBlock` from its attributes; pair with `TextBlock.text` for the content. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the displayed text of a `TextBlock` (`Attr` carrying the run of characters to lay out
    /// and wrap).
    val text: string -> Attr<'msg>

/// Builders for the `Label` control — a short, single-line caption, typically naming an
/// adjacent field.
module Label =
    /// Build a `Label` from its attributes; pair with `Label.text` for the caption. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the caption text of a `Label` (`Attr` carrying the single-line string to display).
    val text: string -> Attr<'msg>

/// Builders for the `Image` control — a bitmap displayed from a source reference.
module Image =
    /// Build an `Image` from its attributes; pair with `Image.source` for the bitmap. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the `Image` source (`Attr` carrying the path/URI string the renderer loads the bitmap
    /// from).
    val source: string -> Attr<'msg>

/// Builders for the `Icon` control — a glyph chosen from the icon set by name.
module Icon =
    /// Build an `Icon` from its attributes; pair with `Icon.name` to choose the glyph. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Select which glyph an `Icon` shows (`Attr` carrying the icon-set name to look up).
    val name: string -> Attr<'msg>

/// Builders for the `Separator` control — a thin divider rule between adjacent content.
module Separator =
    /// Build a `Separator` divider from its attributes (takes no content of its own). The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>

/// Builders for the `Badge` control — a small count/status pill overlaid on or beside content.
module Badge =
    /// Build a `Badge` pill from its attributes; pair with `Badge.text` for its label. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the label shown inside a `Badge` (`Attr` carrying the short count/status string).
    val text: string -> Attr<'msg>

/// Builders for the `Button` control — a clickable command surface with a text label.
module Button =
    /// Build a `Button` from its attributes; pair with `Button.text` and `Button.onClick`. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the `Button` label (`Attr` carrying the caption rendered on the command surface).
    val text: string -> Attr<'msg>
    /// Set whether a `Button` is interactive (`Attr`; `false` greys it out and suppresses click
    /// dispatch). Omitted ≡ enabled.
    val enabled: bool -> Attr<'msg>
    /// Emit a fixed `'msg` when the `Button` is clicked (`Button.onClick`); use `onClickWith` when
    /// the message depends on the event.
    val onClick: 'msg -> Attr<'msg>
    /// Emit a `'msg` derived from the `ControlEvent` when the `Button` is clicked — the
    /// event-aware counterpart to `Button.onClick`.
    val onClickWith: (ControlEvent -> 'msg) -> Attr<'msg>

/// Builders for the `IconButton` control — a compact, glyph-only clickable command.
module IconButton =
    /// Build an `IconButton` from its attributes; pair with `IconButton.icon` and `onClick`. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Choose the glyph an `IconButton` shows (`Attr` carrying the icon-set name; the visual
    /// stand-in for a text label).
    val icon: string -> Attr<'msg>
    /// Emit a fixed `'msg` when the `IconButton` is clicked (`IconButton.onClick`).
    val onClick: 'msg -> Attr<'msg>

/// Builders for the `CheckBox` control — a labelled boolean toggle with a tick box.
module CheckBox =
    /// Build a `CheckBox` from its attributes; pair with `CheckBox.checked'` and `onChanged`. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the label beside a `CheckBox` (`Attr` carrying the descriptive caption text).
    val text: string -> Attr<'msg>
    /// Set the checked state of a `CheckBox` (`CheckBox.checked'`; `true` ticks the box). This is a
    /// controlled value — drive it from model state and reconcile via `onChanged`.
    val checked': bool -> Attr<'msg>
    /// Emit a `'msg` carrying the new `bool` when a `CheckBox` is toggled (`CheckBox.onChanged`).
    val onChanged: (bool -> 'msg) -> Attr<'msg>

/// Builders for the `Switch` control — a sliding on/off toggle (the track-and-thumb form of a
/// boolean).
module Switch =
    /// Build a `Switch` from its attributes; pair with `Switch.checked'` and `onChanged`. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the on/off position of a `Switch` (`Switch.checked'`; `true` slides the thumb on). A
    /// controlled value driven from model state and reconciled via `onChanged`.
    val checked': bool -> Attr<'msg>
    /// Emit a `'msg` carrying the new `bool` when a `Switch` is flipped (`Switch.onChanged`).
    val onChanged: (bool -> 'msg) -> Attr<'msg>

/// Builders for the `Slider` control — a draggable thumb selecting a continuous value along a
/// track.
module Slider =
    /// Build a `Slider` from its attributes; pair with `Slider.value` and `onChanged`. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the `Slider` position (`Attr.value`; a `float` over the control's range, default 0–1).
    /// A controlled value driven from model state and reconciled via `onChanged`.
    val value: float -> Attr<'msg>
    /// Emit a `'msg` carrying the new `float` as a `Slider` is dragged (`Slider.onChanged`).
    val onChanged: (float -> 'msg) -> Attr<'msg>

/// Builders for the `NumericInput` control — a typed numeric field, typically with stepper
/// affordances.
module NumericInput =
    /// Build a `NumericInput` from its attributes; pair with `NumericInput.value` and `onChanged`.
    /// The typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the current number in a `NumericInput` (`Attr.value`; a controlled `float` driven from
    /// model state and reconciled via `onChanged`).
    val value: float -> Attr<'msg>
    /// Emit a `'msg` carrying the edited `float` when a `NumericInput` value changes
    /// (`NumericInput.onChanged`).
    val onChanged: (float -> 'msg) -> Attr<'msg>

/// Builders for the `TextBox` control — a single-line editable text field.
module TextBox =
    /// Build a `TextBox` from its attributes; pair with `TextBox.value` and `onChanged`. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the current text in a `TextBox` (`Attr.value`; a controlled `string` driven from model
    /// state and reconciled via `onChanged`).
    val value: string -> Attr<'msg>
    /// Make a `TextBox` display-only (`Attr.readOnly`; `true` shows the value but blocks editing).
    /// Omitted ≡ editable.
    val readOnly: bool -> Attr<'msg>
    /// Attach a `ValidationState` to a `TextBox` so it renders the matching valid/invalid styling
    /// (`Attr.validation`).
    val validation: ValidationState -> Attr<'msg>
    /// Emit a `'msg` carrying the edited `string` on each `TextBox` change (`TextBox.onChanged`).
    val onChanged: (string -> 'msg) -> Attr<'msg>

/// Builders for the `TextArea` control — a multi-line editable text field (the wrapping
/// counterpart to `TextBox`).
module TextArea =
    /// Build a `TextArea` from its attributes; pair with `TextArea.value` and `onChanged`. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the current multi-line text in a `TextArea` (`Attr.value`; a controlled `string`
    /// reconciled via `onChanged`).
    val value: string -> Attr<'msg>
    /// Emit a `'msg` carrying the edited `string` on each `TextArea` change (`TextArea.onChanged`).
    val onChanged: (string -> 'msg) -> Attr<'msg>

/// Builders for the `RadioGroup` control — a set of mutually-exclusive options, one selected at
/// a time.
module RadioGroup =
    /// Build a `RadioGroup` from its attributes; pair with `RadioGroup.items`, `selected` and
    /// `onChanged`. The typed `Props` front door (`FS.GG.UI.Controls.Typed`) is recommended.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the option labels of a `RadioGroup` (`Attr.items`; one radio button per `string` in
    /// the list, in order).
    val items: string list -> Attr<'msg>
    /// Mark which option of a `RadioGroup` is chosen (`Attr.selected`; the `string` must match one
    /// of `items`). A controlled value reconciled via `onChanged`.
    val selected: string -> Attr<'msg>
    /// Emit a `'msg` carrying the newly-chosen option `string` when a `RadioGroup` selection
    /// changes (`RadioGroup.onChanged`).
    val onChanged: (string -> 'msg) -> Attr<'msg>

/// Builders for the `Stack` container — lays its children single-file along one axis (vertical
/// by default; see `Stack.orientation`).
module Stack =
    /// Build a `Stack` container from its attributes; pair with `Stack.children` for content. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the ordered child controls a `Stack` arranges (`Attr.children`).
    val children: Control<'msg> list -> Attr<'msg>
    /// Lay the stack's children along the row axis when value = "horizontal"; any other
    /// value (or omission) keeps the default vertical column (FR-007).
    val orientation: string -> Attr<'msg>

/// Builders for the `Grid` container — arranges its children in a two-dimensional row/column
/// matrix.
module Grid =
    /// Build a `Grid` container from its attributes; pair with `Grid.children` for content. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the child controls a `Grid` places into its cells (`Attr.children`).
    val children: Control<'msg> list -> Attr<'msg>

/// Builders for the `Dock` container — pins its children to the edges, the last filling the
/// remaining centre.
module Dock =
    /// Build a `Dock` container from its attributes; pair with `Dock.children` for content. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the child controls a `Dock` arranges against its edges (`Attr.children`).
    val children: Control<'msg> list -> Attr<'msg>

/// Builders for the `Wrap` container — flows its children along an axis, wrapping to the next
/// line when they overflow.
module Wrap =
    /// Build a `Wrap` container from its attributes; pair with `Wrap.children` for content. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the child controls a `Wrap` flows and line-wraps (`Attr.children`).
    val children: Control<'msg> list -> Attr<'msg>

/// Builders for the `Border` container — wraps a single child in a stroked/padded frame.
module Border =
    /// Build a `Border` from its attributes; pair with `Border.child` for the wrapped content. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the single control a `Border` frames (`Attr.child`; a `Border` holds exactly one
    /// child, unlike the multi-child containers).
    val child: Control<'msg> -> Attr<'msg>

/// Builders for the `Panel` container — a surface grouping child controls, with optional
/// Header/Footer slots (Feature 095).
module Panel =
    /// Build a `Panel` from its attributes; pair with `Panel.children` for content. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the child controls a `Panel` groups on its surface (`Attr.children`).
    val children: Control<'msg> list -> Attr<'msg>

/// Builders for the `ProgressBar` control — a horizontal fill showing completion of a task.
module ProgressBar =
    /// Build a `ProgressBar` from its attributes; pair with `ProgressBar.value` for the fill. The
    /// typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the completion of a `ProgressBar` (`Attr.value`; a `float` fraction, 0 = empty through
    /// 1 = full).
    val value: float -> Attr<'msg>

/// Builders for the `Spinner` control — an indeterminate busy/loading indicator.
module Spinner =
    /// Build a `Spinner` busy indicator from its attributes (no progress value; it animates
    /// indeterminately). The typed `Props` front door (`FS.GG.UI.Controls.Typed`) is recommended.
    val create: Attr<'msg> list -> Control<'msg>

/// Builders for the `ValidationMessage` control — inline error/hint text shown beneath a field.
module ValidationMessage =
    /// Build a `ValidationMessage` from its attributes; pair with `ValidationMessage.text` for the
    /// message. The typed `Props` front door (`FS.GG.UI.Controls.Typed`) is recommended.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the validation text shown to the user (`Attr` carrying the error/hint `string`).
    val text: string -> Attr<'msg>

/// Builders for the `Tabs` control — a row of tab headers selecting one active page.
module Tabs =
    /// Build a `Tabs` strip from its attributes; pair with `Tabs.items`, `selected` and
    /// `onChanged`. The typed `Props` front door (`FS.GG.UI.Controls.Typed`) is recommended.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the tab header labels of a `Tabs` strip (`Attr.items`; one tab per `string`, in
    /// order).
    val items: string list -> Attr<'msg>
    /// Mark which tab of a `Tabs` strip is active (`Attr.selected`; the `string` must match one of
    /// `items`). A controlled value reconciled via `onChanged`.
    val selected: string -> Attr<'msg>
    /// Emit a `'msg` carrying the newly-activated tab `string` when the `Tabs` selection changes
    /// (`Tabs.onChanged`).
    val onChanged: (string -> 'msg) -> Attr<'msg>

/// Builders for the `Menu` control — a list of selectable command entries.
module Menu =
    /// Build a `Menu` from its attributes; pair with `Menu.items` and `onSelected`. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the entry labels of a `Menu` (`Attr.items`; one selectable row per `string`, in
    /// order).
    val items: string list -> Attr<'msg>
    /// Emit a `'msg` carrying the chosen entry `string` when a `Menu` item is selected
    /// (`Menu.onSelected`).
    val onSelected: (string -> 'msg) -> Attr<'msg>

/// Builders for the `Toolbar` container — a horizontal band of command controls (buttons,
/// icons, separators).
module Toolbar =
    /// Build a `Toolbar` from its attributes; pair with `Toolbar.children` for content. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the command controls a `Toolbar` lays out left-to-right (`Attr.children`).
    val children: Control<'msg> list -> Attr<'msg>

/// Builders for the `Tooltip` control — a transient hover hint floating over content.
module Tooltip =
    /// Build a `Tooltip` from its attributes; pair with `Tooltip.text` for the hint. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the hint text a `Tooltip` shows on hover (`Attr` carrying the `string` to float).
    val text: string -> Attr<'msg>

/// Builders for the `Dialog` container — a modal surface holding a focused task's content.
module Dialog =
    /// Build a `Dialog` from its attributes; pair with `Dialog.children` for content. The typed
    /// `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended authoring path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Supply the child controls a `Dialog` hosts in its modal body (`Attr.children`).
    val children: Control<'msg> list -> Attr<'msg>

/// Builders for the `Toast` control — a brief, auto-dismissing notification banner.
module Toast =
    /// Build a `Toast` notification from its attributes; pair with `Toast.text` for the message.
    /// The typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the message a `Toast` displays (`Attr` carrying the short notification `string`).
    val text: string -> Attr<'msg>

/// Builders for the `Overlay` container — a layer drawn above the rest of the UI to host a
/// single child (scrims, popovers).
module Overlay =
    /// Build an `Overlay` from its attributes; pair with `Overlay.child` for the layered content.
    /// The typed `Props` front door (`FS.GG.UI.Controls.Typed`) is the recommended path.
    val create: Attr<'msg> list -> Control<'msg>
    /// Set the single control an `Overlay` floats above the UI (`Attr.child`; an `Overlay` holds
    /// exactly one child).
    val child: Control<'msg> -> Attr<'msg>
