module Feature121IdleTickTests

// Feature 121 (US2, FR-004) — the live per-tick clock advance is allocation-free when no clock is
// active. `RetainedRender.advanceStateClocks` (internal, reached via InternalsVisibleTo) returns the
// state map reference-equal when nothing is animating, and advances active clocks exactly as the
// per-clock `advance` oracle (features 099/103 unchanged). The in-assembly test IS the user-reachable
// surface for this internal seam (the live host's `wrappedTick` calls it).

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private oneLeaf: Control<int> =
    { Kind = "text-block"
      Key = Some "a"
      Attributes =
        [ { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 120.0 }
          { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 24.0 } ]
      Children = []
      Content = Some "hi"
      Accessibility = None }

let private anId () : RetainedId =
    (RetainedRender.init theme size oneLeaf).Retained.Root.Identity

// A still-in-flight opacity fade (Elapsed 250ms of a 1s tween) — clockActive = true.
let private activeClock () : AnimationClock =
    { Anim =
        { Animation.empty with
            Opacity =
                Some
                    { Start = 0.0
                      End = 1.0
                      Duration = TimeSpan.FromSeconds 1.0
                      Easing = Easing.EaseOut } }
      Elapsed = TimeSpan.FromMilliseconds 250.0
      Target = Normal
      From = [] }

// Past the 1s duration ⇒ clockActive = false (settled, paints byte-identically to static).
let private settledClock () : AnimationClock =
    { activeClock () with Elapsed = TimeSpan.FromSeconds 2.0 }

[<Tests>]
let tests =
    testList
        "Feature 121 idle-tick no-alloc (US2, FR-004)"
        [ test "no active clock ⇒ advanceStateClocks returns the state reference-equal (SC-003)" {
              let id = anId ()

              let noClock = Map.ofList [ id, { Animation = None; Text = None } ]
              let r1 = RetainedRender.advanceStateClocks (TimeSpan.FromMilliseconds 16.0) noClock
              Expect.isTrue (obj.ReferenceEquals(r1, noClock)) "an all-inactive (no clock) state allocates nothing"

              let settled = Map.ofList [ id, { Animation = Some(settledClock ()); Text = None } ]
              let r2 = RetainedRender.advanceStateClocks (TimeSpan.FromMilliseconds 16.0) settled
              Expect.isTrue (obj.ReferenceEquals(r2, settled)) "a settled (inactive) clock also allocates nothing"
          }

          test "an active clock advances by the delta exactly as the per-clock oracle (099/103 unchanged)" {
              let id = anId ()
              let before = activeClock ()
              let state = Map.ofList [ id, { Animation = Some before; Text = None } ]

              let advanced = RetainedRender.advanceStateClocks (TimeSpan.FromMilliseconds 100.0) state
              Expect.isFalse (obj.ReferenceEquals(advanced, state)) "an active clock forces a rebuilt map"

              match (advanced |> Map.find id).Animation with
              | Some c ->
                  Expect.equal c.Elapsed (TimeSpan.FromMilliseconds 350.0) "the active clock advanced by the 100ms delta (250→350)"
                  Expect.equal
                      c.Elapsed
                      (RetainedRender.advance (TimeSpan.FromMilliseconds 100.0) before).Elapsed
                      "matches the per-clock advance oracle exactly"
              | None -> failtest "advanced clock vanished"
          } ]
