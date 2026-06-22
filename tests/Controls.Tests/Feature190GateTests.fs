module Feature190GateTests

// Feature 190 — the §7 hot-path REGRESSION GATE for the `step` decomposition (FR-005/FR-015,
// SC-002/SC-008). Three responsibilities (contract C-GATE):
//   (a) golden-hash corpus equivalence — the retained `step` scene is byte-identical to a full
//       `Control.renderTree` rebuild AT REST over a scene corpus (the byte-identity reference).
//   (b) determinism — identical (prev, next) yields an identical emitted scene + work record.
//   (c) INJECTED-REGRESSION proof (T024/SC-008) — a deliberately perturbed pipeline (a dropped/added
//       damage box, the canonical hot-path regression) changes the golden, so the equality gate goes
//       RED; the real composition is GREEN. This proves the gate is discriminating, not vacuous.
// The per-frame alloc-count + frame-time budget assertions (FR-006/SC-004) live on the existing perf
// lanes (Elmish.Tests Feature160/161/167/173) and are validated under DISPLAY=:1 in T025; the trace-
// span parity assertion (FR-008) lives in Feature190StagePipelineTests ("...trace..."). Reaches the
// internal stages via InternalsVisibleTo("Controls.Tests").

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 400; Height = 300 }

let private rinit (c: Control<int>) = (RetainedRender.init theme size c).Retained
let private attr name value : Attr<int> = { Name = name; Category = AttrCategory.Style; Value = FloatValue value }

let private leaf key (content: string) w h : Control<int> =
    { Kind = "text-block"; Key = Some key; Attributes = [ attr "width" w; attr "height" h ]; Children = []; Content = Some content; Accessibility = None }

let private stack key (attrs: Attr<int> list) children : Control<int> =
    { Kind = "stack"; Key = Some key; Attributes = attrs; Children = children; Content = None; Accessibility = None }

let private tree (content: string) (w: float) : Control<int> =
    stack "root" [] [
        stack "panel" [ attr "width" 200.0; attr "height" 100.0 ] [ leaf "leafA" content w 20.0; leaf "leafB" "B" 50.0 20.0 ]
        leaf "sibling" "S" 100.0 30.0
    ]

let private seedState (prev: RetainedRender<int>) : RetainedRender.FrameState =
    { Tc = prev.TextCache; TextHits = 0; TextMisses = 0; NextId = prev.NextId; Recomputed = 0; ChangedBound = 0
      Shifted = 0; Memo = prev.Memo; MemoHits = 0; MemoMisses = 0; MetadataVisited = 0; VirtualMaterialized = 0
      VirtualTotal = 0; PcEntries = prev.PictureCache.Entries; PcClock = prev.PictureCache.Clock; PictureHits = 0
      PictureMisses = 0; ReplaySkippedNodes = 0; ReplayNativeBytes = 0; RepaintedBoxes = ResizeArray<Rect>() }

let private ctxOf (prev: RetainedRender<int>) : RetainedRender.FrameContext<int> =
    { Theme = theme; Size = size; Prev = prev; ThemeChanged = (prev.Theme <> theme) }

[<Tests>]
let tests =
    testList "Feature190Gate" [

        // (a) golden-hash corpus equivalence -------------------------------------------------------
        test "gate: the retained step scene is byte-identical to a full renderTree at rest over the corpus (FR-005/SC-002)" {
            let corpus =
                [ tree "A" 50.0
                  tree "Hello world" 120.0
                  stack "root" [] [ for i in 0 .. 6 -> leaf (sprintf "row-%d" i) (sprintf "Row %d" i) 80.0 18.0 ]
                  stack "root" [ attr "gap" 4.0 ] [ stack "panel" [ attr "width" 180.0; attr "height" 90.0 ] [ leaf "x" "X" 40.0 16.0 ]; leaf "y" "Y" 60.0 24.0 ] ]

            for c in corpus do
                let atRest = RetainedRender.step theme size (rinit c) c
                let full = Control.renderTree theme size c
                Expect.equal atRest.Render.Scene full.Scene "retained step at rest == full renderTree (byte-identity)"
        }

        // (b) determinism --------------------------------------------------------------------------
        test "gate: identical (prev, next) yields an identical scene + WorkReductionRecord (determinism)" {
            let prev = rinit (tree "A" 50.0)
            let next = tree "A" 70.0
            let a = RetainedRender.step theme size prev next
            let b = RetainedRender.step theme size prev next
            Expect.equal a.Render.Scene b.Render.Scene "deterministic emitted scene"
            Expect.equal a.WorkReduction b.WorkReduction "deterministic work record"
        }

        // (c) injected-regression proof (T024/SC-008) ----------------------------------------------
        test "gate goes RED on an injected regression (a perturbed damage set) and GREEN on the real decomposition (FR-015/SC-008)" {
            let prev = rinit (tree "A" 50.0)
            let next = tree "A" 70.0
            let real = RetainedRender.step theme size prev next

            // Reproduce the pipeline, then INJECT a regression of the exact class the gate must catch:
            // a spurious damage box (the dual of a dropped `RepaintedBoxes.Add`). A guaranteed-distinct
            // off-frame rect changes DirtyRectCount/DirtyArea — the golden the gate compares.
            let result, dirty, invalidated = RetainedRender.diffStage prev next
            let st = seedState prev
            let ctx = ctxOf prev
            let layout = RetainedRender.layoutStage ctx st next dirty
            let newRoot = RetainedRender.paintStage ctx st result.Patch layout.BoundsById next
            st.RepaintedBoxes.Add { X = 9999.0; Y = 9999.0; Width = 7.0; Height = 7.0 } // <-- injected regression
            let perturbed = RetainedRender.assemblyStage ctx st layout result invalidated newRoot next

            // The gate's equality check is DISCRIMINATING: the perturbed golden differs from the real one.
            Expect.notEqual perturbed.WorkReduction real.WorkReduction "RED: the injected damage-box regression changes the WorkReductionRecord golden"
            Expect.notEqual perturbed.WorkReduction.DirtyRectCount real.WorkReduction.DirtyRectCount "the dropped/added damage box is exactly what the gate detects"

            // GREEN: the real, unperturbed decomposition reproduces the golden.
            let clean = RetainedRender.step theme size prev next
            Expect.equal clean.WorkReduction real.WorkReduction "GREEN: the real decomposition matches the golden"
        }
    ]
