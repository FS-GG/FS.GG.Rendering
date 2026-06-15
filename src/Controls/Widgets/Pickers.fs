namespace FS.GG.UI.Controls.Typed

open System
open System.Globalization
open FS.GG.UI.Controls
open FS.GG.UI.Scene

type ColorSwatch = { Name: string; Color: Color }

type DatePickerProps<'msg> =
    { Id: ControlId option
      Value: DateOnly option
      Enabled: bool
      IsOpen: bool
      OnChange: (DateOnly -> 'msg) option }

type TimePickerProps<'msg> =
    { Id: ControlId option
      Value: TimeOnly option
      Enabled: bool
      OnChange: (TimeOnly -> 'msg) option }

type ColorPickerProps<'msg> =
    { Id: ControlId option
      Swatches: ColorSwatch list
      Selected: ColorSwatch option
      OnSelected: (ColorSwatch -> 'msg) option }

// File-private lowering helpers. The new picker / date-time controls are typed-first
// The picker / date-time controls are typed-first COMPOSITIONS of existing legacy builders
// (no new StandardControlKind variant and no renderer change, FR-004). Key application and the
// shared accessibility-metadata builder live once in the internal WidgetLowering module.

module DatePicker =
    let defaults: DatePickerProps<'msg> =
        { Id = None
          Value = None
          Enabled = true
          IsOpen = false
          OnChange = None }

    let view (props: DatePickerProps<'msg>) : Widget<'msg> =
        let formatted =
            match props.Value with
            | Some date -> date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            | None -> ""

        let field =
            FS.GG.UI.Controls.TextBox.create
                [ FS.GG.UI.Controls.TextBox.value formatted
                  FS.GG.UI.Controls.TextBox.readOnly true ]

        let trigger =
            FS.GG.UI.Controls.Button.create
                [ FS.GG.UI.Controls.Button.text "Open calendar"
                  FS.GG.UI.Controls.Button.enabled props.Enabled ]

        // The popup calendar shows one day Button per day of the selected month; no
        // selection ⇒ an empty calendar (placeholder field, dispatches nothing).
        let dayButtons =
            match props.Value with
            | Some date ->
                [ for day in 1 .. DateTime.DaysInMonth(date.Year, date.Month) ->
                      let chosen = DateOnly(date.Year, date.Month, day)

                      FS.GG.UI.Controls.Button.create
                          [ yield FS.GG.UI.Controls.Button.text (string day)
                            yield FS.GG.UI.Controls.Button.enabled props.Enabled
                            match props.OnChange with
                            | Some map -> yield FS.GG.UI.Controls.Button.onClick (map chosen)
                            | None -> () ]
                      |> FS.GG.UI.Controls.Control.withKey (sprintf "day-%d" day) ]
            | None -> []

        let calendar =
            FS.GG.UI.Controls.Grid.create [ FS.GG.UI.Controls.Grid.children dayButtons ]

        let overlay =
            FS.GG.UI.Controls.Overlay.create
                [ FS.GG.UI.Controls.Overlay.child calendar
                  Attr.selected props.IsOpen ]

        FS.GG.UI.Controls.Stack.create
            [ FS.GG.UI.Controls.Stack.children [ field; trigger; overlay ]
              WidgetLowering.a11y
                  AccessibilityRole.TextBox
                  "Date picker"
                  [ "ArrowLeft"; "ArrowRight"; "ArrowUp"; "ArrowDown" ] ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module TimePicker =
    let defaults: TimePickerProps<'msg> =
        { Id = None
          Value = None
          Enabled = true
          OnChange = None }

    let view (props: TimePickerProps<'msg>) : Widget<'msg> =
        let segment (key: string) (text: string) (next: TimeOnly option) =
            FS.GG.UI.Controls.Button.create
                [ yield FS.GG.UI.Controls.Button.text text
                  yield FS.GG.UI.Controls.Button.enabled props.Enabled
                  match next, props.OnChange with
                  | Some time, Some map -> yield FS.GG.UI.Controls.Button.onClick (map time)
                  | _ -> () ]
            |> FS.GG.UI.Controls.Control.withKey key

        let hourText, minuteText =
            match props.Value with
            | Some time -> sprintf "%02d" time.Hour, sprintf "%02d" time.Minute
            | None -> "--", "--"

        let hourSegment =
            segment "hour-segment" hourText (props.Value |> Option.map (fun time -> time.AddHours 1.0))

        let minuteSegment =
            segment "minute-segment" minuteText (props.Value |> Option.map (fun time -> time.AddMinutes 1.0))

        let separator =
            FS.GG.UI.Controls.Label.create [ FS.GG.UI.Controls.Label.text ":" ]

        FS.GG.UI.Controls.Stack.create
            [ FS.GG.UI.Controls.Stack.children [ hourSegment; separator; minuteSegment ]
              WidgetLowering.a11y AccessibilityRole.TextBox "Time picker" [ "ArrowUp"; "ArrowDown" ] ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module ColorPicker =
    let defaults: ColorPickerProps<'msg> =
        { Id = None
          Swatches = []
          Selected = None
          OnSelected = None }

    let view (props: ColorPickerProps<'msg>) : Widget<'msg> =
        let cell (swatch: ColorSwatch) =
            FS.GG.UI.Controls.Button.create
                [ yield FS.GG.UI.Controls.Button.text swatch.Name
                  yield Attr.selected (props.Selected = Some swatch)
                  yield Attr.create "color" Style (UntypedValue(swatch.Color :> obj))
                  match props.OnSelected with
                  | Some map -> yield FS.GG.UI.Controls.Button.onClick (map swatch)
                  | None -> () ]
            |> FS.GG.UI.Controls.Control.withKey (sprintf "swatch-%s" swatch.Name)

        FS.GG.UI.Controls.Wrap.create
            [ FS.GG.UI.Controls.Wrap.children (props.Swatches |> List.map cell)
              WidgetLowering.a11y
                  AccessibilityRole.List
                  "Color picker"
                  [ "ArrowLeft"; "ArrowRight"; "ArrowUp"; "ArrowDown" ] ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl
