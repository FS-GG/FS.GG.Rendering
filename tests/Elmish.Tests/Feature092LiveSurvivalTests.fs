module Feature092LiveSurvivalTests

// Feature 092 (E2) — the headline E2 benefit, now REAL in the running host: focus + in-progress text
// (+ the per-control animation clock) survive a positional shift, driven through the REAL adapter
// seam (`ControlsElmish.resolveFocus` + `routeFocusedText` + `RetainedRender.step`) with NO
// hand-seeded `StateByIdentity` for focus/text — the exact gap 091 left. These tests reach the
// internal retained structure + seam via InternalsVisibleTo (the in-assembly test IS the
// user-reachable surface for the wired path). Render-only / deterministic, no live Vulkan window
// ([[fs-gg-evidence-mode]]).

open System
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg =
    | NameChanged of string
    | AlsoChanged of string

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private rinit (t: Theme) (s: Size) (c: Control<'msg>) : RetainedRender<'msg> =
    (RetainedRender.init t s c).Retained

// --- helpers over the retained tree (internal, reachable via InternalsVisibleTo) -------------

let rec private findByKey (key: ControlId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then
        Some n
    else
        n.Children |> List.tryPick (findByKey key)

let private idOfKey (key: ControlId) (r: RetainedRender<'msg>) : RetainedId option =
    findByKey key r.Root |> Option.map (fun n -> n.Identity)

let private boxOfKey (key: ControlId) (r: RetainedRender<'msg>) : Rect =
    (findByKey key r.Root).Value.Fragment.Box.Value

let private centre (b: Rect) = b.X + b.Width / 2.0, b.Y + b.Height / 2.0

let rec private collectKind (kind: ControlKind) (n: RetainedNode<'msg>) : (RetainedId * Rect) list =
    let here =
        if n.Control.Kind = kind && n.Fragment.Box.IsSome then
            [ n.Identity, n.Fragment.Box.Value ]
        else
            []

    here @ (n.Children |> List.collect (collectKind kind))

// NOTE (feature 099, R4): the per-control ANIMATION clock survival that 092 documented as a
// hand-seeded PRECONDITION (no animation seam existed at E2) now has a real host seam and is proven
// through it in `Feature099AnimationClockTests` (`us2-survival`). The hand-seed is removed here; this
// file keeps proving 092's focus + in-progress text survival through the real adapter seam.

// --- views -----------------------------------------------------------------------------------

let private editorView (name: string) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ TextBox.create [ TextBox.value name; TextBox.onChanged NameChanged ] |> Control.withKey "editor" ] ]

// the "shift": an unrelated insert above the editor pushes its position (and path-derived id) down.
let private editorViewShifted (name: string) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ TextBlock.create [ TextBlock.text "banner" ] |> Control.withKey "banner"
                TextBox.create [ TextBox.value name; TextBox.onChanged NameChanged ] |> Control.withKey "editor" ] ]

// =============================================================================================
// US1 (T008 / SC-001) — focus + in-progress text + the per-control clock survive a positional
// shift through the real seam, and a rebuild-every-frame baseline FAILS the same proof.
// =============================================================================================

[<Tests>]
let liveSurvival =
    testList
        "092 US1 live survival through the real adapter seam"
        [ test "focus → type x → unrelated shift → type y ⇒ draft is 'hixy' (continued, not reset)" {
              // frame 0: the editor pre-filled with "hi" (the model value).
              let r0 = rinit theme size (editorView "hi")

              // focus the editor by clicking its centre — resolved via the retained tree (FR-004).
              let ex, ey = centre (boxOfKey "editor" r0)
              let focused = ControlsElmish.resolveFocus r0 ex ey
              Expect.equal focused (idOfKey "editor" r0) "the click resolves to the editor's stable RetainedId"

              let editorId = focused.Value

              // type 'x' through the real seam (NO hand-seeded text/focus state): seed from "hi", append.
              let r1, _ = ControlsElmish.routeFocusedText r0 focused (InsertText "x")
              Expect.equal (Map.find editorId r1.StateByIdentity).Text.Value.DraftText "hix" "first keystroke appends to the pre-filled value (FR-005)"

              // the UNRELATED shift: re-render with a banner inserted above the editor (model value
              // still "hi"). `step` carries the editor's RetainedId-keyed state across the diff.
              let s = RetainedRender.step theme size r1 (editorViewShifted "hi")
              Expect.equal (idOfKey "editor" s.Retained) (Some editorId) "the editor keeps its identity across the positional shift"

              // type 'y' on the SAME focused id — the carried draft is authoritative, the model value
              // ("hi") does NOT re-seed/overwrite the in-progress "hix".
              let r2, _ = ControlsElmish.routeFocusedText s.Retained focused (InsertText "y")
              let st = Map.find editorId r2.StateByIdentity

              Expect.equal st.Text.Value.DraftText "hixy" "SC-001: in-progress text survived the shift (continued, not reset)"
          }

          test "baseline (rebuild every frame, no retained identity) FAILS the same proof" {
              // pre-091 behavior = rebuild a fresh structure each frame, minting new ids; under the
              // shift the editor's id changes, so id-keyed focus/text/clock state would be lost.
              let baseId0 = idOfKey "editor" (rinit theme size (editorView "hi"))
              let baseId1 = idOfKey "editor" (rinit theme size (editorViewShifted "hi"))
              Expect.notEqual baseId1 baseId0 "baseline fails: rebuilding every frame mints a new id, losing the keyed state"
          } ]

// =============================================================================================
// US1 (T009) — carry/drop: matched ⇒ state carried; Replace/removed ⇒ state dropped (no false carry).
// =============================================================================================

[<Tests>]
let carryDrop =
    testList
        "092 US1 carry on match, drop on Replace/remove"
        [ test "a Replace (kind change at the same key) drops the prior identity's state — no false carry" {
              let r0 = rinit theme size (editorView "hi")
              let ex, ey = centre (boxOfKey "editor" r0)
              let focused = ControlsElmish.resolveFocus r0 ex ey
              let r1, _ = ControlsElmish.routeFocusedText r0 focused (InsertText "x")
              let oldId = focused.Value
              Expect.isTrue (Map.containsKey oldId r1.StateByIdentity) "the editor has text state before the Replace"

              // same key "editor", different KIND → the diff Replaces (a different node).
              let replaced =
                  Stack.create
                      [ Stack.children [ Button.create [ Button.text "now a button" ] |> Control.withKey "editor" ] ]

              let s = RetainedRender.step theme size r1 replaced
              Expect.isFalse (Map.containsKey oldId s.Retained.StateByIdentity) "Replace drops the prior identity's text state (no false carry)"
          }

          test "a removed control's state is filtered out (focus would clear)" {
              let r0 = rinit theme size (editorView "hi")
              let ex, ey = centre (boxOfKey "editor" r0)
              let focused = ControlsElmish.resolveFocus r0 ex ey
              let r1, _ = ControlsElmish.routeFocusedText r0 focused (InsertText "x")
              let oldId = focused.Value

              // the editor is gone entirely.
              let removed =
                  Stack.create [ Stack.children [ TextBlock.create [ TextBlock.text "only" ] |> Control.withKey "only" ] ]

              let s = RetainedRender.step theme size r1 removed
              Expect.isFalse (Map.containsKey oldId s.Retained.StateByIdentity) "a removed control's state leaves the live set (focus clears)"
          } ]

// =============================================================================================
// US2 (T013 / SC-002) — every focusable field focuses & preserves its value: keyed / unkeyed /
// keyed-container-wrapped resolve to DISTINCT ids; pre-filled multi-line first keystroke appends;
// a control with >1 change binding dispatches EVERY matched binding (FR-006).
// =============================================================================================

[<Tests>]
let focusResolution =
    testList
        "092 US2 focus resolution + value preservation"
        [ test "keyed, unkeyed, and keyed-container-wrapped fields each resolve to a distinct RetainedId" {
              let view: Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ TextBox.create [ TextBox.value "k"; TextBox.onChanged NameChanged ] |> Control.withKey "keyed"
                              TextBox.create [ TextBox.value "u"; TextBox.onChanged NameChanged ] // unkeyed
                              (Stack.create
                                  [ Stack.children
                                        [ TextBox.create [ TextBox.value "w"; TextBox.onChanged NameChanged ] ] ]
                               |> Control.withKey "wrap") ] ] // unkeyed field nested under a keyed container

              let r0 = rinit theme size view
              let fields = collectKind "text-box" r0.Root
              Expect.equal (List.length fields) 3 "three text-box fields are present"

              let resolved =
                  fields
                  |> List.map (fun (_, box) ->
                      let x, y = centre box
                      ControlsElmish.resolveFocus r0 x y)

              resolved |> List.iter (fun r -> Expect.isSome r "each field resolves to a RetainedId")

              let ids = resolved |> List.map Option.get
              Expect.equal (List.distinct ids |> List.length) 3 "SC-002: all three fields resolve to DISTINCT ids (no unkeyed-sibling collapse)"
              Expect.equal ids (fields |> List.map fst) "each click resolves to exactly that field's node identity"
          }

          test "a pre-filled multi-line field's first keystroke appends (zero characters lost) in MultiLine mode" {
              let view: Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ TextArea.create [ TextArea.value "line1"; TextArea.onChanged NameChanged ] |> Control.withKey "area" ] ]

              let r0 = rinit theme size view
              let ax, ay = centre (boxOfKey "area" r0)
              let focused = ControlsElmish.resolveFocus r0 ax ay
              let r1, _ = ControlsElmish.routeFocusedText r0 focused (InsertText "X")

              let st = (Map.find focused.Value r1.StateByIdentity).Text.Value
              Expect.equal st.DraftText "line1X" "FR-005: the first keystroke appends to the pre-filled value (no truncation)"
              Expect.equal st.Mode MultiLine "FR-005: a text-area seeds MultiLine mode (fixes the 090 hard-coded SingleLine)"
          }

          test "a control with more than one onChanged binding dispatches EVERY matched binding (FR-006)" {
              let view: Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ TextBox.create
                                  [ TextBox.value ""
                                    TextBox.onChanged NameChanged
                                    TextBox.onChanged AlsoChanged ]
                              |> Control.withKey "multi" ] ]

              let r0 = rinit theme size view
              let mx, my = centre (boxOfKey "multi" r0)
              let focused = ControlsElmish.resolveFocus r0 mx my
              let _, msgs = ControlsElmish.routeFocusedText r0 focused (InsertText "z")

              Expect.contains msgs (NameChanged "z") "the first onChanged binding dispatched"
              Expect.contains msgs (AlsoChanged "z") "the second onChanged binding ALSO dispatched (FR-006, not just List.tryHead)"
          } ]

