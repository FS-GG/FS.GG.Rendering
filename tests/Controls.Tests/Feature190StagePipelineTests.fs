module Feature190StagePipelineTests

// Feature 190 — per-stage ISOLATION + composition byte-identity tests for the `RetainedRender.step`
// pipeline decomposition (`diffStage >> layoutStage >> paintStage >> assemblyStage`). Reaches the
// internal stages, `FrameState`, and `FrameContext` via `InternalsVisibleTo("Controls.Tests")`.
// Contracts C-DIFF .. C-TRACE (specs/190-retained-render-step-pipeline/contracts/stage-contracts.md):
//   C-DIFF   — diffStage standalone == Reconcile.diff oracle + layoutDirtySet; dup key -> KeyCollision.
//   C-LAYOUT — layoutStage standalone: Remeasured == |Invalidated|, ThemeChanged correct, idle re-measures none.
//   C-PAINT  — paintStage standalone: RetainedNode + st mutations match the inline build.
//   C-ASM    — assemblyStage standalone: the 40-field WorkReductionRecord golden == `step`'s.
//   C-COMPOSE— the manual 4-stage composition is byte-identical to `step` AND to a full renderTree at rest.
//   C-TRACE  — every `retained-step-*` span is still emitted under FS_GG_RENDER_LAG_TRACE=1.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 400; Height = 300 }

let private rinit (c: Control<int>) = (RetainedRender.init theme size c).Retained

let private attr name value : Attr<int> =
    { Name = name; Category = AttrCategory.Style; Value = FloatValue value }

let private leaf key (content: string) w h : Control<int> =
    { Kind = "text-block"
      Key = Some key
      Attributes = [ attr "width" w; attr "height" h ]
      Children = []
      Content = Some content
      Accessibility = None }

let private stack key (attrs: Attr<int> list) children : Control<int> =
    { Kind = "stack"
      Key = Some key
      Attributes = attrs
      Children = children
      Content = None
      Accessibility = None }

let private tree (leafAContent: string) (leafAWidth: float) : Control<int> =
    stack "root" [] [
        stack "panel" [ attr "width" 200.0; attr "height" 100.0 ] [
            leaf "leafA" leafAContent leafAWidth 20.0
            leaf "leafB" "B" 50.0 20.0
        ]
        leaf "sibling" "S" 100.0 30.0
    ]

// Seed a FrameState from `prev` EXACTLY as `step` does (the steady-state seed).
let private seedState (prev: RetainedRender<int>) : RetainedRender.FrameState =
    { Tc = prev.TextCache
      TextHits = 0
      TextMisses = 0
      NextId = prev.NextId
      Recomputed = 0
      ChangedBound = 0
      Shifted = 0
      Memo = prev.Memo
      MemoHits = 0
      MemoMisses = 0
      MetadataVisited = 0
      VirtualMaterialized = 0
      VirtualTotal = 0
      PcEntries = prev.PictureCache.Entries
      PcClock = prev.PictureCache.Clock
      PictureHits = 0
      PictureMisses = 0
      ReplaySkippedNodes = 0
      ReplayNativeBytes = 0
      RepaintedBoxes = ResizeArray<Rect>() }

let private ctxOf (prev: RetainedRender<int>) (thm: Theme) : RetainedRender.FrameContext<int> =
    { Theme = thm
      Size = size
      Prev = prev
      ThemeChanged = (prev.Theme <> thm) }

// Drive the four stages in order, replicating `step`'s orchestration (incl. the text-measure-hook
// lifetime, research R4) so the result is the externally-composed analogue of `step`. Used to prove
// the stages compose to exactly `step` (C-COMPOSE) and to exercise assemblyStage's golden (C-ASM).
let private manualCompose (thm: Theme) (prev: RetainedRender<int>) (next: Control<int>) : RetainedRenderStep<int> =
    let result, dirty, invalidated = RetainedRender.diffStage prev next
    let st = seedState prev
    let ctx = ctxOf prev thm
    let frameStartTextKeys = prev.TextCache.Entries |> Map.toSeq |> Seq.map fst |> Set.ofSeq

    let measureCached (text: string) (font: FontSpec) : TextMetrics =
        let key: TextMeasureKey =
            { Text = text
              Family = font.Family
              Size = font.Size
              Weight = font.Weight
              MeasurementVersionBucket = Scene.textMeasurementVersionBucket () }

        let metrics, tc', wasHit = RetainedRender.measureTextCached st.Tc prev.TextCacheEnabled text font
        st.Tc <- tc'
        if not prev.TextCacheEnabled then
            st.TextMisses <- st.TextMisses + 1
        elif wasHit then
            if Set.contains key frameStartTextKeys then
                st.TextHits <- st.TextHits + 1
        else
            st.TextMisses <- st.TextMisses + 1
        metrics

    ControlInternals.setMeasureTextHook (Some measureCached)
    let layout = RetainedRender.layoutStage ctx st next dirty
    let newRoot = RetainedRender.paintStage ctx st result.Patch layout.BoundsById next
    ControlInternals.setMeasureTextHook None
    RetainedRender.assemblyStage ctx st layout result invalidated newRoot next

