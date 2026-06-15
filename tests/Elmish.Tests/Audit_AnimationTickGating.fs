module Audit_AnimationTickGating

// AUDIT (feature 006-verify-imported-mechanisms) — animation tick-gating mechanism.
//   * T007 sanity: `Animation.tickSubscription` is reachable and returns a `Sub`.
//   * T035 US3 effectiveness: the tick subscription gates redraws at the framework-request level. It
//     requests NO tick when no clock is active (model not animating => `Sub.none`/empty + dispatches
//     nothing) and DOES request a tick when one is active (animating model => non-empty Sub that
//     dispatches a frame delta). BOTH directions are proven in one place: a subscription that ALWAYS
//     ticks would fail the idle direction; one that NEVER ticks would fail the active direction. This
//     distinguishes "correctly a no-op when idle" from "broken". Driven through the real Elmish `Sub`
//     plumbing (mirrors AnimationTickTests) — no mocks.

open System
open Expecto
open Elmish
open FS.GG.UI.Elmish

type private DemoModel = { Running: bool }

let private isAnimating (m: DemoModel) = m.Running
let private interval = TimeSpan.FromSeconds 30.0 // far-future: only the immediate first frame fires synchronously

// Start every entry in the Sub through a recording dispatcher, then dispose — returns the messages
// dispatched synchronously on start, in order.
let private runSub (sub: Sub<'msg>) : 'msg list =
    let recorded = System.Collections.Generic.List<'msg>()
    let started = sub |> List.map (fun (_id, start) -> start (fun m -> recorded.Add m))
    started |> List.iter (fun d -> d.Dispose())
    List.ofSeq recorded

let private subFor running =
    Animation.tickSubscription isAnimating AnimationTick interval { Running = running }

[<Tests>]
let tests =
    testList "Audit animation tick-gating mechanism (T007 / T035 US3)" [

        // ---- T007 sanity --------------------------------------------------------------------------
        test "Audit: tickSubscription reachable — builds a Sub for a model (T007)" {
            let sub = subFor true
            Expect.isTrue (List.length sub >= 0) "tickSubscription is reachable and returns a Sub"
        }

        // ---- T035 US3 effectiveness (both directions) ---------------------------------------------
        test "Audit: NO tick is requested when idle (model not animating => Sub.none) (T035, idle direction)" {
            let sub = subFor false
            Expect.isEmpty sub "a settled model carries NO subscription entry (no idle redraw requested)"
            Expect.isEmpty (runSub sub) "a settled subscription dispatches nothing (correctly a no-op, not broken)"
        }

        test "Audit: a tick IS requested when active (animating model => non-empty Sub) (T035, active direction)" {
            let sub = subFor true
            Expect.isNonEmpty sub "an active model carries a ticking subscription entry"
            Expect.equal (runSub sub) [ AnimationTick interval ] "the active subscription dispatches a frame delta"
        }

        test "Audit: gating discriminates BOTH directions — idle empty AND active non-empty (T035, discriminating)" {
            // A subscription that always-ticks would fail the first assertion; one that never-ticks the
            // second. Proving both in one test is the discriminating power of the gating mechanism.
            let idle = subFor false
            let active = subFor true
            Expect.isEmpty idle "idle => empty"
            Expect.isNonEmpty active "active => non-empty"
            Expect.notEqual (List.length idle) (List.length active) "the gate genuinely responds to the model's animating state"
        }
    ]
