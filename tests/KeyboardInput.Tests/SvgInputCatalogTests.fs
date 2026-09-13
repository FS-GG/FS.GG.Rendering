module SvgInputCatalogTests

open System.Text
open Expecto
open FS.GG.UI.KeyboardInput

let private mods = CommandInput.noModifiers
let private logical value = InputGesture.KeyChord(InputKeyIdentity.LogicalKey value, mods)
let private physical value = InputGesture.KeyChord(InputKeyIdentity.PhysicalCode value, mods)

let private contexts =
    [ { Id = "workspace"; Priority = 10; Exclusive = false; Overlaps = [ "editor" ] }
      { Id = "editor"; Priority = 10; Exclusive = false; Overlaps = [ "workspace" ] }
      { Id = "modal"; Priority = 100; Exclusive = true; Overlaps = [] } ]

let private command id contexts alternatives =
    { Id = id
      Label = id
      Contexts = contexts
      AvailabilityKey = None
      Trigger = CommandTriggerPolicy.OncePerPress
      Argument = CommandArgumentPolicy.NoArgument
      Alternatives = alternatives }

let private catalog =
    { Contexts = contexts
      Commands =
        [ command "workspace.help" [ "workspace" ] [ CommandAlternative.Palette ]
          command "editor.commit" [ "editor" ] [ CommandAlternative.Palette; CommandAlternative.Pointer "Commit" ]
          command "editor.cancel" [ "editor" ] [ CommandAlternative.Menu ] ]
      ReservedGestures = [ logical "F5" ]
      AllowTerminalPrefixes = false }

let private binding context command gesture =
    { Context = context; Command = command; Gesture = gesture }

let private profile defaults overrides =
    { Schema = CommandInput.profileSchema
      Id = "authoring"
      Defaults = defaults
      Overrides = overrides }

let private errorCodes (result: Result<EffectiveInputProfile, InputProfileDiagnostic list>) =
    match result with
    | Ok _ -> []
    | Error errors -> errors |> List.map _.Code

