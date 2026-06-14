namespace FS.Skia.UI.Color

open FS.Skia.UI.Scene

// Ramps are derived from Radix Colors (https://www.radix-ui.com/colors), MIT-licensed.
// Copyright (c) 2022 WorkOS. The 12-step, role-structured light/dark scales are reproduced
// here as literal, opaque `Color` steps. Radix states its own guarantees in APCA; the
// WCAG `ContrastCheck` gate — not this source palette — is the authority that certifies the
// WCAG conformance of any value chosen from a ramp (FR-006, research R5). These ramps are
// reusable catalog data only; they are NOT a second source of truth for the shipped themes.
module Palettes =

    type StepRole =
        | AppBackground
        | SubtleBackground
        | ComponentBackground
        | Border
        | FocusRing
        | Solid
        | Text

    type RampVariant =
        | Light
        | Dark

    type PaletteStep =
        { Index: int
          Role: StepRole
          Color: Color }

    type PaletteRamp =
        { Family: string
          Variant: RampVariant
          Steps: PaletteStep list }

    // The documented Radix role for each 1-based step of a 12-step scale: app/subtle
    // backgrounds (1-2), component backgrounds (3-5), borders (6-7), focus ring (8),
    // solids (9-10), low/high-contrast text (11-12).
    let private stepRoles =
        [ AppBackground
          SubtleBackground
          ComponentBackground
          ComponentBackground
          ComponentBackground
          Border
          Border
          FocusRing
          Solid
          Solid
          Text
          Text ]

    let private parseHex (hex: string) =
        let byteAt i = System.Convert.ToByte(hex.Substring(i, 2), 16)
        Colors.rgba (byteAt 0) (byteAt 2) (byteAt 4) 255uy

    let private buildRamp family variant (hexes: string list) =
        { Family = family
          Variant = variant
          Steps =
            List.zip stepRoles hexes
            |> List.mapi (fun i (role, hex) ->
                { Index = i + 1
                  Role = role
                  Color = parseHex hex }) }

    let all =
        [ buildRamp "slate" Light
            [ "fbfcfd"; "f8f9fa"; "f1f3f5"; "eceef0"; "e6e8eb"; "dfe3e6"
              "d7dbdf"; "c1c8cd"; "889096"; "7e868c"; "687076"; "11181c" ]
          buildRamp "slate" Dark
            [ "151718"; "1a1d1e"; "202425"; "26292b"; "2b2f31"; "313538"
              "3a3f42"; "4c5155"; "697177"; "787f85"; "9ba1a6"; "ecedee" ]
          buildRamp "blue" Light
            [ "fbfdff"; "f5faff"; "edf6ff"; "e1f0ff"; "cee7fe"; "b7d9f8"
              "96c7f2"; "5eb0ef"; "0091ff"; "0081f1"; "006adc"; "00254d" ]
          buildRamp "blue" Dark
            [ "0f1720"; "0f1b2d"; "10243e"; "102a4c"; "0f3058"; "0d3868"
              "0a4481"; "0954a5"; "0091ff"; "369eff"; "52a9ff"; "eaf6ff" ]
          buildRamp "red" Light
            [ "fffcfc"; "fff8f8"; "ffefef"; "ffe5e5"; "fdd8d8"; "f9c6c6"
              "f3aeaf"; "eb9091"; "e5484d"; "dc3d43"; "cd2b31"; "381316" ]
          buildRamp "red" Dark
            [ "1f1315"; "291415"; "3c181a"; "481a1d"; "541b1f"; "671e22"
              "822025"; "aa2429"; "e5484d"; "f2555a"; "ff9592"; "feecee" ] ]

    let ramp (family: string) (variant: RampVariant) =
        all
        |> List.tryFind (fun r -> r.Family = family && r.Variant = variant)

    let families = all |> List.map (fun r -> r.Family) |> List.distinct
