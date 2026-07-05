# Phase 0 Research: 2D Visibility Skill + Import-and-Adapt Helper Source

All decisions below resolve the Technical Context so no `NEEDS CLARIFICATION` remains for Phase 1. The
feature deliberately mirrors the collision feature (246); where a decision is identical in shape, the
rationale notes the shared precedent.

## R1 — Delivery mode: package API vs. adaptable source

**Decision**: Ship the **ray-segment intersection + angular sweep** as **product-owned, adaptable
source** (`Visibility.fs`) materialized into the game/sample-pack product; keep the **geometry
vocabulary** (`Point`/`Rect`/`Geometry`/`SpatialGrid`) as the existing package API it reuses. Do **not**
add a new packable library or `.fsi`.

**Rationale**: The vector/box vocabulary and broad-phase bucketing are objective and already frozen —
`Point`/`Rect`/`Geometry.center`/`containsPoint` (`FS.GG.UI.Scene`) and `SpatialGrid.build`/`query`/
`queryRadius` (`FS.GG.UI.Canvas`). The visibility computation itself is *per-game policy* the consumer
edits (sight radius, field-of-view cone, polygon vs. per-cell mask output, soft vs. hard edges) — the
wrong thing to freeze behind a surface-baselined `.fsi`. The requester said "add supporting source code,
same as with collision detection," which is the consumer-owned-source shape, not a referenced package.
This is exactly the collision R1 decision applied to visibility: detection primitives stay frozen; the
opinionated layer (there: response; here: the sweep) ships as adaptable source.

**Alternatives considered**:
- *New `FS.GG.UI.Visibility` package*: rejected — freezes policy every game overrides, adds a surface
  baseline and coherent-set member for a thing meant to be edited, and contradicts the ask.
- *Extend `Geometry` with ray-segment / angle functions*: rejected — pollutes objective AABB primitives
  with opinionated sweep policy; still frozen; still can't be deleted/adapted. (`Geometry` today is
  AABB-only: `intersects`/`contains`/`containsPoint`/`center`/`ofCenter`/`sweptIntersects` — no ray,
  segment, or angle math. That absence is deliberate; the helper fills it in consumer-owned source.)

## R2 — Where the skill and source live, and how they register

**Decision**: Skill body at `template/product-skills/fs-gg-visibility/SKILL.md` (sibling to
`fs-gg-collision`); adaptable source at `template/fragments/visibility/src/Product/Visibility.fs` with a
fragment `README.md`. Register the skill through the **skill-only** path, identical to `fs-gg-collision`:

1. `scripts/generate-skill-manifest.fsx` — add to the `catalog` list (after `fs-gg-ui-widgets`):
   `"fs-gg-visibility", "template/product-skills/fs-gg-visibility/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"`.
2. `template/skill-manifest/skill-manifest.json` — regenerated entry (id, sha256 of the body,
   `resolvablePath: .agents/skills/fs-gg-visibility/SKILL.md`, `materializes-when: profile in [game, sample-pack]`,
   `supplied-by: template/product-skills/fs-gg-visibility/`).
3. `.template.config/template.json` — **two** gated `sources[]`:
   - skill: `condition (profile == "game" || profile == "sample-pack")`, `source template/product-skills/fs-gg-visibility/`,
     `target .agents/skills/fs-gg-visibility/`, `copyOnly ["**/*"]`.
   - source: `condition (profile == "game" || profile == "sample-pack")`, `source template/fragments/visibility/src/Product/`,
     `target src/Product/` (sourceName substitution applies — becomes `src/<ProductDir>/`).
4. **Gate-enforced coherent set** (see [[adding-a-product-skill-touchpoints]] — several are enforced by
   separate test projects and are easy to miss):
   - `tests/Package.Tests/Feature231SkillManifestTests.fs` **and** `Feature238SkillMaterializesWhenTests.fs`
     — add `("fs-gg-visibility", "template/product-skills/fs-gg-visibility/SKILL.md")` to the
     `canonicalSources` list in **both**.
   - `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` — framework product-skill count `15 → 16`.
   - `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` — add `fs-gg-visibility` to the `game`
     **and** `sample-pack` `expectedFrameworkSkills` sets (and any `.agents`-only source count).
   - `scripts/validate-lifecycle-template.fsx` — `frameworkChecked = 15 → 16`.
