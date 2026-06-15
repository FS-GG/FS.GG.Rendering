module Feature101LayoutDriftGuardTests

// Feature 101 (R7) — the Layout Dirty-Set Anti-Drift Guard. R2 (feature 097) lowers geometry in
// `ControlInternals.toLayout` from three attribute NAMES (`width`/`height`/`orientation`) while the
// incremental dirty classifier `RetainedRender.layoutDirtySet` keys on a SEPARATE hand-maintained
// literal `ControlInternals.layoutAffectingAttrNames`. The two agreed only by maintenance discipline.
// R7 converts that correct-but-unguarded invariant into an enforced one WITHOUT changing any runtime
// behavior: a pure drift report (both directions) + a behavioral probe that discovers the REAL
// layout-driving names by toggling each candidate attribute on representative fixtures and observing
// whether the real `ControlInternals.evaluateLayout` output changes. The load-bearing gate asserts the
// probe-discovered set equals the literal.
//
// All helpers below are TEST-LOCAL (no `.fsi`, no public/internal surface). The report is exercised
// against its natural domain (sets of names) and the probe/category units exercise the REAL
// `evaluateLayout` / `layoutDirtySet` / `layoutAffectingAttrNames` — no mock, fake, stub, or in-memory
// substitute for any real dependency, so this is NOT synthetic evidence.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

// ---------------------------------------------------------------------------------------------------
// C1 — the pure drift report (FR-002 under-coverage, FR-003 over-coverage, FR-007 human-legible)
// ---------------------------------------------------------------------------------------------------

/// A single way the classifier's covered name set disagrees with what the layout lowering actually
/// reads. Test-local (no public surface). `Uncovered` = under-coverage (risks STALE cached bounds,
/// FR-002); `OverBroad` = over-coverage (wastes a re-measure, FR-003).
type DriftFinding =
    | Uncovered of name: string
    | OverBroad of name: string

/// Exact set-difference, BOTH directions, as named findings. Pure; total; never throws. Findings are
/// order-stable (F# `Set.toList` is sorted ascending) so failure messages are reproducible:
/// under-coverage findings first (sorted), then over-coverage findings (sorted).
let layoutDriftReport (discovered: Set<string>) (covered: Set<string>) : DriftFinding list =
    [ for name in Set.toList (Set.difference discovered covered) -> Uncovered name ]
    @ [ for name in Set.toList (Set.difference covered discovered) -> OverBroad name ]

let private describeFinding finding =
    match finding with
    | Uncovered name ->
        sprintf "un-covered layout input: '%s' (toLayout reads it but the classifier does not dirty on it)" name
    | OverBroad name ->
        sprintf "over-broad classifier entry: '%s' (the classifier lists it but toLayout never reads it)" name

/// Human-legible, names EACH attribute AND its direction (FR-007). Empty -> an explicit "no drift"
/// string so a passing gate is unambiguous in logs.
let formatDrift (findings: DriftFinding list) : string =
    match findings with
    | [] -> "no drift: the layout dirty-set classifier exactly covers the attribute names toLayout reads"
    | _ -> "layout dirty-set drift:\n" + (findings |> List.map describeFinding |> String.concat "\n")

// ---------------------------------------------------------------------------------------------------
// C2 — the behavioral probe (the load-bearing equality gate against the REAL evaluateLayout)
// ---------------------------------------------------------------------------------------------------

/// A representative control used to observe whether toggling an attribute name changes the lowering.
type ProbeFixture = { Label: string; Control: Control<unit> }

let private probeSize: Size = { Width = 400; Height = 300 }

/// A leaf content control (no children) — exercises the leaf branch of `toLayout` where `width`/
/// `height` always take effect.
let private leafFixtureControl: Control<unit> =
    { Kind = "text-block"
      Key = None
      Attributes = []
      Children = []
      Content = Some "probe"
      Accessibility = None }

/// A plain container (non-grid/toolbar/dock kind, so `orientation` actually drives `Direction`) with
/// one child — exercises the container branch where `width`/`height` apply only when present and where
/// `orientation` flips the layout `Direction`.
let private containerFixtureControl: Control<unit> =
    { Kind = "stack"
      Key = None
      Attributes = []
      Children = [ leafFixtureControl ]
      Content = None
      Accessibility = None }

