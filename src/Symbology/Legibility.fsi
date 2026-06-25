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

    /// How the eye reads a channel — selects which checks apply (FR-003/FR-009).
    type ChannelKind =
        /// separable discrete symbols/hues -> distinct-level overload
        | Categorical
        /// a small discrete count read as rank -> overload + domain
        | Ordered
        /// magnitude/gradient/angle -> domain only, overload-exempt
        | Continuous

    /// Finding severity (FR-006). Error = grammar cannot encode the value; Warning = encodable but overloaded.
    type Severity =
        | Warning
        | Error

    /// One reported issue (FR-006).
    type Finding =
        { Channel: Channel
          Severity: Severity
          Message: string
          /// 0-based indices into the scored set; [] for whole-board findings
          Units: int list }

    /// One row of the fixed capacity table (FR-002).
    type ChannelSpec =
        { Channel: Channel
          Kind: ChannelKind
          /// reliable level count; meaningful for Categorical/Ordered
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
          /// one entry per per-unit channel (11; Motion excluded), table order
          Usage: ChannelUsage list
          Verdict: Verdict }

    /// The fixed capacity table the linter scores against (FR-002) — exposed read-only.
    /// One row per per-unit channel (11); `Motion` has no `ChannelKind` and is not a table row.
    val table: ChannelSpec list

    /// Score a static produced symbol set (FR-001/FR-003/FR-004/FR-005/FR-007/FR-011).
    /// Motion-load is not evaluated (no motion supplied).
    val score: tokens: Token list -> Report

    /// Score an animated produced symbol set; additionally evaluates whole-board motion load (FR-010).
    val scoreAnimated: board: (Motion * Token) list -> Report
