module Symbology.Tests.RichLabelTests

// Feature 198 — rich-text label runs (per-run colour / weight / size). Exercises the styled label channel
// ONLY through the public `token`/`badge`/`ring` surface + the public Scene IR / measurement vocabulary
// (the run layout helpers are internal, omitted from the .fsi).
//
// US1 (T012/T016): all-default `Rich` ≡ `Plain` byte-identity; per-run colour/weight/size are observable
//   in the emitted nodes; an author-supplied colour survives unchanged into the node (B14/FR-013); the
//   measurer-optional pure path emits styled nodes deterministically and never throws (FR-010/B11).
// US2 (T017/T018/T019): an over-wide styled run wraps/shrinks ≤ region (no mid-glyph clip); an over-budget
//   styled label caps to the grammar budget with a trailing ellipsis; every drawn segment measures ≤ the
//   region; a line mixing run sizes/weights has height = its tallest run on a common baseline; empty /
//   all-whitespace / empty-run labels draw nothing and never throw (FR-006/FR-007/SC-005).

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

let private firstBaseline grammar (t: Token) =
    match grammar with
    | "token" -> t.Cy + t.R * 1.5
    | "badge" -> t.Cy + t.R * 1.42
    | "ring" -> t.Cy + t.R * 0.52
    | _ -> t.Cy

let private grammars =
    [ "token", Symbology.token; "badge", Symbology.badge; "ring", Symbology.ring ]

// Ordered glyph-run nodes, preserving draw order so segment order / stacking can be asserted.
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

let private bytesOf scene = (SceneCodec.export scene).CanonicalBytes
let private blue = Colors.rgb 24uy 144uy 255uy
let private amber = Colors.rgb 250uy 173uy 20uy

// ---- US1: zero-drift + per-run channel presence ------------------------------------------------------

[<Tests>]
let zeroDriftTests =
    testList
        "US1 rich zero-drift"
        [ for gname, render in grammars do
              // B3 / FR-002: a single default-styled run is byte-identical to the equivalent plain label.
              test (sprintf "[%s] Rich [run \"HMR-7\"] (all-default) ≡ Plain \"HMR-7\"" gname) {
                  let plain = render { baseT with Label = Some(LabelText.Plain "HMR-7") }
                  let rich = render { baseT with Label = Some(LabelText.Rich [ Symbology.run "HMR-7" ]) }
                  Expect.equal (bytesOf rich) (bytesOf plain) "an all-default single run reproduces the plain label byte-for-byte (FR-002)"
              }

              // A multi-run all-default Rich label ≡ the joined plain text.
              test (sprintf "[%s] Rich of all-default runs ≡ the joined Plain label" gname) {
                  let rich = render { baseT with Label = Some(LabelText.Rich [ Symbology.run "BRAVO"; Symbology.run " ac12" ]) }
                  let plain = render { baseT with Label = Some(LabelText.Plain "BRAVO ac12") }
                  Expect.equal (bytesOf rich) (bytesOf plain) "default runs join to the equivalent plain label (FR-002)"
              } ]

[<Tests>]
let channelPresenceTests =
    testList
        "US1 rich per-run channel"
        [ for gname, render in grammars do
              // B4 / FR-003: two-or-more styled runs draw in reading order, each in its own weight/size/colour.
              test (sprintf "[%s] a 2-run styled label emits ≥2 nodes carrying the runs' weight/size/colour" gname) {
                  let styled =
                      render
                          { baseT with
                              Label =
                                  Some(
                                      LabelText.Rich
                                          [ { Symbology.run "BRAVO" with Weight = Some 700; Color = Some blue }
                                            { Symbology.run "x9" with Scale = Some 0.6; Color = Some amber } ]
                                  ) }

                  let runs = collectRuns styled
                  Expect.isGreaterThanOrEqual runs.Length 2 "≥2 contiguous-style segments ⇒ ≥2 glyph-run nodes (B4)"
                  // weights / scales / colours all present across the emitted nodes
                  let weights = runs |> List.map (fun r -> r.Data.Font.Weight) |> List.distinct
                  Expect.contains weights (Some 700) "the bold run's weight rides FontSpec.Weight (FR-003)"
                  let sizes = runs |> List.map (fun r -> r.Data.Font.Size) |> List.distinct
                  Expect.isGreaterThan sizes.Length 1 "mixed Scale ⇒ distinct per-run sizes (FR-003)"
                  let fills = runs |> List.choose (fun r -> r.Paint.Fill)
                  Expect.contains fills blue "the blue run colour is present in an emitted node (FR-003)"
                  Expect.contains fills amber "the amber run colour is present in an emitted node (FR-003)"
              }

              // B5 / SC-002: same characters, different run styling ⇒ different canonical bytes.
              test (sprintf "[%s] same chars, different run styling ⇒ different bytes (style is a channel)" gname) {
                  let styled =
                      render { baseT with Label = Some(LabelText.Rich [ { Symbology.run "ALFA" with Weight = Some 700; Color = Some blue } ]) }
                  let plain = render { baseT with Label = Some(LabelText.Plain "ALFA") }
                  Expect.notEqual (bytesOf styled) (bytesOf plain) "run styling observably alters the bytes (B5/SC-002)"
              }

              // B14 / FR-013: an author-supplied colour is used as-is — never re-mapped or rejected.
              test (sprintf "[%s] an author-supplied run colour survives unchanged into the node (B14)" gname) {
                  let custom = Colors.rgb 17uy 99uy 211uy
                  let styled = render { baseT with Label = Some(LabelText.Rich [ { Symbology.run "CC" with Color = Some custom } ]) }
                  let fills = collectRuns styled |> List.choose (fun r -> r.Paint.Fill)
                  Expect.contains fills custom "the exact author colour is the node's fill — not re-mapped/rejected (FR-013)"
              } ]

[<Tests>]
let pureFallbackTests =
    testList
        "US1 rich measurer-optional pure path (FR-010)"
        [ for gname, render in grammars do
              // B11 / FR-010: no measurer installed in this project — a styled label still emits nodes,
              // deterministically (render-twice byte-equal), never throws, carrying pure-fallback evidence.
              test (sprintf "[%s] styled label on the no-measurer path: nodes emitted, deterministic, no throw" gname) {
                  let mk () =
                      render { baseT with Label = Some(LabelText.Rich [ { Symbology.run "PURE" with Weight = Some 600; Color = Some blue } ]) }

                  let runs = mk () |> collectRuns
                  Expect.isGreaterThan runs.Length 0 "the pure path still emits the styled node(s) (FR-010)"
                  Expect.equal (bytesOf (mk ())) (bytesOf (mk ())) "styled layout is a deterministic function of Token (FR-009)"
                  for r in runs do
                      Expect.equal r.Data.FallbackMode PureFallbackMode "with no measurer the node is the deterministic pure fallback (FR-010)"
              } ]

// ---- US2: fit / cap / mixed-height / safe-degenerate -------------------------------------------------

[<Tests>]
let fitCapTests =
    testList
        "US2 rich fit / cap"
        [ for gname, render in grammars do
              // B6 / SC-005: an over-wide styled run wraps/shrinks; every drawn segment measures ≤ region.
              test (sprintf "[%s] an over-wide styled run is fitted ≤ region, no mid-glyph clip" gname) {
                  let wide =
                      render
                          { baseT with
                              Label = Some(LabelText.Rich [ { Symbology.run "ALPHA BRAVO CHARLIE DELTA ECHO FOXTROT" with Weight = Some 700 } ]) }

                  let region = regionWidth gname baseT.R
                  let runs = collectRuns wide
                  Expect.isGreaterThan runs.Length 0 "an over-wide styled run still draws (fitted)"
                  for r in runs do
                      let drawn = (Scene.measureGlyphRun r.Data).Width
                      Expect.isLessThanOrEqual drawn (region + 1e-6) (sprintf "segment %A width %f ≤ region %f (FR-006)" r.Data.Text drawn region)
              }

              // B6 / SC-005: an over-budget styled label caps to the grammar budget, last line ellipsised.
              test (sprintf "[%s] an over-budget styled label caps to the budget with a trailing ellipsis" gname) {
                  let many =
                      render
                          { baseT with
                              Label = Some(LabelText.Rich [ { Symbology.run "L1\nL2\nL3\nL4\nL5\nL6" with Weight = Some 700 } ]) }

                  let runs = collectRuns many
                  Expect.equal runs.Length (budget gname) (sprintf "drawn line count capped to the %s budget (SC-005)" gname)
                  Expect.stringEnds (List.last runs).Data.Text "…" "the last kept styled line is ellipsised (SC-005)"
              }

              // A single unbreakable over-wide styled word degrades to one fitted line (no overflow).
              test (sprintf "[%s] an unbreakable over-wide styled word degrades to one fitted line" gname) {
                  let runs =
                      render
                          { baseT with
                              Label = Some(LabelText.Rich [ { Symbology.run "THIS-CALLSIGN-IS-FAR-TOO-LONG-TO-FIT-1234567890" with Weight = Some 700 } ]) }
                      |> collectRuns

                  Expect.equal runs.Length 1 "no wrap point ⇒ one fitted line"
                  let drawn = (Scene.measureGlyphRun (List.head runs).Data).Width
                  Expect.isLessThanOrEqual drawn (regionWidth gname baseT.R + 1e-6) "the unbreakable word is shrunk/ellipsised ≤ region (FR-006)"
              } ]

[<Tests>]
let mixedSizeTests =
    testList
        "US2 rich mixed-size line geometry"
        [ for gname, render in grammars do
              // B7 / FR-006: runs of mixed size on ONE line share a common baseline; the first line keeps
              // the spec-197 baseline anchor; lines never overlap vertically.
              test (sprintf "[%s] mixed-size runs on one line share the first-line baseline (common baseline)" gname) {
                  let styled =
                      render
                          { baseT with
                              Label =
                                  Some(
                                      LabelText.Rich
                                          [ { Symbology.run "BIG" with Scale = Some 1.0; Weight = Some 700 }
                                            { Symbology.run "sm" with Scale = Some 0.5 } ]
                                  ) }

                  let runs = collectRuns styled
                  let ys = runs |> List.map (fun r -> r.Position.Y) |> List.distinct
                  Expect.equal ys.Length 1 "both segments of the single line share one baseline Y (common baseline, B7)"
                  Expect.floatClose Accuracy.high (List.head ys) (firstBaseline gname baseT) "the line keeps the spec-197 first-line baseline (zero-drift anchor)"
              }

              // A mixed-size MULTI-line styled label stacks downward; the tall line's height drives the gap.
              test (sprintf "[%s] mixed-size stacked lines are non-overlapping and stack downward" gname) {
                  let styled =
                      render
                          { baseT with
                              Label =
                                  Some(
                                      LabelText.Rich [ { Symbology.run "TALL\nlow" with Scale = Some 1.0; Weight = Some 700 } ]
                                  ) }

                  let ys = collectRuns styled |> List.map (fun r -> r.Position.Y)
                  Expect.equal ys (List.sort ys) "lines stack downward (monotonically increasing baseline Y)"
                  Expect.equal (List.distinct ys).Length ys.Length "each stacked line has a distinct baseline (non-overlapping, B7)"
              } ]

[<Tests>]
let safeDegenerateTests =
    testList
        "US2 rich safe / empty (FR-007)"
        [ for gname, render in grammars do
              // B8 / FR-007: Rich [], all-empty/whitespace runs, and Plain "" draw no node and never throw.
              for label, desc in
                  [ Some(LabelText.Rich []), "Rich []"
                    Some(LabelText.Rich [ { Symbology.run "   " with Color = Some blue }; Symbology.run "\t" ]), "Rich of whitespace runs"
                    Some(LabelText.Plain ""), "Plain \"\""
                    Some(LabelText.Plain "  \n \n") , "Plain whitespace/newlines" ] do
                  test (sprintf "[%s] %s ⇒ no glyph node, no throw" gname desc) {
                      let kinds = render { baseT with Label = label } |> Scene.describe
                      Expect.isFalse (List.contains GlyphRunElement kinds) (sprintf "%s is equivalent to no label (FR-007)" desc)
                  }

              // Interior empty/whitespace runs normalise (no wasted gap): "A" + "" + "B" on one line ⇒ drawn.
              test (sprintf "[%s] interior empty runs normalise without a drawn gap" gname) {
                  let runs =
                      render { baseT with Label = Some(LabelText.Rich [ { Symbology.run "A" with Weight = Some 700 }; Symbology.run ""; { Symbology.run "B" with Color = Some amber } ]) }
                      |> collectRuns
                  Expect.isGreaterThan runs.Length 0 "non-empty styled runs still draw with interior empties dropped (FR-007)"
              } ]
