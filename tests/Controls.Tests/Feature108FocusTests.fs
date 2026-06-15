module Feature108FocusTests

// Feature 108 (US1) — `Focus.markFocused` stamps `VisualState.Focused` on exactly the focusable
// control whose identity (`Key ?? structural path`) equals the focused id, leaving every other
// control untouched, byte-identical when `None`, and never overriding a consumer-set non-Normal
// state. The in-assembly test IS the user-reachable surface for this pure transition
// ([[fs-gg-ui-widgets]]). Red on the pre-108 build (no `markFocused`).

open Expecto
open FS.GG.UI.Controls

// Last-writer read of a control's VisualState over the PUBLIC attribute surface (mirrors the
// renderer's `visualStateOf`), so the test does not depend on any internal helper.
let private visualStateOf (c: Control<'msg>) : VisualState =
    c.Attributes
    |> List.rev
    |> List.tryPick (fun a ->
        match a.Value with
        | VisualStateValue s -> Some s
        | _ -> None)
    |> Option.defaultValue Normal

// Count controls carrying `Focused` across the whole tree.
let rec private focusedCount (c: Control<'msg>) : int =
    (if visualStateOf c = Focused then 1 else 0)
    + (c.Children |> List.sumBy focusedCount)

let private button () : Control<unit> = Control.create "button" [ Attr.text "B" ]

[<Tests>]
let tests =
    testList "Feature 108 Focus.markFocused (US1, FR-001..005, SC-001/002/012)" [
        test "keyed: exactly one focusable control carries Focused (FR-003)" {
            let a = button () |> Control.withKey "a"
            let b = button () |> Control.withKey "b"
            let root = Control.create "stack" [ Attr.children [ a; b ] ]

            let stamped = Focus.markFocused (Some "a") root
            Expect.equal (focusedCount stamped) 1 "exactly one control is focused"

            let aStamped = stamped.Children |> List.find (fun c -> c.Key = Some "a")
            let bStamped = stamped.Children |> List.find (fun c -> c.Key = Some "b")
            Expect.equal (visualStateOf aStamped) Focused "the targeted keyed control carries the ring"
            Expect.equal (visualStateOf bStamped) Normal "the other keyed control is untouched"
        }

        test "unkeyed same-kind siblings are distinguished by structural path (FR-002, SC-001/002)" {
            // Two unkeyed buttons under a stack root ("0"): their ids are the paths "0.0" and "0.1".
            let root = Control.create "stack" [ Attr.children [ button (); button () ] ]

            let stamped = Focus.markFocused (Some "0.1") root
            Expect.equal (focusedCount stamped) 1 "exactly one unkeyed sibling is focused"
            Expect.equal (visualStateOf stamped.Children.[1]) Focused "the second (path 0.1) button carries the ring"
            Expect.equal (visualStateOf stamped.Children.[0]) Normal "the first (path 0.0) button is untouched"
        }

        test "an unkeyed root focusable control is reachable by its path \"0\"" {
            let root = button ()
            let stamped = Focus.markFocused (Some "0") root
            Expect.equal (visualStateOf stamped) Focused "the unkeyed root is focused via path 0"
        }

        test "markFocused None returns the tree byte-identical (SC-012)" {
            let root = Control.create "stack" [ Attr.children [ button () |> Control.withKey "a" ] ]
            let result = Focus.markFocused None root
            Expect.equal (sprintf "%A" result) (sprintf "%A" root) "None is a structural no-op"
        }

        test "a focused id naming no control stamps nothing (stale target, no throw)" {
            let root = Control.create "stack" [ Attr.children [ button () |> Control.withKey "a" ] ]
            let stamped = Focus.markFocused (Some "does-not-exist") root
            Expect.equal (focusedCount stamped) 0 "no control is focused for a stale id"
        }

        test "structural / non-focusable elements are never stamped (FR-004)" {
            // Target the stack root itself ("0"): a structural container is non-focusable.
            let root = Control.create "stack" [ Attr.children [ button () |> Control.withKey "a" ] ]
            let stamped = Focus.markFocused (Some "0") root
            Expect.equal (focusedCount stamped) 0 "the structural container is not stamped"
        }

        test "a consumer-set non-Normal state (Disabled) wins over Focused" {
            let a =
                Control.create "button" [ Attr.text "B"; Attr.visualState Disabled ]
                |> Control.withKey "a"

            let root = Control.create "stack" [ Attr.children [ a ] ]
            let stamped = Focus.markFocused (Some "a") root
            let aStamped = stamped.Children |> List.find (fun c -> c.Key = Some "a")
            Expect.equal (visualStateOf aStamped) Disabled "Disabled is preserved; Focused does not override it"
            Expect.equal (focusedCount stamped) 0 "no Focused ring is added to a disabled control"
        }
    ]
