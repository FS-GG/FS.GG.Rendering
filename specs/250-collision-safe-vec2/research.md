# Phase 0 Research: Collision-Safe Vec2/Position in the Model Template

**Feature**: 250-collision-safe-vec2 · **Spec**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md)

This resolves the open decisions the plan's Technical Context flagged, grounded in the current template and the
sibling helper features (246 collision, 247 visibility, 248 line-drawing, 249 grid-parts).

---

## Decision 1 — Placement: product-owned template helper, not a new framework public type

**Decision**: Ship the collision-safe vector as a **product-owned, adaptable template file**
`template/base/src/Product/Vec2.fs` (module `AppRoot.Geometry`), gated to `game`/`sample-pack`, `Exists`-guarded, and
compiled **before** `Model.fs` — identical in kind to `Collision.fs` / `Visibility.fs` / `Grids.fs` / `LineDrawing.fs`.
Do **not** add a public `FS.GG.UI.Canvas.Vec2` type in this feature.

**Rationale**:
- Issue #138 says "in the **model template**"; the value is authoring-time trap prevention at the scaffold, not a new
  reusable library primitive.
- The template already has an established, four-times-repeated pattern for exactly this shape of thing — "an adaptable
  helper you own," `Exists`-guarded so the durable `Product.fsproj` stays green if you delete it, reusing shared
  `Scene`/`Canvas` vocabulary (`template/base/docs/scaffold-map.md`, `Product.fsproj` lines 9–27). Reusing it keeps the
  feature small, consistent, and free of a Tier-1 public-API commitment.
- Keeping it product-owned means an author can rename `Vx`/`Vy`, add `Z`, or delete the file after a swap — the point
  of the "you own it" tier.

**Alternatives considered**:
- **Public `FS.GG.UI.Canvas.Vec2` (framework primitive, Tier 1).** Attractive symmetry with feature 239 (which shipped
  `Rect` but conspicuously not a vector) and maximal reuse. Rejected **for this feature**: it is a public-surface
  commitment (`.fsi` + surface-area baseline + ApiCompat) beyond the issue's "model template" scope, and the
  collision-safety property is a property of *field labels in the product tree*, which a framework type does not by
  itself guarantee (an author can still declare bare `X`/`Y`). Recorded as a **possible future promotion** (a follow-up
  akin to how `Rect` became a Canvas primitive) — noted, not scheduled.
- **Declare `Vec2` inline at the top of `Model.fs`.** Simplest (no new file/fsproj item). Rejected: `Model.fs` is
  *replaceable*, so a starter swap deletes the safe type exactly when the author most needs the vocabulary to build
  their replacement. A separate `Vec2.fs` survives the `Model.fs` rewrite.
- **Opt-in-only fragment (like the siblings), base starter unchanged.** Rejected: the feature requires the *base*
  starter to be expressed in the safe type (FR-003) and the type available by default, so it must ship in the base
  game tree, not solely as an opt-in fragment. (A `template/fragments/vec2/` mirror MAY still be emitted for
  consistency with siblings — see Decision 5.)

---

## Decision 2 — Field-label scheme: `Vec2 = { Vx: float; Vy: float }` (zero overlap)

**Decision**: The type is `type Vec2 = { Vx: float; Vy: float }` — a 2D vector reused for position, velocity, and
displacement. `Vx`/`Vy` share **no** label with `Scene.Point` (`X`,`Y`) or `Scene.Rect` (`X`,`Y`,`Width`,`Height`),
satisfying SC-002 / FR-001 structurally. Size/hitbox is expressed **not** as a new colliding record but through the
`toRect` interop (Decision 3), so entity records never declare `Width`/`Height` labels either.

**Rationale**:
- The collision is *record-label inference*: a bare `{ X = …; Y = …; Width = …; Height = … }` literal in the durable
  `LayoutEvidence.fs` (which `open`s both `FS.GG.UI.Scene` and `AppRoot.Model`) can resolve to a model record that
  declares those same labels. The robust fix is to guarantee the model's public record labels contain **none** of
  `X`/`Y`/`Width`/`Height`. Distinct labels (`Vx`/`Vy`) make the mis-inference impossible regardless of how many fields
  the literal has or which records are in scope — strictly safer than a 2-field `{X;Y}` `Vec2` that would merely avoid
  the *4-field* Rect literal while re-introducing ambiguity with `Point`.
