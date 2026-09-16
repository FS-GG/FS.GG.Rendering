module CommandResolverTests

open Expecto
open FS.GG.UI.KeyboardInput

let private none = CommandInput.noModifiers

let private key value =
    InputGesture.KeyChord(InputKeyIdentity.LogicalKey value, none)

let private step value = InputKeyIdentity.LogicalKey value, none

let private binding context command gesture =
    {
        Context = context
        Command = command
        Gesture = gesture
    }

let private descriptor id contexts trigger =
    {
        Id = id
        Label = id
        Contexts = contexts
        AvailabilityKey = None
        Trigger = trigger
        Argument = CommandArgumentPolicy.NoArgument
        Alternatives = [ CommandAlternative.Palette ]
    }

let private catalog =
    {
        Contexts =
            [
                {
                    Id = "workspace"
                    Priority = 10
                    Exclusive = false
                    Overlaps = [ "editor" ]
                }
                {
                    Id = "editor"
                    Priority = 20
                    Exclusive = false
                    Overlaps = [ "workspace" ]
                }
                {
                    Id = "modal"
                    Priority = 100
                    Exclusive = true
                    Overlaps = []
                }
            ]
        Commands =
            [
                descriptor "help" [ "workspace" ] CommandTriggerPolicy.OncePerPress
                descriptor "commit" [ "editor" ] CommandTriggerPolicy.RepeatWhileHeld
                descriptor "move" [ "editor" ] CommandTriggerPolicy.Continuous
                descriptor "prefix" [ "editor" ] CommandTriggerPolicy.OncePerPress
                descriptor "comment" [ "editor" ] CommandTriggerPolicy.OncePerPress
                descriptor "modal.accept" [ "modal" ] CommandTriggerPolicy.OncePerPress
            ]
        ReservedGestures = []
        AllowTerminalPrefixes = true
    }

let private profile =
    {
        Id = "test"
        Bindings =
            [
                binding "workspace" "help" (key "H")
                binding "editor" "commit" (key "Enter")
                binding "editor" "move" (key "ArrowRight")
                binding "editor" "prefix" (key "K")
                binding "editor" "comment" (InputGesture.KeySequence [ step "K"; step "C" ])
                binding "modal" "modal.accept" (key "Enter")
            ]
    }

let private event id source gesture phase repeat available =
    {
        Id = id
        Source = source
        Gesture = gesture
        Phase = phase
        IsRepeat = repeat
        NativeEditable = false
        HostReserved = false
        IsComposing = false
        AvailableCommands = available
    }

let private press id source gesture available =
    CommandResolverObservation.InputEvent(event id source gesture InputGesturePhase.Pressed false available)

let private repeat id source gesture available =
    CommandResolverObservation.InputEvent(event id source gesture InputGesturePhase.Pressed true available)

let private release id source gesture =
    CommandResolverObservation.InputEvent(event id source gesture InputGesturePhase.Released false [])

let private run observations =
    ((CommandResolver.init [ "workspace"; "editor" ] profile, []), observations)
    ||> List.fold (fun (state, allEffects) observation ->
        let next, effects = CommandResolver.update catalog observation state
        next, allEffects @ effects)

let private invocations effects =
    effects
    |> List.choose (function
        | CommandResolverEffect.InvokeCommand value -> Some value.Command
        | _ -> None)