5. **Dev roots + wrapper + mirror** (Deterministic gate → `Rendering.Harness.Tests` skill inventory /
   parity): `.agents/skills/fs-gg-visibility/SKILL.md` (canonical, byte-identical to the product-skills
   body) + `.claude/skills/fs-gg-visibility/` mirror via `template/lifecycle/materialize-skill-roots.fsx`;
   **and the thin `fs-gg-product-visibility` wrapper in both `.agents/` and `.claude/`** (frontmatter
   `name: fs-gg-product-visibility` + the canonical description, body pointing at
   `../../../template/product-skills/fs-gg-visibility/SKILL.md`; the two differ only by the
   "Codex-active" vs "Claude-active" line). Parity asserted by `scripts/check-agent-skill-parity.fsx` /
   `dotnet run --project tools/Rendering.Harness -- skill-parity` (0 findings) → regenerate
   `docs/reports/skills-parity.md`.

**Rationale**: `capabilities.yml` lists only package/fragment-backed *package* capabilities, and
`skillist-reference.md` is a **curated subset** — the collision skill (246) is in **neither** (verified:
`grep -i collision template/base/docs/skillist-reference.md` is empty), so `fs-gg-visibility` follows the
same path and touches **neither** file. The `template.json` gate condition MUST be *semantically equal* to
the manifest `materializes-when` (Feature 238 enforces), so both use the same `profile ∈ {game,
sample-pack}` expression.

> **Correction vs. the 246 contract doc.** The collision `contracts/skill-and-registration.md` listed a
> `skillist-reference.md` edit and did not enumerate the `canonicalSources`/count-bump gate edits. The
> **shipped** collision feature did the opposite — no skillist edit, and the count/`canonicalSources`
> edits *are* required. This plan follows the shipped reality (captured in
> [[adding-a-product-skill-touchpoints]]), not the earlier doc.

**Alternatives considered**:
- *Skill under the fragment (`template/fragments/visibility/skill/`, like `fs-gg-samples`)*: workable but
  the visibility skill is a first-class capability sibling of `fs-gg-collision`, so `product-skills/` is
  the consistent home; `supplied-by` can point at either, so this is purely organizational.
- *Add a `capabilities.yml` / `skillist-reference.md` entry*: rejected — matches the collision/game-core
  precedent (skill-only capabilities appear in neither).

## R3 — Compile-order entry and delete-safety

**Decision**: Add one gated compile item to `template/base/src/Product/Product.fsproj`:

```xml
<!--#if (profile == "game" || profile == "sample-pack") -->
<Compile Include="Visibility.fs" Condition="Exists('Visibility.fs')" />
<!--#endif -->
```

Place it in the same game/sample-pack region as `Collision.fs`, **before** `Model.fs` (the helper is
consumed by the model/update/view, not by the re-export spine).

**Rationale**: identical mechanism to collision R3. `Product.fsproj` already gates compile items with the
C-style `<!--#if (profile == "game" || profile == "sample-pack") -->` preprocessor (`WindowOptions.fs`,
`Collision.fs`, the Canvas reference), so profile-gated *materialization* is established — the file and
its compile line appear only for game/sample-pack (FR-003). The `Condition="Exists('Visibility.fs')"`
MSBuild guard makes **deletion** safe (FR-007): remove the file and the compile item silently drops, so
the product still builds with no dangling `Compile Include` — while `Product.fsproj` stays a "durable — do
not touch" file. The governance compile-order scan anchors on the literal `Compile Include="X.fs"`
substring (commit 3fdcf63), which the conditioned item still satisfies, so the scan is unaffected.

**Alternatives considered**: shipping in the ungated base tree (would materialize into app/headless too,
violating FR-003); an unconditioned `<Compile Include>` (deleting the file would break the build,
violating FR-007); wildcard globbing (breaks the governed explicit compile order). All rejected — same as
collision R3.

