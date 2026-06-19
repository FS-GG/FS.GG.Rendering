module AntShowcase.Tests.Feature167KeyboardCompatibilityTests

open Expecto
open FS.GG.UI.KeyboardInput
open AntShowcase.Core
open AntShowcase.Core.Model

[<Tests>]
let tests =
    testList "Feature167 AntShowcase keyboard compatibility" [
        test "Enter and Space activate on key-down only" {
            Expect.equal (Host.mapKey Enter true) (Some(PageMsg ButtonClicked)) "Enter key-down activates"
            Expect.equal (Host.mapKey Space true) (Some(PageMsg ButtonClicked)) "Space key-down activates"
            Expect.equal (Host.mapKey Enter false) None "Enter key-up does not activate"
            Expect.equal (Host.mapKey Space false) None "Space key-up does not activate"
        }
    ]
