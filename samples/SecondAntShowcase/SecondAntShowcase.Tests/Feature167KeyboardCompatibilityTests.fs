module SecondAntShowcase.Tests.Feature167KeyboardCompatibilityTests

open Expecto
open FS.GG.UI.KeyboardInput
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

[<Tests>]
let tests =
    testList "Feature167 SecondAntShowcase keyboard compatibility" [
        test "Enter and Space activate on key-down only" {
            Expect.equal (Host.mapKey Enter true) (Some(PageMsg ButtonClicked)) "Enter key-down activates"
            Expect.equal (Host.mapKey Space true) (Some(PageMsg ButtonClicked)) "Space key-down activates"
            Expect.equal (Host.mapKey Enter false) None "Enter key-up does not activate"
            Expect.equal (Host.mapKey Space false) None "Space key-up does not activate"
        }
    ]
