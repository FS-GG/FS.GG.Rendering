# 0010 — `fs-gg-ui-template` default starter: a replaceable `game` profile

**Status**: Accepted · **Feature**: 220 (`specs/220-game-starter-scene/`) · **Date**: 2026-06-30

> Seam drafted in T007; accepted in T026 after US1–US3 landed green (game default 26/26,
> swap-to-Pong 27/27 with 0 GovernanceTests edits, FR-007 byte-identical for the three
> non-game profiles, `app` controls showcase 30/30).

## Context

The `fs-gg-ui-template` scaffold ships the **controls showcase** (`app` profile) as the default
starter for the game/rendering provider, and the durable governance test historically pinned a
specific UI family's launch call. A consumer who wanted a real game at the default entrypoint had
to hide it behind a `-- pong`-style flag, because replacing the starter `Model`/`View` at the
normal entrypoint tripped a governance gate. The template machinery already encoded a latent "game
family" as the `//#else` of `profile == "app"` (today reached only by `sample-pack`), but no
profile selected it as a real, exercised path.

This is a **Tier 1** change to the `fs-gg-ui-template` cross-repo contract: it alters the default
starter family, adds a profile, and relaxes a governance assertion. It changes observable generated
output for the new profile only; it changes **no `FS.GG.UI.*` package public surface** (no `.fsi`,
no design-token, no surface-baseline).

## Decision

1. **Add an explicit `game` profile** (`template/profiles/game.yml` + a fourth → fifth `choice` in
   `.template.config/template.json`) that ships a **minimal Pong-style MVU skeleton**
   (`Model`/`Msg`/`update`/`view` + tick) explicitly designated "replace me", and selects the game
   `//#else` content branches throughout `template/base`. Capabilities mirror `app.yml`
   (`scene, skiaviewer, elmish, keyboard-input, layout, controls, full-governance`).
2. **Keep `app` as the explicit, opt-in controls-showcase option** (FR-006) — re-described, output
   unchanged.
3. **Two distinct profile groupings, each pinning `sample-pack`** (confirmed safe by the T004
   probe): content split `game` vs controls (`app | sample-pack`); launch split `app →
   runInteractiveApp` vs `game | sample-pack → Viewer.runApp generatedHost`. `headless-scene` /
   `governed` / `sample-pack` generated output stays **byte-identical** (FR-007), verified by diff.
4. **Relax the durable governance spine to be UI-family agnostic** at the default entrypoint: the
   game-branch launch assertion (`Viewer.runApp viewerOptions generatedHost`) is satisfiable by the
   minimal skeleton **and** survives a Pong swap; no controls-only token is pinned in the game
   branch; no alternate launch flag is a precondition for green governance (FR-002/FR-003/FR-008).
5. **The default-starter selection flip `app → game`** is owned by the **SDD scaffold-provider** and
   is the coordinated cross-repo contract change (C5), sequenced alongside Coordination item **#32**.

## Consequences

- A game/rendering scaffold's no-flag launch renders a live interactive game scene replaceable at
  the default entrypoint, with zero governance-test edits on a swap (SC-001/SC-004).
- SDD must enumerate `game` and flip the default selection; Templates must accept the new default +
  relaxed assertion. Tracked in the Coordination-board issue (T028) and the contract/compatibility
  registry entry (T027).
- The template is republished at a coherent preview version (T029) so downstream is not silently
  broken.
</content>
