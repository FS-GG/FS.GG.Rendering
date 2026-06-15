namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

type ButtonIntent =
    | Primary
    | Secondary
    | Danger
    | Ghost

type StackOrientation =
    | Vertical
    | Horizontal

type TextBlockProps<'msg> =
    { Id: ControlId option
      Text: string }

type ButtonProps<'msg> =
    { Id: ControlId option
      Text: string
      Enabled: bool
      Intent: ButtonIntent
      Classes: StyleClass list
      Leading: Widget<'msg> option
      Trailing: Widget<'msg> option
      OnClick: 'msg option }

type CheckBoxProps<'msg> =
    { Id: ControlId option
      Text: string
      Checked: bool
      Classes: StyleClass list
      OnChanged: (bool -> 'msg) option }

type StackProps<'msg> =
    { Id: ControlId option
      Orientation: StackOrientation
      Spacing: float
      Children: Widget<'msg> list }

// File-private helpers for lowering. The typed `view` calls the exact same
// legacy string-keyed builders, so the lowered IR is structurally equal to the
// legacy authoring call by construction (FR-004, SC-002). Hidden from the public
// surface by absence from Primitives.fsi (Principle II).
module LegacyControls =
    let intentStyle intent =
        match intent with
        | Primary -> "primary"
        | Secondary -> "secondary"
        | Danger -> "danger"
        | Ghost -> "ghost"

    let orientationName orientation =
        match orientation with
        | Vertical -> "vertical"
        | Horizontal -> "horizontal"

    let spacingAttr (spacing: float) : Attr<'msg> =
        Attr.create "spacing" Layout (FloatValue spacing)

    let orientationAttr orientation : Attr<'msg> =
        Attr.create "orientation" Layout (TextValue(orientationName orientation))

module TextBlock =
    let defaults: TextBlockProps<'msg> = { Id = None; Text = "" }

    let view (props: TextBlockProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.TextBlock.create [ FS.GG.UI.Controls.TextBlock.text props.Text ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Button =
    let defaults: ButtonProps<'msg> =
        { Id = None
          Text = ""
          Enabled = true
          Intent = Primary
          Classes = []
          Leading = None
          Trailing = None
          OnClick = None }

    let view (props: ButtonProps<'msg>) : Widget<'msg> =
        // Feature 095 (E5): the ordered (region-name, fill) pairs for the slots the consumer filled.
        // `None` everywhere ⇒ `[]` ⇒ no slot attribute ⇒ `lowerSlots` is a no-op ⇒ byte-identical.
        let slots =
            [ match props.Leading with
              | Some w -> yield "leading", Widget.toControl w
              | None -> ()
              match props.Trailing with
              | Some w -> yield "trailing", Widget.toControl w
              | None -> () ]

        let attrs =
            [ yield FS.GG.UI.Controls.Button.text props.Text
              yield FS.GG.UI.Controls.Button.enabled props.Enabled
              yield Attr.style (LegacyControls.intentStyle props.Intent)
              // Feature 093 (E3): `Classes = []` lowers to NO style attribute (byte-identical to
              // the pre-feature lowering, A1); a non-empty list attaches the ordered classes.
              match props.Classes with
              | [] -> ()
              | classes -> yield Attr.styleClasses classes
              // Feature 095 (E5): no slot filled ⇒ no slot attribute (byte-identical); otherwise
              // the internal carrier transports the fills into `Children` via `lowerSlots` below.
              match slots with
              | [] -> ()
              | fills -> yield ControlInternals.slotFill fills
              match props.OnClick with
              | Some msg -> yield FS.GG.UI.Controls.Button.onClick msg
              | None -> () ]

        FS.GG.UI.Controls.Button.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> ControlInternals.lowerSlots
        |> Widget.ofControl

module CheckBox =
    let defaults: CheckBoxProps<'msg> =
        { Id = None
          Text = ""
          Checked = false
          Classes = []
          OnChanged = None }

    let view (props: CheckBoxProps<'msg>) : Widget<'msg> =
        let attrs =
            [ yield FS.GG.UI.Controls.CheckBox.text props.Text
              yield FS.GG.UI.Controls.CheckBox.checked' props.Checked
              // Feature 093 (E3): `Classes = []` lowers to NO style attribute (byte-identical, A1).
              match props.Classes with
              | [] -> ()
              | classes -> yield Attr.styleClasses classes
              match props.OnChanged with
              | Some map -> yield FS.GG.UI.Controls.CheckBox.onChanged map
              | None -> () ]

        FS.GG.UI.Controls.CheckBox.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Stack =
    let defaults: StackProps<'msg> =
        { Id = None
          Orientation = Vertical
          Spacing = 0.0
          Children = [] }

    let view (props: StackProps<'msg>) : Widget<'msg> =
        let children = props.Children |> List.map Widget.toControl

        let attrs =
            [ LegacyControls.orientationAttr props.Orientation
              LegacyControls.spacingAttr props.Spacing
              FS.GG.UI.Controls.Stack.children children ]

        FS.GG.UI.Controls.Stack.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl
