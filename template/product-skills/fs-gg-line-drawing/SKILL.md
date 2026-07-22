---
name: fs-gg-line-drawing
description: Draw and walk lines across a tile grid in a generated FS.GG.UI product — the deterministic Bresenham cell line, the no-diagonal-gap supercover, and grid line-of-sight over an adaptable helper you own, reusing Cell.
---

# Grid Line-Drawing Capability

## Scope

Use this skill for **grid line-drawing** in a game/sim product: answering *"which tiles does the straight
line between these two cells pass through?"* — the ordered cell line (the **Bresenham** walk), the
**supercover** variant that touches every cell with no diagonal gap, and the point-to-point **line-of-
sight** query built on the same walk. It is the workhorse behind tile line-of-sight, beam/ray attacks,
drawing walls/roads/rivers between two tiles, and moving an agent along a straight path. The grid
vocabulary reuses the framework primitive; the cell walk is game-opinionated and ships as **adaptable
source you own** (`src/<ProductDir>/LineDrawing.fs`), not a frozen package. Everything here is pure,
total, deterministic, and bounded — safe to call from a replayed `update`/`view`. The algorithm is the
classic grid line drawing from the Red Blob Games reference (see **Sources**). Advancing the world on a
fixed step and routing agents is [[fs-gg-game:fs-gg-game-core]]'s job; the *continuous* (float `Point`) sibling is
[[fs-gg-visibility]]. This skill materializes for the `game` and `sample-pack` profiles.

## Public Contract

The grid vocabulary you consume is bundled framework surface; the line-drawing layer is your own product
source:

- `docs/api-surface/Game.Core/Pathfinding.fsi` — the shared integer `Cell` (`{ Col; Row }`) every line is
  expressed over, and the `Cell -> bool` walkability/transparency predicate convention. Shipped in
  `FS.GG.Game.Core` (`game`/`sample-pack`).
- `src/<ProductDir>/LineDrawing.fs` — **product-owned, adaptable** source: `line`, `supercover`, and
  `lineOfSight`. Yours to edit or delete.

All entry points are **total**: degenerate inputs (`a = b`, axis-aligned, diagonal, any octant) return a
documented value, they never throw.

## The grid model

A line is expressed over the shared `Cell` — an **integer** grid coordinate (a discrete tile index),
deliberately not the float `Point`. `Cell` is what `Pathfinding` already routes over, so a line and a
path speak the same vocabulary. Build your endpoints from your world (the player's tile, the target's
tile) and pass them straight in — do **not** re-roll a `(row, col)` record.

## The Bresenham line

`LineDrawing.line a b` returns the **thin**, diagonal-connected cell line from `a` to `b` (both endpoints
included; `a = b` → `[a]`). Each step advances one or both axes by 1, so consecutive cells differ by at
most 1 in each axis.

```fsharp
open FS.GG.Game.Core       // Cell
// LineDrawing lives in your product's own namespace (LineDrawing.fs).

let a = { Col = 0; Row = 0 }
let b = { Col = 5; Row = 2 }
let tiles = LineDrawing.line a b     // ordered cells a..b — draw a road, a beam, a movement track
```

**Determinism is by design, not float lerp.** The walk is **integer Bresenham** — an integer error
accumulator, no floating-point interpolation, no `Math.Round`, no transcendental. Identical endpoints
yield a byte-identical cell list across runs and platforms (safe under replay). The Red Blob Games article
presents a `lerp`-and-`round` form first for clarity; that last-bit rounding can differ across runtimes
and flip a cell — keep the integer form for anything replayed.

## The supercover

`LineDrawing.supercover a b` returns every cell the segment *touches*, strictly **4-connected** (each step
differs by exactly 1 in exactly one axis), so there is **no diagonal gap**. Use this — not `line` — for
sight through walls: a thin Bresenham line steps diagonally and would let sight slip through the corner
where two diagonal walls meet.

## Line-of-sight

`LineDrawing.lineOfSight isTransparent a b` walks the supercover tiles and returns `true` when no tile
**strictly between** `a` and `b` fails the predicate. `isTransparent` is a `Cell -> bool` map — the same
shape as `Pathfinding`'s `isWalkable`, so one map drives both routing and sight. The endpoints are never
tested (you can look FROM and AT an opaque tile); `a = b` → `true`.

```fsharp
let wall = { Col = 3; Row = 1 }
let isTransparent (c: Cell) = c <> wall   // your fog/wall map
LineDrawing.lineOfSight isTransparent a b // false — the wall tile blocks sight
```

## The framework `Los` module — mode-selected and symmetric

`LineDrawing.fs` is your adaptable copy of a contract the framework also ships **canonically**: the
`FS.GG.Game.Core.Los` module, promoted from the frozen game profile's line-drawing fragment (where every
game that wanted sight copied it and diverged). Reach for it directly when you want the promoted version
rather than the editable one — it carries two entry points the thin `line` / `supercover` / `lineOfSight`
trio does not, both keyed on a `LineMode` (`Thin` | `Supercover`):

```fsharp
open FS.GG.Game.Core       // Cell, Los, LineMode

let beam  = Los.trace Thin a b          // diagonal-connected Bresenham — cuts corners
let track = Los.trace Supercover a b    // 4-connected supercover — no diagonal gap
```

`Los.trace mode a b` is `line` or `supercover` selected by `mode` at the call site (both endpoints
included, `a` first), so a caller switches the corner policy with a value instead of picking a function.