let private probeFixtures: ProbeFixture list =
    [ { Label = "plain orientation-sensitive container with one child"; Control = containerFixtureControl }
      { Label = "leaf content control"; Control = leafFixtureControl } ]

/// Distinct attribute names to test. Built from CONCRETE, TRACEABLE sources (research D2), NOT a
/// hand-curated free list, so the under-coverage guarantee tracks the real control vocabulary:
///   (1) the classifier's own covered set `ControlInternals.layoutAffectingAttrNames`;
///   (2) the attribute-name vocabulary the controls layer emits/reads — the `Attr` builder names in
///       `src/Controls/Attributes.fs` and the attribute-name literals `src/Controls/Control.fs` reads;
///   (3) explicit non-layout names to exercise the over-coverage direction and prove non-layout names
///       are NOT discovered.
let probeCorpus: string list =
    // (1) the classifier's own covered set
    Set.toList ControlInternals.layoutAffectingAttrNames
    // (2) the controls-layer attribute vocabulary (Attr builders + Control.fs reads)
    @ [ "width"; "height"; "orientation"; "padding"; "margin"
        "value"; "text"; "selected"; "enabled"; "readOnly"; "visible"; "loading"
        "items"; "nodes"; "styleClasses"; "visualState"; "accessibility"; "style"; "theme" ]
    // (3) explicit non-layout names — the over-coverage probes
    @ [ "background"; "foreground"; "state-hover" ]
    |> List.distinct

/// A value attached for `name` that is DISTINGUISHABLE for a real geometry name (an explicit
/// width/height differing from the fixture default; an `orientation = "horizontal"` on a column-default
/// container) and inert for non-geometry names. The probe only needs the PRESENCE of the name to flip
/// the `LayoutNode` for a real layout input.
let private probeValue (name: string) : AttrValue<unit> =
    match name with
    | "orientation" -> TextValue "horizontal"
    | _ -> FloatValue 173.0

let private probeAttr (name: string) : Attr<unit> =
    { Name = name
      // Category is irrelevant to `toLayout`/`evaluateLayout` (they read NAMES, never `attr.Category`);
      // the probe deliberately measures the NAME channel, so a neutral non-Layout category is used.
      Category = AttrCategory.Style
      Value = probeValue name }

/// True iff attaching an attribute named `name` to `fixture` changes the root `LayoutNode` produced by
/// the REAL `ControlInternals.evaluateLayout` (structural inequality). `evaluateLayout` returns the
/// `toLayout` output as its first element, so this observes exactly the names `toLayout` reads.
let nameDrivesLayout (size: Size) (fixture: ProbeFixture) (name: string) : bool =
    let withAttr =
        { fixture.Control with Attributes = probeAttr name :: fixture.Control.Attributes }

    let baseNode, _, _ = ControlInternals.evaluateLayout size fixture.Control
    let withNode, _, _ = ControlInternals.evaluateLayout size withAttr
    // `LayoutNode` carries a `Measure: ContentMeasure option` (a function) so it has no structural
    // equality; compare its structural rendering instead (`%A` prints `<fun>` identically for the
    // measure closure, so only the geometry-bearing fields — Size / Direction / etc. — distinguish).
    sprintf "%A" baseNode <> sprintf "%A" withNode

/// Union over (corpus x fixtures) of names that drive layout — the discovered truth the gate pins
/// `layoutAffectingAttrNames` to.
///
/// Documented coverage boundary (FR-007 observability): the gate proves equality over names REACHABLE
/// IN THE CORPUS. The corpus is derived from concrete, traceable sources (above), so a future
/// `toLayout` that reads a real control attribute is caught; a name no fixture can make observable is
/// reported non-driving (acceptable — it is genuinely not a current layout input). This is the same
/// "representative" discipline feature 097 used for its >=1000-case property.
let discoverLayoutDrivingNames (size: Size) : Set<string> =
    [ for name in probeCorpus do
          for fixture in probeFixtures do
              if nameDrivesLayout size fixture name then
                  yield name ]
    |> Set.ofList

