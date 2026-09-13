module SvgWorkspaceCommandsTests

open Expecto
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Controls

let private ctrl = { CommandInput.noModifiers with Ctrl = true }
let private chord key = InputGesture.KeyChord(InputKeyIdentity.LogicalKey key, ctrl)
let private sequence first second = InputGesture.KeySequence [ InputKeyIdentity.LogicalKey first, ctrl; InputKeyIdentity.LogicalKey second, ctrl ]

let private contexts =
    [ { Id = "workspace"; Priority = 0; Exclusive = false; Overlaps = [ "workspace.create" ] }
      { Id = "workspace.create"; Priority = 10; Exclusive = false; Overlaps = [ "workspace" ] } ]

let private command id label availability alternatives =
    { Id = id
      Label = label
      Contexts = [ "workspace.create" ]
      AvailabilityKey = availability
      Trigger = CommandTriggerPolicy.OncePerPress
      Argument = CommandArgumentPolicy.NoArgument
      Alternatives = alternatives }

let private commands =
    [ command "editor.save" "Save" (Some "can-save") [ CommandAlternative.Palette; CommandAlternative.Menu; CommandAlternative.Pointer "Save button" ]
      command "editor.group" "Group" (Some "has-selection") [ CommandAlternative.Palette; CommandAlternative.Pointer "Group button" ]
      command "editor.help" "Help" None [ CommandAlternative.Palette ] ]

let private catalog =
    { Contexts = contexts
      Commands = commands
      ReservedGestures = []
      AllowTerminalPrefixes = true }

let private profile =
    { Schema = CommandInput.profileSchema
      Id = "workspace"
      Defaults =
        [ { Gesture = chord "s"; Command = "editor.save"; Context = "workspace.create" }
          { Gesture = sequence "g" "g"; Command = "editor.group"; Context = "workspace.create" } ]
      Overrides = [] }

[<Tests>]
let tests =
    testList "SVG workspace command composition" [
        test "one projection keeps palette pointer keyboard and help availability equal" {
            let effective = CommandInput.compile catalog profile |> Result.defaultWith (failtestf "%A")
            let rows = SvgWorkspaceCommands.project catalog effective [ "workspace"; "workspace.create" ] (fun command -> command.AvailabilityKey <> Some "has-selection")
            Expect.equal (SvgWorkspaceCommands.paletteRows rows |> List.map _.Command) [ "editor.save"; "editor.help" ] "palette follows availability"
            Expect.equal (SvgWorkspaceCommands.pointerRows rows |> List.map _.Command) [ "editor.save" ] "pointer follows the same availability"
            Expect.equal (SvgWorkspaceCommands.helpRows rows |> List.map _.Command) [ "editor.save"; "editor.help" ] "help follows the same availability"
            Expect.equal rows.Head.Gestures [ chord "s" ] "dispatch gesture is carried by the same row"
            Expect.equal rows.Head.AriaKeyShortcuts (Some "Control+s") "logical chord is representable"
            Expect.equal rows.[1].ShortcutText (Some "Control+g then Control+g") "sequence uses accessible prose"
            Expect.isNone rows.[1].AriaKeyShortcuts "sequence is not mislabeled as one chord"
        }

        test "raw unbound capture previews displacement and applies immediately" {
            let captured = chord "s"
            let preview = SvgWorkspaceCommands.previewRebind catalog profile "editor.group" "workspace.create" captured |> Result.defaultWith (failtestf "%A")
            Expect.equal preview.DisplacedCommands [ "editor.save" ] "displaced command is visible before acceptance"
            Expect.contains preview.CandidateProfile.Overrides (InputBindingOverride.UnbindCommand "editor.save") "a fully displaced command remains explicitly unbound"
            Expect.exists preview.EffectiveProfile.Bindings (fun binding -> binding.Command = "editor.group" && binding.Gesture = captured) "new binding is effective in the returned profile"
            Expect.isFalse (preview.EffectiveProfile.Bindings |> List.exists (fun binding -> binding.Command = "editor.save" && binding.Gesture = captured)) "accepted replacement removes the conflict"
            let bytes = InputProfileCodec.encode preview.CandidateProfile
            let reopened = InputProfileCodec.decode bytes |> Result.bind (CommandInput.compile catalog) |> Result.defaultWith (failtestf "%A")
            Expect.equal reopened preview.EffectiveProfile "accepted profile round-trips"
            let unbound = InputGesture.KeyChord(InputKeyIdentity.LogicalKey "u", CommandInput.noModifiers)
            Expect.isOk (SvgWorkspaceCommands.previewRebind catalog profile "editor.help" "workspace.create" unbound) "lookup does not have to bind a raw captured gesture"
        }

        test "invalid rebind preserves the prior profile value" {
            let before = CommandInput.compile catalog profile |> Result.defaultWith (failtestf "%A")
            let invalid = InputGesture.KeyChord(InputKeyIdentity.LogicalKey "", CommandInput.noModifiers)
            Expect.isError (SvgWorkspaceCommands.previewRebind catalog profile "editor.save" "workspace.create" invalid) "invalid capture refuses"
            let after = CommandInput.compile catalog profile |> Result.defaultWith (failtestf "%A")
            Expect.equal after before "caller-owned accepted profile remains unchanged"
        }
    ]
