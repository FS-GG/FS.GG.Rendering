module Feature090RecoveryTests

// Feature 090 US2 (KEYED-ANCESTOR-1) — `Control.nearestAuthored` resolves a structural hit ControlId
// to the nearest authored (withKey) ancestor (FR-004/FR-004a/FR-005, contract recovery.md R1–R4).
// `Control<'msg>` has no equality, so every assertion compares the returned ControlId (a string).

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

type private Msg = Changed

let private theme = Theme.light
let private size = { Width = 320; Height = 200 }

let private render (control: Control<Msg>) = Control.renderTree theme size control

[<Tests>]
let recoveryTests =
    testList
        "Feature 090 nearest-keyed-ancestor recovery (US2)"
        [
          // R1 — a click on an inner UNKEYED, UNBOUND positional node inside a CONTAINER-KEYED
          // composite resolves to the authored container id, not the opaque inner positional id.
          // (Feature 098: an inner node carrying its OWN binding now recovers itself — the
          // binding-aware fixed point — so this container-climb scenario uses an unbound inner node,
          // matching the FR-005 non-regression fixed point in the data model.)
          test "container-keyed composite: an inner unbound hit resolves to the container key (R1)" {
              // A container keyed "picker" whose unkeyed, UNBOUND child lays out at a positional id
              // ("0.0"); with no binding of its own, recovery must climb to recover "picker".
              let composite =
                  Stack.create [ Stack.children [ Button.create [ Button.text "ok" ] ] ]
                  |> Control.withKey "picker"

              let result = render composite
              // The deepest laid-out positional node inside the keyed container.
              let innerHit = "0.0"
              Expect.equal (Control.nearestAuthored result innerHit) (Some "picker") "inner positional hit resolves to the authored container id"
          }

          // R3 — a directly-keyed leaf resolves to itself (non-regressive fixed point).
          test "directly-keyed leaf resolves to itself (R3)" {
              let tree =
                  Stack.create
                      [ Stack.children
                            [ Button.create [ Button.text "go"; Button.onClick Changed ] |> Control.withKey "go" ] ]

              let result = render tree
              Expect.equal (Control.nearestAuthored result "go") (Some "go") "a directly-keyed leaf's hit id is its key; it resolves to itself"
          }

          // R2 — an unkeyed/unbound subtree returns None (the host then falls back to MapPointer raw);
          // recovery never invents a Kind/root id the consumer did not author.
          test "unkeyed/unbound subtree returns None (R2)" {
              let tree = Stack.create [ Stack.children [ Button.create [ Button.text "plain" ] ] ]
              let result = render tree
              Expect.equal (Control.nearestAuthored result "0.0") None "no keyed ancestor on the path ⇒ None (never invents an id)"
              Expect.equal (Control.nearestAuthored result "does-not-exist") None "a hit id absent from the tree ⇒ None (total)"
          }

          // R4 (idempotent fixed point) — nearestAuthored of an already-authored id is that id.
          // A keyed leaf nested at an arbitrary depth resolves to its own key for the key input, and
          // the recovery is total + deterministic over the generated trees.
          test "property: nearestAuthored is an idempotent fixed point, total and deterministic (R4)" {
              // Generate a chain of `depth` nested single-child Stacks with a keyed leaf at the bottom.
              let genCase: Gen<int * string> =
                  gen {
                      let! depth = Gen.choose (0, 8)
                      let! key = Gen.elements [ "a"; "leaf"; "k1"; "deep-key"; "x.y" ]
                      return depth, key
                  }

              let build (depth: int) (key: string) : Control<Msg> =
                  let leaf = Button.create [ Button.text "t"; Button.onClick Changed ] |> Control.withKey key
                  let rec wrap n inner =
                      if n <= 0 then inner else wrap (n - 1) (Stack.create [ Stack.children [ inner ] ])
                  wrap depth leaf

              let prop (depth: int, key: string) =
                  let result = render (build depth key)
                  let once = Control.nearestAuthored result key
                  let twice = Control.nearestAuthored result key
                  // idempotent fixed point: the authored id resolves to itself;
                  // deterministic: same input → same output; total: an absent id → None (no throw).
                  once = Some key
                  && once = twice
                  && Control.nearestAuthored result "absent-id" = None

              let config = Config.QuickThrowOnFailure.WithMaxTest 500
              Check.One(config, Prop.forAll (Arb.fromGen genCase) prop)
          } ]
