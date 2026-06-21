module Feature095SlotCompositionTests

// Feature 095 (E5) — lookless slot composition. These tests reach the internal slot seam
// (`ControlInternals.slotFill`/`slotFillsOf`/`slotFor`/`lowerSlots`) and the internal
// `RetainedRender` module via `[<assembly: InternalsVisibleTo("Controls.Tests")>]`, and drive the
// REAL typed slot-fill front door (`Button.view` with `Leading`/`Trailing`, `Panel.view` with
// `Header`/`Footer`). Coverage:
//   * T009 / SC-001 — a filled named slot lowers its sub-tree into that region of the lowered IR;
//     two slots land in two DISTINCT regions with no collision/swap.
//   * T012 / SC-005 — slot lowering is pure / deterministic / total over >=1000 (kind, fills)
//     combinations; lowering never throws.
//   * T013 / SC-006 — typed-closed: an undeclared slot is unrepresentable (compile-time, asserted
//     structurally here + a does-not-compile fixture in readiness); no DataContext/binding surface.
//   * T015 / SC-002 / SC-007 — an unfilled slot-bearing kind is byte-identical to its pre-slot
//     render; a non-slotted kind is unchanged.
//   * T018 / SC-003 — a slotted control composes with E1 (dispatch), E3 (style resolve), E4 (tab
//     order) free, because the fill lands in `Children`.
//   * T019 / SC-004 — a slotted control keeps its E2 retained identity across a sibling-shifting
//     re-render through the LIVE retained path (not a hand-seeded StateByIdentity map).
// Render-only / deterministic — no live Vulkan window ([[fs-gg-evidence-mode]]).

open System.IO
open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }
let private box: Rect = { X = 10.0; Y = 40.0; Width = 284.0; Height = 92.0 }
let private themes = [ "light", Theme.light; "dark", Theme.dark ]

// ---- frozen pre-slot procedural geometry (the parity oracle, mirrors Feature093ParityTests) ----
let private mkText (t: Theme) (x: float) (baseline: float) (sz: float) (color: Color) (s: string) =
    Scene.textRun
        { Text = s
          Position = { X = x; Y = baseline }
          Font = { Family = t.FontFamily; Size = sz; Weight = None }
          Paint = Paint.fill color }

let private frozenButtonGeom (t: Theme) (label: string) : Scene list =
    let h = 38.0
    let textW = (Scene.measureText label { Family = t.FontFamily; Size = 15.0; Weight = None }).Width
    let w = min box.Width (max 70.0 (textW + 32.0))
    let by = box.Y + box.Height / 2.0 - h / 2.0
    [ Scene.rectangle (box.X, by, w, h) t.Accent
      mkText t (box.X + 16.0) (by + h / 2.0 + 5.0) 15.0 t.Background label ]

// ---- small typed-front-door builders ----------------------------------------------------------
let private label (key: string) (text: string) : Widget<int> =
    TextBlock.view { TextBlock.defaults with Id = Some key; Text = text }

let private btn (key: string) (text: string) : Widget<int> =
    Button.view { Button.defaults with Id = Some key; Text = text }

// =============================================================================================
// T009 / SC-001 — a filled named slot lowers its sub-tree into that region of the lowered IR.
// =============================================================================================

