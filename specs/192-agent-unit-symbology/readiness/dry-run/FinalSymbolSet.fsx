// Final symbol-set module (M5 / T040) — the APPROVED per-game mapping emitted on loop approval.
// Pure, drawing-producing functions: a roster stat record -> the fixed-grammar Token -> a Scene board.
// The grammar (FS.GG.UI.Symbology) is fixed; THIS file is the editable artifact the loop converged on.
//
// Build the two projects first, then: dotnet fsi FinalSymbolSet.fsx
#r "../../../../src/Scene/bin/Debug/net10.0/FS.GG.UI.Scene.dll"
#r "../../../../src/Symbology/bin/Debug/net10.0/FS.GG.UI.Symbology.dll"

open FS.GG.UI.Scene
open FS.GG.UI.Symbology

type UnitStats =
    { Name: string
      Side: string
      Role: string
      Dps: float
      Hp: float
      HpMax: float
      Speed: float
      Armor: float
      Suspected: bool
      Facing: float }

let private factionOf side =
    match side with
    | "blue" -> Ally
    | "red" -> Enemy
    | _ -> Neutral

let private klassOf role =
    match role with
    | "tank" -> Heavy
    | "scout" -> Scout
    | _ -> Mobile

let private sigilOf role =
    match role with
    | "tank" -> Ring
    | "scout" -> Fang
    | _ -> Bolt

/// The approved channel assignment. Urgent state (DPS) is redundant across stroke WIDTH (threat) and
/// interior GRADIENT (charge); team rides the saturated faction HUE; suspected contacts ride the DASH
/// (inspection-only); armour rides the corner MOUNT (inspection). Faction hue and inspection state
/// never share the hue channel.
let mapUnit (u: UnitStats) : Token =
    { Symbology.defaultToken with
        R = 30.0
        Faction = factionOf u.Side
        Klass = klassOf u.Role
        Sigil = sigilOf u.Role
        Threat = min 1.0 (u.Dps / 120.0)
        Charge = min 1.0 (u.Dps / 130.0)
        Speed = int (min 4.0 (u.Speed / 4.0))
        Health = u.Hp / u.HpMax
        State = (if u.Suspected then Suspected else Confirmed)
        Shield = u.Armor > 40.0
        Heading = u.Facing }

/// The approved board: a reproducible 4-wide gallery of the mapped roster.
let board (roster: UnitStats list) : Scene =
    Symbology.gallery 4 95.0 (roster |> List.map mapUnit)
