module Feature232IdSchemeTests

// Feature 232 (#44) — unify control-id schemes onto `Key ?? path`. These assert the fix at the pure
// Controls seams (focus ordering, the runtime visual-state / scroll bridges, widget trigger keys, and
// the re-pointed unkeyed-collapse diagnostic). Each fails on the pre-232 `Key ?? Kind` behaviour for
// UNKEYED controls and passes after; keyed controls are the regression guard (unchanged everywhere).

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 240 }
let private emptyModel = fst (ControlRuntime.init ())
let private stateOf (control: Control<'msg>) = ControlInternals.visualStateOf control.Attributes
let private dump (control: Control<'msg>) = sprintf "%A" control

// A focusable, activation-bound control with NO authored Key (the case the bug regressed).
let private focusable: Attr<int> =
    Attr.accessibility (
        Accessibility.metadata
            AccessibilityRole.Button
            "go"
            [ "normal" ]
            None
            (Accessibility.keyboard true [ "Enter"; "Space" ] [])
            None
            None)

let rec private anyNode (pred: Control<'msg> -> bool) (c: Control<'msg>) : bool =
    pred c || (c.Children |> List.exists (anyNode pred))

[<Tests>]
let feature232IdSchemeTests =
    testList "Feature232 unify control-id schemes onto Key ?? path" [

        // ---- US1 (T007 / SC-003) — Focus.order mints Key ?? path; unkeyed siblings are distinct ----
        test "US1 T007: two unkeyed same-kind focusable siblings get DISTINCT path stop ids" {
            let tree: Control<int> =
                Stack.create
                    [ Stack.children
                          [ Button.create [ Button.text "a"; Button.onClick 1; focusable ]
                            Button.create [ Button.text "b"; Button.onClick 2; focusable ] ] ]

            let stopIds = (Focus.order tree).Stops |> List.map (fun s -> s.Control)
            Expect.equal stopIds [ "0.0"; "0.1" ] "unkeyed same-kind siblings key by their distinct positional paths (was both \"button\")"
            Expect.equal (List.distinct stopIds |> List.length) 2 "no focus-stop id collision"
        }

        test "US1 T007: each focus-stop id equals the unified id boundIdsOf mints for that node" {
            let tree: Control<int> =
                Stack.create
                    [ Stack.children
                          [ Button.create [ Button.text "a"; Button.onClick 1; focusable ]
                            Button.create [ Button.text "b"; Button.onClick 2; focusable ] ] ]

            let stopIds = (Focus.order tree).Stops |> List.map (fun s -> s.Control) |> Set.ofList
            let boundIds = ControlInternals.boundIdsOf tree
            Expect.equal stopIds boundIds "Focus.order and eventBindings/boundIds agree on the unified Key ?? path id"
        }

        // ---- US2 (T013 / SC-002 / SC-005) — hover lands on the pointer-resolved node only ----
        test "US2 T013: hover keyed by path stamps ONLY the matching unkeyed sibling" {
            let btn () : Control<int> = Button.create [ Button.text "x" ] // unkeyed, no visualState
            let tree: Control<int> = Stack.create [ Stack.children [ btn (); btn () ] ]

            let hovered = { emptyModel with HoveredControl = Some "0.1" }
            let bridged = ControlRuntime.applyRuntimeVisualState hovered tree

            Expect.equal (stateOf bridged.Children.[1]) Hover "the pointer-resolved (path \"0.1\") unkeyed sibling is stamped Hover"
            Expect.equal (stateOf bridged.Children.[0]) Normal "its unkeyed same-kind sibling stays Normal (no smear)"
            Expect.equal (dump bridged.Children.[0]) (dump (btn ())) "the un-hovered sibling is byte-identical to its un-bridged form"
        }

        test "US2 T013: a Normal-and-unset tree is byte-identical through the bridge (at rest)" {
            let tree: Control<int> =
                Stack.create [ Stack.children [ Button.create [ Button.text "x" ]; Button.create [ Button.text "y" ] ] ]
            Expect.equal (dump (ControlRuntime.applyRuntimeVisualState emptyModel tree)) (dump tree) "at rest the bridge adds no attribute anywhere"
            Expect.equal
                (Control.renderTree theme size (ControlRuntime.applyRuntimeVisualState emptyModel tree)).Scene
                (Control.renderTree theme size tree).Scene
                "at-rest bridged Scene is byte-identical to the un-bridged build"
        }

        // ---- US2 (T015 / SC-002) — an UNKEYED scroll-viewer scrolls (path-keyed offset) ----
        test "US2 T015: applyScrollOffsets shifts an UNKEYED scroll-viewer via its path id" {
            let content = Stack.create [ Stack.orientation "vertical"; Stack.children [ Button.create [ Button.text "row" ] ] ]
            let tree: Control<int> =
                Stack.create [ Stack.children [ Control.create "scroll-viewer" [ Attr.children [ content ]; Attr.height 40.0 ] ] ]

            let scroll = ScrollState.empty |> ScrollState.withExtent 400.0 40.0 |> ScrollState.applyScrollDelta 25.0
            let model = { emptyModel with ScrollOffsets = Map.ofList [ "0.0", scroll ] } // "0.0" = the scroll-viewer's path
            let stamped = ControlRuntime.applyScrollOffsets model tree

            // On pre-232 (`Key ?? Kind`) the unkeyed scroll-viewer's id was "scroll-viewer" while the
            // model is path-keyed ("0.0"), so nothing matched and the tree was unchanged. After 232 the
            // path-keyed offset is applied, so the stamped tree differs from the un-stamped input.
            Expect.notEqual (dump stamped) (dump tree) "the unkeyed scroll-viewer received its path-keyed scroll offset"
        }

        // ---- US3 (T021 / SC-004) — transient widgets carry the trigger id they declare ----
        test "US3 T021: DatePicker keys its trigger with the declared triggerId (overlay anchor resolves)" {
            let dp = DatePicker.view { DatePicker.defaults with Id = Some "d"; IsOpen = true } |> Widget.toControl
            Expect.isTrue (anyNode (fun c -> c.Key = Some "d-trigger") dp) "a real lowered control carries the declared trigger id \"d-trigger\""
        }

        test "US3 T021: SplitButton keys its trigger with the declared triggerId" {
            let sb =
                SplitButton.view { SplitButton.defaults with Id = Some "s"; Text = "Save"; IsOpen = true }
                |> Widget.toControl
            Expect.isTrue (anyNode (fun c -> c.Key = Some "s-trigger") sb) "a real lowered control carries the declared trigger id \"s-trigger\""
        }

        // ---- US4 (T026) — the unkeyed-collapse diagnostic describes the unified scheme ----
        test "US4 T026: the unkeyed same-kind diagnostic fires and references the unified Key ?? path scheme" {
            let root: Control<int> =
                Stack.create
                    [ Stack.children
                          [ Button.create [ Button.text "a"; Button.onClick 1 ]
                            Button.create [ Button.text "b"; Button.onClick 2 ] ] ]

            let findings = Diagnostics.unkeyedInteractiveSiblings root
            Expect.isNonEmpty findings "two unkeyed interactive same-kind siblings still warn"
            let msg = (List.head findings).Message
            Expect.isTrue (msg.Contains "Key ?? path") "the guidance references the unified Key ?? path scheme"
            Expect.isTrue (msg.Contains "Control.withKey") "the guidance keeps the withKey remediation"
        }
    ]
