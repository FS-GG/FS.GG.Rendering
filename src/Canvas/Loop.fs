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

    /// #266: a carried accumulator that is not a finite, non-negative number is spent time we cannot
    /// account for; treat it as empty rather than propagate it. Mirrors `FixedStep.drain`.
    let inline private finiteAccumulator (acc: float) =
        if System.Double.IsFinite acc && acc >= 0.0 then acc else 0.0

    let init (world: 'world) : StepState<'world> =
        { Current = world; Previous = world; Accumulator = 0.0 }

    let advance
        (dt: float)
        (integrate: 'world -> float -> 'world)
        (frameTime: float)
        (state: StepState<'world>)
        : StepState<'world> =
        if not (System.Double.IsFinite dt) || dt <= 0.0 then
            state
        else
            // Clamp negative/oversized/non-finite frame times before accumulating (determinism + no
            // spiral). Written as an explicit finiteness test, not `max 0.0 (min frameTime 0.25)`:
            // F#'s `min`/`max` PROPAGATE NaN, so that clamp fed NaN straight into the accumulator,
            // `acc >= dt` went false forever, and the simulation latched off (#266).
            let clamped =
                if System.Double.IsFinite frameTime && frameTime > 0.0 then
                    min frameTime maxFrameTime
                else
                    0.0

            // A non-finite or negative carried accumulator is empty, so a poisoned state heals.
            let acc0 = finiteAccumulator state.Accumulator

            let mutable current = state.Current
            let mutable previous = state.Previous
            let mutable acc = acc0 + clamped

            while acc >= dt do
                previous <- current
                current <- integrate current dt
                acc <- acc - dt

            { Current = current; Previous = previous; Accumulator = acc }

    let alpha (dt: float) (state: StepState<'world>) : float =
        if not (System.Double.IsFinite dt) || dt <= 0.0 then
            0.0
        else
            finiteAccumulator state.Accumulator / dt
