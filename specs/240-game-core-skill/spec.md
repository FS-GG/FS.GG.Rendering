# Feature Specification: `fs-gg-game-core` — product skill for simulation patterns

**Feature Branch**: `240-game-core-skill`

**Created**: 2026-07-04

**Status**: Draft

**Input**: Resolves **FS-GG/FS.GG.Rendering#73** — the **P1 Rendering** child of epic
**FS-GG/.github#165**. Source: Space Invaders consumer feedback §5 item 6 (Friction). Pairs with the
simulation primitives shipped in **Feature 239** (`Geometry` / `Rng` / `FixedStep`). Goes through the
skill-union machinery established by **ADR-0017** (FS-GG/.github#163) and **Feature 238**
(per-skill `materializes-when` / `supplied-by` on the product skill-manifest).

## Context (non-normative)

FS.GG.UI ships a family of **product skills** — condition-gated `SKILL.md` bodies that materialize into
a generated product's `.agents/skills/` so the product's coding agent knows how to consume each
capability. Today the family covers rendering (`fs-gg-scene`), layout (`fs-gg-layout`), input
(`fs-gg-keyboard-input`), styling, widgets, viewer, symbology, testing — but **not simulation**. A
game/sim consumer that wants a deterministic update loop, seeded randomness, collision, or off-screen
culling has no product skill telling it that FS.GG.UI already ships these primitives, so it re-rolls
them by hand (the Space Invaders feedback: a mutable `System.Random` in the `Model`, a hand-written
accumulator, an inline AABB test).

Feature 239 closed the **library** half of that gap by shipping three pure, additive helpers:

| Helper | Package / namespace | Public surface (verbatim `.fsi`) |
|---|---|---|
| `Geometry` | `FS.GG.UI.Scene` | `intersects`, `contains`, `containsPoint`, `center`, `ofCenter`, `sweptIntersects` |
| `Rng` | `FS.GG.UI.Canvas` | type `Rng = { State: uint64 }`; `ofSeed`, `nextFloat`, `nextInt`, `split` (each draw returns `struct(value * Rng)`) |
| `FixedStep` | `FS.GG.UI.Canvas` | `defaultMaxFrameTime`, `drain`, `drainWith` (`drain interval frameTime accumulator -> struct(int * float)`) |

This feature closes the **guidance** half: a new `fs-gg-game-core` product skill that teaches a
consumer to reach for those helpers — fixed-timestep march, RNG determinism threaded through the MVU
`Model`, AABB + swept collision, and entity culling against the visible `Rect` — instead of
re-implementing them.

Because it adds a product skill, it MUST also flow through the skill-union machinery so the union gate
knows the skill exists and can enforce its presence/absence honestly (Feature 238): a new
`template.json` body source, a new provider directory `template/product-skills/fs-gg-game-core/`, a new
generator-catalog entry, and a regenerated `template/skill-manifest/skill-manifest.json` carrying the
skill's `materializes-when` + `supplied-by`.

### Scope boundary

- **In scope (Rendering, P1):** the `fs-gg-game-core` `SKILL.md` body; its `template.json` source + gate
  condition; its generator-catalog entry; the regenerated skill-manifest; the skill-manifest /
  materialization tests updated to expect a 13th skill; product-doc cross-links (`template/base/docs/`)
  so the skill is discoverable.
- **Out of scope:** any change to the Feature 239 library surface (`Geometry` / `Rng` / `FixedStep` are
  consumed as-is, not modified); the cross-repo `registry/skills.yml` / `skill-union-assert.sh`
  tightening (owned by FS-GG/.github#164 — this feature only supplies the honest 13-entry input); any
  new runtime code, sample game, or viewer wiring.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — A game consumer discovers the simulation primitives (Priority: P1)

A developer scaffolds a `profile=game` FS.GG.UI product and asks its coding agent to add a game loop
with collision. The agent finds the `fs-gg-game-core` skill in the product's `.agents/skills/`, reads
that FS.GG.UI already ships `FixedStep.drain`, `Rng`, and `Geometry`, and wires them instead of
re-rolling a mutable `System.Random` and a hand-written accumulator.

**Why this priority**: This is the whole point of the feature — a simulation consumer must be *told*
the primitives exist, in the same place they already learn about scene/layout/input. Without the
materialized skill body there is nothing to discover.

**Independent Test**: Scaffold a `profile=game` product; confirm `.agents/skills/fs-gg-game-core/SKILL.md`
is present and its byte content matches the manifest `sha256`; confirm the body names each of
`FixedStep.drain`, `Rng.ofSeed`/`nextInt`/`split`, and `Geometry.intersects`/`sweptIntersects` with a
compilable consumer snippet.

**Acceptance Scenarios**:

1. **Given** a product scaffolded with `profile=game`, **When** its skill set is materialized, **Then**
   `fs-gg-game-core` is present in `.agents/skills/` alongside `fs-gg-scene`/`fs-gg-layout`/`fs-gg-keyboard-input`.
2. **Given** the materialized `fs-gg-game-core/SKILL.md`, **When** a reader follows its usage snippet,
   **Then** the snippet calls only the real Feature-239 public surface (no private/renamed members) and
   compiles against the packed `Scene`/`Canvas` `.fsi`.
3. **Given** the skill body advises RNG determinism, **When** the reader applies it, **Then** the
   guidance threads the value-type `Rng` through the MVU `Model` (never a mutable `System.Random`),
   consistent with Constitution principle IV.

### User Story 2 — The skill materializes only for simulation profiles (Priority: P1)

A developer scaffolds a non-simulation product (`profile=governed`, `headless-scene`, or a plain
`app`). The `fs-gg-game-core` skill is **declared** by the manifest but its body is legitimately
**absent**, and the union gate does not flag it as a supply failure.

**Why this priority**: A simulation skill in a non-simulation product is noise, and an honest
`materializes-when` is what lets the Feature-238/`.github#164` union gate tell "correctly suppressed"
from "missing". Getting the gate condition wrong either leaks the skill everywhere or trips the gate.

**Independent Test**: Evaluate the recorded `materializes-when` against each profile's params; confirm
it is **true** for `profile=game` and `profile=sample-pack` and **false** for `app` / `headless-scene`
/ `governed`; confirm a scaffold of each non-sim profile omits the body while the manifest still
declares it.

**Acceptance Scenarios**:

1. **Given** `profile=game` (or `sample-pack`) params, **When** the manifest `materializes-when` is
   evaluated, **Then** it is `true` and the body is emitted.
2. **Given** `profile=app`, `headless-scene`, or `governed` params, **When** it is evaluated, **Then**
   it is `false`, the body is absent, and the manifest still lists the entry (declared ∧
   condition-false ∧ absent — legitimate, not `[missing]`).
3. **Given** the regenerated manifest, **When** its `materializes-when` for `fs-gg-game-core` is
   compared to the condition on that skill's `template.json` body source, **Then** they are equal (no
   drift), exactly as Feature 238 enforces for the other entries.

### User Story 3 — The manifest and gate stay coherent at 13 skills (Priority: P2)

A maintainer regenerates the skill-manifest. The generator, the on-disk manifest, and the tests all
agree on **thirteen** product skills; `generate-skill-manifest.fsx --check` reports up-to-date and no
existing skill's digest, path, or condition changes.

**Why this priority**: The manifest is machine-generated and digest-checked; adding a skill without
updating the generator catalog and the count-aware tests fails the build. This story guarantees the
addition is coherent rather than partial.

**Independent Test**: Run the generator in `--check` mode after the edit; confirm zero drift and a
13-entry manifest; confirm the twelve pre-existing entries are byte-identical to before.

**Acceptance Scenarios**:

1. **Given** the new catalog entry, **When** `generate-skill-manifest.fsx --check` runs, **Then** it
   reports the on-disk manifest up-to-date with 13 entries sorted ascending by `id`.
2. **Given** the regeneration, **When** the twelve prior entries are diffed, **Then** their `sha256`,
   `resolvablePath`, `materializes-when`, and `supplied-by` are unchanged.

### Edge Cases

- **`sample-pack` overlaps `game`.** A sample-pack product is a legitimate simulation host, so the
  condition includes `sample-pack`; confirm both profiles emit the body and the gate accepts both.
- **`app` deliberately excluded.** A generic desktop `app` is not a simulation; the skill must NOT leak
  into `app` (unlike `fs-gg-scene`/`fs-gg-skiaviewer`, which do include `app`). This is an intentional
  narrowing — the requirements table records it.
- **Body drift vs. digest.** If the `SKILL.md` body is edited without regenerating, the digest test must
  fail (existing Feature-231 behavior, now covering the 13th entry).
- **Snippet references a non-existent member.** If the skill body names a member the Feature-239 `.fsi`
  does not export, the "surface-referenced" check (see FR-008) must fail.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001 — new product skill body.** A canonical `SKILL.md` MUST exist at
  `template/product-skills/fs-gg-game-core/SKILL.md`, `name: fs-gg-game-core`, with a one-line
  `description` in the family's voice ("… in a generated FS.GG.UI product"). It MUST cover the four
  simulation patterns from #73: **fixed-timestep march**, **RNG determinism**, **AABB + swept
  collision**, and **entity culling**.
- **FR-002 — content maps onto the real Feature-239 surface.** Every primitive the body advises MUST
  name the actual public member: `FixedStep.drain` / `drainWith` / `defaultMaxFrameTime`;
  `Rng.ofSeed` / `nextFloat` / `nextInt` / `split` and the value type `Rng = { State: uint64 }`;
  `Geometry.intersects` / `contains` / `containsPoint` / `center` / `ofCenter` / `sweptIntersects`.
  Culling MUST be expressed as `Geometry.intersects`/`containsPoint` against the visible `Rect`
  (no new API is introduced). Snippets MUST use the `struct(value, next)` draw convention and the
  documented totality (degenerate inputs return documented values, never throw).
- **FR-003 — determinism guidance is MVU-shaped.** The RNG section MUST show the value-type `Rng`
  threaded through the consumer's `Model` (draw → store `next` state back in the model), explicitly
  contrasting it with a mutable `System.Random` in the `Model`, consistent with Constitution IV and the
  Feature-239 design intent.
- **FR-004 — `template.json` body source.** `.template.config/template.json` MUST gain one source
  emitting `template/product-skills/fs-gg-game-core/` → `.agents/skills/fs-gg-game-core/` with
  `copyOnly: ["**/*"]` (matching the other product-skill sources so the body ships byte-verbatim and its
  digest holds), gated by the FR-006 condition.
- **FR-005 — generator catalog entry.** `scripts/generate-skill-manifest.fsx` MUST gain a catalog entry
  `("fs-gg-game-core", "template/product-skills/fs-gg-game-core/SKILL.md", <FR-006 condition>)`,
  keeping the catalog sorted so the emitted manifest stays ascending by `id`.
- **FR-006 — `materializes-when` scoped to simulation profiles.** The skill's emission condition MUST be
  `(profile == "game" || profile == "sample-pack")` — verbatim in both the `template.json` source and
  the generator catalog (single source of truth), and therefore in the regenerated manifest's
  `materializes-when`. `app`, `headless-scene`, and `governed` are deliberately excluded (see Edge
  Cases). The manifest entry's `supplied-by` MUST be `template/product-skills/fs-gg-game-core/`.
- **FR-007 — regenerated, coherent manifest.** `template/skill-manifest/skill-manifest.json` MUST be
  regenerated to 13 entries; the new entry carries the correct `sha256`, `resolvablePath`
  `.agents/skills/fs-gg-game-core/SKILL.md`, `materializes-when`, and `supplied-by`; the twelve prior
  entries are unchanged; `generate-skill-manifest.fsx --check` reports up-to-date.
- **FR-008 — tests updated for 13 skills.** The Package.Tests that assert manifest shape / digests /
  materialization (Feature 231 / 238 suites) MUST be updated to expect 13 skills, to recompute and
  match the new digest, and to assert the new entry's `materializes-when` equals the condition on its
  `template.json` body source (no drift) and evaluates **true** under `profile=game`/`sample-pack`,
  **false** under `app`/`headless-scene`/`governed`. A check MUST assert every FS.GG.UI member the skill
  body names in FR-002 exists in the packed Feature-239 `.fsi` surface (guards against a snippet that
  references a renamed/absent member).
- **FR-009 — no regression to existing skills or lanes.** No other skill's body, condition, digest, or
  lane changes. The existing lifecycle/profile suites (Feature 204 / 219 / 231 / 238) stay green; the
  materialized skill set of every currently-tested profile is unchanged except that `game` and
  `sample-pack` products gain `fs-gg-game-core`.
- **FR-010 — discoverability.** Product-facing docs (`template/base/docs/product.md` collision/RNG/loop
  guidance) MUST cross-reference the `fs-gg-game-core` skill so a reader of the shipped docs is pointed
  at it, mirroring how existing capabilities cross-link their skills.

### Key Entities

- **`fs-gg-game-core` product skill**: a condition-gated `SKILL.md` body; attributes = `id`, canonical
  source path, `materializes-when`, `supplied-by`, `sha256`. Sibling of the other twelve product skills.
- **Skill-manifest entry**: the 13th `skills[]` record in `skill-manifest.json` describing the above.
- **Simulation profiles**: `game` and `sample-pack` — the params under which the body materializes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** — A `profile=game` scaffold contains `.agents/skills/fs-gg-game-core/SKILL.md`, byte-equal
  to `template/product-skills/fs-gg-game-core/SKILL.md` and to the manifest `sha256`; a `profile=app` /
  `headless-scene` / `governed` scaffold does **not**, while the manifest still declares the entry.
- **SC-002** — `generate-skill-manifest.fsx --check` reports the on-disk 13-entry manifest up-to-date;
  the twelve pre-existing entries are byte-identical to the pre-feature manifest.
- **SC-003** — A test asserts the new `materializes-when` equals the `template.json` source condition and
  evaluates `true` for `game`/`sample-pack`, `false` for `app`/`headless-scene`/`governed`.
- **SC-004** — Every FS.GG.UI member named in the skill body resolves in the packed Feature-239
  `Scene`/`Canvas` `.fsi`; a deliberately-broken reference fails the check.
- **SC-005** — Issue #73 can close: a game/sample-pack consumer's coding agent has a materialized
  simulation skill, and the union gate (once `.github#164` reads these conditions) classifies
  `fs-gg-game-core` as legitimately suppressed under non-sim profiles rather than `[missing]`.

## Assumptions

- **`game` and `sample-pack` are the simulation profiles.** #73 says "profile in [game, sample-pack]";
  `app` is excluded because a generic app is not a simulation. If a future profile becomes a sim host,
  extending the single condition string (template.json + catalog) is the one-line change.
- **The library surface is fixed.** `Geometry`/`Rng`/`FixedStep` shipped in Feature 239 and are consumed
  verbatim; this feature adds no library code and changes no existing `.fsi`.
- **The manifest stays hand-free.** It is only ever produced by `generate-skill-manifest.fsx`; the
  13-entry manifest is fully regenerated, never hand-edited (existing convention).
- **The cross-repo union gate is owned elsewhere.** `.github#164` owns `registry/skills.yml` and the
  gate that fails on `[missing]`/`[unexpected]`; this feature only makes Rendering's manifest the honest
  13-entry input.
- **Skill body is prose + snippets, not runnable product code.** Like the sibling product skills, it
  ships as guidance; its correctness is proven by the digest test, the surface-referenced check, and a
  compilable snippet against the packed `.fsi` — not by a new test project.