// ---------------------------------------------------------------------------------------------------
// C3 — category honoring is an independent channel (FR-004), asserted on the REAL layoutDirtySet
// ---------------------------------------------------------------------------------------------------

let private catAttr (name: string) (category: AttrCategory) (v: float) : Attr<unit> =
    { Name = name; Category = category; Value = FloatValue v }

let private catTheme = Theme.light

/// A keyed `panel` carrying `panelAttrs`, wrapped in a stack root with a leaf child so it is a real,
/// measured subtree. The category channel is asserted through the EXPOSED `RetainedRender.step` rather
/// than calling the internal `layoutDirtySet` directly — `step` drives the REAL `layoutDirtySet` to
/// build its dirty set, and `WorkReductionRecord.RemeasuredNodeCount` reports exactly the
/// post-propagation count of nodes that classifier dirtied (feature 097). Asserting through `step`
/// keeps `RetainedRender.fsi` untouched (Tier-2, zero surface delta, SC-005) while still exercising the
/// real classifier — strictly more end-to-end than a direct internal call.
let private panelTree (panelAttrs: Attr<unit> list) : Control<unit> =
    { Kind = "stack"
      Key = Some "root"
      Attributes = []
      Children =
        [ { Kind = "panel"
            Key = Some "p"
            Attributes = panelAttrs
            Children =
              [ { Kind = "text-block"; Key = Some "leaf"; Attributes = []; Children = []; Content = Some "x"; Accessibility = None } ]
            Content = None
            Accessibility = None } ]
      Content = None
      Accessibility = None }

/// Nodes the REAL `layoutDirtySet`-driven incremental evaluator re-measured stepping `prev -> next`.
let private remeasuredFor (prev: Control<unit>) (next: Control<unit>) : int =
    let r0 = (RetainedRender.init catTheme probeSize prev).Retained
    (RetainedRender.step catTheme probeSize r0 next).WorkReduction.RemeasuredNodeCount