[<Tests>]
let tests =
    testList
        "SVG-INPUT-01 deterministic modal resolver"
        [
            test "terminal prefix waits for its injected deadline and fires once" {
                let state, effects = run [ press "e1" "keyboard" (key "K") [ "prefix"; "comment" ] ]
                Expect.equal (invocations effects) [] "ambiguous terminal waits"
                Expect.contains effects (CommandResolverEffect.RequestDeadline "sequence:e1") "host owns the deadline"

                let next, deadlineEffects =
                    CommandResolver.update
                        catalog
                        (CommandResolverObservation.DeadlineElapsed("sequence:e1", [ "prefix" ]))
                        state

                Expect.equal (invocations deadlineEffects) [ "prefix" ] "deadline admits the terminal"

                let _, staleEffects =
                    CommandResolver.update
                        catalog
                        (CommandResolverObservation.DeadlineElapsed("sequence:e1", [ "prefix" ]))
                        next

                Expect.isEmpty (invocations staleEffects) "the same deadline cannot invoke twice"
            }

            test "matching continuation cancels the old deadline and invokes the sequence" {
                let state, _ = run [ press "e1" "keyboard" (key "K") [ "prefix"; "comment" ] ]

                let _, effects =
                    CommandResolver.update catalog (press "e2" "keyboard" (key "C") [ "comment" ]) state

                Expect.contains effects (CommandResolverEffect.CancelDeadline "sequence:e1") "old deadline is cancelled"
                Expect.equal (invocations effects) [ "comment" ] "longer binding wins"
            }

            test "nonmatching continuation retries exactly once at the root" {
                let _, effects =
                    run
                        [
                            press "e1" "keyboard" (key "K") [ "prefix"; "comment" ]
                            press "e2" "keyboard" (key "Enter") [ "commit" ]
                        ]

                Expect.equal (invocations effects) [ "commit" ] "the continuation is reconsidered as a root gesture"

                Expect.equal
                    (effects
                     |> List.filter ((=) (CommandResolverEffect.PreventDefault "e2"))
                     |> List.length)
                    1
                    "one host event is claimed once"
            }

            test "deadline rechecks current availability" {
                let state, _ = run [ press "e1" "keyboard" (key "K") [ "prefix"; "comment" ] ]

                let _, effects =
                    CommandResolver.update catalog (CommandResolverObservation.DeadlineElapsed("sequence:e1", [])) state

                Expect.isEmpty (invocations effects) "an unavailable terminal is not dispatched"
            }

            test "profile replacement cancels pending input and applies immediately" {
                let pending, _ = run [ press "e1" "keyboard" (key "K") [ "prefix"; "comment" ] ]

                let replacement =
                    {
                        Id = "replacement"
                        Bindings = [ binding "editor" "commit" (key "H") ]
                    }

                let replaced, replacementEffects =
                    CommandResolver.update catalog (CommandResolverObservation.ProfileChanged replacement) pending

                Expect.contains
                    replacementEffects
                    (CommandResolverEffect.CancelDeadline "sequence:e1")
                    "old profile prefix is cancelled"

                Expect.equal replaced.Profile replacement "new profile is active in the same transition"

                let _, effects =
                    CommandResolver.update catalog (press "e2" "keyboard" (key "H") [ "commit" ]) replaced

                Expect.equal (invocations effects) [ "commit" ] "next observation sees the new bindings"
            }

            test "once-per-press and repeat policies suppress duplicate physical ownership" {
                let _, onceEffects =
                    run
                        [
                            press "h1" "keyboard" (key "H") [ "help" ]
                            repeat "h2" "keyboard" (key "H") [ "help" ]
                            press "h3" "keyboard" (key "H") [ "help" ]
                        ]

                Expect.equal (invocations onceEffects) [ "help" ] "once policy emits once until release"

                let _, repeatingEffects =
                    run
                        [
                            press "r1" "keyboard" (key "Enter") [ "commit" ]
                            repeat "r2" "keyboard" (key "Enter") [ "commit" ]
                            press "r3" "keyboard" (key "Enter") [ "commit" ]
                        ]

                Expect.equal (invocations repeatingEffects) [ "commit"; "commit" ] "only a marked repeat may repeat"
            }

            test "continuous action remains held until its final owning source releases" {
                let state, pressed =
                    run
                        [
                            press "p1" "keyboard-a" (key "ArrowRight") [ "move" ]
                            press "p2" "keyboard-b" (key "ArrowRight") [ "move" ]
                        ]

                Expect.equal
                    (pressed
                     |> List.choose (function
                         | CommandResolverEffect.HeldActionChanged(command, isHeld) -> Some(command, isHeld)
                         | _ -> None))
                    [ "move", true ]
                    "first owner starts the aggregate"

                let oneOwner, firstRelease =
                    CommandResolver.update catalog (release "u1" "keyboard-a" (key "ArrowRight")) state

                Expect.isEmpty
                    (firstRelease
                     |> List.choose (function
                         | CommandResolverEffect.HeldActionChanged(command, isHeld) -> Some(command, isHeld)
                         | _ -> None))
                    "one owner remains"

                let _, finalRelease =
                    CommandResolver.update catalog (release "u2" "keyboard-b" (key "ArrowRight")) oneOwner

                Expect.contains
                    finalRelease
                    (CommandResolverEffect.HeldActionChanged("move", false))
                    "last owner stops the aggregate"
            }

            test "context replacement neutralizes ownership before a late release" {
                let state, _ = run [ press "p1" "keyboard" (key "ArrowRight") [ "move" ] ]

                let reset, effects =
                    CommandResolver.update catalog (CommandResolverObservation.ContextsChanged [ "workspace" ]) state

                Expect.contains
                    effects
                    (CommandResolverEffect.HeldActionChanged("move", false))
                    "context change recovers held state"

                let _, late =
                    CommandResolver.update catalog (release "u1" "keyboard" (key "ArrowRight")) reset

                Expect.isEmpty late "late release cannot emit a duplicate transition"
            }

            test "release ownership survives modifier changes after the press" {
                let shifted =
                    InputGesture.KeyChord(InputKeyIdentity.LogicalKey "ArrowRight", { none with Shift = true })

                let state, _ = run [ press "p1" "keyboard" (key "ArrowRight") [ "move" ] ]

                let _, effects =
                    CommandResolver.update catalog (release "u1" "keyboard" shifted) state

                Expect.contains
                    effects
                    (CommandResolverEffect.HeldActionChanged("move", false))
                    "release matches the owned key identity"
            }

            test "capture receives an unbound gesture before command lookup and restores focus" {
                let state, _ = run [ CommandResolverObservation.BeginCapture "binding-editor" ]

                let next, effects =
                    CommandResolver.update catalog (press "e1" "keyboard" (key "Z") []) state

                Expect.contains effects (CommandResolverEffect.CapturedGesture(key "Z")) "raw gesture is returned"

                Expect.contains
                    effects
                    (CommandResolverEffect.RequestFocus "binding-editor")
                    "capture restores its focus owner"

                Expect.isNone next.CaptureRestoreFocus "capture closes after one gesture"
            }

            test "exclusive modal context blocks every lower context" {
                let state, _ =
                    run
                        [
                            CommandResolverObservation.PushModal
                                {
                                    Context = "modal"
                                    RestoreFocus = "canvas"
                                }
                        ]

                let _, blocked =
                    CommandResolver.update catalog (press "e1" "keyboard" (key "H") [ "help" ]) state

                Expect.isEmpty (invocations blocked) "workspace does not receive modal input"

                let _, accepted =
                    CommandResolver.update
                        catalog
                        (press "e2" "keyboard" (key "Enter") [ "commit"; "modal.accept" ])
                        state

                Expect.equal (invocations accepted) [ "modal.accept" ] "modal command resolves"
            }

            test "an unknown modal owner fails closed" {
                let state, _ =
                    run
                        [
                            CommandResolverObservation.PushModal
                                {
                                    Context = "missing"
                                    RestoreFocus = "canvas"
                                }
                        ]

                let _, effects =
                    CommandResolver.update catalog (press "e1" "keyboard" (key "H") [ "help" ]) state

                Expect.isEmpty (invocations effects) "invalid modal ownership cannot fall through to workspace"
            }

            test "Escape unwinds capture modal and prefix in innermost order without fallthrough" {
                let captured, _ = run [ CommandResolverObservation.BeginCapture "bindings" ]

                let afterCapture, effects1 =
                    CommandResolver.update catalog (CommandResolverObservation.Escape("esc1", "keyboard")) captured

                Expect.contains effects1 (CommandResolverEffect.RequestFocus "bindings") "capture closes first"

                let modal, _ =
                    CommandResolver.update
                        catalog
                        (CommandResolverObservation.PushModal
                            {
                                Context = "modal"
                                RestoreFocus = "canvas"
                            })
                        afterCapture

                let afterModal, effects2 =
                    CommandResolver.update catalog (CommandResolverObservation.Escape("esc2", "keyboard")) modal

                Expect.contains effects2 (CommandResolverEffect.RequestFocus "canvas") "modal closes next"

                let pending, _ =
                    CommandResolver.update catalog (press "e1" "keyboard" (key "K") [ "prefix"; "comment" ]) afterModal

                let cleared, effects3 =
                    CommandResolver.update catalog (CommandResolverObservation.Escape("esc3", "keyboard")) pending

                Expect.contains effects3 (CommandResolverEffect.CancelDeadline "sequence:e1") "prefix closes last"
                Expect.isNone cleared.PendingSequence "prefix is cleared"
                Expect.equal (invocations (effects1 @ effects2 @ effects3)) [] "Escape never falls through to a command"
            }

            test "composition focus takeover disconnect and disposal neutralize transient state" {
                let pending, _ = run [ press "e1" "keyboard" (key "K") [ "prefix"; "comment" ] ]

                let composing, compositionEffects =
                    CommandResolver.update catalog CommandResolverObservation.CompositionStarted pending

                Expect.contains
                    compositionEffects
                    (CommandResolverEffect.CancelDeadline "sequence:e1")
                    "composition cancels prefix"

                let _, ignored =
                    CommandResolver.update catalog (press "e2" "keyboard" (key "Enter") [ "commit" ]) composing

                Expect.isEmpty ignored "composition owns key input"

                let held, _ = run [ press "p1" "pad" (key "ArrowRight") [ "move" ] ]

                let disconnected, disconnectEffects =
                    CommandResolver.update catalog (CommandResolverObservation.SourceDisconnected "pad") held

                Expect.contains
                    disconnectEffects
                    (CommandResolverEffect.HeldActionChanged("move", false))
                    "disconnect recovers its held contribution"

                let taken, _ =
                    CommandResolver.update catalog CommandResolverObservation.ModalTakenOver disconnected

                let disposed, _ =
                    CommandResolver.update catalog CommandResolverObservation.Dispose taken

                let same, effects =
                    CommandResolver.update catalog (press "e3" "keyboard" (key "H") [ "help" ]) disposed

                Expect.equal same disposed "disposed state is inert"
                Expect.isEmpty effects "disposed state emits nothing"
            }

            test "the same observation trace produces structurally identical state and effects" {
                let observations =
                    [
                        press "e1" "keyboard" (key "K") [ "prefix"; "comment" ]
                        press "e2" "keyboard" (key "C") [ "comment" ]
                        release "e3" "keyboard" (key "C")
                        CommandResolverObservation.FocusLost
                    ]

                Expect.equal (run observations) (run observations) "the reducer has no ambient dependencies"
            }
        ]