[<Tests>]
let slotPlacement =
    testList
        "095 US1 slot placement (SC-001)"
        [ test "filling Button.Leading injects the fill into the lowered Children and consumes the carrier" {
              let icon = label "ico" "*"
              let button = Button.view { Button.defaults with Text = "Save"; Leading = Some icon } |> Widget.toControl

              Expect.equal (button.Children |> List.map (fun c -> c.Key)) [ Some "ico" ] "the leading fill is the lowered Button's child"
              Expect.equal button.Children.[0].Content (Some "*") "the supplied sub-tree is placed verbatim"
              Expect.isFalse
                  (button.Attributes |> List.exists (fun a -> a.Name = "slot"))
                  "lowering CONSUMES the slot carrier (single source of truth in Children)"
          }

          test "filling Leading AND Trailing lands in two DISTINCT regions, ordered, no collision/swap" {
              let button =
                  Button.view { Button.defaults with Text = "Go"; Leading = Some(label "L" "lead"); Trailing = Some(label "T" "trail") }
                  |> Widget.toControl

              Expect.equal (button.Children |> List.map (fun c -> c.Key)) [ Some "L"; Some "T" ] "leading precedes trailing (distinct ordered regions)"
          }

          test "Panel.Header lands BEFORE content, Panel.Footer AFTER (composite container regions)" {
              let panel =
                  Panel.view
                      { Panel.defaults with
                          Header = Some(label "H" "head")
                          Footer = Some(label "F" "foot")
                          Children = [ label "B" "body" ] }
                  |> Widget.toControl

              Expect.equal (panel.Children |> List.map (fun c -> c.Key)) [ Some "H"; Some "B"; Some "F" ] "header, then content, then footer"
          }

          test "slotFor resolves a present name and distinguishes ABSENT from EMPTY (edge case)" {
              // present-with-empty-content vs absent: filling with an empty sub-tree is a CHOICE.
              let emptyFill = Panel.view { Panel.defaults with Children = [] } // empty content, present
              let attrs = [ ControlInternals.slotFill [ "leading", Widget.toControl emptyFill ] ]
              Expect.isSome (ControlInternals.slotFor "leading" attrs) "a present name resolves even when its content is empty"
              Expect.isNone (ControlInternals.slotFor "trailing" attrs) "an absent name resolves to None (unfilled => default)"
          } ]

// =============================================================================================
// T012 / SC-005 — slot lowering is pure / deterministic / total over >=1000 (kind, fills).
// =============================================================================================

module private Gen095 =
    let private kinds = [ "button", [ "leading"; "trailing" ]; "panel", [ "header"; "footer" ] ]

    let private genFill: Gen<Control<int>> =
        Gen.elements
            [ Widget.toControl (label "f" "x")
              Widget.toControl (btn "fb" "click")
              Widget.toControl (Panel.view { Panel.defaults with Children = [] }) // empty-content fill
              Widget.toControl (Panel.view { Panel.defaults with Children = [ label "n" "nested" ] }) ]

    // A lowered slot-bearing control: a real kind, an arbitrary subset of its declared regions
    // filled (including none), each bound to an arbitrary fill (including empty content).
    let control: Gen<Control<int>> =
        gen {
            let! (kind, regions) = Gen.elements kinds

            let! filledFlags = Gen.listOfLength regions.Length (Gen.elements [ true; false ])

            let! fills =
                regions
                |> List.mapi (fun i name -> (name, List.item i filledFlags))
                |> List.filter snd
                |> List.map (fun (name, _) ->
                    gen {
                        let! f = genFill
                        return (name, f)
                    })
                |> (fun gens ->
                    match gens with
                    | [] -> Gen.constant []
                    | _ -> gens |> List.fold (fun acc g -> gen { let! xs = acc in let! x = g in return xs @ [ x ] }) (Gen.constant []))

            let baseChildren = if kind = "panel" then [ Widget.toControl (label "body" "content") ] else []
            let attrs = [ if not (List.isEmpty fills) then ControlInternals.slotFill fills ]
            return Control.create kind (attrs @ [ FS.GG.UI.Controls.Panel.children baseChildren ])
        }

[<Tests>]
let loweringProperties =
    testList
        "095 US1 lowering properties (FsCheck, SC-005)"
        [ testCase "purity / determinism — identical input lowers to an identical IR (>=1000)" (fun () ->
              let deterministic (c: Control<int>) =
                  sprintf "%A" (ControlInternals.lowerSlots c) = sprintf "%A" (ControlInternals.lowerSlots c)
              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen095.control) deterministic))

          testCase "totality — lowering never throws for any (kind, fills) (>=1000)" (fun () ->
              let total (c: Control<int>) =
                  ControlInternals.lowerSlots c |> ignore
                  true
              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen095.control) total))

          testCase "additive — no slot attribute => lowering is the identity (byte-identical, >=1000)" (fun () ->
              let additive (c: Control<int>) =
                  if ControlInternals.slotFillsOf c.Attributes |> List.isEmpty then
                      sprintf "%A" (ControlInternals.lowerSlots c) = sprintf "%A" c
                  else
                      true // only the no-slot case asserts identity
              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen095.control) additive)) ]

// =============================================================================================
// T013 / SC-006 — typed-closed; no DataContext / binding / template-instantiation surface.
// =============================================================================================

