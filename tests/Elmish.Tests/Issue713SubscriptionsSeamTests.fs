module Issue713SubscriptionsSeamTests

// Issue #713 — the seam that was hiding behind S-DOC's qualified-citation hatch.
//
// `ControlsElmish.subscriptions` is public, ships to every generated product, and was taught by NO product
// skill and declared in NO ledger line. It passed S-DOC anyway: `fs-gg-elmish` binds the product's own
// `let subscriptions _ = Sub.none` and writes `AppRoot.Model.subscriptions` in its `program` example, and the
// hatch that #692 opened for qualified citations only checked that a qualifier START UPPERCASE — which a
// PRODUCT module does. So the shipped merge helper was documented by a homonym of the product's own function.
// That is #692's `visible` failure one hop further out, and #713 closed it by holding the qualifier against
// the module that actually DECLARES the surface.
//
// Documenting it in `fs-gg-elmish` is what makes the skill-parity gate GRADE it — and #663 already recorded
// what happens next: *"Skill documents `X`, but no test calls it — the seam may be dead."* It was right about
// `ControlRuntime.diagnostics` and `Catalog.validate`, and it would have been right here: nothing in `tests/`
// or `src/` called `ControlsElmish.subscriptions` at all. The repo's rule is that a skill may only document a
// symbol some test exercises, which is the right rule — a seam nothing calls is a seam nothing holds.
//
// So these assert BEHAVIOUR, not the symbol's existence. The implementation is `keyboard @ controls`, and a
// test that merely restated that would be the tautology #663 warns against ("a test written only to turn the
// gate green"). What is actually load-bearing is the ORDER — it is what the skill now teaches, it is the only
// thing that disambiguates two subscriptions sharing an `Id`, and it is the one property a future `@` swap
// could silently reverse.

open Expecto
open FS.GG.UI.Controls.Elmish

/// A subscription that can be told apart by WHAT IT DOES, not merely by its `Id` — so a merge that dropped a
/// thunk, or kept an `Id` while losing the closure behind it, cannot pass by having the right names in a list.
let private sub subId msg : AdapterSubscription<int> =
    { Id = subId
      Subscribe = fun () -> [ DispatchProductMessage msg ] }

/// What the program would actually get if it ran the merged list: every subscription's commands, in order.
let private fireAll (subs: AdapterSubscription<int> list) =
    subs |> List.collect (fun s -> s.Subscribe())

[<Tests>]
let issue713SubscriptionsSeamTests =
    testList "Issue #713 — the subscription merge fs-gg-elmish now documents is exercised" [

        // The seam's whole reason to exist: `program` takes ONE subscription list, and a product with keyboard
        // shortcuts has TWO. Everything else here is a consequence of this being an ORDERED merge.
        test "the merge carries both lists into one, keyboard first" {
            let keyboard = [ sub "keyboard" 1 ]
            let controls = [ sub "controls" 2 ]

            let merged = ControlsElmish.subscriptions keyboard controls

            // No `isNonEmpty` floor here, deliberately: both halves are LITERALS, so a floor over them could
            // never fire, and a guard that cannot fail is the false assurance FS-GG/.github#266 is about — not
            // a defence against it. What keeps this honest is that the assertions below name both entries and
            // both messages, so an input that lost a half could not satisfy them.
            Expect.equal
                (merged |> List.map (fun s -> s.Id))
                [ "keyboard"; "controls" ]
                "both halves survive the merge, and the ORDER is the contract fs-gg-elmish teaches: keyboard \
                 first, controls second"

            Expect.equal
                (fireAll merged)
                [ DispatchProductMessage 1; DispatchProductMessage 2 ]
                "...and the subscriptions still FIRE — the merge carries each thunk, so the commands a product \
                 receives are the ones its two halves actually raised, in the order it was promised"
        }

        // Why the order is a contract rather than an implementation detail. Nothing makes an `Id` unique across
        // the two halves — the keyboard runtime and the control runtime name their subscriptions independently
        // — so a collision is legal, and ORDER is the only thing that says which one a consumer folds first.
        test "a colliding Id is resolved by order, not by dropping one of them" {
            let keyboard = [ sub "activate" 1 ]
            let controls = [ sub "activate" 2 ]

            let merged = ControlsElmish.subscriptions keyboard controls

            Expect.hasLength
                merged
                2
                "a shared `Id` does not deduplicate — this is a concatenation, and a product that expected one \
                 entry back would silently lose a subscription it registered"

            Expect.equal
                (fireAll merged)
                [ DispatchProductMessage 1; DispatchProductMessage 2 ]
                "the KEYBOARD's `activate` is the one that comes first — which is the whole of what the order \
                 buys you, and the one property a careless `controls @ keyboard` would reverse in silence"
        }

        // The ordinary case, and the one the skill's example shows: a product has keyboard shortcuts and no
        // control-runtime subscriptions of its own, so it passes `[]` for the half it does not have.
        test "an empty half leaves the other exactly as it was" {
            let keyboard = [ sub "keyboard" 1 ]

            Expect.equal
                (ControlsElmish.subscriptions keyboard [] |> List.map (fun s -> s.Id))
                [ "keyboard" ]
                "`subscriptions keyboardSubs []` is the shape fs-gg-elmish teaches — passing `[]` for the half \
                 you do not have must not cost you the half you do"

            Expect.equal
                (ControlsElmish.subscriptions [] keyboard |> List.map (fun s -> s.Id))
                [ "keyboard" ]
                "...and it holds from the other side too"

            Expect.isEmpty
                (ControlsElmish.subscriptions [] ([]: AdapterSubscription<int> list))
                "merging two nothings is `Sub.none`, not a phantom entry"
        }
    ]
