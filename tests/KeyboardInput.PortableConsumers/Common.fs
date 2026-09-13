module PortableInputProfile

open System
open FS.GG.UI.KeyboardInput

let run () =
    let none = CommandInput.noModifiers
    let altGraph = { none with Ctrl = true; Alt = true; AltGraph = true }
    let chord identity modifiers = InputGesture.KeyChord(identity, modifiers)
    let context = { Id = "editor"; Priority = 40; Exclusive = false; Overlaps = [] }
    let descriptor =
        { Id = "editor.commit"
          Label = "Commit edit"
          Contexts = [ context.Id ]
          AvailabilityKey = Some "selection.valid"
          Trigger = CommandTriggerPolicy.OncePerPress
          Argument = CommandArgumentPolicy.NoArgument
          Alternatives = [ CommandAlternative.Palette; CommandAlternative.Pointer "Commit" ] }
    let profile =
        { Schema = CommandInput.profileSchema
          Id = "portable-authoring"
          Defaults =
            [ { Context = context.Id
                Command = descriptor.Id
                Gesture = chord (InputKeyIdentity.LogicalKey "é") altGraph } ]
          Overrides =
            [ InputBindingOverride.ReplaceCommand(
                descriptor.Id,
                [ { Context = context.Id
                    Command = descriptor.Id
                    Gesture = chord (InputKeyIdentity.PhysicalCode "KeyE") none } ])
              InputBindingOverride.AddAlias
                { Context = context.Id
                  Command = descriptor.Id
                  Gesture = InputGesture.Touch "double-tap" } ] }
    let catalog =
        { Contexts = [ context ]
          Commands = [ descriptor ]
          ReservedGestures = []
          AllowTerminalPrefixes = false }
    match CommandInput.compile catalog profile, InputProfileCodec.decode (InputProfileCodec.encode profile) with
    | Ok effective, Ok decoded when decoded = profile ->
        printfn "%s" (Convert.ToBase64String(InputProfileCodec.encode decoded))
        effective.Bindings |> List.iter (CommandInput.gestureId << _.Gesture >> printfn "%s")
    | compiled, decoded -> failwithf "portable input profile failed: %A %A" compiled decoded
