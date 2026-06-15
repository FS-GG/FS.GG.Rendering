module Feature091RetainedRenderTests

// Feature 091 (E2) — the parked keyed reconciler (067), now WIRED onto the live render path via
// `module internal RetainedRender`. These tests reach the internal module through
// `[<assembly: InternalsVisibleTo("Controls.Tests")>]`; per the vertical-slice rule the in-assembly
// Expecto/FsCheck test IS the user-reachable surface for these internal user stories, exercising
// `RetainedRender.init`/`step` (the real wired path) over real `(prev, next)` trees and the real
// `Control.renderTree` measure/paint. Authored failing-first against the foundation stub, then
// greened by the real reuse implementation.

open System
open System.IO
open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

// Feature 092: `RetainedRender.init` now returns a `RetainedInit` (the seeded structure + the
// single first-frame Render + first-frame diagnostics). These 067/091 invariant tests only need
// the retained structure, so `rinit` projects it — keeping the invariant assertions unchanged.
let private rinit (t: Theme) (s: Size) (c: Control<'msg>) : RetainedRender<'msg> =
    (RetainedRender.init t s c).Retained

// --- helpers over the retained tree (internal, reachable via InternalsVisibleTo) -----------

let rec private findByKey (key: ControlId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then
        Some n
    else
        n.Children |> List.tryPick (findByKey key)

let rec private allIds (n: RetainedNode<'msg>) : RetainedId list =
    n.Identity :: (n.Children |> List.collect allIds)

let private idOfKey key (r: RetainedRender<'msg>) : RetainedId option =
    findByKey key r.Root |> Option.map (fun n -> n.Identity)

let private repr (x: 'a) : string = sprintf "%A" x

// --- builders --------------------------------------------------------------------------------

let private leaf (key: string) (content: string) : Control<int> =
    { Kind = "text-block"
      Key = Some key
      Attributes = [ { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 120.0 }
                     { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 24.0 } ]
      Children = []
      Content = Some content
      Accessibility = None }

let private stack (children: Control<int> list) : Control<int> =
    { Kind = "stack"
      Key = None
      Attributes = []
      Children = children
      Content = None
      Accessibility = None }

// =============================================================================================
// US1 (T009 / SC-001) — a control keeps its identity across an unrelated re-render.
// =============================================================================================

[<Tests>]
let us1 =
    testList
        "091 US1 identity survives an unrelated re-render"
        [ test "a keyed control matched (not Replaced) carries its RetainedId across an unrelated change" {
              // frame 0: [a, editor]; frame 1: [a', editor] — only `a` changed, editor identical.
              let prev = stack [ leaf "a" "A"; leaf "editor" "type here" ]
              let next = stack [ leaf "a" "A-changed"; leaf "editor" "type here" ]

              let r0 = rinit theme size prev
              let s = RetainedRender.step theme size r0 next

              let id0 = idOfKey "editor" r0
              let id1 = idOfKey "editor" s.Retained

              Expect.isSome id0 "editor exists in frame 0"
              Expect.equal id1 id0 "SC-001: editor's RetainedId is carried across the unrelated re-render"

              // and it was matched (ChildKeep/Update), never Replaced.
              match (Reconcile.diff prev next).Patch with
              | Reconcile.NodePatch.Update u ->
                  let editorOp =
                      u.Children
                      |> List.tryPick (fun op ->
                          match op with
                          | Reconcile.ChildKeep (i, p)
                          | Reconcile.ChildMove (i, _, p) when prev.Children.[i].Key = Some "editor" -> Some p
                          | _ -> None)

                  match editorOp with
                  | Some(Reconcile.NodePatch.Replace _) -> failtest "editor must NOT be Replaced"
                  | Some _ -> ()
                  | None -> failtest "editor child op not found"
              | other -> failtestf "expected root Update, got %A" other
          }

          test "a keyed control whose position SHIFTED is still matched and carries its identity" {
              // frame 1 inserts a sibling BEFORE editor, so editor's positional path changes.
              let prev = stack [ leaf "editor" "type here" ]
              let next = stack [ leaf "banner" "new!"; leaf "editor" "type here" ]

              let r0 = rinit theme size prev
              let s = RetainedRender.step theme size r0 next

              Expect.equal (idOfKey "editor" s.Retained) (idOfKey "editor" r0) "editor identity survives the positional shift"
          }

          test "a control whose Kind changed is Replaced with a FRESH identity (no false identity)" {
              let prev = stack [ leaf "editor" "type here" ]
              // same Key, different Kind -> the diff Replaces; identity must NOT be retained.
              let next =
                  stack
                      [ { Kind = "button"
                          Key = Some "editor"
                          Attributes = []
                          Children = []
                          Content = Some "type here"
                          Accessibility = None } ]

              let r0 = rinit theme size prev
              let s = RetainedRender.step theme size r0 next

              Expect.notEqual (idOfKey "editor" s.Retained) (idOfKey "editor" r0) "SC-001 negative: a Kind change mints a new identity"
          } ]

// =============================================================================================
// US2 (T014 / SC-002) — focus + an in-flight animation clock survive an unrelated state change,
// and a rebuild-every-frame baseline FAILS the same proof.
// =============================================================================================

// An in-flight per-control clock (feature 099 R4 `AnimationClock`): an opacity fade still mid-flight
// (Elapsed 0.25s of a 1s tween) targeting `Normal`, so a Normal-stamped re-render advances it rather
// than retargeting/dropping it — the survival of the carried value across the shift is what 091 proves.
let private startedClock () : AnimationClock =
    { Anim =
        { Animation.empty with
            Opacity =
                Some
                    { Start = 0.0
                      End = 1.0
                      Duration = TimeSpan.FromSeconds 1.0
                      Easing = Easing.EaseOut } }
      Elapsed = TimeSpan.FromMilliseconds 250.0
      Target = Normal
      // Feature 103 (R6): the carried slot gains a prior-snapshot field; `[]` here (091 proves the
      // clock's survival across a positional shift, not the cross-fade) is the plain fade-in case.
      From = [] }

[<Tests>]
let us2 =
    testList
        "091 US2 focus + animation survive an unrelated re-render"
        [ test "wired path: focus + an in-flight clock keyed by RetainedId survive an unrelated update" {
              // The defect class this closes is a POSITIONAL SHIFT: an unrelated change inserts a
              // sibling above `editor`, shifting its path-derived id. The keyed diff still matches it.
              let prev = stack [ leaf "editor" "hi" ]
              let next = stack [ leaf "banner" "new!"; leaf "editor" "hi" ] // editor shifts down

              let r0 = rinit theme size prev
              let editorId = (idOfKey "editor" r0).Value

              // set focus + a started per-control clock keyed by the STABLE identity.
              let clock0 = startedClock ()

              let r0' =
                  { r0 with
                      StateByIdentity = r0.StateByIdentity |> Map.add editorId { Animation = Some clock0; Text = None } }

              let s = RetainedRender.step theme size r0' next
              let editorId1 = (idOfKey "editor" s.Retained).Value

              Expect.equal editorId1 editorId "focus target identity unchanged across the unrelated update"

              match Map.tryFind editorId1 s.Retained.StateByIdentity with
              | Some st ->
                  Expect.isSome st.Animation "the per-control clock survived the unrelated re-render"
                  // advancing the carried clock continues from where it was (did NOT reset to start).
                  let advanced = RetainedRender.advance (TimeSpan.FromMilliseconds 250.0) st.Animation.Value
                  Expect.isGreaterThan advanced.Elapsed clock0.Elapsed "the clock advanced; it did not reset"
              | None -> failtest "SC-002: focus/clock state was lost across the unrelated re-render"
          }

          test "baseline (rebuild every frame, no retained identity) FAILS: editor's id is not stable" {
              // The pre-091 behavior = rebuild a fresh structure each frame (init), minting new ids.
              // Under the same positional shift, the rebuilt id for `editor` differs frame-to-frame.
              let prev = stack [ leaf "editor" "hi" ]
              let next = stack [ leaf "banner" "new!"; leaf "editor" "hi" ]

              let frame0 = rinit theme size prev
              let frame1 = rinit theme size next // rebuild-every-frame baseline

              Expect.notEqual
                  (idOfKey "editor" frame1)
                  (idOfKey "editor" frame0)
                  "baseline fails: rebuilding every frame mints a new id, so id-keyed focus/clock state is lost"
          } ]

// =============================================================================================
// US3 (T018 / SC-003, SC-004) — a localized change repaints only the changed subtree, and the
// wired output is byte-identical to a full rebuild.
// =============================================================================================

[<Tests>]
let us3 =
    testList
        "091 US3 partial update + golden parity"
        [ test "a localized leaf change recomputes only the changed subtree (RecomputedNodeCount < N)" {
              // wide tree of fixed-size leaves; change ONE leaf's content (no geometry shift).
              let leaves prefix n = [ for i in 1..n -> leaf (prefix + string i) ("v" + string i) ]
              let prev = stack (leaves "n" 12)
              let next = stack ((leaf "n1" "CHANGED") :: List.tail (leaves "n" 12))

              let r0 = rinit theme size prev
              let s = RetainedRender.step theme size r0 next

              let w = s.WorkReduction
              Expect.equal w.BaselineNodeCount (Control.count next) "baseline == full node count N"
              Expect.isLessThanOrEqual w.RecomputedNodeCount w.ChangedSubtreeBound "recomputed <= changed-subtree bound"
              Expect.isLessThan w.ChangedSubtreeBound w.BaselineNodeCount "SC-003: changed subtree is strictly smaller than N"
              Expect.equal w.RecomputedNodeCount 1 "only the single changed leaf was recomputed"
          }

          test "golden parity: wired Render is byte-identical to a full rebuild of next (SC-004)" {
              let prev = stack [ leaf "a" "A"; stack [ leaf "b" "B"; leaf "c" "C" ] ]
              let next = stack [ leaf "a" "A2"; stack [ leaf "b" "B"; leaf "c" "C2" ] ]

              let r0 = rinit theme size prev
              let s = RetainedRender.step theme size r0 next
              let full = Control.renderTree theme size next

              Expect.equal s.Render.Scene full.Scene "wired Scene == full rebuild Scene (zero diff)"
              Expect.equal s.Render.Bounds full.Bounds "wired Bounds == full rebuild Bounds"
              Expect.equal s.Render.NodeCount full.NodeCount "wired NodeCount == full rebuild NodeCount"
          } ]

// =============================================================================================
// US4 (T022 / SC-005, T023 / SC-006) — the 067 invariants hold on the WIRED path, and diagnostics
// surface without weakening totality.
// =============================================================================================

module private Gen091 =

    let private genAttrValue: Gen<AttrValue<int>> =
        Gen.oneof
            [ Gen.map TextValue (Gen.elements [ "hi"; "bye"; "x"; "y" ])
              Gen.map BoolValue (Gen.elements [ true; false ])
              Gen.map FloatValue (Gen.elements [ 0.0; 1.0; 2.5 ])
              Gen.map MessageValue (Gen.choose (0, 5)) ]

    let private genAttrs: Gen<Attr<int> list> =
        gen {
            let! names = Gen.subListOf [ "text"; "color"; "size"; "label" ]
            let! values = Gen.listOfLength (List.length names) genAttrValue
            return List.map2 (fun n v -> { Name = n; Category = AttrCategory.Style; Value = v }) names values
        }

    let private genKey: Gen<ControlId option> =
        Gen.frequency [ 2, Gen.constant None; 3, Gen.map Some (Gen.elements [ "a"; "b"; "c"; "d" ]) ]

    let private genKind: Gen<ControlKind> = Gen.elements [ "text-block"; "button"; "stack" ]

    let private genContent: Gen<string option> =
        Gen.frequency [ 1, Gen.constant None; 2, Gen.map Some (Gen.elements [ "A"; "B"; "C" ]) ]

    let rec private genControlOf (sz: int) : Gen<Control<int>> =
        gen {
            let! kind = genKind
            let! key = genKey
            let! attrs = genAttrs
            let! content = genContent

            let! children =
                if sz <= 0 then
                    Gen.constant []
                else
                    gen {
                        let! n = Gen.choose (0, 3)
                        return! Gen.listOfLength n (genControlOf (sz / 2))
                    }

            return
                { Kind = kind
                  Key = key
                  Attributes = attrs
                  Children = children
                  Content = content
                  Accessibility = None }
        }

    /// Drop duplicate keys within each sibling list (keep first, None the rest). Identity-at-rest
    /// assumes unique sibling keys: with a duplicate key the 067 diff legitimately reports a
    /// `KeyCollision` even for `diff x x`, so `diff x x` is not a pure Keep no-op there.
    let rec dedupeKeys (c: Control<int>) : Control<int> =
        let seen = System.Collections.Generic.HashSet<string>()

        let children =
            c.Children
            |> List.map (fun ch ->
                let ch =
                    match ch.Key with
                    | Some k when not (seen.Add k) -> { ch with Key = None }
                    | _ -> ch

                dedupeKeys ch)

        { c with Children = children }

    let control: Gen<Control<int>> = Gen.sized (fun s -> genControlOf (min s 4))

    let pair: Gen<Control<int> * Control<int>> =
        gen {
            let! p = control
            let! n = control
            return (p, n)
        }

[<Tests>]
let us4 =
    testList
        "091 US4 invariants on the wired path (FsCheck, >=1000 cases)"
        [ test "round-trip: wired Render is byte-identical to renderTree next (SC-005/FR-006)" {
              let roundTrips (prev: Control<int>, next: Control<int>) =
                  let s = RetainedRender.step theme size (rinit theme size prev) next
                  let full = Control.renderTree theme size next
                  s.Render.Scene = full.Scene
                  && s.Render.Bounds = full.Bounds
                  && s.Render.NodeCount = full.NodeCount

              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen091.pair) roundTrips)
          }

          test "determinism: identical frame sequences produce identical Render + RetainedIds (SC-005)" {
              let deterministic (prev: Control<int>, next: Control<int>) =
                  let run () =
                      let r0 = rinit theme size prev
                      let s = RetainedRender.step theme size r0 next
                      repr s.Render.Scene, allIds s.Retained.Root

                  run () = run ()

              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen091.pair) deterministic)
          }

          test "totality: step never throws for any (prev, next) (SC-005)" {
              let total (prev: Control<int>, next: Control<int>) =
                  try
                      RetainedRender.step theme size (rinit theme size prev) next |> ignore
                      true
                  with _ ->
                      false

              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen091.pair) total)
          }

          test "identity-at-rest: structurally identical frames => Keep no-op, zero re-measure/id churn (SC-005)" {
              let atRest (c0: Control<int>) =
                  let c = Gen091.dedupeKeys c0
                  let r0 = rinit theme size c
                  let s = RetainedRender.step theme size r0 c
                  s.WorkReduction.RecomputedNodeCount = 0
                  && s.Retained.NextId = r0.NextId
                  && allIds s.Retained.Root = allIds r0.Root
                  && List.isEmpty s.Diagnostics

              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen091.control) atRest)
          }

          // T023 [SEH] synthetic-error-handling-approved: literal malformed (duplicate-keyed) input.
          // SYNTHETIC: the duplicate-key sibling list is a deliberately-malformed literal fixture; the
          // diagnostic itself is produced by the REAL wired diff path (no product capability mocked).
          test "Synthetic: a duplicate-keyed sibling list surfaces KeyCollision on the wired path and stays total (SC-006)" {
              let dup k c : Control<int> = leaf k c
              let prev = stack [ dup "x" "A"; dup "x" "B"; leaf "y" "Y" ] // duplicate key "x"
              let next = stack [ dup "x" "A"; leaf "y" "Y" ]

              let r0 = rinit theme size prev
              let s = RetainedRender.step theme size r0 next // must NOT throw

              let collisions = s.Diagnostics |> List.filter (fun d -> d.Code = KeyCollision)
              Expect.isNonEmpty collisions "SC-006: KeyCollision surfaces through the ControlDiagnostic channel"
              collisions |> List.iter (fun d -> Expect.equal d.Severity Warning "KeyCollision is a Warning")
          } ]

