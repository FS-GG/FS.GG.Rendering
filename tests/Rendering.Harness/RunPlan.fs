namespace Rendering.Harness

module RunPlan =

    type RunPlan =
        { Tier: Tier
          ClaimableProof: ProofLevel
          AuthoritativeFor: string list
          NotAuthoritativeFor: string list
          Degradation: Degradation
          VsyncFaithfulAllowed: bool }

    let proofFor tier =
        match tier with
        | T0 -> Deterministic
        | T1 -> OffscreenPixels
        | T2 -> LiveHost
        | T3 -> Timing
        | TUinput -> KernelInput

    let authoritativeFor tier =
        match tier with
        | T0 -> [ "determinism"; "tree-equality"; "retained-routing"; "non-blank-offscreen-png" ]
        | T1 -> [ "renderer-pixels" ]
        | T2 -> [ "window-creation"; "visibility"; "focus"; "real-input"; "desktop-screenshot" ]
        | T3 -> [ "frame-interval"; "paint-compose-swap-timing" ]
        | TUinput -> [ "evdev-libinput-input-path" ]

    let notAuthoritativeFor tier vsyncOk =
        match tier with
        | T0 -> [ "renderer-vs-desktop-pixels"; "live-host"; "timing" ]
        | T1 -> [ "desktop-visibility"; "focus"; "live-input" ]
        | T2 -> [ "timing"; "vsync-fidelity" ]
        | T3 -> if vsyncOk then [ "functional-correctness" ] else [ "functional-correctness"; "vsync-faithful" ]
        | TUinput -> [ "determinism"; "renderer-pixels"; "live-host"; "timing" ]

    let plan (tier: Tier) (facts: ProbeFacts) : RunPlan =
        let presentComplete = facts.SwapControl.IsSome && facts.VblankSource.IsSome
        let vsyncOk = (tier = T3) && presentComplete

        let degradation =
            match tier with
            | T0
            | T1 -> Run // deterministic / offscreen: no live desktop needed
            | T2
            | T3 ->
                match facts.EffectiveBackend with
                | NoDisplay -> Skip "no live desktop"
                | Wayland -> FailClassified "effective backend is Wayland, not X11"
                | X11 -> Run
            | TUinput ->
                if facts.UinputAvailable then Run
                else Skip "opt-in unavailable: requires host /dev/uinput + /dev/input pass-through"

        { Tier = tier
          ClaimableProof = proofFor tier
          AuthoritativeFor = authoritativeFor tier
          NotAuthoritativeFor = notAuthoritativeFor tier vsyncOk
          Degradation = degradation
          VsyncFaithfulAllowed = vsyncOk }
