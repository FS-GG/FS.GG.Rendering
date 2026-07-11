// See skill: fs-gg-symbology
namespace FS.GG.UI.Symbology

/// Pure legibility linter: scores a produced symbol set against the fixed §4 channel-grammar
/// capacities and reports overload / out-of-domain / degenerate findings (FR-001/FR-012).
/// Deterministic; no wall-clock, randomness, or IO (SC-001). Advisory; never mutates, never
/// raises on valid input (FR-008).
[<RequireQualifiedAccess>]
module Legibility =

    /// Machine-readable identity of a scored grammar channel (FR-006).
    /// `Motion` is whole-board: it appears only as a `Finding.Channel` value from the board-level
    /// motion-load check — it has no `ChannelKind`, no `table` row, and no `ChannelUsage` entry.
    /// `Label` is likewise not a table row: it is budgeted in lines, per grammar (#286).
    type Channel =
        | Faction
        | Klass
        | Sigil
        | Size
        | Heading
        | Threat
        | Charge
        | Health
        | Speed
        | State
        | Shield
        | Motion
        /// Feature 254 — the opt-in second rotation channel. `Continuous`, so it is overload-exempt
        /// like `Heading`: a token that leaves it `None` still contributes a distinct level, and only
        /// a non-finite angle is an error. Declared LAST so that adding it does not renumber the
        /// existing cases' tags; it still sorts next to `Heading` in `table` and `channelOrder`.
        | SecondaryHeading
        /// Feature 286 — the identity label. Like `Motion` it is NOT a `table` row and has no
        /// `ChannelKind`: its budget is a count of LINES, not of separable levels, and that budget is
        /// per-grammar, so it is scored only by `scoreIn`/`scoreAnimatedIn` and never by `score`.
        /// Declared last, after `SecondaryHeading`, for the same tag-stability reason.
        | Label

    /// How the eye reads a channel — selects which checks apply (FR-003/FR-009).
    type ChannelKind =
        /// separable discrete symbols/hues -> distinct-level overload
        | Categorical
        /// a small number of distinct levels read as rank -> overload + domain. The underlying field may
        /// be a float (`Size`/`Threat`/`Charge`): the mapping is expected to quantise it, and a ramp of
        /// distinct raw values overloads exactly as a ramp of distinct hues would.
        | Ordered
        /// magnitude/gradient/angle read as a position on a scale, not a rank -> domain only, overload-exempt
        | Continuous

    /// Finding severity (FR-006). Error = grammar cannot encode the value; Warning = encodable but overloaded.
    ///
    /// Qualified access is mandatory: the bare `Error` case would otherwise shadow `FSharp.Core`'s
    /// `Result.Error` for every consumer that opens this module. Write `Legibility.Severity.Error`.
    [<RequireQualifiedAccess>]
    type Severity =
        | Warning
        | Error

    /// One reported issue (FR-006).
    type Finding =
        { Channel: Channel
          Severity: Severity
          Message: string
          /// 0-based indices into the scored set; [] for whole-board findings.
          ///
          /// For a per-unit `Error` this is the offending unit. For an overload `Warning` it is the
          /// units holding the levels PAST the channel's capacity — the smallest set a re-map has to
          /// move to bring the channel back inside capacity — NOT every unit on the board. Levels are
          /// ranked by (frequency descending, first appearance ascending) and the ones after the
          /// leading `Capacity` are the excess, so the named units are those carrying the
          /// least-frequent levels. Deterministic for a given input (SC-001).
          Units: int list }

    /// One row of the fixed capacity table (FR-002).
    type ChannelSpec =
        { Channel: Channel
          Kind: ChannelKind
          /// How many distinct levels the eye separates — NOT how many the grammar can draw. Meaningful
          /// for Categorical/Ordered; 0 for Continuous. `Speed` draws 0..6 beads but ranks ~4 of them,
          /// so a fifth distinct speed is in-domain per unit and an overload per board.
          Capacity: int }

    /// Per-channel usage evidence behind the findings (FR-007).
    type ChannelUsage =
        { Channel: Channel
          Kind: ChannelKind
          /// for Continuous channels, the count of distinct raw values (informational — never drives an overload finding)
          DistinctLevels: int
          /// from the table; 0/ignored for Continuous
          Capacity: int }

    /// Overall one-line signal (FR-007). Clean iff no findings.
    type Verdict =
        | Clean
        | HasWarnings

    /// The linter's whole output (pure, reproducible from the input alone — SC-001).
    type Report =
        { /// deterministic order: table order, then unit index
          Findings: Finding list
          /// one entry per per-unit channel (12; Motion and Label excluded), table order
          Usage: ChannelUsage list
          Verdict: Verdict }

    /// The fixed capacity table the linter scores against (FR-002) — exposed read-only, and the single
    /// source of the reliable-level counts the symbology skill's §4 prose table quotes.
    /// One row per per-unit channel (12); `Motion` has no `ChannelKind` and is not a table row — its
    /// whole-board rhythm budget is reported in the `scoreAnimated` finding's `Message`.
    val table: ChannelSpec list

    /// Score a static produced symbol set (FR-001/FR-003/FR-004/FR-005/FR-007/FR-011).
    /// Motion-load is not evaluated (no motion supplied).
    val score: tokens: Token list -> Report

    /// Score an animated produced symbol set; additionally evaluates whole-board motion load (FR-010):
    /// more than one distinct non-`Idle` rhythm across the board is a `Warning` on `Motion` with
    /// `Units = []`. The budget is whole-board, not per symbol — `animate` already takes a single
    /// `Motion`, so a symbol cannot stack rhythms.
    val scoreAnimated: board: (Motion * Token) list -> Report

    /// Score a produced symbol set AS DRAWN by `grammar` (#286). Adds grammar-conditional findings on
    /// top of the grammar-independent `score`; it never REMOVES a finding `score` would emit, so a
    /// `Clean` verdict here implies a `Clean` verdict there.
    ///
    /// The one static grammar-conditional fact is the identity label's LINE budget — 3 lines under
    /// `Grammar.Token`, 2 under `Badge` and `Ring` — past which `wrapLabel` drops the surplus and marks
    /// the last kept line with an ellipsis. A token over budget yields a `Warning` on `Label`.
    ///
    /// Scored on the label's HARD line breaks only. The drawn count also depends on greedy wrapping,
    /// which needs a text-measurement provider; the linter is contractually measurement-free and
    /// deterministic (SC-001), and wrapping can only ADD lines — so this under-reports and never
    /// false-positives. It is a backstop, not a layout oracle.
    ///
    /// `scoreIn Grammar.Token` ≡ `score` for any roster whose labels fit three lines.
    val scoreIn: grammar: Grammar -> tokens: Token list -> Report

    /// As `scoreAnimated`, for the selected grammar (#286). Beyond the whole-board motion load and the
    /// `scoreIn` label budget, flags the directional rhythms the grammar cannot draw: `animateIn` builds
    /// `Badge`/`Ring` frames from the grammar-agnostic overlays alone (`Pulse`/`Blink`/`Damage`), so a
    /// unit marked `Motion.Spin` or `Motion.Moving` renders byte-identically to an `Idle` one. That is an
    /// `Error` on `Motion` naming the unit — the channel is dropped outright, not degraded legibly.
    ///
    /// `scoreAnimatedIn Grammar.Token` ≡ `scoreAnimated` for any board whose labels fit three lines.
    val scoreAnimatedIn: grammar: Grammar -> board: (Motion * Token) list -> Report
