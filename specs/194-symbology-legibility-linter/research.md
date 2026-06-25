# Phase 0 Research — Symbology Legibility Linter

All decisions are bounded by the fixed §4 channel grammar already documented in
`src/Symbology/skill/SKILL.md` (lines 31–46) and the `Token` type in `src/Symbology/Symbology.fsi`.
The spec explicitly delegates "exact numeric thresholds and the precise channel→capacity encoding" to
this plan (Assumptions, spec.md). Each decision below resolves one such implementation detail and is
validated against the binding constraint that the **approved M5/M6 roster lints clean** (FR-014/SC-005).

---

## D1. Module home — new pure module in `src/Symbology/`, not a new project, not in `Symbology.Render`

- **Decision**: Add `Legibility.fsi` + `Legibility.fs` (namespace `FS.GG.UI.Symbology`, module
  `Legibility`) to the existing `src/Symbology/Symbology.fsproj`, compiled after `Symbology.fs`.
- **Rationale**: FR-012 requires the linter to live in the pure layer and depend only on the
  channel/`Token` types, with no rendering/raster/GL/IO. Those types are declared in `Symbology.fsi` in
  the same package; co-locating the linter keeps one shared channel vocabulary (FR-002) and adds no new
  project, no new dependency, and no path to raster machinery. `src/Symbology.Render/` is explicitly the
  *only* component allowed to reference `SkiaViewer`/IO — the linter must not go there.
- **Alternatives rejected**: (a) a new sibling project `FS.GG.UI.Symbology.Legibility` — adds a package
  and a project reference for no isolation benefit, since the linter needs the same `Token` types and is
  already pure; (b) folding it into `module Symbology` — muddies the rendering module with a scoring
  concern and a larger surface for one function group. A dedicated `Legibility` module is the smallest
  curated surface.
- **Consequence**: Tier 1 — the packable `FS.GG.UI.Symbology` surface grows, so
  `readiness/surface-baselines/FS.GG.UI.Symbology.txt` is regenerated (zero drift elsewhere).

## D2. The capacity table — per-channel kind + reliable level count (FR-002)

Encoded as fixed in-module data drawn from the §4 grammar table. Each `Token` field maps to one channel.

| Channel | `Token` field(s) | Kind | Capacity | Overload check | Domain check |
|---|---|---|---|---|---|
| `Faction` (hue) | `Faction` | Categorical | **7** | distinct faction values, each distinct `Custom` colour counted separately | — |
| `Klass` (silhouette) | `Klass` | Categorical | **6** | distinct silhouettes | — |
| `Sigil` (identity) | `Sigil` | Categorical | **12** | distinct identity marks (`Mark` paths by value) | — |
| `State` (dash) | `State` | Categorical | **3** | distinct inspection states | — |
| `Shield` (corner mount) | `Shield` | Categorical | **3** | distinct boolean mounts | — |
| `Speed` (tail beads) | `Speed` | Ordered (discrete count) | **4** | distinct bead counts | `0 ≤ Speed ≤ 6` (legible bead max) |
| `Size` (radius) | `R` | Continuous (magnitude) | — | exempt (FR-009) | `R > 0` (else degenerate, FR-005) |
| `Threat` (stroke width) | `Threat` | Continuous (magnitude) | — | exempt (FR-009) | `0 ≤ Threat ≤ 1` |
| `Charge` (interior gradient) | `Charge` | Continuous (gradient) | — | exempt (FR-009) | `0 ≤ Charge ≤ 1` |
| `Health` (belly arc) | `Health` | Continuous (magnitude) | — | exempt (FR-009) | `0 ≤ Health ≤ 1` |
| `Heading` (rotation) | `Heading` | Continuous (angle) | — | exempt (FR-009) | finite (angles wrap; any finite value valid) |
| `Motion` (whole-board rhythm) | paired `Motion` | Board-level | **1** | >1 distinct non-`Idle` rhythm across the board (FR-010) | — |

> **`Motion` is not a `ChannelSpec`/`table` row.** It is "Board-level": its budget of `1` is enforced by
> the dedicated whole-board motion-load check inside `scoreAnimated` (D4), not by a row in
> `Legibility.table`. `ChannelKind` has only `Categorical`/`Ordered`/`Continuous` — no board-level case —
> so `Motion` carries no `ChannelSpec` and no `ChannelUsage` entry; it surfaces only as a `Finding.Channel`
> value. Hence `table` and `Report.Usage` each have **11 entries** (the per-unit channels above), in this
> table's order. The `Motion` row here documents the budget, not a `table` entry.

- **Decision**: capacities are `Faction 7`, `Klass 6`, `Sigil 12`, `State 3`, `Shield 3`, `Speed 4`,
  motion-load budget `1` distinct active rhythm; the legible bead maximum is `6`; the normalised band for
  `Threat`/`Charge`/`Health` is `[0,1]`.
