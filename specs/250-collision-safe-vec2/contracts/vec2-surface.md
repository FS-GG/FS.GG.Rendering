# Contract: Product-Owned `Vec2` Surface + Collision-Safety Assertions

**Feature**: 250-collision-safe-vec2 · **Data model**: [../data-model.md](../data-model.md)

This feature adds **no framework/library public API** (no `FS.GG.UI.*` `.fsi` or surface-area baseline changes — see
plan Constitution Check). The "contract" here is (a) the surface of the emitted **product-owned** `AppRoot.Geometry`
module, and (b) the **generated-product test assertions** that make the collision-safety and behavior guarantees
enforceable. These are validated through a scaffolded game product (the FSI/semantic-test audience of constitution I),
not through a packed library `.fsi`.

## C1 — `AppRoot.Geometry` module surface (product-owned template file `Vec2.fs`)

The generated game/sample-pack product exposes (source, editable by the author):

```
module AppRoot.Geometry
  type Vec2 = { Vx: float; Vy: float }
  val vec2   : float -> float -> Vec2
  val zero   : Vec2
  val add    : Vec2 -> Vec2 -> Vec2
  val sub    : Vec2 -> Vec2 -> Vec2
  val scale  : float -> Vec2 -> Vec2
  val clamp  : min: Vec2 -> max: Vec2 -> Vec2 -> Vec2
  val toPoint: Vec2 -> FS.GG.UI.Scene.Point
  val toRect : center: Vec2 -> w: float -> h: float -> FS.GG.UI.Scene.Rect
```

Stability note: this is **product-owned** source (the "adaptable helper you own" tier). It is not a frozen package
surface — an author may rename/extend/delete it. The template ships it as the safe default; the contract below binds
the *shipped starter*, not the author's later edits.

## C2 — Collision-safety invariant (the load-bearing assertion — SC-002 / FR-001)

**Assert** (generated-product test, real compilation): the field labels of every record type declared in
`AppRoot.Geometry` **and** in the shipped `AppRoot.Model` share **zero** names with `FS.GG.UI.Scene.Point`
(`X`,`Y`) and `FS.GG.UI.Scene.Rect` (`X`,`Y`,`Width`,`Height`).

- Fails before: today's game `Model`/`Ball` avoid it only by ad-hoc `CenterX`/`CenterY`; a test that also guards a
  *newly declared* colliding label (or the pre-fix reproduction) is red without the fix.
- Passes after: `Vx`/`Vy` and the `Vec2`-based `Model` contain none of the four labels.
- Mechanism options (tasks decide the cheaper): reflection over the compiled product record types, or a
  `GovernanceTests`-style source scan of `Model.fs`/`Vec2.fs`. Reflection is preferred (asserts the real compiled
  shape, not text).

## C3 — Durable-spine build-clean invariant (US1 / FR-005 / FR-006)

**Assert**: a freshly scaffolded **game** product with the `Vec2`-based starter builds clean — `dotnet build` exits 0
with **no** `FS3566`/`FS0039` diagnostic originating from `LayoutEvidence.fs` — and `dotnet test` passes, with **no
author edits**. The durable `GovernanceTests.fs` source-scan (six-file order + evidence tokens) is green and unedited.

## C4 — Reproduction guard (the trap is real — plan's early-repro gate)

**Assert** (repro fixture, may be `Synthetic`-tagged if it must fabricate a bad model): a game `Model` that declares
a record with bare `X`/`Y`/`Width`/`Height` and leaves `LayoutEvidence.fs` untouched **fails** to compile with the
expected `FS3566`/`FS0039` wall — confirming the mechanism the fix removes. This is the "fail before" evidence for C2;
it runs once in the Foundational phase and then guards against regressions of the collision vector.

## C5 — Accumulator + `stepSim` behavior (US2 / FR-004)

**Assert**:
1. `Model` carries `SimAccumulator: float` and entity positions/velocities as `Geometry.Vec2` (not bare floats).
2. On `Tick`, `update` advances the sim through `FixedStep.drain` + a pure `stepSim`, and the ball stays inside
   `Playfield` after the step (`clamp`), with byte-identical results for a scripted `frameTime` sequence (determinism).
3. `toPoint`/`toRect` round-trip per the data-model laws.

## C6 — Interop + non-regression (FR-009 / FR-010 / SC-006)

**Assert**: `toPoint`/`toRect` produce the `Scene.Point`/`Rect` the durable spine and `View` consume in one call; and
the `app`/`governed`/`headless-scene` profiles' generated output is byte-identical to pre-change (their `Model` is
untouched) — covered by the existing per-profile composition/golden checks.

## C7 — Authoring guidance surfaced (FR-008 / US3)

**Assert** (docs/skill check): the model-swap authoring guidance (`fs-gg-project` skill note) and the `Model.fs`
comment at the model-editing site both name the `Scene`-field-label collision, name `Geometry.Vec2` as the default, and
state the rule (do not reuse `Scene` field labels on a game record); `scaffold-map.md` lists `Vec2.fs` in the
replaceable "adaptable helper you own" set.
