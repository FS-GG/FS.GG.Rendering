module ControlsGallery.Tests.DegradeTests

open Expecto
open ControlsGallery.Core

/// FR-010 / FR-011 / SC-004: the disclosed-degrade contract, asserted on the pure
/// record functions so the CI signal never depends on a display. (The App's evidence
/// path routes any GL/capture failure through exactly these functions and exits 0.)
[<Tests>]
let degradeTests =
    testList "Degrade" [
        test "degraded summary discloses and never claims a screenshot" {
            let s = Evidence.degraded "no display/GL on host"
            Expect.isFalse s.ProvesScreenshot "provesScreenshot=false"
            Expect.isSome s.UnsupportedHostReason "reason stated"
            Expect.isNone s.Path "no frame path when not proven"
            Expect.isSome s.Fallback "fallback stated"
        }

        test "every record carries a non-empty notAuthoritativeFor (FR-010)" {
            let proven: Evidence.ScreenshotSummary =
                { ProvesScreenshot = true
                  BlockedStage = None
                  UnsupportedHostReason = None
                  Fallback = None
                  Path = Some "frame.png" }
            let provenRecord = Evidence.build "p" 1 [] proven
            let degradedRecord = Evidence.build "p" 1 [] (Evidence.degraded "x")
            Expect.isNonEmpty provenRecord.NotAuthoritativeFor "proven record discloses"
            Expect.isNonEmpty degradedRecord.NotAuthoritativeFor "degraded record discloses"
        }

        test "a proven record claims the non-blank-offscreen-png; a degraded one does not" {
            let proven: Evidence.ScreenshotSummary =
                { ProvesScreenshot = true
                  BlockedStage = None
                  UnsupportedHostReason = None
                  Fallback = None
                  Path = Some "frame.png" }
            let provenRecord = Evidence.build "p" 1 [] proven
            let degradedRecord = Evidence.build "p" 1 [] (Evidence.degraded "x")
            Expect.contains provenRecord.AuthoritativeFor "non-blank-offscreen-png" "proven claims the png"
            Expect.isFalse (List.contains "non-blank-offscreen-png" degradedRecord.AuthoritativeFor) "degraded never claims the png"
        }

        test "degraded run.json marks provesScreenshot false with a stated reason" {
            let json =
                Evidence.toRunJson (Evidence.build "display-typography" 7 [] (Evidence.degraded "headless: offscreen GL unavailable"))
            Expect.stringContains json "\"provesScreenshot\": false" "false flag in json"
            Expect.stringContains json "headless: offscreen GL unavailable" "reason present in json"
            Expect.stringContains json "\"notAuthoritativeFor\"" "disclosure present in json"
        }
    ]
