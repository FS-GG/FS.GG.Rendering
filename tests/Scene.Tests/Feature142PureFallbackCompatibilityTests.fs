module Feature142PureFallbackCompatibilityTests

open Expecto
open FS.GG.UI.Scene

let private font: FontSpec = { Family = None; Size = 20.0; Weight = None }

[<Tests>]
let tests =
    testList "Feature142 pure fallback compatibility" [
        test "pure fallback metrics match the existing Scene heuristic" {
            let shaped = Scene.buildFallbackShapedText "fallback" font
            let legacy = Scene.measureText "fallback" font

            Expect.equal (Scene.measureShapedText shaped) legacy "pure fallback shaped metrics preserve legacy measurement"
        }

        test "provider absence is visible and deterministic" {
            let a = Scene.buildFallbackShapedText "provider absent" font
            let b = Scene.buildFallbackShapedText "provider absent" font

            Expect.equal a.Provider.Availability ProviderUnavailable "absence is explicit"
            Expect.equal a.Diagnostics b.Diagnostics "diagnostics are deterministic"
            Expect.equal a.Fingerprint b.Fingerprint "fingerprint is deterministic"
        }
    ]
