# Collision-safe Vec2 helper fragment

Ships one **product-owned, adaptable** source file into `game` / `sample-pack` products:

- `src/<ProductDir>/Vec2.fs` — a collision-safe 2D vector (`Geometry.Vec2`, position/velocity/displacement)
  plus interop into **both** framework vocabularies — `toPoint` / `toRect` (render) and
  `toSimPoint` / `ofSimPoint` / `toSimRect` (simulation) — that you **own and edit**.

## Why this exists

A game model that stores entity positions naturally reaches for fields named `X` / `Y` (and `Width` / `Height`
for a size). But the durable `LayoutEvidence.fs` opens **both** `FS.GG.UI.Scene` and your model, and it builds
`Rect` records with **bare labels** (`{ X = …; Y = …; Width = …; Height = … }`). With your model also declaring
those labels, F#'s record-label inference can resolve those literals to **your** record instead of `Rect` — a wall
of type errors in a file you were told not to touch, surfacing only after a whole model is written
(the *fs-gg-scene* pitfall).

`Geometry.Vec2` removes the trap **structurally**: its labels `Vx` / `Vy` reuse **none** of `Scene.Point`
(`X`, `Y`) or `Scene.Rect` (`X`, `Y`, `Width`, `Height`), so a model built on it can never trip the mis-inference.
Express an entity's size with `toRect` (a centered AABB) rather than `Width` / `Height` labels, and the size case
stays safe too.

## It's yours

Unlike the framework packages (`FS.GG.UI.Scene` / `.Canvas`), this is **not** a referenced API — it is copied into
your product for you to change: rename `Vx` / `Vy`, add a `Z`, add rotation/normalization, or delete the file after
you swap `Model.fs` off it. Its `Compile` item is `Exists`-guarded, so deleting `Vec2.fs` keeps the build green
(`Product.fsproj` stays a "durable — do not touch" file).

Note: unlike the purely-additive `Collision.fs` / `Grids.fs` fragments, the **shipped starter `Model.fs` depends on
this file** — it expresses the starter's own positions as `Vec2` and demonstrates the accumulator + `stepSim` loop.
So delete `Vec2.fs` only together with (or after) swapping the starter model off it.

See the **`fs-gg-model-swap`** and **`fs-gg-game-core`** skills for the collision rule, the accumulator + `stepSim`
+ `Tick` pattern, and the durable-vs-replaceable map. Guidance-only; no backing package (`no-public-surface`).
