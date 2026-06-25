# Design rationale — M5 dry-run approved symbol set

**Feature**: `192-agent-unit-symbology` | **Roster**: 8 units (blue / red / neutral; tank / scout / dps /
support; one armoured, two suspected). **Loop**: 3 iterations, mapping-only changes, grammar fixed.

## Channel assignments (approved)

| Stat | Channel | Why |
|---|---|---|
| `Side` (team) | **stroke hue** (`Faction`) | Most salient categorical channel; team is the first read. |
| `Role` (class) | **silhouette + sigil** (`Klass`/`Sigil`) | Shape + centre mark separate roles without colour. |
| `Dps` (threat) | **stroke width** (`Threat`) **and** **interior gradient** (`Charge`) | Urgent state encoded redundantly across two pre-attentive channels (assign-by-urgency + redundancy). |
| `Hp/HpMax` | **belly arc** length + green→red hue (`Health`) | Continuous, screen-aligned, low-salience detail. |
| `Speed` | **tail beads** (`Speed`) | Ordered, low-salience; reads as motion intent. |
| `Suspected` | **stroke dash** (`TokenState`) | Inspection-only; never the sole carrier of urgent state. |
| `Armor > 40` | **corner mount** (`Shield`) | Inspection-only boolean; redundant with the heavy silhouette. |
| `Facing` | **whole-body rotation** (`Heading`) | Body+sigil+tail rotate; health arc stays screen-aligned. |

## Iteration history (only the mapping changed)

1. **Round 1** — faction + class + health + heading; gentle threat curve. Critique: enemy DPS not urgent
   enough; suspected contacts invisible; fast units don't read.
2. **Round 2** — steeper threat curve, dash on suspected contacts, speed beads, charge from DPS.
   Critique: heavy armour still doesn't pop at a glance.
3. **Round 3 (approved)** — added the shield corner mount for armoured units (redundancy on a durable
   trait). Board reads cleanly at the target on-board size.

Distinct `SceneCodec` identities per round confirm every mapping tweak observably changed the board
(see each `round-N/mapping-snapshot.md`). The approved board re-exports byte-identically
(`golden-identity.txt`, `byte-stable: true`).

## Rejected alternatives

- **DPS on hue** — rejected: would collide with the faction hue channel (FR-019). DPS rides width+gradient.
- **Suspected on hue/size** — rejected: critical-vs-inspection separation; dash is inspection-only and is
  never the sole carrier of urgent state.
- **Stacking motion on the static board** — rejected here: one active motion at a time; motion is reserved
  for the `animate`/`filmstrip` activity overlay, not the at-a-glance roster board.

## Legibility notes

Faction is separable (blue/red/neutral saturated strokes); classes are distinct (heavy hexagon vs scout
diamond vs mobile arrowhead); health is readable as a screen-aligned belly arc under rotation; no channel
is pushed past its reliable level count (threat/charge/speed each stay within ~4 ordered levels).
