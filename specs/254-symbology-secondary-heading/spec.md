# Feature Specification: An Opt-In Second Rotation Channel on the Symbology `Token`

**Feature Branch**: `item/260-cross-repo-symbology-token-has-one-headi`

**Created**: 2026-07-10

**Status**: Shipped

**Input**: FS-GG/FS.GG.Rendering#260 (cross-repo request from FS.GG.Game). Contract: `fs-gg-symbology`
(`FS.GG.UI.Symbology`). Decision recorded as [ADR-0102](../../docs/product/decisions/0102-symbology-secondary-heading-channel.md).

## Context (why this feature, in plain terms)

`Token` carried a single `Heading : float` — one body orientation, drawn as whole-body rotation in
`Grammar.Token` and as a discrete indicator in `Badge`/`Ring`.

A tank is two independently rotating bodies. The **hull** decides where it drives and which armor plate
faces you; the **turret** decides where the gun points. FS.GG.Game's design makes that split the
load-bearing mechanic, and the fixed grammar could not express it. The shape is not tank-specific: any
turreted vehicle, any unit whose facing differs from its move direction, and any RTS unit with a
separate weapon arc has it.

ADR-0102 chose to **add the channel** rather than sanction a caller-drawn `Sigil.Mark` workaround (which
the legibility linter could not see) or declare the case out of scope (which would exclude every
two-facing consumer at once).

## Requirements

- **FR-001** — `Token` carries `SecondaryHeading : float option`: an absolute angle, `0.0` = north,
  independent of `Heading`. Angles wrap, so any finite value is in-domain.
- **FR-002** — **Zero drift.** `SecondaryHeading = None` is the default, and such a token renders
  byte-identically to the pre-feature symbol in every grammar. Absence contributes **no scene node** —
  not an empty one. (`Scene.empty` is itself a node: `Scene.describe` yields `EmptyElement` for it, so
  the usual "return `Scene.empty` when off" shape would have drifted every golden.)
- **FR-003** — All three grammars draw it, and none degrades. The indicator is a centre-out **barrel
  with a tip mark**, starting clear of the centre sigil (`0.15R`) and sited per grammar so it can never
  be misread as the primary nose / rim pip / needle. It is emitted as bare sibling nodes, never wrapped
  in a group.
- **FR-004** — `Legibility` gains a `SecondaryHeading` channel and one `table` row, scored exactly as
  `Heading` is: `Continuous` (therefore overload-exempt), with a non-finite angle the only `Error`. An
  unset channel is legal and never yields a finding. The DU case is declared **last** so that adding it
  does not renumber the existing cases' compiler-generated tags.
- **FR-005** — A barrel reaches `1.42R`, beyond the `1.3R` a `filmstrip` cell owns at the historic
  `2.6R` spacing. Cells widen to hold it **only when a token in the strip sets the channel**, so a
  barrel-free filmstrip is byte-unchanged.

## Success criteria

- **SC-001** — The pre-feature canonical-byte goldens in `DeterminismTests` pass **unchanged**. These
  are hardcoded SHAs, so they pin FR-002 permanently rather than by convention.
- **SC-002** — For each grammar, two tokens differing only in `SecondaryHeading` produce differing
  canonical bytes, and two tokens differing only in `Heading` still do — the two rotation channels are
  independently observable.
- **SC-003** — For each grammar, setting the channel adds exactly two scene nodes (barrel + tip) and
  unsetting it adds none. A future refactor that starts emitting `Scene.empty` for `None` fails here,
  with a readable reason, instead of as an opaque golden-hash mismatch.
- **SC-006** — SC-002 compares whole-scene bytes and so cannot tell an absolute barrel from a
  body-relative one (both move the bytes). The barrel's own nodes are therefore isolated and compared
  across two body angles: same `SecondaryHeading` ⇒ same barrel, whatever `Heading` does.
- **SC-007** — In a two-cell `filmstrip`, an east-pointing barrel's tip mark stays on its own side of
  the cell boundary; a barrel-free `filmstrip` draws no circles and keeps the `2.6R` spacing.
- **SC-004** — A degenerate token (`R <= 0`) still degrades to the placeholder, barrel or not: the
  placeholder rule wins over the secondary indicator exactly as it wins over the label.
- **SC-005** — `Legibility.score []` reports 12 `ChannelUsage` entries; `SecondaryHeading` findings sort
  after `Heading` and before `Motion`.

## Out of scope

- **No per-unit "the two rotations are nearly aligned" warning.** A tank driving forward with its gun
  forward is the normal rest state, so such a check fires constantly on correct input. The confusability
  hazard is a property of the grammar and is discharged by drawing (FR-003), not by linting. See
  ADR-0102 § *The second indicator must not be readable as the first*.
- **No `AutoField` selector.** `Heading` has no auto-label projection either; a compact angle code is not
  a legible label field.
- **No version bump.** Per standing practice the `<Version>` move belongs to a separate `release:`
  commit. ADR-0102 records that the next `FS.GG.UI.Symbology` release must be a **minor** bump, because
  `Token.new(...)` gaining a parameter is a binary break.
