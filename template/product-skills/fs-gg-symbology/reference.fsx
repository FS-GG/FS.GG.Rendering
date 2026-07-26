// fs-gg-symbology reference recipe — the WHOLE design loop:
// roster -> ChannelMap -> LINT (Legibility.score) -> TWEAK -> re-lint -> galleryIn x3 -> Render.toPng.
// Packaged-product recipe: reference the symbology packages from NuGet, PINNED to FsGgUiVersion
// (Directory.Packages.props). An unpinned `#r "nuget:"` resolves the latest PUBLISHED package, which
// can PREDATE the recipe: a library behaviour change that has merged but not yet shipped then makes the
// recipe throw against a library that never saw it (that is exactly how this recipe was found broken).
// Pinning resolves the exact library the recipe was written against. In this template the pin is held
// in step with FsGgUiVersion by scripts/validate-version-coherence.fsx (rule `symbology-recipe-pin-skew`);
// in a scaffolded product, bump these to your FsGgUiVersion when you upgrade. SkiaSharp and its native
// assets arrive transitively behind FS.GG.UI.SkiaViewer.
#r "nuget: FS.GG.UI.Scene, 0.21.1"
#r "nuget: FS.GG.UI.SkiaViewer, 0.21.1"
#r "nuget: FS.GG.UI.Symbology, 0.21.1"
#r "nuget: FS.GG.UI.Symbology.Render, 0.21.1"

open System.IO
open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open FS.GG.UI.Symbology.Render

// --- INTAKE: a unit roster with per-unit stats ---
type UnitStats =
    { Side: string
      Role: string
      Dps: float
      Hp: float
      HpMax: float
      Speed: float
      Armor: float
      Facing: float }

// Five units spanning the speed range. That span is what will overload a channel below — a roster
// wide enough to exercise the grammar is also wide enough to break it.
//
// ORDER IS LOAD-BEARING for the lint below. The five speeds land in five distinct bands, one unit
// each, so no band is rarer than another and the linter breaks the tie on first appearance: the LAST
// band introduced is the excess one. Listed slowest-to-fastest, that is the fastest unit, index 4.
// Reorder the roster and a different index gets named (the lint still fires; the guard below will
// tell you which unit it expected).
let roster =
    [ { Side = "blue"; Role = "tank";  Dps = 40.0;  Hp = 90.0; HpMax = 100.0; Speed = 2.0;  Armor = 50.0; Facing = 0.0 }
      { Side = "blue"; Role = "dps";   Dps = 95.0;  Hp = 44.0; HpMax = 80.0;  Speed = 6.0;  Armor = 20.0; Facing = 1.9 }
      { Side = "red";  Role = "dps";   Dps = 115.0; Hp = 50.0; HpMax = 80.0;  Speed = 9.0;  Armor = 10.0; Facing = 3.1 }
      { Side = "red";  Role = "scout"; Dps = 70.0;  Hp = 30.0; HpMax = 60.0;  Speed = 14.0; Armor = 5.0;  Facing = 0.8 }
      { Side = "grey"; Role = "scout"; Dps = 25.0;  Hp = 55.0; HpMax = 55.0;  Speed = 20.0; Armor = 35.0; Facing = 4.7 } ]

// `Threat` is Ordered with capacity 4 (Legibility.table), and it carries a float — so the RAW ramp
// `Dps / 120.0` would spend one distinct stroke width per unit and overload the channel. Quantising is
// the map's job: a float channel that is read as a rank must be banded before it reaches the grammar.
// (This is the same tweak the loop below performs on `Speed`, done up front so the recipe demonstrates
// exactly ONE overload at a time.)
let threatBand dps =
    if dps >= 100.0 then 1.0
    elif dps >= 75.0 then 0.75
    elif dps >= 50.0 then 0.5
    else 0.25

