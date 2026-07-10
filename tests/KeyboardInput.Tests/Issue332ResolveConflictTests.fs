module Issue332ResolveConflictTests

// Issue 332 (epic 330) — the standalone `resolve` entry point and conflict detection (`validate` over
// a built keymap, `validateBindings` over a raw binding list). Red on the pre-332 build (no `resolve`,
// `validate`, or `validateBindings`). Covers resolution hit/miss, the two conflict kinds the acceptance
// names (duplicate-key and shared-command), and the determinism of the emitted diagnostics.

open Expecto
open FS.GG.UI.KeyboardInput

let private binding key command : KeyboardBinding = { Key = key; Command = command }

[<Tests>]
let tests =
    testList "Issue 332 resolve + conflict detection" [
        test "resolve returns the bound command, keymap-first" {
            let keymap = Keymap.ofBindings [ binding "Space" "Jump"; binding "Enter" "Confirm" ]
            Expect.equal (Keymap.resolve keymap "Space") (Some "Jump") "the bound key resolves to its command"
            Expect.equal (Keymap.resolve keymap "Enter") (Some "Confirm") "a second bound key resolves too"
        }

        test "resolve returns None for an unbound key" {
            let keymap = Keymap.ofBindings [ binding "Space" "Jump" ]
            Expect.equal (Keymap.resolve keymap "Escape") None "an unbound key resolves to nothing"
            Expect.equal (Keymap.resolve Keymap.empty "Space") None "nothing resolves in an empty keymap"
        }

        test "resolve agrees with tryFind (arguments flipped)" {
            let keymap = Keymap.ofBindings [ binding "ArrowUp" "MoveUp" ]
            Expect.equal (Keymap.resolve keymap "ArrowUp") (Keymap.tryFind "ArrowUp" keymap) "resolve is tryFind flipped"
        }

        test "validate reports no conflict for a clean keymap" {
            let keymap = Keymap.ofBindings [ binding "Space" "Jump"; binding "Enter" "Confirm" ]
            Expect.isEmpty (Keymap.validate keymap) "distinct keys and commands raise nothing"
        }

        test "validate surfaces a shared-command conflict (many keys -> one command)" {
            let keymap = Keymap.ofBindings [ binding "Space" "Confirm"; binding "Enter" "Confirm" ]

            match Keymap.validate keymap with
            | [ d ] ->
                Expect.equal d.Code "SharedCommandBinding" "the shared-command code is used"
                Expect.equal d.Severity "Info" "shared-command is informational"
                Expect.equal d.Key None "a shared-command conflict is not about a single key"
                Expect.stringContains d.Message "Confirm" "the message names the command"
                Expect.stringContains d.Message "Enter" "the message names each bound key"
                Expect.stringContains d.Message "Space" "the message names each bound key"
            | other -> failtestf "expected exactly one shared-command diagnostic, got %A" other
        }

        test "validate cannot see a duplicate-key conflict — a keymap collapses it" {
            // A built keymap indexes by key, so the duplicate is already gone (last-wins). This is why
            // `validateBindings` exists: duplicate-key conflicts are only visible on the raw list.
            let keymap = Keymap.ofBindings [ binding "Space" "Jump"; binding "Space" "Fire" ]
            Expect.equal (Keymap.count keymap) 1 "the duplicate key already collapsed"
            Expect.isEmpty (Keymap.validate keymap) "no duplicate-key conflict survives into a keymap"
        }

        test "validateBindings surfaces a duplicate-key conflict on the raw list" {
            let bindings = [ binding "Space" "Jump"; binding "Space" "Fire" ]

            match Keymap.validateBindings bindings with
            | [ d ] ->
                Expect.equal d.Code "DuplicateKeyBinding" "the duplicate-key code is used"
                Expect.equal d.Severity "Warning" "losing a binding is a warning"
                Expect.equal d.Key (Some "Space") "the diagnostic points at the conflicting key"
                Expect.stringContains d.Message "Jump" "the message lists the shadowed command"
                Expect.stringContains d.Message "Fire" "the message lists the surviving command"
                Expect.stringContains d.Message "'Fire'" "the message names the last-wins winner"
            | other -> failtestf "expected exactly one duplicate-key diagnostic, got %A" other
        }

        test "validateBindings surfaces a shared-command conflict on the raw list" {
            let bindings = [ binding "Space" "Confirm"; binding "Enter" "Confirm" ]
            let codes = Keymap.validateBindings bindings |> List.map (fun d -> d.Code)
            Expect.equal codes [ "SharedCommandBinding" ] "only the shared-command conflict is raised"
        }

        test "validateBindings reports both kinds, duplicate-key first then shared-command" {
            // Bomb both keys -> one command AND a duplicate key, so both diagnostics fire at once.
            let bindings =
                [ binding "Space" "Fire"
                  binding "Space" "Jump"
                  binding "Enter" "Jump" ]

            let codes = Keymap.validateBindings bindings |> List.map (fun d -> d.Code)
            Expect.equal codes [ "DuplicateKeyBinding"; "SharedCommandBinding" ] "duplicate-key precedes shared-command"
        }

        test "validateBindings is deterministic across binding order" {
            let a = [ binding "Enter" "Confirm"; binding "Space" "Confirm" ]
            let b = [ binding "Space" "Confirm"; binding "Enter" "Confirm" ]
            Expect.equal (Keymap.validateBindings a) (Keymap.validateBindings b) "reordering the input does not reorder diagnostics"
        }

        test "validateBindings and validate are empty on clean input" {
            let bindings = [ binding "Space" "Jump"; binding "Enter" "Confirm" ]
            Expect.isEmpty (Keymap.validateBindings bindings) "no conflict in distinct keys/commands (raw)"
            Expect.isEmpty (Keymap.validate (Keymap.ofBindings bindings)) "no conflict in distinct keys/commands (keymap)"
        }
    ]
