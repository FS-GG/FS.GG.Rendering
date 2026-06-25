# Contract — `FS.GG.UI.Symbology.Legibility` public surface

The contract is the curated `.fsi` for the new module. It is authored and FSI-exercised **before** any
`.fs` body (Constitution I). Symbols absent from this signature are private (Constitution II). Adding this
module regenerates `readiness/surface-baselines/FS.GG.UI.Symbology.txt` (Tier 1) with **zero drift** on
every other baseline and zero change to the `token`/`animate`/`gallery`/`filmstrip` rendering surface
(FR-013/SC-007).

## `src/Symbology/Legibility.fsi` (sketch — exact body deferred to implementation)

```fsharp
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
        | Faction | Klass | Sigil | Size | Heading
        | Threat | Charge | Health | Speed | State | Shield | Motion

    /// How the eye reads a channel — selects which checks apply (FR-003/FR-009).
    type ChannelKind =
        | Categorical   // separable discrete symbols/hues -> distinct-level overload
        | Ordered       // a small discrete count read as rank -> overload + domain
        | Continuous    // magnitude/gradient/angle -> domain only, overload-exempt

    /// Finding severity (FR-006). Error = grammar cannot encode the value; Warning = encodable but overloaded.
    type Severity =
        | Warning
        | Error

    /// One reported issue (FR-006).
    type Finding =
        { Channel: Channel
          Severity: Severity
          Message: string
          Units: int list }      // 0-based indices into the scored set; [] for whole-board findings

    /// One row of the fixed capacity table (FR-002).
    type ChannelSpec =
        { Channel: Channel
          Kind: ChannelKind
          Capacity: int }        // reliable level count; meaningful for Categorical/Ordered

    /// Per-channel usage evidence behind the findings (FR-007).
    type ChannelUsage =
        { Channel: Channel
          Kind: ChannelKind
          DistinctLevels: int    // for Continuous channels, the count of distinct raw values (informational — never drives an overload finding)
          Capacity: int }        // from the table; 0/ignored for Continuous

    /// Overall one-line signal (FR-007). Clean iff no findings.
    type Verdict =
        | Clean
        | HasWarnings

    /// The linter's whole output (pure, reproducible from the input alone — SC-001).
    type Report =
        { Findings: Finding list        // deterministic order: table order, then unit index
          Usage: ChannelUsage list      // one entry per per-unit channel (11; Motion excluded), table order
          Verdict: Verdict }

    /// The fixed capacity table the linter scores against (FR-002) — exposed read-only.
    /// One row per per-unit channel (11); `Motion` has no `ChannelKind` and is not a table row.
    val table: ChannelSpec list

    /// Score a static produced symbol set (FR-001/FR-003/FR-004/FR-005/FR-007/FR-011).
    /// Motion-load is not evaluated (no motion supplied).
    val score: tokens: Token list -> Report

    /// Score an animated produced symbol set; additionally evaluates whole-board motion load (FR-010).
    val scoreAnimated: board: (Motion * Token) list -> Report
```

## Behavioural contract (asserted by semantic tests)

| ID | Given | When | Then | Spec |
|---|---|---|---|---|
| C1 | a within-capacity set spanning channels | `score` | `Findings = []`, `Verdict = Clean` | AS-1, SC-002 |
| C2 | a set exceeding one categorical channel's capacity (e.g. > 7 distinct factions, or > 12 sigils) | `score` | exactly one `Warning` on that `Channel` with used-vs-capacity in `Message` and contributing `Units`; in-capacity channels emit nothing | AS-2, SC-002 |
| C3 | a set with > 4 distinct `Speed` bead counts | `score` | one `Warning` on `Speed` naming the units | AS-2, SC-002 |
| C4 | a unit with `Threat`/`Charge`/`Health` outside `[0,1]`, or `Speed` outside `[0,6]` | `score` | one `Error` naming that `Channel` and that unit; scan completes for the rest | AS-3, SC-003 |
| C5 | a unit with `R <= 0` (degenerate) | `score` | one `Error` on `Size` for that unit; remaining units still scored | AS-3, SC-003, SC-004 |
| C6 | a unit with a non-finite float on any field | `score` | one `Error` on that channel; no exception | FR-004, FR-008 |
| C7 | any set | `score` twice (same/another process) | the two `Report`s are structurally equal | AS-4, SC-001 |
| C8 | any report | inspect a finding | `Channel` + `Severity` are machine-readable; caller can filter by channel/severity and read `Verdict` without parsing text | AS-5, SC-006 |
| C9 | `[]` (empty set) | `score`/`scoreAnimated` | `Findings = []`, `Verdict = Clean`, usage all 0 | Edge, FR-011, SC-004 |
| C10 | an all-identical roster | `score` | `Verdict = Clean`; each categorical channel reports `DistinctLevels = 1` | Edge, FR-011 |
| C11 | a board with > 1 distinct non-`Idle` rhythm | `scoreAnimated` | one `Warning` on `Motion`, `Units = []` | Edge, FR-010 |
| C12 | a board with one rhythm across many moving units | `scoreAnimated` | no `Motion` finding (single rhythm is not a stack) | FR-010 |
| C13 | the approved M5/M6 roster (`roster \|> List.map Roster.mapUnit`) | `score` | `Verdict = Clean`, `Findings = []` | FR-014, SC-005 |
| C14 | scoring any valid-but-overloaded set | `score` | returns a report; never throws; inputs unmutated | FR-008 |

## Surface-baseline contract

- **Changes**: `readiness/surface-baselines/FS.GG.UI.Symbology.txt` gains the `Legibility` module and its
  nested types (`Channel`, `ChannelKind`, `Severity`, `Finding`, `ChannelSpec`, `ChannelUsage`, `Verdict`,
  `Report`, plus their `+Tags`/case payload entries) — regenerated via
  `dotnet fsx scripts/refresh-surface-baselines.fsx`.
- **Unchanged (zero drift, asserted)**: `FS.GG.UI.Symbology.Render.txt`, `FS.GG.UI.Scene.txt`,
  `FS.GG.UI.Controls.txt`, `FS.GG.UI.SkiaViewer.txt`, `FS.GG.UI.Canvas.txt`, and the existing
  `FS.GG.UI.Symbology.txt` entries (`Faction`/`Klass`/`Sigil`/`TokenState`/`Motion`/`Token`/`Symbology`).
