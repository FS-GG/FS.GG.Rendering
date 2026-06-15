namespace FS.GG.UI.Elmish

open System
open System.Threading
open Elmish

type AnimationTick = AnimationTick of TimeSpan

module Animation =
    [<Literal>]
    let private subKey = "animation-tick"

    let tickSubscription
        (isAnimating: 'model -> bool)
        (toMsg: TimeSpan -> 'msg)
        (interval: TimeSpan)
        (model: 'model)
        : Sub<'msg> =
        if isAnimating model then
            let subId: SubId = [ "fs-gg-ui"; subKey ]

            let start (dispatch: Dispatch<'msg>) : IDisposable =
                // Emit an immediate frame so the first advance happens without
                // waiting a full interval, then tick once per interval. The
                // delta carried is the nominal `interval` (a supplied, fixed
                // time model — deterministic-friendly, matching the fixed-step
                // game-loop convention).
                dispatch (toMsg interval)

                let periodMs = max 1.0 interval.TotalMilliseconds
                let period = int64 periodMs

                let timer =
                    new Timer((fun _ -> dispatch (toMsg interval)), null, period, period)

                { new IDisposable with
                    member _.Dispose() = timer.Dispose() }

            [ subId, start ]
        else
            // Settled: no subscription entry, so the host stops requesting
            // frames — no idle redraw (FR-006).
            Sub.none
