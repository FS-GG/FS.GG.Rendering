# 0102 — `Token` gains an opt-in second rotation channel; the fixed grammar stays fixed

**Status**: Accepted · **Date**: 2026-07-10 · **Issue**: [FS.GG.Rendering#260](https://github.com/FS-GG/FS.GG.Rendering/issues/260)

> **Numbering.** Repo-local ADRs resume at `0100` (see [ADR-0100](./0100-gate-is-a-required-check.md)).
> This one follows [ADR-0101](./0101-apicompat-stays-advisory.md).

## Context

FS.GG.Game is building a top-down tanks game and wants to consume `FS.GG.UI.Symbology` verbatim.
The fit is good — `Klass` is already the roster vocabulary, and `Threat`/`Health`/`Speed`/`Shield`/
`Faction` map onto the tank stat vector. `TokenState = Confirmed | Suspected` is exactly the
spotted-vs-last-known ghost the design needs.

One thing does not fit. `Token` carries a **single** `Heading : float`. A tank is two independently
rotating bodies: the **hull**, which decides where it drives and which armor plate faces you, and the
**turret**, which decides where the gun points. That split is the load-bearing mechanic of the design.

The requester offered three options and explicitly did not object to any of them, asking only that the
outcome be **recorded** — because the `fs-gg-symbology` doctrine says the grammar is fixed and the
per-game mapping is data, and this is a case where the fixed grammar cannot express the state.

## Decision

**Add an opt-in second rotation channel: `Token.SecondaryHeading : float option`.** This is the
requester's option 2.

- `None` is the default. A token that does not set it renders **byte-identically** to the pre-feature
  symbol in all three grammars — it contributes no scene node at all.
- `Legibility.Channel` gains a `SecondaryHeading` case and `Legibility.table` a row, scored exactly as
  `Heading` is: `Continuous`, therefore overload-exempt; angles wrap, so only a non-finite angle is an
  `Error`.
- All three grammars draw it. None degrades.

### Why not option 1 (`Sigil.Mark of PathSpec` as the sanctioned workaround)

The requester named the cost and we agree with it: the mark is not rotated by the grammar, so the game
would re-implement rotation outside the vocabulary, and the legibility linter would never see the
channel. A channel the linter cannot see is a channel the doctrine cannot govern. `Sigil` is the
*identity* slot; spending it on a rotation would also cost the game its identity mark.

### Why not option 3 (out of scope)

The gap is not tank-specific. Any turreted vehicle, any unit whose facing differs from its move
direction, and any RTS unit with a separate weapon arc has the same shape. "Unit symbols, not vehicle
schematics" would be a defensible line if the second rotation were *detail*; it is not — it is the
state that decides which armor plate a shot lands on. Declaring it out of scope would push every
consumer with two facings out of the vocabulary at once.

### Why this does not break the "fixed grammar" doctrine

The doctrine is that the **channel set** is fixed and the **`'stats -> Token` mapping** is per-game
data. This adds a channel to the fixed set — a deliberate, reviewed, one-time act, exactly as features
198/199/200 added `Label`/`AutoLabel`/`LabelMotion`. It does **not** move any per-game decision into
the library: the game still decides which of its stats is the hull angle and which is the turret angle.

## Consequences

### The second indicator must not be readable as the first

A second rotation on one glyph is a real legibility hazard — the requester raised it. The mitigation is
**form and siting**, not a linter warning. Each grammar draws the secondary as a centre-out **barrel
with a tip mark**, placed where no primary indicator lives:

Every barrel starts at `0.15R`, clear of the centre identity sigil (which each grammar draws out to
`0.42R`) — a line struck through the sigil muddies the identity channel exactly where it is read. Only
the outer end varies:

| Grammar | Primary heading | Secondary heading |
|---|---|---|
| `Token` | whole-body silhouette rotation | barrel overshooting the hull (`1.32R`), tip clear of the belly arc (`1.18R`) |
| `Badge` | pip on the frame rim (`1.0R`, radius `0.12R`) | barrel stopping well inside it (`0.70R`) |
| `Ring`  | needle inside the ring (`0.95R`) | barrel pushing its tip outside the ring (`1.30R`) |

So the two are told apart by **extent and form** even when they point the same way.

The `Badge` figure was set by rendering the aligned case and looking at it. A barrel reaching `0.86R`
puts its `0.10R` tip mark against the rim pip's inner edge at `0.88R`, and the two merge into one blob
precisely when the headings agree — which, for a tank driving forward with its gun forward, is the
common rest state. `0.70R` leaves a visible gap there. `Ring` never had the problem: its barrel exits
the ring, so alignment reads as a needle with a dot beyond the rim.

### A barrel is the widest thing a symbol draws, so `filmstrip` cells had to grow

The barrel reaches `1.42R` (outer `1.32R` plus a `0.1R` tip mark) — further than the previous widest
static element, the belly arc at `1.18R`. `filmstrip`/`filmstripIn` lay cells out at `2.6R`, giving
each `1.3R`, so a strip of turreted units would have bled into its neighbours.

Cells therefore widen to `2 × 1.42R` **only when some token in the strip sets the channel**. A
barrel-free strip keeps `2.6R` exactly, so every existing filmstrip golden is untouched — the same
absent-means-unchanged discipline as the scene nodes themselves.

### The `Channel` case is declared last, not next to `Heading`

A DU case's compiler-generated tag is its declaration index. Declaring `SecondaryHeading` beside
`Heading` — where it reads best — would renumber `Threat`…`Motion`, so any consumer holding a persisted
numeric tag would decode a `Threat` finding as `Charge`. Declaration order carries no meaning here
(neither `table` nor `channelOrder` follows it), so the case is declared last and *ordered* next to
`Heading` where it belongs.

We considered, and rejected, encoding "the two rotations are nearly aligned" as a per-unit `Warning`.
A tank driving forward with its gun forward is the *normal* rest state, so that check would fire
constantly on correct input. The hazard is a property of the grammar, discharged above by drawing;
it is not a property of a token.

### `SecondaryHeading` is `Continuous`, so `Usage` grows from 11 rows to 12

`Legibility.score []` now reports **12** `ChannelUsage` entries. An all-`None` roster reports one
distinct level for the channel, exactly as an all-identical `Heading` roster does. Consumers that
hard-coded `Usage.Length = 11` must update; nothing else in the report moves.

### Adding a record field is a binary break, and ApiCompat will say so

`Token.new(...)` gains a parameter, so the packed assembly is not binary-compatible with the published
`FS.GG.UI.Symbology 0.4.0` — the feed's newest version, per the `latest_version` ordering fix ADR-0101
made. ApiCompat reports exactly one `CP0002`, on that constructor, and nothing else.

Per this repo's standing practice the version bump belongs to a separate `release:` commit, not to the
feature: features 198/199/200 each added a `Token` field without touching `Symbology.fsproj`. **The
next `FS.GG.UI.Symbology` release must be a minor bump (`0.5.0-preview.1`), not a patch**, and
publishing it discharges the break by moving the baseline.

### ApiCompat is a required check, whatever ADR-0101 says

[ADR-0101](./0101-apicompat-stays-advisory.md) is titled *"`API compatibility gate` stays advisory"*,
and the header of `scripts/apicompat-check.sh` says the job "is not in branch protection's required
set, so today a break informs a merge rather than blocking one."

**Both are wrong.** `main`'s branch protection lists two required contexts:

```
Deterministic gate
API compatibility gate (breaking-change → SemVer major)
```

with `enforce_admins: true`, so not even an admin merge bypasses it. A `CP0002` wedges the PR outright.
This was discovered by trying to merge, not by reading — the documentation and the enforced policy have
drifted apart, and the enforced policy wins. Reconciling them (either re-titling ADR-0101 or removing
the required context) is a governance change out of scope here; it should be filed against `gate.yml`
and ADR-0101.

The remedy the script itself names is a deliberate suppression, and the repo has precedent:
`src/SkiaViewer/CompatibilitySuppressions.xml`, added for `ViewerOptions.LogicalSize` (#246) — the same
"F# record gained a field, so its constructor arity changed" shape. So this feature ships
`src/Symbology/CompatibilitySuppressions.xml`: **one** diagnostic, **one** target, and
`IsBaselineSuppression` so that a *new* break introduced later still reddens the gate. It carries a
lifetime note: delete it once a release `>= 0.5.0` is on the feed.

Verified rather than assumed — packing `Symbology` against baseline `0.4.0` exits `1` with one `CP0002`
without the file, and exits `0` with it.

### Source compatibility

Unaffected for the supported construction style: `{ Symbology.defaultToken with ... }` keeps compiling.
A caller that writes a full record literal must add the field — none exists in this repo, and the roster
mapping the skills teach uses `defaultToken`.
