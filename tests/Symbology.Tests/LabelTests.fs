module Symbology.Tests.LabelTests

// T017 [US2] Identity-label fit / empty-whitespace / pure-fallback behaviour (FR-005/FR-006/FR-009).
//
// (a) Fit-within-region (FR-005/SC-005): an overlong label, once fitted, draws a glyph run whose measured
//     width is <= the grammar's label region width — never overflowing the footprint, never clipped
//     mid-glyph. We measure the DRAWN glyph run via the same `Scene.measureTextResolved` seam the fit used,
//     so this is the real fit evidence, not a re-derivation.
// (b) Empty/whitespace (FR-006): a `Some s` with `s.Trim() = ""` emits NO label glyph node and raises no
//     exception — equivalent to `None`.
// (c) Pure-fallback path (FR-009/C-09): in this pure test project NO real measurer is installed, yet a
//     labelled token still emits a label glyph-run node and never throws — the library is measurer-optional;
//     tofu-free *rendering* is a render-edge property, not a precondition of the pure library.
//
// The fit/siting helpers are INTERNAL (omitted from the .fsi); this battery exercises them only through the
// public `token`/`badge`/`ring` surface and the public `Scene.describe`/`measureGlyphRun` vocabulary.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private baseT =
    { Symbology.defaultToken with
        Cx = 60.0
        Cy = 60.0
        R = 24.0
        Faction = Ally
        Health = 0.6 }

// The provisional per-grammar region widths the grammars fit to (data-model.md / Symbology.fs siting).
// Kept here as the test's independent expectation of "within the region"; a fitted label must measure <=
// this. (If the siting widens, this is the contract the fit must still honour.)
let private regionWidth grammar (r: float) =
    match grammar with
    | "token" -> r * 1.9
    | "badge" -> r * 1.7
    | "ring" -> r * 1.05
    | _ -> r

let private grammars =
    [ "token", Symbology.token; "badge", Symbology.badge; "ring", Symbology.ring ]

// Walk the public Scene-IR collecting glyph-run proof data (the label node). Scene/SceneNode/GlyphRun are
// public contract types, so the test inspects the drawn label with no new library surface.
let rec private collectGlyphRuns (scene: Scene) : GlyphRunData list =
    scene.Nodes
    |> List.collect (fun node ->
        match node with
        | GlyphRun r -> [ r.Data ]
        | Group scenes -> scenes |> List.collect collectGlyphRuns
        | ClipNode(_, s) -> collectGlyphRuns s
        | ColorSpaceNode(_, s) -> collectGlyphRuns s
        | PerspectiveNode(_, s) -> collectGlyphRuns s
        | Translate(_, s) -> collectGlyphRuns s
        | _ -> [])

// The single glyph-run node a labelled scene carries (None when no label is drawn).
let private labelGlyphRun (scene: Scene) : GlyphRunData option =
    scene |> collectGlyphRuns |> List.tryHead

[<Tests>]
let fitTests =
    testList
        "US2 label fit within region"
        [ for gname, render in grammars do
              test (sprintf "[%s] an overlong label is fitted within the region width (no overflow)" gname) {
                  let overlong = "THIS-CALLSIGN-IS-FAR-TOO-LONG-TO-FIT-1234567890"
                  let scene = render { baseT with Label = Some (LabelText.Plain overlong) }

                  match labelGlyphRun scene with
                  | None -> failtest "an overlong label must still draw a (fitted) glyph run"
                  | Some data ->
                      let drawn = (Scene.measureGlyphRun data).Width
                      let region = regionWidth gname baseT.R
                      Expect.isLessThanOrEqual drawn (region + 1e-6) (sprintf "fitted label width %f must be <= region %f (FR-005)" drawn region)
              }

              test (sprintf "[%s] a label that already fits is drawn whole (not truncated)" gname) {
                  let scene = render { baseT with Label = Some (LabelText.Plain "A7") }

                  match labelGlyphRun scene with
                  | None -> failtest "a fitting label must draw a glyph run"
                  | Some data -> Expect.equal data.Text "A7" "a fitting short label keeps its text unchanged (no ellipsis)"
              } ]

[<Tests>]
let emptyWhitespaceTests =
    testList
        "US2 empty/whitespace label => no label"
        [ for gname, render in grammars do
              for label in [ Some (LabelText.Plain ""); Some (LabelText.Plain "   "); Some (LabelText.Plain "\t \n") ] do
                  test (sprintf "[%s] label %A emits no glyph node and does not throw" gname label) {
                      let scene = render { baseT with Label = label }
                      let kinds = scene |> Scene.describe
                      Expect.isFalse (List.contains GlyphRunElement kinds) "empty/whitespace label draws nothing (FR-006)"
                  }

              test (sprintf "[%s] whitespace label is byte-identical to no label" gname) {
                  let ws = (SceneCodec.export (render { baseT with Label = Some (LabelText.Plain "   ") })).CanonicalBytes
                  let none = (SceneCodec.export (render { baseT with Label = None })).CanonicalBytes
                  Expect.equal ws none "whitespace == no label (FR-006)"
              } ]

// (c) FR-009/C-09 — pure-fallback path. This project installs no real measurer, so these assertions run on
// the measurer-optional pure path by construction: a labelled token still emits a node and never throws.
[<Tests>]
let pureFallbackTests =
    testList
        "US2 measurer-optional pure library (FR-009)"
        [ for gname, render in grammars do
              test (sprintf "[%s] labelled token on the no-measurer path emits a node and does not throw" gname) {
                  let scene = render { baseT with Label = Some (LabelText.Plain "PURE-1") }
                  // No throw is implied by reaching here; assert the node is present and carries the text.
                  match labelGlyphRun scene with
                  | None -> failtest "the pure library must still emit the label node with no measurer installed"
                  | Some data ->
                      // Text may be fitted (shrunk/ellipsis-truncated) to a narrow region; the FR-009 point is
                      // that a node is emitted (non-empty) and carries deterministic pure-fallback evidence.
                      Expect.isGreaterThan data.Text.Length 0 "the pure-fallback label node carries (fitted) text (FR-009)"
                      Expect.equal data.FallbackMode PureFallbackMode "with no measurer, the node is the deterministic pure fallback (FR-009)"
              } ]