## R4 — Angular-sweep determinism (the load-bearing decision)

**Decision**: The sweep is a **pure function of world state** returning an ordered visibility polygon.
Determinism rules baked into `Visibility.fs`:

- **Order endpoints by a cross-product angular comparator, NOT `atan2`.** Sort the candidate endpoints
  around the source by (half-plane above/below the source, then the sign of the 2D cross product) — a
  total rotational order computed from `Point` subtraction and multiplication only. This avoids the
  `atan2` transcendental, whose last bit can differ across runtimes and could flip the order of two
  near-collinear endpoints, changing the polygon.
- **Break exact angular ties by a stable integer key** — endpoints collinear from the source (shared
  corners, collinear walls) resolve first by sqrt-free squared distance from the source, then by the
  endpoint's integer index in supplied order. Never iterate a `Dictionary`/`HashSet`.
- **Choose the nearest crossing segment by a sqrt-free parametric distance.** Ray-segment intersection
  yields a parameter `t ≥ 0` along the ray; "nearest" compares `t` (or squared distance), never a
  `sqrt`ed length — keeping IEEE-754 output bit-identical across platforms.
- **The sweep visits endpoints in the sorted order deterministically**; the emitted vertex list is a pure
  function of `(source, segments, radius)`.

**Rationale**: The helper is expected to run inside the `FS.GG.UI.Canvas` fixed-step loop, whose value is
replay-identical simulation (Feature 239/245). Any hash-iteration, `atan2` last-bit drift, or `sqrt`-tie
leak would break replay (FR-008). This mirrors the collision R4 determinism contract (integer tiebreak,
sqrt-free) and the ordering discipline `Pathfinding`/`SpatialGrid` already hold. Note the Red Blob Games
article sorts by `atan2` angle for clarity; the FS.GG determinism requirement is *stronger* than the
article's, so this plan substitutes the cross-product comparator (same order, no transcendental) — a
documented, intentional divergence from the reference.