- **Rationale**: hue ~7, silhouette ~6, dash/mount ~3, speed/beads ~4 come straight from the §4 table.
  `Sigil` is listed as "many" in the grammar — identity marks are recognised, not pre-attentively
  separated — so it gets a generous soft bound of **12** (beyond ~a dozen distinct sigils on one board,
  identity becomes a lookup, not recognition); it never trips on the v1 roster. `Klass`/`State`/`Shield`
  capacities exceed the number of DU cases that ship, so they cannot overload in v1 but the linter is
  future-proof if the grammar grows.

## D3. Why `Threat`/`Charge`/`Size` are **continuous (overload-exempt)**, not raw-distinct-counted

This is the central correctness decision and the one that keeps the roster clean.

- **Problem**: FR-003 lists "the ordered magnitude channels" among channels to count for overload, but
  FR-014/SC-005 require the approved roster to lint clean. The roster's `Threat = min 1 (Dps/120)` over 8
  units yields **8 distinct float values**. Counting raw distinct floats would emit an overload finding
  on essentially *every* realistic board, contradicting FR-014 and making the linter useless.
- **Decision**: classify `Threat` (stroke width), `Charge` (interior gradient), `Size` (radius), along
  with `Health` (belly arc) and `Heading` (rotation), as **Continuous** — read by the eye as a
  magnitude/gradient, *not* as separable categories. Continuous channels are **exempt from the
  distinct-level overload check** (FR-009) and are checked only for **out-of-domain** values (FR-004).
- **Spec support**: FR-009 exempts continuous channels and says "at minimum heading rotation and the
  health arc" — the "at minimum" explicitly permits extending the exempt set. The Edge-Cases section
  defines continuous as "channels the eye reads as a magnitude/gradient rather than as categories";
  stroke-width (threat) and interior-gradient (charge) are textbook magnitude/gradient reads. The US1
  overload *examples* are **faction** and **tail-beads** only — both genuinely discrete — never
  threat/charge/size. The out-of-domain examples (US1 (c)) are exactly "magnitude outside its valid
  band", "excess speed", "zero/negative size" — i.e. these channels are domain-checked, not
  distinct-counted.
- **Alternative considered (rejected)**: *quantise* each continuous magnitude into `capacity` perceptual
  bands and count distinct occupied bands. This technically satisfies FR-003's "count distinct levels"
  for these channels, but the count can never exceed `capacity` (there are only `capacity` bands), so the
  check can never fire — it is a vacuous overload check dressed up as a real one. Reporting them honestly
  as continuous/domain-checked is clearer and avoids a misleading "0 overloads possible" check. (A future
  enhancement could add band-coverage reporting if a designer wants it; not needed for M7.)
- **Consequence**: the genuine distinct-level overload checks are reserved for channels where the
  designer chooses discrete levels and exceeding the eye's count is a real failure: **Faction, Klass,
  Sigil, State, Shield (categorical) and Speed (discrete count)**.

## D4. Whole-board motion load — budget = 1 distinct active rhythm (FR-010)

- **Decision**: `scoreAnimated : (Motion * Token) list -> Report` counts the **distinct non-`Idle`
  `Motion` values** present across the board. If that count is `> 1`, emit one whole-board `Motion`
  finding (severity `Warning`, `Units = []` because it is a board property, not a single unit). A single
  per-symbol rhythm is never itself flagged (the grammar applies one `Motion` per symbol structurally).
  `score : Token list -> Report` carries no motion and skips this check entirely.
- **Rationale**: the spec is explicit — "more than one simultaneously-active rhythm across the board is
  reported against the motion-channel budget" (Edge Cases; FR-010). This lifts the per-symbol legibility
  rule "one active motion at a time" to the whole board: the board should pulse to a single beat; many
  units may move, but with the *same* rhythm. Counting distinct rhythm *kinds* (not moving instances)
  honours "a per-symbol rhythm is structurally single … is never itself flagged as a stack".
- **Roster interaction**: the approved-roster-lints-clean assertion (FR-014) scores the **static** symbol
  set via `score` (no motion), which is exactly the M5 dry-run final set; motion-load does not apply
  there. The M6 animated board may legitimately surface a motion-load warning when scored *with* motion —
  that is advisory, by design (FR-008), and does not contradict FR-014.

## D5. Severities (FR-006)

- **`Error`** — the grammar cannot legibly encode the value at all: out-of-domain magnitudes
  (`Threat`/`Charge`/`Health` outside `[0,1]`), `Speed` outside `[0,6]`, non-finite floats (`NaN`/`±∞`)
  on any float field, and the **degenerate unit** (`R <= 0`).