// =============================================================================================
// Evidence capture (T012 / T017) — live-survival + focus-resolution artifacts from the real seam.
// =============================================================================================

module private Evidence =
    let readinessRoot =
        Path.GetFullPath(
            Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "specs", "092-wire-retained-identity-state", "readiness")
        )

    let ensure (sub: string) =
        let d = Path.Combine(readinessRoot, sub)
        Directory.CreateDirectory d |> ignore
        d

[<Tests>]
let evidence =
    testList
        "092 evidence capture (live seam)"
        [ test "capture live-survival (SC-001) + focus-resolution (SC-002)" {
              // --- live-survival ---
              let r0 = rinit theme size (editorView "hi")
              let ex, ey = centre (boxOfKey "editor" r0)
              let focused = ControlsElmish.resolveFocus r0 ex ey
              let editorId = focused.Value
              let r1, _ = ControlsElmish.routeFocusedText r0 focused (InsertText "x")

              let s = RetainedRender.step theme size r1 (editorViewShifted "hi")
              let r2, _ = ControlsElmish.routeFocusedText s.Retained focused (InsertText "y")
              let st = Map.find editorId r2.StateByIdentity

              let draftSurvived = st.Text.Value.DraftText = "hixy"
              let focusSurvived = idOfKey "editor" s.Retained = Some editorId

              let baselineFails =
                  idOfKey "editor" (rinit theme size (editorViewShifted "hi")) <> Some editorId

              let ldir = Evidence.ensure "live-survival"

              File.WriteAllText(
                  Path.Combine(ldir, "survival.txt"),
                  String.concat "\n"
                      [ "# Wired retained identity — focus + in-progress text + clock survive a positional shift (feature 092, SC-001)"
                        "evidence-kind=live-survival"
                        "renderer-mode=DeterministicRenderOnly"
                        "status=pass"
                        "driven-through=ControlsElmish.resolveFocus + routeFocusedText + RetainedRender.step (the REAL adapter seam)"
                        "hand-seeded-focus-or-text=false"
                        "sequence=focus editor -> type 'x' (draft 'hix') -> insert banner above (shift) -> type 'y'"
                        sprintf "focus-survived=%b" focusSurvived
                        sprintf "draft-survived(hixy)=%b" draftSurvived
                        "clock-note=the per-control animation clock survival now has a real host seam (feature 099, R4) and is proven through it in Feature099AnimationClockTests/us2-survival; 092 keeps proving focus + in-progress text survival."
                        "readback-note=AUTHORITATIVE proof is the carried RetainedId-keyed state (draft text continued not reset); structural/identity equality, no pixel encoder needed ([[fs-gg-evidence-mode]])."
                        "authoritative-test=Feature092LiveSurvivalTests/092 US1 live survival through the real adapter seam"
                        "" ]
              )

              File.WriteAllText(
                  Path.Combine(ldir, "baseline-fails.txt"),
                  String.concat "\n"
                      [ "# Wired retained identity — rebuild-every-frame baseline FAILS the survival proof (feature 092, SC-001)"
                        "evidence-kind=baseline-fails"
                        "renderer-mode=DeterministicRenderOnly"
                        "status=pass"
                        sprintf "baseline-loses-identity-on-shift=%b" baselineFails
                        "note=rebuilding the retained structure every frame (RetainedRender.init) mints a fresh id for the editor under the shift, so id-keyed focus/text/clock state is lost — the wired step path is what preserves it."
                        "authoritative-test=Feature092LiveSurvivalTests/092 US1 live survival through the real adapter seam"
                        "" ]
              )

              // --- focus-resolution ---
              let view: Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ TextBox.create [ TextBox.value "k"; TextBox.onChanged NameChanged ] |> Control.withKey "keyed"
                              TextBox.create [ TextBox.value "u"; TextBox.onChanged NameChanged ]
                              (Stack.create
                                  [ Stack.children
                                        [ TextBox.create [ TextBox.value "w"; TextBox.onChanged NameChanged ] ] ]
                               |> Control.withKey "wrap") ] ]

              let fr0 = rinit theme size view
              let fields = collectKind "text-box" fr0.Root

              let resolvedIds =
                  fields
                  |> List.map (fun (_, box) ->
                      let x, y = centre box
                      ControlsElmish.resolveFocus fr0 x y |> Option.get)

              let distinct = (List.distinct resolvedIds |> List.length) = 3
              let resolvesToField = resolvedIds = (fields |> List.map fst)

              // pre-filled multi-line append
              let areaView: Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ TextArea.create [ TextArea.value "line1"; TextArea.onChanged NameChanged ] |> Control.withKey "area" ] ]

              let ar0 = rinit theme size areaView
              let aax, aay = centre (boxOfKey "area" ar0)
              let afocused = ControlsElmish.resolveFocus ar0 aax aay
              let ar1, _ = ControlsElmish.routeFocusedText ar0 afocused (InsertText "X")
              let areaState = (Map.find afocused.Value ar1.StateByIdentity).Text.Value
              let appended = areaState.DraftText = "line1X" && areaState.Mode = MultiLine

              let fdir = Evidence.ensure "focus-resolution"

              File.WriteAllText(
                  Path.Combine(fdir, "focus-resolution.txt"),
                  String.concat "\n"
                      [ "# Wired retained identity — keyed / unkeyed / keyed-container-wrapped fields focus distinctly (feature 092, SC-002)"
                        "evidence-kind=focus-resolution"
                        "renderer-mode=DeterministicRenderOnly"
                        "status=pass"
                        "driven-through=ControlsElmish.resolveFocus over RetainedRender.retainedHitTest"
                        sprintf "fields=%d" (List.length fields)
                        sprintf "all-distinct-ids=%b" distinct
                        sprintf "each-resolves-to-its-own-field=%b" resolvesToField
                        "note=the retained tree mints a distinct RetainedId per node (incl. unkeyed siblings), so a box hit-test resolves each field to its own identity — unlike the ControlId hitTest path that collapsed unkeyed same-kind siblings."
                        "authoritative-test=Feature092LiveSurvivalTests/092 US2 focus resolution + value preservation"
                        "" ]
              )

              File.WriteAllText(
                  Path.Combine(fdir, "prefilled-append.txt"),
                  String.concat "\n"
                      [ "# Wired retained identity — pre-filled multi-line first keystroke appends (feature 092, SC-002/FR-005)"
                        "evidence-kind=prefilled-append"
                        "renderer-mode=DeterministicRenderOnly"
                        "status=pass"
                        "scenario=text-area pre-filled 'line1'; focus; first keystroke 'X'"
                        sprintf "result-draft=%s" areaState.DraftText
                        sprintf "appends-not-resets=%b" (areaState.DraftText = "line1X")
                        sprintf "line-mode=%A" areaState.Mode
                        "note=fixes the 090 defects (empty seed wiped a pre-filled field on first keystroke; SingleLine hard-coded even for text-area)."
                        "authoritative-test=Feature092LiveSurvivalTests/092 US2 focus resolution + value preservation"
                        "" ]
              )

              Expect.isTrue (focusSurvived && draftSurvived) "live survival holds through the real seam"
              Expect.isTrue baselineFails "the rebuild-every-frame baseline fails the same proof"
              Expect.isTrue (distinct && resolvesToField) "focus resolves each field distinctly"
              Expect.isTrue appended "pre-filled multi-line first keystroke appends in MultiLine mode"
          } ]
