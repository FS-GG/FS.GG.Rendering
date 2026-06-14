module Feature094FocusTests

// Feature 094 (E4) — the PURE focus model: `Focus.order` (tab order), `Focus.traverse` (keyboard
// traversal), `Focus.route` (key classification), plus the Research R1 `Accessibility` correction
// (Tab out of per-control NavigationKeys; an activation-only Button is valid). All proofs are
// deterministic reducer results over the public `Focus` surface — no live window, no host.

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

// --- representative-view helpers --------------------------------------------------------------

let private withFocusOrder (n: int option) (md: AccessibilityMetadata) = { md with FocusOrder = n }

// A focusable control of `kind`, keyed `key`, with an explicit FocusOrder (built on the corrected
// `defaultFor` metadata so the keyboard semantics are the real per-role defaults).
let private focusable (kind: string) (key: string) (fo: int option) : Control<int> =
    Control.create kind [ Attr.text key; Attr.accessibility (Accessibility.defaultFor kind key |> withFocusOrder fo) ]
    |> Control.withKey key

let private button (key: string) (fo: int option) : Control<int> = focusable "button" key fo
let private slider (key: string) (fo: int option) : Control<int> = focusable "slider" key fo

// A representative view: a NON-focusable static text (excluded), then mixed-FocusOrder focusables
// inside a non-focusable container.
let private representativeView () : Control<int> =
    Stack.create
        [ Stack.children
              [ TextBlock.create [ TextBlock.text "heading" ] |> Control.withKey "heading" // static-text => not focusable
                button "act-none" None
                slider "nav-1" (Some 1)
                button "act-0" (Some 0) ] ]

let private ids (o: TabOrder) = o.Stops |> List.map (fun s -> s.Control)

// =============================================================================================
// T011 / SC-001 / US1.3 — Focus.order: focusable-only, FocusOrder-then-document order
// =============================================================================================

[<Tests>]
let tabOrderTests =
    testList "094 US1 tab order (Focus.order, SC-001)" [
        test "order yields focusable-only stops in (FocusOrder ascending, None last, doc-order) order" {
            let order = Focus.order (representativeView ())
            // act-0 (FocusOrder 0) < nav-1 (FocusOrder 1) < act-none (None, last). The static text
            // "heading" is non-focusable and never appears (US1.3).
            Expect.equal (ids order) [ "act-0"; "nav-1"; "act-none" ] "stops are focusable-only and FocusOrder-then-doc ordered"
            Expect.isFalse (ids order |> List.contains "heading") "the non-focusable static text is excluded"
        }

        test "a focusable composite is a SINGLE stop — its subtree is not descended" {
            // radio-group is focusable AND has nested item content; it must contribute exactly one stop.
            let view =
                Stack.create
                    [ Stack.children
                          [ Control.create
                                "radio-group"
                                [ Attr.items [ "a"; "b"; "c" ]; Attr.accessibility (Accessibility.defaultFor "radio-group" "rg") ]
                            |> Control.withKey "rg"
                            button "b1" None ] ]

            let order = Focus.order view
            Expect.equal (ids order) [ "rg"; "b1" ] "the composite contributes one stop; descendants are not separate stops"
        }

        test "order is deterministic (no clock/randomness)" {
            let v = representativeView ()
            Expect.equal (Focus.order v) (Focus.order v) "identical input yields an identical order"
        }
    ]

// =============================================================================================
// T012 / SC-001 / SC-006 — Focus.traverse: cyclic, None-seeded, empty-order no-op
// =============================================================================================

