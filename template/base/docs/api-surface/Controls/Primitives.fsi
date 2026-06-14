namespace FS.Skia.UI.Controls.Typed

open FS.Skia.UI.Controls

/// Semantic style intent for a button (Variant taxonomy class).
type ButtonIntent =
    | Primary
    | Secondary
    | Danger
    | Ghost

/// Layout orientation for a Stack.
type StackOrientation =
    | Vertical
    | Horizontal

/// Immutable, compiler-checked authoring surface for a text block.
type TextBlockProps<'msg> =
    { Id: ControlId option
      Text: string }

/// Immutable, compiler-checked authoring surface for a button. `OnClick = None`
/// lowers to NO event binding (FR-008 edge case), never a default message.
/// Feature 093 (E3): `Classes` attaches an ordered list of style classes/variants; default `[]`
/// lowers to NO style attribute — byte-identical to the pre-feature front door (A1 additive).
/// Feature 095 (E5): `Leading` / `Trailing` are the two CLOSED, typed slot regions flanking the
/// label — a consumer fills one with their own `Widget<'msg>` (e.g. an icon before the label) to
/// re-skin the button's SHAPE. Each defaults `None`, which lowers to NO slot attribute (so an
/// unfilled button is byte-identical, FR-003). A slot fill is a static `Widget<'msg>`, NOT a
/// data-bound template; filling a region a kind does NOT declare is a compile error — there is no
/// field for it (FR-001, SC-006).
type ButtonProps<'msg> =
    { Id: ControlId option
      Text: string
      Enabled: bool
      Intent: ButtonIntent
      Classes: StyleClass list
      Leading: Widget<'msg> option
      Trailing: Widget<'msg> option
      OnClick: 'msg option }

/// Immutable, compiler-checked authoring surface for a checkbox.
/// Feature 093 (E3): `Classes` attaches an ordered list of style classes/variants; default `[]`
/// lowers to NO style attribute — byte-identical to the pre-feature front door (A1 additive).
type CheckBoxProps<'msg> =
    { Id: ControlId option
      Text: string
      Checked: bool
      Classes: StyleClass list
      OnChanged: (bool -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a stack container.
type StackProps<'msg> =
    { Id: ControlId option
      Orientation: StackOrientation
      Spacing: float
      Children: Widget<'msg> list }

/// Typed Props front door for the `TextBlock` control.
module TextBlock =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: TextBlockProps<'msg>
    /// Lowers structurally equal to `TextBlock.create [ TextBlock.text props.Text ]`.
    val view: props: TextBlockProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Button` control.
module Button =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: ButtonProps<'msg>
    /// Lowers structurally equal to the legacy `Button.create` attrs;
    /// `OnClick = None` lowers to no event binding.
    val view: props: ButtonProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `CheckBox` control.
module CheckBox =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: CheckBoxProps<'msg>
    /// Lowers structurally equal to the legacy `CheckBox.create` attrs;
    /// `OnChanged = None` lowers to no event binding.
    val view: props: CheckBoxProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Stack` control.
module Stack =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: StackProps<'msg>
    /// Lowers children via `Widget.toControl` into `Stack.children`, order preserved.
    val view: props: StackProps<'msg> -> Widget<'msg>
