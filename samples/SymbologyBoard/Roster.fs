module SymbologyBoard.Roster

// Feature 193 (M6): the APPROVED M5 per-game mapping, brought in-tree from
// specs/192-agent-unit-symbology/readiness/dry-run/FinalSymbolSet.fsx and compiled unchanged. The fixed
// grammar (FS.GG.UI.Symbology) is NOT re-opened — this file only consumes its public surface to turn a
// roster stat row into the grammar's `Token`, plus a pure `motionOf` overlay chosen from already-approved
// motion channels. Pure throughout: equal stats ⇒ equal Token ⇒ equal Scene ⇒ equal canonical bytes.

open FS.GG.UI.Symbology

/// The per-game roster row the approved mapping consumes (ported verbatim from FinalSymbolSet.fsx).
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

/// The approved channel assignment (FinalSymbolSet.fsx, unchanged). Urgent state (DPS) is redundant across
/// stroke WIDTH (threat) and interior GRADIENT (charge); team rides the saturated faction HUE; suspected
/// contacts ride the DASH (inspection-only); armour rides the corner MOUNT (inspection). Faction hue and
/// inspection state never share the hue channel.
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

/// The deterministic motion overlay for a unit (D3): suspected contacts blink (inspection rhythm); a
/// high-threat unit pulses; otherwise heavies spin in place and everything else reads as moving. Pure;
/// chosen from already-approved `Motion` channels, no grammar change.
let motionOf (u: UnitStats) (token: Token) : Motion =
    if u.Suspected then Blink
    elif token.Threat >= 0.66 then Pulse
    else
        match token.Klass with
        | Heavy -> Spin
        | Mobile
        | Scout -> Moving

/// The fixed approved roster (8 units) — a stable per-game set spanning both factions, all three klasses,
/// suspected/confirmed, shielded/unshielded, and the full threat range so every channel is exercised.
let roster: UnitStats list =
    [ { Name = "Vanguard"; Side = "blue"; Role = "tank"; Dps = 48.0; Hp = 900.0; HpMax = 1000.0; Speed = 6.0; Armor = 75.0; Suspected = false; Facing = 0.0 }
      { Name = "Outrider"; Side = "blue"; Role = "scout"; Dps = 60.0; Hp = 320.0; HpMax = 400.0; Speed = 14.0; Armor = 20.0; Suspected = false; Facing = 45.0 }
      { Name = "Lancer"; Side = "blue"; Role = "striker"; Dps = 95.0; Hp = 540.0; HpMax = 600.0; Speed = 9.0; Armor = 35.0; Suspected = false; Facing = 90.0 }
      { Name = "Bulwark"; Side = "red"; Role = "tank"; Dps = 40.0; Hp = 1100.0; HpMax = 1200.0; Speed = 5.0; Armor = 90.0; Suspected = false; Facing = 180.0 }
      { Name = "Stalker"; Side = "red"; Role = "scout"; Dps = 72.0; Hp = 280.0; HpMax = 360.0; Speed = 16.0; Armor = 15.0; Suspected = true; Facing = 225.0 }
      { Name = "Reaver"; Side = "red"; Role = "striker"; Dps = 118.0; Hp = 460.0; HpMax = 600.0; Speed = 11.0; Armor = 30.0; Suspected = false; Facing = 270.0 }
      { Name = "Wraith"; Side = "red"; Role = "striker"; Dps = 105.0; Hp = 300.0; HpMax = 500.0; Speed = 12.0; Armor = 25.0; Suspected = true; Facing = 315.0 }
      { Name = "Sentry"; Side = "blue"; Role = "support"; Dps = 28.0; Hp = 480.0; HpMax = 520.0; Speed = 8.0; Armor = 48.0; Suspected = false; Facing = 135.0 } ]