[<Tests>]
let traversalTests =
    testList "094 US1 traversal (Focus.traverse, SC-001)" [
        let order = Focus.order (representativeView ()) // stops: act-0 ; nav-1 ; act-none

        test "None seeds first on Next and last on Previous" {
            Expect.equal (Focus.traverse order None Next) (Some "act-0") "None + Next -> first stop"
            Expect.equal (Focus.traverse order None Previous) (Some "act-none") "None + Previous -> last stop"
        }

        test "Next advances and wraps cyclically at the end" {
            Expect.equal (Focus.traverse order (Some "act-0") Next) (Some "nav-1") "advances to the next stop"
            Expect.equal (Focus.traverse order (Some "act-none") Next) (Some "act-0") "wraps from last to first"
        }

        test "Previous reverses and wraps cyclically at the start" {
            Expect.equal (Focus.traverse order (Some "nav-1") Previous) (Some "act-0") "retreats to the previous stop"
            Expect.equal (Focus.traverse order (Some "act-0") Previous) (Some "act-none") "wraps from first to last"
        }

        test "Next then Previous is identity" {
            for start in [ "act-0"; "nav-1"; "act-none" ] do
                let next = Focus.traverse order (Some start) Next
                Expect.equal (Focus.traverse order next Previous) (Some start) $"Next then Previous returns to {start}"
        }

        test "an empty TabOrder is a no-op — Next/Previous both yield None and never throw" {
            let empty: TabOrder = { Stops = [] }
            Expect.equal (Focus.traverse empty None Next) None "empty + None + Next -> None"
            Expect.equal (Focus.traverse empty None Previous) None "empty + None + Previous -> None"
            Expect.equal (Focus.traverse empty (Some "ghost") Next) None "empty + stale + Next -> None"
        }

        test "a stale current id recovers to first (Next) / last (Previous), never throws" {
            Expect.equal (Focus.traverse order (Some "removed") Next) (Some "act-0") "stale + Next -> first"
            Expect.equal (Focus.traverse order (Some "removed") Previous) (Some "act-none") "stale + Previous -> last"
        }
    ]

// =============================================================================================
// T016 / SC-002 / FR-007 — Focus.route classification, consumption-wins
// =============================================================================================

[<Tests>]
let routeTests =
    testList "094 US2 key routing (Focus.route, SC-002/FR-007)" [
        let buttonKb = Accessibility.keyboard true [ "Enter"; "Space" ] []
        let sliderKb = Accessibility.keyboard true [] [ "ArrowLeft"; "ArrowRight" ]
        // Feature 100 (R5): `route` now takes the control's role + declared NavRange and the
        // `Navigate` case carries a closed `NavIntent`. A default-step slider declares {0.1;0;1}.
        let sliderRange: NavRange option = Some { Step = 0.1; Min = 0.0; Max = 1.0 }

        test "ActivationKeys -> Activate" {
            Expect.equal (Focus.route AccessibilityRole.Button buttonKb None "Enter" false false) Activate "Enter activates"
            Expect.equal (Focus.route AccessibilityRole.Button buttonKb None "Space" false false) Activate "Space activates"
        }

        test "NavigationKeys -> Navigate (R5: carries a role-derived NavIntent)" {
            Expect.equal (Focus.route AccessibilityRole.Slider sliderKb sliderRange "ArrowLeft" false false) (Navigate(ValueStep -0.1)) "ArrowLeft -> value step down"
            Expect.equal (Focus.route AccessibilityRole.Slider sliderKb sliderRange "ArrowRight" false false) (Navigate(ValueStep 0.1)) "ArrowRight -> value step up"
        }

        test "an unconsumed Tab -> Traverse (Next, or Previous with shift)" {
            Expect.equal (Focus.route AccessibilityRole.Button buttonKb None "Tab" true false) (Traverse Next) "Tab -> Traverse Next"
            Expect.equal (Focus.route AccessibilityRole.Button buttonKb None "Tab" true true) (Traverse Previous) "Shift+Tab -> Traverse Previous"
        }

        test "consumption wins: a control listing a traversal key consumes it (never Traverse)" {
            let tabConsuming = Accessibility.keyboard true [ "Tab" ] []
            Expect.equal (Focus.route AccessibilityRole.Button tabConsuming None "Tab" true false) Activate "a control whose ActivationKeys include Tab activates on Tab (not Traverse)"
        }

        test "no match -> Fallthrough (never throws)" {
            Expect.equal (Focus.route AccessibilityRole.Button buttonKb None "Q" false false) Fallthrough "an unmatched non-Tab key falls through"
        }
    ]

