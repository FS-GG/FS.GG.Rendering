module Symbology.Tests.LaidLabelTests

// Feature 199 — paragraph layout (alignment / justification / explicit breaks). Exercises the laid-out
// label channel ONLY through the public `token`/`badge`/`ring` surface + the public Scene IR /
// measurement vocabulary (the layout helpers are internal, omitted from the .fsi).
//
// US2 (T023–T027): per-paragraph alignment places drawn lines (Center centred, Leading left, Trailing
//   right) within the per-grammar region; Justify fills wrapped lines with the last paragraph line +
//   single-token lines un-justified; explicit paragraphs produce the authored structure; a single
//   Center all-default paragraph is byte-identical to the equivalent Rich/Plain (B4); over-region content
//   caps/ellipsises/fits under every alignment; empty/whitespace ⇒ no node, no throw.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private baseT =
    { Symbology.defaultToken with
        Cx = 100.0
        Cy = 100.0
        R = 40.0
        Faction = Ally
        Health = 0.6 }

let private regionWidth grammar (r: float) =
    match grammar with
    | "token" -> r * 1.9
    | "badge" -> r * 1.7
    | "ring" -> r * 1.05
    | _ -> r

let private budget grammar =
    match grammar with
    | "token" -> 3
    | _ -> 2

let private grammars =
    [ "token", Symbology.token; "badge", Symbology.badge; "ring", Symbology.ring ]

let private blue = Colors.rgb 24uy 144uy 255uy
let private amber = Colors.rgb 250uy 173uy 20uy
let private bytesOf scene = (SceneCodec.export scene).CanonicalBytes

let rec private collectRuns (scene: Scene) : GlyphRun list =
    scene.Nodes
    |> List.collect (fun node ->
        match node with
        | GlyphRun r -> [ r ]
        | Group scenes -> scenes |> List.collect collectRuns
        | ClipNode(_, s) -> collectRuns s
        | ColorSpaceNode(_, s) -> collectRuns s
        | PerspectiveNode(_, s) -> collectRuns s
        | Translate(_, s) -> collectRuns s
        | _ -> [])

// The label's drawn lines, grouped by baseline Y (ascending), each carrying its glyph nodes.
let private linesByY (scene: Scene) : (float * GlyphRun list) list =
    collectRuns scene
    |> List.groupBy (fun r -> r.Position.Y)
    |> List.sortBy fst

let private rightExtent (nodes: GlyphRun list) =
    nodes |> List.map (fun r -> r.Position.X + (Scene.measureGlyphRun r.Data).Width) |> List.max

let private leftStart (nodes: GlyphRun list) = nodes |> List.map (fun r -> r.Position.X) |> List.min

// styled (non-default) run so a single Center paragraph routes through `laidLabelNodes` (not the reduction).
let private styledRun text = { Symbology.run text with Color = Some blue }
let private laid alignment runs = Some(Symbology.laidLabel [ Symbology.align alignment runs ])

// ---- T023: alignment placement -----------------------------------------------------------------------
[<Tests>]
let alignmentTests =
    testList
        "US2 alignment placement"
        [ for gname, render in grammars do
              test (sprintf "[%s] Leading/Center/Trailing place the line left/centre/right (T023)" gname) {
                  let firstX a = leftStart (collectRuns (render { baseT with Label = laid a [ styledRun "A B" ] }))
                  let xl = firstX Leading
                  let xc = firstX Center
                  let xt = firstX Trailing
                  Expect.isLessThan xl xc "Leading sits left of Center within the region"
                  Expect.isLessThan xc xt "Center sits left of Trailing within the region"
              }

              test (sprintf "[%s] each alignment yields distinct bytes (T023)" gname) {
                  let b a = bytesOf (render { baseT with Label = laid a [ styledRun "A B" ] })
                  Expect.notEqual (b Leading) (b Center) "Leading ≠ Center"
                  Expect.notEqual (b Center) (b Trailing) "Center ≠ Trailing"
                  Expect.notEqual (b Leading) (b Trailing) "Leading ≠ Trailing"
              } ]

// ---- T024: justification -----------------------------------------------------------------------------
[<Tests>]
let justifyTests =
    testList
        "US2 justification"
        [ for gname, render in grammars do
              // A Justify paragraph that wraps fills ≥1 line to the region right edge, while the LAST drawn
              // line stays un-justified (identical to the same content under Leading) — never stretched.
              test (sprintf "[%s] justify fills wrapped lines; last line un-justified (T024)" gname) {
                  let content = [ styledRun "aa bb cc dd ee ff gg hh ii jj kk" ]
                  let just = render { baseT with Label = laid Justify content }
                  let lead = render { baseT with Label = laid Leading content }
                  let right = baseT.Cx + regionWidth gname baseT.R / 2.0

                  let jext = linesByY just |> List.map (snd >> rightExtent)
                  let lext = linesByY lead |> List.map (snd >> rightExtent)

                  let filled = jext |> List.filter (fun e -> abs (e - right) < 1e-3)
                  Expect.isGreaterThan filled.Length 0 "≥1 justified line fills to the region right edge (FR-007)"
                  Expect.floatClose Accuracy.high (List.last jext) (List.last lext) "the last drawn line is un-justified (matches Leading, FR-008)"
              }

              // A single-token line has no distributable inter-word gap ⇒ falls back to the base alignment.
              test (sprintf "[%s] a single-token justify line falls back to base alignment (T024)" gname) {
                  let oneTok a = bytesOf (render { baseT with Label = laid a [ styledRun "SOLO" ] })
                  Expect.equal (oneTok Justify) (oneTok Leading) "a single-token line is never stretched (FR-008)"
              } ]