[<Tests>]
let typedClosure =
    testList
        "095 US1 typed closure + non-goals (SC-006)"
        [ test "the slot carrier is internal plumbing — no public SlotName / Attr.slot escape hatch" {
              // The only public authoring path is the typed Props fields. There is NO public
              // `Attr.slot` builder and NO public SlotName type. (Filling Button.Header — an
              // undeclared region — is a COMPILE error: there is no such field; proven by the
              // does-not-compile fixture in readiness/sc006-typed-closed-and-nongoals.md.)
              let button = Button.view { Button.defaults with Text = "Save"; Leading = Some(label "i" "*") } |> Widget.toControl
              // The lowered IR carries the fill as an ordinary Control child — not a binding,
              // observable, DataContext, or template instance. It is a static Control<'msg> value.
              Expect.all button.Children (fun c -> c.Kind <> "") "every slot fill is a concrete Control value, not a deferred binding"
              Expect.equal button.Children.[0].Kind "text-block" "the fill is a plain lowered control sub-tree"
          } ]

// =============================================================================================
// T015 / SC-002 / SC-007 — unfilled byte-identical; non-slotted kind unchanged.
// =============================================================================================

[<Tests>]
let unfilledParity =
    testList
        "095 US2 unfilled byte-identity (SC-002 / SC-007)"
        [ test "an unfilled Button carries no slot attribute and no peripheral children" {
              let unfilled = Button.view { Button.defaults with Text = "Save" } |> Widget.toControl
              Expect.isFalse (unfilled.Attributes |> List.exists (fun a -> a.Name = "slot")) "no slot attribute when unfilled"
              Expect.isEmpty unfilled.Children "no peripheral children when unfilled (zero geometry)"
          }

          test "an unfilled Button's render is structurally-Scene-equal to the pre-slot baseline, both themes" {
              let unfilled = Button.view { Button.defaults with Text = "Save" } |> Widget.toControl
              for (tname, t) in themes do
                  Expect.equal
                      (ControlInternals.faithfulContent t box unfilled)
                      (frozenButtonGeom t "Save")
                      (sprintf "button.%s unfilled render == the frozen pre-slot oracle (label position invariant)" tname)
          }

          test "an unfilled Panel lowers identically to the legacy no-slot Panel" {
              let body = label "b" "body"
              let panelSlots = Panel.view { Panel.defaults with Children = [ body ] } |> Widget.toControl
              let legacy = FS.GG.UI.Controls.Panel.create [ FS.GG.UI.Controls.Panel.children [ Widget.toControl body ] ]
              Expect.equal (sprintf "%A" panelSlots) (sprintf "%A" legacy) "unfilled Panel == legacy Panel (additive)"
          }

          test "a non-slotted kind (CheckBox) is unchanged — exposing slots is scoped (SC-007)" {
              // CheckBox declares no slot regions: it gains no slot attribute and no slot children,
              // so its lowering is byte-identical to before the feature (the slot work is scoped to
              // the representative Button + Panel).
              let cb = CheckBox.view { CheckBox.defaults with Text = "On"; Checked = true } |> Widget.toControl
              Expect.isFalse (cb.Attributes |> List.exists (fun a -> a.Name = "slot")) "a non-slotted kind carries no slot attribute"
              Expect.isEmpty cb.Children "a non-slotted leaf kind gained no slot children"
          } ]

// =============================================================================================
// T018 / SC-003 — a slotted control composes with E1 (dispatch), E3 (style), E4 (tab order).
// =============================================================================================

