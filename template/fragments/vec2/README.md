# Collision-safe Vec2 helper fragment

Ships one **product-owned, adaptable** source file into `game` / `sample-pack` products:

- `src/<ProductDir>/Vec2.fs` — a collision-safe 2D vector (`Geometry.Vec2`, position/velocity/displacement)
  plus interop into **both** framework vocabularies — `toPoint` / `toRect` (render) and
  `toSimPoint` / `ofSimPoint` / `toSimRect` / `ofSimRectCenter` (simulation) — that you **own and edit**.

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

## Consuming this fragment from another repo (#570)

"No backing package" above means **no product references `Vec2` from a package** — that is the point, and it is
what keeps the file yours to rename, extend or delete. It does **not** mean the source is unpublished.

`.template.package/FS.GG.UI.Template.fsproj` packs the repo under `content/`, so the canonical file ships inside
the **`FS.GG.UI.Template`** package at a stable path:

```
content/template/fragments/vec2/src/Product/Vec2.fs
```

**Generate from that; do not re-declare it.** `FS.GG.Game` used to hand-maintain a twin in
`scripts/skill-block-context/_scaffold.fs` because it "cannot reference ours" — true of a *reference*, false of the
*source*. A twin is a second shape that can drift, and only a test stood between a rename here and a downstream
gate reporting green over a fiction (#519). Restoring the template package and generating from the real file removes
the twin instead of guarding it (FS-GG/FS.GG.Game#141).

The file is compilable **verbatim** by a consumer that is not a generated product: it carries no `dotnet new`
conditional, and its namespace (`AppRoot`) is fixed. It defines **both** crossings itself — `toPoint` / `toRect`
into the render vocabulary, and `toSimPoint` / `ofSimPoint` / `toSimRect` / `ofSimRectCenter` into the simulation
one — so a consumer needs the two packages those vocabularies live in (`FS.GG.UI.Scene` and `FS.GG.Game.Core`)
and nothing else. That is what lets a consumer compile the render/sim crossings, which a hand-written twin
deliberately **cannot**: faking `toPoint` against the sim `Point` would be the exact lie such a gate exists to
catch, so the twin omits the helpers a game product most needs to get right.

> **Mind the pin: the SIM half is newer than the published template.** An earlier version of this note said both
> edges "resolve from published packages", and that was wrong in a way that cost somebody real time — it read as
> though `toSimPoint` / `toSimRect` were members of `FS.GG.Game.Core`. They are not: **this file defines them**,
> and `FS.GG.Game.Core` supplies only the `Point` / `Rect` **types** they cross into. Worse, the sim half landed
> in #446 (PR #530), **after** the `fs-gg-ui/v0.9.0` tag — so the fragment inside the **published**
> `FS.GG.UI.Template` still carries only `toPoint` / `toRect`. A consumer generating from the published package
> today gets the render edge and no sim edge, and will conclude the sim crossing does not exist.
>
> It does. It is on `main`, it ships with the next framework release, and until then a consumer wanting the sim
> half must generate from `main` rather than from the package. FS-GG/FS.GG.Rendering#603 is where that was
> rediscovered; FS-GG/FS.GG.Rendering#587 is the release that closes it.
>
> The lesson is the one #550 taught and this note repeated anyway: **a claim about what a consumer can bind is a
> claim about the PUBLISHED artifact, never about `main`.**

Publishing the source rather than a generated surface declaration is deliberate: a declaration would be a *third*
statement of the same shape, with its own generator and its own drift gate — one more copy to keep in step, which
is what #570 set out to remove. The source cannot disagree with itself.

`tests/Rendering.Harness.Tests/Feature570PublishedScaffoldGeometryTests.fs` holds this contract: the package's
`Content` item is a broad `..\**\*` glob, so the fragment ships **by default rather than by decision**, and one
added `Exclude` would stop publishing a file another repo compiles against with nothing to say so.