- `Vx`/`Vy` = "vector component x/y." It reads naturally for velocity (`ball.Velocity.Vx`) and acceptably for position
  (`ball.Pos.Vx`); the doc-comment states the convention. It also matches the repo's existing collision-avoidance
  instinct (the Pong starter already renamed to `CenterX`/`CenterY`).

**Alternatives considered**:
- `{ X: float; Y: float }` (idiomatic Vec2). Rejected: re-introduces the exact `Point` label collision the feature
  exists to remove.
- `{ CenterX; CenterY }` (the starter's current ad-hoc names). Rejected as *type* labels: "center" is a
  position-only reading and reads wrong for a velocity/displacement vector; a general `Vec2` wants neutral components.
  (The starter's fields become `Pos`/`Velocity : Vec2`, so `CenterX`/`CenterY` disappear.)
- Struct-with-hidden-fields + accessors only. Rejected: over-engineered for a value the author is meant to read and
  edit; violates Idiomatic Simplicity.

> **FSI-first (constitution I):** `Vx`/`Vy` is the recommended default; the exact labels are finalized in the FSI
> sketch task before the `.fs` body, per Principle I. Any change from `Vx`/`Vy` must still satisfy SC-002 (zero overlap
> with `Point`/`Rect`).

---

## Decision 3 — Interop with `Scene`: explicit `toPoint` / `toRect`, one step each

**Decision**: `Vec2.fs` exposes total conversions so the safe type integrates with the scene/layout vocabulary in one
obvious step (FR-009):
- `toPoint : Vec2 -> FS.GG.UI.Scene.Point` — `fun v -> { X = v.Vx; Y = v.Vy }`
- `toRect : center: Vec2 -> w: float -> h: float -> FS.GG.UI.Scene.Rect` — a centered AABB, covering the size-bearing
  case (FR-002) without any entity record ever declaring `Width`/`Height`.
- plus pure vector ops used by the demo: `vec2 x y`, `add`, `sub`, `scale`, `clamp` (component clamp for
  playfield/bounds).

**Rationale**: The conversions are the single crossing point between model coordinates and `Scene` records, so bare
`{ X = …; Y = … }` / `{ X = …; Y = …; Width = …; Height = … }` literals appear only inside `Vec2.fs` (where only
`Scene` types are in scope and the resolution is unambiguous) and inside the durable spine (unchanged) — never on an
entity record. Centered `toRect` matches how the starter draws the ball/paddles and how `LayoutEvidence` frames the
active item.

**Alternatives considered**: implicit/operator conversions or a `Size` record on entities (rejected — reintroduces
`Width`/`Height` labels and hides the crossing); leaving size entirely to the author (rejected — FR-002 requires the
size case be covered or its safe naming documented, and a `toRect` helper is the cheaper, testable answer).

---

## Decision 4 — Prevention primary, naming-lint backstop deferred-by-default

**Decision**: The **type is the primary prevention** (an author reaches for `Vec2`/`toRect` and never declares the
colliding labels). Ship a **generated-product test assertion** (Decision in contracts) that fails if the starter model
or `Vec2` reintroduces a `Scene`-colliding label, as the enforceable backstop. A standalone build-time *lint* that
scans arbitrary author records for reused `Scene` labels (FR-007's stronger reading) is **specced but its delivery is a
planning/tasks call** — recommended as a lightweight `GovernanceTests`-style source assertion over the product tree if
cheap, else documented as a known limitation (the compiler's own `FS3566`/`FS0039`, made legible by the upgraded
`Model.fs` comment + skill note, remains the ultimate signal).

**Rationale**: Issue #138 lists "collision-safe type **or** a documented naming lint" as alternatives. The type is the
higher-value, lower-cost mechanism and the one the base starter needs anyway. A full author-record lint has real cost
(where does it run, what's the false-positive story) and belongs behind the type, surfaced not silent (FR-007, VI).

**Alternatives considered**: lint-only (rejected — does not give the base starter a safe vocabulary, only complains);
compiler-analyzer (rejected — constitution notes F# analyzers are effectively no-ops here; disproportionate).

---

## Decision 5 — Accumulator + `stepSim` demonstration via `FixedStep.drain`

**Decision**: Re-express the game starter so `Model` carries the entity positions/velocities as `Vec2` and a
`SimAccumulator: float`; on `Tick`, `update` calls `FixedStep.drain interval frameTime model.SimAccumulator`
(feature 239 primitive, already referenced by the game/sample-pack `Product.fsproj`) and runs a pure
`stepSim : Vec2-based Model -> Model` for the returned step count, carrying the new accumulator. `stepSim` is the
current `stepBall` logic re-expressed over `Vec2`/`add`/`clamp`. Keep it **minimal** — a readable Pong step, not a new
engine.

**Rationale**: FR-004 / US2 ask the starter to *demonstrate* the accumulator + `stepSim` pattern at the edit site.
`FixedStep.drain` is the existing, pure, deterministic primitive for exactly this (its `.fsi` documents
`struct(stepCount, newAccumulator)` and total handling of NaN/negative inputs), so the demo teaches the real,
replay-safe pattern rather than inventing one. The accumulator lives in `Model` (Elmish boundary, Principle IV); no
wall-clock is read inside `update`.

**Alternatives considered**: keep the current 1-step-per-`Tick` starter and only add `Vec2` (rejected — misses FR-004's
explicit "accumulator + stepSim" demonstration); introduce a subscription/timer (rejected — out of scope, and the host
already emits `Tick`).

---

## Decision 6 — Durable re-point + governance/token invariants

**Decision**: Treat `LayoutEvidence.fs` and `EvidenceCommands.fs` as **durable-must-re-point** (per
`scaffold-map.md`): update *only* the model-field reads (`Ball.CenterX`/`CenterY`/`PlayfieldWidth`… → `Ball.Pos.Vx`/…
via `Vec2`/`toPoint`), preserving every scanned evidence token (`hud-region` / `gameplay-region` /
`measurement-mode` / `overlap`) and the six-file compile order. `GovernanceTests.fs` (source-scan) is **not** edited.
`Vec2.fs` inserted before `Model.fs` is explicitly safe (scaffold-map: "a new file inserted before/between/after them
is safe as long as those six keep that relative order").

**Rationale**: The change touches model fields the durable spine reads, which the scaffold-map defines as a re-point
(keep file + tokens, re-point fields), not a forbidden edit. Non-game profiles carry a different `Model`
(`Name`/`RenderCount`, no `X`/`Y`) and are untouched → byte-identical output preserved (FR-010 / SC-006).

**Validation**: composition test (pack→install→instantiate→build→test) for the game profile; a **starter-swap**
assertion (author `Model` in `Vec2`, durable spine + tokens still green) as in the feature-220 swap evidence.

---

## Resolved unknowns

| Unknown (from Technical Context) | Resolution |
| --- | --- |
| New framework public type vs template content? | Template content, product-owned (Decision 1); libraries Tier 2, no `.fsi`/baseline. |
| Field labels that are provably collision-safe? | `Vx`/`Vy`, zero overlap with `Point`/`Rect` (Decision 2); finalized in FSI sketch. |
| How does the safe type reach `Scene`? | Total `toPoint`/`toRect` interop; size case via centered `toRect` (Decision 3). |
| Type, lint, or both (issue's either/or)? | Type primary + test-assertion backstop; author-record lint deferred to a tasks call (Decision 4). |
| What "accumulator + stepSim" means here? | `FixedStep.drain` + a `Model`-carried accumulator + a pure `stepSim`, minimal (Decision 5). |
| Impact on durable spine / governance / non-game? | Durable re-point with tokens preserved; non-game untouched (Decision 6). |
