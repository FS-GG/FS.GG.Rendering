// fs-gg-symbology reference recipe — roster -> ChannelMap -> gallery -> Render.toPng -> read PNG back.
// In-tree variant: #r the built Debug DLLs (build the two projects first). For a packaged product,
// replace the #r lines with `#r "nuget: FS.GG.UI.Symbology"` and `#r "nuget: FS.GG.UI.Symbology.Render"`.
#r "../../../src/Scene/bin/Debug/net10.0/FS.GG.UI.Scene.dll"
#r "../../../src/SkiaViewer/bin/Debug/net10.0/FS.GG.UI.SkiaViewer.dll"
#r "../../../src/Symbology/bin/Debug/net10.0/FS.GG.UI.Symbology.dll"
#r "../../../src/Symbology.Render/bin/Debug/net10.0/FS.GG.UI.Symbology.Render.dll"

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

let roster =
    [ { Side = "blue"; Role = "tank";  Dps = 40.0;  Hp = 90.0;  HpMax = 100.0; Speed = 2.0; Armor = 50.0; Facing = 0.0 }
      { Side = "blue"; Role = "scout"; Dps = 70.0;  Hp = 30.0;  HpMax = 60.0;  Speed = 14.0; Armor = 5.0; Facing = 0.8 }
      { Side = "red";  Role = "dps";   Dps = 115.0; Hp = 50.0;  HpMax = 80.0;  Speed = 8.0; Armor = 10.0; Facing = 3.1 } ]

// --- MAP: the editable per-game ChannelMap (data, NOT library internals). Tweak THIS each round. ---
let mapUnit (u: UnitStats) : Token =
    { Symbology.defaultToken with
        R = 28.0
        Faction = (match u.Side with "blue" -> Ally | "red" -> Enemy | _ -> Neutral)
        Klass = (match u.Role with "tank" -> Heavy | "scout" -> Scout | _ -> Mobile)
        Sigil = (match u.Role with "tank" -> Ring | "scout" -> Fang | _ -> Bolt)
        Threat = min 1.0 (u.Dps / 120.0)
        Health = u.Hp / u.HpMax
        Speed = int (min 4.0 (u.Speed / 4.0))
        Shield = u.Armor > 30.0
        Heading = u.Facing }

// --- RENDER: build the board and rasterise through the fail-loud public bridge ---
let board = Symbology.gallery 3 90.0 (roster |> List.map mapUnit)
let outDir = Path.Combine(Path.GetTempPath(), "fs-gg-symbology-reference")
Directory.CreateDirectory outDir |> ignore
let png = Render.toPng { Width = 300; Height = 130 } board outDir

printfn "board PNG: %s (%d bytes)" png (FileInfo png).Length
// -> READ `png` BACK, CRITIQUE at the target size against the legibility rules, TWEAK mapUnit ONLY, repeat.
