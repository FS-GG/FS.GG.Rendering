module PortableInputProfile

open System
open FS.GG.UI.KeyboardInput

let run () =
    let none = CommandInput.noModifiers

    let altGraph =
        { none with
            Ctrl = true
            Alt = true
            AltGraph = true
        }

    let chord identity modifiers =
        InputGesture.KeyChord(identity, modifiers)

    let context =
        {
            Id = "editor"
            Priority = 40
            Exclusive = false
            Overlaps = []
        }

    let descriptor =
        {
            Id = "editor.commit"
            Label = "Commit edit"
            Contexts = [ context.Id ]
            AvailabilityKey = Some "selection.valid"
            Trigger = CommandTriggerPolicy.OncePerPress
            Argument = CommandArgumentPolicy.NoArgument
            Alternatives = [ CommandAlternative.Palette; CommandAlternative.Pointer "Commit" ]
        }

    let profile =
        {
            Schema = CommandInput.profileSchema
            Id = "portable-authoring"
            Defaults =
                [
                    {
                        Context = context.Id
                        Command = descriptor.Id
                        Gesture = chord (InputKeyIdentity.LogicalKey "é") altGraph
                    }
                ]
            Overrides =
                [
                    InputBindingOverride.ReplaceCommand(
                        descriptor.Id,
                        [
                            {
                                Context = context.Id
                                Command = descriptor.Id
                                Gesture = chord (InputKeyIdentity.PhysicalCode "KeyE") none
                            }
                        ]
                    )
                    InputBindingOverride.AddAlias
                        {
                            Context = context.Id
                            Command = descriptor.Id
                            Gesture = InputGesture.Touch "double-tap"
                        }
                ]
        }

    let catalog =
        {
            Contexts = [ context ]
            Commands = [ descriptor ]
            ReservedGestures = []
            AllowTerminalPrefixes = false
        }

    match CommandInput.compile catalog profile, InputProfileCodec.decode (InputProfileCodec.encode profile) with
    | Ok effective, Ok decoded when decoded = profile ->
        printfn "%s" (Convert.ToBase64String(InputProfileCodec.encode decoded))

        effective.Bindings
        |> List.iter (CommandInput.gestureId << _.Gesture >> printfn "%s")

        let initial = CommandResolver.init [ context.Id ] effective

        let input =
            {
                Id = "portable-press"
                Source = "keyboard"
                Gesture = chord (InputKeyIdentity.PhysicalCode "KeyE") none
                Phase = InputGesturePhase.Pressed
                IsRepeat = false
                NativeEditable = false
                HostReserved = false
                IsComposing = false
                AvailableCommands = [ descriptor.Id ]
            }

        let pressed, effects =
            CommandResolver.update catalog (CommandResolverObservation.InputEvent input) initial

        let released, releaseEffects =
            CommandResolver.update
                catalog
                (CommandResolverObservation.InputEvent
                    { input with
                        Id = "portable-release"
                        Phase = InputGesturePhase.Released
                    })
                pressed

        let effectToken =
            function
            | CommandResolverEffect.InvokeCommand value -> $"invoke:{value.EventId}:{value.Source}:{value.Command}"
            | CommandResolverEffect.HeldActionChanged(command, value) -> $"held:{command}:{value}"
            | CommandResolverEffect.RequestDeadline token -> $"deadline+:{token}"
            | CommandResolverEffect.CancelDeadline token -> $"deadline-:{token}"
            | CommandResolverEffect.RequestFocus target -> $"focus:{target}"
            | CommandResolverEffect.CapturedGesture gesture -> $"capture:{CommandInput.gestureId gesture}"
            | CommandResolverEffect.PreventDefault eventId -> $"prevent:{eventId}"
            | CommandResolverEffect.ResolverDiagnostic code -> $"diagnostic:{code}"

        printfn "%s" (effects |> List.map effectToken |> String.concat ",")
        printfn "%s" (releaseEffects |> List.map effectToken |> String.concat ",")

        printfn
            "resolver-state profile=%s contexts=%d modals=%d capture=%b pending=%b owned=%d composing=%b disposed=%b"
            released.Profile.Id
            released.ActiveContexts.Length
            released.ModalStack.Length
            released.CaptureRestoreFocus.IsSome
            released.PendingSequence.IsSome
            released.OwnedPresses.Length
            released.IsComposing
            released.IsDisposed

        let command id trigger =
            { descriptor with
                Id = id
                Label = id
                Trigger = trigger
            }

        let terminal = command "terminal" CommandTriggerPolicy.OncePerPress
        let continuation = command "continuation" CommandTriggerPolicy.OncePerPress
        let held = command "held" CommandTriggerPolicy.Continuous
        let k = InputKeyIdentity.LogicalKey "K", none
        let c = InputKeyIdentity.LogicalKey "C", none

        let modelCatalog =
            { catalog with
                Commands = [ terminal; continuation; held ]
                AllowTerminalPrefixes = true
            }

        let modelProfile =
            {
                Id = "model-correspondence"
                Bindings =
                    [
                        {
                            Context = context.Id
                            Command = terminal.Id
                            Gesture = InputGesture.KeyChord k
                        }
                        {
                            Context = context.Id
                            Command = continuation.Id
                            Gesture = InputGesture.KeySequence [ k; c ]
                        }
                        {
                            Context = context.Id
                            Command = held.Id
                            Gesture = chord (InputKeyIdentity.LogicalKey "ArrowRight") none
                        }
                    ]
            }

        let modelEvent id source gesture available =
            { input with
                Id = id
                Source = source
                Gesture = gesture
                AvailableCommands = available
            }

        let modelInitial = CommandResolver.init [ context.Id ] modelProfile

        let prefixState, _ =
            CommandResolver.update
                modelCatalog
                (CommandResolverObservation.InputEvent(
                    modelEvent "prefix" "keyboard" (InputGesture.KeyChord k) [ terminal.Id; continuation.Id ]
                ))
                modelInitial

        let terminalState, terminalEffects =
            CommandResolver.update
                modelCatalog
                (CommandResolverObservation.DeadlineElapsed("sequence:prefix", [ terminal.Id ]))
                prefixState

        let heldState, heldEffects =
            CommandResolver.update
                modelCatalog
                (CommandResolverObservation.InputEvent(
                    modelEvent "held" "pad" (chord (InputKeyIdentity.LogicalKey "ArrowRight") none) [ held.Id ]
                ))
                modelInitial

        let recoveredState, recoveredEffects =
            CommandResolver.update modelCatalog CommandResolverObservation.FocusLost heldState

        let _, lateReleaseEffects =
            CommandResolver.update
                modelCatalog
                (CommandResolverObservation.InputEvent
                    { modelEvent "release" "pad" (chord (InputKeyIdentity.LogicalKey "ArrowRight") none) [] with
                        Phase = InputGesturePhase.Released
                    })
                recoveredState

        let invoked command effects =
            effects
            |> List.exists (function
                | CommandResolverEffect.InvokeCommand value when value.Command = command -> true
                | _ -> false)

        let heldChanged expected effects =
            effects
            |> List.exists (function
                | CommandResolverEffect.HeldActionChanged(command, value) when command = held.Id && value = expected ->
                    true
                | _ -> false)

        printfn
            "model-correspondence terminalPending=%b terminalCleared=%b terminalInvoked=%b heldStarted=%b heldRecovered=%b lateReleaseSilent=%b"
            prefixState.PendingSequence.IsSome
            terminalState.PendingSequence.IsNone
            (invoked terminal.Id terminalEffects)
            (heldChanged true heldEffects)
            (heldChanged false recoveredEffects)
            lateReleaseEffects.IsEmpty
    | compiled, decoded -> failwithf "portable input profile failed: %A %A" compiled decoded
