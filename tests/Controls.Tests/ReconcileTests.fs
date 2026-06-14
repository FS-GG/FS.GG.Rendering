module ReconcileTests

// Feature 067 — internal keyed reconciliation. These tests reach the
// `module internal Reconcile` in FS.Skia.UI.Controls via
// `[<assembly: InternalsVisibleTo("Controls.Tests")>]`. Per the feature's
// vertical-slice rule, the in-assembly Expecto/FsCheck test IS the
// user-reachable surface for these internal-only user stories. Authored
// failing-first against the T006 stub, then greened by the real diff/apply.
//
// `Control<'msg>` does not satisfy F#'s `equality` constraint (its `AttrValue`
// carries a function case, `EventValue`), so the round-trip / determinism oracle
// compares structural `sprintf "%A"` reprs of the (function-free) generated trees
// rather than `=` on `Control`/`NodePatch`/`ReconcileResult`.

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.Skia.UI.Controls

// --- builders -------------------------------------------------------------

let private node kind key attrs children content : Control<int> =
    { Kind = kind
      Key = key
      Attributes = attrs
      Children = children
      Content = content
      Accessibility = None }

/// A keyed leaf with a `Kind` of "TextBlock".
let private leaf key content : Control<int> =
    node "TextBlock" (Some key) [] [] (Some content)

let private parent children : Control<int> =
    node "Stack" None [] children None

let private attr name value : Attr<int> =
    { Name = name; Category = AttrCategory.Style; Value = value }

