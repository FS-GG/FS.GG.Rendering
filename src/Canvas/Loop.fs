namespace FS.GG.UI.Canvas

type StepState<'world> =
    { Current: 'world
      Previous: 'world
      Accumulator: float }

[<RequireQualifiedAccess>]
module Loop =

    /// FR-009: the spiral-of-death clamp — a runaway frame never injects more than 0.25s of simulation.
    [<Literal>]
    let private maxFrameTime = 0.25

    let init (world: 'world) : StepState<'world> =
        { Current = world; Previous = world; Accumulator = 0.0 }

    let advance
        (dt: float)
        (integrate: 'world -> float -> 'world)
        (frameTime: float)
        (state: StepState<'world>)
        : StepState<'world> =
        if dt <= 0.0 then
            state
        else
            // Clamp negative/oversized frame times before accumulating (determinism + no spiral).
            let clamped = max 0.0 (min frameTime maxFrameTime)
            let mutable current = state.Current
            let mutable previous = state.Previous
            let mutable acc = state.Accumulator + clamped

            while acc >= dt do
                previous <- current
                current <- integrate current dt
                acc <- acc - dt

            { Current = current; Previous = previous; Accumulator = acc }

    let alpha (dt: float) (state: StepState<'world>) : float =
        if dt <= 0.0 then 0.0 else state.Accumulator / dt
