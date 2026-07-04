# Phase 0 Research — `fs-gg-game-core` product skill

All decisions below were resolvable from the repo; no `NEEDS CLARIFICATION` remained after the spec.

## D1 — Emission profiles (which params materialize the body)

- **Decision**: `(profile == "game" || profile == "sample-pack")` — no `lifecycle` clause.
- **Rationale**: #73 says "profile in [game, sample-pack]". Simulation is game-shaped; `game` is the
  primary host and `sample-pack` legitimately ships playable samples. `app`, `headless-scene`, and
  `governed` are excluded — a generic app / a headless scene renderer / a governed doc product is not a
  simulation consumer, and leaking a sim skill there is noise. Omitting the `lifecycle` clause matches
  every sibling product skill and is what makes the skill emit in **both** the spec-kit and sdd lanes.
- **Alternatives considered**: (a) include `app` (matches `fs-gg-scene`/`fs-gg-skiaviewer`) — rejected:
  over-broad, contradicts #73 and the "not a simulation" edge case; (b) gate on `lifecycle` too —
  rejected: no reason simulation guidance should differ by lifecycle, and it would break sdd-lane emission.

## D2 — One source drives both lanes

- **Decision**: Add exactly **one** `template.json` source (`template/product-skills/fs-gg-game-core/` →
  `.agents/skills/fs-gg-game-core/`, `copyOnly: ["**/*"]`), no separate sdd wiring.
- **Rationale**: `Feature219EmitFrameworkSkillsTests` derives the sdd-lane framework-skill set directly
  from the `template/product-skills/*` sources (a source with a profile predicate and no
  `lifecycle == "spec-kit"` clause emits for that profile under every lifecycle). So the single
  profile-gated source is simultaneously the spec-kit materialize input and the sdd-emit input. `copyOnly`
  ships the body byte-verbatim so its `sha256` in the manifest holds (ADR-0014 audit fix F5).
- **Alternatives considered**: a bespoke sdd emit entry — rejected: redundant and would double-count in
  Feature219's `sources.Length` assertion.

## D3 — Manifest regeneration is generator-only

- **Decision**: Add one tuple to the `generate-skill-manifest.fsx` catalog (kept sorted asc by id) and
  regenerate; never hand-edit `skill-manifest.json`.
- **Rationale**: The manifest is machine-generated and digest-checked (`--check`,
  `Feature231SkillManifestTests`). `suppliedByOf` already derives `supplied-by` from the source path, and
  the catalog tuple already carries the `materializes-when` string, so a single tuple yields a complete,
  correct 13th entry. Keeping the list sorted preserves the "entries ascending by id" invariant.
- **Alternatives considered**: manual JSON edit — rejected: violates the hand-free convention and would
  drift from the generator on the next `--check`.

## D4 — Which members the body may cite (surface-referenced check)

- **Decision**: The body cites only these Feature-239 members, and a test asserts each resolves in the
  packed `.fsi`:
  - `FS.GG.UI.Scene.Geometry`: `intersects`, `contains`, `containsPoint`, `center`, `ofCenter`,
    `sweptIntersects`
  - `FS.GG.UI.Canvas.Rng`: type `Rng = { State: uint64 }`; `ofSeed`, `nextFloat`, `nextInt`, `split`
    (each draw returns `struct(value * Rng)`)
  - `FS.GG.UI.Canvas.FixedStep`: `defaultMaxFrameTime`, `drain`, `drainWith`
  - Culling is expressed as `Geometry.intersects` / `Geometry.containsPoint` against the visible `Rect`
    — **no new API**.
- **Rationale**: Prevents a snippet that names a renamed/absent member from shipping. The packed
  surface under `template/base/docs/api-surface/{Scene,Canvas}` is the same `.fsi` a consumer reads.
- **Alternatives considered**: trust prose review — rejected: Principle I / SC-004 want machine evidence.

## D5 — Change tier & baseline impact

- **Decision**: **Tier 1 (contracted)**, but **no** `.fsi` / surface-area baseline change.
- **Rationale**: The skill-manifest catalog and the profile→skill emission matrix are cross-repo-consumed
  contracts (`.github#164`), so the full artifact chain applies. But no F# public surface is added or
  changed — the body only *documents* existing Feature-239 members — so per the Tier-1 rule the `.fsi`/
  baseline artifacts are correctly untouched, and the plan records that explicitly.

## D6 — RNG guidance shape (Constitution IV)

- **Decision**: The RNG section shows the value-type `Rng` stored in and threaded through the consumer's
  MVU `Model` — each draw returns `struct(value, next)` and the `next` state is written back to the model
  — and explicitly warns against a mutable `System.Random` in the `Model`.
- **Rationale**: This is the exact determinism smell Feature 239 set out to remove, and it keeps the
  guidance on the correct side of the MVU boundary (Principle IV) rather than modeling hidden mutable state.
