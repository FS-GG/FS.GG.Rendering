module Issue331KeymapTests

// Issue 331 (epic 330) — the `Keymap` mechanism: an immutable key→command value with pure edit ops
// (add / remove / replace / rebind / clear) each returning a new keymap, round-tripping to/from
// `KeyboardBinding list`. Red on the pre-331 build (no `Keymap`). Covers every op, including the
// headline case of rebinding an already-bound key.

open Expecto
open FS.GG.UI.KeyboardInput

let private binding key command : KeyboardBinding = { Key = key; Command = command }

// Order-insensitive comparison: `toBindings` is key-ordered, so sort the expected side too.
let private expectBindings (expected: KeyboardBinding list) (keymap: Keymap) message =
    let sortKey (b: KeyboardBinding) = b.Key
    Expect.equal (Keymap.toBindings keymap) (expected |> List.sortBy sortKey) message

[<Tests>]
let tests =
    testList "Issue 331 keymap value + rebind operations" [
        test "empty has no bindings" {
            Expect.equal (Keymap.count Keymap.empty) 0 "empty keymap is empty"
            Expect.equal (Keymap.toBindings Keymap.empty) [] "empty keymap yields no bindings"
            Expect.equal (Keymap.tryFind "ArrowLeft" Keymap.empty) None "nothing is bound in empty"
        }

        test "ofBindings / toBindings round-trips a binding list (deduped, key-ordered)" {
            let bindings = [ binding "ArrowUp" "MoveUp"; binding "ArrowDown" "MoveDown" ]
            let keymap = Keymap.ofBindings bindings
            Expect.equal (Keymap.count keymap) 2 "both keys retained"
            expectBindings bindings keymap "round-trips back to the same bindings"
        }

        test "ofBindings keeps the LAST binding when a key repeats" {
            let keymap = Keymap.ofBindings [ binding "Space" "Jump"; binding "Space" "Fire" ]
            Expect.equal (Keymap.count keymap) 1 "the duplicate key collapses to one binding"
            Expect.equal (Keymap.tryFind "Space" keymap) (Some "Fire") "the last binding wins"
        }

        test "add binds a fresh key" {
            let keymap = Keymap.empty |> Keymap.add "Enter" "Confirm"
            Expect.equal (Keymap.tryFind "Enter" keymap) (Some "Confirm") "the key is now bound"
        }

        test "add is non-destructive on an already-bound key" {
            let keymap =
                Keymap.empty
                |> Keymap.add "Enter" "Confirm"
                |> Keymap.add "Enter" "Cancel"

            Expect.equal (Keymap.tryFind "Enter" keymap) (Some "Confirm") "the original binding is kept"
            Expect.equal (Keymap.count keymap) 1 "no second binding is created"
        }

        test "remove drops a binding, and is a no-op on an unbound key" {
            let keymap = Keymap.empty |> Keymap.add "Escape" "Back"
            let removed = Keymap.remove "Escape" keymap
            Expect.equal (Keymap.tryFind "Escape" removed) None "the binding is gone"
            Expect.equal (Keymap.remove "F1" keymap) keymap "removing an unbound key changes nothing"
        }

        test "replace updates an existing binding only" {
            let keymap = Keymap.empty |> Keymap.add "Space" "Jump"
            let replaced = Keymap.replace "Space" "Fire" keymap
            Expect.equal (Keymap.tryFind "Space" replaced) (Some "Fire") "the command is updated"
            Expect.equal (Keymap.replace "Tab" "Next" keymap) keymap "replace on an unbound key is a no-op"
        }

        test "rebind an already-bound key replaces its command (the headline op)" {
            let keymap = Keymap.empty |> Keymap.add "ArrowLeft" "MoveLeft"
            let rebound = Keymap.rebind "ArrowLeft" "StrafeLeft" keymap
            Expect.equal (Keymap.tryFind "ArrowLeft" rebound) (Some "StrafeLeft") "the key points at the new command"
            Expect.equal (Keymap.count rebound) 1 "still one binding for the key"
        }

        test "rebind a fresh key adds it (upsert)" {
            let keymap = Keymap.rebind "Digit1" "Weapon1" Keymap.empty
            Expect.equal (Keymap.tryFind "Digit1" keymap) (Some "Weapon1") "a fresh key is added"
        }

        test "assignKey names the key-indexed upsert and preserves another key for the same command" {
            let keymap = Keymap.empty |> Keymap.assignKey "w" "MoveUp" |> Keymap.assignKey "ArrowUp" "MoveUp"
            expectBindings [ binding "w" "MoveUp"; binding "ArrowUp" "MoveUp" ] keymap "both command bindings remain"
        }

        test "replaceCommandBinding removes old command keys and displaces the intended key" {
            let before =
                Keymap.ofBindings
                    [ binding "w" "MoveUp"
                      binding "ArrowUp" "MoveUp"
                      binding "z" "Fire" ]

            let after = before |> Keymap.replaceCommandBinding "MoveUp" "z"
            expectBindings [ binding "z" "MoveUp" ] after "the selected command has exactly one key and the prior owner is displaced"
        }

        test "clear removes every binding" {
            let keymap =
                Keymap.ofBindings [ binding "ArrowUp" "MoveUp"; binding "ArrowDown" "MoveDown" ]
                |> Keymap.clear

            Expect.equal (Keymap.count keymap) 0 "no bindings remain"
            Expect.equal keymap Keymap.empty "clear yields the empty keymap"
        }

        test "the ops never mutate their argument" {
            let original = Keymap.empty |> Keymap.add "Space" "Jump"
            Keymap.rebind "Space" "Fire" original |> ignore
            Keymap.remove "Space" original |> ignore
            Keymap.clear original |> ignore
            Expect.equal (Keymap.tryFind "Space" original) (Some "Jump") "the original keymap is unchanged"
        }

        test "a keymap round-trips through Keyboard.init and resolves a command" {
            let keymap = Keymap.ofBindings [ binding "Space" "Jump" ]
            let model, _ = Keyboard.init (Keymap.toBindings keymap)
            let next, effects = Keyboard.update (KeyboardMsg.KeyDown "Space") model
            Expect.equal next.LastCommand (Some "Jump") "the bound command resolves through the live model"
            Expect.contains effects (CommandResolved "Jump") "the resolved command is emitted as an effect"
        }
    ]