// --- MAP: the editable per-game ChannelMap (data, NOT library internals). Tweak THIS each round. ---
// Only the speed banding varies between rounds below, so it is the one parameter lifted out.
let mapUnitWith (speedBand: float -> int) (u: UnitStats) : Token =
    { Symbology.defaultToken with
        R = 28.0
        Faction = (match u.Side with "blue" -> Ally | "red" -> Enemy | _ -> Neutral)
        Klass = (match u.Role with "tank" -> Heavy | "scout" -> Scout | _ -> Mobile)
        Sigil = (match u.Role with "tank" -> Ring | "scout" -> Fang | _ -> Bolt)
        Threat = threatBand u.Dps
        Health = u.Hp / u.HpMax
        Speed = speedBand u.Speed
        Shield = u.Armor > 30.0
        Heading = u.Facing }

// --- CRITIQUE (a) LINT: the mechanical backstop. Pure, no rendering, no eyeballs. ---
let lint label (tokens: Token list) =
    let report = Legibility.score tokens
    printfn "%s: %A" label report.Verdict

    for f in report.Findings do
        printfn "  %A %A — %s" f.Severity f.Channel f.Message
        printfn "    units: %A" f.Units

    report

// Round 1. `Speed` is Ordered with capacity 4 (Legibility.table), but this banding yields five
// distinct pip counts (0..4) across the roster — one more level than the eye reliably ranks.
let round1 = roster |> List.map (mapUnitWith (fun s -> int (min 4.0 (s / 4.0))))
let report1 = lint "round 1" round1
// -> round 1: HasWarnings
//      Warning Speed — Speed overloaded: 5 distinct levels used, capacity 4
//        units: [4]
//
// `units` names the units holding the levels PAST capacity, not every unit on the board. Here that
// is the 20 m/s scout alone: it is the only unit in the fifth pip band, so it is the only unit the
// re-map below has to move. The other four already fit the four ranks the eye can separate.

// The recipe teaches the loop by SHOWING a warning. If a capacity change ever un-fires this one,
// say so instead of quietly demonstrating the backstop with nothing for it to catch.
if report1.Verdict <> Legibility.HasWarnings then
    failwith "reference recipe: round 1 was supposed to score HasWarnings"

match report1.Findings |> List.tryFind (fun f -> f.Channel = Legibility.Speed) with
| Some f when f.Severity = Legibility.Severity.Warning && f.Units = [ 4 ] -> ()
| other -> failwithf "reference recipe: round 1 was supposed to warn that unit 4 alone overloads Speed; got %A" other

// --- TWEAK: a WARNING is fixed in the ChannelMap, never in the library. Widen the bands so the
// roster lands on four pip counts (0..3) instead of five. No unit is dropped; the rank coarsens. ---
let mapUnit = mapUnitWith (fun s -> int (min 3.0 (s / 6.0)))

let tokens = roster |> List.map mapUnit
let report2 = lint "round 2 (after tweak)" tokens
// -> round 2 (after tweak): Clean

if report2.Verdict <> Legibility.Clean then
    failwith "reference recipe: the tweak was supposed to clear every finding"

// --- RENDER: the SAME unchanged `mapUnit` drives all three grammars. Selecting a grammar changes
// the drawing, never the per-game ChannelMap — so the A/B is a one-word edit, not a re-mapping. ---
let outDir = Path.Combine(Path.GetTempPath(), "fs-gg-symbology-reference")

for grammar in [ Grammar.Token; Grammar.Badge; Grammar.Ring ] do
    let board = Symbology.galleryIn grammar 3 90.0 tokens
    // One directory per grammar: `toPng` names the file after the scene, so sibling boards would
    // otherwise be distinguishable only by hash.
    let dir = Path.Combine(outDir, (sprintf "%A" grammar).ToLowerInvariant())
    Directory.CreateDirectory dir |> ignore
    let png = Render.toPng { Width = 300; Height = 220 } board dir
    printfn "%-6A board PNG: %s (%d bytes)" grammar png (FileInfo png).Length

// -> READ the PNGs BACK, CRITIQUE at the target size against the legibility rules, TWEAK mapUnit
//    ONLY, repeat. The linter above is the backstop; your eyes are still the judge.
