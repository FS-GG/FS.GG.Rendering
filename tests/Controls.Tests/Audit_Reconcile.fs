module Audit_Reconcile

// AUDIT (feature 006, T004 sanity + T018) — keyed reconciliation `Reconcile.diff`/`apply`.
//   * Round-trip PROPERTY (FR-005, SC-003): over generated keyed/positional/kind-mismatch tree pairs,
//     `apply prev (diff prev next).Patch` reproduces `next`.
//   * DISCRIMINATING POWER: a deliberately-broken apply (force a `Keep` patch where `prev <> next`, or
//     mutate the emitted patch) makes the round-trip equality FALSE — proving the property has teeth
//     (it would go RED if the diff/apply were wrong). Asserted as an inequality so the proof itself is
//     a green assertion over a known-bad patch.
//
// Reaches `module internal Reconcile` via `[<assembly: InternalsVisibleTo("Controls.Tests")>]`.
// `Control<'msg>` carries a function case (EventValue) so it does not satisfy `equality`; the oracle
// compares `sprintf "%A"` reprs of the (function-free) generated trees, canonicalizing attribute order
// (the diff sorts by Name, FR-007/FR-009). All `Audit:`-prefixed for `--filter Audit`.

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Scene
open FS.GG.UI.Controls

// --- builders -------------------------------------------------------------

let private node kind key attrs children content : Control<int> =
    { Kind = kind; Key = key; Attributes = attrs; Children = children; Content = content; Accessibility = None }