let private repr (x: 'a) : string = sprintf "%A" x

let private expectUpdate (patch: Reconcile.NodePatch<int>) : Reconcile.UpdatePatch<int> =
    match patch with
    | Reconcile.NodePatch.Update u -> u
    | other -> failtestf "expected NodePatch.Update, got %A" other

let private expectKeep label (p: Reconcile.NodePatch<int>) =
    match p with
    | Reconcile.NodePatch.Keep -> ()
    | other -> failtestf "%s: expected NodePatch.Keep, got %A" label other

/// Key of the prev-side child a child-op refers to (for keyed assertions).
let private opPrevKey (prev: Control<int>) (op: Reconcile.ChildOp<int>) : ControlId option =
    match op with
    | Reconcile.ChildKeep (i, _) -> prev.Children.[i].Key
    | Reconcile.ChildMove (f, _, _) -> prev.Children.[f].Key
    | Reconcile.ChildInsert (_, c) -> c.Key
    | Reconcile.ChildRemove (k, _) -> k

let rec private hasReplace (patch: Reconcile.NodePatch<int>) : bool =
    match patch with
    | Reconcile.NodePatch.Replace _ -> true
    | Reconcile.NodePatch.Keep -> false
    | Reconcile.NodePatch.Update u ->
        u.Children
        |> List.exists (fun op ->
            match op with
            | Reconcile.ChildKeep (_, p)
            | Reconcile.ChildMove (_, _, p) -> hasReplace p
            | Reconcile.ChildInsert _
            | Reconcile.ChildRemove _ -> false)

// --- US1: keyed reorder ---------------------------------------------------

[<Tests>]
let us1 =
    testList
        "US1 keyed reorder"
        [ test "reorder [a;b;c] -> [c;a;b] yields zero replaces, keep/move keyed to a/b/c" {
              let a, b, c = leaf "a" "A", leaf "b" "B", leaf "c" "C"
              let prev = parent [ a; b; c ]
              let next = parent [ c; a; b ]
              let result = Reconcile.diff prev next
              let u = expectUpdate result.Patch

              Expect.isFalse (hasReplace result.Patch) "SC-001: a pure reorder contains no Replace op"

              for op in u.Children do
                  match op with
                  | Reconcile.ChildKeep _
                  | Reconcile.ChildMove _ -> ()
                  | other -> failtestf "reorder should only keep/move, got %A" other

              let keys = u.Children |> List.map (opPrevKey prev) |> List.choose id |> List.sort
              Expect.equal keys [ "a"; "b"; "c" ] "every keyed child is matched by key"

              let moves =
                  u.Children
                  |> List.filter (function
                      | Reconcile.ChildMove _ -> true
                      | _ -> false)
              Expect.isNonEmpty moves "a reorder records at least one move (US1 AC#1)"
          }

          test "a keyed child moved but unchanged carries NodePatch.Keep (US1 AC#2)" {
              let a, b = leaf "a" "A", leaf "b" "B"
              let prev = parent [ a; b ]
              let next = parent [ b; a ]
              let u = expectUpdate (Reconcile.diff prev next).Patch

              for op in u.Children do
                  match op with
                  | Reconcile.ChildKeep (_, p)
                  | Reconcile.ChildMove (_, _, p) -> expectKeep "unchanged moved/kept node" p
                  | other -> failtestf "expected keep/move, got %A" other
          } ]

// --- US2: minimal in-place patch -----------------------------------------

[<Tests>]
let us2 =
    testList
        "US2 minimal in-place patch"
        [ test "single changed attribute touches only that node and names that attribute (SC-003)" {
              let x0 = node "TextBlock" (Some "x") [ attr "text" (TextValue "hi"); attr "color" (TextValue "red") ] [] None
              let x1 = node "TextBlock" (Some "x") [ attr "text" (TextValue "bye"); attr "color" (TextValue "red") ] [] None
              let y = leaf "y" "Y"
              let prev = parent [ x0; y ]
              let next = parent [ x1; y ]
              let u = expectUpdate (Reconcile.diff prev next).Patch

              let xOp = u.Children |> List.find (fun op -> opPrevKey prev op = Some "x")
              let yOp = u.Children |> List.find (fun op -> opPrevKey prev op = Some "y")

              let xPatch =
                  match xOp with
                  | Reconcile.ChildKeep (_, p)
                  | Reconcile.ChildMove (_, _, p) -> expectUpdate p
                  | other -> failtestf "x should be a targeted update, got %A" other

              match xPatch.AttrChanges with
              | [ Reconcile.AttrSet a ] -> Expect.equal a.Name "text" "exactly the changed attribute is named"
              | other -> failtestf "expected a single AttrSet for 'text', got %A" other

              Expect.equal xPatch.ContentChange Reconcile.Unchanged "content is untouched"

              match yOp with
              | Reconcile.ChildKeep (_, Reconcile.NodePatch.Keep) -> ()
              | other -> failtestf "the sibling node must be untouched (Keep), got %A" other
          }

          test "a content-only difference records exactly one content change and nothing else" {
              let prev = node "TextBlock" (Some "x") [] [] (Some "a")
              let next = node "TextBlock" (Some "x") [] [] (Some "b")
              let u = expectUpdate (Reconcile.diff prev next).Patch
              Expect.isEmpty u.AttrChanges "no attribute changes"
              Expect.equal u.ContentChange (Reconcile.ChangedTo(Some "b")) "the content change is recorded"
              Expect.isEmpty u.Children "no child ops"
          } ]

// --- US3: insertion / removal --------------------------------------------

[<Tests>]
let us3 =
    testList
        "US3 insert / remove"
        [ test "[a;b] -> [a;b;c] yields exactly one insert for c" {
              let a, b, c = leaf "a" "A", leaf "b" "B", leaf "c" "C"
              let u = expectUpdate (Reconcile.diff (parent [ a; b ]) (parent [ a; b; c ])).Patch

              let inserts =
                  u.Children
                  |> List.choose (function
                      | Reconcile.ChildInsert (_, cc) -> Some cc.Key
                      | _ -> None)
              Expect.equal inserts [ Some "c" ] "exactly one insert, for c"

              let removes =
                  u.Children
                  |> List.filter (function
                      | Reconcile.ChildRemove _ -> true
                      | _ -> false)
              Expect.isEmpty removes "nothing removed"
          }

          test "[a;b;c] -> [a;c] yields exactly one remove for b" {
              let a, b, c = leaf "a" "A", leaf "b" "B", leaf "c" "C"
              let u = expectUpdate (Reconcile.diff (parent [ a; b; c ]) (parent [ a; c ])).Patch

              let removes =
                  u.Children
                  |> List.choose (function
                      | Reconcile.ChildRemove (k, _) -> Some k
                      | _ -> None)
              Expect.equal removes [ Some "b" ] "exactly one remove, for b"

              let inserts =
                  u.Children
                  |> List.filter (function
                      | Reconcile.ChildInsert _ -> true
                      | _ -> false)
              Expect.isEmpty inserts "nothing inserted"
          } ]

// --- US4: deterministic unkeyed / mixed fallback -------------------------

[<Tests>]
let us4 =
    testList
        "US4 deterministic fallback"
        [ test "unkeyed sibling lists diff byte-for-byte identically on repeated runs (SC-004)" {
              let mk c = node "TextBlock" None [] [] (Some c)
              let prev = parent [ mk "a"; mk "b" ]
              let next = parent [ mk "a"; mk "z" ]
              let r1 = Reconcile.diff prev next
              let r2 = Reconcile.diff prev next
              Expect.equal (repr r1) (repr r2) "repeated diff of unkeyed lists is identical"
          }

          test "mixed list matches keyed by key first, residual unkeyed positionally (FR-010)" {
              let keyed = leaf "k" "K"
              let keyed' = leaf "k" "K2"
              let unkeyedPrev = node "TextBlock" None [] [] (Some "u")
              let unkeyedNext = node "TextBlock" None [] [] (Some "u2")
              // prev: [unkeyed, keyed]  next: [keyed', unkeyed']  — key wins across positions.
              let prev = parent [ unkeyedPrev; keyed ]
              let next = parent [ keyed'; unkeyedNext ]
              let u = expectUpdate (Reconcile.diff prev next).Patch

              // keyed node matched by key (k -> k) regardless of position.
              let kOp = u.Children |> List.find (fun op -> opPrevKey prev op = Some "k")
              match kOp with
              | Reconcile.ChildKeep (_, p)
              | Reconcile.ChildMove (_, _, p) ->
                  let ku = expectUpdate p
                  Expect.equal ku.ContentChange (Reconcile.ChangedTo(Some "K2")) "keyed node updated in place"
              | other -> failtestf "keyed node should match by key, got %A" other

              // the unkeyed node matched positionally among unkeyed residuals.
              let uOp = u.Children |> List.find (fun op -> opPrevKey prev op = None)
              match uOp with
              | Reconcile.ChildKeep (_, p)
              | Reconcile.ChildMove (_, _, p) ->
                  let uu = expectUpdate p
                  Expect.equal uu.ContentChange (Reconcile.ChangedTo(Some "u2")) "unkeyed node matched positionally"
              | other -> failtestf "unkeyed residual should match positionally, got %A" other
          } ]

// --- Edge cases -----------------------------------------------------------

[<Tests>]
let edges =
    testList
        "Reconcile edge cases"
        [ test "root Kind change -> whole-subtree Replace (FR-006)" {
              let prev = node "Stack" None [] [ leaf "a" "A" ] None
              let next = node "Grid" None [] [ leaf "a" "A" ] None
              match (Reconcile.diff prev next).Patch with
              | Reconcile.NodePatch.Replace r -> Expect.equal (repr r) (repr next) "replace carries the next subtree"
              | other -> failtestf "expected Replace, got %A" other
          }

          test "duplicate keys -> first occurrence wins and a KeyCollision Warning is emitted (FR-011)" {
              let a0 = leaf "a" "A0"
              let a1 = leaf "a" "A1"
              let b = leaf "b" "B"
              let prev = parent [ a0; a1; b ]
              let next = parent [ a0; b ]
              let result = Reconcile.diff prev next

              let collisions = result.Diagnostics |> List.filter (fun d -> d.Code = KeyCollision)
              Expect.isNonEmpty collisions "a duplicate key surfaces a KeyCollision diagnostic"
              collisions
              |> List.iter (fun d -> Expect.equal d.Severity Warning "KeyCollision is a Warning")

              // total + deterministic on this input
              Expect.equal (repr result) (repr (Reconcile.diff prev next)) "duplicate-key diff is deterministic"
          }

          test "empty -> non-empty is all inserts; non-empty -> empty is all removes; both-empty is Keep" {
              let a, b = leaf "a" "A", leaf "b" "B"

              let inserts = expectUpdate (Reconcile.diff (parent []) (parent [ a; b ])).Patch
              Expect.isTrue
                  (inserts.Children
                   |> List.forall (function
                       | Reconcile.ChildInsert _ -> true
                       | _ -> false))
                  "empty -> non-empty is all inserts"

              let removes = expectUpdate (Reconcile.diff (parent [ a; b ]) (parent [])).Patch
              Expect.isTrue
                  (removes.Children
                   |> List.forall (function
                       | Reconcile.ChildRemove _ -> true
                       | _ -> false))
                  "non-empty -> empty is all removes"

              expectKeep "both-empty" (Reconcile.diff (parent []) (parent [])).Patch
          }

          test "identical trees -> Keep (round-trip identity)" {
              let t = parent [ leaf "a" "A"; node "Button" (Some "b") [ attr "text" (TextValue "go") ] [] None ]
              expectKeep "identical trees" (Reconcile.diff t t).Patch
          } ]

// --- FsCheck generator + properties (T020) -------------------------------

module private Gen067 =

    let private genAttrValue : Gen<AttrValue<int>> =
        Gen.oneof
            [ Gen.map TextValue (Gen.elements [ "hi"; "bye"; "x"; "y" ])
              Gen.map BoolValue (Gen.elements [ true; false ])
              Gen.map FloatValue (Gen.elements [ 0.0; 1.0; 2.5; -1.0 ])
              Gen.map MessageValue (Gen.choose (0, 5)) ]

    // Distinct attribute names per node (by-name diffing requires uniqueness; the
    // IR's DuplicateAttribute concern is out of scope for the round-trip oracle).
    let private genAttrs : Gen<Attr<int> list> =
        gen {
            let! names = Gen.subListOf [ "text"; "color"; "size"; "label" ]
            let! values = Gen.listOfLength (List.length names) genAttrValue
            return List.map2 (fun n v -> { Name = n; Category = AttrCategory.Style; Value = v }) names values
        }

    let private genKey : Gen<ControlId option> =
        Gen.frequency
            [ 2, Gen.constant None
              3, Gen.map Some (Gen.elements [ "a"; "b"; "c"; "d" ]) ]

    let private genKind : Gen<ControlKind> = Gen.elements [ "TextBlock"; "Button"; "Stack" ]

    let private genContent : Gen<string option> =
        Gen.frequency [ 1, Gen.constant None; 2, Gen.map Some (Gen.elements [ "A"; "B"; "C" ]) ]

    let private a11y : AccessibilityMetadata =
        { Role = StaticText
          NameSource = "test"
          State = []
          FocusOrder = None
          Keyboard = { Focusable = false; ActivationKeys = []; NavigationKeys = [] }
          Contrast = None
          Navigation = None
          Collection = None }

    let private genA11y : Gen<AccessibilityMetadata option> =
        Gen.frequency [ 3, Gen.constant None; 1, Gen.constant (Some a11y) ]

    let rec private genControlOf (size: int) : Gen<Control<int>> =
        gen {
            let! kind = genKind
            let! key = genKey
            let! attrs = genAttrs
            let! content = genContent
            let! acc = genA11y

            let! children =
                if size <= 0 then
                    Gen.constant []
                else
                    gen {
                        let! n = Gen.choose (0, 3)
                        return! Gen.listOfLength n (genControlOf (size / 2))
                    }

            return
                { Kind = kind
                  Key = key
                  Attributes = attrs
                  Children = children
                  Content = content
                  Accessibility = acc }
        }

    let control : Gen<Control<int>> = Gen.sized (fun s -> genControlOf (min s 4))

    let pair : Gen<Control<int> * Control<int>> =
        gen {
            let! p = control
            let! n = control
            return (p, n)
        }

/// Attribute order is not semantically meaningful (FR-007 diffs by name), so the
/// round-trip oracle canonicalizes attribute order before structural comparison.
let rec private canon (c: Control<int>) : Control<int> =
    { c with
        Attributes = c.Attributes |> List.sortBy (fun a -> a.Name)
        Children = c.Children |> List.map canon }

[<Tests>]
let properties =
    testList
        "Reconcile properties (FsCheck)"
        [ test "round-trip: apply prev (diff prev next).Patch == next over >=1000 cases (SC-002/FR-008)" {
              let roundTrips (prev: Control<int>, next: Control<int>) =
                  let patch = (Reconcile.diff prev next).Patch
                  repr (canon (Reconcile.apply prev patch)) = repr (canon next)

              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen067.pair) roundTrips)
          }

          test "determinism: diff prev next == diff prev next (SC-004)" {
              let deterministic (prev: Control<int>, next: Control<int>) =
                  repr (Reconcile.diff prev next) = repr (Reconcile.diff prev next)

              let config = Config.QuickThrowOnFailure.WithMaxTest 1000
              Check.One(config, Prop.forAll (Arb.fromGen Gen067.pair) deterministic)
          } ]
