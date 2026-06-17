# 0008 — G2 Games + Productivity Sample Apps (curated slice)

**Status**: Accepted · **Feature**: 134 (`specs/134-sample-apps-g2/`) · **Date**: 2026-06-16

## Context

Workstream G2 adds runnable games + productivity samples on top of the G1 (feature 123)
Controls Gallery precedent. Like G1, the tree is a **package-only consumer** of `FS.GG.UI.*`
(local NuGet feed), kept **outside `FS.GG.Rendering.slnx`** — building it is the SC-006
public-consumer proof. It is **Tier 2 (additive consumer)**: no public product surface, no
`.fsi`, no design-token, no surface-baseline change.

## Decision

Ship a **curated slice of six** — three games (Tetris, Snake, Pong) and three productivity
apps (Todo, Kanban, Calendar) — rather than all ~22 archived specs. The other 16 are a
disclosed, machine-checked `Deferred` backlog (`coverage-backlog.md`).

Three divergences from the G1 pattern, each justified (plan Complexity Tracking):

1. **`Tick`-driven game loop** — game time advances only through injected `FrameInput.Tick`
   deltas mapped by the host's `Tick` to a step message (`Gravity`/`Advance`/`Step`). G1 set
   `Tick = fun _ -> None`; G2 is the first consumer to use the existing `Tick` seam for a real
   loop. No new framework surface.
2. **Pure `--seed`-driven PRNG** (`Prng.fs`, an MMIX LCG threaded through each game model) —
   all in-game randomness is a referentially-transparent function of `--seed`; no
   `System.Random`, no wall-clock (FR-006/SC-002).
3. **Closure-erased heterogeneous registry** (`SampleEntry`) — six independent MVU apps with
   distinct `Model`/`Msg` are held in one non-generic list by hiding the type parameters behind
   closures (`RunEvidence`/`Interactive`). F# has no first-class existential; this is the plain
   idiom.

## Consequences

- The deterministic seeded-evidence harness, the package-only evidence record, and the
  coverage/backlog honesty check are ported + extended from G1 (an `Outcome` block is added).
- Acceptance outcomes are **authored** (the archive text is not in-repo) and pinned as literals,
  asserted by the Expecto suite; the authoring + `FS.Skia.UI.* → FS.GG.UI.*` rebrand are
  disclosed in `PROVENANCE.md`.
- A decision record is **optional** for a Tier-2 consumer (G1 shipped without one); this one is
  recorded for the three divergences above.
