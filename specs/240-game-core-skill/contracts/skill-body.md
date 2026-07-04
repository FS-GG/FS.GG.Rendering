# Contract — `fs-gg-game-core` SKILL.md body

**Artifact**: `template/product-skills/fs-gg-game-core/SKILL.md`
**Materializes to**: `.agents/skills/fs-gg-game-core/SKILL.md` (byte-verbatim, `copyOnly`)
**Consumers**: the generated product's coding agent; `Feature225ProductSkillVocabularyTests` (quality),
`Feature231SkillManifestTests` (digest), the surface-referenced test (cited members).

## Required front-matter

```yaml
---
name: fs-gg-game-core
description: <one line, family voice, e.g. "Simulate a generated FS.GG.UI product — deterministic
  fixed-step loop, seeded RNG, AABB collision, and entity culling.">
---
```

- `name` MUST be exactly `fs-gg-game-core`.
- `description` MUST be a single line naming the simulation value and end in "FS.GG.UI product" phrasing,
  matching the sibling skills' voice (`Feature225` vocabulary check).

## Required sections (see data-model §3 for the full table)

1. **Scope** — game/sim consumers; the four patterns; note it emits for `game`/`sample-pack`.
2. **Public Contract** — reference the packed `.fsi` under `docs/api-surface/{Scene,Canvas}`; name modules
   `Geometry` (Scene), `Rng` and `FixedStep` (Canvas).
3. **Fixed-timestep march** — `FixedStep.drain` / `drainWith` / `defaultMaxFrameTime`; the `struct(int * float)`
   return; totality on degenerate inputs (non-positive interval, negative dt) — never throws.
4. **RNG determinism** — value-type `Rng`; `ofSeed`/`nextInt`/`nextFloat`/`split`; `struct(value, next)`
   threaded through the MVU `Model`; explicit contrast vs a mutable `System.Random` in the `Model` (FR-003).
5. **Collision** — `intersects` / `contains` / `containsPoint` / `sweptIntersects` / `center` / `ofCenter`.
6. **Culling** — `Geometry.intersects` / `containsPoint` against the visible `Rect`; no new API.
7. **Common pitfalls** — enumerated in data-model §3.

## Cited-member contract (surface-referenced check)

The body MUST NOT name any FS.GG.UI member outside this set (each MUST resolve in the packed `.fsi`):

| Module (namespace) | Members |
|---|---|
| `Geometry` (`FS.GG.UI.Scene`) | `intersects`, `contains`, `containsPoint`, `center`, `ofCenter`, `sweptIntersects` |
| `Rng` (`FS.GG.UI.Canvas`) | type `Rng` (`{ State: uint64 }`), `ofSeed`, `nextFloat`, `nextInt`, `split` |
| `FixedStep` (`FS.GG.UI.Canvas`) | `defaultMaxFrameTime`, `drain`, `drainWith` |

## Compilable snippet contract (Principle I / SC-004)

The body MUST include one end-to-end snippet that: opens `FS.GG.UI.Scene` + `FS.GG.UI.Canvas`; drains an
accumulator with `FixedStep.drain`; draws with `Rng.nextInt`/`split` threading the returned state;
tests a collision with `Geometry.intersects` or `sweptIntersects`; and culls a list against a visible
`Rect`. It MUST compile against the packed Feature-239 `.fsi` (no private/renamed members, correct
`struct` deconstruction, correct arities).

## Acceptance

- `Feature225` vocabulary + leak checks pass on the new body.
- The surface-referenced test finds every cited member in the packed `.fsi`.
- `sha256(body)` matches the regenerated manifest entry.
