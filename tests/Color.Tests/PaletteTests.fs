module PaletteTests

(* Feature 083 / US2 (SC-003) — failing-first (Principle I/VI) ramp-invariant test for the
   Radix-derived palettes in FS.GG.UI.Color. Asserts every offered family has matched
   Light + Dark ramps and that at least one documented Text-step over a documented
   AppBackground-step meets AA body text (>= 4.5:1) under Contrast.ratio. *)

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Color

[<Tests>]
let rampInvariantTests =
    testList "Feature 083 palette ramp invariants (SC-003)" [

        test "every family offers a matched Light and Dark ramp" {
            for family in Palettes.families do
                Expect.isSome (Palettes.ramp family Palettes.Light) (sprintf "%s has a Light ramp" family)
                Expect.isSome (Palettes.ramp family Palettes.Dark) (sprintf "%s has a Dark ramp" family)
        }

        test "each ramp has a documented Text step over a documented AppBackground step meeting AA (>= 4.5)" {
            for ramp in Palettes.all do
                let textStep = ramp.Steps |> List.find (fun s -> s.Role = Palettes.Text)
                let bgStep = ramp.Steps |> List.find (fun s -> s.Role = Palettes.AppBackground)
                let measured = Contrast.ratio textStep.Color bgStep.Color
                Expect.isGreaterThanOrEqual
                    measured
                    4.5
                    (sprintf "%s/%A text-step %d over app-background meets AA body text" ramp.Family ramp.Variant textStep.Index)
        }

        test "ramp colors are opaque (alpha 255)" {
            for ramp in Palettes.all do
                for step in ramp.Steps do
                    Expect.equal step.Color.Alpha 255uy (sprintf "%s/%A step %d is opaque" ramp.Family ramp.Variant step.Index)
        }

        test "the shipped families needed to cure the themes are present" {
            for family in [ "slate"; "blue"; "red" ] do
                Expect.contains Palettes.families family (sprintf "%s ramp is offered" family)
        }
    ]
