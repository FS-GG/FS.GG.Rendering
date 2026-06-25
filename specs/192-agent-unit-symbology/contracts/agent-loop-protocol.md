# Contract — `fs-gg-symbology` agent loop protocol

**Phase 1 (/speckit-plan).** This is the *behavioral* contract the orchestrating skill (M4) must encode
and the dry-run (M5) must demonstrate. It is not an F# surface; it is the fixed protocol + guardrails that
make the design loop repeatable and auditable (FR-014, FR-016, FR-017, FR-018). The skill is authored once
and mirrored to `.claude/skills/`, `.agents/skills/`, `template/product-skills/` (R4/G4), and must pass
`scripts/check-agent-skill-parity.fsx --fail-on high` (SC-005).

## Inputs

- A unit roster with per-unit stats (`name, faction, role, hp, dps, speed, armor, heading, …`).
- A target on-board symbol size (legibility is size-dependent — SC-007).
- A working directory for provenance.

## The fixed loop (FR-016 — the unit of change each iteration is the per-game mapping, never the grammar)

```
1. INTAKE   read roster + stats; pick grammar (default + only v1: Directional Token).
2. MAP      draft ChannelMap : 'stats -> Token  (assign-by-urgency; redundancy on critical state).
3. RENDER   FSI: build `Symbology.gallery …`; `Render.toPng size scene dir`; READ THE PNG BACK.
4. CRITIQUE self-check vs the legibility rules (below) at the target size.
5. REVIEW   present the PNG to the human; capture feedback.
6. TWEAK    adjust the ChannelMap / Token params ONLY (never library internals); goto 3.
7. APPROVE  on satisfaction: emit final symbol-set module + rationale; pin a golden board.
```

## Legibility rules the skill MUST encode and CRITIQUE against (FR-014)

- **Assign-by-urgency**: the most urgent state goes on the most salient channels (hue, motion, size).
- **Redundancy on critical state**: encode urgent state across *multiple* pre-attentive channels.
- **One active motion at a time**: never stack motion rhythms on one symbol.
- **Never critical state on dash alone**: dash + corner mounts are inspection-only channels.
- **No faction/state hue collision** (FR-019): faction uses the saturated palette; state semantics reuse
  the repo's Ant status tokens — they never share the hue channel.
- **Critique checklist**: faction separable? class distinct? health readable at target size? any channel
  overloaded beyond its reliable level count (§ data-model channel table)?

## Provenance the loop MUST write (FR-017 / FR-018, D7)

- **Every iteration** → under the working dir: a *timestamped board image* (the rendered gallery PNG) **and**
  a *snapshot of the mapping* that produced it. Together these form an auditable design history.
- **On approval** → a *final symbol-set module* (pure drawing-producing functions), a *design rationale*
  (channel assignments + rejected alternatives + legibility notes), and a *pinned golden board* with a stable
  `SceneCodec` identity.

Timestamps/filenames are stamped by the *workflow*, not by library code — the library and render helper read
no clock (determinism preserved).

## Determinism obligations the loop relies on (carried from the library contracts)

- `token`/`animate`/`gallery`/`filmstrip` are pure ⇒ a re-render of an unchanged mapping is byte-identical
  (SC-001) and filmstrip frames are reproducible from the phase schedule (SC-006).
- `Render.toPng` fails loud (never a blank success) so a CRITIQUE step never reasons over a fake image (SC-008).

## Acceptance of the protocol (M5 dry-run — SC-009)

A dry-run on a real roster (6–10 units), across ≥2 feedback rounds where **only** the `ChannelMap` changes
between rounds, must produce for *every* iteration a timestamped board + mapping snapshot, and on approval a
final module + rationale + a pinned golden board — a complete render→tweak→approve audit trail. The skill
must be present and consistent across all three trees (skill-parity green).