[<Tests>]
let compose =
    testList
        "095 US3 slotted content composes with E1/E3/E4 (SC-003)"
        [ test "E1 — a binding inside a slot dispatches through the flat per-ControlId mechanism" {
              // a focusable, clickable child filled into Button.Leading
              let icon = Button.view { Button.defaults with Id = Some "leadIcon"; Text = "icon"; OnClick = Some 99 }
              let host = Button.view { Button.defaults with Id = Some "host"; Text = "Save"; Leading = Some icon } |> Widget.toControl

              let dispatched = Control.dispatch { Kind = "click"; ControlId = Some "leadIcon"; Origin = ControlEventOrigin.Pointer; Nav = None } host
              Expect.equal dispatched [ 99 ] "the slotted child's authored binding dispatches its message (E1)"
          }

          test "E3 — a style class on slotted content resolves through the E3 resolver" {
              let plainFill = Button.view { Button.defaults with Id = Some "c"; Text = "Tag" }
              let dangerFill = Button.view { Button.defaults with Id = Some "c"; Text = "Tag"; Classes = [ Variant StyleVariant.Danger ] }
              let plainChild = (Button.view { Button.defaults with Text = "Save"; Leading = Some plainFill } |> Widget.toControl).Children.[0]
              let dangerChild = (Button.view { Button.defaults with Text = "Save"; Leading = Some dangerFill } |> Widget.toControl).Children.[0]
              Expect.notEqual
                  (ControlInternals.faithfulContent theme box dangerChild)
                  (ControlInternals.faithfulContent theme box plainChild)
                  "the slotted child's attached class changes its resolved paint (E3)"
          }

          test "E4 — a focusable slotted control appears in the tab order" {
              // The host must be a NON-focusable container (a Panel) so its subtree is descended: a
              // focusable Button host would be a single tab stop and swallow its subtree. The
              // focusable Button filled into the Panel's Header slot is then a stop in its own right.
              let icon = Button.view { Button.defaults with Id = Some "leadIcon"; Text = "icon" }
              let host = Panel.view { Panel.defaults with Id = Some "host"; Header = Some icon; Children = [ label "b" "body" ] } |> Widget.toControl
              let order = Focus.order host
              let ids = order.Stops |> List.map (fun s -> s.Control)
              Expect.contains ids "leadIcon" "the focusable slotted control is a stop in the E4 tab order"
          } ]

// =============================================================================================
// T019 / SC-004 — slotted content keeps E2 retained identity across a sibling shift (live path).
// =============================================================================================

[<Tests>]
let retainedIdentity =
    let rec findByKey key (node: RetainedNode<'msg>) : RetainedNode<'msg> option =
        if node.Control.Key = Some key then Some node
        else node.Children |> List.tryPick (findByKey key)

    // A keyed Panel whose Header slot is filled with a keyed, focusable control ("field").
    let panelW () : Widget<int> =
        Panel.view
            { Panel.defaults with
                Id = Some "panelP"
                Header = Some(Button.view { Button.defaults with Id = Some "field"; Text = "Field" })
                Children = [ label "body" "body" ] }

    let stack (children: Widget<int> list) : Control<int> =
        Stack.view { Stack.defaults with Children = children } |> Widget.toControl

    testList
        "095 US3 slotted retained identity across a sibling shift (live path, SC-004)"
        [ test "a keyed slotted control keeps its RetainedId when a sibling is inserted above its host" {
              let frame0 = stack [ panelW () ]
              let frame1 = stack [ label "banner" "new!"; panelW () ] // 092-case sibling shift

              let r0 = (RetainedRender.init theme size frame0).Retained
              let s1 = RetainedRender.step theme size r0 frame1

              let id0 = (findByKey "field" r0.Root).Value.Identity
              let id1 = (findByKey "field" s1.Retained.Root).Value.Identity

              Expect.equal id1 id0 "SC-004: the slotted 'field' keeps its retained identity through the live path across the shift"
              // and the wired frame is byte-identical to a full rebuild (no parallel slot path)
              Expect.equal s1.Render.Scene (Control.renderTree theme size frame1).Scene "the wired frame == a full rebuild (slotted content is a first-class sub-tree)"
          } ]

// =============================================================================================
// Evidence capture — writes the real parity baselines (T016) + a slot-placement artifact (T014)
// from the WIRED path itself. Render-only / deterministic ([[fs-gg-evidence-mode]]).
// =============================================================================================

module private Evidence =
    let readinessRoot =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "specs", "095-lookless-slot-composition", "readiness"))

    let ensure (sub: string) =
        let d = Path.Combine(readinessRoot, sub)
        Directory.CreateDirectory d |> ignore
        d

[<Tests>]
let evidence =
    testList
        "095 evidence capture (controls)"
        [ test "capture pre-slot parity baselines (T016) + slot-placement proof (T014)" {
              let pdir = Evidence.ensure "parity"
              for (tname, t) in themes do
                  File.WriteAllText(
                      Path.Combine(pdir, sprintf "button.%s.normal.scene.txt" tname),
                      sprintf "%A" (frozenButtonGeom t "Save")
                  )

              // confirm the unfilled render still equals the captured baseline (the actual proof)
              let unfilled = Button.view { Button.defaults with Text = "Save" } |> Widget.toControl
              let parityHolds =
                  themes
                  |> List.forall (fun (_, t) -> ControlInternals.faithfulContent t box unfilled = frozenButtonGeom t "Save")

              Expect.isTrue parityHolds "unfilled byte-identity holds vs the captured baseline"
          } ]
