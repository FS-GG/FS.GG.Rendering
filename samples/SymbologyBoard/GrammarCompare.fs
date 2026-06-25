module SymbologyBoard.GrammarCompare

// T026 [US3] (OPTIONAL — not a contract) A lightweight grammar-compare demo: render the approved roster as
// a gallery in each grammar (Token / Badge / Ring), stacked in bands so a designer can A/B which form
// factor reads best (quickstart §7). Pure: equal inputs => equal Scene => byte-reproducible board.

open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open SymbologyBoard.Roster

/// The approved roster drawn as three stacked galleries — one band per grammar. Each band reuses the same
/// `Token` set and the same grid layout, so only the grammar (the drawing) differs between bands.
let compareBoard (spacing: float) : Scene =
    let tokens = roster |> List.map mapUnit
    let cols = 4
    let rows = (List.length tokens + cols - 1) / cols
    let bandGap = spacing * (float rows + 0.5)

    [ Grammar.Token; Grammar.Badge; Grammar.Ring ]
    |> List.mapi (fun band g ->
        tokens
        |> List.mapi (fun i tk ->
            let row = i / cols
            let col = i % cols
            let cx = spacing * (float col + 0.5)
            let cy = float band * bandGap + spacing * (float row + 0.5)
            Symbology.render g { tk with Cx = cx; Cy = cy }))
    |> List.concat
    |> Scene.group
