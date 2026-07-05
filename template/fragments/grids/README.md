# Grid-parts helper fragment

Ships one **product-owned, adaptable** source file into `game` / `sample-pack` products:

- `src/<ProductDir>/Grids.fs` — the grid-parts vocabulary (an `Edge` and a `Vertex` value with one
  canonical coordinate each, the six part-to-part adjacency conversions, and the pixel mapping) that you
  **own and edit**.

Unlike the framework packages (`FS.GG.UI.Scene`/`.Canvas`), this is **not** a referenced API — it is
copied into your product for you to change: move the grid origin, add a diagonal-edge variant, reorder
the corners, extend it toward hex/triangle grids, or delete the file entirely. Its `Compile` item is
`Exists`-guarded, so deleting `Grids.fs` keeps the build green (`Product.fsproj` stays a "durable — do
not touch" file).

The face and pixel vocabulary still reuses the framework primitives — `FS.GG.UI.Canvas.Cell` is the
**face**, `FS.GG.UI.Scene.Point`/`Rect` are the pixels — the fragment adds only the `Edge`/`Vertex`
parts and the conversions the shared vocabulary genuinely lacks and that do not belong in a frozen
package.

See the **`fs-gg-grids`** skill for the full parts model → adjacency → pixel-map guidance, the
applications (edge-walls, autotiling / marching-squares, region borders, snapping), and the common
footguns. Algorithm references: <https://www.redblobgames.com/grids/parts/> and
<https://www.redblobgames.com/grids/edges/>. Guidance-only; no backing package (`no-public-surface`).
