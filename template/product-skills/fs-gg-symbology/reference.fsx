// fs-gg-symbology reference recipe — the WHOLE design loop:
// roster -> ChannelMap -> LINT (Legibility.score) -> TWEAK -> re-lint -> galleryIn x3 -> Render.toPng.
// Packaged-product recipe: reference the symbology packages from NuGet. For version coherence
// with this product, pin each to your FsGgUiVersion (Directory.Packages.props), e.g.
// `#r "nuget: FS.GG.UI.Symbology, <FsGgUiVersion>"`; unpinned resolves the latest published set.
// SkiaSharp and its native assets arrive transitively behind FS.GG.UI.SkiaViewer.
#r "nuget: FS.GG.UI.Scene"
#r "nuget: FS.GG.UI.SkiaViewer"
#r "nuget: FS.GG.UI.Symbology"
#r "nuget: FS.GG.UI.Symbology.Render"

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
let roster =
    [ { Side = "blue"; Role = "tank";  Dps = 40.0;  Hp = 90.0; HpMax = 100.0; Speed = 2.0;  Armor = 50.0; Facing = 0.0 }
      { Side = "blue"; Role = "dps";   Dps = 95.0;  Hp = 44.0; HpMax = 80.0;  Speed = 6.0;  Armor = 20.0; Facing = 1.9 }
      { Side = "red";  Role = "dps";   Dps = 115.0; Hp = 50.0; HpMax = 80.0;  Speed = 9.0;  Armor = 10.0; Facing = 3.1 }
      { Side = "red";  Role = "scout"; Dps = 70.0;  Hp = 30.0; HpMax = 60.0;  Speed = 14.0; Armor = 5.0;  Facing = 0.8 }
      { Side = "grey"; Role = "scout"; Dps = 25.0;  Hp = 55.0; HpMax = 55.0;  Speed = 20.0; Armor = 35.0; Facing = 4.7 } ]

// --- MAP: the editable per-game ChannelMap (data, NOT library internals). Tweak THIS each round. ---
// Only the speed banding varies between rounds below, so it is the one parameter lifted out.
let mapUnitWith (speedBand: float -> int) (u: UnitStats) : Token =
    { Symbology.defaultToken with
        R = 28.0
        Faction = (match u.Side with "blue" -> Ally | "red" -> Enemy | _ -> Neutral)
        Klass = (match u.Role with "tank" -> Heavy | "scout" -> Scout | _ -> Mobile)
        Sigil = (match u.Role with "tank" -> Ring | "scout" -> Fang | _ -> Bolt)
        Threat = min 1.0 (u.Dps / 120.0)
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
//        units: [0; 1; 2; 3; 4]

// The recipe teaches the loop by SHOWING a warning. If a capacity change ever un-fires this one,
// say so instead of quietly demonstrating the backstop with nothing for it to catch.
if
    report1.Verdict <> Legibility.HasWarnings
    || not (report1.Findings |> List.exists (fun f -> f.Channel = Legibility.Speed))
then
    failwith "reference recipe: round 1 was supposed to overload the Speed channel"

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