- **`Warning`** — the value is encodable but the board is overloaded: categorical/speed distinct-level
  overload, and whole-board motion load.
- **Verdict** (FR-007): `Clean` when there are no findings; `HasWarnings` when any finding (of either
  severity) is present. The verdict is the one-line pass signal; callers needing error-vs-warning gating
  filter `Finding.Severity` themselves (FR-008 — gating is the caller's choice). A two-state verdict
  matches the spec's "clean / has-warnings" wording; severity carries the error-vs-warning detail.

## D6. Degenerate / out-of-domain handling — findings, never exceptions (FR-005/FR-008)

- **Decision**: a degenerate unit (`R <= 0`) produces a `Size`/`Error` finding naming the unit index and
  the scan **continues** for the remaining units; it never short-circuits or throws. Out-of-domain values
  likewise produce findings and the scan continues. The linter never raises on any input (FR-008) —
  malformed input is reported *as data*, consistent with Constitution VI's "safe failure".
- **Distinct-level counting in the presence of degenerates**: a degenerate unit still contributes its
  categorical channel values to distinct-level counts (its faction/class/sigil are real), but its
  out-of-domain magnitudes are reported, not counted as levels (the magnitude channels are continuous and
  not distinct-counted anyway — D3).

## D7. Edge cases (FR-011) — empty and all-identical

- **Empty set**: `score []` / `scoreAnimated []` returns `{ Findings = []; Usage = <every channel at 0
  distinct levels>; Verdict = Clean }` — vacuously legible, no divide-by-zero (no division is performed;
  counts are set cardinalities).
- **All-identical roster**: every unit shares one value per channel → exactly one distinct level per
  categorical channel, far under every capacity → `Clean`, with the usage summary reporting `1` distinct
  level per channel.

## D8. Roster-clean validation (FR-014/SC-005) — computed against the M5/M6 mapping

Using `Roster.mapUnit` and the 8-unit M6 roster literal (`samples/SymbologyBoard/Roster.fs`):

| Channel | Distinct levels used | Capacity | Result |
|---|---|---|---|
| Faction | `{Ally, Enemy}` = **2** | 7 | clean |
| Klass | `{Heavy, Scout, Mobile}` = **3** | 6 | clean |
| Sigil | `{Ring, Fang, Bolt}` = **3** | 12 | clean |
| Speed (`int(min 4 (Speed/4))`) | `{1,2,3,4}` = **4** | 4 | clean (at capacity; overload is strictly `>`) |
| State | `{Confirmed, Suspected}` = **2** | 3 | clean |
| Shield (`Armor > 40`) | `{true, false}` ≤ **2** | 3 | clean |
| Threat/Charge/Health/Size/Heading | continuous — domain only; all `R=30>0`, magnitudes in `[0,1]`, headings finite | — | clean |

`score (roster |> List.map mapUnit)` ⇒ **`Verdict = Clean`, `Findings = []`**. The exact-at-capacity
Speed result (4 distinct = capacity 4) independently confirms `Speed`'s `~4` threshold is the value the
approved designer actually used — a useful corroboration of D2.

## D9. Determinism (FR-001/SC-001)

- **Decision**: `score`/`scoreAnimated` are total pure functions — distinct-level counts via `Set`/
  `List.distinct`, capacity table as fixed data, findings produced in a fixed channel order (the §4 table
  order) then by ascending unit index. No `DateTime`, no RNG, no IO. Two calls on the same input return
  structurally equal `Report`s; the report is comparable by structural equality for the determinism test.
- **Finding ordering**: deterministic and stable (channel-table order, then unit index) so that the whole
  `Report` — not just a set of findings — is reproducible byte-for-byte in its serialised form.

## D10. Loop integration & skill parity (FR-015/SC-008)

- **Decision**: update the CRITIQUE step in the canonical `src/Symbology/skill/SKILL.md` (the "fixed
  feedback loop" section, step 4) to **run the linter** on the symbol set the current mapping produces —
  the mechanical complement to the human eyeball check — and keep the unit of change the per-game mapping
  (tweak the mapping until findings clear, never the grammar). The `.claude/` and `.agents/` wrappers are
  thin pointers to the canonical file and inherit the edit automatically; the
  `template/product-skills/fs-gg-symbology/SKILL.md` full copy is edited to mirror it. Parity is verified
  by `scripts/check-agent-skill-parity.fsx`.
- **Rationale**: this turns "a linter exists" (US1) into "the loop catches overload automatically" (US2)
  without re-opening the fixed grammar, and proves the linter agrees with prior human judgment (the
  approved roster lints clean, D8).

---

## Open questions

None. All NEEDS-CLARIFICATION items the spec delegated to planning (numeric thresholds, channel→capacity
encoding, motion-load interpretation, module home) are resolved above and validated against FR-014.
