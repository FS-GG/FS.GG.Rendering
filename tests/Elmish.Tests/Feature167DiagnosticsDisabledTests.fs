module Feature167DiagnosticsDisabledTests

open Expecto
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Controls.Elmish

[<Tests>]
let tests =
    testList "Feature167 diagnostics disabled compatibility" [
        test "disabled diagnostics leaves Perf.runScript count/bool shape unchanged" {
            let script = [ FrameInput.Key(Enter, ViewerKeyboard.noModifiers); FrameInput.Idle ]
            let before = Feature167ResponsivenessFixtures.run script
            let after = Feature167ResponsivenessFixtures.run script
            let compatibility = ControlsElmish.diagnosticsDisabledCompatibility before after

            Expect.isTrue compatibility.FrameMetricsUnchanged "deterministic metrics are unchanged"
            Expect.equal compatibility.RecordsWritten 0 "disabled diagnostics writes no records"
            Expect.isTrue compatibility.ClockFreePerfScript "deterministic timings stay zero"
        }
    ]