// ---- T025: explicit structure + default equivalence --------------------------------------------------
[<Tests>]
let structureTests =
    testList
        "US2 explicit structure / default equivalence"
        [ for gname, render in grammars do
              // B4 / SC-003: a single Center all-default paragraph reduces to the Rich/Plain path verbatim.
              test (sprintf "[%s] single Center all-default paragraph ≡ Rich ≡ Plain (B4, T025)" gname) {
                  let laidL = render { baseT with Label = Some(Symbology.laidLabel [ Symbology.paragraph [ Symbology.run "HMR-7" ] ]) }
                  let rich = render { baseT with Label = Some(LabelText.Rich [ Symbology.run "HMR-7" ]) }
                  let plain = render { baseT with Label = Some(LabelText.Plain "HMR-7") }
                  Expect.equal (bytesOf laidL) (bytesOf rich) "default Center single paragraph = the Rich flow (FR-004)"
                  Expect.equal (bytesOf laidL) (bytesOf plain) "and = the Plain flow (layered zero drift, SC-003)"
              }

              // FR-002: explicit paragraphs produce the authored structure; paragraphs may differ in alignment.
              test (sprintf "[%s] two paragraphs with different alignment produce distinct lines (T025)" gname) {
                  let two =
                      render
                          { baseT with
                              Label =
                                  Some(
                                      Symbology.laidLabel
                                          [ Symbology.align Leading [ styledRun "AA" ]
                                            Symbology.align Trailing [ styledRun "BB" ] ]
                                  ) }

                  let lines = linesByY two
                  Expect.equal lines.Length 2 "two paragraphs ⇒ two drawn lines (FR-002)"
                  let topX = leftStart (snd lines.[0])
                  let botX = leftStart (snd lines.[1])
                  Expect.notEqual topX botX "the Leading and Trailing paragraphs start at different x (per-paragraph alignment)"
              } ]

// ---- T026: fit / cap under every alignment -----------------------------------------------------------
[<Tests>]
let fitCapTests =
    testList
        "US2 fit / cap under every alignment"
        [ for gname, render in grammars do
              for a in [ Leading; Center; Trailing; Justify ] do
                  test (sprintf "[%s] %A over-numerous content caps to budget + ellipsis, ≤ region (T026)" gname a) {
                      let many = render { baseT with Label = laid a [ styledRun "L1\nL2\nL3\nL4\nL5\nL6" ] }
                      let lines = linesByY many
                      Expect.equal lines.Length (budget gname) (sprintf "drawn line count capped to the %s budget (SC-005)" gname)

                      let left = baseT.Cx - regionWidth gname baseT.R / 2.0
                      let right = baseT.Cx + regionWidth gname baseT.R / 2.0

                      for r in collectRuns many do
                          let w = (Scene.measureGlyphRun r.Data).Width
                          Expect.isGreaterThanOrEqual r.Position.X (left - 1e-6) "no overflow past the region left edge"
                          Expect.isLessThanOrEqual (r.Position.X + w) (right + 1e-6) "no overflow past the region right edge (SC-005)"

                      let lastLine = lines |> List.last |> snd
                      let lastText = lastLine |> List.map (fun r -> r.Data.Text) |> String.concat ""
                      Expect.stringEnds lastText "…" "the last drawn line is ellipsised (surplus signalled, SC-005)"
                  } ]

// ---- T027: empty / whitespace ------------------------------------------------------------------------
[<Tests>]
let emptyTests =
    testList
        "US2 empty / whitespace (FR-009)"
        [ for gname, render in grammars do
              for label, desc in
                  [ Some(Symbology.laidLabel []), "Laid []"
                    Some(Symbology.laidLabel [ Symbology.paragraph [] ]), "Laid of an empty paragraph"
                    Some(Symbology.laidLabel [ Symbology.align Justify [ { Symbology.run "   " with Color = Some blue }; { Symbology.run "\t" with Underline = Some true } ] ]),
                    "Laid of all-whitespace decorated runs" ] do
                  test (sprintf "[%s] %s ⇒ no glyph node, no throw (T027)" gname desc) {
                      let kinds = render { baseT with Label = label } |> Scene.describe
                      Expect.isFalse (List.contains GlyphRunElement kinds) (sprintf "%s is equivalent to no label regardless of alignment/decoration (FR-009)" desc)
                  } ]
