# Phase 0 Research: Collision Detection Skill + Import-and-Adapt Helper Source

All decisions below resolve the Technical Context so no `NEEDS CLARIFICATION` remains for Phase 1.

## R1 — Delivery mode: package API vs. adaptable source

**Decision**: Ship the collision **response** layer as **product-owned, adaptable source**
(`Collision.fs`) materialized into the game/sample-pack product; keep **detection** as the existing
package API it reuses. Do **not** add a new packable library or `.fsi`.

**Rationale**: Detection is objective and already frozen — `Geometry.intersects`/`contains`/
`containsPoint`/`center`/`ofCenter`/`sweptIntersects` (`FS.GG.UI.Scene`) and `SpatialGrid.build`/
`query`/`queryRadius` (`FS.GG.UI.Canvas`). Response (which way / how far bodies separate, slide vs.
bounce, restitution, layer rules) is *per-game policy* the consumer must edit — the wrong thing to
freeze behind a surface-baselined `.fsi`. The requester confirmed "import and adapt," which is the
consumer-owned-source shape (like the replaceable starter `Model.fs`/`View.fs`), not a referenced
package.

**Alternatives considered**:
- *New `FS.GG.UI.Collision` package*: rejected — freezes policy that every game overrides, adds a
  surface baseline and coherent-set member for a thing meant to be edited, and contradicts the ask.
- *Extend `Geometry`/`SpatialGrid` with response functions*: rejected — pollutes objective detection
  primitives with opinionated response; still frozen; still can't be deleted/adapted.

## R2 — Where the skill and source live, and how they register

**Decision**: Skill body at `template/product-skills/fs-gg-collision/SKILL.md` (sibling to
`fs-gg-game-core`); adaptable source at `template/fragments/collision/src/Product/Collision.fs` with a
fragment `README.md`. Register the skill through the **skill-only** path, identical to `fs-gg-audio`/
`fs-gg-game-core`:

1. `scripts/generate-skill-manifest.fsx` — add to the `catalog` list:
   `"fs-gg-collision", "template/product-skills/fs-gg-collision/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"`.
2. `template/skill-manifest/skill-manifest.json` — regenerated entry (id, sha256 of the body,
   `resolvablePath: .agents/skills/fs-gg-collision/SKILL.md`, `materializes-when: profile in [game, sample-pack]`,
   `supplied-by: template/product-skills/fs-gg-collision/`).
3. `.template.config/template.json` — **two** gated `sources[]`:
   - skill: `condition (profile == "game" || profile == "sample-pack")`, `source template/product-skills/fs-gg-collision/`,
     `target .agents/skills/fs-gg-collision/`, `copyOnly ["**/*"]`.
   - source: `condition (profile == "game" || profile == "sample-pack")`, `source template/fragments/collision/src/Product/`,
     `target src/Product/` (sourceName substitution applies — becomes `src/<ProductDir>/`).
4. `template/base/docs/skillist-reference.md` — add the `fs-gg-collision` row to the full registry.
5. Dev roots: `.agents/skills/fs-gg-collision/SKILL.md` (canonical) + `.claude/skills/fs-gg-collision/`
   mirror via `template/lifecycle/materialize-skill-roots.fsx`; parity asserted by
   `scripts/check-agent-skill-parity.fsx`.

**Rationale**: `capabilities.yml` lists only package/fragment-backed *package* capabilities (scene,
skiaviewer, elmish, keyboard-input, layout, controls, testing, samples). The skill-only capabilities
(`fs-gg-game-core`, `fs-gg-audio`, `fs-gg-persistence`, `fs-gg-model-swap`) are absent from it and
register through the manifest/template/skillist path — so `fs-gg-collision` follows the same path and
`capabilities.yml` is **not** edited. The `template.json` gate condition MUST be *semantically equal* to
the manifest `materializes-when` (Feature 238 test enforces), so both use the same
`profile ∈ {game, sample-pack}` expression the sibling skills use.

**Alternatives considered**:
- *Skill under the fragment (`template/fragments/collision/skill/`, like `fs-gg-samples`)*: workable but
  the collision skill is a first-class capability sibling of `fs-gg-game-core`, so `product-skills/` is
  the more consistent home; `supplied-by` can point at either, so this is purely organizational.
