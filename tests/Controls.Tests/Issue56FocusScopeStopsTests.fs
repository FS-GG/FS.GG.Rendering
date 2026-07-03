module Issue56FocusScopeStopsTests

// Issue #56 — the transient-widget focus scope no longer fabricates `surfaceId + "-item-N"`
// stops that no lowered control carries. These are the honest oracle the parity suites lack:
// they read the FocusScope back out of each widget's REAL lowering and assert every stop is an
// id some lowered control actually carries (under the unified `Key ?? path` scheme its NodeId IS
// its key), that `InitialFocus` is the first such stop, and that no `-item-N` phantom survives.

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed

type private Msg =
    | DateChosen of DateOnly
    | ColorChosen of ColorSwatch
    | Picked of string

// Every id a lowered control carries as its authored Key (all of #56's real stops are keyed).
let rec private keysOf (c: Control<'msg>) : Set<ControlId> =
    let here = c.Key |> Option.map Set.singleton |> Option.defaultValue Set.empty
    c.Children |> List.fold (fun acc child -> Set.union acc (keysOf child)) here

let private focusScopeOf (widget: Widget<'msg>) =
    let control = Widget.toControl widget
    match TransientWidget.collect control with
    | metadata :: _ -> control, metadata.FocusScope
    | [] -> failwith "widget lowered no transient-widget metadata"

// Assert the shared invariant for a transient widget: no phantom `-item-N`, every stop is carried
// by a real lowered control, and InitialFocus is the head stop (or None for an empty scope).
let private assertRealStops (label: string) (widget: Widget<'msg>) =
    let control, scope = focusScopeOf widget
    let realKeys = keysOf control

    for stop in scope.Stops do
        Expect.isFalse (stop.Contains "-item-") $"{label}: stop \"{stop}\" is a fabricated -item-N phantom"
        Expect.isTrue (realKeys.Contains stop) $"{label}: stop \"{stop}\" is carried by a real lowered control"

    Expect.equal scope.InitialFocus (List.tryHead scope.Stops) $"{label}: InitialFocus is the first real stop"

let private color r g b : Color = { Red = r; Green = g; Blue = b; Alpha = 255uy }

[<Tests>]
let issue56FocusScopeStopsTests =
    testList "Issue56 transient focus scopes point at real lowered controls" [

        test "DatePicker calendar stops are the real day-button ids (was surfaceId + -item-N)" {
            let widget =
                DatePicker.view
                    { DatePicker.defaults with
                        Id = Some "d"
                        Value = Some(DateOnly(2026, 6, 15))
                        IsOpen = true
                        OnChange = Some DateChosen }

            let _, scope = focusScopeOf widget
            Expect.equal scope.Stops.Length 30 "June ⇒ 30 day stops"
            Expect.equal (List.head scope.Stops) "day-1" "first stop is the first day button"
            assertRealStops "DatePicker" widget
        }

        test "DatePicker with no value declares an honestly empty scope (no phantom stops)" {
            let widget = DatePicker.view { DatePicker.defaults with Id = Some "d"; OnChange = Some DateChosen }
            let _, scope = focusScopeOf widget
            Expect.isEmpty scope.Stops "no value ⇒ empty calendar ⇒ empty scope"
            Expect.isNone scope.InitialFocus "empty scope captures no initial focus"
        }

        test "ColorPicker palette stops are the real swatch ids" {
            let widget =
                ColorPicker.view
                    { ColorPicker.defaults with
                        Id = Some "c"
                        Swatches = [ { Name = "Red"; Color = color 255uy 0uy 0uy }; { Name = "Blue"; Color = color 0uy 0uy 255uy } ]
                        OnSelected = Some ColorChosen }

            let _, scope = focusScopeOf widget
            Expect.equal scope.Stops [ "swatch-Red"; "swatch-Blue" ] "stops are the swatch cell ids"
            assertRealStops "ColorPicker" widget
        }

        test "SplitButton menu is the surface's one real focus stop" {
            let widget =
                SplitButton.view
                    { SplitButton.defaults with
                        Id = Some "s"
                        Text = "Save"
                        IsOpen = true
                        Items = [ { Key = "cut"; Label = "Cut" } ]
                        OnSelected = Some Picked }

            let _, scope = focusScopeOf widget
            Expect.equal scope.Stops [ "s-menu" ] "the keyed menu content is the single real stop"
            assertRealStops "SplitButton" widget
        }

        test "Menu keys its content with the declared surface id (default id, previously unkeyed)" {
            let widget = Menu.view { Menu.defaults with Items = [ "file" ]; OnSelected = Some Picked }
            let _, scope = focusScopeOf widget
            Expect.equal scope.Stops [ "menu" ] "the default-id menu is keyed \"menu\""
            assertRealStops "Menu" widget
        }

        test "ContextMenu keys its content with the declared surface id" {
            let widget = ContextMenu.view { ContextMenu.defaults with Items = [ "copy" ]; OnSelected = Some Picked }
            let _, scope = focusScopeOf widget
            Expect.equal scope.Stops [ "context-menu" ] "the default-id context-menu is keyed \"context-menu\""
            assertRealStops "ContextMenu" widget
        }

        test "no transient widget emits a fabricated -item-N stop" {
            let widgets: Widget<Msg> list =
                [ DatePicker.view { DatePicker.defaults with Id = Some "d"; Value = Some(DateOnly(2026, 6, 15)); OnChange = Some DateChosen }
                  ColorPicker.view { ColorPicker.defaults with Id = Some "c"; Swatches = [ { Name = "Red"; Color = color 255uy 0uy 0uy } ]; OnSelected = Some ColorChosen }
                  SplitButton.view { SplitButton.defaults with Id = Some "s"; Text = "Save"; IsOpen = true; OnSelected = Some Picked }
                  Menu.view { Menu.defaults with Items = [ "file" ]; OnSelected = Some Picked }
                  ContextMenu.view { ContextMenu.defaults with Items = [ "copy" ]; OnSelected = Some Picked } ]

            for widget in widgets do
                let _, scope = focusScopeOf widget
                Expect.isFalse (scope.Stops |> List.exists (fun s -> s.Contains "-item-")) "no -item-N phantom in any transient scope"
        }
    ]