`Los.lineOfSightBy mode isTransparent a b` is line-of-sight under an **explicit** `LineMode`, and — unlike
the fixed-`Supercover` `lineOfSight` — it is **symmetric in every mode**:

```fsharp
let canSee = Los.lineOfSightBy Supercover isTransparent a b   // symmetric sight; the default policy
```

`Los.lineOfSightBy m p a b = Los.lineOfSightBy m p b a` because the tiles are traced over the *canonical*
ordered endpoint pair (`min(a, b)` → `max(a, b)`), so both argument orders test one identical cell
sequence. This is the invariant that makes combat fair — without it a unit can shoot one that cannot shoot
back — and `Thin` does **not** get it for free: its fixed error-tie break visits different intermediate
cells depending on which endpoint the walk starts from. The endpoint rule matches `lineOfSight` (endpoints
never tested; `a = b` is `true`). The continuous (float `Point`) grid-FOV sibling `Fov` lives in
[[fs-gg-visibility]].

## Applications

- **Tile line-of-sight / FOV** — `lineOfSight` from the viewer to each candidate tile (roguelike sight,
  guard cones, shooting checks).
- **Beam / ray attacks** — `line` (or a length-capped slice) for a laser/arrow that stops at the first
  solid tile.
- **Drawing walls / roads / rivers** — `line` or `supercover` between two tiles as a level-editor brush.
- **Movement along a line** — step an agent through the `line` cells for a straight dash/charge.

## The adaptable helper

`LineDrawing.fs` is **yours** — a small, readable file classified *replaceable* in the scaffold map (see
[[fs-gg-game:fs-gg-model-swap]]). Switch `line` for `supercover`, cap the returned list for a limited-range beam,
make `lineOfSight` stop-and-report the first blocker, or delete the file if you don't need it: its
`Compile` item is `Exists`-guarded, so the build stays green and you never touch the durable
`Product.fsproj`.

## Common pitfalls

- **Re-rolling a grid coordinate.** Reuse the framework `Cell`; don't define a look-alike `(row, col)` /
  grid-position record that shadows it (the [[fs-gg-game:fs-gg-game-core]] `Cell` vocabulary). And don't conflate
  `Cell` (discrete integer tile) with the float `Point` ([[fs-gg-scene]]) — they are different atoms.
- **Sorting/sampling the line by float `lerp`.** The reference article uses `lerp` + `round` for clarity,
  but its last bit can differ across runtimes and flip a cell — breaking replay determinism. Keep the
  integer Bresenham (it is already the default); only fall back to float sampling for a purely cosmetic,
  non-replayed effect.
- **Using the thin `line` for sight.** A thin Bresenham line steps diagonally and leaks sight through a
  diagonal wall join. Use `supercover` (the 4-connected walk) for line-of-sight — `lineOfSight` already
  does.
- **Testing the endpoints in LOS.** `lineOfSight` deliberately never tests `a`/`b`, so you can see from
  and at an opaque tile. If you re-roll it, keep that convention or a wall you stand on blocks all sight.
- **Deleting `LineDrawing.fs` and then editing `Product.fsproj`.** You don't need to — the compile item is
  `Exists`-guarded. Leave `Product.fsproj` alone.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned line-drawing examples (assert the line connects
its endpoints and each step is adjacent; a blocked tile hides a target and removing it restores sight;
determinism replays; degenerate/all-octant totality).

## Evidence

Record line-drawing evidence (connectivity/endpoint cases, LOS blocked/clear, determinism replays) under
this product's `readiness/` paths. Do not copy framework readiness reports into the product.

## Package Boundary

`Cell` and the `Pathfinding` predicate convention are in `FS.GG.Game.Core` (referenced only on the
`game`/`sample-pack` profiles). `LineDrawing.fs` is **product-owned source with no backing package**. Keep
rendering in [[fs-gg-scene]] and host wiring in [[fs-gg-skiaviewer]].

## Generated Product

Build endpoints from your world each fixed step, call `LineDrawing.line`/`supercover`/`lineOfSight` from
your `update`/`view`, and hand the result to your `View` — draw the tiles, gate a shot, or reveal fog.
Pair it with [[fs-gg-collision]] and [[fs-gg-visibility]] for a full geometry pass, and [[fs-gg-game:fs-gg-game-core]]
for the routing the line complements.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is **mandatory** —
consult **official online docs first** (the F#/.NET docs and the Red Blob Games reference), then community
sources. If your product uses Spec Kit, record findings and resolving links under the feature's
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** line and any product-local
`docs/`. Offline, the mandate degrades to recording "research blocked — <why>" rather than hard-failing.

## Related

- [[fs-gg-visibility]] — the *continuous* (float `Point`, angular-sweep) line-of-sight sibling; this skill
  is its discrete-grid counterpart.
- [[fs-gg-collision]] — the per-frame geometry pass (detection + response) over the shared vocabulary.
- [[fs-gg-game:fs-gg-game-core]] — the simulation loop, RNG, and `Cell` **pathfinding** the line complements (route a
  path, then draw/step along a line).
- [[fs-gg-scene]] — owns the float `Point`/`Rect`; renders the tiles a line produces.
- [[fs-gg-skiaviewer]] — drives the fixed-step loop from the host window.
- [[fs-gg-game:fs-gg-model-swap]] — classifies `LineDrawing.fs` as replaceable/adaptable source.

## Sources / links

- Red Blob Games, "Line Drawing on a Grid": https://www.redblobgames.com/grids/line-drawing/
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- Bresenham's line algorithm background: https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
