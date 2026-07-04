# Phase 1 Data Model — `fs-gg-game-core`

Two "entities": the **skill-manifest entry** (machine data) and the **SKILL.md body** (structured prose).

## 1. Skill-manifest entry (13th `skills[]` record)

Emitted by `scripts/generate-skill-manifest.fsx`; shape is `schemaVersion: 1` (Feature 238), unchanged.

```jsonc
{
  "id": "fs-gg-game-core",
  "scope": "product",
  "sha256": "<64-char lowercase hex of SKILL.md UTF-8 text>",   // computed by the generator
  "resolvablePath": ".agents/skills/fs-gg-game-core/SKILL.md",
  "materializes-when": "(profile == \"game\" || profile == \"sample-pack\")",
  "supplied-by": "template/product-skills/fs-gg-game-core/"
}
```

**Placement**: sorted ascending by `id` — lands between `fs-gg-feedback-capture` and
`fs-gg-keyboard-input` (…`feedback-capture`, `game-core`, `keyboard-input`…).

**Generator catalog tuple** (single source of truth for the two derived-into-manifest strings):

```fsharp
"fs-gg-game-core", "template/product-skills/fs-gg-game-core/SKILL.md",
    "(profile == \"game\" || profile == \"sample-pack\")"
```

**Invariants** (all test-enforced):
- `materializes-when` equals the `condition` on the `fs-gg-game-core` `template.json` body source
  (Feature 238 no-drift check).
- `sha256` equals `sha256(SKILL.md text)` (Feature 231 digest check).
- `supplied-by` = `dirname(source) + "/"` = `template/product-skills/fs-gg-game-core/`.
- Evaluated: `true` under `{profile=game}` and `{profile=sample-pack}`; `false` under
  `{profile=app|headless-scene|governed}`.
- The twelve pre-existing entries are byte-identical to the pre-feature manifest.

## 2. `template.json` source (the emission gate)

```jsonc
{
  "condition": "(profile == \"game\" || profile == \"sample-pack\")",
  "source": "template/product-skills/fs-gg-game-core/",
  "target": ".agents/skills/fs-gg-game-core/",
  "copyOnly": ["**/*"]
}
```

- No `lifecycle` clause → emits in both spec-kit and sdd lanes (see research D2).
- `copyOnly` → body ships byte-verbatim so the manifest `sha256` holds.

## 3. SKILL.md body model (structured prose)

Front-matter + sections mirroring the sibling product skills (`fs-gg-scene` as the template):

| Part | Content |
|---|---|
| front-matter | `name: fs-gg-game-core`; `description:` one line, family voice ("… in a generated FS.GG.UI product") |
| `# …` title | e.g. "Game Core (Simulation) Capability" |
| `## Scope` | when to use: deterministic update loop, seeded randomness, collision, culling — for `game`/`sample-pack` products |
| `## Public Contract` | points at the packed `Scene`/`Canvas` `.fsi` (`docs/api-surface/…`); names the modules `Geometry`, `Rng`, `FixedStep` |
| `## Fixed-timestep march` | `FixedStep.drain interval frameTime accumulator -> struct(int * float)`; `drainWith` clamp; `defaultMaxFrameTime`; totality on degenerate inputs |
| `## RNG determinism` | value-type `Rng = { State: uint64 }`; `ofSeed`/`nextInt`/`nextFloat`/`split`; `struct(value, next)` threaded through the MVU `Model`; **contrast** vs mutable `System.Random` (FR-003) |
| `## Collision` | `Geometry.intersects` / `contains` / `containsPoint` / `sweptIntersects` (fast projectiles); `center` / `ofCenter` |
| `## Culling` | keep only entities whose `Rect` `Geometry.intersects` the visible `Rect` (or `containsPoint`) — no new API |
| `## Common pitfalls` | mutable `System.Random` in `Model`; ignoring the returned `next` `Rng`; unbounded accumulator (spiral of death) → use `drainWith`/`defaultMaxFrameTime`; consumer `Point`/`Rect` colliding with framework types (as in `fs-gg-scene`) |

**Cited-member set** (exactly the D4 list) — the surface-referenced test asserts each exists in the packed
`.fsi`. A compilable end-to-end snippet (loop + draw + collide + cull) is included and forms the FSI-audience
evidence (Principle I / SC-004).