[<Tests>]
let tests =
    testList "Feature190 stage pipeline" [

        // ---- C-DIFF (T009) ---------------------------------------------------------------------
        test "diffStage standalone == Reconcile.diff oracle + layoutDirtySet; duplicate key surfaces KeyCollision (C-DIFF, FR-010)" {
            let prev = rinit (tree "A" 50.0)
            let next = tree "A" 70.0 // localized geometry change on leafA

            let result, dirty, invalidated = RetainedRender.diffStage prev next
            let oracle = Reconcile.diff prev.Root.Control next

            Expect.equal result.Diagnostics oracle.Diagnostics "diffStage diagnostics == Reconcile.diff diagnostics"
            Expect.equal invalidated (Set.count dirty) "invalidated == |dirty| (pre-propagation size)"
            Expect.isNonEmpty dirty "a layout-affecting (width) change makes the dirty set non-empty"
            // diffStage is pure over (prev, next): deterministic dirty set + diagnostics.
            let result2, dirty2, invalidated2 = RetainedRender.diffStage prev next
            Expect.equal dirty2 dirty "diffStage dirty set is deterministic"
            Expect.equal invalidated2 invalidated "diffStage invalidated is deterministic"
            Expect.equal result2.Diagnostics result.Diagnostics "diffStage diagnostics deterministic"

            // Duplicate-keyed siblings -> the diff surfaces a KeyCollision diagnostic; diffStage is total.
            let dupPrev = rinit (stack "root" [] [ leaf "x" "A" 40.0 20.0; leaf "x" "B" 40.0 20.0; leaf "y" "Y" 40.0 20.0 ])
            let dupNext = stack "root" [] [ leaf "x" "A" 40.0 20.0; leaf "y" "Y" 40.0 20.0 ]
            let dupResult, _, _ = RetainedRender.diffStage dupPrev dupNext
            Expect.isNonEmpty (dupResult.Diagnostics |> List.filter (fun d -> d.Code = KeyCollision)) "FR-010: duplicate key surfaces KeyCollision through diffStage"
        }

        // ---- C-LAYOUT (T010) -------------------------------------------------------------------
        test "layoutStage standalone: Remeasured == |Invalidated|, ThemeChanged honest, empty dirty re-measures nothing (C-LAYOUT)" {
            let prev = rinit (tree "A" 50.0)
            let next = tree "A" 70.0
            let _, dirty, _ = RetainedRender.diffStage prev next

            let st = seedState prev
            let ctx = ctxOf prev theme
            let layout = RetainedRender.layoutStage ctx st next dirty

            Expect.equal layout.Remeasured (layout.LayoutResult.Invalidated |> List.length) "Remeasured == post-propagation Invalidated count"
            Expect.isFalse layout.ThemeChanged "same theme -> ThemeChanged = false"

            // Idle frame: an empty dirty set re-measures nothing.
            let stIdle = seedState prev
            let idle = RetainedRender.layoutStage ctx stIdle next Set.empty
            Expect.equal idle.Remeasured 0 "empty dirty set -> zero re-measured (idle frame)"

            // A theme switch flips ThemeChanged.
            let ctxThemed = ctxOf prev Theme.dark
            Expect.isTrue (RetainedRender.layoutStage ctxThemed (seedState prev) next dirty).ThemeChanged "different theme -> ThemeChanged = true"
        }

        // ---- C-PAINT (T011) --------------------------------------------------------------------
        test "paintStage standalone: a Replace mints fresh + records ChangedBound/repaint damage matching the inline build (C-PAINT)" {
            // Replace at leafA (kind change text-block -> button) forces a fresh subtree: ChangedBound
            // grows by the replaced node count and every painted node contributes a repaint box.
            let prev = rinit (tree "A" 50.0)
            let nextReplace =
                stack "root" [] [
                    stack "panel" [ attr "width" 200.0; attr "height" 100.0 ] [
                        { Kind = "button"; Key = Some "leafA"; Attributes = [ attr "width" 60.0; attr "height" 24.0 ]; Children = []; Content = Some "A"; Accessibility = None }
                        leaf "leafB" "B" 50.0 20.0
                    ]
                    leaf "sibling" "S" 100.0 30.0
                ]

            let result, dirty, _ = RetainedRender.diffStage prev nextReplace
            let st = seedState prev
            let ctx = ctxOf prev theme
            let layout = RetainedRender.layoutStage ctx st nextReplace dirty
            let newRoot = RetainedRender.paintStage ctx st result.Patch layout.BoundsById nextReplace

            Expect.equal newRoot.Control.Kind "stack" "paintStage returns the next root node"
            Expect.isGreaterThan st.Recomputed 0 "a Replace repaints at least the replaced node (Recomputed > 0)"
            Expect.isGreaterThan st.ChangedBound 0 "a Replace contributes to ChangedBound"
            Expect.isGreaterThan st.RepaintedBoxes.Count 0 "every repainted node contributes its damage box"

            // The standalone paintStage must agree with `step`'s reported counters for the SAME input.
            let s = RetainedRender.step theme size prev nextReplace
            Expect.equal st.Recomputed s.WorkReduction.RecomputedNodeCount "paintStage Recomputed == step RecomputedNodeCount"
            Expect.equal st.ChangedBound s.WorkReduction.ChangedSubtreeBound "paintStage ChangedBound == step ChangedSubtreeBound"
            Expect.equal st.Shifted s.WorkReduction.ShiftedNodeCount "paintStage Shifted == step ShiftedNodeCount"
        }

        // ---- C-ASM (T012) + C-COMPOSE (T013) ---------------------------------------------------
        test "assemblyStage golden: the 40-field WorkReductionRecord from the manual composition == step's, across scenarios (C-ASM)" {
            let scenarios =
                [ "idle",      (fun () -> tree "A" 50.0), (fun () -> tree "A" 50.0)
                  "localized", (fun () -> tree "A" 50.0), (fun () -> tree "A" 70.0)
                  "content",   (fun () -> tree "A" 50.0), (fun () -> tree "Z" 50.0)
                  "insert",    (fun () -> tree "A" 50.0), (fun () -> stack "root" [] [ stack "panel" [ attr "width" 200.0; attr "height" 100.0 ] [ leaf "leafA" "A" 50.0 20.0; leaf "leafB" "B" 50.0 20.0; leaf "leafC" "C" 50.0 20.0 ]; leaf "sibling" "S" 100.0 30.0 ]) ]

            for name, mkPrev, mkNext in scenarios do
                let prev = rinit (mkPrev ())
                let next = mkNext ()
                let manual = manualCompose theme prev next
                let real = RetainedRender.step theme size prev next
                Expect.equal manual.WorkReduction real.WorkReduction (sprintf "%s: 40-field WorkReductionRecord golden matches step" name)
                Expect.equal manual.Diagnostics real.Diagnostics (sprintf "%s: diagnostics match step" name)
        }

        test "composition byte-identity: step.Render at rest == a full Control.renderTree, and the manual composition == step (C-COMPOSE)" {
            let prev = rinit (tree "A" 50.0)
            let next = tree "A" 70.0

            // (1) the manual 4-stage composition reproduces step's emitted scene + work record.
            let manual = manualCompose theme prev next
            let real = RetainedRender.step theme size prev next
            Expect.equal manual.Render.Scene real.Render.Scene "manual composition scene == step scene"
            Expect.equal manual.WorkReduction real.WorkReduction "manual composition work record == step"

            // (2) step's emitted scene is byte-identical to a full rebuild of `next` AT REST (the
            //     standing retained-render invariant the decomposition must preserve).
            let atRest = RetainedRender.step theme size (rinit next) next
            let full = Control.renderTree theme size next
            Expect.equal atRest.Render.Scene full.Scene "at-rest retained scene == full renderTree scene"
        }

        // ---- C-TRACE (T013) --------------------------------------------------------------------
        // Dual-mode (the trace `enabled` flag is read once at module load): under
        // FS_GG_RENDER_LAG_TRACE=1 (the quickstart `--filter "Feature190.*trace"` run) this asserts the
        // full pre-change span set is emitted; otherwise it asserts the trace channel is correctly silent.
        test "trace parity: every retained-step-* span is emitted under FS_GG_RENDER_LAG_TRACE=1 (C-TRACE, FR-008)" {
            let expectedSpans =
                [ "retained-step-diff"
                  "retained-step-layout-dirty-set"
                  "retained-step-layout-incremental"
                  "retained-step-build"
                  "retained-step-count-virtual"
                  "retained-step-damage-reduce"
                  "retained-step-picture-walk"
                  "retained-step-offscreen-diagnostics"
                  "retained-step-index-prior-own"
                  "retained-step-state-collect"
                  "retained-step-scene-assembly"
                  "retained-step-render-result"
                  "retained-step-work-node-count" ]

            let enabled =
                System.String.Equals(System.Environment.GetEnvironmentVariable("FS_GG_RENDER_LAG_TRACE"), "1", System.StringComparison.Ordinal)

            let prev = rinit (tree "A" 50.0)
            let next = tree "A" 70.0

            let originalErr = System.Console.Error
            let sw = new System.IO.StringWriter()
            System.Console.SetError(sw)
            try
                RetainedRender.step theme size prev next |> ignore
            finally
                System.Console.SetError(originalErr)

            let captured = sw.ToString()

            if enabled then
                for span in expectedSpans do
                    Expect.isTrue (captured.Contains("event=" + span)) (sprintf "FR-008: span %s emitted under the trace" span)
            else
                Expect.isFalse (captured.Contains "event=retained-step-") "trace channel is silent when FS_GG_RENDER_LAG_TRACE is unset"
        }
    ]