[<Tests>]
let tests =
    testList "SVG-INPUT-01 portable command catalog" [
        test "ordered replace alias and unbind overrides stay distinct" {
            let input =
                profile
                    [ binding "workspace" "workspace.help" (logical "?")
                      binding "editor" "editor.commit" (logical "Enter")
                      binding "editor" "editor.cancel" (logical "Escape") ]
                    [ InputBindingOverride.ReplaceCommand(
                        "editor.commit",
                        [ binding "editor" "editor.commit" (physical "KeyK") ])
                      InputBindingOverride.AddAlias(binding "editor" "editor.commit" (logical "Enter"))
                      InputBindingOverride.UnbindCommand "editor.cancel" ]

            match CommandInput.compile catalog input with
            | Error errors -> failtestf "expected valid profile, got %A" errors
            | Ok effective ->
                Expect.equal effective.Bindings.Length 3 "replace drops old command bindings, alias adds one, and unbind removes one"
                Expect.contains effective.Bindings (binding "editor" "editor.commit" (physical "KeyK")) "replacement retains physical identity"
                Expect.contains effective.Bindings (binding "editor" "editor.commit" (logical "Enter")) "alias is retained"
                Expect.isFalse (effective.Bindings |> List.exists (fun value -> value.Command = "editor.cancel")) "explicit unbind remains unbound"
        }

        test "legacy keymap migration preserves logical key identity" {
            let old = Keymap.ofBindings [ { Key = "Space"; Command = "editor.commit" } ]
            let migrated = CommandInput.ofKeymap "legacy" "editor" old
            Expect.equal migrated.Schema CommandInput.profileSchema "migration is additive into the new profile"
            Expect.equal migrated.Defaults [ binding "editor" "editor.commit" (logical "Space") ] "old key is not reinterpreted as a physical code"
            Expect.isEmpty migrated.Overrides "migration invents no override"
        }

        test "raw invalid input reports every located defect before construction" {
            let badCatalog =
                { catalog with
                    ReservedGestures = [ logical "F5" ] }
            let input =
                { Schema = "future"
                  Id = ""
                  Defaults =
                    [ binding "missing" "unknown" (logical "Ctrl+K")
                      binding "editor" "editor.commit" (logical "F5") ]
                  Overrides = [ InputBindingOverride.UnbindCommand "missing-command" ] }
            let codes = CommandInput.compile badCatalog input |> errorCodes |> Set.ofList
            [ "unsupported-profile-schema"
              "empty-profile-id"
              "collapsed-modifiers"
              "unknown-binding-command"
              "unknown-binding-context"
              "reserved-gesture"
              "unknown-override-command" ]
            |> List.iter (fun code -> Expect.contains codes code (sprintf "reports %s" code))
        }

        test "equal-priority overlap is ambiguous independent of insertion order" {
            let bindings =
                [ binding "workspace" "workspace.help" (logical "K")
                  binding "editor" "editor.commit" (logical "K") ]
            let forward = CommandInput.compile catalog (profile bindings []) |> errorCodes
            let reverse = CommandInput.compile catalog (profile (List.rev bindings) []) |> errorCodes
            Expect.contains forward "ambiguous-gesture" "forward order refuses ambiguity"
            Expect.contains reverse "ambiguous-gesture" "reverse order refuses ambiguity"
        }

        test "terminal prefixes and undiscoverable timed sequences are refused" {
            let stepK = InputKeyIdentity.LogicalKey "K", mods
            let stepC = InputKeyIdentity.LogicalKey "C", mods
            let noAlternativeCatalog =
                { catalog with
                    Commands = command "workspace.help" [ "workspace" ] [] :: (catalog.Commands |> List.tail) }
            let input =
                profile
                    [ binding "workspace" "workspace.help" (InputGesture.KeyChord stepK)
                      binding "workspace" "workspace.help" (InputGesture.KeySequence [ stepK; stepC ]) ]
                    []
            let codes = CommandInput.compile noAlternativeCatalog input |> errorCodes
            Expect.contains codes "terminal-prefix-conflict" "terminal prefix is explicit policy"
            Expect.contains codes "sequence-needs-alternative" "timed sequence remains discoverable"
        }

        test "codec round-trips physical logical AltGraph and ordered overrides byte-identically" {
            let altGraph = { mods with AltGraph = true; Ctrl = true; Alt = true }
            let input =
                profile
                    [ binding "editor" "editor.commit" (InputGesture.KeyChord(InputKeyIdentity.LogicalKey "é", altGraph)) ]
                    [ InputBindingOverride.ReplaceCommand(
                        "editor.commit",
                        [ binding "editor" "editor.commit" (physical "KeyE") ])
                      InputBindingOverride.AddAlias(binding "editor" "editor.commit" (InputGesture.Touch "double-tap"))
                      InputBindingOverride.UnbindCommand "editor.cancel" ]
            let encoded = InputProfileCodec.encode input
            Expect.equal encoded (InputProfileCodec.encode input) "encoding is deterministic"
            Expect.stringStarts (Encoding.UTF8.GetString encoded) "fsgg.input-profile\t1\nschema\t" "versioned envelope is explicit"
            match InputProfileCodec.decode encoded with
            | Error errors -> failtestf "expected decode, got %A" errors
            | Ok decoded ->
                Expect.equal decoded input "all typed identities and ordered overrides round-trip"
                Expect.equal (InputProfileCodec.encode decoded) encoded "decode/encode is byte-identical"
        }

        test "codec rejects malformed and incomplete envelopes without throwing" {
            match InputProfileCodec.decode (Encoding.UTF8.GetBytes "future\t9\n") with
            | Error [ error ] -> Expect.equal error.Code "unsupported-envelope" "version refusal is explicit"
            | other -> failtestf "unexpected version result %A" other

            match InputProfileCodec.decode (Encoding.UTF8.GetBytes "fsgg.input-profile\t1\nschema\tZnNnZy5pbnB1dC1wcm9maWxlLzE=\nid\tYQ==\n") with
            | Error [ error ] -> Expect.equal error.Code "missing-end" "truncation is explicit"
            | other -> failtestf "unexpected incomplete result %A" other
        }
    ]