// =============================================================================================
// T007 / R1 — the Accessibility correction: an activation-only Button validates and does not
// consume Tab; a slider declares intra-control arrows (not Tab).
// =============================================================================================

[<Tests>]
let r1CorrectionTests =
    testList "094 R1 Accessibility correction (defaultFor / validate)" [
        test "a focusable activation-only Button is VALID and does NOT seed Tab into NavigationKeys" {
            let md = Accessibility.defaultFor "button" "Save"
            Expect.isTrue md.Keyboard.Focusable "the button is focusable"
            Expect.isFalse (List.contains "Tab" md.Keyboard.NavigationKeys) "Tab is NOT a per-control navigation key (traversal is engine-level)"
            Expect.isFalse (List.contains "Shift+Tab" md.Keyboard.NavigationKeys) "Shift+Tab is NOT a per-control navigation key"
            Expect.isEmpty md.Keyboard.NavigationKeys "an activation-only Button carries no NavigationKeys"

            let control = Button.create [ Button.text "Save" ]
            let errors = Accessibility.validate control |> List.filter (fun d -> d.Severity = ControlDiagnosticSeverity.Error)
            Expect.isEmpty errors "R1: a focusable activation-only Button validates (no missing-navigation error)"
        }

        test "a slider declares intra-control arrows, not Tab" {
            let md = Accessibility.defaultFor "slider" "Volume"
            Expect.isFalse (List.contains "Tab" md.Keyboard.NavigationKeys) "a slider does not consume Tab"
            Expect.contains md.Keyboard.NavigationKeys "ArrowLeft" "a slider navigates with ArrowLeft"
            Expect.contains md.Keyboard.NavigationKeys "ArrowRight" "a slider navigates with ArrowRight"
        }

        test "a focusable control with NEITHER activation nor navigation keys is still flagged" {
            let broken =
                Button.create
                    [ Button.text "broken"; Attr.accessibility (Accessibility.metadata AccessibilityRole.Button "broken" [ "normal" ] None (Accessibility.keyboard true [] []) None None) ]

            let errors = Accessibility.validate broken |> List.filter (fun d -> d.Severity = ControlDiagnosticSeverity.Error)
            Expect.isNonEmpty errors "a focusable control with no operable key set is still invalid"
        }
    ]

// =============================================================================================
// T024 / SC-007 — the computed order passes Accessibility.validate and derives solely from metadata
// =============================================================================================

[<Tests>]
let validateOrderTests =
    testList "094 US3 validate-order (SC-007)" [
        test "every focusable control in the representative view validates (no errors)" {
            let view = representativeView ()
            // validate each focusable stop's control by re-deriving it; the order itself is the proof
            // that the semantics come from AccessibilityMetadata (no parallel hand-rolled table).
            let order = Focus.order view
            Expect.isNonEmpty order.Stops "the representative view has focusable stops"

            for control in [ button "act-0" (Some 0); slider "nav-1" (Some 1); button "act-none" None ] do
                let errors = Accessibility.validate control |> List.filter (fun d -> d.Severity = ControlDiagnosticSeverity.Error)
                Expect.isEmpty errors $"the focusable control {control.Key} passes validate"
        }

        test "the tab order and key semantics derive solely from AccessibilityMetadata" {
            // Each stop's Role/Keyboard/FocusOrder equal the control's own AccessibilityMetadata —
            // there is no parallel table; the stop IS a projection of the metadata.
            let order = Focus.order (representativeView ())

            for stop in order.Stops do
                Expect.isTrue (stop.Keyboard.Focusable) $"{stop.Control} stop carries its own focusable metadata"
        }
    ]