// =============================================================================================
// Evidence capture (T017 / T021) — writes the real offscreen artifacts the contract requires,
// from the WIRED path itself (this assembly has InternalsVisibleTo access to RetainedRender).
// Render-only / deterministic: no live Vulkan window ([[fs-gg-evidence-mode]]). The readiness
// dir is located deterministically from the source file location.
// =============================================================================================

module private Evidence =

    let readinessRoot =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "specs", "091-wire-reconciler-render-path", "readiness"))

    let ensure (sub: string) =
        let d = Path.Combine(readinessRoot, sub)
        Directory.CreateDirectory d |> ignore
        d

    let hashOf (scene: Scene) : string = (Scene.renderReadbackEvidence size scene).DeterministicHash

    let private isPng (bytes: byte[]) =
        bytes.Length > 8 && bytes.[0] = 0x89uy && bytes.[1] = 0x50uy && bytes.[2] = 0x4Euy && bytes.[3] = 0x47uy

    /// HONEST image capture: writes a decodable `.png` ONLY when the bytes are a real PNG (magic
    /// header). NOTE `SceneEvidence.renderPng` is a deterministic CAPABILITY-hash function, not a
    /// pixel encoder (it returns the readback hash text as bytes — no rasterization), so it never
    /// yields a decodable image regardless of GPU; the hash is written to a `.capability-hash`
    /// sidecar and `decodable` is reported `false`. A real pixel PNG would need the windowed
    /// render-target path (`SkiaViewer.captureScreenshotEvidence`), which is out of scope here
    /// ([[fs-gg-evidence-mode]]: a hash must never be presented as a decodable image).
    let writeImage (basePath: string) (scene: Scene) : bool =
        match SceneEvidence.renderPng size scene with
        | Ok bytes when isPng bytes ->
            File.WriteAllBytes(basePath + ".png", bytes)
            true
        | _ ->
            File.WriteAllText(basePath + ".capability-hash", hashOf scene)
            false