**Alternatives considered**:
- *Sort by `atan2` angle (the article's approach) with an integer tiebreak*: rejected for the primary
  path — exact ties break fine, but near-ties can flip on last-bit `atan2` drift across platforms,
  breaking byte-identity. Acceptable for a non-replayed cosmetic light; the helper documents the
  cross-product comparator as the deterministic default and notes `atan2` as the simpler-but-driftier
  option for consumers who do not need replay.
- *Full angular plane sweep with an ordered active-set tree*: out of scope — the endpoint-sort +
  nearest-hit-per-wedge approach is enough for arcade visibility and stays pure and simple.

## R5 — Bounding the rays (FR-011)

**Decision**: Every ray is bounded by a **sight radius** carried in `Settings.Radius`; a ray that strikes
no segment terminates on the bound. Implement the bound as the four edges of the axis-aligned box
`[source ± radius]` (added to the segment set as synthetic bound walls) so an unhit ray always hits the
box — reusing the **same radius** as the `SpatialGrid.queryRadius` broad-phase cull, so culled-out
occluders can never affect the (bounded) result.

**Rationale**: The angular sweep needs a finite closed boundary or unhit rays have no terminus (FR-011).
A box bound is sqrt-free (axis-aligned edge intersection is a parametric line test, no circle/`sqrt`),
keeps the polygon a simple closed ring, and unifies the bound with the cull radius so the two can never
disagree. A circular bound is available as a documented consumer edit (it needs a ray-circle test) but is
not the default because the `sqrt` reintroduces the determinism hazard R4 removes.

**Alternatives considered**:
- *Unbounded rays clipped to an infinite plane*: rejected — no finite polygon, FR-011 unmet.
- *Circle bound by default*: rejected for the default — `sqrt` in the ray-circle test risks last-bit
  drift; offered as an edit, not the shipped default.

## R6 — Vocabulary reuse (no look-alike geometry)

**Decision**: `Visibility.fs` operates on the shared `FS.GG.UI.Scene.Point`/`Rect`. A wall is a small
`Segment = { A: Point; B: Point }` record (two shared `Point`s — the minimal domain concept the shared
vocabulary genuinely lacks, and not a look-alike of `Point`/`Rect`); a ray direction and a hit are
`Point`s; the bound region is a `Rect`. No new point/vector/bounds record is introduced.

**Rationale**: FR-009 and the documented consumer-vs-framework / consumer-vs-consumer `.Pos` footguns: a
second `{ X; Y }`-shaped type invites the bare-record-inference bug, so the helper never defines one.
`Segment` is a *pair of shared points*, not a competing vector type, and is the one concept neither
`Point` (a location) nor `Rect` (an AABB) expresses. Reusing `Point`/`Rect` also lets the helper feed
`SpatialGrid`/`Geometry` directly with no conversion. The skill's `## Common pitfalls` restates the
geometry-clash footgun (as `fs-gg-scene`/`fs-gg-collision` already do).

**Alternatives considered**: a dedicated `Vec2`/`Ray` record — rejected per above; a ray is carried as
`origin: Point` + `dir: Point`, not a new type.

## R7 — Change classification and cross-repo contract

**Decision**: **Tier 1 template-contract change** (no F# package public surface). On release: bump the
FS.GG.UI coherent set and, publish-before-flip, update `registry/dependencies.yml`,
`registry/CHANGELOG.md`, and `docs/registry/compatibility.md` in `FS-GG/.github` for the
`fs-gg-ui-template` contract; confirm exact edges through the `cross-repo-coordination` skill.

**Rationale**: identical to collision R6. The set of files the template emits (skills + product source) is
the `fs-gg-ui-template` contract that generated products and the SDD scaffold-provider consume; adding a
skill + a materialized source file + a compile item changes it. Sibling skill additions (243 audio, 244
persistence, 246 collision) took the same path. No surface-area baseline is added because no packed public
API changes.

**Alternatives considered**: *Tier 2 (local)* — rejected: the emitted-file set is cross-repo observable,
so treating it as local would risk an incoherent registry.

## R8 — Testing the adaptable source (which is a template file)

**Decision**: Two tests, mirroring collision R7.
1. `tests/Package.Tests/Feature247VisibilitySkillTests.fs` — coherence: the `catalog` entry, the
   regenerated `skill-manifest.json` digest, the two `template.json` sources, the dev-root/wrapper/mirror
   parity, and the materialize condition (exactly `profile ∈ {game, sample-pack}` — present for those,
   absent otherwise) all agree.
2. `tests/Canvas.Tests/VisibilityHelperTests.fs` — logic: the raw
   `template/fragments/visibility/src/Product/Visibility.fs` (literal `namespace Product`, the default
   `sourceName`) is added via `<Compile Include>` and compiles unmodified into `Canvas.Tests`, which
   already references `FS.GG.UI.Canvas` + `FS.GG.UI.Scene`; assert (a) a target behind a wall is **not**
   visible and **is** visible with the wall removed (occlusion correctness), (b) the visibility polygon is
   a closed ring bounded by the radius, (c) repeat-run byte-identity on a fixed scenario including
   equal-angle endpoints (determinism), (d) degenerate inputs (empty segment set, zero-length segment,
   source on a wall/endpoint, collinear/near-parallel grazing ray) return documented totals without
   throwing or emitting NaN.

**Home for the logic test**: `tests/Product.Tests/` does **not** exist in this framework repo — it is the
*generated* product's project. The framework-side logic test therefore lives in `tests/Canvas.Tests/`
(references already cover the helper's dependencies), while the generated product's own `Product.Tests`
exercises it after scaffolding. Mirrors collision R7 exactly.

**Rationale**: gives real fail-before/pass-after evidence (Constitution V) for a deliverable that is a
template file rather than a packed library; mirrors `Feature246CollisionSkillTests` (coherence) +
`CollisionHelperTests` (logic).

**Alternatives considered**: only a coherence test — rejected: it would leave the sweep *logic* (and its
determinism/bound/totality contracts) unverified.
