namespace FS.Skia.UI.Controls.Typed

open FS.Skia.UI.Controls

type SplitButtonItem = { Key: string; Label: string }

type ToggleButtonProps<'msg> =
    { Id: ControlId option
      Text: string
      IsOn: bool
      Enabled: bool
      OnToggle: (bool -> 'msg) option }

type SplitButtonProps<'msg> =
    { Id: ControlId option
      Text: string
      Enabled: bool
      IsOpen: bool
      Items: SplitButtonItem list
      OnClick: 'msg option
      OnSelected: (string -> 'msg) option }

// The button-family controls are typed-first COMPOSITIONS of existing legacy builders
// (no new StandardControlKind variant, FR-004). Key application and the shared
// accessibility-metadata builder live once in the internal WidgetLowering module.

module ToggleButton =
    let defaults: ToggleButtonProps<'msg> =
        { Id = None
          Text = ""
          IsOn = false
          Enabled = true
          OnToggle = None }

    let view (props: ToggleButtonProps<'msg>) : Widget<'msg> =
        let attrs =
            [ yield FS.Skia.UI.Controls.Button.text props.Text
              yield FS.Skia.UI.Controls.Button.enabled props.Enabled
              yield Attr.selected props.IsOn
              match props.OnToggle with
              | Some map -> yield FS.Skia.UI.Controls.Button.onClick (map (not props.IsOn))
              | None -> ()
              yield WidgetLowering.a11y AccessibilityRole.Button "Toggle button" [ "Tab"; "Shift+Tab" ] ]

        FS.Skia.UI.Controls.Button.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module SplitButton =
    let defaults: SplitButtonProps<'msg> =
        { Id = None
          Text = ""
          Enabled = true
          IsOpen = false
          Items = []
          OnClick = None
          OnSelected = None }

    let view (props: SplitButtonProps<'msg>) : Widget<'msg> =
        let primary =
            FS.Skia.UI.Controls.Button.create
                [ yield FS.Skia.UI.Controls.Button.text props.Text
                  yield FS.Skia.UI.Controls.Button.enabled props.Enabled
                  match props.OnClick with
                  | Some msg -> yield FS.Skia.UI.Controls.Button.onClick msg
                  | None -> () ]

        let trigger =
            FS.Skia.UI.Controls.Button.create
                [ FS.Skia.UI.Controls.Button.text "More"
                  FS.Skia.UI.Controls.Button.enabled props.Enabled ]

        let menu =
            FS.Skia.UI.Controls.Menu.create
                [ yield FS.Skia.UI.Controls.Menu.items (props.Items |> List.map (fun item -> item.Label))
                  match props.OnSelected with
                  | Some map -> yield FS.Skia.UI.Controls.Menu.onSelected map
                  | None -> () ]

        // Popup visibility is product-owned via `IsOpen`; the overlay is always present so
        // node counts stay stable across open/closed states (FR-009 stable node counts).
        let overlay =
            FS.Skia.UI.Controls.Overlay.create
                [ FS.Skia.UI.Controls.Overlay.child menu
                  Attr.selected props.IsOpen ]

        FS.Skia.UI.Controls.Toolbar.create
            [ FS.Skia.UI.Controls.Toolbar.children [ primary; trigger; overlay ]
              WidgetLowering.a11y AccessibilityRole.Menu "Split button" [ "ArrowDown"; "ArrowUp"; "Tab" ] ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl
