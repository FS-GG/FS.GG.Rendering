module Symbology.Tests.MultilineLabelTests

// T008 [US1] + T011 [US2] Multi-line / paragraph label behaviour (FR-001/FR-003/FR-005/FR-006/FR-009).
//
// US1 (T008): a `\n`-bearing label emits N STACKED glyph-run nodes, the first at spec-196's exact baseline;
//   a one-line-fitting label is a SINGLE node at that same baseline (the zero-drift anchor); and on the
//   measurer-optional pure path (no measurer installed in this project) a multi-line token still emits its
//   line nodes deterministically and never throws (FR-009).
// US2 (T011): a too-wide WHITESPACE label wraps to multiple lines each ≤ region width; an over-budget label
//   is CAPPED to the grammar budget with the last drawn line ending in `…`; interior blank/whitespace
//   segments are COLLAPSED (`"A\n\n\nB"` ⇒ two lines, `"\n  \n"` ⇒ no label); a single unbroken word wider
//   than the region degrades to one fitted line (no wrap point, no overflow).
//
// The wrap/fit/siting helpers are INTERNAL (omitted from the .fsi); this battery exercises them only
// through the public `token`/`badge`/`ring` surface and the public Scene IR / measurement vocabulary.

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

// Provisional per-grammar region width + line budget (data-model.md / Symbology.fs siting). Kept here as
// the test's independent expectation; a fitted line must measure ≤ width, and the drawn count ≤ budget.
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

// First-line baseline per grammar (the spec-196 zero-drift anchor).
let private firstBaseline grammar (t: Token) =
    match grammar with
    | "token" -> t.Cy + t.R * 1.5
    | "badge" -> t.Cy + t.R * 1.42
    | "ring" -> t.Cy + t.R * 0.52
    | _ -> t.Cy

let private grammars =
    [ "token", Symbology.token; "badge", Symbology.badge; "ring", Symbology.ring ]

// Ordered glyph-run nodes (label lines) of a scene, preserving draw order so stacking can be asserted.
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

[<Tests>]
let stackingTests =
    testList
        "US1 multi-line stacking"
        [ for gname, render in grammars do
              test (sprintf "[%s] a `\\n`-bearing label emits N stacked nodes, first at the 196 baseline" gname) {
                  let runs = render { baseT with Label = Some (LabelText.Plain "AL\nBR") } |> collectRuns
                  Expect.equal runs.Length 2 "two short lines ⇒ two stacked glyph-run nodes"
                  let ys = runs |> List.map (fun r -> r.Position.Y)
                  Expect.floatClose Accuracy.high (List.head ys) (firstBaseline gname baseT) "first line sits at the spec-196 baseline (zero-drift anchor)"
                  Expect.equal ys (List.sort ys) "lines stack DOWNWARD (monotonically increasing baseline Y)"
                  Expect.equal (List.distinct ys).Length ys.Length "each line has a distinct baseline (non-overlapping)"
              }

              test (sprintf "[%s] a one-line-fitting label is a SINGLE node at the 196 baseline" gname) {
                  let runs = render { baseT with Label = Some (LabelText.Plain "A7") } |> collectRuns
                  Expect.equal runs.Length 1 "a single fitting line ⇒ exactly one node (byte-identity anchor)"
                  Expect.floatClose Accuracy.high (List.head runs).Position.Y (firstBaseline gname baseT) "the single line keeps spec-196's baseline"
              }

              // FR-009 — the pure library never installs/requires a measurer: a multi-line token still emits
              // its nodes deterministically (render-twice byte-equal) and never throws on the pure path.
              test (sprintf "[%s] multi-line on the no-measurer pure path: nodes emitted, deterministic, no throw (FR-009)" gname) {
                  let mk () = render { baseT with Label = Some (LabelText.Plain "AL\nBR") }
                  Expect.isGreaterThan (mk () |> collectRuns |> List.length) 0 "the pure path still emits the line nodes"
                  Expect.equal (bytesOf (mk ())) (bytesOf (mk ())) "multi-line layout is a deterministic function of Token (FR-009)"
              } ]

[<Tests>]
let wrapCapTests =
    testList
        "US2 multi-line wrap / cap / collapse"
        [ for gname, render in grammars do
              test (sprintf "[%s] a too-wide whitespace label wraps to multiple lines, each ≤ region width" gname) {
                  let wide = "ALPHA BRAVO CHARLIE DELTA ECHO FOXTROT GOLF HOTEL"
                  let runs = render { baseT with Label = Some (LabelText.Plain wide) } |> collectRuns
                  Expect.isGreaterThan runs.Length 1 "a long whitespace label wraps to more than one line"

                  let region = regionWidth gname baseT.R

                  for r in runs do
                      let drawn = (Scene.measureGlyphRun r.Data).Width
                      Expect.isLessThanOrEqual drawn (region + 1e-6) (sprintf "wrapped line %A width %f ≤ region %f (FR-005)" r.Data.Text drawn region)
              }

              test (sprintf "[%s] an over-budget label is capped to the budget; the last drawn line ends with the ellipsis" gname) {
                  let many = "L1\nL2\nL3\nL4\nL5\nL6"
                  let runs = render { baseT with Label = Some (LabelText.Plain many) } |> collectRuns
                  Expect.equal runs.Length (budget gname) (sprintf "drawn line count is capped to the %s budget (FR-005)" gname)
                  Expect.stringEnds (List.last runs).Data.Text "…" "the last drawn line is ellipsised to signal dropped content (SC-005)"
              }

              test (sprintf "[%s] interior blank/whitespace segments collapse (\"A\\n\\n\\nB\" ⇒ two lines)" gname) {
                  let runs = render { baseT with Label = Some (LabelText.Plain "A\n\n\nB") } |> collectRuns
                  Expect.equal runs.Length 2 "blank interior segments are dropped — no wasted gap (FR-006)"
              }

              test (sprintf "[%s] a blank-lines-only label (\"\\n  \\n\") draws no label and does not throw" gname) {
                  let kinds = render { baseT with Label = Some (LabelText.Plain "\n  \n") } |> Scene.describe
                  Expect.isFalse (List.contains GlyphRunElement kinds) "a blank-lines-only label is equivalent to None (FR-006)"
              }

              test (sprintf "[%s] a single unbroken word wider than the region degrades to one fitted line (no overflow)" gname) {
                  let runs = render { baseT with Label = Some (LabelText.Plain "THIS-CALLSIGN-IS-FAR-TOO-LONG-TO-FIT-1234567890") } |> collectRuns
                  Expect.equal runs.Length 1 "no whitespace ⇒ no wrap point ⇒ one fitted line"
                  let drawn = (Scene.measureGlyphRun (List.head runs).Data).Width
                  let region = regionWidth gname baseT.R
                  Expect.isLessThanOrEqual drawn (region + 1e-6) "the unbroken word is shrunk/ellipsised ≤ region, never overflowed (FR-005)"
              } ]