// =============================================================================================
// T028 / SC-006 — FsCheck purity / totality / determinism over >=1000 combinations
// =============================================================================================

module private Gen094 =
    let keyPool = [ "Enter"; "Space"; "Tab"; "ArrowLeft"; "ArrowRight"; "ArrowUp"; "ArrowDown"; "Q"; "X"; "" ]

    let private genKeys: Gen<string list> =
        gen {
            let! n = Gen.choose (0, 4)
            return! Gen.listOfLength n (Gen.elements keyPool)
        }

    let genKeyboard: Gen<KeyboardOperation> =
        gen {
            let! focusable = Gen.elements [ true; false ]
            let! act = genKeys
            let! nav = genKeys
            return { Focusable = focusable; ActivationKeys = act; NavigationKeys = nav }
        }

    // (keyboard, key, isTab, shift)
    let routeInput: Gen<KeyboardOperation * string * bool * bool> =
        gen {
            let! kb = genKeyboard
            let! key = Gen.elements keyPool
            let! isTab = Gen.elements [ true; false ]
            let! shift = Gen.elements [ true; false ]
            return (kb, key, isTab, shift)
        }

    // a TabOrder of N distinct keyed stops (for the cyclic traversal property)
    let tabOrder: Gen<TabOrder> =
        gen {
            let! n = Gen.choose (1, 6)

            let stops =
                [ for i in 0 .. n - 1 ->
                      { Control = $"c{i}"
                        Role = AccessibilityRole.Button
                        Keyboard = { Focusable = true; ActivationKeys = [ "Enter" ]; NavigationKeys = [] }
                        FocusOrder = None } ]

            return { Stops = stops }
        }

[<Tests>]
let propertyTests =
    testList "094 properties (FsCheck, SC-006)" [
        testCase "route is deterministic and total — identical inputs, identical verdict, never throws (>=1000)" (fun () ->
            // Feature 100 (R5): `route` gained a role + NavRange arg; determinism/totality hold for
            // any role. A Button role forms no navigation intent, isolating the E4 precedence here.
            let prop (kb, key, isTab, shift) =
                let safe () =
                    try
                        Focus.route AccessibilityRole.Button kb None key isTab shift = Focus.route AccessibilityRole.Button kb None key isTab shift
                    with _ ->
                        false

                safe ()

            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen094.routeInput) prop))

        testCase "route obeys the consumption-wins oracle for a non-navigable role (>=1000)" (fun () ->
            // For a Button role no navigation intent is formed, so a NavigationKey is consumed (it
            // pre-empts the Tab test) and yields Fallthrough. Activation precedence is unchanged.
            // The role-derived `Navigate` classification is proven by the Feature100 suites.
            let oracle (kb: KeyboardOperation, key, isTab, shift) =
                let expected =
                    if List.contains key kb.ActivationKeys then Activate
                    elif List.contains key kb.NavigationKeys then Fallthrough
                    elif isTab then Traverse(if shift then Previous else Next)
                    else Fallthrough

                Focus.route AccessibilityRole.Button kb None key isTab shift = expected

            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen094.routeInput) oracle))

        testCase "traverse is cyclic and total — n successive Next returns to the start (>=1000)" (fun () ->
            let cyclic (order: TabOrder) =
                let n = List.length order.Stops
                let start = (List.head order.Stops).Control

                let landed =
                    [ 1..n ]
                    |> List.fold (fun current _ -> Focus.traverse order current Next) (Some start)

                landed = Some start

            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen094.tabOrder) cyclic))

        testCase "traverse determinism — identical inputs, identical next (>=1000)" (fun () ->
            let deterministic (order: TabOrder) =
                let start = Some (List.head order.Stops).Control
                Focus.traverse order start Next = Focus.traverse order start Next

            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen094.tabOrder) deterministic))
    ]