let private leaf key content : Control<int> = node "TextBlock" (Some key) [] [] (Some content)
let private parent children : Control<int> = node "Stack" None [] children None
let private repr (x: 'a) : string = sprintf "%A" x

/// Attribute order is not semantically meaningful (FR-007 diffs by name); canonicalize before compare.
let rec private canon (c: Control<int>) : Control<int> =
    { c with
        Attributes = c.Attributes |> List.sortBy (fun a -> a.Name)
        Children = c.Children |> List.map canon }

let private roundTrip (prev: Control<int>) (next: Control<int>) : bool =
    let patch = (Reconcile.diff prev next).Patch
    repr (canon (Reconcile.apply prev patch)) = repr (canon next)

// --- FsCheck generator over keyed / positional / kind-mismatch trees ------

module private G =
    let private genAttrValue : Gen<AttrValue<int>> =
        Gen.oneof
            [ Gen.map TextValue (Gen.elements [ "hi"; "bye"; "x"; "y" ])
              Gen.map BoolValue (Gen.elements [ true; false ])
              Gen.map FloatValue (Gen.elements [ 0.0; 1.0; 2.5; -1.0 ]) ]

    let private genAttrs : Gen<Attr<int> list> =
        gen {
            let! names = Gen.subListOf [ "text"; "color"; "size"; "label" ]
            let! values = Gen.listOfLength (List.length names) genAttrValue
            return List.map2 (fun n v -> { Name = n; Category = AttrCategory.Style; Value = v }) names values
        }

    // keyed AND unkeyed mixed so the diff exercises keyed reorder + positional residual + kind mismatch.
    let private genKey : Gen<ControlId option> =
        Gen.frequency [ 2, Gen.constant None; 3, Gen.map Some (Gen.elements [ "a"; "b"; "c"; "d" ]) ]

    let private genKind : Gen<ControlKind> = Gen.elements [ "TextBlock"; "Button"; "Stack" ]
    let private genContent : Gen<string option> =
        Gen.frequency [ 1, Gen.constant None; 2, Gen.map Some (Gen.elements [ "A"; "B"; "C" ]) ]

    let rec private genControlOf (size: int) : Gen<Control<int>> =
        gen {
            let! kind = genKind
            let! key = genKey
            let! attrs = genAttrs
            let! content = genContent
            let! children =
                if size <= 0 then Gen.constant []
                else gen { let! n = Gen.choose (0, 3) in return! Gen.listOfLength n (genControlOf (size / 2)) }
            return { Kind = kind; Key = key; Attributes = attrs; Children = children; Content = content; Accessibility = None }
        }

    let control : Gen<Control<int>> = Gen.sized (fun s -> genControlOf (min s 4))
    let pair : Gen<Control<int> * Control<int>> = gen { let! p = control in let! n = control in return (p, n) }

[<Tests>]
let tests =
    testList "Audit: Reconcile diff/apply round-trip + discriminating power (FR-005, SC-003)" [

        // ---- T004 scaffold sanity: the seams + counters are reachable ----
        test "Audit: Reconcile scaffold reachability (T004)" {
            // diff/apply seam reachable
            let r = Reconcile.diff (leaf "a" "A") (leaf "a" "B")
            let _ = Reconcile.apply (leaf "a" "A") r.Patch
            // a WorkReductionRecord counter is reachable via the wired step (compile-time reachability)
            let theme = Theme.light
            let size: Size = { Width = 320; Height = 240 }
            let init = RetainedRender.init theme size (parent [ leaf "a" "A" ])
            let s = RetainedRender.step theme size init.Retained (parent [ leaf "a" "A" ])
            Expect.isTrue (s.WorkReduction.RecomputedNodeCount >= 0) "WorkReductionRecord counter reachable"
        }

        // ---- T018 round-trip property over generated trees ----
        test "Audit: apply (diff prev next) reproduces next over >=1000 generated pairs (FR-005)" {
            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen G.pair) (fun (p, n) -> roundTrip p n))
        }

        test "Audit: round-trip holds on the hand-built keyed / positional / kind-mismatch corpus" {
            // keyed reorder
            Expect.isTrue (roundTrip (parent [ leaf "a" "A"; leaf "b" "B"; leaf "c" "C" ]) (parent [ leaf "c" "C"; leaf "a" "A"; leaf "b" "B" ])) "keyed reorder round-trips"
            // positional unkeyed change
            Expect.isTrue (roundTrip (parent [ node "T" None [] [] (Some "u"); node "T" None [] [] (Some "v") ]) (parent [ node "T" None [] [] (Some "u2"); node "T" None [] [] (Some "v") ])) "positional unkeyed round-trips"
            // kind mismatch ⇒ whole-subtree replace
            Expect.isTrue (roundTrip (node "Stack" None [] [ leaf "a" "A" ] None) (node "Grid" None [] [ leaf "a" "A" ] None)) "kind mismatch round-trips"
            // insert + remove
            Expect.isTrue (roundTrip (parent [ leaf "a" "A"; leaf "b" "B" ]) (parent [ leaf "a" "A"; leaf "c" "C" ])) "insert/remove round-trips"
        }

        // ---- DISCRIMINATING POWER: prove the round-trip oracle goes RED on a broken patch ----
        test "Audit: DISCRIMINATING — a forced wrong Keep patch breaks the round-trip (proves teeth)" {
            // For a genuine change (prev <> next) the CORRECT patch round-trips, but substituting the
            // bypass patch `NodePatch.Keep` (the "do nothing" patch) reconstructs `prev`, not `next`.
            // So the round-trip EQUALITY the property asserts is FALSE under the broken patch — the
            // property would go RED if apply mis-behaved. We assert that inequality to prove teeth.
            let prev = parent [ leaf "a" "A"; leaf "b" "B" ]
            let next = parent [ leaf "a" "CHANGED"; leaf "b" "B" ]

            // correct patch round-trips (sanity)
            Expect.isTrue (roundTrip prev next) "the genuine diff/apply round-trips"

            // broken patch: Keep ⇒ apply reproduces prev, which differs from next
            let brokenApplied = Reconcile.apply prev Reconcile.NodePatch.Keep
            Expect.notEqual (repr (canon brokenApplied)) (repr (canon next))
                "a forced Keep patch fails to reproduce next — the round-trip oracle is discriminating (RED when bypassed)"
            // and it does reproduce prev (the bypass is a genuine identity, not noise)
            Expect.equal (repr (canon brokenApplied)) (repr (canon prev)) "the Keep bypass reproduces prev exactly"
        }

        // ---- DISCRIMINATING POWER #2: a mutated Replace patch diverges from next ----
        test "Audit: DISCRIMINATING — a mutated Replace patch reconstructs the wrong tree" {
            let prev = parent [ leaf "a" "A" ]
            let next = parent [ leaf "a" "B" ]
            // mutate the patch to Replace with a DIFFERENT tree than next
            let wrongTarget = parent [ leaf "a" "WRONG" ]
            let mutated = Reconcile.NodePatch.Replace wrongTarget
            let applied = Reconcile.apply prev mutated
            Expect.notEqual (repr (canon applied)) (repr (canon next)) "a mutated Replace target does not match next (oracle catches it)"
            Expect.equal (repr (canon applied)) (repr (canon wrongTarget)) "Replace faithfully reconstructs its (wrong) target"
        }
    ]
