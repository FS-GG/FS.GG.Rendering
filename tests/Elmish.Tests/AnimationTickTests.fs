module AnimationTickTests

// Feature 073 (US2, FR-006, SC-004/SC-007): the animation tick subscription gates
// redraws at the framework-request level. It emits `AnimationTick` deltas while
// `isAnimating` holds and goes silent (Sub.none) once the model settles; dropping
// the animating state from the model (a removed widget) stops further ticks
// cleanly. Exercised through the real Elmish `Sub` plumbing — no mocks.

open System
open Expecto
open Elmish
open FS.Skia.UI.Elmish

type private DemoModel = { Running: bool }

let private isAnimating (m: DemoModel) = m.Running

/// A far-future interval so the recurring timer cannot fire inside the
/// synchronous test window: only the immediate first frame is recorded, then the
/// subscription is disposed.
let private interval = TimeSpan.FromSeconds 30.0

/// Start every entry in the Sub through a recording dispatcher, then dispose —
/// returns the messages dispatched synchronously on start, in order.
let private runSub (sub: Sub<'msg>) : 'msg list =
    let recorded = System.Collections.Generic.List<'msg>()
    let started = sub |> List.map (fun (_id, start) -> start (fun m -> recorded.Add m))
    started |> List.iter (fun d -> d.Dispose())
    List.ofSeq recorded

[<Tests>]
let animationTickTests =
    testList "Animation.tickSubscription gating (FR-006)" [
        test "emits an AnimationTick while isAnimating holds" {
            let sub = Animation.tickSubscription isAnimating AnimationTick interval { Running = true }
            Expect.isNonEmpty sub "an active model carries a ticking subscription entry"
            Expect.equal (runSub sub) [ AnimationTick interval ] "the active subscription dispatches a frame delta"
        }

        test "goes silent (Sub.none) once the model settles" {
            let sub = Animation.tickSubscription isAnimating AnimationTick interval { Running = false }
            Expect.isEmpty sub "a settled model carries no subscription entry (no idle redraw)"
            Expect.isEmpty (runSub sub) "a settled subscription dispatches nothing"
        }

        test "the subscription id is stably scoped" {
            let sub = Animation.tickSubscription isAnimating AnimationTick interval { Running = true }
            let ids = sub |> List.map fst
            Expect.equal ids [ [ "fs-skia-ui"; "animation-tick" ] ] "stable, scoped SubId"
        }

        test "dropping the animating state stops further ticks cleanly (removed widget edge)" {
            // Model transitions running → settled (e.g. the animating widget is
            // removed): the subscription diff sees the entry disappear and stops.
            let active = Animation.tickSubscription isAnimating AnimationTick interval { Running = true }
            let settled = Animation.tickSubscription isAnimating AnimationTick interval { Running = false }
            Expect.isNonEmpty active "active before"
            Expect.isEmpty settled "silent after the state is dropped — Elmish stops the removed sub"
            // Disposing the active subscription does not throw.
            runSub active |> ignore
        }
    ]