[<Tests>]
let evidence =
    testList
        "091 evidence capture"
        [ test "capture retained-parity + work-reduction artifacts (SC-003/SC-004)" {
              let dir = Evidence.ensure "retained-parity"
              let pdir = Evidence.ensure "partial-update"

              // localized single-leaf change over a wide fixed-size tree.
              let leaves n = [ for i in 1..n -> leaf ("n" + string i) ("v" + string i) ]
              let prev = stack (leaves 12)
              let next = stack ((leaf "n1" "CHANGED") :: List.tail (leaves 12))

              let r0 = rinit theme size prev
              let s = RetainedRender.step theme size r0 next
              let full = Control.renderTree theme size next

              // AUTHORITATIVE parity proof: structural equality of the produced Scene values — pure
              // and environment-independent (needs no rasterizer). The wired Scene IS the full-rebuild Scene.
              let sceneIdentical = (s.Render.Scene = full.Scene)
              Expect.isTrue sceneIdentical "wired Scene == full rebuild (zero diff)"

              // Best-effort image: decodable PNG only if a real renderer is present; otherwise a
              // capability-hash sidecar (renderPng is a capability-hash, not a pixel encoder).
              let wiredPng = Evidence.writeImage (Path.Combine(dir, "wired")) s.Render.Scene
              let rebuildPng = Evidence.writeImage (Path.Combine(dir, "rebuild")) full.Scene
              let decodable = wiredPng && rebuildPng

              File.WriteAllText(
                  Path.Combine(dir, "retained-parity.txt"),
                  String.concat "\n"
                      [ "# Retained-tree reconciliation — golden-diff parity (feature 091, SC-004)"
                        "evidence-kind=golden-diff-parity"
                        "renderer-mode=DeterministicRenderOnly"
                        "status=pass"
                        "parity-proof=structural-scene-equality"
                        sprintf "scene-identical=%b" sceneIdentical
                        "scene-diff=zero"
                        sprintf "artifact-decodable=%b" decodable
                        sprintf "proves-scene-rendering=%b" decodable
                        "proves-desktop-visibility=false"
                        "readback-authoritative=false"
                        "readback-note=SceneEvidence.renderPng/renderReadbackEvidence are deterministic CAPABILITY-hash functions (size + sorted scene-capability descriptors), NOT pixel encoders, so they emit no decodable PNG and equal hashes mean equal capability sets (not identical pixels) — this is independent of the GPU (the environment HAS one). The AUTHORITATIVE parity proof is the pure structural equality of the ControlRenderResult.Scene values (wired == full rebuild), asserted by the test. A real pixel PNG would require the windowed render-target path (SkiaViewer.captureScreenshotEvidence), out of scope here."
                        "authoritative-test=Feature091RetainedRenderTests/091 US3 partial update + golden parity"
                        "note=wired Render.Scene is byte-identical to Control.renderTree next (zero diff); no live Vulkan window required ([[fs-gg-evidence-mode]])."
                        "" ]
              )

              File.WriteAllText(
                  Path.Combine(pdir, "work-reduction.txt"),
                  String.concat "\n"
                      [ "# Retained-tree reconciliation — measured per-frame work reduction (feature 091, SC-003)"
                        "evidence-kind=work-reduction"
                        "status=pass"
                        "scenario=localized single-leaf content change over a wide fixed-size tree (no geometry shift)"
                        sprintf "baselineCount=%d" s.WorkReduction.BaselineNodeCount
                        sprintf "wiredCount=%d" s.WorkReduction.RecomputedNodeCount
                        sprintf "subtreeBound=%d" s.WorkReduction.ChangedSubtreeBound
                        sprintf "invariant=RecomputedNodeCount(%d) <= ChangedSubtreeBound(%d) < BaselineNodeCount(%d)"
                            s.WorkReduction.RecomputedNodeCount s.WorkReduction.ChangedSubtreeBound s.WorkReduction.BaselineNodeCount
                        "authoritative-test=Feature091RetainedRenderTests/091 US3 partial update + golden parity"
                        "" ]
              )

              Expect.isLessThan s.WorkReduction.ChangedSubtreeBound s.WorkReduction.BaselineNodeCount "work bounded by changed subtree"
          }

          test "capture survives-proof artifacts (SC-002)" {
              let dir = Evidence.ensure "survives-proof"

              let prev = stack [ leaf "editor" "hi" ]
              let next = stack [ leaf "banner" "new!"; leaf "editor" "hi" ] // editor shifts down (unrelated insert)

              let r0 = rinit theme size prev
              let editorId = (idOfKey "editor" r0).Value
              let clock0 = startedClock ()

              let r0' =
                  { r0 with
                      StateByIdentity = r0.StateByIdentity |> Map.add editorId { Animation = Some clock0; Text = None } }

              let s = RetainedRender.step theme size r0' next
              let editorId1 = (idOfKey "editor" s.Retained).Value

              let focusSurvived = editorId1 = editorId

              let clockAdvanced =
                  match Map.tryFind editorId1 s.Retained.StateByIdentity with
                  | Some st when st.Animation.IsSome ->
                      (RetainedRender.advance (TimeSpan.FromMilliseconds 250.0) st.Animation.Value).Elapsed > clock0.Elapsed
                  | _ -> false

              // baseline = rebuild every frame (init); the id is not stable across the shift.
              let baselineFails =
                  idOfKey "editor" (rinit theme size next) <> Some editorId

              let before = (Control.renderTree theme size prev).Scene
              let after = s.Render.Scene
              // the before/after frames genuinely differ (the unrelated insert adds `banner`) — a pure
              // structural fact, environment-independent.
              let scenesDiffer = (before <> after)
              let beforePng = Evidence.writeImage (Path.Combine(dir, "before")) before
              let afterPng = Evidence.writeImage (Path.Combine(dir, "after")) after

              File.WriteAllText(
                  Path.Combine(dir, "survives-proof.txt"),
                  String.concat "\n"
                      [ "# Retained-tree reconciliation — focus/animation survives an unrelated re-render (feature 091, SC-002)"
                        "evidence-kind=survives-proof"
                        "renderer-mode=DeterministicRenderOnly"
                        "status=pass"
                        sprintf "focus-survived=%b" focusSurvived
                        sprintf "clock-advanced=%b" clockAdvanced
                        sprintf "baseline-fails=%b" baselineFails
                        sprintf "scenes-differ=%b" scenesDiffer
                        sprintf "artifact-decodable=%b" (beforePng && afterPng)
                        "proves-scene-rendering=false"
                        "proves-desktop-visibility=false"
                        "readback-authoritative=false"
                        "readback-note=no decodable PNG because SceneEvidence.renderPng is a deterministic capability-hash, not a pixel encoder (independent of the GPU — the environment HAS one). The AUTHORITATIVE proof is pure: the focused control's stable RetainedId is carried across the unrelated re-render (focus-survived), its clock advances rather than resets (clock-advanced), and a rebuild-every-frame baseline mints a new id so it loses the state (baseline-fails) — all asserted by the test."
                        "mechanism=RetainedRender.StateByIdentity keyed by the stable RetainedId carried across the diff"
                        "authoritative-test=Feature091RetainedRenderTests/091 US2 focus + animation survive an unrelated re-render"
                        "" ]
              )

              Expect.isTrue focusSurvived "focus identity survived the unrelated re-render"
              Expect.isTrue clockAdvanced "the per-control clock advanced (did not reset)"
              Expect.isTrue baselineFails "a rebuild-every-frame baseline fails the same proof"
          } ]