- *Add a `capabilities.yml` entry (like `samples`)*: rejected as unnecessary — `samples` is there only
  because it is a distinct non-runtime *package* capability with a smoke-test project; the collision
  helper has no separate project/package and is exercised through the product's own build/tests.

## R3 — Compile-order entry and delete-safety

**Decision**: Add one gated compile item to `template/base/src/Product/Product.fsproj`:

```xml
<!--#if (profile == "game" || profile == "sample-pack") -->
<Compile Include="Collision.fs" Condition="Exists('Collision.fs')" />
<!--#endif -->
```

Place it **before** `EvidenceCommands.fs`/`Program.fs` and after `Model.fs`/`View.fs` (the helper is
consumed by the model/update, not by the re-export spine).

**Rationale**: `Product.fsproj` already gates compile items and package refs with the C-style
`<!--#if (profile == "game" || profile == "sample-pack") -->` preprocessor (`WindowOptions.fs`, the
Canvas reference), so profile-gated *materialization* is the established mechanism — the file and its
compile line only appear for game/sample-pack (FR-003). The `Condition="Exists('Collision.fs')"` MSBuild
guard makes **deletion** safe (FR-007): remove the file and the compile item silently drops, so the
product still builds with no dangling `Compile Include` — even though `Product.fsproj` itself stays a
"durable — do not touch" file. The governance compile-order scan anchors on the literal
`Compile Include="X.fs"` substring (commit 3fdcf63), which the conditioned item still satisfies, so the
scan is unaffected.

**Alternatives considered**:
- *Ship `Collision.fs` in the ungated base tree (like `WindowOptions.fs`) with only the Compile gated*:
  rejected — the file would then materialize into app/headless/governed products too, violating FR-003
  (no orphaned collision source). Shipping it via a game/sample-pack-gated `template.json` source keeps
  non-collision products clean.
- *Unconditioned `<Compile Include="Collision.fs" />`*: rejected — deleting the file would break the
  build, violating FR-007.
- *Wildcard/recursive `<Compile Include="**/*.fs" />`*: rejected — the repo governs explicit compile
  order; globbing would break the compile-order scan and reorder existing files.

## R4 — Collision response determinism

**Decision**: The response pass is a **pure function of world state** returning resolved bodies /
separation vectors. Determinism rules baked into `Collision.fs`:
- **Broad-phase candidate pairs are emitted in a stable, total order** — iterate bodies in their
  supplied (insertion) order (which `SpatialGrid.query` already preserves) and form pairs `(i, j)` with
  `i < j` by index; never iterate a `Dictionary`/`HashSet`.
- **The pair-processing order does not depend on float comparisons** — order by the integer index key,
  not by penetration depth or distance (which could tie in floating point).
- **Response math is deterministic** — penetration/MTV and separation use the same float ops in the same
  order every run; equal inputs ⇒ bit-identical output. Restitution is a caller-supplied constant, not
  drawn from any generator.

- **Response math stays sqrt-free.** MTV is computed with min/subtraction on the overlap extents (no
  vector normalization); if a radius test is ever needed, reuse `SpatialGrid.queryRadius`'s
  squared-distance comparison. Avoiding `sqrt`/transcendentals keeps IEEE-754 output bit-identical across
  platforms (a `sqrt` result can differ in the last bit between runtimes).

**Rationale**: The helper is expected to run inside the `FS.GG.UI.Canvas` fixed-step loop, whose whole
value is replay-identical simulation (Feature 239/245). Any hash-iteration or float-tie leak would break
replay (FR-008). This mirrors the determinism contract `Pathfinding`/`SpatialGrid` already hold (integer
tie-break / insertion order). Float *arithmetic* is deterministic under IEEE-754 with a fixed operation
order; the hazards are *ordering/among-ties* (removed by the integer-index rule) and *`sqrt`/transcendental
last-bit drift* (removed by the sqrt-free rule).

