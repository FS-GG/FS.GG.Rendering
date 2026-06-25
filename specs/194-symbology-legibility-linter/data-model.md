# Phase 1 Data Model — Symbology Legibility Linter

The linter's entire surface is a small set of pure value types in module
`FS.GG.UI.Symbology.Legibility`. They map 1:1 to the spec's Key Entities. All types are records/DUs with
structural equality (enabling the determinism assertion, SC-001). Channel identities and capacities are
derived from the §4 grammar (`research.md` D2); the renderer and the linter therefore share one channel
vocabulary (FR-002).

## Type: `Channel` — machine-readable channel identity (DU)

One case per scored channel of the §4 grammar. Stable, filterable identity (FR-006).

| Case | Grammar channel | `Token` field(s) |
|---|---|---|
| `Faction` | stroke hue → faction | `Faction` |
| `Klass` | silhouette → class | `Klass` |
| `Sigil` | centre mark → identity | `Sigil` |
| `Size` | radius → magnitude | `R` |
| `Heading` | rotation → heading | `Heading` |
| `Threat` | stroke width → threat | `Threat` |
| `Charge` | interior gradient → charge | `Charge` |
| `Health` | belly arc → health | `Health` |
| `Speed` | tail beads → speed | `Speed` |
| `State` | dash → confirmed/suspected | `State` |
| `Shield` | corner mount → boolean | `Shield` |
| `Motion` | whole-board rhythm | paired `Motion` |

## Type: `ChannelKind` — how the eye reads a channel (DU)

Drives which checks apply (`research.md` D3).

| Case | Meaning | Checks applied |
|---|---|---|
| `Categorical` | separable discrete symbols/hues | distinct-level overload |
| `Ordered` | a small discrete count read as rank | distinct-level overload + domain |
| `Continuous` | magnitude/gradient/angle read as "more/less" | domain only (overload-exempt, FR-009) |

> v1 mapping: `Faction`/`Klass`/`Sigil`/`State`/`Shield` are `Categorical`; `Speed` is `Ordered`;
> `Size`/`Threat`/`Charge`/`Health`/`Heading` are `Continuous`. `Motion` is handled by the dedicated
> board-level motion-load check, not by `ChannelKind` (it has no per-unit kind).
>
> **Because `Motion` has no `ChannelKind`, it has no `ChannelSpec` row in `table` and no `ChannelUsage`
> entry** — it appears only as a `Finding.Channel` value emitted by the board-level motion-load check
> (`scoreAnimated`). `Legibility.table` and `Report.Usage` therefore each carry exactly **11 entries**,
> one per per-unit channel, in table order.

## Type: `ChannelSpec` — one row of the fixed capacity table (record)

```
{ Channel  : Channel
  Kind     : ChannelKind
  Capacity : int }      // reliable level count; meaningful for Categorical/Ordered only
```

Exposed read-only as `Legibility.table : ChannelSpec list` (FR-002 — the stable reference the linter
scores against; also lets callers/tests inspect the capacities without reaching into internals). It
contains **one row per per-unit channel (the 11 channels that have a `ChannelKind`)**; `Motion` is
excluded because it has no `ChannelKind` (see the `ChannelKind` note above).

## Type: `Severity` — finding severity (DU)

`Warning` | `Error` (`research.md` D5). `Error` = the grammar cannot legibly encode the value
(out-of-domain, non-finite, degenerate). `Warning` = encodable but overloaded.

## Type: `Finding` — a single reported issue (record)

```
{ Channel  : Channel        // machine-readable identity (FR-006)
  Severity : Severity        // Warning | Error (filterable/gateable)
  Message  : string          // human-readable reason
  Units    : int list }      // 0-based indices into the scored set; [] for whole-board findings (Motion)
```

Validation rules → findings:

- **Categorical/Ordered overload** (FR-003) → `Warning`; `Units` = the indices contributing the
  over-budget distinct levels; `Message` names the channel and the used-vs-capacity counts.
- **Out-of-domain** (FR-004) → `Error`; `Units` = the single offending unit; `Message` names the channel
  and the bad value.
- **Degenerate unit** `R <= 0` (FR-005) → `Error` on `Size`; scanning continues.
- **Non-finite float** (NaN/±∞) on any float field → `Error` on that channel.
- **Whole-board motion load** > 1 distinct active rhythm (FR-010) → `Warning` on `Motion`; `Units = []`.

## Type: `ChannelUsage` — per-channel usage summary (record)

```
{ Channel        : Channel
  Kind           : ChannelKind
  DistinctLevels : int        // distinct levels used; for Continuous channels, the count of distinct raw values (informational — never drives an overload finding)
  Capacity       : int }       // from the capacity table; 0/ignored for Continuous
```

`Report.Usage` carries one entry per channel in `table` order — the 11 per-unit channels; `Motion` is
excluded (it has no `table` row). The usage is the evidence behind overload findings and a quick read of
how close each channel is to its budget (FR-007). For `Continuous` channels `DistinctLevels` is reported
as the count of distinct raw values (informational only; never drives an overload finding — D3).

## Type: `Verdict` — overall signal (DU)

`Clean` | `HasWarnings` (FR-007). `Clean` ⇔ `Findings = []`. Any finding (either severity) ⇒
`HasWarnings`. Error-vs-warning gating is the caller's, via `Finding.Severity` (FR-008).

## Type: `Report` — the linter's whole output (record)

```
{ Findings : Finding list       // all issues, deterministic order (table order, then unit index)
  Usage    : ChannelUsage list  // one entry per per-unit channel (11; Motion excluded), table order
  Verdict  : Verdict }
```

Pure and reproducible from the symbol set alone (SC-001). Structural equality lets the determinism test
assert `score s = score s`.

## Functions (the public surface — full signatures in `contracts/legibility-api.md`)

| Function | Signature | Notes |
|---|---|---|
| `table` | `ChannelSpec list` | the fixed capacity table (FR-002) |
| `score` | `Token list -> Report` | static board; motion-load skipped (FR-001) |
| `scoreAnimated` | `(Motion * Token) list -> Report` | adds whole-board motion-load check (FR-010) |

## Entity → spec Key-Entity mapping

| Spec Key Entity | Type(s) here |
|---|---|
| Channel capacity table | `ChannelSpec` + `Legibility.table` |
| Symbol set under test | the `Token list` / `(Motion * Token) list` input (no new type — reuses `Token`) |
| Legibility finding | `Finding` (+ `Channel`, `Severity`) |
| Channel usage summary | `ChannelUsage` |
| Legibility report | `Report` (+ `Verdict`) |

## State transitions

None — the linter is a pure, stateless function. There is no lifecycle, no mutation, no Elmish model
(Constitution IV is N/A; `research.md` D9 / plan Constitution Check).
