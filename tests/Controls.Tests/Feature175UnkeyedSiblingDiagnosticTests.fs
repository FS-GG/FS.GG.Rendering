module Feature175UnkeyedSiblingDiagnosticTests

// Feature 175 F4 — the diagnostic for the unkeyed-same-kind-sibling visual-state bleed. Stamping keys
// by `Key ?? Kind`, so unkeyed interactive siblings of the same kind collapse onto one stamp id and
// focus/hover/press marks them all (routing distinguishes them, so the failure is silent). The
// `Diagnostics.unkeyedInteractiveSiblings` analyzer warns so the author keys them.

open Expecto
open FS.GG.UI.Controls

let private button onClick = Button.create [ Button.text "b"; Button.onClick onClick ]
let private keyedButton key onClick = button onClick |> Control.withKey key
let private label text = TextBlock.create [ TextBlock.text text ]
let private row children = Stack.create [ Stack.children children ]

[<Tests>]
let tests =
    testList "Feature175UnkeyedSiblingDiagnostic" [
        test "two UNKEYED interactive same-kind siblings raise one MissingStableKey warning" {
            let tree = row [ button 1; button 2 ]
            let diags = Diagnostics.unkeyedInteractiveSiblings tree
            match diags with
            | [ d ] ->
                Expect.equal d.Code MissingStableKey "the collision is reported as a missing-stable-key warning"
                Expect.equal d.Severity Warning "it is advisory, not blocking"
                Expect.stringContains d.Message "button" "the message names the colliding kind"
            | other -> failtestf "expected exactly one diagnostic, got %A" (other |> List.map (fun d -> d.Code))
        }

        test "KEYED interactive same-kind siblings raise nothing (the fix)" {
            let tree = row [ keyedButton "a" 1; keyedButton "b" 2 ]
            Expect.isEmpty (Diagnostics.unkeyedInteractiveSiblings tree) "distinct keys resolve the collision"
        }

        test "unkeyed NON-interactive same-kind siblings raise nothing (no stamp to smear)" {
            let tree = row [ label "x"; label "y"; label "z" ]
            Expect.isEmpty (Diagnostics.unkeyedInteractiveSiblings tree) "non-interactive siblings carry no visual state"
        }

        test "a single unkeyed interactive control among keyed siblings does not collide" {
            let tree = row [ keyedButton "a" 1; button 2 ]
            Expect.isEmpty (Diagnostics.unkeyedInteractiveSiblings tree) "one unkeyed member alone shares its id with no sibling"
        }

        test "the walk is recursive — a nested unkeyed group is found" {
            let tree = row [ label "header"; row [ button 1; button 2; button 3 ] ]
            match Diagnostics.unkeyedInteractiveSiblings tree with
            | [ d ] -> Expect.stringContains d.Message "3 unkeyed" "the nested group of three is counted"
            | other -> failtestf "expected one nested diagnostic, got %A" (other |> List.map (fun d -> d.Message))
        }
    ]