**Alternatives considered**:
- *Order pairs by penetration depth (deepest first)*: rejected — depth ties break deterministically only
  with a secondary integer key anyway, so index order is simpler and already total.
- *Simultaneous impulse solve*: out of scope (that is the "full physics step" the spec excludes); the
  helper does per-pair positional separation, which is enough for arcade collision and stays pure.

## R5 — Vocabulary reuse (no look-alike geometry)

**Decision**: `Collision.fs` operates on the shared `FS.GG.UI.Scene.Rect`/`Point`. A *body* is `Rect`
(+ an optional caller id/tag); a separation/MTV is a `Point` used as a vector. No new bounds or vector
record is introduced.

**Rationale**: FR-009 and the documented consumer-vs-framework / consumer-vs-consumer `.Pos` footguns:
a second `{ X; Y }`-shaped type invites the bare-record-inference bug. Reusing `Rect`/`Point` also lets
the helper feed `SpatialGrid`/`Geometry` directly with no conversion. The skill's `## Common pitfalls`
restates the geometry-clash footgun (as `fs-gg-scene`/`fs-gg-game-core` already do).

**Alternatives considered**: a dedicated `Body`/`Vec2` record — rejected per above; a tag/id is carried
as a generic `'T` (as `SpatialGrid<'T>` already does) rather than a new type.

## R6 — Change classification and cross-repo contract

**Decision**: **Tier 1 template-contract change** (no F# package public surface). On release: bump the
FS.GG.UI coherent set and, publish-before-flip, update `registry/dependencies.yml`,
`registry/CHANGELOG.md`, and `docs/registry/compatibility.md` in `FS-GG/.github` for the
`fs-gg-ui-template` contract; confirm exact edges through the `cross-repo-coordination` skill.

**Rationale**: The set of files the template emits (skills + product source) is the `fs-gg-ui-template`
contract that generated products and the SDD scaffold-provider consume; adding a skill + a materialized
source file + a compile item changes it. Sibling skill additions (243 audio, 244 persistence) took the
same path. No surface-area baseline is added because no packed public API changes.

**Alternatives considered**: *Tier 2 (local)* — rejected: the emitted-file set is cross-repo observable,
so treating it as local would risk an incoherent registry. The coordination team confirms the precise
version edge at release; the plan schedules the flip either way.

## R7 — Testing the adaptable source (which is a template file)

**Decision**: Two tests.
1. `tests/Package.Tests/Feature246CollisionSkillTests.fs` — coherence: the `catalog` entry, the
   regenerated `skill-manifest.json` digest, the two `template.json` sources, the `skillist-reference.md`
   row, and the dev-root/mirror parity all agree; and the materialize condition is exactly
   `profile ∈ {game, sample-pack}` (present for those, absent otherwise).
2. `tests/Canvas.Tests/CollisionHelperTests.fs` — logic: the raw
   `template/fragments/collision/src/Product/Collision.fs` (literal `namespace Product`, the default
   `sourceName`) is added via `<Compile Include>` and compiles unmodified into `Canvas.Tests`, which
   already references `FS.GG.UI.Canvas` + `FS.GG.UI.Scene`; assert (a) two overlapping bodies produce a
   non-zero separation that removes the overlap, (b) repeat-run byte-identity on a fixed scenario
   (determinism), (c) degenerate inputs (zero-area, exactly touching, contained, empty set) return
   documented totals without throwing.

**Home for the logic test**: `tests/Product.Tests/` does **not** exist in this framework repo — it is
the *generated* product's project (`template/base/tests/Product.Tests/`), which only runs when a product
is scaffolded. The framework-side logic test therefore lives in `tests/Canvas.Tests/` (the existing
project whose references already cover the helper's dependencies), while the generated product's own
`Product.Tests` continues to exercise it after scaffolding.

**Rationale**: Mirrors `Feature240GameCoreSkillTests` (coherence) and the existing `Canvas.Tests`
suite (logic) precedents, giving real fail-before/pass-after evidence (Constitution V) for a deliverable
that is a template file rather than a packed library.

**Alternatives considered**: only a coherence test — rejected: it would leave the collision *logic*
(and its determinism/totality contracts) unverified.
