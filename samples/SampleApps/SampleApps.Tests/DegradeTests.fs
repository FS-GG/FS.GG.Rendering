module SampleApps.Tests.DegradeTests

open Expecto
open SampleApps.Core
open SampleApps.Core.Games
open SampleApps.Core.Productivity

/// FR-007 / FR-008 / SC-003: the disclosed-degrade contract, asserted on the pure record path
/// (the `recordFor` no-capture path every suite uses) so the CI signal never depends on a
/// display. The App's evidence path routes any GL/capture failure through exactly these
/// functions and still exits 0 — the deterministic state + outcome remain authoritative.
let private records () =
    [ Tetris.recordAt 7
      Snake.recordAt 7
      Pong.recordAt 7
      Todo.recordAt 7
      Kanban.recordAt 7
      Calendar.recordAt 7 ]

[<Tests>]
let degradeTests =
    testList "Degrade" [
        test "every sample degrades-and-discloses on a no-GL path (FR-008/SC-003)" {
            for r in records () do
                Expect.isFalse r.Screenshot.ProvesScreenshot (sprintf "%s does not claim a screenshot" r.SampleId)
                Expect.isSome r.Screenshot.UnsupportedHostReason (sprintf "%s states an unsupported-host reason" r.SampleId)
                Expect.equal r.Screenshot.Fallback (Some "deterministic-state-only") (sprintf "%s discloses the fallback" r.SampleId)
                Expect.equal r.Screenshot.BlockedStage (Some "capture") (sprintf "%s names the blocked stage" r.SampleId)
                Expect.isNone r.Screenshot.Path (sprintf "%s leaves no frame path when not proven" r.SampleId)
                Expect.isNonEmpty r.NotAuthoritativeFor (sprintf "%s discloses what it is NOT authoritative for (FR-007)" r.SampleId)
                Expect.isFalse (List.contains "non-blank-offscreen-png" r.AuthoritativeFor) (sprintf "%s never claims the png when degraded" r.SampleId)
                // the deterministic state + outcome survive degradation (success, not a fabricated pass).
                Expect.contains r.AuthoritativeFor "outcome" (sprintf "%s outcome stays authoritative" r.SampleId)
        }

        test "the degraded summary never claims a screenshot and always discloses" {
            let s = Evidence.degraded "no display/GL on host"
            Expect.isFalse s.ProvesScreenshot "provesScreenshot=false"
            Expect.isSome s.UnsupportedHostReason "reason stated"
            Expect.isNone s.Path "no frame path"
            Expect.isSome s.Fallback "fallback stated"
        }

        test "degraded run.json marks provesScreenshot false and carries the disclosure" {
            let json = Evidence.toRunJson (Tetris.recordAt 7)
            Expect.stringContains json "\"provesScreenshot\": false" "false flag in json"
            Expect.stringContains json "\"notAuthoritativeFor\"" "disclosure present in json"
            Expect.stringContains json "deterministic-state-only" "fallback present in json"
        }
    ]
