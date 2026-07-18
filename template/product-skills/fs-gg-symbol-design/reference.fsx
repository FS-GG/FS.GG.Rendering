// fs-gg-symbol-design — runnable in-tree reference for the multi-candidate faithful-frame loop.
//
// Run from the FS.GG.Rendering repo root once the packages/DLLs are available, e.g.:
//   dotnet fsi template/product-skills/fs-gg-symbol-design/reference.fsx
//
// It renders TWO candidates of a visual language for the SAME captured frame — real symbols at real
// board positions over real terrain (NOT a gallery) — lints each, and prints where the PNGs landed.
// The unit of change between rounds is a ChannelMap (data), never the grammar library.
//
// Kept to PURE Scene (positions are floats), so it reaches only the two Symbology packages — never the
// render adapter — and so this reference never out-reaches the skill's own profiles (R-REACH, FS.GG.Rendering#430).
// Swap the two #r "nuget:" lines for #r of the in-tree built DLLs when iterating against local builds.

#r "nuget: FS.GG.UI.Symbology"
#r "nuget: FS.GG.UI.Symbology.Render"

open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open FS.GG.UI.Symbology.Render

// --- The product's own frame vocabulary (shape is the game's; the framework fixes none of it) --------
type Stats = { Side: string; Role: string; Dps: float; Hp: float; HpMax: float; Speed: float; Facing: float }
type Placed = { Stats: Stats; Cell: int * int }   // (col, row) — the product's own board coordinate

let cellSize = 48.0

// A captured "hard" frame: a contested chokepoint, mixed factions overlapping, one low-health unit.
let frameRoster : Placed list =
    [ { Stats = { Side = "blue"; Role = "tank";  Dps = 90.0;  Hp = 40.0; HpMax = 100.0; Speed = 6.0;  Facing = 90.0 };  Cell = (4, 3) }
      { Stats = { Side = "blue"; Role = "scout"; Dps = 20.0;  Hp = 55.0; HpMax = 55.0;  Speed = 14.0; Facing = 45.0 };  Cell = (5, 3) }
      { Stats = { Side = "red";  Role = "tank";  Dps = 120.0; Hp = 80.0; HpMax = 100.0; Speed = 5.0;  Facing = 270.0 }; Cell = (5, 4) }
      { Stats = { Side = "red";  Role = "mob";   Dps = 45.0;  Hp = 12.0; HpMax = 60.0;  Speed = 8.0;  Facing = 200.0 }; Cell = (6, 4) } ]

let walkableCells = [ for col in 2 .. 8 do for row in 2 .. 6 -> (col, row) ]

// --- Candidate A: threat rides stroke WIDTH; fixed body size ------------------------------------------
let mapUnitA (s: Stats) : Token =
    { Symbology.defaultToken with
        R       = 22.0
        Faction = (match s.Side with "blue" -> Ally | "red" -> Enemy | _ -> Neutral)
        Klass   = (match s.Role with "tank" -> Heavy | "scout" -> Scout | _ -> Mobile)
        Threat  = min 1.0 (s.Dps / 120.0)
        Health  = s.Hp / s.HpMax
        Speed   = int (min 4.0 (s.Speed / 4.0))
        Heading = s.Facing }

// --- Candidate B: ONE axis different — threat rides SIZE, width neutral -------------------------------
let mapUnitB (s: Stats) : Token =
    { mapUnitA s with R = 16.0 + 14.0 * (min 1.0 (s.Dps / 120.0)); Threat = 0.0 }

// --- Place a mapped token at its real board centre (what `gallery` refuses to do) ---------------------
let place (m: Stats -> Token) (u: Placed) : Scene =
    let col, row = u.Cell
    let cx, cy = float col * cellSize + cellSize / 2.0, float row * cellSize + cellSize / 2.0
    Symbology.render Grammar.Token { m u.Stats with Cx = cx; Cy = cy }

// --- Terrain in pure Scene: a filled square per walkable cell -----------------------------------------
let tile (col, row) =
    Scene.filledRectangle
        { X = float col * cellSize; Y = float row * cellSize; Width = cellSize; Height = cellSize }
        { Red = 30uy; Green = 32uy; Blue = 38uy; Alpha = 255uy }
let terrain = Scene.group [ for c in walkableCells -> tile c ]

let frameScene (m: Stats -> Token) : Scene =
    Scene.group (terrain :: (frameRoster |> List.map (place m)))

let size = { Width = 960; Height = 640 }

// --- Render + screen each candidate BEFORE any human sees it ------------------------------------------
for (name, m) in [ "A-threat-on-width", mapUnitA; "B-threat-on-size", mapUnitB ] do
    let png    = Render.toPng size (frameScene m) (sprintf "./work/symbol-design/round-01/%s" name)
    let report = Legibility.scoreIn Grammar.Token (frameRoster |> List.map (fun u -> m u.Stats))
    printfn "candidate %-18s -> %s   verdict=%A findings=%d" name png report.Verdict report.Findings.Length
    for f in report.Findings do
        printfn "    %A %A: %s (units %A)" f.Severity f.Channel f.Message f.Units

// Next: assemble the two PNGs into one contact sheet, PRESENT, capture the DIRECTION (not pixels),
// narrow to the winner, then hand off to the fs-gg-symbology single-mapping RENDER->LINT->TWEAK loop.
