// See skill: fs-gg-symbology
namespace FS.GG.UI.Symbology

/// Pure visual-representation COVERAGE check — the visual analog of F# match exhaustiveness (#989).
/// Given a game's DECLARED renderable-element set and its element->visual mapping, reports every
/// gameplay element that resolves to NO visible representation and carries NO explicit
/// hidden-by-mechanic opt-out (the silent-omission defect). Deterministic; no wall-clock, randomness,
/// or IO. Advisory; never mutates, never raises on valid input.
///
/// The element type is PRODUCT-DEFINED. A game's renderable elements (doors, bombs, explosions,
/// projectiles, enemy kinds, room types, …) are its OWN discriminated unions; this module supplies the
/// coverage function and the first-class opt-out, never the per-game element list — exactly as
/// `Legibility` scores a game's produced tokens without owning the game's stat set. It is the enforcement
/// half of the pair; the catalog it checks against is authored and maintained by `fs-gg-symbol-design`
/// (#990).
[<RequireQualifiedAccess>]
module Coverage =

    /// A gameplay element's declared visual disposition — one visual "match arm". `Shown` renders as a
    /// symbology `Token` (a visible representation). `Hidden` is a deliberate, reasoned opt-out whose
    /// string names the gameplay MECHANIC that suppresses the visual (fog of war, stealth, an off-screen
    /// or purely-internal marker). There is deliberately NO third "nothing" case: a total
    /// `'element -> Representation` is exhaustive exactly as an F# match with no wildcard is, and a
    /// PARTIAL mapping is surfaced by `check` as a `Missing` finding rather than encoded as a value.
    type Representation =
        | Shown of token: Token
        | Hidden of reason: string

    /// Why a declared element fails coverage. Both are defects; a passing element yields neither.
    type Gap =
        /// The mapping produced neither a `Token` nor an opt-out: the element was forgotten. It would
        /// render nothing, with no error — the exact silent-omission failure #989 targets.
        | Missing
        /// A `Hidden` opt-out whose reason is blank/whitespace: an UNREASONED opt-out. An opt-out must
        /// name the mechanic that hides the element; a blank one is indistinguishable from forgetting it,
        /// so it is rejected rather than trusted.
        | Unreasoned

    /// Overall one-line signal — a pure function of the input. `Covered` iff `Findings` is empty.
    type Verdict =
        | Covered
        | HasGaps

    /// The check's whole output — reproducible from the input alone.
    type Report<'element> =
        { /// findings in DECLARED-element order; re-checking an equal input yields an equal report.
          Findings: Finding<'element> list
          /// the explicit opt-out ledger: every element deliberately `Hidden`, paired with its stated
          /// mechanic, in declared order — the audit trail proving each non-render was a DECISION, not an
          /// omission. Only reasoned opt-outs appear here; a blank-reason one is a `Finding`, not a row.
          OptedOut: ('element * string) list
          Verdict: Verdict }

    /// One reported coverage issue: the offending element plus a human-readable message. `'element` is the
    /// game's own element type.
    type Finding<'element> =
        { Element: 'element
          Gap: Gap
          Message: string }

    /// `check` against a lookup `Map` — the canonical pattern spelled out: a forgotten element is an
    /// absent key. `checkMap elements table` is exactly `check elements (fun e -> Map.tryFind e table)`.
    val checkMap:
        elements: 'element list -> table: Map<'element, Representation> -> Report<'element>
            when 'element: comparison

    /// The coverage check. `elements` is the game's DECLARED renderable-element set; `resolve` is its
    /// element->visual mapping — `Some representation` for a handled element, `None` for one the mapping
    /// forgot (the canonical `resolve` is `fun e -> Map.tryFind e table`, where a forgotten element is
    /// simply an absent key). Reports, in declared order, every element resolving to no visible
    /// representation and no reasoned opt-out. `Verdict = Covered` iff every declared element maps to a
    /// `Shown` token OR a `Hidden` opt-out with a non-blank reason.
    val check: elements: 'element list -> resolve: ('element -> Representation option) -> Report<'element>
