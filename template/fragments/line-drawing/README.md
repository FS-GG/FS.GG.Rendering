# Line-drawing helper fragment

Ships one **product-owned, adaptable** source file into `game` / `sample-pack` products:

- `src/<ProductDir>/LineDrawing.fs` — a grid line-drawing pass (integer Bresenham `line`, the
  4-connected `supercover` walk, and a `lineOfSight` query over a `Cell -> bool` transparency map) that
  you **own and edit**.

Unlike the framework packages (`FS.GG.UI.Scene`/`.Canvas`), this is **not** a referenced API — it is
copied into your product for you to change: switch the thin line for the supercover, cap the length for a
limited-range beam, make line-of-sight stop-and-report the first blocker, or delete the file entirely. Its
`Compile` item is `Exists`-guarded, so deleting `LineDrawing.fs` keeps the build green (`Product.fsproj`
stays a "durable — do not touch" file).

The grid vocabulary still reuses the framework primitive (`Cell`, and the `Pathfinding` `Cell -> bool`
predicate convention) — the fragment adds only the cell walk that does not belong in a frozen package.

See the **`fs-gg-line-drawing`** skill for the full `Cell` model → Bresenham → supercover → line-of-sight
guidance, the applications (tile line-of-sight, beam attacks, drawing walls/roads, movement along a line),
and the common footguns. Algorithm reference: <https://www.redblobgames.com/grids/line-drawing/>.
Guidance-only; no backing package (`no-public-surface`).