[<Tests>]
let tests =
    testList "Feature101 layout dirty-set anti-drift guard (R7)" [

        // -------- C1: pure drift report, both directions, sorted (FR-002/FR-003) --------

        test "shipping state passes — discovered == covered yields no findings (US1 scenario 4)" {
            let s = Set.ofList [ "width"; "height"; "orientation" ]
            Expect.equal (layoutDriftReport s s) [] "equal sets => no drift"
        }

        test "under-coverage: an un-covered layout input is named Uncovered (US1 scenario 1, FR-002)" {
            let discovered = Set.ofList [ "width"; "height"; "padding" ]
            let covered = Set.ofList [ "width"; "height" ]
            Expect.equal (layoutDriftReport discovered covered) [ Uncovered "padding" ] "padding is under-covered"
        }

        test "over-coverage: a name toLayout ignores is named OverBroad (US1 scenario 2, FR-003)" {
            let discovered = Set.ofList [ "width" ]
            let covered = Set.ofList [ "width"; "orientation" ]
            Expect.equal (layoutDriftReport discovered covered) [ OverBroad "orientation" ] "orientation is over-broad"
        }

        test "both directions reported, sorted/order-stable" {
            let discovered = Set.ofList [ "a"; "b" ]
            let covered = Set.ofList [ "b"; "c" ]
            Expect.equal
                (layoutDriftReport discovered covered)
                [ Uncovered "a"; OverBroad "c" ]
                "under-coverage findings first (sorted), then over-coverage (sorted)"
        }

        test "formatDrift names each attribute AND its direction; empty -> explicit no-drift (FR-007)" {
            let under = formatDrift [ Uncovered "padding" ]
            Expect.stringContains under "padding" "names the under-covered attribute"
            Expect.stringContains under "un-covered" "names the under-coverage direction"

            let over = formatDrift [ OverBroad "orientation" ]
            Expect.stringContains over "orientation" "names the over-broad attribute"
            Expect.stringContains over "over-broad" "names the over-coverage direction"

            let both = formatDrift [ Uncovered "a"; OverBroad "c" ]
            Expect.stringContains both "a" "names every finding (a)"
            Expect.stringContains both "c" "names every finding (c)"

            Expect.stringContains (formatDrift []) "no drift" "empty list yields an explicit no-drift string"
        }

        // -------- C2: the load-bearing behavioral-probe equality gate (FR-001, SC-001/SC-002) --------

        test "the behavioral probe discovers EXACTLY the geometry names {width;height;orientation}" {
            let discovered = discoverLayoutDrivingNames probeSize
            Expect.equal discovered (Set.ofList [ "width"; "height"; "orientation" ]) "probe discovers the real layout inputs"
        }

        test "LOAD-BEARING GATE: probe-discovered names == layoutAffectingAttrNames, no drift (FR-001/FR-002/FR-003)" {
            let discovered = discoverLayoutDrivingNames probeSize
            let report = layoutDriftReport discovered ControlInternals.layoutAffectingAttrNames
            // The gate: the classifier's covered set is EXACTLY what `toLayout` reads. Fails (with a named
            // attribute) the instant `toLayout` starts reading a corpus name absent from the literal, or
            // the literal lists a name `toLayout` ignores.
            Expect.equal report [] (formatDrift report)
        }

        test "non-layout names are NOT discovered (over-coverage direction is real)" {
            // Sanity: the corpus DOES include non-layout names; none of them is discovered, so an over-broad
            // literal entry would be caught by the gate above.
            let discovered = discoverLayoutDrivingNames probeSize
            for name in [ "background"; "foreground"; "text"; "value"; "selected"; "padding"; "margin" ] do
                Expect.isFalse (Set.contains name discovered) (sprintf "'%s' is not a layout-driving name" name)
        }

        // -------- C3: category honoring is an INDEPENDENT channel (FR-004) --------

        test "AttrSet with Category=Layout dirties even when its NAME is absent from the name set (FR-004a)" {
            // 'elevation' is NOT in layoutAffectingAttrNames, but a Layout-category change must still dirty.
            Expect.isFalse
                (Set.contains "elevation" ControlInternals.layoutAffectingAttrNames)
                "'elevation' is deliberately not a name-covered attribute"

            let prev = panelTree [ catAttr "elevation" AttrCategory.Layout 1.0 ]
            let next = panelTree [ catAttr "elevation" AttrCategory.Layout 2.0 ]
            Expect.isGreaterThan (remeasuredFor prev next) 0 "a Layout-category attr change re-measures (category channel)"
        }

        test "AttrRemoved of a prev Layout-category attr dirties (category recovered from prev, FR-004b)" {
            let prev = panelTree [ catAttr "elevation" AttrCategory.Layout 1.0 ]
            let next = panelTree []
            Expect.isGreaterThan (remeasuredFor prev next) 0 "removing a prev Layout-category attr re-measures"
        }

        test "a content/style change (non-Layout category, non-geometry name) does NOT dirty (SC-004)" {
            let prev = panelTree [ catAttr "background" AttrCategory.Style 1.0 ]
            let next = panelTree [ catAttr "background" AttrCategory.Style 2.0 ]
            Expect.equal (remeasuredFor prev next) 0 "a style/content change re-measures nothing"
        }

        test "the name-set gate does NOT demand a category-only attribute appear (channels independent, FR-003<->FR-004)" {
            // 'elevation' dirties via the category channel (asserted above) yet is NOT a name the probe
            // discovers, and the gate operates on names only — so it does not demand 'elevation' appear in
            // layoutAffectingAttrNames. The two channels are independent.
            let elevationDrivesByName =
                probeFixtures |> List.exists (fun f -> nameDrivesLayout probeSize f "elevation")
            Expect.isFalse elevationDrivesByName "'elevation' is not name-driving (it is a category-only layout signal)"
            Expect.isFalse
                (Set.contains "elevation" (discoverLayoutDrivingNames probeSize))
                "so the name-set gate does not demand 'elevation' appear in the literal"
        }
    ]
